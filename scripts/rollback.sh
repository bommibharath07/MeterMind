#!/usr/bin/env bash
set -euo pipefail
: "${MM_ENVIRONMENT:?}" "${MM_RESOURCE_GROUP:?}"
case "${GITHUB_REF_NAME:-}" in main|master) ;; *) exit 1;; esac
python - <<'PY'
import json,re
d=json.load(open('release.json'))
for key in ['api','worker','mcp','frontend']:assert re.fullmatch(r'[a-z0-9.-]+/metermind/'+key+r'@sha256:[a-f0-9]{64}',d[key])
PY
server=$(az containerapp show --name "mm-${MM_ENVIRONMENT}-api" --resource-group "$MM_RESOURCE_GROUP" --query 'properties.configuration.registries[0].server' -o tsv)
registry="${server%%.*}"
# Environment approval certifies compatibility with the current schema. No migration reversal.
for service in api worker mcp frontend; do
  source=$(jq -r --arg key "$service" '.[$key]' release.json)
  repository="${source#*/}"
  digest="${source##*@}"
  if [[ "${source%%/*}" != "$server" ]]; then
    if ! az acr repository show --name "$registry" --image "$repository" --output none 2>/dev/null; then
      : "${MM_SOURCE_ACR_ID:?An older release must be imported from its trusted source registry}"
      az acr import --name "$registry" --registry "$MM_SOURCE_ACR_ID" --source "$repository" --image "metermind/$service:rollback-${GITHUB_RUN_ID:?}" --force --output none
    fi
    actual=$(az acr repository show --name "$registry" --image "$repository" --query digest -o tsv)
    test "$actual" = "$digest"
  fi
  image="$server/$repository"
  az containerapp update --name "mm-${MM_ENVIRONMENT}-${service}" --resource-group "$MM_RESOURCE_GROUP" --image "$image" --output none
done
host=$(az containerapp show --name "mm-${MM_ENVIRONMENT}-frontend" --resource-group "$MM_RESOURCE_GROUP" --query properties.configuration.ingress.fqdn -o tsv)
export MM_BASE_URL="https://$host"
python scripts/smoke.py
