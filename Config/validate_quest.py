"""Validate Luban quest sources and fixed-map-object bindings."""
from pathlib import Path
import sys
import zipfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent / 'DataTables' / 'Datas'
NS = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}


def rows(path):
    with zipfile.ZipFile(path) as archive:
        shared = []
        if 'xl/sharedStrings.xml' in archive.namelist():
            shared = [''.join(item.itertext()) for item in ET.fromstring(archive.read('xl/sharedStrings.xml')).findall('s:si', NS)]
        result = []
        for row in ET.fromstring(archive.read('xl/worksheets/sheet1.xml')).findall('s:sheetData/s:row', NS):
            values = {}
            for cell in row.findall('s:c', NS):
                letters = ''.join(char for char in cell.attrib['r'] if char.isalpha())
                column = 0
                for char in letters:
                    column = column * 26 + ord(char) - ord('A') + 1
                value = cell.find('s:v', NS)
                value = value.text if value is not None else ''
                if cell.get('t') == 's':
                    value = shared[int(value)]
                elif cell.get('t') == 'inlineStr':
                    value = ''.join(cell.find('s:is', NS).itertext())
                values[column - 1] = value
            result.append([values.get(index, '') for index in range(max(values, default=0) + 1)])
        return result


def records(path):
    source = rows(path)
    fields = source[0][1:]
    result = {}
    for row in source[4:]:
        if len(row) < 2 or not row[1] or row[0].startswith('##'):
            continue
        record = dict(zip(fields, row[1:] + [''] * len(fields)))
        ident = str(record['id'])
        if ident in result:
            raise ValueError(f'{path}: duplicate id {ident}')
        result[ident] = record
    return result


def integer(value, label, allow_zero=True):
    try:
        number = int(value or 0)
    except (TypeError, ValueError):
        raise ValueError(f'{label}: expected integer, got {value!r}')
    if number < 0 or (not allow_zero and number == 0):
        raise ValueError(f'{label}: invalid value {number}')
    return number


def validate(root=ROOT):
    def require(condition, message):
        if not condition:
            raise ValueError(message)

    quests = records(root / 'Quest' / '#QuestDefinition.xlsx')
    groups = records(root / 'Quest' / '#QuestObjectiveGroup.xlsx')
    objectives = records(root / 'Quest' / '#QuestObjective.xlsx')
    rewards = records(root / 'Quest' / '#QuestReward.xlsx')
    maps = records(root / 'Map' / '#MapData.xlsx')
    map_objects = records(root / 'Map' / '#MapObject.xlsx')
    npcs = records(root / 'Map' / '#Npc.xlsx')
    dialogues = records(root / 'Dialogue' / 'Dialogue.xlsx')
    dialogue_nodes = records(root / 'Dialogue' / '#DialogueNode.xlsx')
    enemies = records(root / 'Entity' / '#Monster.xlsx')
    items = records(root / 'item' / '#Item.xlsx')
    localization = records(root / '#Localization.xlsx')

    def text(key, label):
        require(bool(key) and key in localization, f'{label}: missing localization {key!r}')
        require(all(localization[key].get(language) for language in ('ChineseSimplified', 'ChineseTraditional', 'English')),
                f'{label}: incomplete localization {key}')

    map_placements = {}
    for map_id, map_row in maps.items():
        instances = set()
        for placement in filter(None, map_row.get('Objects', '').split(';')):
            fields = placement.split(',')
            require(len(fields) in (10, 11), f'Map {map_id}: fixed object must contain 10 or 11 fields')
            instance_id = integer(fields[0], f'Map {map_id} instance id', allow_zero=False)
            object_id = integer(fields[1], f'Map {map_id} object id', allow_zero=False)
            objective_id = integer(fields[10], f'Map {map_id} objective id') if len(fields) == 11 else 0
            require(instance_id not in instances, f'Map {map_id}: duplicate instance {instance_id}')
            instances.add(instance_id)
            definition = map_objects.get(str(object_id))
            require(definition is not None, f'Map {map_id} instance {instance_id}: unknown object {object_id}')
            kind = definition['type']
            require(kind in ('TaskMarker', 'SceneProp', '1', '2'), f'Object {object_id}: invalid type {kind}')
            is_task_marker = kind in ('TaskMarker', '1')
            require((objective_id > 0) if is_task_marker else objective_id == 0,
                    f'Map {map_id} instance {instance_id}: TaskMarker needs an objective, SceneProp must not bind one')
            map_placements[(map_id, str(instance_id))] = (str(object_id), objective_id, is_task_marker)

    flag_keys = {node['flagKey'] for node in dialogue_nodes.values() if node['type'] == 'SetFlag' and node.get('flagKey')}
    group_by_id = groups
    objective_by_id = objectives
    groups_by_quest = {}
    objectives_by_group = {}
    for group_id, group in group_by_id.items():
        quest_id = str(group['questId'])
        require(quest_id in quests, f'Group {group_id}: unknown quest {quest_id}')
        order = integer(group['order'], f'Group {group_id} order', allow_zero=False)
        required = integer(group['requiredObjectiveCount'], f'Group {group_id} required count', allow_zero=False)
        groups_by_quest.setdefault(quest_id, []).append((order, group_id, required))
        objectives_by_group[group_id] = []

    objectives_by_id = set(objectives)
    for objective_id, objective in objectives.items():
        group_id = str(objective['groupId'])
        require(group_id in group_by_id, f'Objective {objective_id}: unknown group {group_id}')
        objectives_by_group[group_id].append(objective)
        kind = objective['type']
        require(kind in ('InspectObject', 'CompleteDialogue', 'KillEnemy', 'CollectItem'),
                f'Objective {objective_id}: invalid type {kind}')
        target_count = integer(objective['targetCount'], f'Objective {objective_id} target count', allow_zero=False)
        text(objective['descriptionKey'], f'Objective {objective_id}')
        if objective.get('completionTextKey'):
            text(objective['completionTextKey'], f'Objective {objective_id} completion text')
        map_id = str(integer(objective.get('sourceMapId'), f'Objective {objective_id} source map'))
        if kind in ('InspectObject', 'CollectItem'):
            instance = str(integer(objective.get('sourceInstanceId'), f'Objective {objective_id} source instance', allow_zero=False))
            placement = map_placements.get((map_id, instance))
            require(placement is not None and placement[1] == int(objective_id),
                    f'Objective {objective_id}: map placement {map_id}/{instance} does not bind back to this objective')
            require(placement[2], f'Objective {objective_id}: source object is not a TaskMarker')
            require(map_id in maps, f'Objective {objective_id}: unknown source map {map_id}')
            if kind == 'InspectObject' and integer(objective.get('dialogueId'), f'Objective {objective_id} feedback dialogue') > 0:
                feedback_id = str(objective['dialogueId'])
                require(feedback_id in dialogues and any(str(node.get('dialogueId')) == feedback_id
                        for node in dialogue_nodes.values()),
                        f'Objective {objective_id}: feedback dialogue {feedback_id} is missing')
        if kind == 'CompleteDialogue':
            npc_id = str(integer(objective.get('sourceNpcId'), f'Objective {objective_id} source NPC', allow_zero=False))
            dialogue_id = str(integer(objective.get('dialogueId'), f'Objective {objective_id} dialogue', allow_zero=False))
            require(npc_id in npcs and dialogue_id in dialogues, f'Objective {objective_id}: invalid NPC/dialogue source')
            require(str(npcs[npc_id].get('dialogueId', '')) == dialogue_id,
                    f'Objective {objective_id}: dialogue does not match NPC {npc_id}')
            require(map_id in maps, f'Objective {objective_id}: unknown source map {map_id}')
            map_npcs = {entry.split(',')[0] for entry in filter(None, maps[map_id].get('Npc', '').split(';'))}
            require(npc_id in map_npcs, f'Objective {objective_id}: NPC {npc_id} is not placed on map {map_id}')
        if kind == 'KillEnemy':
            enemy_id = str(integer(objective.get('enemyId'), f'Objective {objective_id} enemy', allow_zero=False))
            require(enemy_id in enemies, f'Objective {objective_id}: unknown enemy {enemy_id}')
        if kind == 'CollectItem':
            item_id = str(integer(objective.get('itemId'), f'Objective {objective_id} item', allow_zero=False))
            require(item_id in items, f'Objective {objective_id}: unknown item {item_id}')
            integer(objective.get('collectCount'), f'Objective {objective_id} collect count', allow_zero=False)

    prerequisite_edges = {}
    for quest_id, quest in quests.items():
        require(quest.get('category') in ('Main', 'Side', '1', '2'), f'Quest {quest_id}: invalid category')
        require(quest.get('activation') in ('AutoOnMapEnter', 'AutoAfterPrerequisites', 'AcceptFromNpc', '1', '2', '3'),
                f'Quest {quest_id}: invalid activation')
        text(quest.get('titleKey'), f'Quest {quest_id} title')
        text(quest.get('descriptionKey'), f'Quest {quest_id} description')
        if quest.get('briefingTextKey'):
            text(quest['briefingTextKey'], f'Quest {quest_id} briefing')
        prerequisite = str(integer(quest.get('prerequisiteQuestId'), f'Quest {quest_id} prerequisite'))
        if prerequisite:
            require(prerequisite == '0' or prerequisite in quests, f'Quest {quest_id}: unknown prerequisite {prerequisite}')
        prerequisite_edges[quest_id] = [] if prerequisite in ('', '0') else [prerequisite]
        flag = quest.get('prerequisiteStoryFlagKey', '')
        if flag:
            require(flag in flag_keys, f'Quest {quest_id}: flag {flag} is not set by any dialogue node')
        activation = quest['activation']
        if activation in ('AutoOnMapEnter', '1'):
            require(str(integer(quest.get('startMapId'), f'Quest {quest_id} start map', allow_zero=False)) in maps,
                    f'Quest {quest_id}: unknown start map')
        if activation in ('AcceptFromNpc', '3'):
            require(str(integer(quest.get('acceptNpcId'), f'Quest {quest_id} accept NPC', allow_zero=False)) in npcs,
                    f'Quest {quest_id}: unknown accepting NPC')
        quest_groups = sorted(groups_by_quest.get(quest_id, []))
        require(quest_groups, f'Quest {quest_id}: no objective groups')
        require(len({entry[0] for entry in quest_groups}) == len(quest_groups), f'Quest {quest_id}: duplicate group order')
        for _, group_id, required in quest_groups:
            members = objectives_by_group[group_id]
            require(members and required <= len(members), f'Group {group_id}: required objective count exceeds available objectives')

    def visit(node, active, done):
        if node in active:
            raise ValueError(f'Quest prerequisite cycle at {node}')
        if node in done:
            return
        active.add(node)
        for previous in prerequisite_edges[node]:
            visit(previous, active, done)
        active.remove(node)
        done.add(node)
    done = set()
    for quest_id in quests:
        visit(quest_id, set(), done)

    reward_ids = set()
    for reward_id, reward in rewards.items():
        require(str(reward['questId']) in quests, f'Reward {reward_id}: unknown quest')
        item_id = str(integer(reward.get('itemId'), f'Reward {reward_id} item', allow_zero=False))
        count = integer(reward.get('count'), f'Reward {reward_id} count', allow_zero=False)
        require(item_id in items, f'Reward {reward_id}: unknown item {item_id}')
        require(count <= 2**31 - 1, f'Reward {reward_id}: item stack overflows int32')
        reward_ids.add(reward_id)

    for (map_id, instance_id), (object_id, objective_id, is_task_marker) in map_placements.items():
        if is_task_marker:
            require(str(objective_id) in objectives_by_id, f'Map {map_id} instance {instance_id}: unknown objective {objective_id}')
            objective = objectives[str(objective_id)]
            require(str(objective['sourceMapId']) == map_id and str(objective['sourceInstanceId']) == instance_id,
                    f'Map {map_id} instance {instance_id}: objective source does not match placement')
            require(objective['type'] in ('InspectObject', 'CollectItem'),
                    f'Map {map_id} instance {instance_id}: TaskMarker bound to non-map objective')

    return f'Quest validation passed: {len(quests)} quests, {len(groups)} groups, {len(objectives)} objectives, {len(rewards)} rewards.'


if __name__ == '__main__':
    try:
        print(validate())
    except (ValueError, KeyError, IndexError) as error:
        print(f'Quest validation failed: {error}', file=sys.stderr)
        sys.exit(1)
