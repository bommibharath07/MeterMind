import json,os,time,statistics,platform,urllib.request
from pathlib import Path
base=os.environ.get('MM_BENCHMARK_URL','http://127.0.0.1:5080')
with urllib.request.urlopen(urllib.request.Request(base+'/auth/demo',data=b'',method='POST')) as r:token=json.load(r)['token']
results={}
for route in ['/api/network/summary','/api/meters?RssiBelow=-90','/api/network/topology']:
    samples=[];data=None
    for i in range(11):
        start=time.perf_counter()
        with urllib.request.urlopen(urllib.request.Request(base+route,headers={'Authorization':'Bearer '+token}),timeout=60) as r:data=json.load(r)
        elapsed=(time.perf_counter()-start)*1000
        if i:samples.append(elapsed)
    results[route]={'samples':len(samples),'medianMs':statistics.median(samples),'p95Ms':sorted(samples)[-1]}
output={'machine':platform.platform(),'logicalProcessors':os.cpu_count(),'dataset':'300 meters, 3897300 baseline records','kind':'single-client warm local HTTP; ten samples; P95 is nearest-rank maximum','measurements':results}
Path('artifacts').mkdir(exist_ok=True);Path('artifacts/performance.json').write_text(json.dumps(output,indent=2));print(json.dumps(output,indent=2))
