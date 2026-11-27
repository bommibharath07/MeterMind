-- Run as a reviewed migration only after verifying pg_available_extensions and Azure allowlists.
-- Native partitioning is the baseline; TimescaleDB is not installed automatically.
CREATE EXTENSION IF NOT EXISTS postgis;
ALTER TABLE "Meters" ADD COLUMN IF NOT EXISTS "Location" geography(Point,4326)
GENERATED ALWAYS AS (ST_SetSRID(ST_MakePoint("Longitude","Latitude"),4326)::geography) STORED;
CREATE INDEX IF NOT EXISTS "IX_Meters_Location" ON "Meters" USING GIST ("Location");
-- Example utility-scoped radius query: WHERE "UtilityId"=:utility AND ST_DWithin("Location",:center,:radius_m).
