import os,json,urllib.request,urllib.error
key=os.environ.get('OPENAI_API_KEY','').strip();model=os.environ['OPENAI_MODEL']
if not key:raise SystemExit('OPENAI_API_KEY is unavailable. Add it as an Actions repository secret before running this evaluation.')
# One request, at most 300 output tokens, no customer records. Ordinary CI uses mocked transport.
body={'model':model,'input':'Call network_probe once; do not invent its result.','tools':[{'type':'function','name':'network_probe','description':'Return synthetic acceptance count','strict':True,'parameters':{'type':'object','properties':{},'required':[],'additionalProperties':False}}],'tool_choice':'required','max_output_tokens':300,'store':False}
req=urllib.request.Request('https://api.openai.com/v1/responses',data=json.dumps(body).encode(),headers={'Authorization':'Bearer '+key,'Content-Type':'application/json'})
try:
    with urllib.request.urlopen(req,timeout=60) as response:result=json.load(response)
except urllib.error.HTTPError as error:
    # Do not print the response body, which can include credential fragments.
    raise SystemExit(f'OpenAI Responses request failed with HTTP {error.code}. Check the Actions secret, model access and API billing.') from None
assert any(x.get('type')=='function_call' and x.get('name')=='network_probe' for x in result['output'])
print('Configured model supports required Responses function calling. This does not establish factual accuracy on production data.')
