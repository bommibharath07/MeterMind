using MeterMind.Application;
using MeterMind.Domain;
namespace MeterMind.Diagnostic.Tests;

public class RuleTests
{
    [Theory]
    [InlineData("weak-signal")]
    [InlineData("coverage-gap")]
    [InlineData("antenna")]
    [InlineData("congestion")]
    [InlineData("routing-instability")]
    [InlineData("gateway")]
    [InlineData("firmware")]
    [InlineData("power")]
    [InlineData("dlms")]
    public void KnownControlledCauseTriggersExpectedEvidenceRule(string cause) { var m = new Meter { Rssi = -70, Pdr = .99, Neighbors = 6, Retries = 0, ReadSuccess = 1 }; var events = Array.Empty<string>(); var failed = 0; var changes = 0; bool? online = true; switch (cause) { case "weak-signal": m.Rssi = -95; break; case "coverage-gap": m.Neighbors = 0; break; case "antenna": m.Rssi = -105; m.Neighbors = 0; break; case "congestion": m.Pdr = .7; m.Retries = 10; break; case "routing-instability": changes = 8; break; case "gateway": online = false; break; case "firmware": events = ["firmware-error"]; break; case "power": events = ["power-loss"]; break; case "dlms": failed = 3; break; } var findings = DiagnosticRules.Evaluate(m, "degraded", events, failed, changes, online, []); var f = Assert.Single(findings, f => f.Rule == cause); Assert.NotEmpty(f.Evidence); Assert.NotEmpty(f.Alternatives); Assert.NotEmpty(f.Actions); }
    [Fact] public void OfflineWithoutEvidenceDoesNotInventRootCause() { var findings = DiagnosticRules.Evaluate(new Meter { Neighbors = 5 }, "offline", [], 0, 0, null, []); Assert.Contains(findings, f => f.Rule == "insufficient-evidence" && f.EvidenceQuality == "insufficient"); Assert.DoesNotContain(findings, f => f.Rule == "power" || f.Rule == "gateway"); }
    [Fact] public void HealthyControlHasNoFalsePositive() { Assert.Empty(DiagnosticRules.Evaluate(new Meter { Rssi = -70, Pdr = .99, Neighbors = 6, ReadSuccess = .99 }, "online", [], 0, 0, true, [])); }
}
