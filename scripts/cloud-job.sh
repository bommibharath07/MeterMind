#!/usr/bin/env bash
set -euo pipefail
: "${MM_ENVIRONMENT:?}" "${MM_RESOURCE_GROUP:?}"
case "${1:?}" in migration|simulator) job="mm-${MM_ENVIRONMENT}-$1";; *) echo 'Unrecognized cloud job' >&2; exit 1;; esac
execution=$(az containerapp job start --name "$job" --resource-group "$MM_RESOURCE_GROUP" --query name -o tsv)
for ((i=0;i<720;i++)); do
  status=$(az containerapp job execution show --name "$job" --resource-group "$MM_RESOURCE_GROUP" --job-execution-name "$execution" --query properties.status -o tsv)
  case "$status" in Succeeded) echo "$job succeeded"; exit 0;; Failed|Stopped|Degraded) echo "$job failed: $status" >&2; exit 1;; esac
  sleep 5
done
echo "$job timed out; inspect cloud execution" >&2
exit 1
