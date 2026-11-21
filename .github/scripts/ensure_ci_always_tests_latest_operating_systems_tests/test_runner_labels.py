from __future__ import annotations

import re
import unittest

from ensure_ci_always_tests_latest_operating_systems.data_types import (
    AndroidLeg, AppleLabel, IosLeg, PinnedVersions, RunnerImageRow)
from ensure_ci_always_tests_latest_operating_systems.downloads import Downloader
from ensure_ci_always_tests_latest_operating_systems.errors import (
    DownloadError, NotFoundError, RunnerImagesReadmeError, UnreadableSourceError)
from ensure_ci_always_tests_latest_operating_systems.runner_labels import (
    README_URL, ROLLOUT_STATE_URL, check_runner_labels, macos_labels_to_inspect, parse_rollout_state,
    parse_runner_images_readme, read_runner_images)

from .helpers import FakeDownloader, fixture

TODAY_README = fixture('runners/README-0e63d4f9d.md').decode('utf-8')


def ios_leg(runner: str) -> IosLeg:
    return IosLeg(ios_version='26.5', runner=runner, xcode='26.5', install_runtime=False, entry_start=1, entry_end=2)


def pinned_versions(**runner_labels) -> PinnedVersions:
    families = {'ubuntu': ['ubuntu-24.04'], 'windows': ['windows-2025'], 'macos': ['macos-26', 'macos-15'],
                'xcode': ['xcode-27']}
    families.update(runner_labels)
    return PinnedVersions(
        shared_env={}, android_legs=[], coverage_levels=[],
        ios_legs=[ios_leg('macos-26'), ios_leg('macos-15'), ios_leg('macos-15')],
        apple_label=AppleLabel(label='xcode-27', occurrences=[]), runner_labels=families,
        android_include_line=0, ios_include_line=0, android_dash_prefix='', ios_dash_prefix='')


def row(name: str, labels, badges=()) -> RunnerImageRow:
    return RunnerImageRow(name=name, arch='x64', labels=list(labels), badges=list(badges), rollout_state_key=None)


def keys_of(plan):
    return [thing.key for thing in plan.things_to_check]


class ParseReadmeTests(unittest.TestCase):
    def setUp(self):
        self.rows = parse_runner_images_readme(TODAY_README)
        self.by_name = {row.name: row for row in self.rows}

    def test_every_row_of_todays_table_is_read(self):
        self.assertEqual(len(self.rows), 16)
        self.assertEqual(self.rows[0].name, 'Ubuntu 26.04')

    def test_labels_come_from_the_backtick_tokens(self):
        self.assertEqual(self.by_name['Xcode 27'].labels, ['xcode-27', 'xcode-27-xlarge'])
        self.assertEqual(self.by_name['macOS 26 Arm64'].labels, ['macos-latest', 'macos-26', 'macos-26-xlarge'])
        self.assertEqual(self.by_name['macOS 15'].labels, ['macos-15-large', 'macos-15-intel'])

    def test_badges_are_the_words_of_the_badge_images(self):
        self.assertEqual(self.by_name['Xcode 27'].badges, ['preview'])
        self.assertEqual(self.by_name['macOS 14'].badges, ['deprecated'])
        self.assertEqual(self.by_name['macOS 26 Arm64'].badges, [])

    def test_rollout_state_key_comes_from_the_endpoint_badge(self):
        self.assertEqual(self.by_name['Xcode 27'].rollout_state_key, 'xcode-27-arm64')
        self.assertEqual(self.by_name['macOS 26 Arm64'].rollout_state_key, 'macos-26-arm64')
        self.assertEqual(self.by_name['Ubuntu 24.04'].rollout_state_key, 'ubuntu24')
        self.assertEqual(self.by_name['Windows Server 2025'].rollout_state_key, 'win25-vs2026')

    def test_architecture_column_is_optional_data(self):
        self.assertEqual(self.by_name['Xcode 27'].arch, 'arm64')

    def test_the_three_column_format_of_2025_still_parses(self):
        text = fixture('runners/README-e812bbc61.md').decode('utf-8')
        text = '\n'.join(line for line in text.splitlines() if 'macOS 26 Arm64' not in line)
        by_name = {row.name: row for row in parse_runner_images_readme(text)}
        self.assertEqual(by_name['macOS 15 Arm64'].labels, ['macos-latest', 'macos-15', 'macos-15-xlarge'])
        self.assertEqual(by_name['Ubuntu 24.04'].labels, ['ubuntu-latest', 'ubuntu-24.04'])
        self.assertEqual(by_name['Windows Server 2019'].badges, ['deprecated'])
        self.assertTrue(all(row.arch == '' and row.rollout_state_key is None for row in by_name.values()))

    def test_the_2025_readme_that_gave_macos_15_to_two_rows_is_an_error(self):
        with self.assertRaises(RunnerImagesReadmeError):
            parse_runner_images_readme(fixture('runners/README-e812bbc61.md').decode('utf-8'))

    def test_columns_are_found_by_header_name_not_position(self):
        text = '| YAML Label | Image |\n|---|---|\n| `foo-1` | Foo |\n'
        rows = parse_runner_images_readme(text)
        self.assertEqual((rows[0].name, rows[0].labels), ('Foo', ['foo-1']))

    def test_rows_stop_at_the_first_line_that_is_not_a_table_row(self):
        text = TODAY_README + '\nnot a row\n| Image | YAML Label |\n| a | `later-1` |\n'
        self.assertEqual(len(parse_runner_images_readme(text)), 16)

    def test_missing_header_is_an_error(self):
        with self.assertRaises(RunnerImagesReadmeError):
            parse_runner_images_readme('## Available Images\n\n| Name | Label |\n|---|---|\n| a | `b` |\n')

    def test_no_rows_is_an_error(self):
        with self.assertRaises(RunnerImagesReadmeError):
            parse_runner_images_readme('| Image | YAML Label |\n|---|---|\n')

    def test_a_row_without_a_label_is_an_error(self):
        with self.assertRaises(RunnerImagesReadmeError):
            parse_runner_images_readme('| Image | YAML Label |\n|---|---|\n| Foo | none |\n')

    def test_a_label_listed_twice_is_an_error(self):
        with self.assertRaises(RunnerImagesReadmeError) as caught:
            parse_runner_images_readme('| Image | YAML Label |\n|---|---|\n| Foo | `a` |\n| Bar | `a` |\n')
        self.assertIn('twice', str(caught.exception))

    def test_a_short_row_is_an_error(self):
        with self.assertRaises(RunnerImagesReadmeError):
            parse_runner_images_readme('| Image | YAML Label |\n|---|---|\n| Foo |\n')


class CheckRunnerLabelsTests(unittest.TestCase):
    def setUp(self):
        self.rows = parse_runner_images_readme(TODAY_README)

    def test_today_gives_exactly_the_newer_ubuntu_item(self):
        plan = check_runner_labels(pinned_versions(), self.rows)
        self.assertEqual(plan.family, 'runner-labels')
        self.assertEqual(keys_of(plan), ['ubuntu-label:ubuntu-26.04'])
        self.assertIn('GA', plan.things_to_check[0].text)
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.notes, [])

    def test_a_windows_preview_row_numbered_higher_is_reported_with_its_badge_word(self):
        rows = self.rows + [row('Windows Server 2028', ['windows-2028'], ['preview'])]
        plan = check_runner_labels(pinned_versions(), rows)
        self.assertIn('windows-label:windows-2028', keys_of(plan))
        thing = [t for t in plan.things_to_check if t.key == 'windows-label:windows-2028'][0]
        self.assertIn('preview', thing.text)

    def test_variants_of_a_pinned_label_are_not_newer_labels(self):
        rows = [row('Windows', ['windows-2025', 'windows-2025-vs2026']), row('Ubuntu', ['ubuntu-24.04', 'ubuntu-24.04-arm'])]
        pinned = pinned_versions(macos=[], xcode=[])
        self.assertEqual(keys_of(check_runner_labels(pinned, rows)), [])

    def test_a_pinned_label_removed_from_the_readme(self):
        rows = [r for r in self.rows if 'macos-15' not in r.labels]
        plan = check_runner_labels(pinned_versions(), rows)
        self.assertIn('runner-label-missing:macos-15', keys_of(plan))

    def test_a_deprecated_badge_on_a_pinned_label(self):
        rows = [row('macOS 15 Arm64', ['macos-15'], ['deprecated']) if 'macos-15' in r.labels else r for r in self.rows]
        plan = check_runner_labels(pinned_versions(), rows)
        self.assertIn('runner-label-deprecated:macos-15', keys_of(plan))
        self.assertNotIn('runner-label-missing:macos-15', keys_of(plan))

    def test_a_deprecated_label_nobody_uses_is_not_reported(self):
        self.assertNotIn('runner-label-deprecated:macos-14', keys_of(check_runner_labels(pinned_versions(), self.rows)))

    def test_a_newer_macos_is_a_note_only(self):
        rows = self.rows + [row('macOS 27 Arm64', ['macos-27'])]
        plan = check_runner_labels(pinned_versions(), rows)
        self.assertEqual(keys_of(plan), ['ubuntu-label:ubuntu-26.04'])
        self.assertEqual(len(plan.notes), 1)
        self.assertIn('macos-27', plan.notes[0])

    def test_a_newer_xcode_is_a_note_that_says_a_person_moves_the_jobs(self):
        rows = self.rows + [row('Xcode 28', ['xcode-28'])]
        plan = check_runner_labels(pinned_versions(), rows)
        self.assertEqual(keys_of(plan), ['ubuntu-label:ubuntu-26.04'])
        self.assertEqual(len(plan.notes), 1)
        self.assertIn('xcode-28', plan.notes[0])
        self.assertIn('A person moves the Apple jobs', plan.notes[0])

    def test_readme_text_appears_only_inside_code_spans(self):
        hostile_name = 'macOS 26 @everyone <script>x</script>'
        hostile_badge = '@octocat see <img src=x onerror=alert(1)> `x`'
        rows = [row(hostile_name, ['macos-15'], ['deprecated']) if 'macos-15' in r.labels else r for r in self.rows]
        rows.append(row('Windows', ['windows-2028'], [hostile_badge]))
        plan = check_runner_labels(pinned_versions(), rows)
        deprecated = [t.text for t in plan.things_to_check if t.key == 'runner-label-deprecated:macos-15'][0]
        newer_windows = [t.text for t in plan.things_to_check if t.key == 'windows-label:windows-2028'][0]
        for text in (deprecated, newer_windows):
            outside_code_spans = re.sub(r'`[^`]*`', '', text)
            for dangerous in ('@everyone', '@octocat', '<script>', '<img'):
                self.assertNotIn(dangerous, outside_code_spans, text)
        self.assertIn('`macOS 26 @everyone <script>x</script>`', deprecated)

    def test_every_key_is_a_valid_item_key(self):
        from ensure_ci_always_tests_latest_operating_systems.markers import is_valid_item_key
        rows = [r for r in self.rows if 'macos-15' not in r.labels] + [row('W', ['windows-2028'])]
        for key in keys_of(check_runner_labels(pinned_versions(), rows)):
            self.assertTrue(is_valid_item_key(key), key)

    def test_arabic_indic_digits_are_not_version_numbers(self):
        rows = self.rows + [row('Odd', ['ubuntu-٢٦.04'])]
        self.assertEqual(keys_of(check_runner_labels(pinned_versions(), rows)), ['ubuntu-label:ubuntu-26.04'])


class LabelsToInspectTests(unittest.TestCase):
    def setUp(self):
        self.rows = parse_runner_images_readme(TODAY_README)

    def test_today(self):
        self.assertEqual(macos_labels_to_inspect(pinned_versions(), self.rows, [27]), ['xcode-27', 'macos-26', 'macos-15'])

    def test_a_required_major_28_is_added_when_the_readme_lists_it(self):
        rows = self.rows + [row('Xcode 28', ['xcode-28'])]
        self.assertEqual(macos_labels_to_inspect(pinned_versions(), rows, [27, 28]),
                         ['xcode-27', 'macos-26', 'macos-15', 'xcode-28'])

    def test_a_required_major_28_is_left_out_when_the_readme_has_no_row(self):
        self.assertEqual(macos_labels_to_inspect(pinned_versions(), self.rows, [28]),
                         ['xcode-27', 'macos-26', 'macos-15'])

    def test_a_label_the_readme_dropped_is_left_out(self):
        rows = [r for r in self.rows if 'macos-15' not in r.labels]
        self.assertEqual(macos_labels_to_inspect(pinned_versions(), rows, [27]), ['xcode-27', 'macos-26'])

    def test_when_the_readme_failed_only_the_repos_own_labels_are_listed(self):
        self.assertEqual(macos_labels_to_inspect(pinned_versions(), None, [27, 28]), ['xcode-27', 'macos-26', 'macos-15'])

    def test_labels_that_are_not_xcode_or_macos_are_filtered_out(self):
        pinned = pinned_versions()
        pinned.ios_legs.append(ios_leg('macos-latest'))
        pinned.ios_legs.append(ios_leg('macos-26-xlarge'))
        pinned.ios_legs.append(ios_leg('ubuntu-24.04'))
        self.assertEqual(macos_labels_to_inspect(pinned, None, []), ['xcode-27', 'macos-26', 'macos-15'])


class RolloutStateTests(unittest.TestCase):
    def test_deployed(self):
        state = parse_rollout_state(b'{"message":"Deployed","label":"20261104.0337"}')
        self.assertEqual((state.deployed_build, state.message, state.deploying_to), ('20261104.0337', 'Deployed', None))

    def test_deploying_to_a_build(self):
        state = parse_rollout_state(fixture('runners/rollout-state-xcode-27-arm64.json'))
        self.assertEqual((state.deployed_build, state.deploying_to), ('20261104.0337', '20261119.0378'))

    def test_a_byte_order_mark_is_tolerated(self):
        self.assertEqual(parse_rollout_state(b'\xef\xbb\xbf{"message":"Deployed","label":"1"}').deployed_build, '1')

    def test_not_json_is_an_unreadable_source(self):
        with self.assertRaises(UnreadableSourceError):
            parse_rollout_state(b'<html>')

    def test_no_message_is_an_unreadable_source(self):
        with self.assertRaises(UnreadableSourceError):
            parse_rollout_state(b'{"label":"1"}')


class ReadRunnerImagesTests(unittest.TestCase):
    def mapping(self, **overrides):
        mapping = {README_URL: fixture('runners/README-0e63d4f9d.md')}
        for key in ('xcode-27-arm64', 'macos-26-arm64', 'macos-15-arm64'):
            mapping[ROLLOUT_STATE_URL.format(key=key)] = fixture('runners/rollout-state-%s.json' % key)
        mapping.update(overrides)
        return mapping

    def test_reads_the_states_of_the_rows_that_hold_the_labels_and_nothing_else(self):
        downloader = FakeDownloader(self.mapping())
        result = read_runner_images(downloader, ['xcode-27', 'macos-26', 'macos-15'])
        self.assertEqual(result['status'], 'ok')
        self.assertEqual(sorted(result['rollout_states']), ['macos-15-arm64', 'macos-26-arm64', 'xcode-27-arm64'])
        self.assertEqual(result['rollout_states']['xcode-27-arm64']['deploying_to'], '20261119.0378')
        self.assertEqual(len(result['rows']), 16)
        self.assertIsNone(result['rollout_states_error'])
        self.assertEqual(downloader.urls_requested()[0], README_URL)
        self.assertEqual(len(downloader.urls_requested()), 4)

    def test_the_result_survives_a_json_round_trip(self):
        import json
        result = read_runner_images(FakeDownloader(self.mapping()), ['xcode-27'])
        self.assertEqual(json.loads(json.dumps(result)), result)
        self.assertEqual(RunnerImageRow.from_json(result['rows'][7]).labels, ['xcode-27', 'xcode-27-xlarge'])

    def test_a_404_state_is_unknown_not_an_error(self):
        url = ROLLOUT_STATE_URL.format(key='xcode-27-arm64')
        downloader = FakeDownloader({README_URL: fixture('runners/README-0e63d4f9d.md'), url: NotFoundError(url)})
        result = read_runner_images(downloader, ['xcode-27'])
        self.assertEqual(result['status'], 'ok')
        self.assertEqual(result['rollout_states'], {'xcode-27-arm64': None})
        self.assertIsNone(result['rollout_states_error'])

    def test_one_failing_state_among_several_is_not_a_source_problem(self):
        broken = ROLLOUT_STATE_URL.format(key='macos-15-arm64')
        mapping = self.mapping()
        mapping[broken] = DownloadError(broken, 503)
        result = read_runner_images(FakeDownloader(mapping), ['xcode-27', 'macos-26', 'macos-15'])
        self.assertIsNone(result['rollout_states']['macos-15-arm64'])
        self.assertIsNone(result['rollout_states_error'])

    def test_every_state_request_failing_is_reported(self):
        mapping = {README_URL: fixture('runners/README-0e63d4f9d.md')}
        for key in ('xcode-27-arm64', 'macos-26-arm64'):
            url = ROLLOUT_STATE_URL.format(key=key)
            mapping[url] = DownloadError(url, 503)
        result = read_runner_images(FakeDownloader(mapping), ['xcode-27', 'macos-26'])
        self.assertEqual(result['status'], 'ok')
        self.assertIn('503', result['rollout_states_error'])

    def test_a_broken_state_body_counts_as_a_failed_request(self):
        mapping = {README_URL: fixture('runners/README-0e63d4f9d.md'),
                   ROLLOUT_STATE_URL.format(key='xcode-27-arm64'): b'<html>'}
        result = read_runner_images(FakeDownloader(mapping), ['xcode-27'])
        self.assertIsNone(result['rollout_states']['xcode-27-arm64'])
        self.assertIsNotNone(result['rollout_states_error'])

    def test_a_readme_problem_is_an_error_result(self):
        result = read_runner_images(FakeDownloader({README_URL: b'# nothing here'}), ['xcode-27'])
        self.assertEqual(result['status'], 'error')
        self.assertIn('Image', result['error'])
        self.assertEqual(result['rows'], [])

    def test_a_readme_that_cannot_be_downloaded_is_an_error_result(self):
        result = read_runner_images(FakeDownloader({README_URL: DownloadError(README_URL, 500)}), ['xcode-27'])
        self.assertEqual(result['status'], 'error')

    def test_no_request_carries_a_token(self):
        downloader = FakeDownloader(self.mapping())
        read_runner_images(downloader, ['xcode-27', 'macos-26', 'macos-15'])
        for call in downloader.calls:
            self.assertEqual({name.lower() for name in call['headers']} & {'authorization', 'x-github-token'}, set())

    def test_the_real_downloader_refuses_credentials(self):
        with self.assertRaises(ValueError):
            Downloader().get(ROLLOUT_STATE_URL.format(key='x'), headers={'Authorization': 'Bearer x'})

    def test_hosts_are_the_public_raw_hosts(self):
        self.assertTrue(README_URL.startswith('https://raw.githubusercontent.com/actions/runner-images/main/'))
        self.assertTrue(ROLLOUT_STATE_URL.startswith('https://gist.githubusercontent.com/hosted-runners-images-bot/'))


if __name__ == '__main__':
    unittest.main()
