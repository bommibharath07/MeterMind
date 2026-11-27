using System.Buffers;
using System.Text.Json;
using MeterMind.Application;
using MeterMind.Domain;
using MeterMind.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
namespace MeterMind.Infrastructure;
// MeterMind canonical envelope, deliberately not a claimed Wirepas Gateway protobuf decoder.
public record TelemetryEnvelope(string Serial, DateTimeOffset Timestamp, double? Rssi, double? Snr, double? Pdr, double? LatencyMs, int Retries, double? ReadSuccess);
public sealed class CanonicalIngestion(MeterDb db, ITenantContext tenant) : ICommunicationAdapter
{
    public string Transport => "metermind-canonical-mqtt-v1";
    public async Task Ingest(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        if (payload.Length > 16384) throw new ArgumentException("Telemetry envelope exceeds 16 KiB.");
        var e = JsonSerializer.Deserialize<TelemetryEnvelope>(payload.Span, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new ArgumentException("Invalid envelope.");
        if (e.Timestamp.Offset != TimeSpan.Zero || e.Timestamp > DateTimeOffset.UtcNow.AddMinutes(5) || e.Rssi is < -200 or > 0 || e.Pdr is < 0 or > 1 || e.ReadSuccess is < 0 or > 1 || e.Retries < 0 || e.LatencyMs < 0) throw new ArgumentException("Telemetry outside accepted bounds.");
        var m = await db.Meters.SingleOrDefaultAsync(m => m.Serial == e.Serial, ct) ?? throw new KeyNotFoundException("Meter is not provisioned in the ingestion utility.");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Telemetry\" (\"Id\",\"UtilityId\",\"MeterId\",\"Timestamp\",\"SourceTimestamp\",\"Rssi\",\"Snr\",\"Pdr\",\"LatencyMs\",\"Retries\",\"HopCount\",\"RoutingCost\",\"ReadSuccess\",\"Quality\") VALUES ({Guid.NewGuid()},{tenant.UtilityId},{m.Id},{e.Timestamp},{e.Timestamp},{e.Rssi},{e.Snr},{e.Pdr},{e.LatencyMs},{e.Retries},{m.HopCount},{m.RoutingCost},{e.ReadSuccess},{"observed"}) ON CONFLICT (\"UtilityId\",\"MeterId\",\"Timestamp\") DO NOTHING", ct);
        if (m.ObservedAt is null || m.ObservedAt <= e.Timestamp) { m.Rssi = e.Rssi; m.Snr = e.Snr; m.Pdr = e.Pdr; m.LatencyMs = e.LatencyMs; m.Retries = e.Retries; m.ReadSuccess = e.ReadSuccess; m.LastCommunication = e.Timestamp; m.ObservedAt = e.Timestamp; m.Transport = Transport; await db.SaveChangesAsync(ct); }
    }
}
public sealed class MqttIngestionWorker(IConfiguration config, IServiceScopeFactory scopes, ILogger<MqttIngestionWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!config.GetValue<bool>("Mqtt:Enabled")) return;
        if (config.GetValue<bool>("Simulation:Enabled")) throw new InvalidOperationException("Physical ingestion must use a separate, non-simulation worker.");
        using var client = new MqttClientFactory().CreateMqttClient();
        var host = config["Mqtt:Host"] ?? throw new InvalidOperationException("Mqtt__Host required"); var topic = config["Mqtt:Topic"] ?? throw new InvalidOperationException("Mqtt__Topic required");
        var options = new MqttClientOptionsBuilder().WithTcpServer(host, config.GetValue("Mqtt:Port", 8883)).WithTlsOptions(o => o.UseTls()).WithCredentials(config["Mqtt:Username"], config["Mqtt:Password"]).WithClientId("metermind-" + Guid.NewGuid().ToString("N")).Build();
        client.ApplicationMessageReceivedAsync += async e => { try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<CanonicalIngestion>().Ingest(e.ApplicationMessage.Payload.ToArray(), ct); } catch (Exception ex) { log.LogWarning(ex, "Canonical MQTT ingestion rejected"); } };
        while (!ct.IsCancellationRequested) { try { if (!client.IsConnected) { await client.ConnectAsync(options, ct); await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)).Build(), ct); } } catch (Exception e) when (e is not OperationCanceledException) { log.LogWarning(e, "MQTT connection unavailable"); } await Task.Delay(TimeSpan.FromSeconds(10), ct); }
    }
}
