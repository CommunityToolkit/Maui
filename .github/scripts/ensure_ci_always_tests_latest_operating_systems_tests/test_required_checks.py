from __future__ import annotations

import json
import unittest

from ensure_ci_always_tests_latest_operating_systems.data_types import SafeChange
from ensure_ci_always_tests_latest_operating_systems.errors import GitHubApiError
from ensure_ci_always_tests_latest_operating_systems.required_checks import read_branch_rules, renamed_required_checks

from .helpers import FakeGitHub, fixture

RULES_PATH = '/repos/CommunityToolkit/Maui/rules/branches/main'
RULES = json.loads(fixture('github/rules-branches-main.json').decode('utf-8'))


def fake_github(answer) -> FakeGitHub:
    github = FakeGitHub({'GET ' + RULES_PATH: answer})
    github.repo = 'CommunityToolkit/Maui'
    return github


def change(old, new) -> SafeChange:
    return SafeChange(family='xcode', kind='set-xcode-version', file='f', line=1, old=old, new=new,
                      inserted_lines=[], pin='p', description='d', source='s')


class ReadBranchRulesTests(unittest.TestCase):
    def test_contexts_and_pull_request_rule_from_the_live_fixture(self):
        github = fake_github(RULES)
        result = read_branch_rules(github, 'main')
        self.assertEqual(result['status'], 'ok')
        self.assertEqual(result['base'], 'main')
        self.assertEqual(len(result['required_checks']), 18)
        self.assertIn('Build Library (xcode-27)', result['required_checks'])
        self.assertIn('Device Tests / Device Tests (iOS 26.5 Simulator)', result['required_checks'])
        self.assertTrue(result['require_last_push_approval'])
        self.assertEqual(result['required_approving_review_count'], 1)
        self.assertEqual(github.calls[0].method, 'GET')
        self.assertEqual(github.write_calls(), [])

    def test_the_result_is_plain_json(self):
        result = read_branch_rules(fake_github(RULES), 'main')
        self.assertEqual(json.loads(json.dumps(result)), result)

    def test_a_failure_is_an_error_result(self):
        result = read_branch_rules(fake_github(GitHubApiError('GET', RULES_PATH, 403, 'no access')), 'main')
        self.assertEqual(result['status'], 'error')
        self.assertIn('403', result['error'])
        self.assertEqual(result['required_checks'], [])

    def test_no_pull_request_rule_means_no_last_push_approval(self):
        result = read_branch_rules(fake_github([{'type': 'deletion'}]), 'main')
        self.assertEqual(result['status'], 'ok')
        self.assertFalse(result['require_last_push_approval'])
        self.assertEqual(result['required_approving_review_count'], 0)

    def test_two_rulesets_are_combined(self):
        rules = [
            {'type': 'required_status_checks', 'parameters': {'required_status_checks': [{'context': 'A'}, {'context': 'B'}]}},
            {'type': 'required_status_checks', 'parameters': {'required_status_checks': [{'context': 'B'}, {'context': 'C'}]}},
            {'type': 'pull_request', 'parameters': {'require_last_push_approval': False, 'required_approving_review_count': 1}},
            {'type': 'pull_request', 'parameters': {'require_last_push_approval': True, 'required_approving_review_count': 2}},
        ]
        result = read_branch_rules(fake_github(rules), 'main')
        self.assertEqual(result['required_checks'], ['A', 'B', 'C'])
        self.assertTrue(result['require_last_push_approval'])
        self.assertEqual(result['required_approving_review_count'], 2)

    def test_a_branch_name_with_a_slash_is_encoded(self):
        github = FakeGitHub({'GET /repos/CommunityToolkit/Maui/rules/branches/release%2F1': []})
        github.repo = 'CommunityToolkit/Maui'
        self.assertEqual(read_branch_rules(github, 'release/1')['status'], 'ok')


class RenamedRequiredChecksTests(unittest.TestCase):
    CONTEXTS = [c['context'] for r in RULES if r['type'] == 'required_status_checks'
                for c in r['parameters']['required_status_checks']]

    def test_xcode_27_is_found_in_both_xcode_checks(self):
        self.assertEqual(renamed_required_checks(change('xcode-27', 'xcode-28'), self.CONTEXTS), [
            ('Build Sample App using Latest .NET SDK (xcode-27)', 'Build Sample App using Latest .NET SDK (xcode-28)'),
            ('Build Library (xcode-27)', 'Build Library (xcode-28)')])

    def test_the_ios_version_in_a_device_test_check(self):
        self.assertEqual(renamed_required_checks(change('26.5', '26.6'), self.CONTEXTS),
                         [('Device Tests / Device Tests (iOS 26.5 Simulator)', 'Device Tests / Device Tests (iOS 26.6 Simulator)')])

    def test_android_level_in_a_check_name(self):
        self.assertEqual(renamed_required_checks(change('36', '37'), self.CONTEXTS),
                         [('Device Tests / Device Tests (Android API 36)', 'Device Tests / Device Tests (Android API 37)')])

    def test_no_check_holds_the_pinned_xcode_value_today(self):
        self.assertEqual(renamed_required_checks(change('27.2', '27.3'), self.CONTEXTS), [])

    def test_27_2_does_not_match_inside_27_20(self):
        self.assertEqual(renamed_required_checks(change('27.2', '27.3'), ['Build 27.20 (x)']), [])

    def test_a_longer_number_or_name_before_the_value_does_not_match(self):
        self.assertEqual(renamed_required_checks(change('27.2', '27.3'), ['Build 127.2', 'Build v27.2', 'Build 27.2.1']), [])

    def test_the_value_at_the_ends_or_before_punctuation_matches(self):
        self.assertEqual(renamed_required_checks(change('27.2', '27.3'), ['27.2', 'Build 27.2)', '(27.2 x)']),
                         [('27.2', '27.3'), ('Build 27.2)', 'Build 27.3)'), ('(27.2 x)', '(27.3 x)')])

    def test_a_sentence_ending_dot_after_the_value_blocks_the_match_like_a_version_dot(self):
        self.assertEqual(renamed_required_checks(change('27.2', '27.3'), ['Build 27.2.']), [])

    def test_every_occurrence_in_one_context_is_renamed(self):
        self.assertEqual(renamed_required_checks(change('a1', 'a2'), ['x a1 y a1']), [('x a1 y a1', 'x a2 y a2')])

    def test_replacement_text_with_backslashes_is_literal(self):
        self.assertEqual(renamed_required_checks(change('a1', r'\1'), ['x a1']), [('x a1', r'x \1')])

    def test_turkish_dotted_capitals_neither_match_nor_break(self):
        self.assertEqual(renamed_required_checks(change('xcode-27', 'xcode-28'), ['BİLD (XCODE-27)', 'Build (xcode-27)']),
                         [('Build (xcode-27)', 'Build (xcode-28)')])
        self.assertEqual(renamed_required_checks(change('ı-1', 'ı-2'), ['A ı-1']), [('A ı-1', 'A ı-2')])

    def test_an_insertion_or_an_unchanged_value_renames_nothing(self):
        self.assertEqual(renamed_required_checks(change(None, '27.3'), self.CONTEXTS), [])
        self.assertEqual(renamed_required_checks(change('27.2', None), self.CONTEXTS), [])
        self.assertEqual(renamed_required_checks(change('27.2', '27.2'), ['27.2']), [])
        self.assertEqual(renamed_required_checks(change('', '27.2'), ['27.2']), [])


if __name__ == '__main__':
    unittest.main()
