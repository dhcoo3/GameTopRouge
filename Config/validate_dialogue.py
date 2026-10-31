"""Validate dialogue Excel sources using only Python's standard library."""
from pathlib import Path
import sys
import zipfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent / 'DataTables' / 'Datas'
NS = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}


def rows(path):
    """Read the first worksheet, including shared and inline string cells."""
    with zipfile.ZipFile(path) as archive:
        strings = []
        if 'xl/sharedStrings.xml' in archive.namelist():
            strings = [''.join(x.itertext()) for x in ET.fromstring(archive.read('xl/sharedStrings.xml')).findall('s:si', NS)]
        result = []
        for row in ET.fromstring(archive.read('xl/worksheets/sheet1.xml')).findall('s:sheetData/s:row', NS):
            values = {}
            for cell in row.findall('s:c', NS):
                letters = ''.join(c for c in cell.attrib['r'] if c.isalpha())
                col = 0
                for c in letters:
                    col = col * 26 + ord(c) - ord('A') + 1
                value = cell.find('s:v', NS)
                value = value.text if value is not None else ''
                if cell.get('t') == 's': value = strings[int(value)]
                elif cell.get('t') == 'inlineStr': value = ''.join(cell.find('s:is', NS).itertext())
                values[col - 1] = value
            result.append([values.get(i, '') for i in range(max(values, default=0) + 1)])
        return result


def records(path):
    """Map Luban field names to source values and reject duplicate identifiers."""
    source = rows(path)
    fields = source[0][1:]
    result = {}
    for row in source[4:]:
        if len(row) < 2 or not row[1] or row[0].startswith('##'): continue
        record = dict(zip(fields, row[1:] + [''] * len(fields)))
        if record['id'] in result: raise ValueError(f'{path.name}: duplicate id {record["id"]}')
        result[record['id']] = record
    return result


def validate(root=ROOT):
    """Check references, localization, branches and terminal reachability."""
    folder = root / 'Dialogue'
    dialogues = records(folder / 'Dialogue.xlsx')
    npcs = records(root / 'Map' / '#Npc.xlsx')
    nodes = records(folder / '#DialogueNode.xlsx')
    options = records(folder / '#DialogueOption.xlsx')
    localization = records(root / '#Localization.xlsx')
    edges = {}

    def require(condition, message):
        if not condition: raise ValueError(message)

    def text(key):
        require(key in localization, f'Missing localization: {key}')
        require(all(localization[key].get(lang) for lang in ('ChineseSimplified', 'ChineseTraditional', 'English')), f'Incomplete localization: {key}')

    for npc in npcs.values():
        text(npc['name'])
        entry = npc.get('dialogueId', '0')
        require(entry in ('', '0') or entry in dialogues, f'NPC {npc["id"]}: invalid dialogue entry {entry}')
    for ident, node in nodes.items():
        kind = node['type']
        require(node['dialogueId'] in dialogues, f'Node {ident}: unknown dialogue')
        require(node['npcId'] in ('0', '') or node['npcId'] in npcs, f'Node {ident}: unknown NPC')
        require(kind in ('Line', 'Choice', 'Condition', 'SetFlag', 'End'), f'Node {ident}: invalid type')
        targets = []
        if kind in ('Line', 'Choice'): text(node['textKey'])
        if kind in ('Condition', 'SetFlag'): require(bool(node['flagKey']), f'Node {ident}: missing flag')
        if kind in ('Line', 'SetFlag'): targets = [node['nextNodeId']]
        elif kind == 'Condition': targets = [node['trueNodeId'], node['falseNodeId']]
        elif kind == 'Choice':
            choices = [x for x in options.values() if x['nodeId'] == ident]
            require(1 <= len(choices) <= 4, f'Node {ident}: must have 1..4 options')
            require(len({int(x['order']) for x in choices}) == len(choices), f'Node {ident}: duplicate order')
            targets = [x['targetNodeId'] for x in choices]
        for target in targets:
            require(target in nodes and nodes[target]['dialogueId'] == node['dialogueId'], f'Node {ident}: invalid/cross-dialogue target {target}')
        edges[ident] = targets
    for option in options.values():
        require(option['nodeId'] in nodes and nodes[option['nodeId']]['type'] == 'Choice', f'Orphan option {option["id"]}')
        text(option['textKey'])
    for ident, dialogue in dialogues.items():
        entry = dialogue['entryNodeId']
        require(entry in nodes and nodes[entry]['dialogueId'] == ident, f'Dialogue {ident}: invalid entry')
        can_end = {i for i, n in nodes.items() if n['dialogueId'] == ident and n['type'] == 'End'}
        while True:
            expanded = can_end | {i for i, n in nodes.items() if n['dialogueId'] == ident and any(t in can_end for t in edges[i])}
            if expanded == can_end: break
            can_end = expanded
        require(all(i in can_end for i, n in nodes.items() if n['dialogueId'] == ident), f'Dialogue {ident}: node without reachable end')
    return f'Dialogue validation passed: {len(dialogues)} dialogues, {len(nodes)} nodes, {len(options)} options.'


if __name__ == '__main__':
    try: print(validate())
    except (ValueError, KeyError) as error:
        print(f'Dialogue validation failed: {error}', file=sys.stderr)
        sys.exit(1)
