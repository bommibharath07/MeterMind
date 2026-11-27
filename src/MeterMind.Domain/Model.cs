namespace MeterMind.Domain;

public abstract class TenantEntity { public Guid Id { get; set; } public Guid UtilityId { get; set; } }
public class Utility { public Guid Id { get; set; } public string Name { get; set; } = ""; }
public class Sector : TenantEntity { public string Name { get; set; } = ""; }
public class MeshNetwork : TenantEntity { public string Name { get; set; } = ""; public uint Address { get; set; } }
public class Gateway : TenantEntity { public string Name { get; set; } = ""; public bool Online { get; set; } = true; public double Latitude { get; set; } public double Longitude { get; set; } }
public class Sink : TenantEntity { public Guid GatewayId { get; set; } public Guid NetworkId { get; set; } public string Name { get; set; } = ""; public double X { get; set; } public double Y { get; set; } }
public class Meter : TenantEntity
{
    public string Serial { get; set; } = ""; public uint NodeAddress { get; set; }
    public Guid NetworkId { get; set; }
    public Guid SectorId { get; set; }
    public Guid? SinkId { get; set; }
    public Guid? NextHopId { get; set; }
    public int? HopCount { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Firmware { get; set; } = "sim-1.2.0"; public string Transport { get; set; } = "simulated-rf";
    public int InstantaneousMinutes { get; set; } = 15; public int BlockMinutes { get; set; } = 30;
    public double? Rssi { get; set; }
    public double? Snr { get; set; }
    public double? Pdr { get; set; }
    public double? LatencyMs { get; set; }
    public double? ReadSuccess { get; set; }
    public int Neighbors { get; set; }
    public double? RoutingCost { get; set; }
    public int Retries { get; set; }
    public DateTimeOffset? LastCommunication { get; set; }
    public DateTimeOffset? ObservedAt { get; set; }
}
public class Telemetry : TenantEntity
{
    public Guid MeterId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset SourceTimestamp { get; set; }
    public double? Rssi { get; set; }
    public double? Snr { get; set; }
    public double? Pdr { get; set; }
    public double? LatencyMs { get; set; }
    public int Retries { get; set; }
    public int? HopCount { get; set; }
    public double? RoutingCost { get; set; }
    public double? ReadSuccess { get; set; }
    public string Quality { get; set; } = "simulated";
}
public class Neighbor : TenantEntity { public Guid MeterId { get; set; } public Guid NeighborId { get; set; } public double DistanceM { get; set; } public double Rssi { get; set; } }
public class RouteChange : TenantEntity { public Guid MeterId { get; set; } public Guid? NextHopId { get; set; } public Guid? SinkId { get; set; } public int? HopCount { get; set; } public DateTimeOffset Timestamp { get; set; } }
public class ProfileDefinition : TenantEntity { public string Key { get; set; } = ""; public string Name { get; set; } = ""; public int? IntervalMinutes { get; set; } public string CaptureObjects { get; set; } = "[]"; }
public class ObisObject : TenantEntity { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Unit { get; set; } = ""; public int Scaler { get; set; } public string DataType { get; set; } = "float64"; public string Access { get; set; } = "read"; }
public class ProfileReading : TenantEntity
{
    public Guid MeterId { get; set; }
    public string Profile { get; set; } = ""; public DateTimeOffset Timestamp { get; set; }
    public double? Voltage { get; set; }
    public double? CurrentA { get; set; }
    public double? CurrentB { get; set; }
    public double? CurrentC { get; set; }
    public double? ActivePowerKw { get; set; }
    public double? ApparentPowerKva { get; set; }
    public double? PowerFactor { get; set; }
    public double? FrequencyHz { get; set; }
    public double? CumulativeKwh { get; set; }
    public double? IntervalKwh { get; set; }
    public double? MaximumDemandKw { get; set; }
    public string Quality { get; set; } = "simulated"; public DateTimeOffset SourceTimestamp { get; set; }
}
public class MeterEvent : TenantEntity { public Guid MeterId { get; set; } public string Profile { get; set; } = ""; public string Code { get; set; } = ""; public string Description { get; set; } = ""; public DateTimeOffset Timestamp { get; set; } }
public class CommunicationAttempt : TenantEntity { public Guid MeterId { get; set; } public string Profile { get; set; } = ""; public bool Success { get; set; } public string? Error { get; set; } public DateTimeOffset Timestamp { get; set; } }
public class DiagnosticRecord : TenantEntity { public Guid MeterId { get; set; } public string Rule { get; set; } = ""; public string Evidence { get; set; } = ""; public DateTimeOffset Timestamp { get; set; } }
public class Incident : TenantEntity { public Guid? MeterId { get; set; } public string Title { get; set; } = ""; public string Severity { get; set; } = "warning"; public string Status { get; set; } = "open"; public DateTimeOffset Timestamp { get; set; } }
public class WorkOrder : TenantEntity { public Guid? MeterId { get; set; } public string Description { get; set; } = ""; public string Status { get; set; } = "proposed"; public bool HumanApproved { get; set; } }
public class Conversation : TenantEntity { public string Owner { get; set; } = ""; public DateTimeOffset Timestamp { get; set; } public string Messages { get; set; } = "[]"; }
public class ToolExecution : TenantEntity { public Guid? ConversationId { get; set; } public string Tool { get; set; } = ""; public string Arguments { get; set; } = "{}"; public string Result { get; set; } = ""; public DateTimeOffset Timestamp { get; set; } }
public class GeneratedReport : TenantEntity { public string Owner { get; set; } = ""; public string Format { get; set; } = "csv"; public string Kind { get; set; } = "inventory"; public string Filter { get; set; } = "{}"; public string Status { get; set; } = "queued"; public byte[]? Content { get; set; } public string? Error { get; set; } public DateTimeOffset Timestamp { get; set; } public DateTimeOffset ExpiresAt { get; set; } }
public class AuditLog : TenantEntity { public string Actor { get; set; } = ""; public string Action { get; set; } = ""; public string Resource { get; set; } = ""; public DateTimeOffset Timestamp { get; set; } }
public class Simulation : TenantEntity { public string Name { get; set; } = "Default demo"; public bool Running { get; set; } public bool SeedingComplete { get; set; } public DateTimeOffset Clock { get; set; } public DateTimeOffset Start { get; set; } public double Latitude { get; set; } public double Longitude { get; set; } public double RadiusM { get; set; } public int Seed { get; set; } }

public static class Demo
{
    public static readonly Guid UtilityId = Id(1);
    public static Guid Id(int i) { var b = new byte[16]; BitConverter.GetBytes(i).CopyTo(b, 0); b[15] = 1; return new Guid(b); }
    public static readonly string[] Profiles = ["instantaneous", "block", "daily", "billing", "voltage-events", "current-events", "power-events", "transaction-events", "other-events", "non-rollover-events"];
}
