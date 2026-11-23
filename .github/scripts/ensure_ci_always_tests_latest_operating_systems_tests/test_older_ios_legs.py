from __future__ import annotations

import copy
import json
import unittest
from typing import Any, Dict, List

from ensure_ci_always_tests_latest_operating_systems import ios_runtimes, older_ios_legs
from ensure_ci_always_tests_latest_operating_systems.data_types import FamilyPlan
from ensure_ci_always_tests_latest_operating_systems.pinned_versions import read_pinned_versions_from_texts

from .helpers import fixture


def pinned_versions(device_tests_edit: Any = None) -> Any:
    device_tests = fixture('repo/device-tests.yml').decode('utf-8')
    if device_tests_edit is not None:
        device_tests = device_tests_edit(device_tests)
    return read_pinned_versions_from_texts(
        device_tests, fixture('repo/dotnet-build.yml').decode('utf-8'), fixture('repo/shared.env').decode('utf-8'))


def listing(name: str) -> Dict[str, Any]:
    return json.loads(fixture('macos-runner-image/' + name + '.json').decode('utf-8'))


def todays_listings() -> Dict[str, Any]:
    return {'macos-26': listing('macos-26'), 'macos-15': listing('macos-15'), 'xcode-27': listing('xcode-27-20261104')}


def runtime(version: str, platform: str = 'iOS', available: bool = True) -> Dict[str, Any]:
    return {'platform': platform, 'version': version, 'name': '%s %s' % (platform, version), 'identifier': 'x',
            'available': available}


def runtime_listing_one_screen_iphone(version: str, identifier: str, lists_iphone_with_one_screen: Any) -> Dict[str, Any]:
    return dict(runtime(version), identifier='com.apple.CoreSimulator.SimRuntime.' + identifier,
                lists_iphone_with_one_screen=lists_iphone_with_one_screen)


def keys(result: FamilyPlan) -> List[str]:
    return [thing.key for thing in result.things_to_check]


class TodayTests(unittest.TestCase):
    def test_today_has_nothing_to_check(self) -> None:
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), todays_listings())
        self.assertEqual(result.family, 'older-ios-legs')
        self.assertEqual(result.things_to_check, [])
        self.assertEqual(result.source_problems, [])
        self.assertEqual(result.safe_changes, [])


class RuntimeCheckTests(unittest.TestCase):
    def test_a_newer_runtime_of_the_same_major_is_reported(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'].append(runtime('26.6'))
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-newer-runtime:macos-26:26.5:26.6'])
        self.assertIn('a person does it', result.things_to_check[0].text)

    def test_a_newer_runtime_of_another_major_is_not_reported(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'].append(runtime('27.0'))
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_a_runtime_of_another_platform_does_not_count(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'].append(runtime('26.6', platform='tvOS'))
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_an_unavailable_runtime_does_not_count(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'].append(runtime('26.6', available=False))
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_a_missing_runtime_is_reported(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'] = [r for r in listings['macos-26']['runtimes'] if r['version'] != '26.5']
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-runtime-missing:macos-26:26.5'])

    def test_a_patch_release_runtime_does_not_answer_for_its_major_minor(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'] = [runtime('26.5.1')]
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-runtime-missing:macos-26:26.5', 'ios-leg-newer-runtime:macos-26:26.5:26.5.1'])

    def test_a_point_release_swap_is_missing_and_newer_by_its_full_version(self) -> None:
        listings = todays_listings()
        for entry in listings['macos-26']['runtimes']:
            if entry['version'] == '26.5':
                entry['version'] = '26.5.1'
                entry['name'] = 'iOS 26.5'
                entry['identifier'] = 'com.apple.CoreSimulator.SimRuntime.iOS-26-5'
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-runtime-missing:macos-26:26.5', 'ios-leg-newer-runtime:macos-26:26.5:26.5.1'])

    def test_a_leg_with_a_three_part_version_is_missing_when_the_image_swaps_the_runtime(self) -> None:
        edited = pinned_versions(lambda text: text.replace("ios-version: '26.5'", "ios-version: '26.5.1'", 1))
        listings = todays_listings()
        listings['macos-26']['runtimes'] = [runtime('26.5.2')]
        listings['macos-26']['runtimes'][0].update(name='iOS 26.5', identifier='com.apple.CoreSimulator.SimRuntime.iOS-26-5')
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(edited, listings)),
                         ['ios-leg-runtime-missing:macos-26:26.5.1', 'ios-leg-newer-runtime:macos-26:26.5.1:26.5.2'])

    def test_the_newer_runtime_is_named_by_its_full_version(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'].append(runtime('26.6.1'))
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(pinned_versions(), listings)),
                         ['ios-leg-newer-runtime:macos-26:26.5:26.6.1'])

    def test_a_beta_runtime_is_not_offered_as_the_newer_runtime(self) -> None:
        listings = todays_listings()
        listings['macos-26']['xcode_apps'].append({'folder': 'Xcode_26.7_beta.app', 'version': '26.7', 'build': '0',
                                                   'license_type': 'Beta', 'symlinks': []})
        listings['macos-26']['runtimes'].append(runtime('26.7'))
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_a_runtime_found_by_name_or_identifier_suffix_counts(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'] = [dict(runtime('26.5.1'), name='26.5')]
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(pinned_versions(), listings)),
                         ['ios-leg-newer-runtime:macos-26:26.5:26.5.1'])
        listings['macos-26']['runtimes'] = [dict(runtime('26.5.1'), identifier='com.apple.CoreSimulator.SimRuntime.26.5')]
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(pinned_versions(), listings)),
                         ['ios-leg-newer-runtime:macos-26:26.5:26.5.1'])

    def test_a_leg_named_by_identifier_is_checked_without_a_number(self) -> None:
        identifier = 'com.apple.CoreSimulator.SimRuntime.iOS-26-5'
        edited = pinned_versions(lambda text: text.replace("ios-version: '26.5'", "ios-version: '%s'" % identifier, 1))
        listings = todays_listings()
        listings['macos-26']['runtimes'] = [dict(runtime('26.5'), identifier=identifier), runtime('26.6')]
        self.assertEqual(older_ios_legs.check_older_ios_legs(edited, listings).things_to_check, [])
        listings['macos-26']['runtimes'] = [runtime('26.6')]
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(edited, listings)), ['ios-leg-runtime-missing:macos-26:%s' % identifier])

    def test_the_newer_runtime_is_compared_by_number_not_by_text(self) -> None:
        edited = pinned_versions(lambda text: text.replace("ios-version: '26.5'", "ios-version: '26.9'", 1))
        listings = todays_listings()
        listings['macos-26']['runtimes'] = [runtime('26.9'), runtime('26.10')]
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(edited, listings)), ['ios-leg-newer-runtime:macos-26:26.9:26.10'])

    def test_a_runtime_of_a_beta_with_a_three_part_version_is_not_offered(self) -> None:
        listings = todays_listings()
        listings['macos-26']['xcode_apps'].append({'folder': 'Xcode_26.7_beta.app', 'version': '26.7', 'build': '0',
                                                   'license_type': 'Beta', 'symlinks': []})
        listings['macos-26']['runtimes'].append(runtime('26.7.1'))
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_a_runtime_of_a_ga_named_xcode_whose_license_could_not_be_read_is_offered(self) -> None:
        listings = todays_listings()
        listings['macos-26']['xcode_apps'].append({'folder': 'Xcode_26.7.app', 'version': '26.7', 'build': '0',
                                                   'license_type': None, 'symlinks': []})
        listings['macos-26']['runtimes'].append(runtime('26.7'))
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(pinned_versions(), listings)),
                         ['ios-leg-newer-runtime:macos-26:26.5:26.7'])

    def test_a_runtime_found_by_its_full_identifier_counts(self) -> None:
        identifier = 'com.apple.CoreSimulator.SimRuntime.iOS-26-5'
        self.assertTrue(ios_runtimes.is_found_by_ios_script(dict(runtime('26.5'), identifier=identifier), identifier))

    def test_install_runtime_legs_skip_the_runtime_checks(self) -> None:
        listings = todays_listings()
        listings['macos-15']['runtimes'] = []
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])


class RuntimeThatCannotRunTheTestsTests(unittest.TestCase):
    def listings_where_the_leg_runtime_lists_only_iphone_duo(self) -> Dict[str, Any]:
        listings = todays_listings()
        listings['macos-26']['runtimes'] = [runtime_listing_one_screen_iphone('26.5', 'iOS-26-5', False)]
        return listings

    def test_a_leg_whose_only_matching_runtime_has_no_one_screen_iphone_is_reported(self) -> None:
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), self.listings_where_the_leg_runtime_lists_only_iphone_duo())
        self.assertEqual(keys(result), ['ios-leg-runtime-cannot-run-tests:macos-26:26.5'])
        self.assertIn('No other runtime of its major', result.things_to_check[0].text)

    def test_the_report_names_the_runtime_of_the_same_major_that_can_run_the_tests(self) -> None:
        listings = self.listings_where_the_leg_runtime_lists_only_iphone_duo()
        listings['macos-26']['runtimes'].append(runtime_listing_one_screen_iphone('26.4', 'iOS-26-4', True))
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-runtime-cannot-run-tests:macos-26:26.5'])
        self.assertIn('`26.4` runtime can run them', result.things_to_check[0].text)

    def test_one_good_runtime_among_two_that_match_is_enough(self) -> None:
        listings = self.listings_where_the_leg_runtime_lists_only_iphone_duo()
        listings['macos-26']['runtimes'].append(runtime_listing_one_screen_iphone('26.5', 'iOS-26-5-generic', True))
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_the_first_match_decides_when_no_match_lists_a_one_screen_iphone_like_ios_ps1_does(self) -> None:
        listings = self.listings_where_the_leg_runtime_lists_only_iphone_duo()
        listings['macos-26']['runtimes'].append(runtime_listing_one_screen_iphone('26.5', 'iOS-26-5-other', None))
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(pinned_versions(), listings)),
                         ['ios-leg-runtime-cannot-run-tests:macos-26:26.5'])
        listings['macos-26']['runtimes'].reverse()
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_a_newer_runtime_that_the_cannot_run_item_names_is_not_offered_a_second_time(self) -> None:
        listings = self.listings_where_the_leg_runtime_lists_only_iphone_duo()
        listings['macos-26']['runtimes'].append(runtime_listing_one_screen_iphone('26.6', 'iOS-26-6', True))
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-runtime-cannot-run-tests:macos-26:26.5'])
        self.assertIn('`26.6` runtime can run them', result.things_to_check[0].text)

    def test_a_leg_named_by_identifier_or_name_is_told_which_runtime_of_its_major_can_run_the_tests(self) -> None:
        for named_by in ('com.apple.CoreSimulator.SimRuntime.iOS-27-2-iPhone19-4', 'iOS-27-2-iPhone19-4', 'iOS 27.2'):
            with self.subTest(named_by=named_by):
                listings = todays_listings()
                listings['macos-26']['xcode_apps'] += [
                    {'folder': 'Xcode_27.2.app', 'version': '27.2', 'build': '0', 'license_type': 'Release', 'symlinks': []},
                    {'folder': 'Xcode_27.app', 'version': '27.0', 'build': '0', 'license_type': 'Release', 'symlinks': []}]
                listings['macos-26']['runtimes'] = [
                    runtime_listing_one_screen_iphone('27.0', 'iOS-27-0', True),
                    runtime_listing_one_screen_iphone('27.2', 'iOS-27-2-iPhone19-4', False)]
                result = older_ios_legs.check_older_ios_legs(
                    pinned_versions(lambda text: text.replace("ios-version: '26.5'", "ios-version: '%s'" % named_by)), listings)
                cannot_run = [thing for thing in result.things_to_check if thing.key.startswith('ios-leg-runtime-cannot-run-tests')]
                self.assertEqual(len(cannot_run), 1)
                self.assertIn('`27.0` runtime can run them', cannot_run[0].text)

    def test_a_runtime_that_is_not_known_to_lack_a_one_screen_iphone_is_not_reported(self) -> None:
        listings = self.listings_where_the_leg_runtime_lists_only_iphone_duo()
        listings['macos-26']['runtimes'][0]['lists_iphone_with_one_screen'] = None
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_a_missing_runtime_is_reported_as_missing_only(self) -> None:
        listings = self.listings_where_the_leg_runtime_lists_only_iphone_duo()
        listings['macos-26']['runtimes'][0]['version'] = '26.6'
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(pinned_versions(), listings)),
                         ['ios-leg-runtime-missing:macos-26:26.5'])

    def test_a_newer_runtime_that_lists_no_one_screen_iphone_is_not_suggested(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'].append(runtime_listing_one_screen_iphone('26.6', 'iOS-26-6', False))
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])

    def test_a_newer_runtime_that_can_run_the_tests_is_still_suggested(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'].append(runtime_listing_one_screen_iphone('26.6', 'iOS-26-6', True))
        self.assertEqual(keys(older_ios_legs.check_older_ios_legs(pinned_versions(), listings)),
                         ['ios-leg-newer-runtime:macos-26:26.5:26.6'])


class XcodeCheckTests(unittest.TestCase):
    def test_an_xcode_missing_on_the_runner_is_reported_once_for_four_legs(self) -> None:
        listings = todays_listings()
        listings['macos-15']['xcode_apps'] = [a for a in listings['macos-15']['xcode_apps'] if a['version'] != '16.4']
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-xcode-missing:macos-15:16.4'])

    def test_a_beta_only_xcode_counts_as_missing(self) -> None:
        listings = todays_listings()
        for app in listings['macos-15']['xcode_apps']:
            if app['version'] == '16.4':
                app['license_type'] = 'Beta'
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-xcode-missing:macos-15:16.4'])

    def test_the_leg_xcode_is_compared_by_major_and_minor(self) -> None:
        listings = todays_listings()
        listings['macos-26']['xcode_apps'].append({'folder': 'Xcode_26.6.1.app', 'version': '26.6.1', 'build': '0',
                                                   'license_type': 'Release', 'symlinks': []})
        self.assertEqual(older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check, [])


class MissingListingTests(unittest.TestCase):
    def test_a_missing_listing_is_a_source_problem_that_names_the_runner(self) -> None:
        listings = todays_listings()
        del listings['macos-15']
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual([(p.key, p.family) for p in result.source_problems],
                         [('source:macos-runner-image:macos-15', 'older-ios-legs')])
        self.assertEqual(result.things_to_check, [])

    def test_a_failed_listing_quotes_its_error_once_for_all_its_legs(self) -> None:
        listings = todays_listings()
        listings['macos-15'] = {'status': 'error', 'error': 'no image information'}
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(len(result.source_problems), 1)
        self.assertEqual(result.source_problems[0].message, 'no image information')

    def test_the_other_runners_are_still_checked(self) -> None:
        listings = todays_listings()
        del listings['macos-15']
        listings['macos-26']['runtimes'].append(runtime('26.6'))
        result = older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(keys(result), ['ios-leg-newer-runtime:macos-26:26.5:26.6'])


class OutsideTextTests(unittest.TestCase):
    def test_runner_names_are_shown_in_code_spans(self) -> None:
        listings = todays_listings()
        listings['macos-26']['runtimes'].append(runtime('26.6'))
        text = older_ios_legs.check_older_ios_legs(pinned_versions(), listings).things_to_check[0].text
        self.assertIn('`macos-26`', text)
        self.assertIn('`26.6`', text)

    def test_the_input_is_not_changed(self) -> None:
        listings = todays_listings()
        before = copy.deepcopy(listings)
        older_ios_legs.check_older_ios_legs(pinned_versions(), listings)
        self.assertEqual(listings, before)


if __name__ == '__main__':
    unittest.main()
