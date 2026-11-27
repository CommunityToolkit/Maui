from __future__ import annotations

import json
import unittest

from ensure_ci_always_tests_latest_operating_systems import markers
from ensure_ci_always_tests_latest_operating_systems.data_types import SafeChange


class ConstantTests(unittest.TestCase):
    def test_names_match_the_decisions(self) -> None:
        self.assertEqual(markers.WORKFLOW_FILE, 'ensure-ci-always-tests-latest-operating-systems.yml')
        self.assertEqual(markers.PULL_REQUEST_MARKER, '<!-- ensure-ci-always-tests-latest-operating-systems:pr -->')
        self.assertEqual(markers.ISSUE_MARKER, '<!-- ensure-ci-always-tests-latest-operating-systems:issue -->')
        self.assertEqual(markers.CLOSED_BY_BOT_MARKER, '<!-- ensure-ci-always-tests-latest-operating-systems:closed-by-bot -->')
        self.assertEqual(markers.COMMIT_TRAILER, 'Generated-by: ensure-ci-always-tests-latest-operating-systems.yml')
        self.assertEqual(markers.TOKEN_SECRET_NAME, 'ENSURE_CI_ALWAYS_TESTS_LATEST_OPERATING_SYSTEMS_TOKEN')
        self.assertEqual(markers.TOKEN_ENVIRONMENT, 'ensure-ci-always-tests-latest-operating-systems')
        self.assertEqual(markers.ISSUE_TITLE, 'Ensure CI always tests the latest operating systems: a person needs to act')

    def test_labels_are_the_repos_labels(self) -> None:
        self.assertEqual(markers.LABELS[0], 'requirements updated')
        self.assertEqual(markers.LABELS[1], '\u267b housekeeping')

    def test_source_stays_ascii(self) -> None:
        with open(markers.__file__, encoding='utf-8') as source:
            source.read().encode('ascii')

    def test_pin_files(self) -> None:
        self.assertEqual(len(markers.PIN_FILES), 3)
        self.assertTrue(all(path.startswith('.github/workflows/') for path in markers.PIN_FILES))


class ItemMarkerTests(unittest.TestCase):
    def test_round_trip(self) -> None:
        keys = ['ubuntu-label:ubuntu-26.04', 'source:pinned-versions', 'xcode-label:xcode-27:xcode-28', 'a+b_c']
        body = '\n'.join(markers.item_marker(key) + '\ntext' for key in keys)
        self.assertEqual(markers.read_item_keys(body), keys)

    def test_marker_text(self) -> None:
        self.assertEqual(markers.item_marker('x:y'), '<!-- ensure-ci-always-tests-latest-operating-systems:item=x:y -->')

    def test_rejects_invalid_keys(self) -> None:
        for key in ['a--b', 'Upper', 'has space', '', '-leading', 'a>b', 'tr\u0130', 'a\n']:
            with self.subTest(repr(key)):
                with self.assertRaises(ValueError):
                    markers.item_marker(key)

    def test_reading_skips_invalid_and_missing(self) -> None:
        body = '<!-- ensure-ci-always-tests-latest-operating-systems:item=a--b -->'
        self.assertEqual(markers.read_item_keys(body), [])
        self.assertEqual(markers.read_item_keys(None), [])
        self.assertEqual(markers.read_item_keys(''), [])


class ChangeListTests(unittest.TestCase):
    CHANGES = [
        {'family': 'android', 'kind': 'replace-api-level', 'file': '.github/workflows/device-tests.yml',
         'pin': 'android-leg:google_apis:37', 'old': "'37.0'", 'new': "'37.1'", 'inserted_lines': []},
        {'family': 'android', 'kind': 'insert-android-leg', 'file': '.github/workflows/device-tests.yml',
         'pin': 'android-leg:google_apis_ps16k:38', 'old': '', 'new': "'38.0'",
         'inserted_lines': ["-   api-level: '38.0'", "    target: google_apis_ps16k  # a -- b --> c", '- > --> ->']},
    ]

    def test_round_trip_with_hyphens_and_angle_brackets(self) -> None:
        body = 'text\n' + markers.change_list_marker(self.CHANGES) + '\nmore'
        self.assertEqual(markers.read_change_list(body), self.CHANGES)

    def test_marker_cannot_end_the_comment_early(self) -> None:
        marker = markers.change_list_marker(self.CHANGES)
        inner = marker[len('<!-- '):-len(' -->')]
        self.assertNotIn('-->', inner)
        self.assertNotIn('>', inner)
        self.assertNotIn('-', inner.split('changes=', 1)[1])

    def test_json_is_compact_sorted_and_ascii(self) -> None:
        marker = markers.change_list_marker([{'family': 'f', 'kind': 'k', 'file': 'x', 'pin': 'p', 'old': 'o\u00e9',
                                              'new': 'n', 'inserted_lines': []}])
        self.assertTrue(marker.isascii())
        payload = marker.split('changes=', 1)[1][:-len(' -->')]
        self.assertTrue(payload.startswith('[{"family":"f","file":"x","inserted_lines":[],'))

    def test_accepts_safe_changes(self) -> None:
        change = SafeChange(family='android', kind='replace-api-level', file='f', line=3, old='a', new='b',
                            inserted_lines=[], pin='android-leg:google_apis:37', description='d', source='s')
        read_back = markers.read_change_list(markers.change_list_marker([change]))
        self.assertEqual(read_back, [{'family': 'android', 'kind': 'replace-api-level', 'file': 'f',
                                      'pin': 'android-leg:google_apis:37', 'old': 'a', 'new': 'b',
                                      'inserted_lines': []}])

    def test_no_line_numbers_in_the_list(self) -> None:
        change = SafeChange('a', 'k', 'f', 99, 'o', 'n', [], 'p', 'd', 's')
        self.assertNotIn('99', markers.change_list_marker([change]))

    def test_empty_list_round_trips(self) -> None:
        self.assertEqual(markers.read_change_list(markers.change_list_marker([])), [])

    def test_a_list_that_does_not_parse_is_ignored(self) -> None:
        prefix = '<!-- ensure-ci-always-tests-latest-operating-systems:changes='
        for payload in ['{not json', '{"a":1}', '[1,2]', '"text"', '']:
            with self.subTest(payload):
                self.assertIsNone(markers.read_change_list(prefix + payload + ' -->'))

    def test_a_list_whose_entries_have_the_wrong_types_is_ignored(self) -> None:
        good = {'family': 'android', 'kind': 'replace-api-level', 'file': 'f', 'pin': 'p', 'old': 'a', 'new': 'b',
                'inserted_lines': []}
        wrong = [dict(good, kind=['replace-api-level']), dict(good, old=5), dict(good, inserted_lines=5),
                 dict(good, inserted_lines='abc'), dict(good, inserted_lines=[1]), dict(good, new={'a': 1})]
        for entry in wrong:
            with self.subTest(entry=entry):
                body = '<!-- ensure-ci-always-tests-latest-operating-systems:changes=%s -->' % json.dumps([good, entry])
                self.assertIsNone(markers.read_change_list(body))

    def test_missing_marker_gives_none(self) -> None:
        self.assertIsNone(markers.read_change_list('no marker here'))
        self.assertIsNone(markers.read_change_list(None))

    def test_non_string_values_are_refused(self) -> None:
        with self.assertRaises(ValueError):
            markers.change_list_marker([{'family': 'f', 'kind': 'k', 'file': 'x', 'pin': 'p', 'old': -1, 'new': 'n',
                                         'inserted_lines': []}])

    def test_parses_with_plain_json(self) -> None:
        payload = markers.change_list_marker(self.CHANGES).split('changes=', 1)[1][:-len(' -->')]
        self.assertEqual(json.loads(payload), self.CHANGES)


class HasMarkerTests(unittest.TestCase):
    def test_has_marker(self) -> None:
        body = 'x ' + markers.PULL_REQUEST_MARKER
        self.assertTrue(markers.has_marker(body, markers.PULL_REQUEST_MARKER))
        self.assertFalse(markers.has_marker(body, markers.ISSUE_MARKER))
        self.assertFalse(markers.has_marker(None, markers.ISSUE_MARKER))
        self.assertFalse(markers.has_marker('', markers.ISSUE_MARKER))


if __name__ == '__main__':
    unittest.main()
