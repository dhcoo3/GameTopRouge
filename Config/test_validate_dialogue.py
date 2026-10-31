"""Regression tests for dialogue source validation; no workbook mutations."""
import copy
import unittest
from unittest.mock import patch
import validate_dialogue as validator


class DialogueValidationTests(unittest.TestCase):
    """Exercise malformed references and graph failure cases against sample data."""
    def setUp(self):
        names = ['Dialogue/Dialogue.xlsx', 'Map/#Npc.xlsx', 'Dialogue/#DialogueNode.xlsx', 'Dialogue/#DialogueOption.xlsx', '#Localization.xlsx']
        self.data = {name.split('/')[-1]: validator.records(validator.ROOT / name) for name in names}

    def validate(self):
        with patch.object(validator, 'records', side_effect=lambda path: copy.deepcopy(self.data[path.name])):
            return validator.validate()

    def test_sample(self):
        self.assertIn('10 nodes', self.validate())

    def test_missing_entry(self):
        self.data['Dialogue.xlsx']['1']['entryNodeId'] = '9999'
        with self.assertRaisesRegex(ValueError, 'invalid entry'): self.validate()

    def test_missing_npc(self):
        self.data['#DialogueNode.xlsx']['101']['npcId'] = '9999'
        with self.assertRaisesRegex(ValueError, 'unknown NPC'): self.validate()

    def test_narration_needs_no_npc(self):
        self.data['#DialogueNode.xlsx']['101']['npcId'] = '0'
        self.assertIn('10 nodes', self.validate())

    def test_npc_dialogue_entry(self):
        self.assertEqual(self.data['#Npc.xlsx']['1500000']['dialogueId'], '1')
        self.assertEqual(self.data['#DialogueNode.xlsx']['101']['npcId'], '1500000')

    def test_invalid_npc_dialogue_entry(self):
        self.data['#Npc.xlsx']['1500000']['dialogueId'] = '9999'
        with self.assertRaisesRegex(ValueError, 'invalid dialogue entry'): self.validate()

    def test_cross_dialogue_jump(self):
        self.data['Dialogue.xlsx']['2'] = {'id': '2', 'entryNodeId': '199'}
        self.data['#DialogueNode.xlsx']['199']['dialogueId'] = '2'
        with self.assertRaisesRegex(ValueError, 'cross-dialogue'): self.validate()

    def test_duplicate_option_order(self):
        self.data['#DialogueOption.xlsx']['2']['order'] = '1'
        with self.assertRaisesRegex(ValueError, 'duplicate order'): self.validate()

    def test_four_options_allowed(self):
        self.data['#DialogueOption.xlsx']['4'] = dict(self.data['#DialogueOption.xlsx']['3'], id='4', order='4')
        self.assertIn('4 options', self.validate())

    def test_five_options_rejected(self):
        for i in (4, 5): self.data['#DialogueOption.xlsx'][str(i)] = dict(self.data['#DialogueOption.xlsx']['3'], id=str(i), order=str(i))
        with self.assertRaisesRegex(ValueError, '1..4'): self.validate()

    def test_no_end_cycle(self):
        self.data['#DialogueNode.xlsx']['130']['nextNodeId'] = '130'
        with self.assertRaisesRegex(ValueError, 'without reachable end'): self.validate()

    def test_missing_localization(self):
        del self.data['#Localization.xlsx']['Dialogue.Guard.Opening']
        with self.assertRaisesRegex(ValueError, 'Missing localization'): self.validate()

    def test_orphan_option(self):
        self.data['#DialogueOption.xlsx']['1']['nodeId'] = '101'
        with self.assertRaisesRegex(ValueError, 'Orphan option'): self.validate()


if __name__ == '__main__': unittest.main()
