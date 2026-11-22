from __future__ import annotations

import copy
import json
import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from typing import Any, Dict, List, Optional

from ensure_ci_always_tests_latest_operating_systems import macos_runner_image, xcode_changes
from ensure_ci_always_tests_latest_operating_systems.data_types import FamilyPlan, PinnedVersions
from ensure_ci_always_tests_latest_operating_systems.pinned_versions import read_pinned_versions_from_texts

from .helpers import fixture
from .test_macos_runner_image import make_applications_folder

NOW = datetime(2026, 11, 21, 12, 0, tzinfo=timezone.utc)
LIBRARY_KEY = 'CommunityToolkitLibrary_Xcode_Version'
SAMPLE_KEY = 'CommunityToolkitSample_Xcode_Version'


def repo_text(name: str) -> str:
    return fixture('repo/' + name).decode('utf-8')


def pinned_versions(apple_label: str = 'xcode-27', xcode: str = '27.2') -> PinnedVersions:
    device_tests = repo_text('device-tests.yml').replace('runs-on: xcode-27', 'runs-on: ' + apple_label)
    dotnet_build = repo_text('dotnet-build.yml').replace('xcode-27', apple_label)
    shared_env = repo_text('shared.env').replace('_Xcode_Version=27.2', '_Xcode_Version=' + xcode)
    return read_pinned_versions_from_texts(device_tests, dotnet_build, shared_env)


def listing(name: str) -> Dict[str, Any]:
    return json.loads(fixture('macos-runner-image/' + name + '.json').decode('utf-8'))


def app(folder: str, version: str, license_type: Optional[str] = 'Release', symlinks: Optional[List[str]] = None) -> Dict[str, Any]:
    return {'folder': folder, 'version': version, 'build': '0', 'license_type': license_type, 'symlinks': symlinks or []}


def rollout_state(label: str, message: str = 'Deployed') -> Dict[str, Any]:
    return {'deployed_build': label, 'message': message,
            'deploying_to': message[len('Deploying to '):] if message.startswith('Deploying to ') else None}


def readme_row(name: str, labels: List[str], key: str) -> Dict[str, Any]:
    return {'name': name, 'arch': 'arm64', 'labels': labels, 'badges': [], 'rollout_state_key': key}


def runner_images(with_xcode_28: bool = False, states: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
    rows = [readme_row('Xcode 27', ['xcode-27'], 'xcode-27-arm64'),
            readme_row('macOS 26 Arm64', ['macos-26'], 'macos-26-arm64'),
            readme_row('macOS 15 Arm64', ['macos-15'], 'macos-15-arm64')]
    rollout_states = {
        'xcode-27-arm64': rollout_state('20261104.0337', 'Deploying to 20261119.0378'),
        'macos-26-arm64': rollout_state('20261117.0516'),
        'macos-15-arm64': rollout_state('20261118.0550'),
    }
    if with_xcode_28:
        rows.insert(0, readme_row('Xcode 28', ['xcode-28'], 'xcode-28-arm64'))
        rollout_states['xcode-28-arm64'] = rollout_state('20270601.0100')
    rollout_states.update(states or {})
    return {'status': 'ok', 'rows': rows, 'rollout_states': rollout_states, 'rollout_states_error': None}


def xcode_28_listing() -> Dict[str, Any]:
    return {'status': 'ok', 'label': 'xcode-28', 'image': {'name': 'xcode-28-arm64', 'version': '20270601.0100.1'},
            'xcode_apps': [app('Xcode_28.app', '28.0', symlinks=['Xcode.app'])],
            'runtimes': [{'platform': 'iOS', 'version': '28.0', 'name': 'iOS 28.0', 'identifier': 'x', 'available': True}],
            'dotnet_sdks': ['11.0.100']}


def listings(**overrides: Any) -> Dict[str, Any]:
    result = {'xcode-27': listing('xcode-27-20261104'), 'macos-26': listing('macos-26'), 'macos-15': listing('macos-15')}
    result.update(overrides)
    return result


def required_xcode(library: str = '27.2', sample: Optional[str] = None, library_maccatalyst: Optional[str] = None,
                   published: str = '2026-11-01T00:00:00Z', set_version: str = '11.0.100.1',
                   extra_bands: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
    def band_result(band: str, ios: str, maccatalyst: Optional[str]) -> Dict[str, Any]:
        return {'status': 'ok', 'band': band, 'set_package_version': '11.100.1', 'set_version': set_version,
                'published': published, 'ios': {'manifest': '27.2.11039', 'band': '11.0.100', 'xcode': ios},
                'maccatalyst': {'manifest': '27.2.11039', 'band': '11.0.100', 'xcode': maccatalyst or ios}}

    bands = {'11.0.100': band_result('11.0.100', library, library_maccatalyst)}
    if sample is not None:
        bands['11.0.200'] = band_result('11.0.200', sample, None)
    bands.update(extra_bands or {})
    return {'status': 'ok', 'toolkit_net_version': '11.0.100', 'latest_net_version': '11.0.x', 'latest_sdk': '11.0.100',
            'library_pinned_band': '11.0.100', 'sample_band': '11.0.200' if sample is not None else '11.0.100',
            'bands': bands}


def branch_rules() -> Dict[str, Any]:
    rules = json.loads(fixture('github/rules-branches-main.json').decode('utf-8'))
    checks = []
    for rule in rules:
        if rule['type'] == 'required_status_checks':
            checks = [check['context'] for check in rule['parameters']['required_status_checks']]
    return {'status': 'ok', 'base': 'main', 'required_checks': checks, 'require_last_push_approval': True,
            'required_approving_review_count': 1}


def with_ga_xcode(source: Dict[str, Any], version: str) -> Dict[str, Any]:
    changed = copy.deepcopy(source)
    folder = 'Xcode_%s.app' % version
    changed['xcode_apps'].append(app(folder, version, symlinks=['Xcode_%s.0.app' % version]))
    return changed


def plan(pinned: Optional[PinnedVersions] = None, required: Optional[Dict[str, Any]] = None,
         the_listings: Optional[Dict[str, Any]] = None, images: Optional[Dict[str, Any]] = None,
         rules: Optional[Dict[str, Any]] = None) -> FamilyPlan:
    return xcode_changes.plan_xcode_changes(
        pinned or pinned_versions(), required or required_xcode(), the_listings or listings(),
        images or runner_images(), rules or branch_rules(), NOW)


def change_pairs(family_plan: FamilyPlan) -> List[Any]:
    return [(change.pin, change.old, change.new) for change in family_plan.safe_changes]


class GaRuleTests(unittest.TestCase):
    def test_ga_versions_of_the_real_apps_folder(self) -> None:
        with tempfile.TemporaryDirectory() as applications:
            make_applications_folder(applications)
            apps = macos_runner_image.list_xcode_apps(applications)
        self.assertEqual(xcode_changes.ga_xcode_versions({'xcode_apps': apps}), {'27.0', '27.1', '27.2'})

    def test_a_ga_named_folder_without_a_license_file_is_not_ga(self) -> None:
        listing_without_license = {'xcode_apps': [app('Xcode_27.3.app', '27.3', license_type=None)]}
        self.assertEqual(xcode_changes.ga_xcode_versions(listing_without_license), set())

    def test_a_beta_license_type_is_not_ga_in_any_case(self) -> None:
        for license_type in ('Beta', 'beta', 'Developer Beta'):
            with self.subTest(license_type=license_type):
                self.assertEqual(xcode_changes.ga_xcode_versions(
                    {'xcode_apps': [app('Xcode_27.3.app', '27.3', license_type=license_type)]}), set())

    def test_beta_and_release_candidate_folder_names_are_not_ga_even_with_a_release_license(self) -> None:
        apps = [app('Xcode_27.3_beta.app', '27.3', 'Release'), app('Xcode_27.2_Release_Candidate.app', '27.2', 'GM'),
                app('Xcode_27.2_beta_2.app', '27.2', 'Release'), app('Xcode.app', '27.1', 'Release')]
        self.assertEqual(xcode_changes.ga_xcode_versions({'xcode_apps': apps}), set())

    def test_the_version_comes_from_the_plist_not_the_folder_name(self) -> None:
        self.assertEqual(xcode_changes.ga_xcode_versions({'xcode_apps': [
            app('Xcode_26.4.1.app', '26.4.1'), app('Xcode_27.app', '27.0')]}), {'26.4', '27.0'})

    def test_the_real_readme_fixtures(self) -> None:
        self.assertEqual(xcode_changes.ga_xcode_versions(listing('xcode-27-20261104')), {'27.0', '27.1', '27.2'})
        self.assertEqual(xcode_changes.ga_xcode_versions(listing('xcode-27-20261119')), {'27.0', '27.1', '27.2'})
        self.assertIn('26.6', xcode_changes.ga_xcode_versions(listing('macos-26')))
        self.assertIn('16.4', xcode_changes.ga_xcode_versions(listing('macos-15')))


class RolloutReadyTests(unittest.TestCase):
    def test_the_deployed_build_is_ready_while_a_newer_build_is_deploying(self) -> None:
        ready, reason = xcode_changes.rollout_ready(listing('xcode-27-20261104'), runner_images()['rollout_states'])
        self.assertTrue(ready, reason)

    def test_a_listing_on_the_build_that_is_still_deploying_is_not_ready(self) -> None:
        ready, reason = xcode_changes.rollout_ready(listing('xcode-27-20261119'), runner_images()['rollout_states'])
        self.assertFalse(ready)
        self.assertIn('20261119.0378', reason)

    def test_a_listing_on_an_older_build_is_not_ready(self) -> None:
        states = {'xcode-27-arm64': rollout_state('20261119.0378')}
        self.assertFalse(xcode_changes.rollout_ready(listing('xcode-27-20261104'), states)[0])

    def test_the_listing_on_the_new_build_is_ready_once_it_is_deployed(self) -> None:
        states = {'xcode-27-arm64': rollout_state('20261119.0378')}
        self.assertTrue(xcode_changes.rollout_ready(listing('xcode-27-20261119'), states)[0])

    def test_a_missing_state_is_not_ready(self) -> None:
        ready, reason = xcode_changes.rollout_ready(listing('xcode-27-20261104'), {'xcode-27-arm64': None})
        self.assertFalse(ready)
        self.assertIn('unknown', reason)

    def test_the_state_may_be_given_as_json(self) -> None:
        states = {'macos-26-arm64': {'deployed_build': '20261117.0516', 'message': 'Deployed', 'deploying_to': None}}
        self.assertTrue(xcode_changes.rollout_ready(listing('macos-26'), states)[0])


class TodayTests(unittest.TestCase):
    def test_today_needs_no_change_and_reports_nothing(self) -> None:
        result = plan()
        self.assertEqual(result.safe_changes, [])
        self.assertEqual(result.changes_for_a_person, [])
        self.assertEqual(result.things_to_check, [])
        self.assertEqual(result.source_problems, [])
        self.assertEqual(result.deferred, [])

    def test_today_needs_nothing_even_when_the_rollout_state_is_unknown(self) -> None:
        result = plan(images=runner_images(states={'xcode-27-arm64': None}))
        self.assertEqual(result.things_to_check, [])
        self.assertEqual(result.deferred, [])


class SameLabelBumpTests(unittest.TestCase):
    def newer_xcode_listing(self, source: str) -> Dict[str, Any]:
        return with_ga_xcode(listing(source), '27.3')

    def test_both_values_change_when_the_listing_is_on_the_deployed_build(self) -> None:
        result = plan(required=required_xcode('27.3', '27.3'),
                      the_listings=listings(**{'xcode-27': self.newer_xcode_listing('xcode-27-20261104')}))
        self.assertEqual(change_pairs(result), [
            ('env:' + LIBRARY_KEY, '27.2', '27.3'), ('env:' + SAMPLE_KEY, '27.2', '27.3')])
        self.assertEqual(result.deferred, [])
        change = result.safe_changes[0]
        self.assertEqual((change.family, change.kind, change.file, change.line, change.inserted_lines),
                         ('xcode', 'set-xcode-version', '.github/workflows/shared.env', 18, []))
        self.assertEqual(result.safe_changes[1].line, 21)

    def test_the_change_waits_when_the_listing_is_on_the_deploying_build(self) -> None:
        result = plan(required=required_xcode('27.3', '27.3'),
                      the_listings=listings(**{'xcode-27': self.newer_xcode_listing('xcode-27-20261119')}))
        self.assertEqual(result.safe_changes, [])
        self.assertEqual(len(result.deferred), 1)
        self.assertIn('rollout of `xcode-27-arm64` in progress', result.deferred[0])
        self.assertEqual(result.things_to_check, [])

    def test_an_unknown_rollout_state_defers_and_says_so(self) -> None:
        result = plan(required=required_xcode('27.3', '27.3'),
                      the_listings=listings(**{'xcode-27': self.newer_xcode_listing('xcode-27-20261104')}),
                      images=runner_images(states={'xcode-27-arm64': None}))
        self.assertEqual(result.safe_changes, [])
        self.assertEqual([thing.key for thing in result.things_to_check], ['rollout-state-unknown:xcode-27-arm64'])
        self.assertTrue(result.deferred)

    def test_a_duplicated_apple_label_defers_the_xcode_change_with_a_note(self) -> None:
        images = dict(runner_images(), duplicate_labels=['xcode-27'])
        result = plan(required=required_xcode('27.3', '27.3'),
                      the_listings=listings(**{'xcode-27': self.newer_xcode_listing('xcode-27-20261104')}), images=images)
        self.assertEqual(result.safe_changes, [])
        self.assertEqual(len(result.deferred), 1)
        self.assertIn('lists `xcode-27` twice', result.deferred[0])
        self.assertTrue(any('lists `xcode-27` twice' in note for note in result.notes), result.notes)

    def test_a_duplicated_apple_label_whose_first_row_is_another_image_names_no_unknown_rollout_state(self) -> None:
        rows_without_the_xcode_image = [row for row in runner_images()['rows'] if 'xcode-27' not in row['labels']]
        images = dict(runner_images(states={'xcode-27-arm64': None}), rows=rows_without_the_xcode_image,
                      duplicate_labels=['xcode-27'])
        result = plan(required=required_xcode('27.3', '27.3'),
                      the_listings=listings(**{'xcode-27': self.newer_xcode_listing('xcode-27-20261104')}), images=images)
        self.assertEqual([thing.key for thing in result.things_to_check], [])
        self.assertEqual(len(result.deferred), 1)
        self.assertIn('lists `xcode-27` twice', result.deferred[0])

    def test_a_duplicated_label_that_is_not_involved_defers_nothing(self) -> None:
        images = dict(runner_images(), duplicate_labels=['macos-15'])
        result = plan(required=required_xcode('27.3', '27.3'),
                      the_listings=listings(**{'xcode-27': self.newer_xcode_listing('xcode-27-20261104')}), images=images)
        self.assertEqual(len(result.safe_changes), 2)
        self.assertEqual(result.deferred, [])

    def test_a_beta_only_xcode_is_not_on_the_image(self) -> None:
        states = {'xcode-27-arm64': rollout_state('20261119.0378')}
        result = plan(required=required_xcode('27.3', '27.3'),
                      the_listings=listings(**{'xcode-27': listing('xcode-27-20261119')}), images=runner_images(states=states))
        self.assertEqual(result.safe_changes, [])
        self.assertEqual([thing.key for thing in result.things_to_check],
                         ['xcode-not-on-image:27.3:xcode-27'])
        text = result.things_to_check[0].text
        self.assertIn('`Xcode_27.3_beta.app` on `xcode-27`', text)
        self.assertIn('`xcode-27` has GA 27.0, 27.1, 27.2', text)

    def test_a_release_candidate_only_xcode_is_not_on_the_image(self) -> None:
        with_candidate = listing('xcode-27-20261104')
        with_candidate['xcode_apps'].append(app('Xcode_27.3_Release_Candidate.app', '27.3', 'GM', ['Xcode_27.3.app']))
        result = plan(required=required_xcode('27.3', '27.3'), the_listings=listings(**{'xcode-27': with_candidate}))
        self.assertEqual([thing.key for thing in result.things_to_check], ['xcode-not-on-image:27.3:xcode-27'])
        self.assertIn('`Xcode_27.3_Release_Candidate.app` on `xcode-27`', result.things_to_check[0].text)

    def test_the_pinned_xcode_missing_from_the_image_is_its_own_item(self) -> None:
        without_27_2 = listing('xcode-27-20261104')
        without_27_2['xcode_apps'] = [a for a in without_27_2['xcode_apps'] if a['version'] != '27.2']
        result = plan(the_listings=listings(**{'xcode-27': without_27_2}))
        self.assertEqual([thing.key for thing in result.things_to_check],
                         ['xcode-missing-on-image:xcode-27:27.2'])
        self.assertIn('does not have as a GA release', result.things_to_check[0].text)

    def test_ios_and_mac_catalyst_needing_different_xcodes_skips_that_sdk(self) -> None:
        result = plan(required=required_xcode('27.3', library_maccatalyst='27.2'),
                      the_listings=listings(**{'xcode-27': self.newer_xcode_listing('xcode-27-20261104')}))
        self.assertEqual(result.safe_changes, [])
        self.assertEqual([thing.key for thing in result.things_to_check],
                         ['xcode-ios-and-maccatalyst-differ:library:11.0.100.1:27.3:27.2',
                          'xcode-ios-and-maccatalyst-differ:sample:11.0.100.1:27.3:27.2'])

    def test_the_library_uses_the_band_the_image_preinstalls(self) -> None:
        the_listing = self.newer_xcode_listing('xcode-27-20261104')
        the_listing['dotnet_sdks'].append('11.0.203')
        band_200_newer = {'status': 'ok', 'band': '11.0.200', 'set_package_version': '11.200.1',
                          'set_version': '11.0.200.1', 'published': '2026-11-01T00:00:00Z',
                          'ios': {'manifest': 'm', 'band': '11.0.100', 'xcode': '27.3'},
                          'maccatalyst': {'manifest': 'm', 'band': '11.0.100', 'xcode': '27.3'}}
        result = plan(required=required_xcode('27.2', extra_bands={'11.0.200': band_200_newer}),
                      the_listings=listings(**{'xcode-27': the_listing}))
        self.assertEqual(change_pairs(result), [('env:' + LIBRARY_KEY, '27.2', '27.3')])
        self.assertTrue(any('11.0.200' in note for note in result.notes))

    def test_an_image_band_without_a_readable_set_falls_back_to_the_pinned_band(self) -> None:
        the_listing = listing('xcode-27-20261104')
        the_listing['dotnet_sdks'].append('11.0.203')
        result = plan(required=required_xcode('27.2'), the_listings=listings(**{'xcode-27': the_listing}))
        self.assertEqual(result.safe_changes, [])
        self.assertTrue(any('11.0.200' in note and '11.0.100' in note for note in result.notes))


class DowngradeTests(unittest.TestCase):
    def run_with_publication(self, hours_ago: int) -> FamilyPlan:
        published = (NOW - timedelta(hours=hours_ago)).strftime('%Y-%m-%dT%H:%M:%S.0000000Z')
        return plan(required=required_xcode('27.1', '27.1', published=published))

    def test_a_set_published_three_hours_ago_waits(self) -> None:
        result = self.run_with_publication(3)
        self.assertEqual(result.safe_changes, [])
        self.assertEqual(len(result.deferred), 1)
        self.assertIn('less than 24 hours ago', result.deferred[0])

    def test_a_set_published_thirty_hours_ago_is_followed(self) -> None:
        result = self.run_with_publication(30)
        self.assertEqual(change_pairs(result), [
            ('env:' + LIBRARY_KEY, '27.2', '27.1'), ('env:' + SAMPLE_KEY, '27.2', '27.1')])
        self.assertEqual(result.deferred, [])


class LabelSwitchTests(unittest.TestCase):
    def switch_listings(self) -> Dict[str, Any]:
        old_image = listing('xcode-27-20261104')
        old_image['runtimes'].append({'platform': 'iOS', 'version': '27.3', 'name': 'iOS 27.3', 'identifier': 'x',
                                      'available': True})
        return listings(**{'xcode-27': old_image, 'xcode-28': xcode_28_listing()})

    def test_the_major_switch_is_one_change_for_a_person(self) -> None:
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(),
                      images=runner_images(with_xcode_28=True))
        self.assertEqual(result.safe_changes, [])
        self.assertEqual(result.things_to_check, [])
        self.assertEqual(len(result.changes_for_a_person), 1)
        change = result.changes_for_a_person[0]
        self.assertEqual(change.key, 'xcode-label:xcode-27:xcode-28')
        self.assertEqual(change.title, 'Move the Apple jobs from `xcode-27` to `xcode-28`')
        self.assertEqual(change.patch_file, 'patches/xcode-label_xcode-27_xcode-28.patch')
        kinds = sorted(c.kind for c in change.changes)
        self.assertEqual(kinds, ['insert-ios-leg'] + ['replace-runner-label'] * 5 + ['set-xcode-version'] * 2)

    def test_a_duplicated_target_label_defers_the_switch(self) -> None:
        images = dict(runner_images(with_xcode_28=True), duplicate_labels=['xcode-28'])
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(), images=images)
        self.assertEqual((result.safe_changes, result.changes_for_a_person), ([], []))
        self.assertTrue(any('lists `xcode-28` twice' in reason for reason in result.deferred), result.deferred)

    def test_the_patch_edits_hold_the_values_the_new_label_needs(self) -> None:
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(),
                      images=runner_images(with_xcode_28=True))
        changes = result.changes_for_a_person[0].changes
        env_changes = [(c.pin, c.old, c.new, c.line) for c in changes if c.kind == 'set-xcode-version']
        self.assertEqual(env_changes, [('env:' + LIBRARY_KEY, '27.2', '28.0', 18), ('env:' + SAMPLE_KEY, '27.2', '28.0', 21)])
        label_changes = [(c.file.split('/')[-1], c.line, c.old, c.new) for c in changes if c.kind == 'replace-runner-label']
        self.assertEqual(sorted(label_changes), sorted([
            ('device-tests.yml', 551, 'xcode-27', 'xcode-28'), ('device-tests.yml', 673, 'xcode-27', 'xcode-28'),
            ('device-tests.yml', 905, 'xcode-27', 'xcode-28'), ('dotnet-build.yml', 57, 'xcode-27', 'xcode-28'),
            ('dotnet-build.yml', 101, 'xcode-27', 'xcode-28')]))
        self.assertEqual(sorted(c.pin for c in changes if c.kind == 'replace-runner-label'), sorted([
            'apple-label:.github/workflows/device-tests.yml:device_tests_ios',
            'apple-label:.github/workflows/device-tests.yml:device_tests_ios_app',
            'apple-label:.github/workflows/device-tests.yml:device_tests_maccatalyst',
            'apple-label:.github/workflows/dotnet-build.yml:build_sample',
            'apple-label:.github/workflows/dotnet-build.yml:build_library']))

    def test_the_old_ios_major_keeps_a_leg_on_the_old_label(self) -> None:
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(),
                      images=runner_images(with_xcode_28=True))
        leg = [c for c in result.changes_for_a_person[0].changes if c.kind == 'insert-ios-leg'][0]
        self.assertEqual((leg.file, leg.line, leg.new, leg.pin), ('.github/workflows/device-tests.yml', 776, '27.3', 'ios-leg:27'))
        self.assertEqual(leg.inserted_lines, [
            "                    -   ios-version: '27.3'",
            '                        runner: xcode-27',
            "                        xcode: '27.2'"])

    def test_the_steps_name_both_required_checks_and_their_new_names(self) -> None:
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(),
                      images=runner_images(with_xcode_28=True))
        steps = '\n'.join(result.changes_for_a_person[0].steps)
        self.assertIn('`Build Library (xcode-27)` with `Build Library (xcode-28)`', steps)
        self.assertIn('`Build Sample App using Latest .NET SDK (xcode-27)` with '
                      '`Build Sample App using Latest .NET SDK (xcode-28)`', steps)
        self.assertNotIn('iOS 26.5', steps)
        self.assertIn('git apply -3', steps)
        self.assertIn('Another maintainer approves', steps)
        self.assertEqual(
            "Workload set `11.0.100.1` needs Xcode 28.0, which only `xcode-28` has as a GA release. The bot doesn't make this "
            'change itself: required checks are named after the label.', result.changes_for_a_person[0].why)

    def test_the_why_names_each_sdk_when_the_two_workload_sets_differ(self) -> None:
        sets = required_xcode('28.0', '28.0')
        sets['bands']['11.0.200']['set_version'] = '11.0.200.1'
        result = plan(required=sets, the_listings=self.switch_listings(), images=runner_images(with_xcode_28=True))
        self.assertEqual(
            "The library's workload set `11.0.100.1` needs Xcode 28.0 and the sample's workload set `11.0.200.1` needs Xcode 28.0, "
            "which only `xcode-28` has as a GA release. The bot doesn't make this change itself: required checks are named after "
            'the label.', result.changes_for_a_person[0].why)

    def test_a_sample_skipped_because_ios_and_mac_catalyst_differ_still_has_to_fit_the_new_label(self) -> None:
        sets = required_xcode('28.0', '28.0')
        sets['bands']['11.0.200']['maccatalyst']['xcode'] = '27.2'
        result = plan(required=sets, the_listings=self.switch_listings(), images=runner_images(with_xcode_28=True))
        self.assertEqual(result.changes_for_a_person, [])
        self.assertEqual(result.safe_changes, [])
        self.assertIn('xcode-label-cannot-host-both:xcode-27:xcode-28', [thing.key for thing in result.things_to_check])

    def test_a_sample_with_no_workload_set_for_its_band_still_has_to_fit_the_new_label(self) -> None:
        sets = required_xcode('28.0', '28.0')
        del sets['bands']['11.0.200']
        result = plan(required=sets, the_listings=self.switch_listings(), images=runner_images(with_xcode_28=True))
        self.assertEqual(result.changes_for_a_person, [])
        self.assertEqual(result.safe_changes, [])
        self.assertEqual([thing.key for thing in result.things_to_check], ['xcode-label-cannot-host-both:xcode-27:xcode-28'])

    def test_an_unknown_rollout_state_of_the_new_label_is_a_thing_to_check(self) -> None:
        images = runner_images(with_xcode_28=True, states={'xcode-28-arm64': None})
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(), images=images)
        self.assertEqual(result.changes_for_a_person, [])
        self.assertEqual([thing.key for thing in result.things_to_check], ['rollout-state-unknown:xcode-28-arm64'])
        self.assertTrue(any('the move to it waits' in reason for reason in result.deferred))

    def test_unreadable_branch_rules_still_give_steps(self) -> None:
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(),
                      images=runner_images(with_xcode_28=True),
                      rules={'status': 'error', 'error': 'x', 'required_checks': []})
        steps = '\n'.join(result.changes_for_a_person[0].steps)
        self.assertIn('could not read the `main` ruleset', steps)

    def test_a_switch_night_puts_the_other_sdks_safe_change_in_the_persons_patch(self) -> None:
        result = plan(required=required_xcode('28.0', '27.3'),
                      the_listings=listings(**{'xcode-27': with_ga_xcode(listing('xcode-27-20261104'), '27.3'),
                                               'xcode-28': dict(xcode_28_listing(), xcode_apps=[
                                                   app('Xcode_28.app', '28.0'), app('Xcode_27.3.app', '27.3')])}),
                      images=runner_images(with_xcode_28=True))
        self.assertEqual(result.safe_changes, [])
        env = [(c.pin, c.new) for c in result.changes_for_a_person[0].changes if c.kind == 'set-xcode-version']
        self.assertEqual(env, [('env:' + LIBRARY_KEY, '28.0'), ('env:' + SAMPLE_KEY, '27.3')])

    def test_the_label_cannot_host_both_when_a_final_value_is_missing_there(self) -> None:
        result = plan(required=required_xcode('28.0', '27.2'), the_listings=self.switch_listings(),
                      images=runner_images(with_xcode_28=True))
        self.assertEqual(result.changes_for_a_person, [])
        self.assertEqual([thing.key for thing in result.things_to_check],
                         ['xcode-label-cannot-host-both:xcode-27:xcode-28'])

    def test_a_label_missing_from_the_readme_is_reported_as_not_on_the_image(self) -> None:
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(),
                      images=runner_images(with_xcode_28=False))
        self.assertEqual(result.changes_for_a_person, [])
        self.assertEqual([thing.key for thing in result.things_to_check], ['xcode-not-on-image:28.0:xcode-27'])

    def test_a_target_label_that_was_not_listed_is_reported_as_not_on_the_image(self) -> None:
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=listings(),
                      images=runner_images(with_xcode_28=True))
        self.assertEqual(result.changes_for_a_person, [])
        self.assertEqual([thing.key for thing in result.things_to_check], ['xcode-not-on-image:28.0:xcode-27'])

    def test_the_move_waits_while_the_new_label_is_still_rolling_out(self) -> None:
        images = runner_images(with_xcode_28=True, states={'xcode-28-arm64': rollout_state('20270501.0001', 'Deploying to 20270601.0100')})
        result = plan(required=required_xcode('28.0', '28.0'), the_listings=self.switch_listings(), images=images)
        self.assertEqual(result.changes_for_a_person, [])
        self.assertTrue(any('xcode-28-arm64' in reason for reason in result.deferred))

    def test_the_fork_moves_from_macos_26_to_xcode_27_without_an_ios_leg(self) -> None:
        fork_listings = listings()
        fork_images = runner_images()
        result = plan(pinned=pinned_versions(apple_label='macos-26'), the_listings=fork_listings, images=fork_images)
        self.assertEqual(result.safe_changes, [])
        self.assertEqual(len(result.changes_for_a_person), 1)
        change = result.changes_for_a_person[0]
        self.assertEqual(change.key, 'xcode-label:macos-26:xcode-27')
        self.assertEqual(sorted(c.kind for c in change.changes), ['replace-runner-label'] * 5)
        self.assertEqual({(c.old, c.new) for c in change.changes}, {('macos-26', 'xcode-27')})
        self.assertEqual(result.things_to_check, [])


class SourceProblemTests(unittest.TestCase):
    def test_a_workload_set_error_is_a_source_problem_and_nothing_else(self) -> None:
        result = plan(required={'status': 'error', 'error': 'nuget is down'})
        self.assertEqual([(p.key, p.family, p.message) for p in result.source_problems],
                         [('source:workload-sets', 'xcode', 'nuget is down')])
        self.assertEqual(result.safe_changes, [])

    def test_a_missing_apple_listing_is_a_source_problem(self) -> None:
        without = listings()
        del without['xcode-27']
        result = plan(the_listings=without)
        self.assertEqual([p.key for p in result.source_problems], ['source:macos-runner-image:xcode-27'])

    def test_a_failed_apple_listing_quotes_its_error(self) -> None:
        result = plan(the_listings=listings(**{'xcode-27': {'status': 'error', 'error': 'no image information'}}))
        self.assertEqual(result.source_problems[0].message, 'no image information')

    def test_every_rollout_state_failing_is_a_source_problem(self) -> None:
        images = dict(runner_images(), rollout_states_error='gist is down')
        result = plan(images=images)
        self.assertEqual([p.key for p in result.source_problems], ['source:runner-rollout-states'])

    def test_a_readme_problem_with_no_change_needed_stays_quiet(self) -> None:
        images = {'status': 'error', 'error': 'bad readme', 'rows': [], 'rollout_states': {}, 'rollout_states_error': None}
        result = plan(images=images)
        self.assertEqual((result.source_problems, result.things_to_check, result.safe_changes), ([], [], []))

    def test_a_readme_problem_defers_a_label_switch_as_not_on_the_image(self) -> None:
        images = {'status': 'error', 'error': 'bad readme', 'rows': [], 'rollout_states': {}, 'rollout_states_error': None}
        result = plan(required=required_xcode('28.0', '28.0'), images=images)
        self.assertEqual(result.changes_for_a_person, [])
        self.assertTrue(result.deferred)

    def test_an_unreadable_readme_defers_a_needed_change_without_blaming_a_rollout_state(self) -> None:
        images = {'status': 'error', 'error': 'bad readme', 'rows': [], 'rollout_states': {}, 'rollout_states_error': None}
        result = plan(required=required_xcode('27.3', '27.3'),
                      the_listings=listings(**{'xcode-27': listing('xcode-27-20261104')}), images=images)
        self.assertEqual(result.deferred, ['the runner images README could not be read; Xcode change waits'])
        self.assertIn('The runner images README could not be read; Xcode change waits.', result.notes)
        self.assertEqual(result.things_to_check, [])


if __name__ == '__main__':
    unittest.main()
