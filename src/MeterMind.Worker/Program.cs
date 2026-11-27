using MeterMind.Domain;
using MeterMind.Infrastructure;
using MeterMind.Persistence;
using MeterMind.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
var builder = Host.CreateApplicationBuilder(args);
var utility = Guid.TryParse(builder.Configuration["WorkerUtility"], out var u) ? u : Demo.UtilityId;
builder.Services.AddMeterMind(builder.Configuration, builder.Environment.IsDevelopment(), utility);
builder.Services.AddHostedService<ContinuousWorker>(); builder.Services.AddHostedService<MqttIngestionWorker>(); await builder.Build().RunAsync();
public sealed class ContinuousWorker(IServiceScopeFactory scopes, ILogger<ContinuousWorker> log, IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var nextTick = DateTimeOffset.UtcNow;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var s = scopes.CreateScope(); await s.ServiceProvider.GetRequiredService<ReportProcessor>().Process(ct);
                if (config.GetValue<bool>("Simulation:Enabled") && DateTimeOffset.UtcNow >= nextTick) { await s.ServiceProvider.GetRequiredService<SimulationService>().Tick(ct); nextTick = DateTimeOffset.UtcNow.AddMinutes(5); }
                var db = s.ServiceProvider.GetRequiredService<MeterDb>(); await db.Set<GeneratedReport>().Where(r => r.ExpiresAt < DateTimeOffset.UtcNow && r.Content != null).ExecuteUpdateAsync(x => x.SetProperty(r => r.Content, (byte[]?)null).SetProperty(r => r.Status, "expired"), ct);
                var retention = config.GetValue("RetentionDays", 90); if (retention < 1) throw new InvalidOperationException("RetentionDays must be positive");
                var clock = await db.Simulations.Select(x => (DateTimeOffset?)x.Clock).SingleOrDefaultAsync(ct) ?? DateTimeOffset.UtcNow; var cutoff = clock.AddDays(-retention);
                await db.Telemetry.Where(x => x.Timestamp < cutoff).ExecuteDeleteAsync(ct); await db.Readings.Where(x => x.Timestamp < cutoff).ExecuteDeleteAsync(ct);
            }
            catch (Exception e) when (e is not OperationCanceledException) { log.LogError(e, "Worker iteration failed"); }
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }
}
