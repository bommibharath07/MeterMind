#!/usr/bin/env bash
set -euo pipefail
: "${MM_ENVIRONMENT:?}" "${MM_RESOURCE_GROUP:?}"
case "${GITHUB_REF_NAME:-}" in main|master) ;; *) exit 1;; esac
python - <<'PY'
import json,re
d=json.load(open('release.json'))
for key in ['api','worker','mcp','frontend']:assert re.fullmatch(r'[a-z0-9.-]+/metermind/'+key+r'@sha256:[a-f0-9]{64}',d[key])
PY
# Environment approval certifies compatibility with the current schema. No migration reversal.
for service in api worker mcp frontend; do
  image=$(jq -r --arg key "$service" '.[$key]' release.json)
  az containerapp update --name "mm-${MM_ENVIRONMENT}-${service}" --resource-group "$MM_RESOURCE_GROUP" --image "$image" --output none
done
host=$(az containerapp show --name "mm-${MM_ENVIRONMENT}-frontend" --resource-group "$MM_RESOURCE_GROUP" --query properties.configuration.ingress.fqdn -o tsv)
export MM_BASE_URL="https://$host"
python scripts/smoke.py
