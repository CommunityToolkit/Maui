from __future__ import annotations

import dataclasses
import re
import unittest

from ensure_ci_always_tests_latest_operating_systems.change_validator import validate_changes
from ensure_ci_always_tests_latest_operating_systems.data_types import (
    IosLeg, PinnedVersions, RunnerImageRow, RunnerLabelOccurrence, SafeChange, ThingToCheck)
from ensure_ci_always_tests_latest_operating_systems.downloads import Downloader
from ensure_ci_always_tests_latest_operating_systems.errors import (
    DownloadError, NotFoundError, RunnerImagesReadmeError, UnreadableSourceError)
from ensure_ci_always_tests_latest_operating_systems.line_editor import apply_changes_to_texts
from ensure_ci_always_tests_latest_operating_systems.markers import PIN_FILES
from ensure_ci_always_tests_latest_operating_systems.pinned_versions import read_pinned_versions_from_texts
from ensure_ci_always_tests_latest_operating_systems.runner_labels import (
    README_URL, ROLLOUT_STATE_URL, build_runner_label_move, describe_runner_label_move, drop_items_that_the_planned_moves_fix,
    find_newest_move_target, macos_labels_to_inspect, make_runner_label_pin, parse_rollout_state, parse_runner_images_readme,
    parse_runner_images_readme_and_duplicate_labels, parse_runner_label_pin, plan_runner_label_changes, read_runner_images)

from .helpers import FakeDownloader, fixture

TODAY_README = fixture('runners/README-0e63d4f9d.md').decode('utf-8')
README_BEHIND_ISSUE_3457 = fixture('runners/README-c61a86321.md').decode('utf-8')
DEVICE_TESTS = PIN_FILES[1]
DOTNET_BUILD = PIN_FILES[2]
UBUNTU_JOBS = ('device_tests_android', 'device_tests_android_app', 'device_tests_android_runtime')


def ios_leg(runner: str) -> IosLeg:
    return IosLeg(ios_version='26.5', runner=runner, xcode='26.5', install_runtime=False, entry_start=1, entry_end=2)


def repo_texts():
    return {path: fixture('repo/' + path.rsplit('/', 1)[1]).decode('utf-8') for path in PIN_FILES}


def pinned_from_texts(texts) -> PinnedVersions:
    return read_pinned_versions_from_texts(texts[DEVICE_TESTS], texts[DOTNET_BUILD], texts[PIN_FILES[0]])


def pinned_versions(**runner_labels) -> PinnedVersions:
    pinned = pinned_from_texts(repo_texts())
    families = dict(pinned.runner_labels)
    families.update(runner_labels)
    return dataclasses.replace(pinned, runner_labels=families)


def row(name: str, labels, badges=(), arch: str = 'x64') -> RunnerImageRow:
    return RunnerImageRow(name=name, arch=arch, labels=list(labels), badges=list(badges), rollout_state_key=None)


def readme_rows(markdown: str = TODAY_README):
    return parse_runner_images_readme(markdown)


def moves_of(plan):
    return [(change.line, change.old, change.new) for change in plan.safe_changes]


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

    def test_the_2025_readme_that_gave_macos_15_to_two_rows_keeps_the_first_row_and_names_the_label(self):
        text = fixture('runners/README-e812bbc61.md').decode('utf-8')
        rows, duplicates = parse_runner_images_readme_and_duplicate_labels(text)
        self.assertEqual(duplicates, ['macos-15'])
        self.assertEqual(sum('macos-15' in row.labels for row in rows), 1)

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

    def test_a_label_listed_twice_keeps_its_first_row(self):
        rows, duplicates = parse_runner_images_readme_and_duplicate_labels(
            '| Image | YAML Label |\n|---|---|\n| Foo | `a` |\n| Bar | `a`, `b` |\n| Baz | `a` |\n')
        self.assertEqual([(row.name, row.labels) for row in rows], [('Foo', ['a']), ('Bar', ['b'])])
        self.assertEqual(duplicates, ['a'])

    def test_two_rows_with_the_same_name_still_keep_a_shared_label_only_in_the_first(self):
        rows, duplicates = parse_runner_images_readme_and_duplicate_labels(
            '| Image | YAML Label |\n|---|---|\n| Foo | `a` |\n| Foo | `a`, `b` |\n')
        self.assertEqual([(row.name, row.labels) for row in rows], [('Foo', ['a']), ('Foo', ['b'])])
        self.assertEqual(duplicates, ['a'])

    def test_a_label_written_twice_in_one_row_is_not_a_duplicate(self):
        rows, duplicates = parse_runner_images_readme_and_duplicate_labels(
            '| Image | YAML Label |\n|---|---|\n| Foo | `macos-26` or `macos-26` |\n')
        self.assertEqual([row.labels for row in rows], [['macos-26']])
        self.assertEqual(duplicates, [])

    def test_read_runner_images_reports_the_duplicate_labels(self):
        text = '| Image | YAML Label |\n|---|---|\n| Foo | `xcode-27` |\n| Bar | `xcode-27` |\n'
        result = read_runner_images(FakeDownloader({README_URL: text.encode('utf-8')}), [])
        self.assertEqual((result['status'], result['duplicate_labels']), ('ok', ['xcode-27']))

    def test_other_format_errors_stay_errors(self):
        for text in ('no table', '| Image | YAML Label |\n|---|---|\n', '| Image | YAML Label |\n|---|---|\n| Foo | none |\n'):
            with self.subTest(text=text):
                self.assertEqual(read_runner_images(FakeDownloader({README_URL: text.encode('utf-8')}), [])['status'], 'error')

    def test_a_short_row_is_an_error(self):
        with self.assertRaises(RunnerImagesReadmeError):
            parse_runner_images_readme('| Image | YAML Label |\n|---|---|\n| Foo |\n')


class PlanRunnerLabelMovesTests(unittest.TestCase):
    def setUp(self):
        self.rows = readme_rows()

    def assert_moves_and_asks_nobody(self, markdown: str) -> None:
        plan = plan_runner_label_changes(pinned_versions(), readme_rows(markdown))
        self.assertEqual(plan.family, 'runner-labels')
        self.assertEqual(moves_of(plan), [(73, 'ubuntu-24.04', 'ubuntu-26.04'), (264, 'ubuntu-24.04', 'ubuntu-26.04'),
                                          (379, 'ubuntu-24.04', 'ubuntu-26.04')])
        for change, job in zip(plan.safe_changes, UBUNTU_JOBS):
            self.assertEqual((change.family, change.kind, change.file, change.source, change.inserted_lines),
                             ('runner-labels', 'replace-runner-label', DEVICE_TESTS, 'runner-images-readme', []))
            self.assertEqual(change.pin, 'runner-label:%s:%s' % (DEVICE_TESTS, job))
            self.assertEqual(change.description, 'Move the job %s from ubuntu-24.04 to ubuntu-26.04' % job)
        self.assertEqual(plan.things_to_check, [])
        self.assertEqual(plan.notes, [])

    def test_today_moves_the_three_android_jobs_and_asks_nobody(self):
        self.assert_moves_and_asks_nobody(TODAY_README)

    def test_the_readme_behind_issue_3457_gives_the_same_moves(self):
        self.assert_moves_and_asks_nobody(README_BEHIND_ISSUE_3457)

    def test_a_ga_windows_server_2028_moves_the_windows_job_and_the_ubuntu_jobs_move_as_before(self):
        rows = self.rows + [row('Windows Server 2028', ['windows-2028'])]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertIn((1054, 'windows-2025', 'windows-2028'), moves_of(plan))
        self.assertEqual(len(plan.safe_changes), 4)
        self.assertEqual(plan.things_to_check, [])

    def test_a_readme_without_a_windows_row_and_a_workflow_without_windows_labels_plans_without_error(self):
        pinned = dataclasses.replace(pinned_versions(), ubuntu_and_windows_label_occurrences=[
            occurrence for occurrence in pinned_versions().ubuntu_and_windows_label_occurrences
            if not occurrence.label.startswith('windows-')])
        rows = [image_row for image_row in self.rows if not any(label.startswith('windows-') for label in image_row.labels)]
        plan = plan_runner_label_changes(pinned, rows)
        self.assertEqual(len(plan.safe_changes), 3)
        self.assertFalse(any('windows' in note for note in plan.notes), plan.notes)

    def test_a_beta_label_between_the_pinned_one_and_the_ga_target_gets_no_note(self):
        plan = plan_runner_label_changes(pinned_versions(), self.rows + [row('Ubuntu 25.10', ['ubuntu-25.10'], ['beta'])])
        self.assertFalse(any('ubuntu-25.10' in note for note in plan.notes), plan.notes)

    def test_a_duplicated_beta_label_gets_only_the_duplicate_note(self):
        rows = self.rows + [row('Ubuntu 28.04', ['ubuntu-28.04'], ['beta'])]
        plan = plan_runner_label_changes(pinned_versions(), rows, ['ubuntu-28.04'])
        notes = [note for note in plan.notes if 'ubuntu-28.04' in note]
        self.assertEqual(len(notes), 1, notes)
        self.assertIn('twice', notes[0])

    def test_an_unmovable_use_that_is_already_on_the_target_gets_no_unmoved_note(self):
        texts = repo_texts()
        texts[DOTNET_BUILD] += '\n    sign:\n        runs-on: ubuntu-26.04\n'
        plan = plan_runner_label_changes(pinned_from_texts(texts), self.rows)
        self.assertFalse(any("doesn't move" in note for note in plan.notes), plan.notes)

    def test_a_badge_on_a_newer_row_gives_a_note_and_no_move_to_it(self):
        for badge in ('beta', 'preview', 'deprecated'):
            with self.subTest(badge):
                rows = self.rows + [row('Ubuntu 28.04', ['ubuntu-28.04'], [badge])]
                plan = plan_runner_label_changes(pinned_versions(), rows)
                self.assertEqual({change.new for change in plan.safe_changes}, {'ubuntu-26.04'})
                self.assertEqual(plan.notes, [
                    "The runner images README lists `ubuntu-28.04` (`%s`), so the bot doesn't move the ubuntu jobs to it: "
                    'it moves only to GA labels.' % badge])

    def test_the_2025_format_with_a_word_in_backticks_is_not_ga(self):
        rows = self.rows + [row('Ubuntu 28.04 `beta`', ['ubuntu-28.04'])]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertEqual({change.new for change in plan.safe_changes}, {'ubuntu-26.04'})
        self.assertEqual(len(plan.notes), 1)
        self.assertIn("`Ubuntu 28.04 'beta'`", plan.notes[0])

    def test_the_newest_ga_label_wins(self):
        rows = self.rows + [row('Ubuntu 28.04', ['ubuntu-28.04'])]
        self.assertEqual({change.new for change in plan_runner_label_changes(pinned_versions(), rows).safe_changes}, {'ubuntu-28.04'})

    def test_a_ga_label_below_a_beta_one_moves_and_the_beta_one_is_noted(self):
        rows = self.rows + [row('Ubuntu 28.04', ['ubuntu-28.04'], ['beta'])]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertEqual(len(plan.safe_changes), 3)
        self.assertEqual(len(plan.notes), 1)

    def test_a_label_is_never_moved_down_or_sideways(self):
        for pinned_label in ('ubuntu-26.04', 'ubuntu-28.04'):
            with self.subTest(pinned_label):
                plan = plan_runner_label_changes(self.pinned_with_every_ubuntu_job_on(pinned_label), self.rows)
                self.assertEqual(plan.safe_changes, [])

    def pinned_with_every_ubuntu_job_on(self, label: str) -> PinnedVersions:
        texts = repo_texts()
        texts[DEVICE_TESTS] = texts[DEVICE_TESTS].replace('runs-on: ubuntu-24.04', 'runs-on: ' + label)
        return pinned_from_texts(texts)

    def test_a_duplicated_newer_label_is_never_a_target_and_gives_a_note_only(self):
        plan = plan_runner_label_changes(pinned_versions(), self.rows, ['ubuntu-26.04'])
        self.assertEqual(plan.safe_changes, [])
        self.assertIn("The runner images README lists `ubuntu-26.04` twice, so the bot doesn't move a job to it until it is "
                      'listed once.', plan.notes)
        self.assertEqual(plan.things_to_check, [])

    def test_a_duplicated_xcode_label_stays_an_item_and_another_duplicate_is_a_note(self):
        plan = plan_runner_label_changes(pinned_versions(), self.rows, ['xcode-28', 'ubuntu-latest'])
        self.assertEqual(keys_of(plan), ['runner-images-readme-duplicate-label:xcode-28'])
        self.assertIn('The runner images README lists `ubuntu-latest` twice; the bot used the first row.', plan.notes)

    def test_a_label_in_an_arm_row_is_never_a_target(self):
        rows = self.rows + [row('Ubuntu 28.04 Arm64', ['ubuntu-28.04'], arch='arm64')]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertEqual({change.new for change in plan.safe_changes}, {'ubuntu-26.04'})
        self.assertEqual(plan.notes, ["The runner images README lists `ubuntu-28.04` on an `arm64` image, so the bot doesn't "
                                      'move the x64 ubuntu jobs to it.'])

    def test_variants_are_never_targets(self):
        rows = self.rows + [row('Arm', ['ubuntu-28.04-arm']), row('Vs', ['windows-2028-vs2026'])]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertEqual({change.new for change in plan.safe_changes}, {'ubuntu-26.04'})
        self.assertEqual(plan.notes, [])

    def test_two_pinned_ubuntu_labels_both_move(self):
        texts = repo_texts()
        texts[DEVICE_TESTS] = texts[DEVICE_TESTS].replace('runs-on: ubuntu-24.04', 'runs-on: ubuntu-22.04', 1)
        plan = plan_runner_label_changes(pinned_from_texts(texts), self.rows)
        self.assertEqual([(change.old, change.new) for change in plan.safe_changes],
                         [('ubuntu-22.04', 'ubuntu-26.04')] + [('ubuntu-24.04', 'ubuntu-26.04')] * 2)

    def test_an_unmovable_use_gets_no_change_and_one_note_that_names_it(self):
        pinned = self.pinned_with_unmovable_sign()
        rows = self.rows + [row('Windows Server 2028', ['windows-2028'])]
        plan = plan_runner_label_changes(pinned, rows)
        self.assertEqual(len([change for change in plan.safe_changes if change.file == DOTNET_BUILD]), 0)
        self.assertEqual(plan.notes, [
            "The bot doesn't move `sign` on `windows-2025` (`dotnet-build.yml`) to `windows-2028`: it moves only the runs-on "
            'line of a job that `device_tests` needs, which the required Device Tests check runs.'])

    def pinned_with_unmovable_sign(self) -> PinnedVersions:
        texts = repo_texts()
        texts[DOTNET_BUILD] += '\n    sign:\n        runs-on: windows-2025\n'
        return pinned_from_texts(texts)

    def test_arabic_indic_digits_are_not_version_numbers(self):
        rows = self.rows + [row('Odd', ['ubuntu-٢٦.04'])]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertEqual({change.new for change in plan.safe_changes}, {'ubuntu-26.04'})
        self.assertEqual(plan.notes, [])

    def test_every_planned_change_applies_and_validates(self):
        rows = self.rows + [row('Windows Server 2028', ['windows-2028'])]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        texts = repo_texts()
        self.assertEqual(validate_changes(texts, apply_changes_to_texts(texts, plan.safe_changes)), [])

    def test_a_family_without_a_pinned_label_plans_nothing(self):
        pinned = dataclasses.replace(pinned_versions(), ubuntu_and_windows_label_occurrences=[])
        self.assertEqual(plan_runner_label_changes(pinned, self.rows).safe_changes, [])


class RunnerLabelItemTests(unittest.TestCase):
    def setUp(self):
        self.rows = readme_rows()

    def texts_of(self, plan):
        return {thing.key: thing.text for thing in plan.things_to_check}

    def deprecated_ubuntu_24_rows(self):
        return [row('Ubuntu 24.04', ['ubuntu-24.04'], ['deprecated']) if 'ubuntu-24.04' in image_row.labels else image_row for image_row in self.rows]

    def test_a_deprecated_label_with_a_ga_target_says_the_move_is_not_in_tonights_pull_request(self):
        plan = plan_runner_label_changes(pinned_versions(), self.deprecated_ubuntu_24_rows())
        self.assertEqual(self.texts_of(plan)['runner-label-deprecated:ubuntu-24.04'],
                         'The runner images README marks `ubuntu-24.04` (`Ubuntu 24.04`) as deprecated, and the workflows use it. '
                         "The bot isn't moving the jobs to `ubuntu-26.04` tonight: a person closed the bump pull request that held the move "
                         "(run the workflow with `reopen-rejected` to bring it back), or another item here holds it back.")

    def test_a_deprecated_label_with_no_newer_ga_label_says_so(self):
        rows = [image_row for image_row in self.deprecated_ubuntu_24_rows() if 'ubuntu-26.04' not in image_row.labels]
        text = self.texts_of(plan_runner_label_changes(pinned_versions(), rows))['runner-label-deprecated:ubuntu-24.04']
        self.assertTrue(text.endswith(' The README lists no newer GA ubuntu label that the bot can move to yet; the bot moves the jobs once it does.'), text)

    def test_a_deprecated_label_that_an_unmovable_job_also_uses_names_it(self):
        texts = repo_texts()
        texts[DOTNET_BUILD] += '\n    sign:\n        runs-on: windows-2025\n'
        rows = [row('Windows Server 2025', ['windows-2025'], ['deprecated']) if 'windows-2025' in image_row.labels else image_row
                for image_row in self.rows] + [row('Windows Server 2028', ['windows-2028'])]
        text = self.texts_of(plan_runner_label_changes(pinned_from_texts(texts), rows))['runner-label-deprecated:windows-2025']
        self.assertIn('move `sign` on `windows-2025` (`dotnet-build.yml`) yourself.', text)

    def test_a_missing_label_with_a_ga_target_carries_the_same_reason(self):
        rows = [image_row for image_row in self.rows if 'ubuntu-24.04' not in image_row.labels]
        text = self.texts_of(plan_runner_label_changes(pinned_versions(), rows))['runner-label-missing:ubuntu-24.04']
        self.assertTrue(text.startswith('The runner images README no longer lists `ubuntu-24.04`, which the workflows use.'))
        self.assertTrue(text.endswith('or another item here holds it back.'))

    def test_the_filter_drops_both_keys_when_every_use_moved(self):
        pinned = pinned_versions()
        plan = plan_runner_label_changes(pinned, self.deprecated_ubuntu_24_rows())
        things = plan.things_to_check + [ThingToCheck('runner-label-missing:ubuntu-24.04', 'x'),
                                         ThingToCheck('runner-label-deprecated:macos-15', 'y')]
        kept = drop_items_that_the_planned_moves_fix(things, plan.safe_changes, pinned)
        self.assertEqual([thing.key for thing in kept], ['runner-label-deprecated:macos-15'])

    def test_the_filter_keeps_an_item_when_one_use_did_not_move(self):
        pinned = pinned_versions()
        plan = plan_runner_label_changes(pinned, self.deprecated_ubuntu_24_rows())
        kept = drop_items_that_the_planned_moves_fix(plan.things_to_check, plan.safe_changes[:2], pinned)
        self.assertEqual([thing.key for thing in kept], ['runner-label-deprecated:ubuntu-24.04'])

    def test_the_filter_keeps_an_item_when_a_use_is_unmovable(self):
        texts = repo_texts()
        texts[DOTNET_BUILD] += '\n    sign:\n        runs-on: ubuntu-24.04\n'
        pinned = pinned_from_texts(texts)
        plan = plan_runner_label_changes(pinned, self.deprecated_ubuntu_24_rows())
        kept = drop_items_that_the_planned_moves_fix(plan.things_to_check, plan.safe_changes, pinned)
        self.assertEqual([thing.key for thing in kept], ['runner-label-deprecated:ubuntu-24.04'])

    def test_the_filter_ignores_an_xcode_family_change_with_an_apple_label_pin(self):
        pinned = pinned_versions()
        apple_change = SafeChange('xcode', 'replace-runner-label', DEVICE_TESTS, 73, 'ubuntu-24.04', 'ubuntu-26.04', [],
                                  'apple-label:%s:device_tests_android' % DEVICE_TESTS, '', 'x')
        thing = ThingToCheck('runner-label-deprecated:ubuntu-24.04', 'x')
        self.assertEqual(drop_items_that_the_planned_moves_fix([thing], [apple_change], pinned), [thing])

    def test_the_filter_keeps_the_item_of_a_label_with_no_occurrence(self):
        thing = ThingToCheck('runner-label-deprecated:ubuntu-22.04', 'x')
        self.assertEqual(drop_items_that_the_planned_moves_fix([thing], [], pinned_versions()), [thing])

    def test_every_key_is_a_valid_item_key(self):
        from ensure_ci_always_tests_latest_operating_systems.markers import is_valid_item_key
        rows = [image_row for image_row in self.rows if 'macos-15' not in image_row.labels] + [row('W', ['windows-2028'])]
        for key in keys_of(plan_runner_label_changes(pinned_versions(), rows)):
            self.assertTrue(is_valid_item_key(key), key)
        for key in keys_of(plan_runner_label_changes(pinned_versions(), self.deprecated_ubuntu_24_rows())):
            self.assertTrue(is_valid_item_key(key), key)


class PlanRunnerLabelOtherLabelsTests(unittest.TestCase):
    def setUp(self):
        self.rows = readme_rows()

    def test_a_duplicate_label_is_a_thing_to_check_and_not_a_source_problem(self):
        plan = plan_runner_label_changes(pinned_versions(), self.rows, ['macos-15'])
        thing = [item for item in plan.things_to_check if item.key == 'runner-images-readme-duplicate-label:macos-15'][0]
        self.assertEqual(thing.text, 'The runner images README lists `macos-15` twice; the bot used the first row.')
        self.assertEqual(plan.source_problems, [])

    def test_variants_of_a_pinned_label_are_not_newer_labels(self):
        rows = [row('Windows', ['windows-2025', 'windows-2025-vs2026']), row('Ubuntu', ['ubuntu-24.04', 'ubuntu-24.04-arm'])]
        pinned = pinned_versions(macos=[], xcode=[])
        plan = plan_runner_label_changes(pinned, rows)
        self.assertEqual((keys_of(plan), plan.safe_changes, plan.notes), ([], [], []))

    def test_a_pinned_label_removed_from_the_readme(self):
        rows = [image_row for image_row in self.rows if 'macos-15' not in image_row.labels]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertIn('runner-label-missing:macos-15', keys_of(plan))
        self.assertEqual(plan.things_to_check[-1].text, 'The runner images README no longer lists `macos-15`, which the workflows use.')

    def test_a_deprecated_badge_on_a_pinned_label(self):
        rows = [row('macOS 15 Arm64', ['macos-15'], ['deprecated']) if 'macos-15' in image_row.labels else image_row for image_row in self.rows]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertIn('runner-label-deprecated:macos-15', keys_of(plan))
        self.assertNotIn('runner-label-missing:macos-15', keys_of(plan))
        self.assertEqual(plan.things_to_check[0].text,
                         'The runner images README marks `macos-15` (`macOS 15 Arm64`) as deprecated, and the workflows use it.')

    def test_a_deprecated_label_nobody_uses_is_not_reported(self):
        self.assertNotIn('runner-label-deprecated:macos-14', keys_of(plan_runner_label_changes(pinned_versions(), self.rows)))

    def test_a_newer_macos_is_a_note_only(self):
        rows = self.rows + [row('macOS 27 Arm64', ['macos-27'])]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertEqual(keys_of(plan), [])
        self.assertEqual(len(plan.notes), 1)
        self.assertIn('macos-27', plan.notes[0])

    def test_a_newer_xcode_is_a_note_that_says_a_person_moves_the_jobs(self):
        rows = self.rows + [row('Xcode 28', ['xcode-28'])]
        plan = plan_runner_label_changes(pinned_versions(), rows)
        self.assertEqual(keys_of(plan), [])
        self.assertEqual(len(plan.notes), 1)
        self.assertIn('xcode-28', plan.notes[0])
        self.assertIn('A person moves the Apple jobs', plan.notes[0])

    def test_readme_text_appears_only_inside_code_spans(self):
        hostile_name = 'macOS 26 @everyone <script>x</script>'
        hostile_badge = '@octocat see <img src=x onerror=alert(1)> `x`'
        rows = [row(hostile_name, ['macos-15'], ['deprecated']) if 'macos-15' in image_row.labels else image_row for image_row in self.rows]
        rows.append(row('Windows', ['windows-2028'], [hostile_badge]))
        rows.append(row('Ubuntu @everyone <script>y</script>', ['ubuntu-28.04'], arch='arm64 @octocat'))
        plan = plan_runner_label_changes(pinned_versions(), rows)
        deprecated = [item.text for item in plan.things_to_check if item.key == 'runner-label-deprecated:macos-15'][0]
        for text in [deprecated] + plan.notes:
            outside_code_spans = re.sub(r'`[^`]*`', '', text)
            for dangerous in ('@everyone', '@octocat', '<script>', '<img'):
                self.assertNotIn(dangerous, outside_code_spans, text)
        self.assertIn('`macOS 26 @everyone <script>x</script>`', deprecated)
        self.assertTrue(any('octocat' in note for note in plan.notes))


class RunnerLabelPinTests(unittest.TestCase):
    def test_a_pin_round_trips(self):
        pin = make_runner_label_pin(DEVICE_TESTS, 'device_tests_android')
        self.assertEqual(pin, 'runner-label:%s:device_tests_android' % DEVICE_TESTS)
        self.assertEqual(parse_runner_label_pin(pin), (DEVICE_TESTS, 'device_tests_android'))

    def test_other_pins_do_not_parse(self):
        for pin in ('apple-label:%s:device_tests_ios' % DEVICE_TESTS, 'runner-label:.github/workflows/other.yml:job',
                    'runner-label:%s:' % DEVICE_TESTS, 'runner-label:%s:a/b' % DEVICE_TESTS, 'env:X', ''):
            with self.subTest(pin):
                self.assertIsNone(parse_runner_label_pin(pin))

    def test_the_description_names_the_job_and_both_labels(self):
        self.assertEqual(describe_runner_label_move('job_a', 'ubuntu-24.04', 'ubuntu-26.04'),
                         'Move the job job_a from ubuntu-24.04 to ubuntu-26.04')

    def test_a_move_is_built_from_the_occurrence(self):
        occurrence = RunnerLabelOccurrence(DEVICE_TESTS, 73, 'device_tests_android', 'ubuntu-24.04', True)
        change = build_runner_label_move(occurrence, 'ubuntu-26.04')
        self.assertEqual((change.line, change.old, change.new, change.kind), (73, 'ubuntu-24.04', 'ubuntu-26.04', 'replace-runner-label'))

    def test_the_newest_target_of_a_family_is_found_without_a_move(self):
        rows = readme_rows()
        self.assertEqual(find_newest_move_target(rows, 'ubuntu', []), 'ubuntu-26.04')
        self.assertEqual(find_newest_move_target(rows, 'windows', []), 'windows-2025')
        self.assertIsNone(find_newest_move_target(rows, 'ubuntu', ['ubuntu-26.04', 'ubuntu-24.04', 'ubuntu-22.04']))


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
        rows = [image_row for image_row in self.rows if 'macos-15' not in image_row.labels]
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
