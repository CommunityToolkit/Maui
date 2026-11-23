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


DEVICE_TYPE_BUNDLES = os.path.join(os.path.dirname(__file__), 'fixtures', 'simulator-device-types')


def bundle(name: str) -> str:
    return os.path.join(DEVICE_TYPE_BUNDLES, name + '.simdevicetype')


def count_screens_of_the_fake_bundle_by_name(bundle_path: str) -> Optional[int]:
    """The local simctl fixture names real profile folders; iPhone Duo gets the two-screen profile, every other device the one-screen."""
    fake = bundle('real-iphone-duo' if 'iPhone Duo' in bundle_path else 'real-iphone')
    return macos_runner_image.read_built_in_screen_count(fake)


def iphone(name: str, bundle_path: Optional[str]) -> Dict[str, str]:
    return {'name': name, 'productFamily': 'iPhone', 'identifier': 'id.' + name.replace(' ', '-'), 'bundlePath': bundle_path or ''}


def read_screen_answer_of_a_runtime_listing(device_types: object, count_screens: object = None) -> Optional[bool]:
    runtime = {'platform': 'iOS', 'version': '27.0', 'name': 'iOS 27.0', 'identifier': 'x', 'isAvailable': True}
    if device_types is not None:
        runtime['supportedDeviceTypes'] = device_types
    runtimes = macos_runner_image.list_simulator_runtimes(
        FakeCommands({SIMCTL: (0, json.dumps({'runtimes': [runtime]}))}), count_screens=count_screens)
    return runtimes[0]['lists_iphone_with_one_screen']


class BuiltInScreenCountTests(unittest.TestCase):
    def test_a_profile_counts_its_integrated_and_primary_displays_only(self) -> None:
        self.assertEqual(macos_runner_image.read_built_in_screen_count(bundle('one-screen')), 1)
        self.assertEqual(macos_runner_image.read_built_in_screen_count(bundle('one-screen-binary')), 1)
        self.assertEqual(macos_runner_image.read_built_in_screen_count(bundle('two-screens')), 2)

    def test_a_real_profile_counts_only_the_built_in_screens_among_the_tv_out_car_play_and_resizable_outputs(self) -> None:
        self.assertEqual(macos_runner_image.read_built_in_screen_count(bundle('real-iphone')), 1)
        self.assertEqual(macos_runner_image.read_built_in_screen_count(bundle('real-iphone-duo')), 2)

    def test_the_names_of_the_displays_are_compared_ignoring_case(self) -> None:
        self.assertEqual(macos_runner_image.read_built_in_screen_count(bundle('capitalized-names')), 2)

    def test_a_profile_that_cannot_be_read_has_no_count(self) -> None:
        for path in (bundle('garbage'), bundle('no-displays'), bundle('xcode-26-profile-without-displays'), bundle('truncated-xml'),
                     bundle('capabilities-is-a-list'), bundle('malformed-date'),
                     bundle('missing'), '', None):
            with self.subTest(path=path):
                self.assertIsNone(macos_runner_image.read_built_in_screen_count(path))


class SimulatorRuntimeScreenTests(unittest.TestCase):
    def test_the_local_mac_lists_iphone_duo_only_on_27_1_and_the_device_specific_27_2(self) -> None:
        run = FakeCommands({SIMCTL: (0, fixture('macos-runner-image/simctl-runtimes-local-mac.json').decode('utf-8'))})
        problems: List[str] = []
        runtimes = macos_runner_image.list_simulator_runtimes(run, problems, count_screens_of_the_fake_bundle_by_name)
        self.assertEqual(problems, [])
        self.assertEqual({runtime['identifier'].rsplit('.', 1)[1]: runtime['lists_iphone_with_one_screen'] for runtime in runtimes}, {
            'iOS-17-5': True, 'iOS-18-6': True, 'iOS-26-2': True, 'iOS-26-4': True, 'iOS-26-5': True, 'iOS-27-0': True,
            'iOS-27-1': False, 'iOS-27-2-iPhone19-4': False})

    def test_some_iphone_with_one_screen_is_enough(self) -> None:
        self.assertIs(read_screen_answer_of_a_runtime_listing([iphone('Duo', bundle('two-screens')), iphone('One', bundle('one-screen'))]), True)

    def test_every_iphone_with_more_than_one_screen_is_false(self) -> None:
        self.assertIs(read_screen_answer_of_a_runtime_listing([iphone('Duo', bundle('two-screens'))]), False)

    def test_no_iphone_listed_is_false(self) -> None:
        self.assertIs(read_screen_answer_of_a_runtime_listing([]), False)
        self.assertIs(read_screen_answer_of_a_runtime_listing([{'name': 'iPad', 'productFamily': 'iPad', 'identifier': 'ipad', 'bundlePath': bundle('one-screen')}]), False)

    def test_a_runtime_without_device_types_is_unknown(self) -> None:
        self.assertIsNone(read_screen_answer_of_a_runtime_listing(None))

    def test_a_profile_that_cannot_be_read_is_unknown_and_never_an_error(self) -> None:
        self.assertIsNone(read_screen_answer_of_a_runtime_listing([iphone('Broken', bundle('garbage'))]))
        self.assertIsNone(read_screen_answer_of_a_runtime_listing([iphone('Missing', bundle('missing'))]))
        self.assertIsNone(read_screen_answer_of_a_runtime_listing([iphone('Duo', bundle('two-screens')), iphone('Broken', bundle('garbage'))]))
        self.assertIsNone(read_screen_answer_of_a_runtime_listing([iphone('Broken', bundle('garbage')), iphone('Duo', bundle('two-screens'))]))
        self.assertIsNone(read_screen_answer_of_a_runtime_listing([iphone('Truncated', bundle('truncated-xml'))]))
        self.assertIs(read_screen_answer_of_a_runtime_listing([iphone('Broken', bundle('garbage')), iphone('One', bundle('one-screen'))]), True)

    def test_a_reader_that_raises_is_unknown_and_never_an_error(self) -> None:
        def raises(bundle_path: str) -> Optional[int]:
            raise OSError('disk gone')
        self.assertIsNone(read_screen_answer_of_a_runtime_listing([iphone('Duo', bundle('two-screens'))], raises))

    def test_a_device_type_shared_by_runtimes_is_read_once(self) -> None:
        read: List[str] = []

        def count(bundle_path: str) -> Optional[int]:
            read.append(bundle_path)
            return 2
        device_types = [iphone('Duo', bundle('two-screens'))]
        runtimes = [{'platform': 'iOS', 'version': version, 'name': version, 'identifier': version, 'isAvailable': True,
                     'supportedDeviceTypes': device_types} for version in ('27.1', '27.2')]
        macos_runner_image.list_simulator_runtimes(FakeCommands({SIMCTL: (0, json.dumps({'runtimes': runtimes}))}), count_screens=count)
        self.assertEqual(len(read), 1)


class CommandOutputTests(unittest.TestCase):
    def test_simulator_runtimes_keep_the_readme_version_and_availability(self) -> None:
        run = FakeCommands({SIMCTL: (0, fixture('macos-runner-image/simctl-runtimes.json').decode('utf-8'))})
        runtimes = macos_runner_image.list_simulator_runtimes(run)
        self.assertEqual([runtime['version'] for runtime in runtimes], ['27.0', '27.1', '27.2'])
        self.assertEqual(runtimes[0], {'platform': 'iOS', 'version': '27.0', 'name': 'iOS 27.0',
                                       'identifier': 'com.apple.CoreSimulator.SimRuntime.iOS-27-0', 'available': True,
                                       'lists_iphone_with_one_screen': None})
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
        with mock.patch.object(subprocess, 'run', side_effect=subprocess.TimeoutExpired('xcrun', 120)) as run:
            listing = self.inspect_with(macos_runner_image.run_command)
        self.assertEqual([call.kwargs['timeout'] for call in run.call_args_list], [120, 240, 120, 240])
        self.assertIn('`dotnet --list-sdks` timed out after 120 s and again after 240 s', listing['error'])
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
