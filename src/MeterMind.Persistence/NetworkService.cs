using MeterMind.Application;
using MeterMind.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace MeterMind.Persistence;

public sealed class NetworkService(MeterDb db, IOptions<HealthThresholds> thresholds) : INetworkService
{
    public async Task<DateTimeOffset> Clock(CancellationToken ct) => await db.Simulations.Select(s => (DateTimeOffset?)s.Clock).SingleOrDefaultAsync(ct) ?? DateTimeOffset.UtcNow;
    public async Task<Page<MeterView>> Search(MeterFilter f, CancellationToken ct = default)
    {
        if (f.Limit is < 1 or > 1000 || f.Offset < 0 || f.RssiBelow is > 0 or < -200 || f.Status is not null && !new[] { "online", "degraded", "offline", "unknown" }.Contains(f.Status)) throw new ArgumentException("Invalid meter filter.");
        var q = db.Meters.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(f.Search)) q = q.Where(m => m.Serial.Contains(f.Search) || m.NodeAddress.ToString() == f.Search);
        if (f.RssiBelow is not null) q = q.Where(m => m.Rssi < f.RssiBelow);
        if (f.SectorId is not null) q = q.Where(m => m.SectorId == f.SectorId);
        if (f.SinkId is not null) q = q.Where(m => m.SinkId == f.SinkId);
        if (f.GatewayId is not null) q = q.Where(m => db.Sinks.Any(s => s.Id == m.SinkId && s.GatewayId == f.GatewayId));
        if (f.MinHops is not null) q = q.Where(m => m.HopCount >= f.MinHops);
        if (f.MinPdr is not null) q = q.Where(m => m.Pdr >= f.MinPdr);
        if (f.MaxLatencyMs is not null) q = q.Where(m => m.LatencyMs <= f.MaxLatencyMs);
        if (f.Firmware is not null) q = q.Where(m => m.Firmware == f.Firmware);
        var now = await Clock(ct); var t = thresholds.Value; var stale = now.AddMinutes(-t.StaleMinutes); var offline = now.AddMinutes(-t.OfflineMinutes);
        if (f.Status == "offline") q = q.Where(m => m.LastCommunication != null && m.ObservedAt != null && m.LastCommunication < offline);
        if (f.Status == "unknown") q = q.Where(m => m.LastCommunication == null || m.ObservedAt == null || (m.LastCommunication >= offline && (m.ObservedAt < stale || m.Rssi == null || m.Pdr == null)));
        if (f.Status is "online" or "degraded")
        {
            q = q.Where(m => m.LastCommunication >= offline && m.ObservedAt >= stale && m.Rssi != null && m.Pdr != null);
            q = f.Status == "online" ? q.Where(m => m.Rssi >= t.WeakRssi && m.Pdr >= t.MinPdr && (m.ReadSuccess == null || m.ReadSuccess >= .9)) : q.Where(m => m.Rssi < t.WeakRssi || m.Pdr < t.MinPdr || m.ReadSuccess < .9);
        }
        var count = await q.LongCountAsync(ct); var rows = await q.OrderBy(m => m.Serial).Skip(f.Offset).Take(f.Limit).ToListAsync(ct);
        return new(rows.Select(m => new MeterView(m, HealthClassifier.Classify(m, now, t), m.Pdr is null ? null : 1 - m.Pdr)).ToList(), count, DateTimeOffset.UtcNow, now);
    }
    public async Task<MeterView> Details(string id, CancellationToken ct = default)
    {
        var m = Guid.TryParse(id, out var g) ? await db.Meters.AsNoTracking().SingleOrDefaultAsync(m => m.Id == g, ct) : await db.Meters.AsNoTracking().SingleOrDefaultAsync(m => m.Serial == id, ct);
        if (m == null) throw new KeyNotFoundException("Meter not found."); return new(m, HealthClassifier.Classify(m, await Clock(ct), thresholds.Value), m.Pdr is null ? null : 1 - m.Pdr);
    }
    public async Task<object> Summary(CancellationToken ct = default)
    {
        var clock = await Clock(ct); var rows = await db.Meters.AsNoTracking().ToListAsync(ct); var states = rows.GroupBy(m => HealthClassifier.Classify(m, clock, thresholds.Value)).ToDictionary(g => g.Key, g => g.Count());
        var start = clock.AddHours(-24);
        var trend = await db.Telemetry.Where(t => t.Timestamp >= start && t.Quality != "missing").GroupBy(t => new { t.Timestamp.Year, t.Timestamp.Month, t.Timestamp.Day, t.Timestamp.Hour }).Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, Rssi = g.Average(x => x.Rssi), Pdr = g.Average(x => x.Pdr), ReadSuccess = g.Average(x => x.ReadSuccess) }).OrderBy(g => g.Year).ThenBy(g => g.Month).ThenBy(g => g.Day).ThenBy(g => g.Hour).ToListAsync(ct);
        return new { Total = rows.Count, States = states, Gateways = await db.Gateways.CountAsync(ct), Sinks = await db.Sinks.CountAsync(ct), WeakSignal = rows.Count(m => m.Rssi < thresholds.Value.WeakRssi), AveragePdr = rows.Average(m => m.Pdr), AverageReadSuccess = rows.Average(m => m.ReadSuccess), OpenIncidents = await db.Set<Incident>().CountAsync(i => i.Status == "open", ct), Trend = trend, Mode = rows.All(m => m.Transport.StartsWith("simulated")) ? "simulation" : "integrated", DataTime = clock, QueryTime = DateTimeOffset.UtcNow, Thresholds = thresholds.Value };
    }
    public async Task<object> Topology(DateTimeOffset? at = null, CancellationToken ct = default)
    {
        var meters = await db.Meters.AsNoTracking().ToListAsync(ct); var clock = await Clock(ct);
        if (at is not null) { if (at > clock) throw new ArgumentException("Playback time exceeds data time."); var ids = meters.Select(m => m.Id).ToArray(); var routes = await db.Routes.Where(r => ids.Contains(r.MeterId) && r.Timestamp <= at).GroupBy(r => r.MeterId).Select(g => g.OrderByDescending(r => r.Timestamp).First()).ToListAsync(ct); foreach (var m in meters) { var r = routes.FirstOrDefault(r => r.MeterId == m.Id); m.NextHopId = r?.NextHopId; m.SinkId = r?.SinkId; m.HopCount = r?.HopCount; } }
        return new { Meters = meters.Select(m => new MeterView(m, at is null ? HealthClassifier.Classify(m, clock, thresholds.Value) : "unknown", m.Pdr is null ? null : 1 - m.Pdr)), Sinks = await db.Sinks.AsNoTracking().ToListAsync(ct), Gateways = await db.Gateways.AsNoTracking().ToListAsync(ct), Neighbors = await db.Neighbors.AsNoTracking().ToListAsync(ct), At = at ?? clock, HistoricalStatus = at is null ? "current" : "routing only; current health is not historical health" };
    }
    public async Task<object> Neighbors(string id, CancellationToken ct = default) { var m = (await Details(id, ct)).Meter; return await db.Neighbors.Where(n => n.MeterId == m.Id).ToListAsync(ct); }
    public async Task<object> Routes(string id, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) { Range(from, to); var m = (await Details(id, ct)).Meter; return await db.Routes.Where(r => r.MeterId == m.Id && r.Timestamp >= from && r.Timestamp <= to).OrderBy(r => r.Timestamp).Take(1000).ToListAsync(ct); }
    public async Task<object> Events(string id, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) { Range(from, to); var m = (await Details(id, ct)).Meter; return await db.Set<MeterEvent>().Where(r => r.MeterId == m.Id && r.Timestamp >= from && r.Timestamp <= to).OrderByDescending(r => r.Timestamp).Take(1000).ToListAsync(ct); }
    public async Task<object> Profiles(string id, string profile, DateTimeOffset from, DateTimeOffset to, int limit = 500, CancellationToken ct = default)
    {
        Range(from, to); if (limit is < 1 or > 2000) throw new ArgumentException("Profile limit must be 1–2000.");
        if (!await db.Set<ProfileDefinition>().AnyAsync(p => p.Key == profile, ct)) throw new ArgumentException("Unknown profile."); var m = (await Details(id, ct)).Meter;
        if (profile.EndsWith("events")) return new { Items = await db.Set<MeterEvent>().Where(r => r.MeterId == m.Id && r.Profile == profile && r.Timestamp >= from && r.Timestamp <= to).OrderByDescending(r => r.Timestamp).Take(limit).ToListAsync(ct), From = from, To = to, QueryTime = DateTimeOffset.UtcNow };
        var q = db.Readings.Where(r => r.MeterId == m.Id && r.Profile == profile && r.Timestamp >= from && r.Timestamp <= to);
        return new { Items = await q.OrderByDescending(r => r.Timestamp).Take(limit).ToListAsync(ct), Total = await q.LongCountAsync(ct), From = from, To = to, QueryTime = DateTimeOffset.UtcNow, Units = new { Voltage = "V", Current = "A", ActivePower = "kW", ApparentPower = "kVA", Energy = "kWh", Frequency = "Hz" } };
    }
    public async Task<object> Gateways(CancellationToken ct = default) { var ms = await db.Meters.ToListAsync(ct); var ss = await db.Sinks.ToListAsync(ct); var gs = await db.Gateways.ToListAsync(ct); return gs.Select(g => new { Gateway = g, Sinks = ss.Where(s => s.GatewayId == g.Id), Meters = ms.Count(m => ss.Any(s => s.GatewayId == g.Id && m.SinkId == s.Id)), ReadSuccess = ms.Where(m => ss.Any(s => s.GatewayId == g.Id && m.SinkId == s.Id)).Select(m => m.ReadSuccess).DefaultIfEmpty().Average() }); }
    public async Task<object> Affected(Guid id, CancellationToken ct = default) { var ms = await db.Meters.AsNoTracking().ToListAsync(ct); var sinks = await db.Sinks.AsNoTracking().ToListAsync(ct); if (!ms.Any(m => m.Id == id) && !sinks.Any(s => s.Id == id || s.GatewayId == id)) throw new KeyNotFoundException(); var roots = sinks.Where(s => s.Id == id || s.GatewayId == id).Select(s => s.Id).Append(id).ToHashSet(); var result = new HashSet<Guid>(); bool changed; do { changed = false; foreach (var m in ms) if ((m.NextHopId is not null && (roots.Contains(m.NextHopId.Value) || result.Contains(m.NextHopId.Value))) && !result.Contains(m.Id)) { result.Add(m.Id); changed = true; } } while (changed); return new { FailureNode = id, Interpretation = "Potential downstream impact under selected observed routes; no failure has been executed.", Meters = ms.Where(m => result.Contains(m.Id)).Select(m => new { m.Id, m.Serial }), Count = result.Count }; }
    public async Task<Diagnosis> Diagnose(string id, CancellationToken ct = default)
    {
        var view = await Details(id, ct); var m = view.Meter; var now = await Clock(ct); var start = now.AddDays(-1);
        var events = await db.Set<MeterEvent>().Where(e => e.MeterId == m.Id && e.Timestamp >= start).ToListAsync(ct);
        var failed = await db.Set<CommunicationAttempt>().CountAsync(e => e.MeterId == m.Id && !e.Success && e.Timestamp >= start, ct);
        var changes = await db.Routes.CountAsync(e => e.MeterId == m.Id && e.Timestamp >= start, ct);
        var gateway = m.SinkId is null ? null : await db.Gateways.SingleOrDefaultAsync(g => db.Sinks.Any(s => s.Id == m.SinkId && s.GatewayId == g.Id), ct);
        var history = await db.Telemetry.Where(t => t.MeterId == m.Id && t.Timestamp >= start && t.Quality != "missing").OrderBy(t => t.Timestamp).Select(t => new SignalEvidence(t.Rssi, t.Pdr)).ToListAsync(ct);
        var findings = DiagnosticRules.Evaluate(m, view.Status, events.Select(e => e.Code).ToArray(), failed, changes, gateway?.Online, history);
        return new(m.Id, m.Serial, findings, Math.Min(100, findings.Count * 15 + (view.Status == "offline" ? 40 : 0)), "Rule-based prioritization; not a calibrated probability", DateTimeOffset.UtcNow);
    }
    public static void Range(DateTimeOffset from, DateTimeOffset to) { if (from > to || to - from > TimeSpan.FromDays(366)) throw new ArgumentException("Time range must be ordered and at most 366 days."); }
}
