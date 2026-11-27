#!/usr/bin/env bash
set -euo pipefail
: "${MM_RESOURCE_GROUP:?}" "${MM_POSTGRES_SERVER:?}" "${MM_POSTGRES_PASSWORD:?}" "${MM_STORAGE_ACCOUNT:?}"
restore="mm-restore-${GITHUB_RUN_ID:?}"
dump=$(mktemp)
trap 'rm -f "$dump"; az postgres flexible-server delete --resource-group "$MM_RESOURCE_GROUP" --name "$restore" --yes --output none' EXIT
source_id=$(az postgres flexible-server show -g "$MM_RESOURCE_GROUP" -n "$MM_POSTGRES_SERVER" --query id -o tsv)
az postgres flexible-server restore --resource-group "$MM_RESOURCE_GROUP" --name "$restore" --source-server "$source_id" --restore-time "$(date -u -d '10 minutes ago' +%FT%TZ)" --output none
host=$(az postgres flexible-server show -g "$MM_RESOURCE_GROUP" -n "$restore" --query fullyQualifiedDomainName -o tsv)
export PGPASSWORD="$MM_POSTGRES_PASSWORD"
psql "host=$host dbname=metermind user=metermind sslmode=verify-full" -v ON_ERROR_STOP=1 -c 'SELECT count(*) FROM "__EFMigrationsHistory"; SELECT count(*) FROM "Meters";'
pg_dump "host=$host dbname=metermind user=metermind sslmode=verify-full" --format=custom --file "$dump"
pg_restore --list "$dump" > /dev/null
az storage blob upload --account-name "$MM_STORAGE_ACCOUNT" --container-name backups --name "$restore.dump" --file "$dump" --auth-mode login --overwrite false --output none
echo 'Point-in-time restore queried successfully; logical dump archived.'
