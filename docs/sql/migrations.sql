CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Utilities" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        CONSTRAINT "PK_Utilities" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "AuditLog" (
        "Id" uuid NOT NULL,
        "Actor" text NOT NULL,
        "Action" text NOT NULL,
        "Resource" text NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_AuditLog" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_AuditLog_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_AuditLog_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Conversation" (
        "Id" uuid NOT NULL,
        "Owner" text NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "Messages" text NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Conversation" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Conversation_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Conversation_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Gateways" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Online" boolean NOT NULL,
        "Latitude" double precision NOT NULL,
        "Longitude" double precision NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Gateways" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Gateways_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Gateways_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "GeneratedReport" (
        "Id" uuid NOT NULL,
        "Owner" text NOT NULL,
        "Format" text NOT NULL,
        "Kind" text NOT NULL,
        "Filter" text NOT NULL,
        "Status" text NOT NULL,
        "Content" bytea,
        "Error" text,
        "Timestamp" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_GeneratedReport" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_GeneratedReport_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_GeneratedReport_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "MeshNetwork" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Address" bigint NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_MeshNetwork" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_MeshNetwork_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_MeshNetwork_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "ObisObject" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Unit" text NOT NULL,
        "Scaler" integer NOT NULL,
        "DataType" text NOT NULL,
        "Access" text NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_ObisObject" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_ObisObject_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_ObisObject_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "ProfileDefinition" (
        "Id" uuid NOT NULL,
        "Key" text NOT NULL,
        "Name" text NOT NULL,
        "IntervalMinutes" integer,
        "CaptureObjects" text NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_ProfileDefinition" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_ProfileDefinition_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_ProfileDefinition_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Sector" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Sector" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Sector_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Sector_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Simulations" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Running" boolean NOT NULL,
        "SeedingComplete" boolean NOT NULL,
        "Clock" timestamp with time zone NOT NULL,
        "Start" timestamp with time zone NOT NULL,
        "Latitude" double precision NOT NULL,
        "Longitude" double precision NOT NULL,
        "RadiusM" double precision NOT NULL,
        "Seed" integer NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Simulations" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Simulations_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Simulations_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "ToolExecution" (
        "Id" uuid NOT NULL,
        "ConversationId" uuid,
        "Tool" text NOT NULL,
        "Arguments" text NOT NULL,
        "Result" text NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_ToolExecution" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_ToolExecution_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_ToolExecution_Conversation_UtilityId_ConversationId" FOREIGN KEY ("UtilityId", "ConversationId") REFERENCES "Conversation" ("UtilityId", "Id"),
        CONSTRAINT "FK_ToolExecution_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Sinks" (
        "Id" uuid NOT NULL,
        "GatewayId" uuid NOT NULL,
        "NetworkId" uuid NOT NULL,
        "Name" text NOT NULL,
        "X" double precision NOT NULL,
        "Y" double precision NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Sinks" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Sinks_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Sinks_Gateways_UtilityId_GatewayId" FOREIGN KEY ("UtilityId", "GatewayId") REFERENCES "Gateways" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Sinks_MeshNetwork_UtilityId_NetworkId" FOREIGN KEY ("UtilityId", "NetworkId") REFERENCES "MeshNetwork" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Sinks_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Meters" (
        "Id" uuid NOT NULL,
        "Serial" text NOT NULL,
        "NodeAddress" bigint NOT NULL,
        "NetworkId" uuid NOT NULL,
        "SectorId" uuid NOT NULL,
        "SinkId" uuid,
        "NextHopId" uuid,
        "HopCount" integer,
        "X" double precision NOT NULL,
        "Y" double precision NOT NULL,
        "Latitude" double precision NOT NULL,
        "Longitude" double precision NOT NULL,
        "Firmware" text NOT NULL,
        "Transport" text NOT NULL,
        "InstantaneousMinutes" integer NOT NULL,
        "BlockMinutes" integer NOT NULL,
        "Rssi" double precision,
        "Snr" double precision,
        "Pdr" double precision,
        "LatencyMs" double precision,
        "ReadSuccess" double precision,
        "Neighbors" integer NOT NULL,
        "RoutingCost" double precision,
        "Retries" integer NOT NULL,
        "LastCommunication" timestamp with time zone,
        "ObservedAt" timestamp with time zone,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Meters" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Meters_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Meters_MeshNetwork_UtilityId_NetworkId" FOREIGN KEY ("UtilityId", "NetworkId") REFERENCES "MeshNetwork" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Meters_Sector_UtilityId_SectorId" FOREIGN KEY ("UtilityId", "SectorId") REFERENCES "Sector" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Meters_Sinks_UtilityId_SinkId" FOREIGN KEY ("UtilityId", "SinkId") REFERENCES "Sinks" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Meters_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "CommunicationAttempt" (
        "Id" uuid NOT NULL,
        "MeterId" uuid NOT NULL,
        "Profile" text NOT NULL,
        "Success" boolean NOT NULL,
        "Error" text,
        "Timestamp" timestamp with time zone NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_CommunicationAttempt" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_CommunicationAttempt_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_CommunicationAttempt_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_CommunicationAttempt_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "DiagnosticRecord" (
        "Id" uuid NOT NULL,
        "MeterId" uuid NOT NULL,
        "Rule" text NOT NULL,
        "Evidence" text NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_DiagnosticRecord" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_DiagnosticRecord_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_DiagnosticRecord_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DiagnosticRecord_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Incident" (
        "Id" uuid NOT NULL,
        "MeterId" uuid,
        "Title" text NOT NULL,
        "Severity" text NOT NULL,
        "Status" text NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Incident" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Incident_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Incident_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Incident_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "MeterEvent" (
        "Id" uuid NOT NULL,
        "MeterId" uuid NOT NULL,
        "Profile" text NOT NULL,
        "Code" text NOT NULL,
        "Description" text NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_MeterEvent" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_MeterEvent_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_MeterEvent_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_MeterEvent_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Neighbors" (
        "Id" uuid NOT NULL,
        "MeterId" uuid NOT NULL,
        "NeighborId" uuid NOT NULL,
        "DistanceM" double precision NOT NULL,
        "Rssi" double precision NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Neighbors" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Neighbors_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Neighbors_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Neighbors_Meters_UtilityId_NeighborId" FOREIGN KEY ("UtilityId", "NeighborId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Neighbors_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Readings" (
        "UtilityId" uuid NOT NULL,
        "MeterId" uuid NOT NULL,
        "Profile" text NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "Voltage" double precision,
        "CurrentA" double precision,
        "CurrentB" double precision,
        "CurrentC" double precision,
        "ActivePowerKw" double precision,
        "ApparentPowerKva" double precision,
        "PowerFactor" double precision,
        "FrequencyHz" double precision,
        "CumulativeKwh" double precision,
        "IntervalKwh" double precision,
        "MaximumDemandKw" double precision,
        "Quality" text NOT NULL,
        "SourceTimestamp" timestamp with time zone NOT NULL,
        "Id" uuid NOT NULL,
        CONSTRAINT "PK_Readings" PRIMARY KEY ("UtilityId", "MeterId", "Profile", "Timestamp"),
        CONSTRAINT "FK_Readings_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Readings_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Routes" (
        "Id" uuid NOT NULL,
        "MeterId" uuid NOT NULL,
        "NextHopId" uuid,
        "SinkId" uuid,
        "HopCount" integer,
        "Timestamp" timestamp with time zone NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_Routes" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Routes_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_Routes_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Routes_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "Telemetry" (
        "UtilityId" uuid NOT NULL,
        "MeterId" uuid NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "SourceTimestamp" timestamp with time zone NOT NULL,
        "Rssi" double precision,
        "Snr" double precision,
        "Pdr" double precision,
        "LatencyMs" double precision,
        "Retries" integer NOT NULL,
        "HopCount" integer,
        "RoutingCost" double precision,
        "ReadSuccess" double precision,
        "Quality" text NOT NULL,
        "Id" uuid NOT NULL,
        CONSTRAINT "PK_Telemetry" PRIMARY KEY ("UtilityId", "MeterId", "Timestamp"),
        CONSTRAINT "FK_Telemetry_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Telemetry_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE TABLE "WorkOrder" (
        "Id" uuid NOT NULL,
        "MeterId" uuid,
        "Description" text NOT NULL,
        "Status" text NOT NULL,
        "HumanApproved" boolean NOT NULL,
        "UtilityId" uuid NOT NULL,
        CONSTRAINT "PK_WorkOrder" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_WorkOrder_UtilityId_Id" UNIQUE ("UtilityId", "Id"),
        CONSTRAINT "FK_WorkOrder_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId", "MeterId") REFERENCES "Meters" ("UtilityId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_WorkOrder_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_CommunicationAttempt_UtilityId_MeterId_Profile_Timestamp" ON "CommunicationAttempt" ("UtilityId", "MeterId", "Profile", "Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_DiagnosticRecord_UtilityId_MeterId" ON "DiagnosticRecord" ("UtilityId", "MeterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_GeneratedReport_UtilityId_Status" ON "GeneratedReport" ("UtilityId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Incident_UtilityId_MeterId" ON "Incident" ("UtilityId", "MeterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_MeterEvent_UtilityId_MeterId_Profile_Timestamp" ON "MeterEvent" ("UtilityId", "MeterId", "Profile", "Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Meters_UtilityId_Latitude_Longitude" ON "Meters" ("UtilityId", "Latitude", "Longitude");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Meters_UtilityId_NetworkId" ON "Meters" ("UtilityId", "NetworkId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_Meters_UtilityId_NodeAddress_NetworkId" ON "Meters" ("UtilityId", "NodeAddress", "NetworkId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Meters_UtilityId_Rssi" ON "Meters" ("UtilityId", "Rssi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Meters_UtilityId_SectorId" ON "Meters" ("UtilityId", "SectorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_Meters_UtilityId_Serial" ON "Meters" ("UtilityId", "Serial");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Meters_UtilityId_SinkId" ON "Meters" ("UtilityId", "SinkId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_Neighbors_UtilityId_MeterId_NeighborId" ON "Neighbors" ("UtilityId", "MeterId", "NeighborId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Neighbors_UtilityId_NeighborId" ON "Neighbors" ("UtilityId", "NeighborId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_ObisObject_UtilityId_Code" ON "ObisObject" ("UtilityId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_ProfileDefinition_UtilityId_Key" ON "ProfileDefinition" ("UtilityId", "Key");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_Routes_UtilityId_MeterId_Timestamp" ON "Routes" ("UtilityId", "MeterId", "Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE UNIQUE INDEX "IX_Simulations_UtilityId" ON "Simulations" ("UtilityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Sinks_UtilityId_GatewayId" ON "Sinks" ("UtilityId", "GatewayId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Sinks_UtilityId_NetworkId" ON "Sinks" ("UtilityId", "NetworkId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_Telemetry_UtilityId_Timestamp" ON "Telemetry" ("UtilityId", "Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_ToolExecution_UtilityId_ConversationId" ON "ToolExecution" ("UtilityId", "ConversationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    CREATE INDEX "IX_WorkOrder_UtilityId_MeterId" ON "WorkOrder" ("UtilityId", "MeterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127062938_InitialSchema') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261127062938_InitialSchema', '10.0.15');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127064056_PartitionMeasurements') THEN

    ALTER TABLE "Telemetry" RENAME TO "Telemetry_unpartitioned";
    CREATE TABLE "Telemetry" (LIKE "Telemetry_unpartitioned" INCLUDING DEFAULTS INCLUDING CONSTRAINTS) PARTITION BY RANGE ("Timestamp");
    DO $partitions$
    DECLARE d date; finish date; partition_name text;
    BEGIN
    SELECT LEAST(date_trunc('month',CURRENT_DATE-interval '1 year')::date,COALESCE(date_trunc('month',min("Timestamp"))::date,CURRENT_DATE)) INTO d FROM "Telemetry_unpartitioned";
    SELECT GREATEST((CURRENT_DATE+interval '2 years')::date,COALESCE((max("Timestamp")+interval '2 months')::date,CURRENT_DATE)) INTO finish FROM "Telemetry_unpartitioned";
    WHILE d<finish LOOP
    partition_name := 'Telemetry_' || to_char(d,'YYYYMM');
    EXECUTE format('CREATE TABLE %I PARTITION OF "Telemetry" FOR VALUES FROM (%L) TO (%L)',partition_name,d,(d+interval '1 month')::date);
    d := (d+interval '1 month')::date;
    END LOOP;
    END $partitions$;
    CREATE TABLE "Telemetry_default" PARTITION OF "Telemetry" DEFAULT;
    INSERT INTO "Telemetry" SELECT * FROM "Telemetry_unpartitioned";
    DROP TABLE "Telemetry_unpartitioned";
    ALTER TABLE "Telemetry" ADD CONSTRAINT "PK_Telemetry" PRIMARY KEY ("UtilityId","MeterId","Timestamp");
    ALTER TABLE "Telemetry" ADD CONSTRAINT "FK_Telemetry_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId","MeterId") REFERENCES "Meters" ("UtilityId","Id") ON DELETE RESTRICT;
    ALTER TABLE "Telemetry" ADD CONSTRAINT "FK_Telemetry_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT;
    CREATE INDEX "IX_Telemetry_UtilityId_Timestamp" ON "Telemetry" ("UtilityId","Timestamp");

    ALTER TABLE "Readings" RENAME TO "Readings_unpartitioned";
    CREATE TABLE "Readings" (LIKE "Readings_unpartitioned" INCLUDING DEFAULTS INCLUDING CONSTRAINTS) PARTITION BY RANGE ("Timestamp");
    DO $partitions$
    DECLARE d date; finish date; partition_name text;
    BEGIN
    SELECT LEAST(date_trunc('month',CURRENT_DATE-interval '1 year')::date,COALESCE(date_trunc('month',min("Timestamp"))::date,CURRENT_DATE)) INTO d FROM "Readings_unpartitioned";
    SELECT GREATEST((CURRENT_DATE+interval '2 years')::date,COALESCE((max("Timestamp")+interval '2 months')::date,CURRENT_DATE)) INTO finish FROM "Readings_unpartitioned";
    WHILE d<finish LOOP
    partition_name := 'Readings_' || to_char(d,'YYYYMM');
    EXECUTE format('CREATE TABLE %I PARTITION OF "Readings" FOR VALUES FROM (%L) TO (%L)',partition_name,d,(d+interval '1 month')::date);
    d := (d+interval '1 month')::date;
    END LOOP;
    END $partitions$;
    CREATE TABLE "Readings_default" PARTITION OF "Readings" DEFAULT;
    INSERT INTO "Readings" SELECT * FROM "Readings_unpartitioned";
    DROP TABLE "Readings_unpartitioned";
    ALTER TABLE "Readings" ADD CONSTRAINT "PK_Readings" PRIMARY KEY ("UtilityId","MeterId","Profile","Timestamp");
    ALTER TABLE "Readings" ADD CONSTRAINT "FK_Readings_Meters_UtilityId_MeterId" FOREIGN KEY ("UtilityId","MeterId") REFERENCES "Meters" ("UtilityId","Id") ON DELETE RESTRICT;
    ALTER TABLE "Readings" ADD CONSTRAINT "FK_Readings_Utilities_UtilityId" FOREIGN KEY ("UtilityId") REFERENCES "Utilities" ("Id") ON DELETE RESTRICT;
    CREATE INDEX "IX_Readings_UtilityId_Timestamp" ON "Readings" ("UtilityId","Timestamp");

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261127064056_PartitionMeasurements') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261127064056_PartitionMeasurements', '10.0.15');
    END IF;
END $EF$;
COMMIT;

