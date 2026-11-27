import json,sys
d=json.load(open(sys.argv[1],encoding='utf-8-sig'))
assert not d.get('errors'),d.get('errors')
bad=[]
for project in d.get('projects',[]):
    for framework in project.get('frameworks',[]):
        for package in framework.get('topLevelPackages',[])+framework.get('transitivePackages',[]):
            for issue in package.get('vulnerabilities',[]):
                if issue['severity'].lower() in ['high','critical']:bad.append((package['id'],issue['severity']))
assert not bad, f'High or critical dependency vulnerabilities: {bad}'
print('NuGet audit passed')
