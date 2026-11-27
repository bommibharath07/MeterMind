import os,json,urllib.request
key=os.environ['OPENAI_API_KEY'];model=os.environ['OPENAI_MODEL']
# One request, at most 300 output tokens, no customer records. Ordinary CI uses mocked transport.
body={'model':model,'input':'Call network_probe once; do not invent its result.','tools':[{'type':'function','name':'network_probe','description':'Return synthetic acceptance count','strict':True,'parameters':{'type':'object','properties':{},'required':[],'additionalProperties':False}}],'tool_choice':'required','max_output_tokens':300,'store':False}
req=urllib.request.Request('https://api.openai.com/v1/responses',data=json.dumps(body).encode(),headers={'Authorization':'Bearer '+key,'Content-Type':'application/json'})
with urllib.request.urlopen(req,timeout=60) as response:result=json.load(response)
assert any(x.get('type')=='function_call' and x.get('name')=='network_probe' for x in result['output'])
print('Configured model supports required Responses function calling. This does not establish factual accuracy on production data.')
