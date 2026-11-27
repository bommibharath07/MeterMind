using MeterMind.Domain;
namespace MeterMind.Simulator;

public record MeshDataset(IReadOnlyList<Meter> Meters, IReadOnlyList<Gateway> Gateways, IReadOnlyList<Sink> Sinks, IReadOnlyList<Neighbor> Neighbors);
public sealed class SimulationOptions
{
    public double Latitude { get; set; } = 12.9716; public double Longitude { get; set; } = 77.5946;
    public double RadiusM { get; set; } = 1000; public int Seed { get; set; } = 20261127;
    public int Days { get; set; } = 30; public DateTimeOffset End { get; set; } = new(2026, 11, 27, 0, 0, 0, TimeSpan.Zero);
}
public static class MeshGenerator
{
    public static MeshDataset Generate(SimulationOptions o)
    {
        if (o.RadiusM <= 0 || o.RadiusM > 10000 || Math.Abs(o.Latitude) > 85 || Math.Abs(o.Longitude) > 180 || o.Days is < 1 or > 366) throw new ArgumentOutOfRangeException(nameof(o));
        var meters = new List<Meter>(); var gateways = new List<Gateway>(); var sinks = new List<Sink>(); var neighbors = new List<Neighbor>();
        for (int s = 0; s < 3; s++)
        {
            var a = s * 2 * Math.PI / 3; var x = .45 * o.RadiusM * Math.Cos(a); var y = .45 * o.RadiusM * Math.Sin(a);
            gateways.Add(new() { Id = Demo.Id(10 + s), UtilityId = Demo.UtilityId, Name = $"GW-0{s + 1}", Latitude = o.Latitude + y / 111320, Longitude = o.Longitude + x / (111320 * Math.Cos(o.Latitude * Math.PI / 180)) });
            sinks.Add(new() { Id = Demo.Id(20 + s), UtilityId = Demo.UtilityId, GatewayId = Demo.Id(10 + s), NetworkId = Demo.Id(2), Name = $"Sink {s + 1}", X = x, Y = y });
        }
        for (int i = 0; i < 300; i++)
        {
            var radius = o.RadiusM * .995 * Math.Sqrt((i + .5) / 300); var a = i * Math.PI * (3 - Math.Sqrt(5)) + (o.Seed % 360) * Math.PI / 180;
            var x = radius * Math.Cos(a); var y = radius * Math.Sin(a); var sector = (int)((Math.Atan2(y, x) + Math.PI) / (2 * Math.PI / 3));
            meters.Add(new() { Id = Demo.Id(1000 + i), UtilityId = Demo.UtilityId, Serial = $"MM-{i + 1:0000}", NodeAddress = (uint)(1001 + i), NetworkId = Demo.Id(2), SectorId = Demo.Id(30 + Math.Min(2, sector)), X = x, Y = y, Latitude = o.Latitude + y / 111320, Longitude = o.Longitude + x / (111320 * Math.Cos(o.Latitude * Math.PI / 180)) });
        }
        // Range is local; obstacle crossing loses 22 dB. This is an illustrative RF model, not a Wirepas radio model.
        double range = o.RadiusM * .235;
        foreach (var m in meters) foreach (var n in meters.Where(n => n.Id != m.Id))
        {
            var d = Distance(m.X, m.Y, n.X, n.Y); var obstacle = (m.X < 100 && n.X > 100 || n.X < 100 && m.X > 100) && m.Y > 100 && n.Y > 100;
            var rssi = -42 - 22 * Math.Log10(Math.Max(d, 1)) - (obstacle ? 22 : 0);
            if (d <= range && rssi > -113) neighbors.Add(new() { Id = Guid.NewGuid(), UtilityId = Demo.UtilityId, MeterId = m.Id, NeighborId = n.Id, DistanceM = d, Rssi = rssi });
        }
        var queue = new Queue<Meter>();
        foreach (var m in meters)
        {
            var sink = sinks.MinBy(s => Distance(m.X, m.Y, s.X, s.Y))!;
            if (Distance(m.X, m.Y, sink.X, sink.Y) <= range) { m.NextHopId = sink.Id; m.SinkId = sink.Id; m.HopCount = 1; queue.Enqueue(m); }
        }
        var byId = meters.ToDictionary(m => m.Id); var adjacent = neighbors.ToLookup(n => n.NeighborId);
        while (queue.TryDequeue(out var parent)) foreach (var edge in adjacent[parent.Id])
        {
            var m = byId[edge.MeterId]; if (m.HopCount != null) continue;
            m.NextHopId = parent.Id; m.SinkId = parent.SinkId; m.HopCount = parent.HopCount + 1; queue.Enqueue(m);
        }
        for (int i = 0; i < meters.Count; i++)
        {
            var m = meters[i]; m.Neighbors = neighbors.Count(n => n.MeterId == m.Id); m.RoutingCost = m.HopCount * 10;
            if (i >= 294) { m.NextHopId = null; m.SinkId = null; m.HopCount = null; m.RoutingCost = null; }
            m.Rssi = i % 17 == 0 ? -99 : -67 - i % 22; m.Pdr = i % 19 == 0 ? .72 : .97 + i % 3 * .01; m.ReadSuccess = i % 23 == 0 ? .6 : .99; m.LatencyMs = 20 + (m.HopCount ?? 0) * 17; m.Retries = i % 19 == 0 ? 9 : 1;
            m.ObservedAt = o.End.AddMinutes(-5); m.LastCommunication = i >= 294 ? o.End.AddHours(-3) : m.ObservedAt;
        }
        // Removing leaf-outage nodes must not leave connected paths pointing through them.
        bool changed;
        do { changed = false; foreach (var m in meters.Where(m => m.NextHopId != null)) if (byId.TryGetValue(m.NextHopId!.Value, out var p) && p.NextHopId == null) { m.NextHopId = null; m.SinkId = null; m.HopCount = null; m.LastCommunication = o.End.AddHours(-3); changed = true; } } while (changed);
        return new(meters, gateways, sinks, neighbors);
    }
    public static double Distance(double x, double y, double xx, double yy) => Math.Sqrt((x - xx) * (x - xx) + (y - yy) * (y - yy));
}
public static class ElectricalModel
{
    // Integral of a positive periodic load; interval and cumulative energy share this exact primitive.
    public static double Cumulative(int node, DateTimeOffset t, DateTimeOffset epoch)
    {
        var h = (t - epoch).TotalHours; var baseline = .45 + (node % 13) * .065; return 1000 + node * 3 + baseline * h + .15 * 24 / (2 * Math.PI) * (1 - Math.Cos(2 * Math.PI * h / 24));
    }
    public static ProfileReading Reading(Meter m, int node, string profile, DateTimeOffset at, int minutes, DateTimeOffset epoch)
    {
        var h = (at - epoch).TotalHours; var power = .45 + node % 13 * .065 + .15 * Math.Sin(2 * Math.PI * h / 24); var voltage = 230 + 3 * Math.Sin(h); var pf = .96;
        return new() { Id = Guid.NewGuid(), UtilityId = m.UtilityId, MeterId = m.Id, Profile = profile, Timestamp = at, SourceTimestamp = at, Voltage = voltage, CurrentA = power * 1000 / (3 * voltage * pf), CurrentB = power * 1000 / (3 * voltage * pf), CurrentC = power * 1000 / (3 * voltage * pf), ActivePowerKw = power, ApparentPowerKva = power / pf, PowerFactor = pf, FrequencyHz = 50 + .02 * Math.Sin(h), CumulativeKwh = Cumulative(node, at, epoch), IntervalKwh = Cumulative(node, at, epoch) - Cumulative(node, at.AddMinutes(-minutes), epoch), MaximumDemandKw = .60 + node % 13 * .065 };
    }
}
