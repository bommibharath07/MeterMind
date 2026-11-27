using MeterMind.Domain;
namespace MeterMind.Application;

public record SignalEvidence(double? Rssi, double? Pdr);
public static class DiagnosticRules
{
    public static IReadOnlyList<Finding> Evaluate(Meter m, string status, string[] eventCodes, int failedReads, int routeChanges, bool? gatewayOnline, IReadOnlyList<SignalEvidence> history)
    {
        var f = new List<Finding>();
        void Add(string rule, string conclusion, string quality, string[] evidence, string[] alternatives, string[] actions) => f.Add(new(rule, conclusion, quality, evidence, alternatives, actions));
        if (m.Rssi < -90) Add("weak-signal", "Weak signal observed", "direct measurement", [$"Latest RSSI {m.Rssi:F1} dBm"], ["Obstruction", "Antenna installation", "Distance"], ["Inspect placement and compare adjacent meters"]);
        if (m.Neighbors < 2) Add("coverage-gap", "Few reachable neighbors", "direct observation", [$"Neighbor count {m.Neighbors}"], ["Neighbor diagnostics may be incomplete"], ["Survey local coverage"]);
        if (m.Rssi < -100 && m.Neighbors < 2) Add("antenna", "Possible antenna or severe coverage issue", "hypothesis", ["Very weak RSSI and sparse neighbors"], ["Building attenuation", "Missing diagnostics"], ["Inspect antenna before replacing hardware"]);
        if (m.Pdr < .9 && m.Retries > 5) Add("congestion", "RF congestion or interference is plausible", "correlated indicators", [$"PDR {m.Pdr:P1}; retries {m.Retries}"], ["Weak signal", "Gateway load"], ["Survey RF spectrum and compare retry trends"]);
        if (routeChanges > 5) Add("routing-instability", "Frequent route changes", "direct observation", [$"{routeChanges} route changes in 24 hours"], ["Planned topology changes"], ["Inspect unstable upstream nodes"]);
        if (gatewayOnline == false) Add("gateway", "Associated gateway is offline", "direct gateway status", ["Gateway reports offline"], ["Gateway status may be stale"], ["Check gateway backhaul and supply"]);
        if (eventCodes.Contains("firmware-error")) Add("firmware", "Firmware error event recorded", "direct event", ["firmware-error event"], ["Transient software fault"], ["Review approved firmware release and logs"]);
        if (eventCodes.Contains("power-loss")) Add("power", "Supply interruption event recorded", "direct event", ["power-loss event"], ["Event may precede a separate radio fault"], ["Verify supply restoration"]);
        if (failedReads > 0 || m.ReadSuccess < .9) Add("dlms", "Profile collection failures observed", "direct attempt", [$"{failedReads} failed attempts in 24 hours; read success {m.ReadSuccess:P1}"], ["Transport loss", "Authentication or schedule mismatch"], ["Inspect collection error codes and profile catalog"]);
        if (history.Count >= 2 && history[0].Rssi is double first && history[^1].Rssi is double last && last - first < -6) Add("signal-trend", "Signal declined over the observed day", "trend heuristic", [$"RSSI change {last - first:F1} dB"], ["Diurnal conditions", "Installation changes"], ["Review coverage before failure occurs"]);
        if (status == "offline" && f.Count == 0) Add("insufficient-evidence", "Communication is stale; cause is unknown", "insufficient", ["Fresh communication not observed"], ["Power", "RF", "Gateway", "Collection issue"], ["Collect gateway and local event evidence"]);
        return f;
    }
}
