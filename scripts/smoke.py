import os,json,time,urllib.request
base=os.environ['MM_BASE_URL'].rstrip('/')
token=os.environ.get('MM_SMOKE_TOKEN','')
assert token,'MM_SMOKE_TOKEN must be a short-lived utility-scoped token from the configured identity provider'
def get(path,auth=False):
    req=urllib.request.Request(base+path,headers={'Authorization':'Bearer '+token} if auth else {})
    with urllib.request.urlopen(req,timeout=30) as r:return json.load(r)
for attempt in range(60):
    try:get('/health/ready');break
    except Exception:
        if attempt==59:raise
        time.sleep(5)
summary=get('/api/network/summary',True)
meters=get('/api/meters?Limit=1',True)
assert 'total' in summary and 'items' in meters
from pathlib import Path
Path('artifacts').mkdir(exist_ok=True)
Path('artifacts/smoke.json').write_text(json.dumps({'base':base,'ready':True,'databaseQuery':True,'total':summary['total'],'sourceSha':os.getenv('EXPECTED_SHA'),'queryTime':summary['queryTime']}))
print('Readiness and authorized database smoke tests passed.')
