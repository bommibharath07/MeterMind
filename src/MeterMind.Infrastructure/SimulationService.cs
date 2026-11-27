using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Persistence;
using MeterMind.Simulator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
namespace MeterMind.Infrastructure;

public sealed class SimulationService(MeterDb db, ITenantContext tenant, IConfiguration config, IOptions<SimulationOptions> options)
{
    void Guard() { if (!tenant.CanOperate || tenant.UtilityId != Demo.UtilityId || !config.GetValue<bool>("Simulation:Enabled")) throw new UnauthorizedAccessException("Simulation control is restricted to the isolated demo utility."); }
    public async Task<object> Status(CancellationToken ct = default) => await db.Simulations.AsNoTracking().SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Seed this demo first.");
    public async Task SetRunning(bool value, CancellationToken ct = default) { Guard(); await db.Simulations.ExecuteUpdateAsync(s => s.SetProperty(x => x.Running, value), ct); await Audit(value ? "start" : "stop", ct); }
    public async Task Reset(CancellationToken ct = default)
    {
        Guard(); await using var tx = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(714300)", ct);
        // Only the fixed demo utility is eligible. No TRUNCATE and no database drop.
        foreach (var type in new[] { typeof(ToolExecution), typeof(GeneratedReport), typeof(AuditLog), typeof(Conversation), typeof(WorkOrder), typeof(Incident), typeof(DiagnosticRecord), typeof(CommunicationAttempt), typeof(MeterEvent), typeof(ProfileReading), typeof(Telemetry), typeof(RouteChange), typeof(Neighbor), typeof(Meter), typeof(Sink), typeof(Gateway), typeof(ProfileDefinition), typeof(ObisObject), typeof(MeshNetwork), typeof(Sector), typeof(Simulation) })
        {
            var table = db.Model.FindEntityType(type)!.GetTableName()!;
            // Identifiers come only from the fixed entity-type allowlist above. Values are always parameters.
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = tx.GetDbTransaction(); command.CommandText = $"DELETE FROM \"{table.Replace("\"", "\"\"")}\" WHERE \"UtilityId\"=@utility";
            var parameter = command.CreateParameter(); parameter.ParameterName = "utility"; parameter.Value = tenant.UtilityId; command.Parameters.Add(parameter); await command.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct); db.ChangeTracker.Clear(); await new HistoricalSeeder(db).Seed(options.Value, ct); await Audit("reset", ct);
    }
    public async Task Tick(CancellationToken ct = default)
    {
        Guard(); await using var tx = db.Database.CurrentTransaction is null?await db.Database.BeginTransactionAsync(ct):null; await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(714300)", ct);
        var sim = await db.Simulations.SingleOrDefaultAsync(ct); if (sim is null || !sim.Running) { if(tx is not null)await tx.CommitAsync(ct); return; }
        var at = sim.Clock.AddMinutes(5); var ms = await db.Meters.ToListAsync(ct);
        foreach (var m in ms)
        {
            var connected = m.NextHopId is not null; m.ObservedAt = at; if (connected) m.LastCommunication = at;
            m.Rssi = connected ? -76 + 15 * Math.Sin((at - sim.Start).TotalHours + m.NodeAddress) : null; m.Pdr = connected ? .98 : null;
            db.Telemetry.Add(new() { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, MeterId = m.Id, Timestamp = at, SourceTimestamp = at, Rssi = m.Rssi, Pdr = m.Pdr, LatencyMs = m.LatencyMs, HopCount = m.HopCount, RoutingCost = m.RoutingCost, Retries = m.Retries, ReadSuccess = m.ReadSuccess, Quality = connected ? "simulated" : "missing" });
            foreach (var (p, minutes) in new[] { ("instantaneous", m.InstantaneousMinutes), ("block", m.BlockMinutes), ("daily", 1440) }) if (minutes > 0 && (at - sim.Start).TotalMinutes % minutes == 0)
            {
                var reading = ElectricalModel.Reading(m, (int)m.NodeAddress - 1001, p, at, minutes, sim.Start); if (!connected) { reading.Quality = "missing"; reading.Voltage = null; reading.CurrentA = null; reading.CurrentB = null; reading.CurrentC = null; reading.ActivePowerKw = null; reading.ApparentPowerKva = null; reading.PowerFactor = null; reading.FrequencyHz = null; reading.CumulativeKwh = null; reading.IntervalKwh = null; reading.MaximumDemandKw = null; }
                db.Readings.Add(reading);
                db.Add(new CommunicationAttempt { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, MeterId = m.Id, Profile = p, Success = connected, Error = connected ? null : "simulated-timeout", Timestamp = at });
            }
        }
        sim.Clock = at; await db.SaveChangesAsync(ct); if(tx is not null)await tx.CommitAsync(ct);
    }
    async Task Audit(string action, CancellationToken ct) { db.Add(new AuditLog { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, Actor = tenant.UserId, Action = "simulation." + action, Resource = Demo.Id(3).ToString(), Timestamp = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct); }
    public async Task<object> Scenario(string serial, string cause, CancellationToken ct = default)
    {
        Guard(); var m = await db.Meters.SingleOrDefaultAsync(m => m.Serial == serial, ct) ?? throw new KeyNotFoundException(); var clock = await db.Simulations.Select(s => s.Clock).SingleAsync(ct);
        var causeMarker=$"\"cause\":\"{cause}\"";
        if(await db.Set<DiagnosticRecord>().AnyAsync(d=>d.MeterId==m.Id&&d.Rule=="simulation-ground-truth"&&d.Timestamp==clock&&d.Evidence.Contains(causeMarker),ct))return new{Meter=serial,Cause=cause,AlreadyApplied=true};
        switch (cause)
        {
            case "weak-signal": m.Rssi = -99; break;
            case "coverage-gap": m.Neighbors = 1; break;
            case "antenna": m.Rssi = -108; m.Neighbors = 1; break;
            case "congestion": m.Pdr = .65; m.Retries = 12; break;
            case "gateway":
                var sink = await db.Sinks.SingleOrDefaultAsync(s => s.Id == m.SinkId, ct) ?? throw new ArgumentException("Select a connected meter for gateway failure.");
                var gateway = await db.Gateways.SingleAsync(g => g.Id == sink.GatewayId, ct); gateway.Online = false;
                foreach (var affected in await db.Meters.Where(x => x.SinkId == sink.Id).ToListAsync(ct)) affected.LastCommunication = clock.AddHours(-2);
                break;
            case "firmware":
            case "power":
                db.Add(new MeterEvent { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, MeterId = m.Id, Profile = cause == "power" ? "power-events" : "other-events", Code = cause == "power" ? "power-loss" : "firmware-error", Description = "Controlled simulated " + cause + " scenario", Timestamp = clock }); if (cause == "power") m.LastCommunication = clock.AddHours(-2); break;
            case "dlms": m.ReadSuccess = .4; db.Add(new CommunicationAttempt { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, MeterId = m.Id, Profile = "instantaneous", Success = false, Error = "simulated-dlms-timeout", Timestamp = clock }); break;
            case "routing-instability":
                var neighbors = await db.Neighbors.Where(n => n.MeterId == m.Id).Select(n => n.NeighborId).ToListAsync(ct);
                var parent = await db.Meters.FirstOrDefaultAsync(n => neighbors.Contains(n.Id) && n.HopCount < m.HopCount && n.SinkId == m.SinkId && n.Id != m.NextHopId, ct) ?? throw new ArgumentException("This meter has no valid alternate upstream route; choose a different connected meter.");
                for (int i = 0; i < 8; i++) db.Routes.Add(new() { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, MeterId = m.Id, NextHopId = i % 2 == 0 ? parent.Id : m.NextHopId, SinkId = m.SinkId, HopCount = i % 2 == 0 ? parent.HopCount + 1 : m.HopCount, Timestamp = clock.AddMinutes(-40 + i * 5) }); break;
            default: throw new ArgumentException("Unsupported scenario.");
        }
        m.ObservedAt = clock;
        db.Add(new DiagnosticRecord { Id = Guid.NewGuid(), UtilityId = tenant.UtilityId, MeterId = m.Id, Rule = "simulation-ground-truth", Evidence = System.Text.Json.JsonSerializer.Serialize(new { cause, serial, clock }), Timestamp = clock });
        await db.SaveChangesAsync(ct); await Audit("scenario." + cause, ct); return new { Meter = serial, Cause = cause, SimulationClock = clock, GroundTruth = "Recorded separately from diagnostic evidence; reset demo to restore baseline." };
    }
}
public sealed class SimulatedDlmsAdapter : IDlmsAdapter
{
    public string Mode => "simulation; no DLMS wire protocol";
    public Task<ProfileReading> Read(Meter m, string profile, DateTimeOffset at, CancellationToken ct = default) { if (!Demo.Profiles.Take(4).Contains(profile)) throw new ArgumentException("Use event collection for event modules."); return Task.FromResult(ElectricalModel.Reading(m, (int)m.NodeAddress - 1001, profile, at, profile == "block" ? m.BlockMinutes : m.InstantaneousMinutes, at.Date.AddDays(-30))); }
}
