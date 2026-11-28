from __future__ import annotations

import unittest

from ensure_ci_always_tests_latest_operating_systems import pinned_versions as pv
from ensure_ci_always_tests_latest_operating_systems.data_types import PinnedVersions, RunnerLabelOccurrence
from ensure_ci_always_tests_latest_operating_systems.errors import PinnedVersionsFormatError

from .helpers import fixture


def repo_texts():
    return (fixture('repo/device-tests.yml').decode('utf-8'), fixture('repo/dotnet-build.yml').decode('utf-8'),
            fixture('repo/shared.env').decode('utf-8'))


def read_with(device_tests=None, dotnet_build=None, shared_env=None) -> PinnedVersions:
    original = repo_texts()
    return pv.read_pinned_versions_from_texts(device_tests or original[0], dotnet_build or original[1],
                                              shared_env or original[2])


def replaced_once(text: str, old: str, new: str) -> str:
    assert text.count(old) == 1, '%r appears %d times' % (old, text.count(old))
    return text.replace(old, new)


class ReadLiveFixtureTests(unittest.TestCase):
    def setUp(self) -> None:
        self.pinned = read_with()

    def test_android_legs(self) -> None:
        legs = [(leg.level.text, leg.target, leg.api_level_line, leg.entry_start, leg.entry_end)
                for leg in self.pinned.android_legs]
        self.assertEqual(legs, [
            ('27', 'default', 387, 387, 388), ('28', 'google_apis', 396, 396, 397), ('29', 'google_apis', 398, 398, 398),
            ('30', 'google_apis', 399, 399, 399), ('32', 'google_apis', 401, 401, 401),
            ('33', 'google_apis', 402, 402, 402), ('34', 'google_apis', 403, 403, 403),
            ('35', 'google_apis', 404, 404, 404), ('36.1', 'google_apis', 406, 406, 406),
            ('37.0', 'google_apis', 411, 411, 412), ('37.2', 'google_apis_ps16k', 415, 415, 417)])

    def test_include_lines_and_dash_prefixes(self) -> None:
        self.assertEqual(self.pinned.android_include_line, 385)
        self.assertEqual(self.pinned.ios_include_line, 776)
        self.assertEqual(self.pinned.android_dash_prefix, ' ' * 20 + '-   ')
        self.assertEqual(self.pinned.ios_dash_prefix, ' ' * 20 + '-   ')

    def test_coverage_levels_are_read_but_not_legs(self) -> None:
        self.assertEqual([level.text for level in self.pinned.coverage_levels], ['36', '25'])

    def test_ios_legs(self) -> None:
        legs = [(leg.ios_version, leg.runner, leg.xcode, leg.install_runtime, leg.entry_start, leg.entry_end)
                for leg in self.pinned.ios_legs]
        self.assertEqual(legs, [
            ('26.5', 'macos-26', '26.6', False, 778, 780), ('18.6', 'macos-15', '16.4', True, 782, 785),
            ('17.5', 'macos-15', '16.4', True, 786, 789), ('16.4', 'macos-15', '16.4', True, 790, 793),
            ('15.5', 'macos-15', '16.4', True, 794, 797)])

    def test_apple_label_has_five_occurrences_with_job_ids(self) -> None:
        self.assertEqual(self.pinned.apple_label.label, 'xcode-27')
        self.assertEqual(self.pinned.apple_label.occurrences, [
            ('.github/workflows/device-tests.yml', 551, 'device_tests_ios'),
            ('.github/workflows/device-tests.yml', 673, 'device_tests_ios_app'),
            ('.github/workflows/device-tests.yml', 905, 'device_tests_maccatalyst'),
            ('.github/workflows/dotnet-build.yml', 57, 'build_sample'),
            ('.github/workflows/dotnet-build.yml', 101, 'build_library')])

    def test_runner_label_families(self) -> None:
        self.assertEqual(self.pinned.runner_labels, {
            'ubuntu': ['ubuntu-24.04'], 'windows': ['windows-2025'], 'macos': ['macos-26', 'macos-15'],
            'xcode': ['xcode-27']})

    def test_shared_env_values_and_lines(self) -> None:
        env = self.pinned.shared_env
        self.assertEqual((env['TOOLKIT_NET_VERSION'].value, env['TOOLKIT_NET_VERSION'].line), ('11.0.100', 9))
        self.assertEqual((env['LATEST_NET_VERSION'].value, env['LATEST_NET_VERSION'].line), ('11.0.x', 12))
        self.assertEqual((env['CommunityToolkitLibrary_Xcode_Version'].value,
                          env['CommunityToolkitLibrary_Xcode_Version'].line), ('27.2', 18))
        self.assertEqual((env['CommunityToolkitSample_Xcode_Version'].value,
                          env['CommunityToolkitSample_Xcode_Version'].line), ('27.2', 21))

    def test_survives_a_json_round_trip(self) -> None:
        again = PinnedVersions.from_json(self.pinned.to_json())
        self.assertEqual(again.to_json(), self.pinned.to_json())

    def test_carriage_returns_in_shared_env_are_tolerated(self) -> None:
        pinned = read_with(shared_env=repo_texts()[2].replace('\n', '\r\n'))
        self.assertEqual(pinned.shared_env['CommunityToolkitSample_Xcode_Version'].value, '27.2')


class ErrorTests(unittest.TestCase):
    def assert_unreadable(self, fragment: str, **texts: str) -> None:
        with self.assertRaises(PinnedVersionsFormatError) as caught:
            read_with(**texts)
        self.assertIn(fragment, str(caught.exception))
        self.assertNotIn('\n', str(caught.exception))

    def test_unquoted_api_level(self) -> None:
        device_tests = replaced_once(repo_texts()[0], "-   api-level: '27'\n", '-   api-level: 27\n')
        self.assert_unreadable('single-quoted', device_tests=device_tests)

    def test_include_that_is_an_expression(self) -> None:
        text = repo_texts()[0]
        start = text.index('    device_tests_android_runtime:\n')
        end = text.index('include:\n', start)
        device_tests = text[:end] + "include: ${{ fromJSON('[]') }}\n" + text[end + len('include:\n'):]
        self.assert_unreadable('include', device_tests=device_tests)

    def test_a_repeated_required_shared_env_key(self) -> None:
        shared_env = repo_texts()[2] + 'CommunityToolkitLibrary_Xcode_Version=26.0\n'
        self.assert_unreadable('appears twice', shared_env=shared_env)

    def test_a_fifth_apple_label_that_differs(self) -> None:
        dotnet_build = replaced_once(repo_texts()[1], 'os: [ windows-latest, xcode-27 ]\n        outputs:',
                                     'os: [ windows-latest, xcode-28 ]\n        outputs:')
        self.assert_unreadable('one label', dotnet_build=dotnet_build)

    def test_a_sixth_apple_label_that_differs(self) -> None:
        third_label_in_the_os_list = replaced_once(repo_texts()[1], 'os: [ windows-latest, xcode-27 ]\n        outputs:',
                                                   'os: [ windows-latest, xcode-27, xcode-28 ]\n        outputs:')
        self.assert_unreadable('expected one os list with one non-Windows label', dotnet_build=third_label_in_the_os_list)
        text = repo_texts()[0]
        index = text.index('    device_tests_ios:\n')
        line_start = text.index('        runs-on: xcode-27\n', index)
        duplicated = text[:line_start] + '        runs-on: xcode-28\n' + text[line_start:]
        self.assert_unreadable('expected one runs-on line, found 2', device_tests=duplicated)

    def test_apple_label_that_is_not_a_label(self) -> None:
        text = repo_texts()[0]
        device_tests = text.replace('runs-on: xcode-27', 'runs-on: some-box')
        dotnet_build = repo_texts()[1].replace('xcode-27', 'some-box')
        self.assert_unreadable('xcode-NN', device_tests=device_tests, dotnet_build=dotnet_build)

    def test_missing_job(self) -> None:
        device_tests = replaced_once(repo_texts()[0], '    device_tests_android_runtime:\n',
                                     '    device_tests_android_other:\n')
        self.assert_unreadable('device_tests_android_runtime', device_tests=device_tests)

    def test_two_four_kb_legs_of_one_major(self) -> None:
        device_tests = replaced_once(repo_texts()[0], "-   api-level: '32'\n", "-   api-level: '36.0'\n")
        self.assert_unreadable('Android 36', device_tests=device_tests)

    def test_one_level_twice(self) -> None:
        device_tests = replaced_once(repo_texts()[0], "-   api-level: '33'\n", "-   api-level: '34'\n")
        self.assert_unreadable('appears twice', device_tests=device_tests)

    def test_coverage_include_without_levels(self) -> None:
        text = repo_texts()[0]
        lines = text.split('\n')
        lines = [line.replace('"api-level"', '"level"') if line.lstrip().startswith('include: ${{ fromJSON(inputs.trimming') else line
                 for line in lines]
        self.assert_unreadable('lists no', device_tests='\n'.join(lines))

    def test_an_entry_line_that_is_not_key_and_value(self) -> None:
        device_tests = replaced_once(repo_texts()[0], "                    -   api-level: '29'\n",
                                     "                    -   api-level: '29'\n                        just words\n")
        self.assert_unreadable('expected "key: value"', device_tests=device_tests)

    def test_a_double_quoted_value_is_not_supported(self) -> None:
        device_tests = replaced_once(repo_texts()[0], "-   api-level: '29'\n", '-   api-level: "29"\n')
        self.assert_unreadable('only plain and single-quoted', device_tests=device_tests)

    def test_shared_env_without_a_required_key(self) -> None:
        shared_env = '\n'.join(line for line in repo_texts()[2].split('\n')
                               if not line.startswith('CommunityToolkitSample_Xcode_Version'))
        self.assert_unreadable('CommunityToolkitSample_Xcode_Version', shared_env=shared_env)

    def test_xcode_version_that_is_not_major_minor(self) -> None:
        shared_env = replaced_once(repo_texts()[2], 'CommunityToolkitLibrary_Xcode_Version=27.2',
                                   'CommunityToolkitLibrary_Xcode_Version=27')
        self.assert_unreadable('must look like', shared_env=shared_env)

    def test_install_runtime_that_is_not_a_boolean(self) -> None:
        device_tests = replaced_once(repo_texts()[0], '                        install-runtime: true\n'
                                     '                    -   ios-version: \'17.5\'',
                                     '                        install-runtime: maybe\n'
                                     '                    -   ios-version: \'17.5\'')
        self.assert_unreadable('install-runtime', device_tests=device_tests)


DEVICE_TESTS_PATH = pv.DEVICE_TESTS_FILE
DOTNET_BUILD_PATH = pv.DOTNET_BUILD_FILE
TODAYS_MOVABLE_LINES = [
    RunnerLabelOccurrence(DEVICE_TESTS_PATH, 73, 'device_tests_android', 'ubuntu-24.04', True),
    RunnerLabelOccurrence(DEVICE_TESTS_PATH, 264, 'device_tests_android_app', 'ubuntu-24.04', True),
    RunnerLabelOccurrence(DEVICE_TESTS_PATH, 379, 'device_tests_android_runtime', 'ubuntu-24.04', True),
    RunnerLabelOccurrence(DEVICE_TESTS_PATH, 1054, 'device_tests_windows', 'windows-2025', True)]


def occurrences_of(pinned: PinnedVersions):
    return pinned.ubuntu_and_windows_label_occurrences


class RunnerLabelOccurrenceTests(unittest.TestCase):
    def test_the_fixture_has_exactly_four_movable_occurrences(self) -> None:
        self.assertEqual(occurrences_of(read_with()), TODAYS_MOVABLE_LINES)

    def test_a_trailing_comment_makes_a_line_unmovable_but_still_an_occurrence(self) -> None:
        text = replaced_once(repo_texts()[0], '        runs-on: windows-2025\n', '        runs-on: windows-2025 # why\n')
        self.assertEqual(occurrences_of(read_with(device_tests=text))[-1],
                         RunnerLabelOccurrence(DEVICE_TESTS_PATH, 1054, 'device_tests_windows', 'windows-2025', False))

    def test_a_quoted_label_is_unmovable(self) -> None:
        text = replaced_once(repo_texts()[0], '        runs-on: windows-2025\n', "        runs-on: 'windows-2025'\n")
        self.assertFalse(occurrences_of(read_with(device_tests=text))[-1].is_movable)

    def test_a_runner_value_of_a_matrix_is_unmovable_and_not_an_error(self) -> None:
        text = (repo_texts()[0] + '\n    other:\n        runs-on: ubuntu-latest\n        strategy:\n            matrix:\n'
                '                include:\n                    -   runner: ubuntu-22.04\n')
        occurrence = occurrences_of(read_with(device_tests=text))[-1]
        self.assertEqual((occurrence.label, occurrence.job_id, occurrence.is_movable), ('ubuntu-22.04', 'other', False))

    def test_an_os_list_of_dotnet_build_is_unmovable(self) -> None:
        text = replaced_once(repo_texts()[1], 'os: [ windows-latest, xcode-27 ]\n        outputs:',
                             'os: [ windows-2025, xcode-27 ]\n        outputs:')
        occurrence = occurrences_of(read_with(dotnet_build=text))[-1]
        self.assertEqual((occurrence.file, occurrence.job_id, occurrence.label, occurrence.is_movable),
                         (DOTNET_BUILD_PATH, 'build_library', 'windows-2025', False))

    def test_a_job_that_device_tests_does_not_need_is_unmovable(self) -> None:
        text = replaced_once(repo_texts()[0], 'device_tests_android, device_tests_android_app, device_tests_android_runtime, ',
                             'device_tests_android, device_tests_android_runtime, ')
        movable = [occurrence.job_id for occurrence in occurrences_of(read_with(device_tests=text)) if occurrence.is_movable]
        self.assertEqual(movable, ['device_tests_android', 'device_tests_android_runtime', 'device_tests_windows'])
        self.assertEqual(len(occurrences_of(read_with(device_tests=text))), 4)

    def test_a_sign_job_of_dotnet_build_is_never_movable(self) -> None:
        text = repo_texts()[1] + '\n    sign:\n        runs-on: windows-2025\n'
        occurrence = occurrences_of(read_with(dotnet_build=text))[-1]
        self.assertEqual((occurrence.job_id, occurrence.is_movable), ('sign', False))

    def test_values_that_are_not_labels_are_no_occurrences(self) -> None:
        text = repo_texts()[0]
        text = replaced_once(text, '        runs-on: windows-2025\n', '        runs-on: ${{ matrix.os }}\n        # runs-on: ubuntu-24.04\n')
        text += '\n    extra:\n        runs-on: ubuntu-latest\n'
        text += '    extra2:\n        runs-on: ubuntu-24.04-arm\n    extra3:\n        runs-on: windows-2025-vs2026\n    extra4:\n        runs-on: windows-11-arm\n'
        self.assertEqual([occurrence.label for occurrence in occurrences_of(read_with(device_tests=text))], ['ubuntu-24.04'] * 3)

    def test_a_workflow_call_key_under_on_is_never_a_job(self) -> None:
        text = replaced_once(repo_texts()[0], '    workflow_call:\n', '    workflow_call:\n        runs-on: ubuntu-22.04\n')
        first = occurrences_of(read_with(device_tests=text))[0]
        self.assertEqual((first.label, first.job_id, first.is_movable), ('ubuntu-22.04', '', False))

    def test_a_top_level_key_after_the_jobs_clears_the_job_id(self) -> None:
        text = repo_texts()[0] + '\nother:\n    inner:\n        runs-on: ubuntu-22.04\n'
        last = occurrences_of(read_with(device_tests=text))[-1]
        self.assertEqual((last.label, last.job_id, last.is_movable), ('ubuntu-22.04', '', False))

    def test_a_job_header_with_a_comment_ends_the_job_above_it(self) -> None:
        text = replaced_once(repo_texts()[0], '    device_tests_windows:\n',
                             '    extra_job: # not needed by device_tests\n        runs-on: ubuntu-24.04\n    device_tests_windows:\n')
        extra = [occurrence for occurrence in occurrences_of(read_with(device_tests=text)) if occurrence.label == 'ubuntu-24.04'][-1]
        self.assertEqual((extra.job_id, extra.is_movable), ('', False))
        self.assertEqual(sorted(pv.read_movable_runs_on_lines(text)), [73, 264, 379, 1056])

    def test_a_runs_on_line_nested_deeper_than_the_job_level_is_unmovable(self) -> None:
        text = replaced_once(repo_texts()[0], '        runs-on: windows-2025\n',
                             '        runs-on: ${{ matrix.runner }}\n        strategy:\n            runs-on: ubuntu-24.04\n')
        nested = occurrences_of(read_with(device_tests=text))[-1]
        self.assertEqual((nested.job_id, nested.label, nested.is_movable), ('device_tests_windows', 'ubuntu-24.04', False))

    def test_a_needs_line_nested_deeper_than_the_job_level_is_ignored(self) -> None:
        text = repo_texts()[0]
        needs = [line for line in text.split('\n') if line.startswith('        needs: [ device_tests_android,')][0]
        nested = text.replace(needs, needs + '\n            needs: [ nope ]')
        self.assertEqual(pv.read_jobs_that_device_tests_needs(pv.split_into_lines(nested)),
                         pv.read_jobs_that_device_tests_needs(pv.split_into_lines(text)))

    def test_a_label_before_jobs_has_no_job_id(self) -> None:
        text = 'runs-on: ubuntu-22.04\n' + repo_texts()[0]
        first = occurrences_of(read_with(device_tests=text))[0]
        self.assertEqual((first.job_id, first.is_movable), ('', False))

    def test_the_read_movable_lines_are_the_four_fixture_lines(self) -> None:
        self.assertEqual(sorted(pv.read_movable_runs_on_lines(repo_texts()[0])), [73, 264, 379, 1054])

    def test_the_json_round_trip_keeps_the_occurrences_and_an_old_file_has_none(self) -> None:
        pinned = read_with()
        self.assertEqual(PinnedVersions.from_json(pinned.to_json()).ubuntu_and_windows_label_occurrences, TODAYS_MOVABLE_LINES)
        old = pinned.to_json()
        del old['ubuntu_and_windows_label_occurrences']
        self.assertEqual(PinnedVersions.from_json(old).ubuntu_and_windows_label_occurrences, [])

    def assert_unreadable(self, fragment: str, device_tests: str) -> None:
        with self.assertRaises(PinnedVersionsFormatError) as caught:
            read_with(device_tests=device_tests)
        self.assertIn(fragment, str(caught.exception))
        self.assertNotIn('\n', str(caught.exception))

    def test_a_workflow_with_no_device_tests_job_is_unreadable(self) -> None:
        self.assert_unreadable("no job 'device_tests'", replaced_once(repo_texts()[0], '    device_tests:\n', '    device_tests_x:\n'))

    def test_zero_or_two_needs_lines_are_unreadable(self) -> None:
        text = repo_texts()[0]
        needs = [line for line in text.split('\n') if line.startswith('        needs: [ device_tests_android,')][0]
        self.assert_unreadable('expected one needs', text.replace(needs, '        needs: device_tests_android'))
        self.assert_unreadable('found 2', text.replace(needs, needs + '\n' + needs))

    def test_a_needed_job_that_does_not_exist_is_unreadable(self) -> None:
        self.assert_unreadable("no job 'nope'", replaced_once(repo_texts()[0], 'device_tests_maccatalyst, device_tests_windows ]',
                                                              'device_tests_maccatalyst, nope, device_tests_windows ]'))

    def test_a_needs_entry_that_is_not_a_job_id_is_unreadable(self) -> None:
        self.assert_unreadable('is not a job id', replaced_once(repo_texts()[0], 'device_tests_maccatalyst, device_tests_windows ]',
                                                                'device_tests_maccatalyst, a/b, device_tests_windows ]'))

    def test_two_movable_runs_on_lines_in_one_needed_job_are_unreadable(self) -> None:
        text = replaced_once(repo_texts()[0], '        runs-on: windows-2025\n', '        runs-on: windows-2025\n        runs-on: windows-2025\n')
        self.assert_unreadable('at most one runs-on line', text)


class ParseUbuntuOrWindowsLabelTests(unittest.TestCase):
    def test_labels_parse_to_a_family_and_a_number(self) -> None:
        self.assertEqual(pv.parse_ubuntu_or_windows_label('ubuntu-26.04'), ('ubuntu', (26, 4)))
        self.assertEqual(pv.parse_ubuntu_or_windows_label('windows-2025'), ('windows', (2025,)))

    def test_everything_else_is_none(self) -> None:
        for label in ('ubuntu-26.04-arm', 'ubuntu-latest', 'windows-11-arm', 'xcode-27', 'macos-15', 'ubuntu-\u0662\u0666.04',
                      'windows-\uff12\uff10\uff12\uff15', 'windows-2025-vs2026', ''):
            with self.subTest(label):
                self.assertIsNone(pv.parse_ubuntu_or_windows_label(label))

    def test_only_a_rise_within_a_family_is_newer(self) -> None:
        self.assertTrue(pv.is_a_newer_label_of_the_same_family('ubuntu-24.04', 'ubuntu-26.04'))
        self.assertTrue(pv.is_a_newer_label_of_the_same_family('windows-2025', 'windows-2028'))
        for old, new in (('ubuntu-26.04', 'ubuntu-26.4'), ('ubuntu-26.04', 'ubuntu-26.04'), ('ubuntu-26.04', 'ubuntu-24.04'),
                         ('ubuntu-24.04', 'windows-2028'), ('ubuntu-24.04', 'ubuntu-26.04-arm'), ('xcode-27', 'xcode-28'),
                         ('macos-15', 'macos-26'), ('ubuntu-24.04', 'ubuntu-latest')):
            with self.subTest((old, new)):
                self.assertFalse(pv.is_a_newer_label_of_the_same_family(old, new))


class HelperTests(unittest.TestCase):
    def test_job_block_ends_before_the_next_jobs_comments(self) -> None:
        lines = ['jobs:', '    # about a', '    a:', '        x: 1', '', '    # about b', '    b:', '        y: 2']
        self.assertEqual(pv.find_job_block(lines, 'a'), (3, 4))
        self.assertEqual(pv.find_job_block(lines, 'b'), (7, 8))

    def test_leading_comment_of_the_next_entry_is_not_part_of_an_entry(self) -> None:
        lines = ['jobs:', '    a:', '        include:', '            -   k: 1', '                # inside', '                m: 2',
                 '            # leads the next entry', '            -   k: 3', '        env:', '            X: 1']
        include_line, prefix, entries = pv.read_matrix_include(lines, pv.find_job_block(lines, 'a'))
        self.assertEqual((include_line, prefix), (3, ' ' * 12 + '-   '))
        self.assertEqual([(e.start, e.end, e.leading_comment_start) for e in entries], [(4, 6, 4), (8, 8, 7)])
        self.assertEqual(entries[0].values, {'k': '1', 'm': '2'})

    def test_split_into_lines_matches_line_feed_numbering(self) -> None:
        self.assertEqual(pv.split_into_lines('a\r\nb\n'), ['a', 'b'])
        self.assertEqual(pv.split_into_lines('a b\nc'), ['a b', 'c'])


if __name__ == '__main__':
    unittest.main()
