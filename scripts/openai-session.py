"""Validate an OpenAI credential from stdin; optionally run the local API with an in-memory secret.

No credential is written to disk, command arguments or program output.
"""
import getpass, json, os, subprocess, sys, urllib.request, urllib.error
from pathlib import Path

if sys.stdin.isatty():
    config={'key':getpass.getpass('OpenAI API key (hidden): '),'serve':True}
else:
    config=json.loads(sys.stdin.readline())
key=config.pop('key')
headers={'Authorization':'Bearer '+key,'Content-Type':'application/json'}
try:
    with urllib.request.urlopen(urllib.request.Request('https://api.openai.com/v1/models',headers=headers),timeout=30) as response:
        models=[row['id'] for row in json.load(response)['data']]
except urllib.error.HTTPError as error:
    try:code=json.load(error).get('error',{}).get('code')
    except Exception:code='unavailable'
    print(json.dumps({'authenticated':False,'status':error.code,'errorCode':code}),flush=True)
    sys.exit(2)
except Exception as error:
    print(json.dumps({'authenticated':False,'errorType':type(error).__name__}),flush=True);sys.exit(2)
preferred=['gpt-4.1-mini','gpt-4.1','gpt-5-mini']
model=config.get('model') or next((m for m in preferred if m in models),None)
print(json.dumps({'authenticated':True,'model':model}),flush=True)
if not config.get('serve'):sys.exit(0)
if not model:raise SystemExit('Choose an available tool-capable model.')
root=Path(__file__).resolve().parents[1]
env=os.environ.copy()
env.update({'OpenAI__ApiKey':key,'OpenAI__Model':model,'ConnectionStrings__MeterDb':'Host=127.0.0.1;Port=55432;Database=metermind;Username=metermind;Password=local-only','ASPNETCORE_ENVIRONMENT':'Development','DemoAuth':'true','Simulation__Enabled':'true'})
key=None;headers=None
command=[str(root/'.tools/dotnet/dotnet.exe'),'run','--no-build','--project',str(root/'src/MeterMind.API'),'--urls','http://127.0.0.1:5080']
try:subprocess.run(command,env=env,cwd=root,check=True)
except KeyboardInterrupt:pass
