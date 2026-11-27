using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace MeterMind.Persistence.Migrations;

public partial class PartitionMeasurements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { migrationBuilder.Sql("""

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

"""); }
    protected override void Down(MigrationBuilder migrationBuilder) => throw new System.NotSupportedException("Use a reviewed forward migration; do not reverse partitioning automatically.");
}
