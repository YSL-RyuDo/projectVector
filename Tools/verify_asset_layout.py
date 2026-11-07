"""Check scene-owned asset placement, GUID identity and Resources keys.

Run after Unity import. --baseline accepts the external pre-move snapshot for
the one-time migration audit; ordinary runs require only tracked manifests.
"""
from collections import Counter
from pathlib import Path
import argparse
import json
import re

ROOT = Path(__file__).resolve().parents[1]


def resource_key(path):
    return path.split('/Resources/', 1)[1].rsplit('.', 1)[0] if '/Resources/' in path else None


def verify(baseline=None):
    migration = json.loads((ROOT/'Tools/AssetMigration.json').read_text(encoding='utf-8'))
    metas = {}
    for path in (ROOT/'Assets').rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8-sig'), re.M)
        if match:
            metas[path.relative_to(ROOT).as_posix()] = match[1]
    duplicates = [guid for guid, count in Counter(metas.values()).items() if count > 1]
    assert not duplicates, f'Duplicate GUIDs: {duplicates}'
    moved_assets = 0
    for item in migration['files']:
        assert (ROOT/item['new']).is_file(), f'Missing relocated asset: {item["new"]}'
        assert not (ROOT/item['old']).exists(), f'Old path remains: {item["old"]}'
        if 'guid' in item:
            assert metas[item['new']+'.meta'] == item['guid'], f'Identity changed: {item["new"]}'
            assert resource_key(item['old']) == resource_key(item['new']), f'Resource name changed: {item["new"]}'
            moved_assets += 1
    allowed = {'ProjectVector', 'ThirdParty', 'Plugins', 'TextMesh Pro'}
    assert {path.name for path in (ROOT/'Assets').iterdir() if path.is_dir()} <= allowed, 'Unclassified Assets root folder'
    assert not [path for path in (ROOT/'Assets').iterdir() if path.is_file() and path.suffix != '.meta'], 'Loose root asset'
    resources = {path.relative_to(ROOT).as_posix(): resource_key(path.relative_to(ROOT).as_posix())
                 for path in (ROOT/'Assets').rglob('*')
                 if path.is_file() and path.suffix != '.meta' and '/Resources/' in path.as_posix()}
    old_counts = Counter(item['resourceKey'] for item in migration['files'] if 'resourceKey' in item)
    for key, count in Counter(resources.values()).items():
        assert count <= max(1, old_counts[key]), f'New Resources key collision: {key}'
    build = (ROOT/'ProjectSettings/EditorBuildSettings.asset').read_text(encoding='utf-8')
    entries = re.findall(r'- enabled: (\d)\s+path: (.+)\s+guid: (\w+)', build)
    for _, path, guid in entries:
        assert (ROOT/path).is_file() and metas[path+'.meta'] == guid, f'Build scene mismatch: {path}'
    if baseline:
        before = json.loads(Path(baseline).read_text(encoding='utf-8'))
        assert set(before['guids'].values()) <= set(metas.values()), 'Original GUID lost'
        assert Counter(before['resources'].values()) == Counter(resources.values()), 'Resources catalog changed'
        old_entries = re.findall(r'- enabled: (\d)\s+path: (.+)\s+guid: (\w+)', before['buildSettings'])
        # A disabled legacy scene had a stale Build Settings GUID before this
        # migration. Identity comes from its original .meta, not that stale entry.
        assert [(flag, before['guids'].get(path+'.meta', guid)) for flag, path, guid in old_entries] == [(flag, guid) for flag, _, guid in entries], 'Build order or enabled flags changed'
        print(f'BASELINE_PASS: {len(before["guids"])} original GUIDs, {len(resources)} resource keys, build order/flags preserved')
    print(f'ASSET_LAYOUT_PASS: {moved_assets} relocated assets, {len(metas)} unique GUIDs, {len(entries)} build entries')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--baseline', type=Path)
    verify(parser.parse_args().baseline)
