from __future__ import annotations

import unittest
from typing import List
from unittest import mock

from ensure_ci_always_tests_latest_operating_systems import change_validator as validator
from ensure_ci_always_tests_latest_operating_systems import line_editor as editor
from ensure_ci_always_tests_latest_operating_systems.markers import PIN_FILES
from ensure_ci_always_tests_latest_operating_systems.runner_labels import make_runner_label_pin

from .test_pinned_versions import replaced_once
from .test_line_editor import (api_level_change, change, insert_android_leg, label_changes, pinned, repo_texts,
                               xcode_change)

SHARED_ENV, DEVICE_TESTS, DOTNET_BUILD = PIN_FILES


def problems_after(changes, path: str) -> List[str]:
    texts = repo_texts()
    head = editor.apply_changes_to_texts(texts, changes)
    return validator.validate_file_change(path, texts[path], head[path])


def runner_moves():
    pins = {73: 'device_tests_android', 264: 'device_tests_android_app', 379: 'device_tests_android_runtime',
            1054: 'device_tests_windows'}
    return [change('replace-runner-label', DEVICE_TESTS, line, 'windows-2025' if line == 1054 else 'ubuntu-24.04',
                   'windows-2028' if line == 1054 else 'ubuntu-26.04', pin=make_runner_label_pin(DEVICE_TESTS, job))
            for line, job in pins.items()]


def problems_of_edited_text(path: str, edited: str) -> List[str]:
    return validator.validate_file_change(path, repo_texts()[path], edited)


class AcceptedEditsTests(unittest.TestCase):
    def test_nothing_changed(self) -> None:
        for path in PIN_FILES:
            self.assertEqual(problems_after([], path), [])

    def test_both_xcode_versions(self) -> None:
        changes = [xcode_change('CommunityToolkitLibrary_Xcode_Version', 18), xcode_change('CommunityToolkitSample_Xcode_Version', 21)]
        self.assertEqual(problems_after(changes, SHARED_ENV), [])

    def test_a_level_that_rises(self) -> None:
        self.assertEqual(problems_after([api_level_change('36.1', '36.2', 406)], DEVICE_TESTS), [])

    def test_two_adjacent_levels_that_rise(self) -> None:
        changes = [api_level_change('29', '29.1', 398), api_level_change('30', '30.1', 399)]
        self.assertEqual(problems_after(changes, DEVICE_TESTS), [])

    def test_a_new_four_kb_leg_and_a_new_sixteen_kb_leg(self) -> None:
        for changes in ([insert_android_leg('38.0', 'google_apis', 417)],
                        [insert_android_leg('38.1', 'google_apis_ps16k', 417)],
                        [insert_android_leg('38.0', 'google_apis', 417), insert_android_leg('38.1', 'google_apis_ps16k', 417)],
                        [insert_android_leg('39.0', 'google_apis', 417), insert_android_leg('38.0', 'google_apis', 417)]):
            with self.subTest([c.inserted_lines for c in changes]):
                self.assertEqual(problems_after(changes, DEVICE_TESTS), [])

    def test_a_level_that_rises_next_to_an_inserted_leg(self) -> None:
        changes = [api_level_change('36.1', '36.2', 406), insert_android_leg('36.3', 'google_apis_ps16k', 406)]
        self.assertEqual(problems_after(changes, DEVICE_TESTS), [])

    def test_a_one_line_entry_that_rises(self) -> None:
        texts = repo_texts()
        edited = replaced_once(texts[DEVICE_TESTS], "-   api-level: '35'\n", "-   api-level: '35.1'\n")
        self.assertEqual(problems_of_edited_text(DEVICE_TESTS, edited), [])

    def test_a_rising_level_and_an_inserted_leg_in_one_edit(self) -> None:
        changes = [api_level_change('37.2', '37.3', 415), insert_android_leg('38.0', 'google_apis', 417)]
        self.assertEqual(problems_after(changes, DEVICE_TESTS), [])

    def test_every_editor_output_with_many_changes_passes_the_whole_set_check(self) -> None:
        texts = repo_texts()
        changes = [xcode_change('CommunityToolkitSample_Xcode_Version', 21), api_level_change('36.1', '36.2', 406),
                   insert_android_leg('38.0', 'google_apis', 417)]
        self.assertEqual(validator.validate_changes(texts, editor.apply_changes_to_texts(texts, changes)), [])

    def test_each_runner_label_move_alone_and_all_together(self) -> None:
        moves = runner_moves()
        for move in moves:
            with self.subTest(move.line):
                self.assertEqual(problems_after([move], DEVICE_TESTS), [])
        self.assertEqual(problems_after(moves, DEVICE_TESTS), [])

    def test_runner_label_moves_together_with_an_api_level_rise_and_an_inserted_leg(self) -> None:
        texts = repo_texts()
        changes = runner_moves() + [api_level_change('37.2', '37.3', 415), insert_android_leg('38.0', 'google_apis', 417)]
        self.assertEqual(validator.validate_changes(texts, editor.apply_changes_to_texts(texts, changes)), [])

    def test_a_busy_night_of_fourteen_blocks_stays_under_the_cap_of_sixteen(self) -> None:
        texts = repo_texts()
        changes = runner_moves() + [xcode_change('CommunityToolkitLibrary_Xcode_Version', 18),
                                    xcode_change('CommunityToolkitSample_Xcode_Version', 21),
                                    api_level_change('27', '27.1', 387), api_level_change('28', '28.1', 396),
                                    api_level_change('29', '29.1', 398), api_level_change('32', '32.1', 401),
                                    api_level_change('34', '34.1', 403), api_level_change('36.1', '36.2', 406),
                                    api_level_change('37.0', '37.1', 411), api_level_change('37.2', '37.3', 415)]
        self.assertEqual(validator.validate_changes(texts, editor.apply_changes_to_texts(texts, changes)), [])

    def test_a_carriage_return_base_with_carriage_return_moves(self) -> None:
        base = repo_texts()[DEVICE_TESTS].replace('\n', '\r\n')
        head = base.replace('runs-on: ubuntu-24.04\r\n', 'runs-on: ubuntu-26.04\r\n')
        self.assertEqual(validator.validate_file_change(DEVICE_TESTS, base, head), [])

    def test_carriage_returns_throughout_are_allowed_when_the_base_has_them(self) -> None:
        base = repo_texts()[SHARED_ENV].replace('\n', '\r\n')
        head = base.replace('CommunityToolkitSample_Xcode_Version=27.2', 'CommunityToolkitSample_Xcode_Version=27.3')
        self.assertEqual(validator.validate_file_change(SHARED_ENV, base, head), [])


class RejectedEditsTests(unittest.TestCase):
    def assert_rejected(self, problems: List[str], fragment: str = '') -> None:
        self.assertTrue(problems, 'the edit was accepted')
        self.assertTrue(any(fragment in problem for problem in problems), problems)

    def edit_device_tests(self, old: str, new: str) -> List[str]:
        return problems_of_edited_text(DEVICE_TESTS, replaced_once(repo_texts()[DEVICE_TESTS], old, new))

    def test_a_changed_comment(self) -> None:
        self.assert_rejected(self.edit_device_tests('# Android 8.1. The only', '# Android 8.2. The only'), 'api-level line')

    def test_a_level_in_the_coverage_job(self) -> None:
        self.assert_rejected(self.edit_device_tests('"api-level":"36"', '"api-level":"37"'), 'api-level')

    def test_a_level_in_another_job(self) -> None:
        self.assert_rejected(self.edit_device_tests("api-level: ${{ matrix.api-level }}", "api-level: '38'"), 'api-level line')

    def test_an_unquoted_level(self) -> None:
        self.assert_rejected(self.edit_device_tests("-   api-level: '36.1'\n", '-   api-level: 36.2\n'), 'single quotes')

    def test_a_changed_emulator_features_value(self) -> None:
        lines = repo_texts()[DEVICE_TESTS].split('\n')
        number = next(index for index, line in enumerate(lines[386:], start=386) if 'emulator-features:' in line)
        lines[number] = lines[number].replace('-HardwareDecoder', '-GLDMA').replace('-GLDMA,-YUVCache', '-GLDMA')
        lines[number] += ',-Extra'
        self.assert_rejected(problems_of_edited_text(DEVICE_TESTS, '\n'.join(lines)), 'api-level line')

    def test_an_inserted_ram_size_line(self) -> None:
        texts = repo_texts()
        lines = texts[DEVICE_TESTS].split('\n')
        lines.insert(406, ' ' * 24 + 'ram-size: 2048M')
        self.assert_rejected(problems_of_edited_text(DEVICE_TESTS, '\n'.join(lines)), 'new Android leg')

    def test_a_deleted_line(self) -> None:
        lines = repo_texts()[DEVICE_TESTS].split('\n')
        del lines[403]
        self.assert_rejected(problems_of_edited_text(DEVICE_TESTS, '\n'.join(lines)), 'deleted')

    def test_another_shared_env_key(self) -> None:
        edited = replaced_once(repo_texts()[SHARED_ENV], 'TOOLKIT_NET_VERSION=11.0.100', 'TOOLKIT_NET_VERSION=11.0.101')
        self.assert_rejected(problems_of_edited_text(SHARED_ENV, edited), 'Xcode')

    def test_an_xcode_value_that_is_not_major_minor(self) -> None:
        edited = replaced_once(repo_texts()[SHARED_ENV], 'CommunityToolkitSample_Xcode_Version=27.2',
                               'CommunityToolkitSample_Xcode_Version=27')
        self.assert_rejected(problems_of_edited_text(SHARED_ENV, edited), 'Xcode')

    def test_a_value_moved_to_the_other_key(self) -> None:
        edited = replaced_once(repo_texts()[SHARED_ENV], 'CommunityToolkitSample_Xcode_Version=27.2',
                               'CommunityToolkitLibrary_Xcode_Version=27.3')
        self.assert_rejected(problems_of_edited_text(SHARED_ENV, edited), 'another key')

    def test_any_label_edit(self) -> None:
        texts = repo_texts()
        after = editor.apply_changes_to_texts(texts, label_changes('xcode-28'))
        for path in (DEVICE_TESTS, DOTNET_BUILD):
            with self.subTest(path):
                self.assert_rejected(validator.validate_file_change(path, texts[path], after[path]))

    def test_an_inserted_ios_leg(self) -> None:
        lines = editor.ios_leg_lines(pinned().ios_dash_prefix, '27.3', 'xcode-27', '27.3')
        self.assert_rejected(problems_after([change('insert-ios-leg', DEVICE_TESTS, 776, inserted=lines)], DEVICE_TESTS))

    def test_a_file_outside_the_list(self) -> None:
        self.assert_rejected(validator.validate_file_change('.github/workflows/other.yml', 'a\n', 'b\n'), 'not one of the files')

    def edit_line(self, number: int, new_text: str, path: str = DEVICE_TESTS) -> List[str]:
        lines = repo_texts()[path].split('\n')
        lines[number - 1] = new_text
        return problems_of_edited_text(path, '\n'.join(lines))

    def test_a_runner_label_move_to_another_family(self) -> None:
        self.assert_rejected(self.edit_line(73, '        runs-on: windows-2028'), 'another family')

    def test_an_equal_and_a_lower_runner_label(self) -> None:
        for label in ('ubuntu-22.04', 'ubuntu-24.4'):
            with self.subTest(label):
                self.assert_rejected(self.edit_line(73, '        runs-on: ' + label), 'only move to a newer one')

    def test_runner_labels_that_are_not_versions(self) -> None:
        for label in ('ubuntu-latest', 'ubuntu-26.04-arm', 'xcode-27', '${{ matrix.os }}', 'ubuntu-\u0662\u0666.04'):
            with self.subTest(label):
                self.assert_rejected(self.edit_line(73, '        runs-on: ' + label))
        self.assert_rejected(self.edit_line(1054, '        runs-on: windows-2025-vs2026'))

    def test_an_added_quote_comment_or_indent_on_a_runner_label(self) -> None:
        for text in ("        runs-on: 'ubuntu-26.04'", '        runs-on: ubuntu-26.04 # new', '          runs-on: ubuntu-26.04',
                     '        runs-on:  ubuntu-26.04', '        runs-on: ubuntu-26.04 '):
            with self.subTest(text):
                self.assert_rejected(self.edit_line(73, text), 'may change only its label')

    def test_a_label_of_a_job_whose_header_has_a_comment_cannot_move(self) -> None:
        base = replaced_once(repo_texts()[DEVICE_TESTS], '    device_tests_windows:\n',
                             '    extra_job: # not needed by device_tests\n        runs-on: ubuntu-24.04\n    device_tests_windows:\n')
        lines = base.split('\n')
        number = lines.index('    extra_job: # not needed by device_tests') + 2
        lines[number - 1] = '        runs-on: ubuntu-26.04'
        self.assert_rejected(validator.validate_file_change(DEVICE_TESTS, base, '\n'.join(lines)), 'api-level line')

    def test_the_device_tests_aggregators_own_runs_on_cannot_move(self) -> None:
        self.assert_rejected(self.edit_line(1196, '        runs-on: ubuntu-26.04'), 'api-level line')

    def test_an_apple_label_cannot_move_through_the_runner_label_path(self) -> None:
        for number in (551, 673, 905):
            with self.subTest(number):
                self.assert_rejected(self.edit_line(number, '        runs-on: xcode-28'), 'api-level line')

    def test_the_ios_matrix_runner_lines_cannot_move(self) -> None:
        self.assert_rejected(self.edit_line(779, '                        runner: macos-27'), 'api-level line')

    def test_a_comment_that_holds_a_label_cannot_move(self) -> None:
        lines = repo_texts()[DEVICE_TESTS].split('\n')
        lines.insert(72, '        # runs-on: ubuntu-24.04')
        base = '\n'.join(lines)
        lines[72] = '        # runs-on: ubuntu-26.04'
        self.assert_rejected(validator.validate_file_change(DEVICE_TESTS, base, '\n'.join(lines)), 'api-level line')

    def test_a_job_taken_out_of_needs_in_the_base_cannot_move(self) -> None:
        base = replaced_once(repo_texts()[DEVICE_TESTS], 'device_tests_android, device_tests_android_app, ', 'device_tests_android, ')
        lines = base.split('\n')
        lines[263] = '        runs-on: ubuntu-26.04'
        self.assert_rejected(validator.validate_file_change(DEVICE_TESTS, base, '\n'.join(lines)), 'api-level line')

    def test_a_head_that_adds_a_job_to_needs_and_moves_it_gains_nothing(self) -> None:
        base = replaced_once(repo_texts()[DEVICE_TESTS], 'device_tests_android, device_tests_android_app, ', 'device_tests_android, ')
        head = replaced_once(base, 'device_tests_android, device_tests_android_runtime',
                             'device_tests_android, device_tests_android_app, device_tests_android_runtime')
        lines = head.split('\n')
        lines[263] = '        runs-on: ubuntu-26.04'
        self.assert_rejected(validator.validate_file_change(DEVICE_TESTS, base, '\n'.join(lines)))

    def test_a_needs_edit_alone_is_rejected(self) -> None:
        self.assert_rejected(self.edit_device_tests('device_tests_android, device_tests_android_app, ', 'device_tests_android, '))

    def test_a_dotnet_build_runs_on_edit_is_rejected(self) -> None:
        base = repo_texts()[DOTNET_BUILD] + '\n    sign:\n        runs-on: windows-2025\n'
        head = base.replace('runs-on: windows-2025', 'runs-on: windows-2028')
        self.assert_rejected(validator.validate_file_change(DOTNET_BUILD, base, head), 'a person must make this edit')

    def test_a_base_with_no_needs_line_cannot_be_read(self) -> None:
        base = replaced_once(repo_texts()[DEVICE_TESTS], 'device_tests_android, device_tests_android_app, ', 'device_tests_android, ')
        base = base.replace('        needs: [ device_tests_android, device_tests_android_runtime',
                            '        needs_x: [ device_tests_android, device_tests_android_runtime')
        self.assert_rejected(validator.validate_file_change(DEVICE_TESTS, base, base + '# x\n'), "can't be read")

    def test_too_many_blocks(self) -> None:
        lines = repo_texts()[DEVICE_TESTS].split('\n')
        for number in range(1, 341, 20):
            lines[number] += ' x'
        self.assert_rejected(problems_of_edited_text(DEVICE_TESTS, '\n'.join(lines)), 'changed blocks')

    def test_blocks_are_counted_across_files(self) -> None:
        texts = repo_texts()
        changes = [xcode_change('CommunityToolkitLibrary_Xcode_Version', 18), xcode_change('CommunityToolkitSample_Xcode_Version', 21),
                   api_level_change('27', '27.1', 387), api_level_change('28', '28.1', 396), api_level_change('36.1', '36.2', 406)]
        head = editor.apply_changes_to_texts(texts, changes)
        self.assertEqual(validator.validate_changes(texts, head), [])
        with mock.patch.object(validator, 'MAX_CHANGED_BLOCKS', 4):
            self.assert_rejected(validator.validate_changes(texts, head), 'in total')

    def test_an_introduced_carriage_return(self) -> None:
        texts = repo_texts()
        edited = replaced_once(texts[SHARED_ENV], 'CommunityToolkitSample_Xcode_Version=27.2\n',
                               'CommunityToolkitSample_Xcode_Version=27.3\r\n')
        self.assert_rejected(problems_of_edited_text(SHARED_ENV, edited), 'line ending')

    def test_a_removed_final_newline(self) -> None:
        edited = repo_texts()[SHARED_ENV].rstrip('\n')
        self.assert_rejected(problems_of_edited_text(SHARED_ENV, edited))

    def test_a_lower_or_equal_level(self) -> None:
        for new in ('36.0', '35'):
            with self.subTest(new):
                self.assert_rejected(self.edit_device_tests("-   api-level: '36.1'\n", "-   api-level: '%s'\n" % new), 'may only rise')

    def test_a_level_that_moves_to_another_major(self) -> None:
        self.assert_rejected(self.edit_device_tests("-   api-level: '36.1'\n", "-   api-level: '38.0'\n"), 'another major')
        self.assert_rejected(self.edit_device_tests("-   api-level: '35'\n", "-   api-level: '36'\n"), 'another major')

    def test_a_level_equal_to_a_coverage_level(self) -> None:
        texts = repo_texts()
        base = replaced_once(texts[DEVICE_TESTS], '"api-level":"36"', '"api-level":"36.2"')
        head = replaced_once(base, "-   api-level: '36.1'\n", "-   api-level: '36.2'\n")
        self.assert_rejected(validator.validate_file_change(DEVICE_TESTS, base, head), 'coverage job')
        head = editor.apply_changes_to_texts(texts, [insert_android_leg('36.0', 'google_apis_ps16k', 417)])
        self.assert_rejected(validator.validate_file_change(DEVICE_TESTS, texts[DEVICE_TESTS], head[DEVICE_TESTS]))

    def test_two_legs_of_one_major_and_target(self) -> None:
        self.assert_rejected(problems_after([insert_android_leg('37.1', 'google_apis', 417)], DEVICE_TESTS), 'target')

    def test_an_insert_outside_the_android_list(self) -> None:
        change_outside = change('insert-android-leg', DEVICE_TESTS, 100,
                                inserted=editor.android_leg_lines(pinned().android_dash_prefix, '38.0', 'google_apis'))
        self.assert_rejected(problems_after([change_outside], DEVICE_TESTS), 'right after the last line of an existing leg')

    def test_an_insert_inside_an_existing_leg_would_move_its_later_keys_to_the_new_leg(self) -> None:
        for level, target, after_line in (('31', 'google_apis', 396), ('38.0', 'google_apis', 416), ('26', 'google_apis', 387)):
            with self.subTest(level=level, after_line=after_line):
                problems = problems_after([insert_android_leg(level, target, after_line)], DEVICE_TESTS)
                self.assert_rejected(problems, 'right after the last line of an existing leg')

    def test_a_carriage_return_added_to_one_line_of_a_file_that_already_mixes_endings(self) -> None:
        base = repo_texts()[SHARED_ENV]
        first, rest = base.split('\n', 1)
        mixed = first + '\r\n' + rest
        head = replaced_once(mixed, 'CommunityToolkitSample_Xcode_Version=27.2\n', 'CommunityToolkitSample_Xcode_Version=27.3\r\n')
        self.assert_rejected(validator.validate_file_change(SHARED_ENV, mixed, head), 'line ending')
        same_ending = replaced_once(mixed, 'CommunityToolkitSample_Xcode_Version=27.2\n', 'CommunityToolkitSample_Xcode_Version=27.3\n')
        self.assertEqual(validator.validate_file_change(SHARED_ENV, mixed, same_ending), [])

    def test_an_insert_with_another_dash_prefix(self) -> None:
        change_wrong = change('insert-android-leg', DEVICE_TESTS, 417,
                              inserted=editor.android_leg_lines('                -   ', '38.0', 'google_apis'))
        self.assert_rejected(problems_after([change_wrong], DEVICE_TESTS), 'dash prefix')

    def test_a_file_added_or_removed(self) -> None:
        texts = repo_texts()
        self.assert_rejected(validator.validate_changes(texts, {SHARED_ENV: texts[SHARED_ENV]}), 'added or removed')


if __name__ == '__main__':
    unittest.main()
