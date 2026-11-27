from pathlib import Path
import re,json,yaml
expected=['ci','security','infrastructure','database-migrations','seed-database','build-images','deploy-staging','deploy-production','ai-evaluation','simulation','backup-verification','rollback']
for name in expected:assert Path(f'.github/workflows/{name}.yml').is_file(),name
for p in Path('.github').rglob('*.yml'):
    data=yaml.load(p.read_text(),Loader=yaml.BaseLoader)
    assert data and 'name' in data,p
    for use in re.findall(r'uses:\s*([^\s#]+)',p.read_text()):
        if use.startswith('./'):assert Path(use).exists(),use
        else:assert re.fullmatch(r'[^@]+@[0-9a-f]{40}',use),(p,use)
    if 'workflows' in str(p):assert 'on' in data and 'jobs' in data,p
print('All 12 requested workflows and reusable workflows parse; remote actions are pinned to full SHAs. Run actionlint for expression and runner validation.')
