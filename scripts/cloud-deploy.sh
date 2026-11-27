#!/usr/bin/env bash
set -euo pipefail
: "${MM_RESOURCE_GROUP:?}" "${MM_ENVIRONMENT:?}" "${MM_AUTH_AUTHORITY:?}" "${MM_AUTH_AUDIENCE:?}" "${MM_POSTGRES_PASSWORD:?}"
case "$MM_ENVIRONMENT" in staging|production) ;; *) exit 1;; esac
case "${GITHUB_REF_NAME:-}" in main|master) ;; *) echo 'Cloud deployments require a protected main or master ref.' >&2; exit 1;; esac
mkdir -p artifacts
params=$(mktemp)
chmod 600 "$params"
trap 'rm -f "$params"' EXIT
export MM_DEPLOY_MODE="${1:?}"
python - "$params" <<'PY'
import json,os,sys,re
mode=os.environ['MM_DEPLOY_MODE'];images={}
if mode=='release':
    images=json.load(open('release.json'))
    assert images['sourceSha']==os.environ['EXPECTED_SHA']
    for key in ['api','worker','migration','simulator','mcp','frontend']:
        assert re.fullmatch(r'[a-z0-9.-]+/metermind/'+key+r'@sha256:[a-f0-9]{64}',images[key]),'Invalid immutable image reference'
params={'environment':os.environ['MM_ENVIRONMENT'],'postgresPassword':os.environ['MM_POSTGRES_PASSWORD'],'authAuthority':os.environ['MM_AUTH_AUTHORITY'],'authAudience':os.environ['MM_AUTH_AUDIENCE'],'openAiApiKey':os.getenv('MM_OPENAI_KEY',''),'openAiModel':os.getenv('MM_OPENAI_MODEL',''),'deployJobs':False,'deployServices':False,'images':images}
json.dump({'$schema':'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#','contentVersion':'1.0.0.0','parameters':{k:{'value':v} for k,v in params.items()}},open(sys.argv[1],'w'))
PY
az deployment group what-if --resource-group "$MM_RESOURCE_GROUP" --template-file infra/main.bicep --parameters "@$params" --only-show-errors
outputs=$(az deployment group create --resource-group "$MM_RESOURCE_GROUP" --template-file infra/main.bicep --parameters "@$params" --only-show-errors --query properties.outputs -o json)
if [[ "$MM_DEPLOY_MODE" == release ]]; then
  registry=$(printf '%s' "$outputs" | jq -r .registryName.value)
  server=$(printf '%s' "$outputs" | jq -r .registryServer.value)
  if [[ "$MM_ENVIRONMENT" == production ]]; then
    : "${MM_SOURCE_ACR_ID:?Set the tested staging registry resource ID for digest-preserving import}"
    for service in api worker migration simulator mcp frontend; do
      source=$(jq -r --arg key "$service" '.[$key]' release.json)
      repository="${source#*/}"
      digest="${source##*@}"
      az acr import --name "$registry" --registry "$MM_SOURCE_ACR_ID" --source "$repository" --image "metermind/$service:$EXPECTED_SHA" --force --output none
      actual=$(az acr repository show --name "$registry" --image "metermind/$service:$EXPECTED_SHA" --query digest -o tsv)
      test "$actual" = "$digest"
    done
  fi
  export MM_DESTINATION_REGISTRY="$server"
  python - "$params" <<'PY'
import json,os,sys
p=json.load(open(sys.argv[1]));images=p['parameters']['images']['value']
for key in ['api','worker','migration','simulator','mcp','frontend']:
    source=images[key]
    if os.environ['MM_ENVIRONMENT']=='staging':assert source.split('/')[0]==os.environ['MM_DESTINATION_REGISTRY'],'Build registry differs from deployment registry'
    images[key]=os.environ['MM_DESTINATION_REGISTRY']+'/'+source.split('/',1)[1]
p['parameters']['deployJobs']['value']=True
json.dump(p,open(sys.argv[1],'w'))
PY
  az deployment group create --resource-group "$MM_RESOURCE_GROUP" --template-file infra/main.bicep --parameters "@$params" --only-show-errors --output none
  bash scripts/cloud-job.sh migration
  if [[ "$MM_ENVIRONMENT" == staging ]]; then bash scripts/cloud-job.sh simulator; fi
  python - "$params" <<'PY'
import json,sys
p=json.load(open(sys.argv[1]));p['parameters']['deployServices']['value']=True;json.dump(p,open(sys.argv[1],'w'))
PY
  url=$(az deployment group create --resource-group "$MM_RESOURCE_GROUP" --template-file infra/main.bicep --parameters "@$params" --query properties.outputs.frontendUrl.value -o tsv --only-show-errors)
  export MM_BASE_URL="$url"
  python scripts/smoke.py
fi
