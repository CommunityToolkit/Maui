from __future__ import annotations

import json
import os
import plistlib
import subprocess
import tempfile
import unittest
from typing import Dict, List, Optional, Tuple
from unittest import mock

from ensure_ci_always_tests_latest_operating_systems import macos_runner_image
from ensure_ci_always_tests_latest_operating_systems.errors import RunnerSoftwareError
from ensure_ci_always_tests_latest_operating_systems.xcode_changes import ga_xcode_versions

from .helpers import fixture


def write_xcode_app(applications: str, folder: str, version: str, build: str, license_type: Optional[str]) -> None:
    contents = os.path.join(applications, folder, 'Contents')
    os.makedirs(os.path.join(contents, 'Resources'))
    with open(os.path.join(contents, 'version.plist'), 'wb') as file:
        plistlib.dump({'CFBundleShortVersionString': version, 'ProductBuildVersion': build}, file)
    if license_type is not None:
        with open(os.path.join(contents, 'Resources', 'LicenseInfo.plist'), 'wb') as file:
            plistlib.dump({'licenseType': license_type}, file)


def make_applications_folder(applications: str, with_license_for_27_1: bool = True) -> None:
    """The mix a real runner image has: GA apps, a beta and a release candidate that answer to GA names through symlinks."""
    write_xcode_app(applications, 'Xcode_27.2.app', '27.2', '27B52', 'Release')
    write_xcode_app(applications, 'Xcode_27.1.app', '27.1', '27A9275', 'Release' if with_license_for_27_1 else None)
    write_xcode_app(applications, 'Xcode_27.app', '27.0', '27A266a', 'Release')
    write_xcode_app(applications, 'Xcode_27.3_beta.app', '27.3', '27C5020n', 'Beta')
    write_xcode_app(applications, 'Xcode_27.2_Release_Candidate.app', '27.2', '27B50', 'GM')
    write_xcode_app(applications, 'Xcode.app', '27.1', '27A9275', 'Release')
    os.symlink('Xcode_27.3_beta.app', os.path.join(applications, 'Xcode_27.3.app'))
    os.symlink('Xcode_27.2.app', os.path.join(applications, 'Xcode_27.2.0.app'))
    os.makedirs(os.path.join(applications, 'Safari.app'))


class FakeCommands:
    def __init__(self, answers: Dict[str, Tuple[int, str]]) -> None:
        self.answers = answers
        self.calls: List[List[str]] = []
        self.timeouts: List[int] = []

    def __call__(self, argv: List[str], timeout_seconds: int = 120) -> Tuple[int, str]:
        self.calls.append(list(argv))
        self.timeouts.append(timeout_seconds)
        answer = self.answers.get(' '.join(argv), (127, ''))
        return answer.pop(0) if isinstance(answer, list) else answer


SIMCTL = 'xcrun simctl list runtimes -j'
DOTNET = 'dotnet --list-sdks'


class ListXcodeAppsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.folder = tempfile.TemporaryDirectory()
        self.addCleanup(self.folder.cleanup)
        make_applications_folder(self.folder.name)

    def test_lists_every_real_xcode_folder_with_license_type_and_symlinks(self) -> None:
        apps = macos_runner_image.list_xcode_apps(self.folder.name)
        by_folder = {app['folder']: app for app in apps}
        self.assertEqual(sorted(by_folder), sorted([
            'Xcode.app', 'Xcode_27.2.app', 'Xcode_27.2_Release_Candidate.app', 'Xcode_27.3_beta.app', 'Xcode_27.1.app',
            'Xcode_27.app']))
        self.assertEqual(by_folder['Xcode_27.2.app']['license_type'], 'Release')
        self.assertEqual(by_folder['Xcode_27.2.app']['symlinks'], ['Xcode_27.2.0.app'])
        self.assertEqual(by_folder['Xcode_27.2.app']['build'], '27B52')
        self.assertEqual(by_folder['Xcode_27.3_beta.app']['license_type'], 'Beta')
        self.assertEqual(by_folder['Xcode_27.3_beta.app']['symlinks'], ['Xcode_27.3.app'])
        self.assertEqual(by_folder['Xcode_27.2_Release_Candidate.app']['license_type'], 'GM')

    def test_symlinks_and_other_apps_are_not_listed(self) -> None:
        folders = [app['folder'] for app in macos_runner_image.list_xcode_apps(self.folder.name)]
        self.assertNotIn('Xcode_27.3.app', folders)
        self.assertNotIn('Xcode_27.2.0.app', folders)
        self.assertNotIn('Safari.app', folders)

    def test_the_unnumbered_xcode_app_is_listed_but_is_not_a_ga_folder_name(self) -> None:
        listing = {'xcode_apps': macos_runner_image.list_xcode_apps(self.folder.name)}
        self.assertIn('Xcode.app', [app['folder'] for app in listing['xcode_apps']])
        self.assertEqual(ga_xcode_versions(listing), {'27.0', '27.1', '27.2'})

    def test_a_missing_license_file_is_listed_without_a_license_type(self) -> None:
        with tempfile.TemporaryDirectory() as applications:
            make_applications_folder(applications, with_license_for_27_1=False)
            by_folder = {app['folder']: app for app in macos_runner_image.list_xcode_apps(applications)}
            self.assertIsNone(by_folder['Xcode_27.1.app']['license_type'])
            self.assertEqual(ga_xcode_versions({'xcode_apps': list(by_folder.values())}), {'27.0', '27.2'})

    def test_a_missing_applications_folder_gives_an_empty_list(self) -> None:
        self.assertEqual(macos_runner_image.list_xcode_apps(os.path.join(self.folder.name, 'nowhere')), [])


class CommandOutputTests(unittest.TestCase):
    def test_simulator_runtimes_keep_the_readme_version_and_availability(self) -> None:
        run = FakeCommands({SIMCTL: (0, fixture('macos-runner-image/simctl-runtimes.json').decode('utf-8'))})
        runtimes = macos_runner_image.list_simulator_runtimes(run)
        self.assertEqual([runtime['version'] for runtime in runtimes], ['27.0', '27.1', '27.2'])
        self.assertEqual(runtimes[0], {'platform': 'iOS', 'version': '27.0', 'name': 'iOS 27.0',
                                       'identifier': 'com.apple.CoreSimulator.SimRuntime.iOS-27-0', 'available': True})
        self.assertEqual(run.calls, [['xcrun', 'simctl', 'list', 'runtimes', '-j']])

    def test_an_unavailable_runtime_is_marked(self) -> None:
        output = json.dumps({'runtimes': [{'platform': 'iOS', 'version': '15.5', 'name': 'iOS 15.5',
                                           'identifier': 'x', 'isAvailable': False}]})
        runtimes = macos_runner_image.list_simulator_runtimes(FakeCommands({SIMCTL: (0, output)}))
        self.assertFalse(runtimes[0]['available'])

    def test_a_failing_or_broken_simctl_gives_no_runtimes(self) -> None:
        self.assertEqual(macos_runner_image.list_simulator_runtimes(FakeCommands({SIMCTL: (1, '')})), [])
        self.assertEqual(macos_runner_image.list_simulator_runtimes(FakeCommands({SIMCTL: (0, 'not json')})), [])
        self.assertEqual(macos_runner_image.list_simulator_runtimes(FakeCommands({})), [])

    def test_dotnet_sdks_are_the_first_token_of_each_line(self) -> None:
        run = FakeCommands({DOTNET: (0, fixture('macos-runner-image/list-sdks.txt').decode('utf-8'))})
        sdks = macos_runner_image.list_dotnet_sdks(run)
        self.assertEqual(sdks[0], '8.0.101')
        self.assertEqual(sdks[-1], '11.0.100')
        self.assertEqual(run.calls, [['dotnet', '--list-sdks']])

    def test_no_dotnet_gives_no_sdks(self) -> None:
        self.assertEqual(macos_runner_image.list_dotnet_sdks(FakeCommands({})), [])


class RunnerImageInformationTests(unittest.TestCase):
    def test_imagedata_gives_the_name_and_version(self) -> None:
        with tempfile.TemporaryDirectory() as home:
            with open(os.path.join(home, 'imagedata.json'), 'wb') as file:
                file.write(fixture('macos-runner-image/imagedata.json'))
            image = macos_runner_image.read_runner_image(home, {})
        self.assertEqual(image, {'name': 'xcode-27-arm64', 'version': '20261104.0337.1'})

    def test_the_environment_is_the_fallback(self) -> None:
        with tempfile.TemporaryDirectory() as home:
            image = macos_runner_image.read_runner_image(home, {'ImageVersion': '20261117.0516.1', 'ImageOS': 'macos26'})
        self.assertEqual(image, {'name': 'macos26', 'version': '20261117.0516.1'})

    def test_no_image_information_is_an_error(self) -> None:
        with tempfile.TemporaryDirectory() as home:
            with self.assertRaises(RunnerSoftwareError):
                macos_runner_image.read_runner_image(home, {})

    def test_an_imagedata_file_without_the_runner_image_group_falls_back_to_the_environment(self) -> None:
        with tempfile.TemporaryDirectory() as home:
            with open(os.path.join(home, 'imagedata.json'), 'w', encoding='utf-8') as file:
                json.dump([{'group': 'Operating System', 'detail': 'macOS'}], file)
            with self.assertRaises(RunnerSoftwareError):
                macos_runner_image.read_runner_image(home, {})


class InspectTests(unittest.TestCase):
    def setUp(self) -> None:
        self.applications = tempfile.TemporaryDirectory()
        self.home = tempfile.TemporaryDirectory()
        self.addCleanup(self.applications.cleanup)
        self.addCleanup(self.home.cleanup)
        make_applications_folder(self.applications.name)
        self.run = FakeCommands({
            SIMCTL: (0, fixture('macos-runner-image/simctl-runtimes.json').decode('utf-8')),
            DOTNET: (0, fixture('macos-runner-image/list-sdks.txt').decode('utf-8')),
        })

    def test_a_complete_image_reports_ok(self) -> None:
        with open(os.path.join(self.home.name, 'imagedata.json'), 'wb') as file:
            file.write(fixture('macos-runner-image/imagedata.json'))
        listing = macos_runner_image.inspect_macos_runner_image(
            'xcode-27', run=self.run, apps_dir=self.applications.name, home=self.home.name, environ={})
        self.assertEqual(listing['status'], 'ok')
        self.assertEqual(listing['label'], 'xcode-27')
        self.assertEqual(listing['image'], {'name': 'xcode-27-arm64', 'version': '20261104.0337.1'})
        self.assertEqual(len(listing['xcode_apps']), 6)
        self.assertEqual(len(listing['runtimes']), 3)
        self.assertIn('11.0.100', listing['dotnet_sdks'])

    def test_missing_imagedata_is_an_error_but_the_lists_stay(self) -> None:
        listing = macos_runner_image.inspect_macos_runner_image(
            'local', run=self.run, apps_dir=self.applications.name, home=self.home.name, environ={})
        self.assertEqual(listing['status'], 'error')
        self.assertIn('imagedata.json', listing['error'])
        self.assertEqual(len(listing['xcode_apps']), 6)
        self.assertEqual(len(listing['runtimes']), 3)

    def inspect_with(self, run) -> dict:
        with open(os.path.join(self.home.name, 'imagedata.json'), 'wb') as file:
            file.write(fixture('macos-runner-image/imagedata.json'))
        return macos_runner_image.inspect_macos_runner_image(
            'xcode-27', run=run, apps_dir=self.applications.name, home=self.home.name, environ={})

    def test_a_simctl_that_exits_non_zero_is_an_error_naming_the_command_and_its_exit_code(self) -> None:
        run = FakeCommands({SIMCTL: (1, ''), DOTNET: (0, fixture('macos-runner-image/list-sdks.txt').decode('utf-8'))})
        listing = self.inspect_with(run)
        self.assertEqual(listing['status'], 'error')
        self.assertIn('`xcrun simctl list runtimes -j` failed with exit code 1', listing['error'])
        self.assertEqual(len(listing['xcode_apps']), 6)

    def test_a_simctl_that_timed_out_is_an_error(self) -> None:
        with mock.patch.object(subprocess, 'run', side_effect=subprocess.TimeoutExpired('xcrun', 120)):
            listing = self.inspect_with(macos_runner_image.run_command)
        self.assertEqual(listing['status'], 'error')
        self.assertIn('`xcrun simctl list runtimes -j` timed out after 120 s and again after 240 s', listing['error'])
        self.assertEqual(listing['error'].count('simctl'), 1)

    def test_a_simctl_that_times_out_once_is_retried_with_a_longer_timeout_and_is_no_problem(self) -> None:
        sdks = fixture('macos-runner-image/list-sdks.txt').decode('utf-8')
        simctl = fixture('macos-runner-image/simctl-runtimes.json').decode('utf-8')
        run = FakeCommands({SIMCTL: [(124, ''), (0, simctl)], DOTNET: (0, sdks)})
        listing = self.inspect_with(run)
        self.assertEqual(listing['status'], 'ok')
        self.assertEqual(len(listing['runtimes']), 3)
        self.assertEqual(run.timeouts, [120, 240, 120])

    def test_two_timeouts_make_one_problem_naming_both_attempts(self) -> None:
        sdks = fixture('macos-runner-image/list-sdks.txt').decode('utf-8')
        run = FakeCommands({SIMCTL: [(124, ''), (124, '')], DOTNET: (0, sdks)})
        listing = self.inspect_with(run)
        self.assertEqual(listing['status'], 'error')
        self.assertEqual(listing['error'], '`xcrun simctl list runtimes -j` timed out after 120 s and again after 240 s')
        self.assertEqual(run.calls.count(['xcrun', 'simctl', 'list', 'runtimes', '-j']), 2)

    def test_a_command_that_fails_without_timing_out_is_not_retried(self) -> None:
        sdks = fixture('macos-runner-image/list-sdks.txt').decode('utf-8')
        run = FakeCommands({SIMCTL: (1, ''), DOTNET: (0, sdks)})
        self.inspect_with(run)
        self.assertEqual(run.calls.count(['xcrun', 'simctl', 'list', 'runtimes', '-j']), 1)

    def test_a_simctl_that_prints_text_that_is_not_json_is_an_error(self) -> None:
        run = FakeCommands({SIMCTL: (0, 'not json'), DOTNET: (0, fixture('macos-runner-image/list-sdks.txt').decode('utf-8'))})
        listing = self.inspect_with(run)
        self.assertEqual(listing['status'], 'error')
        self.assertIn('not JSON', listing['error'])

    def test_a_simctl_that_cannot_be_started_says_so(self) -> None:
        with mock.patch.object(subprocess, 'run', side_effect=FileNotFoundError('xcrun')):
            listing = self.inspect_with(macos_runner_image.run_command)
        self.assertIn('`xcrun simctl list runtimes -j` could not be started', listing['error'])

    def test_a_simctl_json_without_a_runtimes_list_is_an_error(self) -> None:
        for printed in ('{}', '[]', '{"runtimes": 5}'):
            with self.subTest(printed=printed):
                run = FakeCommands({SIMCTL: (0, printed), DOTNET: (0, fixture('macos-runner-image/list-sdks.txt').decode('utf-8'))})
                listing = self.inspect_with(run)
                self.assertEqual(listing['status'], 'error')
                self.assertIn('printed JSON without a `runtimes` list', listing['error'])

    def test_an_unreadable_applications_folder_is_an_error(self) -> None:
        with open(os.path.join(self.home.name, 'imagedata.json'), 'wb') as file:
            file.write(fixture('macos-runner-image/imagedata.json'))
        listing = macos_runner_image.inspect_macos_runner_image(
            'xcode-27', run=self.run, apps_dir=os.path.join(self.applications.name, 'nowhere'), home=self.home.name, environ={})
        self.assertEqual(listing['status'], 'error')
        self.assertIn('could not list', listing['error'])
        self.assertEqual(len(listing['runtimes']), 3)

    def test_the_listing_survives_a_json_round_trip(self) -> None:
        listing = macos_runner_image.inspect_macos_runner_image(
            'local', run=self.run, apps_dir=self.applications.name, home=self.home.name, environ={})
        self.assertEqual(json.loads(json.dumps(listing)), listing)

    def test_every_real_command_is_run_with_utf8_text_decoding(self) -> None:
        completed = subprocess.CompletedProcess(['x'], 0, stdout='', stderr='')
        with mock.patch.object(subprocess, 'run', return_value=completed) as run:
            macos_runner_image.run_command(['xcrun', 'simctl', 'list', 'runtimes', '-j'])
        self.assertEqual(run.call_args.kwargs['encoding'], 'utf-8')
        self.assertEqual(run.call_args.kwargs['timeout'], 120)

    def test_a_missing_program_is_a_failed_command_not_a_crash(self) -> None:
        with mock.patch.object(subprocess, 'run', side_effect=FileNotFoundError('dotnet')):
            self.assertEqual(macos_runner_image.run_command(['dotnet', '--list-sdks']), (127, ''))
        with mock.patch.object(subprocess, 'run', side_effect=subprocess.TimeoutExpired('x', 120)):
            self.assertEqual(macos_runner_image.run_command(['x']), (124, ''))


if __name__ == '__main__':
    unittest.main()
