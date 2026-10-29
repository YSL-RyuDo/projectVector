"""Validate script relocation without launching Unity or modifying project assets."""
from pathlib import Path
import argparse
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[1]


def verify(pure_move=False):
    records = json.loads((ROOT / 'Tools/ScriptMigration.json').read_text(encoding='utf-8'))
    seen = set()
    for record in records:
        path = ROOT / record['new']
        assert path.is_file(), f'Missing relocated script: {path}'
        assert not (ROOT / record['old']).exists(), f'Old script remains: {record["old"]}'
        guid = re.search(r'^guid: (\w+)', Path(str(path)+'.meta').read_text(), re.M)[1]
        assert guid == record['guid'], f'GUID changed: {path}'
        assert guid not in seen, f'Duplicate GUID: {guid}'
        seen.add(guid)
        if pure_move and path.name != 'SentinelPolishAuthoring.cs':
            assert hashlib.sha256(path.read_bytes()).hexdigest() == record['sha256'], f'Unexpected code change: {path}'
    for directory in ['Assets/Scripts', 'Assets/Editor', 'Assets/KSW', 'Assets/Prefab']:
        assert not list((ROOT/directory).rglob('*.cs')), f'Owned source outside modules: {directory}'
    print(f'MODULE_LAYOUT_PASS: {len(records)} scripts, unchanged GUIDs, no source copies in old folders')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--pure-move', action='store_true')
    verify(parser.parse_args().pure_move)
