using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Simulator;
namespace MeterMind.Unit.Tests;

public class SimulationTests
{
    [Fact] public void DefaultMeshHas300UniqueMetersWithinRadiusAndBoundedLinks() { var o = new SimulationOptions(); var d = MeshGenerator.Generate(o); Assert.Equal(300, d.Meters.Count); Assert.Equal(300, d.Meters.Select(x => x.Serial).Distinct().Count()); Assert.Equal(3, d.Sinks.Count); Assert.Equal(3, d.Gateways.Count); Assert.All(d.Meters, m => Assert.InRange(MeshGenerator.Distance(0, 0, m.X, m.Y), 0, 1000)); Assert.All(d.Neighbors, n => Assert.InRange(n.DistanceM, 0, 235.0001)); }
    [Fact] public void EveryConnectedRouteTerminatesAtASinkWithoutLoops() { var d = MeshGenerator.Generate(new()); var meters = d.Meters.ToDictionary(m => m.Id); var sinks = d.Sinks.Select(s => s.Id).ToHashSet(); Assert.True(d.Meters.Count(m => m.HopCount > 1) > 100); foreach (var m in d.Meters.Where(m => m.NextHopId != null)) { var seen = new HashSet<Guid>(); var current = m; while (current.NextHopId is Guid next && !sinks.Contains(next)) { Assert.True(seen.Add(current.Id)); current = meters[next]; Assert.NotNull(current.NextHopId); } Assert.Contains(current.NextHopId!.Value, sinks); Assert.Equal(m.SinkId, current.NextHopId); } }
    [Fact] public void SimulationIsDeterministicForCoordinatesAndRoutes() { var o = new SimulationOptions(); var a = MeshGenerator.Generate(o); var b = MeshGenerator.Generate(o); Assert.Equal(a.Meters.Select(m => (m.Id, m.X, m.Y, m.NextHopId)), b.Meters.Select(m => (m.Id, m.X, m.Y, m.NextHopId))); }
    [Fact] public void IntegratedEnergyMatchesBlockAndDailyIntervals() { var o = new SimulationOptions(); var m = MeshGenerator.Generate(o).Meters[11]; var start = o.End.AddDays(-30); var total = 0d; var previous = ElectricalModel.Cumulative(11, start, start); for (int i = 1; i <= 48; i++) { var r = ElectricalModel.Reading(m, 11, "block", start.AddMinutes(30 * i), 30, start); Assert.Equal(r.CumulativeKwh!.Value - previous, r.IntervalKwh!.Value, 9); Assert.True(r.IntervalKwh > 0); total += r.IntervalKwh.Value; previous = r.CumulativeKwh.Value; } var daily = ElectricalModel.Reading(m, 11, "daily", start.AddDays(1), 1440, start); Assert.Equal(daily.IntervalKwh!.Value, total, 8); }
    [Fact] public void MissingAndStaleAreDifferentFromZero() { var clock = DateTimeOffset.UtcNow; var m = new Meter { Rssi = 0, Pdr = 0, ObservedAt = clock, LastCommunication = clock }; Assert.Equal("degraded", HealthClassifier.Classify(m, clock, new())); m.Rssi = null; Assert.Equal("unknown", HealthClassifier.Classify(m, clock, new())); m.LastCommunication = clock.AddHours(-2); Assert.Equal("offline", HealthClassifier.Classify(m, clock, new())); }
    [Theory]
    [InlineData("=HYPERLINK(\"bad\")")]
    [InlineData("  +CMD")]
    [InlineData("\t=1+1")]
    [InlineData("@SUM(A1)")]
    public void ExportCellsNeutralizeFormulaInjection(string input) => Assert.StartsWith("'", MeterMind.Reporting.ReportService.SafeCell(input));
}
