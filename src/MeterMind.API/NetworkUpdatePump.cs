using MeterMind.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
namespace MeterMind.API;
// Only utility IDs and freshness timestamps cross this system background boundary.
public sealed class NetworkUpdatePump(IConfiguration config, IHubContext<NetworkHub> hub, ILogger<NetworkUpdatePump> logger) : BackgroundService
{
    readonly Dictionary<Guid, DateTimeOffset> observed = [];
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var db = new MeterDb(new DbContextOptionsBuilder<MeterDb>().UseNpgsql(config.GetConnectionString("MeterDb")).Options, new FixedTenant(Guid.Empty));
                var clocks = await db.Simulations.IgnoreQueryFilters().OrderBy(s=>s.UtilityId).Select(s => new { s.UtilityId, Time = s.Clock }).Take(10000).ToListAsync(ct);
                var physical = await db.Meters.IgnoreQueryFilters().Where(m => !m.Transport.StartsWith("simulated") && m.ObservedAt != null).GroupBy(m => m.UtilityId).Select(g => new { UtilityId = g.Key, Time = g.Max(m => m.ObservedAt)!.Value }).OrderBy(g=>g.UtilityId).Take(10000).ToListAsync(ct);
                foreach (var row in clocks.Concat(physical).GroupBy(x => x.UtilityId).Select(g => g.MaxBy(x => x.Time)!)) if (!observed.TryGetValue(row.UtilityId, out var previous) || previous != row.Time) { observed[row.UtilityId] = row.Time; await hub.Clients.Group(row.UtilityId.ToString()).SendAsync("networkUpdated", new { dataTime = row.Time }, ct); }
            }
            catch (Exception e) when (e is not OperationCanceledException) { logger.LogWarning("Network freshness polling unavailable: {ErrorType}", e.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
