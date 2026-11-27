using MeterMind.Domain;
using MeterMind.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace MeterMind.Simulator;

public sealed class HistoricalSeeder(MeterDb db)
{
    public async Task Seed(SimulationOptions o, CancellationToken ct = default)
    {
        if (db.UtilityId != Demo.UtilityId) throw new InvalidOperationException("Seeder is restricted to the isolated demo utility.");
        var mesh = MeshGenerator.Generate(o); var start = o.End.AddDays(-o.Days);
        await db.Database.OpenConnectionAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(714300)", ct);
        var existing = await db.Simulations.AsNoTracking().SingleOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (existing.Start != start || existing.Clock < o.End || existing.Seed != o.Seed || existing.RadiusM != o.RadiusM || existing.Latitude != o.Latitude || existing.Longitude != o.Longitude) throw new InvalidOperationException("Existing seed configuration differs. Reset the isolated demo explicitly.");
            if (existing.SeedingComplete) { await tx.CommitAsync(ct); return; }
        }
        if (!await db.Utilities.AnyAsync(ct)) db.Utilities.Add(new() { Id = Demo.UtilityId, Name = "Bengaluru Demonstration Utility" });
        if (!await db.Meters.AnyAsync(ct))
        {
            db.Add(new MeshNetwork { Id = Demo.Id(2), UtilityId = Demo.UtilityId, Name = "Central RF mesh", Address = 100 });
            for (int i = 0; i < 3; i++) db.Add(new Sector { Id = Demo.Id(30 + i), UtilityId = Demo.UtilityId, Name = $"Sector {i + 1}" });
            db.Gateways.AddRange(mesh.Gateways); db.Sinks.AddRange(mesh.Sinks); db.Meters.AddRange(mesh.Meters); db.Neighbors.AddRange(mesh.Neighbors);
            for (int p = 0; p < Demo.Profiles.Length; p++) db.Add(new ProfileDefinition { Id = Demo.Id(50 + p), UtilityId = Demo.UtilityId, Key = Demo.Profiles[p], Name = Demo.Profiles[p].Replace('-', ' '), IntervalMinutes = p switch { 0 => 15, 1 => 30, 2 => 1440, _ => null }, CaptureObjects = "[\"1-0:32.7.0*255\",\"1-0:1.8.0*255\"]" });
            var obis = new[] { ("1-0:32.7.0*255", "Phase voltage", "V"), ("1-0:31.7.0*255", "Phase current", "A"), ("1-0:1.7.0*255", "Active power", "kW"), ("1-0:1.8.0*255", "Import energy", "kWh"), ("1-0:13.7.0*255", "Power factor", "1"), ("1-0:14.7.0*255", "Frequency", "Hz") };
            for (int j = 0; j < obis.Length; j++) db.Add(new ObisObject { Id = Demo.Id(70 + j), UtilityId = Demo.UtilityId, Code = obis[j].Item1, Name = obis[j].Item2, Unit = obis[j].Item3 });
            db.Simulations.Add(new() { Id = Demo.Id(3), UtilityId = Demo.UtilityId, Clock = o.End, Start = start, Latitude = o.Latitude, Longitude = o.Longitude, RadiusM = o.RadiusM, Seed = o.Seed });
            foreach (var m in mesh.Meters)
            {
                db.Routes.Add(new() { Id = Guid.NewGuid(), UtilityId = m.UtilityId, MeterId = m.Id, NextHopId = m.NextHopId, SinkId = m.SinkId, HopCount = m.HopCount, Timestamp = start });
                var alt = mesh.Neighbors.Where(n => n.MeterId == m.Id).Select(n => mesh.Meters.First(x => x.Id == n.NeighborId)).FirstOrDefault(n => n.HopCount < m.HopCount && n.SinkId == m.SinkId);
                if (alt is not null && alt.Id != m.NextHopId)
                {
                    db.Routes.Add(new() { Id = Guid.NewGuid(), UtilityId = m.UtilityId, MeterId = m.Id, NextHopId = alt.Id, SinkId = m.SinkId, HopCount = alt.HopCount + 1, Timestamp = start.AddDays(10) });
                    db.Routes.Add(new() { Id = Guid.NewGuid(), UtilityId = m.UtilityId, MeterId = m.Id, NextHopId = m.NextHopId, SinkId = m.SinkId, HopCount = m.HopCount, Timestamp = start.AddDays(20) });
                }
                for (int p = 4; p < 10; p++) db.Add(new MeterEvent { Id = Guid.NewGuid(), UtilityId = m.UtilityId, MeterId = m.Id, Profile = Demo.Profiles[p], Code = p == 6 && m.NextHopId == null ? "power-loss" : "simulated-observation", Description = p == 6 && m.NextHopId == null ? "Simulated supply interruption" : "Simulated profile observation; no physical event asserted", Timestamp = o.End.AddHours(-2) });
                db.Add(new CommunicationAttempt { Id = Guid.NewGuid(), UtilityId = m.UtilityId, MeterId = m.Id, Profile = "instantaneous", Success = m.ReadSuccess >= .9, Error = m.ReadSuccess < .9 ? "simulated-timeout" : null, Timestamp = o.End.AddMinutes(-15) });
                if (m.NextHopId == null) db.Add(new Incident { Id = Guid.NewGuid(), UtilityId = m.UtilityId, MeterId = m.Id, Title = $"Communication lost: {m.Serial}", Severity = "critical", Timestamp = o.End.AddHours(-3) });
            }
            await db.SaveChangesAsync(ct);
        }
        // Set-based bulk insertion: one transaction, bounded server generation and natural-key conflict handling.
        const string telemetrySql = """
        INSERT INTO "Telemetry" ("Id","UtilityId","MeterId","Timestamp","SourceTimestamp","Rssi","Snr","Pdr","LatencyMs","Retries","HopCount","RoutingCost","ReadSuccess","Quality")
        SELECT gen_random_uuid(),m."UtilityId",m."Id",t,t,m."Rssi"+2*sin(extract(epoch from t)/3600),NULL,m."Pdr",m."LatencyMs",m."Retries",m."HopCount",m."RoutingCost",m."ReadSuccess",CASE WHEN m."NextHopId" IS NULL AND t>@end-interval '3 hours' THEN 'missing' ELSE 'simulated' END
        FROM "Meters" m CROSS JOIN generate_series(@start+interval '5 minutes',@end,interval '5 minutes') t WHERE m."UtilityId"=@utility
        ON CONFLICT ("UtilityId","MeterId","Timestamp") DO NOTHING;
        """;
        await Execute(telemetrySql, start, o.End, ct);
        foreach (var (profile, minutes) in new[] { ("instantaneous", 15), ("block", 30), ("daily", 1440), ("billing", o.Days * 1440) })
        {
            var sql = """
            INSERT INTO "Readings" ("Id","UtilityId","MeterId","Profile","Timestamp","SourceTimestamp","Voltage","CurrentA","CurrentB","CurrentC","ActivePowerKw","ApparentPowerKva","PowerFactor","FrequencyHz","CumulativeKwh","IntervalKwh","MaximumDemandKw","Quality")
            SELECT gen_random_uuid(),m."UtilityId",m."Id",@profile,t,t,230+3*sin(h),p*1000/(3*(230+3*sin(h))*.96),p*1000/(3*(230+3*sin(h))*.96),p*1000/(3*(230+3*sin(h))*.96),p,p/.96,.96,50+.02*sin(h),1000+node*3+b*h+.15*24/(2*pi())*(1-cos(2*pi()*h/24)),b*@minutes/60.0+.15*24/(2*pi())*(cos(2*pi()*(h-@minutes/60.0)/24)-cos(2*pi()*h/24)),b+.15,'simulated'
            FROM "Meters" m CROSS JOIN generate_series(@start+make_interval(mins=>@minutes),@end,make_interval(mins=>@minutes)) t
            CROSS JOIN LATERAL (SELECT (m."NodeAddress"-1001)::int AS node,extract(epoch from t-@start)/3600.0 AS h) c
            CROSS JOIN LATERAL (SELECT .45+(node%13)*.065 AS b) d
            CROSS JOIN LATERAL (SELECT b+.15*sin(2*pi()*h/24) AS p) e
            WHERE m."UtilityId"=@utility ON CONFLICT ("UtilityId","MeterId","Profile","Timestamp") DO NOTHING;
            """;
            await Execute(sql, start, o.End, ct, profile, minutes);
        }
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Telemetry\" SET \"Rssi\"=NULL,\"Snr\"=NULL,\"Pdr\"=NULL,\"LatencyMs\"=NULL,\"ReadSuccess\"=NULL WHERE \"UtilityId\"={Demo.UtilityId} AND \"Quality\"='missing'", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Readings\" r SET \"Quality\"='missing',\"Voltage\"=NULL,\"CurrentA\"=NULL,\"CurrentB\"=NULL,\"CurrentC\"=NULL,\"ActivePowerKw\"=NULL,\"ApparentPowerKva\"=NULL,\"PowerFactor\"=NULL,\"FrequencyHz\"=NULL,\"CumulativeKwh\"=NULL,\"IntervalKwh\"=NULL,\"MaximumDemandKw\"=NULL FROM \"Meters\" m WHERE r.\"UtilityId\"={Demo.UtilityId} AND m.\"UtilityId\"=r.\"UtilityId\" AND m.\"Id\"=r.\"MeterId\" AND m.\"NextHopId\" IS NULL AND r.\"Timestamp\">{o.End.AddHours(-3)}", ct);
        await db.Simulations.ExecuteUpdateAsync(s => s.SetProperty(x => x.SeedingComplete, true), ct);
        await tx.CommitAsync(ct);
    }
    async Task Execute(string sql, DateTimeOffset start, DateTimeOffset end, CancellationToken ct, string? profile = null, int minutes = 0)
    {
        var pars = new List<object> { new NpgsqlParameter("start", start), new NpgsqlParameter("end", end), new NpgsqlParameter("utility", Demo.UtilityId) };
        if (profile is not null) { pars.Add(new NpgsqlParameter("profile", profile)); pars.Add(new NpgsqlParameter("minutes", minutes)); }
        db.Database.SetCommandTimeout(900); await db.Database.ExecuteSqlRawAsync(sql, pars, ct);
    }
}
