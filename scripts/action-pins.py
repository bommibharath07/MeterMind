from pathlib import Path
import subprocess,json
actions={'actions/checkout':'v6','actions/setup-dotnet':'v6','actions/setup-node':'v7','actions/upload-artifact':'v4','actions/download-artifact':'v4','azure/login':'v2','docker/setup-buildx-action':'v3','docker/login-action':'v3','docker/build-push-action':'v6','github/codeql-action':'v4','anchore/scan-action':'v7'}
pins={}
for repo,tag in actions.items():
    lines=subprocess.check_output(['git','ls-remote',f'https://github.com/{repo}.git',f'refs/tags/{tag}',f'refs/tags/{tag}^{{}}'],text=True).splitlines()
    if not lines:raise RuntimeError(f'Tag not found: {repo}@{tag}')
    sha=lines[-1].split()[0]
    if len(sha)!=40:raise RuntimeError('Invalid SHA')
    pins[repo]={'tag':tag,'sha':sha}
Path('docs/action-pins.json').write_text(json.dumps(pins,indent=2)+'\n')
print(json.dumps(pins,indent=2))
