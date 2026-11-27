using MeterMind.Domain;
namespace MeterMind.Application;

public interface ITenantContext { Guid UtilityId { get; } string UserId { get; } bool CanOperate { get; } }
public record MeterFilter(string? Search = null, double? RssiBelow = null, string? Status = null, Guid? SectorId = null, Guid? GatewayId = null, int Limit = 300, int Offset = 0, int? MinHops = null, double? MinPdr = null, double? MaxLatencyMs = null, string? Firmware = null, Guid? SinkId = null);
public record Page<T>(IReadOnlyList<T> Items, long Total, DateTimeOffset QueryTime, DateTimeOffset? DataTime);
public record MeterView(Meter Meter, string Status, double? PacketLoss);
public record Finding(string Rule, string Conclusion, string EvidenceQuality, IReadOnlyList<string> Evidence, IReadOnlyList<string> Alternatives, IReadOnlyList<string> Actions);
public record Diagnosis(Guid MeterId, string Serial, IReadOnlyList<Finding> Findings, int RiskScore, string RiskLabel, DateTimeOffset QueryTime);
public interface INetworkService
{
    Task<object> Summary(CancellationToken ct = default);
    Task<Page<MeterView>> Search(MeterFilter filter, CancellationToken ct = default);
    Task<MeterView> Details(string id, CancellationToken ct = default);
    Task<object> Topology(DateTimeOffset? at = null, CancellationToken ct = default);
    Task<object> Neighbors(string id, CancellationToken ct = default);
    Task<object> Routes(string id, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<object> Profiles(string id, string profile, DateTimeOffset from, DateTimeOffset to, int limit = 500, CancellationToken ct = default);
    Task<object> Events(string id, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<object> Gateways(CancellationToken ct = default);
    Task<object> Affected(Guid id, CancellationToken ct = default);
    Task<Diagnosis> Diagnose(string id, CancellationToken ct = default);
}
public record ReportRequest(string Format, string Kind, MeterFilter Filter, string? MeterId = null, string? Profile = null, DateTimeOffset? From = null, DateTimeOffset? To = null);
public interface IReportService { Task<Guid> Queue(ReportRequest request, CancellationToken ct = default); Task<object> List(CancellationToken ct = default); Task<(byte[] Content, string Mime, string Name)> Download(Guid id, CancellationToken ct = default); }
public interface IToolDispatcher { IReadOnlyList<string> Names { get; } Task<object> Execute(string name, System.Text.Json.JsonElement args, CancellationToken ct = default); }
public sealed class ToolCallContext { public Guid? ConversationId { get; set; } }
public interface IHesDlmsAdapter : IDlmsAdapter { string Manufacturer { get; } }
public interface ICellularAdapter : ICommunicationAdapter { string Provider { get; } }
public interface IDlmsAdapter { string Mode { get; } Task<ProfileReading> Read(Meter meter, string profile, DateTimeOffset at, CancellationToken ct = default); }
public interface ICommunicationAdapter { string Transport { get; } Task Ingest(ReadOnlyMemory<byte> payload, CancellationToken ct = default); }
public class HealthThresholds { public double WeakRssi { get; set; } = -90; public double MinPdr { get; set; } = .9; public int StaleMinutes { get; set; } = 15; public int OfflineMinutes { get; set; } = 60; }
public static class HealthClassifier
{
    public static string Classify(Meter m, DateTimeOffset clock, HealthThresholds t)
    {
        if (m.LastCommunication is null || m.ObservedAt is null) return "unknown";
        if (clock - m.LastCommunication > TimeSpan.FromMinutes(t.OfflineMinutes)) return "offline";
        if (clock - m.ObservedAt > TimeSpan.FromMinutes(t.StaleMinutes) || m.Rssi is null || m.Pdr is null) return "unknown";
        return m.Rssi < t.WeakRssi || m.Pdr < t.MinPdr || m.ReadSuccess < .9 ? "degraded" : "online";
    }
}
