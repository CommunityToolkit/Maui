from __future__ import annotations

import os
import subprocess
import sys
import unittest

from ensure_ci_always_tests_latest_operating_systems import change_validator as validator
from ensure_ci_always_tests_latest_operating_systems import line_editor as editor
from ensure_ci_always_tests_latest_operating_systems.markers import PIN_FILES
from ensure_ci_always_tests_latest_operating_systems.pinned_versions import read_pinned_versions
from ensure_ci_always_tests_latest_operating_systems.runner_labels import build_runner_label_move

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
NEWEST_ANDROID_API_LEVEL_SCRIPT = os.path.join(REPO_ROOT, '.github', 'scripts', 'newest-android-api-level.py')
DEVICE_TESTS_FILE = os.path.join(REPO_ROOT, '.github', 'workflows', 'device-tests.yml')


def newest_four_kb_level_by_the_trimmed_legs_script() -> str:
    result = subprocess.run([sys.executable, '-I', NEWEST_ANDROID_API_LEVEL_SCRIPT, DEVICE_TESTS_FILE],
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE, encoding='utf-8')
    if result.returncode != 0:
        raise AssertionError('newest-android-api-level.py failed: %s' % result.stderr)
    return result.stdout.strip()


class LiveRepoFilesTests(unittest.TestCase):
    """A change that reformats the pin files beyond what the bot reads fails here, before the nightly run does."""

    def setUp(self) -> None:
        self.pinned = read_pinned_versions(REPO_ROOT)

    def test_android_legs_are_found(self) -> None:
        self.assertGreater(len(self.pinned.android_legs), 1)
        self.assertTrue([leg for leg in self.pinned.android_legs if leg.target == 'google_apis'])

    def test_coverage_levels_are_found(self) -> None:
        self.assertGreaterEqual(len(self.pinned.coverage_levels), 1)

    def test_apple_label_occurs_five_times(self) -> None:
        self.assertEqual(len(self.pinned.apple_label.occurrences), 5)

    def test_ios_legs_are_found(self) -> None:
        self.assertGreaterEqual(len(self.pinned.ios_legs), 1)

    def test_shared_env_holds_both_xcode_versions(self) -> None:
        self.assertIn('CommunityToolkitLibrary_Xcode_Version', self.pinned.shared_env)
        self.assertIn('CommunityToolkitSample_Xcode_Version', self.pinned.shared_env)

    def test_a_plan_without_changes_validates(self) -> None:
        for path in PIN_FILES:
            with open(os.path.join(REPO_ROOT, *path.split('/')), encoding='utf-8', newline='') as file:
                text = file.read()
            self.assertEqual(editor.apply_changes(text, []), text)
            self.assertEqual(validator.validate_file_change(path, text, text), [])

    def test_every_ubuntu_or_windows_label_of_device_tests_can_be_moved_by_the_bot(self) -> None:
        in_device_tests = [occurrence for occurrence in self.pinned.ubuntu_and_windows_label_occurrences if occurrence.file == PIN_FILES[1]]
        unmovable = [occurrence for occurrence in in_device_tests if not occurrence.is_movable]
        self.assertEqual(unmovable, [], 'Put every ubuntu-NN.NN or windows-NNNN label of device-tests.yml on the job\'s own '
                                        'runs-on: line, unquoted and with no comment, in a job that device_tests needs; '
                                        'otherwise the bot can never move it.')

    def test_the_movable_jobs_are_the_android_emulator_jobs_and_the_windows_job(self) -> None:
        movable_jobs = [occurrence.job_id for occurrence in self.pinned.ubuntu_and_windows_label_occurrences if occurrence.is_movable]
        self.assertEqual(sorted(movable_jobs), ['device_tests_android', 'device_tests_android_app',
                                                'device_tests_android_runtime', 'device_tests_windows'],
                         'The bot moves the runner labels of exactly these jobs. When a job that device_tests needs is added or '
                         'removed, update this list so the change is deliberate.')

    def test_moving_every_movable_label_to_a_higher_one_validates(self) -> None:
        texts = {}
        for path in PIN_FILES:
            with open(os.path.join(REPO_ROOT, *path.split('/')), encoding='utf-8', newline='') as file:
                texts[path] = file.read()
        higher = {'ubuntu': 'ubuntu-98.04', 'windows': 'windows-2098'}
        changes = [build_runner_label_move(occurrence, higher[occurrence.label.split('-')[0]])
                   for occurrence in self.pinned.ubuntu_and_windows_label_occurrences if occurrence.is_movable]
        self.assertTrue(changes)
        self.assertEqual(validator.validate_changes(texts, editor.apply_changes_to_texts(texts, changes)), [])

    def test_the_trimmed_legs_script_agrees_with_the_newest_four_kb_leg(self) -> None:
        newest = max((leg for leg in self.pinned.android_legs if leg.target == 'google_apis'), key=lambda leg: leg.level)
        self.assertEqual(newest_four_kb_level_by_the_trimmed_legs_script(), newest.level.text)


if __name__ == '__main__':
    unittest.main()
