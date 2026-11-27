using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeterMind.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Utilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Resource = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.UniqueConstraint("AK_AuditLog_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_AuditLog_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Messages = table.Column<string>(type: "text", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversation", x => x.Id);
                    table.UniqueConstraint("AK_Conversation_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Conversation_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Gateways",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Online = table.Column<bool>(type: "boolean", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gateways", x => x.Id);
                    table.UniqueConstraint("AK_Gateways_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Gateways_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedReport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Filter = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedReport", x => x.Id);
                    table.UniqueConstraint("AK_GeneratedReport_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_GeneratedReport_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeshNetwork",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<long>(type: "bigint", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeshNetwork", x => x.Id);
                    table.UniqueConstraint("AK_MeshNetwork_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeshNetwork_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObisObject",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Scaler = table.Column<int>(type: "integer", nullable: false),
                    DataType = table.Column<string>(type: "text", nullable: false),
                    Access = table.Column<string>(type: "text", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObisObject", x => x.Id);
                    table.UniqueConstraint("AK_ObisObject_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_ObisObject_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfileDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "integer", nullable: true),
                    CaptureObjects = table.Column<string>(type: "text", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileDefinition", x => x.Id);
                    table.UniqueConstraint("AK_ProfileDefinition_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProfileDefinition_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sector", x => x.Id);
                    table.UniqueConstraint("AK_Sector_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Sector_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Simulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Running = table.Column<bool>(type: "boolean", nullable: false),
                    SeedingComplete = table.Column<bool>(type: "boolean", nullable: false),
                    Clock = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    RadiusM = table.Column<double>(type: "double precision", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulations", x => x.Id);
                    table.UniqueConstraint("AK_Simulations_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Simulations_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToolExecution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tool = table.Column<string>(type: "text", nullable: false),
                    Arguments = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolExecution", x => x.Id);
                    table.UniqueConstraint("AK_ToolExecution_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_ToolExecution_Conversation_UtilityId_ConversationId",
                        columns: x => new { x.UtilityId, x.ConversationId },
                        principalTable: "Conversation",
                        principalColumns: new[] { "UtilityId", "Id" });
                    table.ForeignKey(
                        name: "FK_ToolExecution_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GatewayId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sinks", x => x.Id);
                    table.UniqueConstraint("AK_Sinks_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Sinks_Gateways_UtilityId_GatewayId",
                        columns: x => new { x.UtilityId, x.GatewayId },
                        principalTable: "Gateways",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sinks_MeshNetwork_UtilityId_NetworkId",
                        columns: x => new { x.UtilityId, x.NetworkId },
                        principalTable: "MeshNetwork",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sinks_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Meters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Serial = table.Column<string>(type: "text", nullable: false),
                    NodeAddress = table.Column<long>(type: "bigint", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SinkId = table.Column<Guid>(type: "uuid", nullable: true),
                    NextHopId = table.Column<Guid>(type: "uuid", nullable: true),
                    HopCount = table.Column<int>(type: "integer", nullable: true),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Firmware = table.Column<string>(type: "text", nullable: false),
                    Transport = table.Column<string>(type: "text", nullable: false),
                    InstantaneousMinutes = table.Column<int>(type: "integer", nullable: false),
                    BlockMinutes = table.Column<int>(type: "integer", nullable: false),
                    Rssi = table.Column<double>(type: "double precision", nullable: true),
                    Snr = table.Column<double>(type: "double precision", nullable: true),
                    Pdr = table.Column<double>(type: "double precision", nullable: true),
                    LatencyMs = table.Column<double>(type: "double precision", nullable: true),
                    ReadSuccess = table.Column<double>(type: "double precision", nullable: true),
                    Neighbors = table.Column<int>(type: "integer", nullable: false),
                    RoutingCost = table.Column<double>(type: "double precision", nullable: true),
                    Retries = table.Column<int>(type: "integer", nullable: false),
                    LastCommunication = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meters", x => x.Id);
                    table.UniqueConstraint("AK_Meters_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Meters_MeshNetwork_UtilityId_NetworkId",
                        columns: x => new { x.UtilityId, x.NetworkId },
                        principalTable: "MeshNetwork",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meters_Sector_UtilityId_SectorId",
                        columns: x => new { x.UtilityId, x.SectorId },
                        principalTable: "Sector",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meters_Sinks_UtilityId_SinkId",
                        columns: x => new { x.UtilityId, x.SinkId },
                        principalTable: "Sinks",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meters_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationAttempt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Profile = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationAttempt", x => x.Id);
                    table.UniqueConstraint("AK_CommunicationAttempt_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_CommunicationAttempt_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationAttempt_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosticRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rule = table.Column<string>(type: "text", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticRecord", x => x.Id);
                    table.UniqueConstraint("AK_DiagnosticRecord_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_DiagnosticRecord_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiagnosticRecord_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Incident",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident", x => x.Id);
                    table.UniqueConstraint("AK_Incident_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Incident_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incident_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeterEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Profile = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterEvent", x => x.Id);
                    table.UniqueConstraint("AK_MeterEvent_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_MeterEvent_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeterEvent_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Neighbors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    NeighborId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistanceM = table.Column<double>(type: "double precision", nullable: false),
                    Rssi = table.Column<double>(type: "double precision", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Neighbors", x => x.Id);
                    table.UniqueConstraint("AK_Neighbors_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Neighbors_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Neighbors_Meters_UtilityId_NeighborId",
                        columns: x => new { x.UtilityId, x.NeighborId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Neighbors_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Readings",
                columns: table => new
                {
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Profile = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Voltage = table.Column<double>(type: "double precision", nullable: true),
                    CurrentA = table.Column<double>(type: "double precision", nullable: true),
                    CurrentB = table.Column<double>(type: "double precision", nullable: true),
                    CurrentC = table.Column<double>(type: "double precision", nullable: true),
                    ActivePowerKw = table.Column<double>(type: "double precision", nullable: true),
                    ApparentPowerKva = table.Column<double>(type: "double precision", nullable: true),
                    PowerFactor = table.Column<double>(type: "double precision", nullable: true),
                    FrequencyHz = table.Column<double>(type: "double precision", nullable: true),
                    CumulativeKwh = table.Column<double>(type: "double precision", nullable: true),
                    IntervalKwh = table.Column<double>(type: "double precision", nullable: true),
                    MaximumDemandKw = table.Column<double>(type: "double precision", nullable: true),
                    Quality = table.Column<string>(type: "text", nullable: false),
                    SourceTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readings", x => new { x.UtilityId, x.MeterId, x.Profile, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_Readings_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Readings_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    NextHopId = table.Column<Guid>(type: "uuid", nullable: true),
                    SinkId = table.Column<Guid>(type: "uuid", nullable: true),
                    HopCount = table.Column<int>(type: "integer", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.UniqueConstraint("AK_Routes_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_Routes_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Telemetry",
                columns: table => new
                {
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Rssi = table.Column<double>(type: "double precision", nullable: true),
                    Snr = table.Column<double>(type: "double precision", nullable: true),
                    Pdr = table.Column<double>(type: "double precision", nullable: true),
                    LatencyMs = table.Column<double>(type: "double precision", nullable: true),
                    Retries = table.Column<int>(type: "integer", nullable: false),
                    HopCount = table.Column<int>(type: "integer", nullable: true),
                    RoutingCost = table.Column<double>(type: "double precision", nullable: true),
                    ReadSuccess = table.Column<double>(type: "double precision", nullable: true),
                    Quality = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Telemetry", x => new { x.UtilityId, x.MeterId, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_Telemetry_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Telemetry_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    HumanApproved = table.Column<bool>(type: "boolean", nullable: false),
                    UtilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrder", x => x.Id);
                    table.UniqueConstraint("AK_WorkOrder_UtilityId_Id", x => new { x.UtilityId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkOrder_Meters_UtilityId_MeterId",
                        columns: x => new { x.UtilityId, x.MeterId },
                        principalTable: "Meters",
                        principalColumns: new[] { "UtilityId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrder_Utilities_UtilityId",
                        column: x => x.UtilityId,
                        principalTable: "Utilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationAttempt_UtilityId_MeterId_Profile_Timestamp",
                table: "CommunicationAttempt",
                columns: new[] { "UtilityId", "MeterId", "Profile", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticRecord_UtilityId_MeterId",
                table: "DiagnosticRecord",
                columns: new[] { "UtilityId", "MeterId" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReport_UtilityId_Status",
                table: "GeneratedReport",
                columns: new[] { "UtilityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_UtilityId_MeterId",
                table: "Incident",
                columns: new[] { "UtilityId", "MeterId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeterEvent_UtilityId_MeterId_Profile_Timestamp",
                table: "MeterEvent",
                columns: new[] { "UtilityId", "MeterId", "Profile", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meters_UtilityId_Latitude_Longitude",
                table: "Meters",
                columns: new[] { "UtilityId", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Meters_UtilityId_NetworkId",
                table: "Meters",
                columns: new[] { "UtilityId", "NetworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meters_UtilityId_NodeAddress_NetworkId",
                table: "Meters",
                columns: new[] { "UtilityId", "NodeAddress", "NetworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meters_UtilityId_Rssi",
                table: "Meters",
                columns: new[] { "UtilityId", "Rssi" });

            migrationBuilder.CreateIndex(
                name: "IX_Meters_UtilityId_SectorId",
                table: "Meters",
                columns: new[] { "UtilityId", "SectorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meters_UtilityId_Serial",
                table: "Meters",
                columns: new[] { "UtilityId", "Serial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meters_UtilityId_SinkId",
                table: "Meters",
                columns: new[] { "UtilityId", "SinkId" });

            migrationBuilder.CreateIndex(
                name: "IX_Neighbors_UtilityId_MeterId_NeighborId",
                table: "Neighbors",
                columns: new[] { "UtilityId", "MeterId", "NeighborId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Neighbors_UtilityId_NeighborId",
                table: "Neighbors",
                columns: new[] { "UtilityId", "NeighborId" });

            migrationBuilder.CreateIndex(
                name: "IX_ObisObject_UtilityId_Code",
                table: "ObisObject",
                columns: new[] { "UtilityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileDefinition_UtilityId_Key",
                table: "ProfileDefinition",
                columns: new[] { "UtilityId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_UtilityId_MeterId_Timestamp",
                table: "Routes",
                columns: new[] { "UtilityId", "MeterId", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_UtilityId",
                table: "Simulations",
                column: "UtilityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sinks_UtilityId_GatewayId",
                table: "Sinks",
                columns: new[] { "UtilityId", "GatewayId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sinks_UtilityId_NetworkId",
                table: "Sinks",
                columns: new[] { "UtilityId", "NetworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_Telemetry_UtilityId_Timestamp",
                table: "Telemetry",
                columns: new[] { "UtilityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecution_UtilityId_ConversationId",
                table: "ToolExecution",
                columns: new[] { "UtilityId", "ConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_UtilityId_MeterId",
                table: "WorkOrder",
                columns: new[] { "UtilityId", "MeterId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "CommunicationAttempt");

            migrationBuilder.DropTable(
                name: "DiagnosticRecord");

            migrationBuilder.DropTable(
                name: "GeneratedReport");

            migrationBuilder.DropTable(
                name: "Incident");

            migrationBuilder.DropTable(
                name: "MeterEvent");

            migrationBuilder.DropTable(
                name: "Neighbors");

            migrationBuilder.DropTable(
                name: "ObisObject");

            migrationBuilder.DropTable(
                name: "ProfileDefinition");

            migrationBuilder.DropTable(
                name: "Readings");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Simulations");

            migrationBuilder.DropTable(
                name: "Telemetry");

            migrationBuilder.DropTable(
                name: "ToolExecution");

            migrationBuilder.DropTable(
                name: "WorkOrder");

            migrationBuilder.DropTable(
                name: "Conversation");

            migrationBuilder.DropTable(
                name: "Meters");

            migrationBuilder.DropTable(
                name: "Sector");

            migrationBuilder.DropTable(
                name: "Sinks");

            migrationBuilder.DropTable(
                name: "Gateways");

            migrationBuilder.DropTable(
                name: "MeshNetwork");

            migrationBuilder.DropTable(
                name: "Utilities");
        }
    }
}
