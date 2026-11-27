from pathlib import Path
import sys
actual=Path(sys.argv[1]).read_text(encoding='utf-8-sig').splitlines()
expected=Path('docs/sql/migrations.sql').read_text(encoding='utf-8-sig').splitlines()
assert actual==expected,'Generated migration SQL differs from committed artifact'
print('Migration SQL matches the committed artifact')
