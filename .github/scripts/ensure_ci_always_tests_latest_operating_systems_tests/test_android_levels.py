from __future__ import annotations

import unittest
from typing import Any, Dict, Iterable, List, Optional, Tuple

from ensure_ci_always_tests_latest_operating_systems import android_levels
from ensure_ci_always_tests_latest_operating_systems.android_levels import (
    PLATFORMS_URL, SYSTEM_IMAGES_URL, parse_platforms, parse_system_images, plan_android_changes, read_android_index,
    stable_levels)
from ensure_ci_always_tests_latest_operating_systems.data_types import (
    AndroidLeg, AppleLabel, FamilyPlan, PinnedVersions, SafeChange)
from ensure_ci_always_tests_latest_operating_systems.errors import AndroidIndexFormatError
from ensure_ci_always_tests_latest_operating_systems.version_numbers import AndroidLevel, parse_android_level

from .helpers import FakeDownloader, fixture, system_image_entry

DEVICE_TESTS = '.github/workflows/device-tests.yml'
FOUR_KB = 'google_apis'
SIXTEEN_KB = 'google_apis_ps16k'
DASH_PREFIX = ' ' * 20 + '-   '
CONTINUATION_PREFIX = ' ' * 24

# Levels of Google's index on 2026-11-21, as the worked cases of DESIGN 4.2 list them.
TODAY_RELEASED_FOUR_KB = {'21', '22', '23', '24', '25', '26', '28', '29', '30', '31', '32', '33', '34', '35', '36',
                          '36.1', '37.0'}
TODAY_RELEASED_SIXTEEN_KB = {'35', '36', '36.1', '37.0', '37.1', '37.2'}


def level(text: str) -> AndroidLevel:
    return parse_android_level(text)


def leg(text: str, target: str, api_level_line: int, entry_end: Optional[int] = None) -> AndroidLeg:
    return AndroidLeg(level=level(text), target=target, api_level_line=api_level_line,
                      entry_start=api_level_line, entry_end=entry_end or api_level_line)


LEGS_TODAY_AT_FIXTURE_LINES = [
    leg('27', 'default', 387, 388), leg('28', FOUR_KB, 396, 397), leg('29', FOUR_KB, 398), leg('30', FOUR_KB, 399),
    leg('32', FOUR_KB, 401), leg('33', FOUR_KB, 402), leg('34', FOUR_KB, 403), leg('35', FOUR_KB, 404),
    leg('36.1', FOUR_KB, 406), leg('37.0', FOUR_KB, 411, 412), leg('37.2', SIXTEEN_KB, 415, 417),
]
COVERAGE_TODAY = ['36', '25']
AFTER_38_0_FOUR_KB_MERGED = LEGS_TODAY_AT_FIXTURE_LINES + [leg('38.0', FOUR_KB, 418)]
AFTER_38_1_SIXTEEN_KB_MERGED = AFTER_38_0_FOUR_KB_MERGED + [leg('38.1', SIXTEEN_KB, 419, 420)]
AFTER_38_0_SIXTEEN_KB_MERGED = LEGS_TODAY_AT_FIXTURE_LINES + [leg('38.0', SIXTEEN_KB, 418, 419)]


def pinned_versions(legs: List[AndroidLeg], coverage: Iterable[str] = COVERAGE_TODAY) -> PinnedVersions:
    return PinnedVersions(
        shared_env={}, android_legs=list(legs), coverage_levels=[level(text) for text in coverage], ios_legs=[],
        apple_label=AppleLabel(label='xcode-27', occurrences=[]), runner_labels={}, android_include_line=385,
        ios_include_line=0, android_dash_prefix=DASH_PREFIX, ios_dash_prefix='')


def android_json(released_four_kb: Iterable[str], released_sixteen_kb: Iterable[str],
                 four_kb_with_platform: Optional[Iterable[str]] = None,
                 sixteen_kb_with_platform: Optional[Iterable[str]] = None) -> Dict[str, Any]:
    released_four_kb, released_sixteen_kb = sorted(released_four_kb), sorted(released_sixteen_kb)
    return {
        'status': 'ok',
        'stable': {FOUR_KB: released_four_kb, SIXTEEN_KB: released_sixteen_kb},
        'with_platform': {
            FOUR_KB: sorted(four_kb_with_platform) if four_kb_with_platform is not None else released_four_kb,
            SIXTEEN_KB: sorted(sixteen_kb_with_platform) if sixteen_kb_with_platform is not None
            else released_sixteen_kb},
    }


def plan_with(legs: List[AndroidLeg], extra_four_kb: Iterable[str] = (), extra_sixteen_kb: Iterable[str] = (),
              coverage: Iterable[str] = COVERAGE_TODAY, removed_four_kb: Iterable[str] = ()) -> FamilyPlan:
    four_kb = (TODAY_RELEASED_FOUR_KB - set(removed_four_kb)) | set(extra_four_kb)
    sixteen_kb = TODAY_RELEASED_SIXTEEN_KB | set(extra_sixteen_kb)
    return plan_android_changes(pinned_versions(legs, coverage), android_json(four_kb, sixteen_kb))


def replacement(old: str, new: str, line: int, target: str) -> SafeChange:
    page_size = '4 KB' if target == FOUR_KB else '16 KB'
    major = level(old).major
    return SafeChange(
        family='android', kind='replace-api-level', file=DEVICE_TESTS, line=line, old=old, new=new, inserted_lines=[],
        pin='android-leg:%s:%d' % (target, major),
        description='Move the %s Android leg from API %s to %s' % (page_size, old, new),
        source='android-system-images')


def insertion(new: str, after_line: int, target: str) -> SafeChange:
    page_size = '4 KB' if target == FOUR_KB else '16 KB'
    lines = [DASH_PREFIX + "api-level: '%s'" % new]
    if target == SIXTEEN_KB:
        lines.append(CONTINUATION_PREFIX + 'target: google_apis_ps16k')
    return SafeChange(
        family='android', kind='insert-android-leg', file=DEVICE_TESTS, line=after_line, old=None, new=new,
        inserted_lines=lines, pin='android-leg:%s:%d' % (target, level(new).major),
        description='Add a %s Android leg for API %s' % (page_size, new), source='android-system-images')


def item_keys(plan: FamilyPlan) -> List[str]:
    return [thing.key for thing in plan.things_to_check]


class ParsingTests(unittest.TestCase):
    def test_todays_index_gives_the_newest_released_levels(self) -> None:
        images = parse_system_images(fixture('android/google_apis-sys-img2-4.xml'))
        four_kb, sixteen_kb = stable_levels(images, FOUR_KB), stable_levels(images, SIXTEEN_KB)
        self.assertEqual(max(level(text) for text in four_kb).text, '37.0')
        self.assertEqual(max(level(text) for text in sixteen_kb).text, '37.2')
        self.assertEqual(four_kb, TODAY_RELEASED_FOUR_KB)
        self.assertEqual(sixteen_kb, TODAY_RELEASED_SIXTEEN_KB)

    def test_ext_canary_and_beta_images_are_left_out(self) -> None:
        images = parse_system_images(fixture('android/google_apis-sys-img2-4.xml'))
        paths = {image.path for image in images}
        for path in paths:
            self.assertNotIn('ext', path)
            self.assertNotIn('canary', path.lower())
            self.assertNotIn('beta', path)
        self.assertNotIn('37.2-beta3', ' '.join(image.level_text for image in images))
        self.assertTrue(all(image.abi == 'x86_64' for image in images))

    def test_the_older_03_index_root_parses_too(self) -> None:
        images = parse_system_images(fixture('android/google_apis-sys-img2-3.xml'))
        self.assertIn('37.0', stable_levels(images, FOUR_KB))

    def test_the_2025_01_03_capture_parses(self) -> None:
        images = parse_system_images(fixture('android/google_apis-sys-img2-3-2025-01-03.xml'))
        four_kb = stable_levels(images, FOUR_KB)
        self.assertTrue(four_kb)
        self.assertNotIn('37.0', four_kb)
        self.assertEqual(max(level(text) for text in four_kb).major, 35)

    def test_duplicate_paths_collapse_in_the_level_set(self) -> None:
        entry = system_image_entry('system-images;android-38.0;google_apis;x86_64', '38.0')
        data = self._index_with(entry + entry)
        images = parse_system_images(data)
        self.assertEqual(len([image for image in images if image.level_text == '38.0']), 2)
        self.assertEqual(len({text for text in stable_levels(images, FOUR_KB) if text == '38.0'}), 1)

    def test_blank_and_missing_codenames_are_released_but_canary_is_not(self) -> None:
        released_blank = system_image_entry('system-images;android-38.0;google_apis;x86_64', '38.0', codename='')
        released_spaces = system_image_entry('system-images;android-38.1;google_apis;x86_64', '38.1', codename='  ')
        released_absent = system_image_entry('system-images;android-38.2;google_apis;x86_64', '38.2')
        preview = system_image_entry('system-images;android-38.3;google_apis;x86_64', '38.3', codename='CANARY')
        images = parse_system_images(self._index_with(released_blank + released_spaces + released_absent + preview))
        four_kb = stable_levels(images, FOUR_KB)
        self.assertTrue({'38.0', '38.1', '38.2'} <= four_kb)
        self.assertNotIn('38.3', four_kb)

    def test_obsolete_images_are_skipped(self) -> None:
        entry = system_image_entry('system-images;android-38.0;google_apis;x86_64', '38.0', obsolete=True)
        images = parse_system_images(self._index_with(entry))
        self.assertNotIn('38.0', stable_levels(images, FOUR_KB))

    def test_the_level_comes_from_the_path_not_the_api_level_element(self) -> None:
        entry = system_image_entry('system-images;android-38;google_apis;x86_64', '99.9')
        images = parse_system_images(self._index_with(entry))
        self.assertIn('38', stable_levels(images, FOUR_KB))
        self.assertNotIn('99.9', stable_levels(images, FOUR_KB))

    def test_a_wrong_root_is_a_format_error(self) -> None:
        with self.assertRaises(AndroidIndexFormatError):
            parse_system_images(fixture('android/repository2-3.xml'))
        with self.assertRaises(AndroidIndexFormatError):
            parse_platforms(fixture('android/google_apis-sys-img2-4.xml'))

    def test_an_index_without_packages_is_a_format_error(self) -> None:
        with self.assertRaises(AndroidIndexFormatError):
            parse_system_images(b'<?xml version="1.0"?><sys-img:sdk-sys-img xmlns:sys-img="urn:x"/>')
        with self.assertRaises(AndroidIndexFormatError):
            parse_platforms(b'<sdk-repository/>')

    def test_bad_xml_is_a_format_error(self) -> None:
        with self.assertRaises(AndroidIndexFormatError):
            parse_system_images(b'<sdk-sys-img><remotePackage')

    def test_platforms_accept_an_empty_codename_and_reject_previews_and_obsolete_packages(self) -> None:
        data = (
            b'<?xml version="1.0"?><sdk:sdk-repository xmlns:sdk="http://schemas.android.com/repository/android/common/02">'
            b'<remotePackage path="platforms;android-37.0"><type-details><codename></codename></type-details></remotePackage>'
            b'<remotePackage path="platforms;android-37.1"><type-details><api-level>37</api-level></type-details></remotePackage>'
            b'<remotePackage path="platforms;android-34"><type-details><codename>UpsideDownCake</codename></type-details></remotePackage>'
            b'<remotePackage obsolete="true" path="platforms;android-33"><type-details></type-details></remotePackage>'
            b'<remotePackage path="platform-tools"><type-details></type-details></remotePackage>'
            b'</sdk:sdk-repository>')
        self.assertEqual(parse_platforms(data), {'37.0', '37.1'})

    def test_todays_platforms_cover_every_released_level_the_planner_could_pick(self) -> None:
        platforms = {level(text) for text in parse_platforms(fixture('android/repository2-3.xml'))}
        for text in ('36.1', '37.0', '37.1', '37.2'):
            self.assertIn(level(text), platforms)

    def test_read_android_index_gives_the_json_the_planner_reads(self) -> None:
        downloader = FakeDownloader({SYSTEM_IMAGES_URL: fixture('android/google_apis-sys-img2-4.xml'),
                                     PLATFORMS_URL: fixture('android/repository2-3.xml')})
        result = read_android_index(downloader)
        self.assertEqual(result['status'], 'ok')
        self.assertEqual(result['stable'][FOUR_KB][-1], '37.0')
        self.assertEqual(result['stable'][SIXTEEN_KB][-1], '37.2')
        self.assertEqual(set(result['with_platform'][FOUR_KB]), TODAY_RELEASED_FOUR_KB)
        self.assertEqual(downloader.urls_requested(), [SYSTEM_IMAGES_URL, PLATFORMS_URL])
        self.assertTrue(all(call['max_bytes'] == 20_000_000 for call in downloader.calls))

    def test_read_android_index_keeps_a_level_without_a_platform_out_of_with_platform(self) -> None:
        index = fixture('android/google_apis-sys-img2-4.xml').decode('utf-8').replace(
            '</sys-img:sdk-sys-img>',
            system_image_entry('system-images;android-37.3;google_apis_ps16k;x86_64', '37.3')
            + '</sys-img:sdk-sys-img>')
        downloader = FakeDownloader({SYSTEM_IMAGES_URL: index, PLATFORMS_URL: fixture('android/repository2-3.xml')})
        result = read_android_index(downloader)
        self.assertIn('37.3', result['stable'][SIXTEEN_KB])
        self.assertNotIn('37.3', result['with_platform'][SIXTEEN_KB])

    @staticmethod
    def _index_with(entries: str) -> bytes:
        text = fixture('android/google_apis-sys-img2-4.xml').decode('utf-8')
        return text.replace('</sys-img:sdk-sys-img>', entries + '</sys-img:sdk-sys-img>').encode('utf-8')


class WorkedCaseTests(unittest.TestCase):
    def assert_plan(self, plan: FamilyPlan, changes: List[SafeChange], keys: List[str]) -> None:
        self.assertEqual(plan.safe_changes, changes)
        self.assertEqual(item_keys(plan), keys)
        self.assertEqual(plan.source_problems, [])

    def test_a01_today_changes_nothing_and_reports_nothing(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES)
        self.assert_plan(plan, [], [])
        self.assertEqual(plan.notes, [])
        self.assertEqual(plan.family, 'android')

    def test_a01_todays_fixtures_end_to_end_give_no_change_and_no_item(self) -> None:
        downloader = FakeDownloader({SYSTEM_IMAGES_URL: fixture('android/google_apis-sys-img2-4.xml'),
                                     PLATFORMS_URL: fixture('android/repository2-3.xml')})
        plan = plan_android_changes(pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES), read_android_index(downloader))
        self.assert_plan(plan, [], [])
        self.assertEqual(plan.notes, [])

    def test_a02_a_newer_four_kb_minor_moves_the_four_kb_leg_and_leaves_the_sixteen_kb_leg(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['37.1'])
        self.assert_plan(plan, [replacement('37.0', '37.1', 411, FOUR_KB)], [])

    def test_a03_a_four_kb_image_at_the_sixteen_kb_level_is_a_collision(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['37.2'])
        self.assert_plan(plan, [], ['android-level-collision:37.2', 'android-16kb-leg-has-4kb-image:37.2:37.2'])

    def test_a04_the_four_kb_leg_falls_back_to_the_highest_free_level(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['37.1', '37.2'])
        self.assert_plan(plan, [replacement('37.0', '37.1', 411, FOUR_KB)],
                         ['android-level-collision:37.2', 'android-16kb-leg-has-4kb-image:37.2:37.2'])

    def test_a05_a_newer_sixteen_kb_only_release_moves_the_sixteen_kb_leg(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_sixteen_kb=['37.3'])
        self.assert_plan(plan, [replacement('37.2', '37.3', 415, SIXTEEN_KB)], [])

    def test_a06_a_release_in_both_kinds_moves_the_four_kb_leg_and_warns_about_the_sixteen_kb_leg(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['37.3'], extra_sixteen_kb=['37.3'])
        self.assert_plan(plan, [replacement('37.0', '37.3', 411, FOUR_KB)],
                         ['android-level-collision:37.3', 'android-16kb-leg-has-4kb-image:37.2:37.3'])

    def test_a07_a_new_major_in_both_kinds_adds_only_the_four_kb_leg(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['38.0'], extra_sixteen_kb=['38.0'])
        self.assert_plan(plan, [insertion('38.0', 417, FOUR_KB)], [])
        self.assertEqual(len(plan.notes), 1)
        self.assertTrue(plan.notes[0].startswith('New Android major 38: the trimmed Android legs (`device_tests_android`) now boot 38.0.'))
        self.assertIn('`case "$level" in 37|37.*`', plan.notes[0])

    def test_a08_a_sixteen_kb_only_minor_of_the_new_major_goes_after_the_new_major(self) -> None:
        plan = plan_android_changes(
            pinned_versions(AFTER_38_0_FOUR_KB_MERGED),
            android_json(TODAY_RELEASED_FOUR_KB | {'38.0'}, TODAY_RELEASED_SIXTEEN_KB | {'38.0', '38.1'}))
        self.assert_plan(plan, [insertion('38.1', 418, SIXTEEN_KB)], [])
        self.assertEqual(plan.notes, [])

    def test_a09_an_older_major_still_gets_its_late_sixteen_kb_release(self) -> None:
        plan = plan_android_changes(
            pinned_versions(AFTER_38_1_SIXTEEN_KB_MERGED),
            android_json(TODAY_RELEASED_FOUR_KB | {'38.0'}, TODAY_RELEASED_SIXTEEN_KB | {'38.0', '38.1', '37.3'}))
        self.assert_plan(plan, [replacement('37.2', '37.3', 415, SIXTEEN_KB)], [])

    def test_a10_an_older_major_still_gets_its_late_four_kb_minor(self) -> None:
        plan = plan_android_changes(
            pinned_versions(AFTER_38_0_FOUR_KB_MERGED),
            android_json(TODAY_RELEASED_FOUR_KB | {'38.0', '37.1'}, TODAY_RELEASED_SIXTEEN_KB | {'38.0'}))
        self.assert_plan(plan, [replacement('37.0', '37.1', 411, FOUR_KB)], [])

    def test_a11_an_undotted_level_is_inserted_as_written(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['38'], extra_sixteen_kb=['38'])
        self.assert_plan(plan, [insertion('38', 417, FOUR_KB)], [])

    def test_a12_a_sixteen_kb_only_new_major_gets_a_sixteen_kb_leg_and_the_trimmed_legs_stay(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_sixteen_kb=['38.0'])
        self.assert_plan(plan, [insertion('38.0', 417, SIXTEEN_KB)], [])
        self.assertEqual(len(plan.notes), 1)
        self.assertIn('Android major 38', plan.notes[0])
        self.assertIn('no 4 KB image yet', plan.notes[0])
        self.assertIn('stay on 37.0', plan.notes[0])

    def test_a13_a_level_without_its_platforms_package_waits(self) -> None:
        plan = plan_android_changes(
            pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES),
            android_json(TODAY_RELEASED_FOUR_KB, TODAY_RELEASED_SIXTEEN_KB | {'37.3'},
                         sixteen_kb_with_platform=TODAY_RELEASED_SIXTEEN_KB))
        self.assert_plan(plan, [], [])
        self.assertEqual(plan.notes, ['Android 37.3 (google_apis_ps16k) waits for its platforms package.'])

    def test_a_four_kb_level_of_a_major_that_has_only_a_sixteen_kb_leg_waits_with_a_note(self) -> None:
        plan = plan_android_changes(
            pinned_versions(AFTER_38_0_SIXTEEN_KB_MERGED),
            android_json(TODAY_RELEASED_FOUR_KB | {'38.1'}, TODAY_RELEASED_SIXTEEN_KB | {'38.0'},
                         four_kb_with_platform=TODAY_RELEASED_FOUR_KB))
        self.assertEqual(plan.safe_changes, [])
        self.assertIn('Android 38.1 (google_apis) waits for its platforms package.', plan.notes)

    def test_a_sixteen_kb_only_level_of_an_existing_major_without_a_sixteen_kb_leg_waits_with_a_note(self) -> None:
        plan = plan_android_changes(
            pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES),
            android_json(TODAY_RELEASED_FOUR_KB, TODAY_RELEASED_SIXTEEN_KB | {'36.2'},
                         sixteen_kb_with_platform=TODAY_RELEASED_SIXTEEN_KB))
        self.assertEqual(plan.safe_changes, [])
        self.assertIn('Android 36.2 (google_apis_ps16k) waits for its platforms package.', plan.notes)

    def test_a_level_below_what_the_planner_would_use_gets_no_waiting_note(self) -> None:
        plan = plan_android_changes(
            pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES),
            android_json(TODAY_RELEASED_FOUR_KB | {'36.0'}, TODAY_RELEASED_SIXTEEN_KB,
                         four_kb_with_platform=TODAY_RELEASED_FOUR_KB))
        self.assertEqual(plan.notes, [])

    def test_one_level_spelled_two_ways_gives_the_same_text_whatever_the_order(self) -> None:
        for spellings in (['38', '38.0'], ['38.0', '38']):
            with self.subTest(spellings=spellings):
                self.assertEqual(android_levels._sorted_texts(spellings), ['38.0'])
                self.assertEqual(android_levels._sorted_texts(iter(spellings)), ['38.0'])

    def test_a14_a_leg_whose_image_left_the_index_is_reported(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, removed_four_kb=['36.1'])
        self.assert_plan(plan, [], ['android-leg-missing:google_apis:36.1'])

    def test_a14_an_obsolete_platform_package_does_not_raise_the_missing_leg_item(self) -> None:
        plan = plan_android_changes(
            pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES),
            android_json(TODAY_RELEASED_FOUR_KB, TODAY_RELEASED_SIXTEEN_KB,
                         four_kb_with_platform=TODAY_RELEASED_FOUR_KB - {'36.1'}))
        self.assert_plan(plan, [], [])

    def test_a15_a_truncated_index_is_a_source_problem_and_changes_nothing(self) -> None:
        truncated = {text for text in TODAY_RELEASED_FOUR_KB if level(text).major <= 34}
        plan = plan_android_changes(pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES), android_json(truncated, TODAY_RELEASED_SIXTEEN_KB))
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.things_to_check, [])
        self.assertEqual([problem.key for problem in plan.source_problems], ['source:android-system-images'])
        self.assertEqual(plan.source_problems[0].family, 'android')
        self.assertEqual(plan.source_problems[0].source, 'android-system-images')

    def test_a15_an_index_without_four_kb_images_is_a_source_problem(self) -> None:
        plan = plan_android_changes(pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES), android_json([], TODAY_RELEASED_SIXTEEN_KB))
        self.assertEqual([problem.key for problem in plan.source_problems], ['source:android-system-images'])

    def test_a15_a_frozen_sixteen_kb_list_is_a_source_problem(self) -> None:
        plan = plan_android_changes(pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES),
                                    android_json(TODAY_RELEASED_FOUR_KB, TODAY_RELEASED_SIXTEEN_KB - {'37.2'}))
        self.assertEqual([problem.key for problem in plan.source_problems], ['source:android-system-images'])
        self.assertEqual(plan.safe_changes, [])

    def test_an_index_that_could_not_be_read_is_a_source_problem_with_its_message(self) -> None:
        plan = plan_android_changes(pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES), {'status': 'error', 'error': 'no network'})
        self.assertEqual([problem.key for problem in plan.source_problems], ['source:android-system-images'])
        self.assertEqual(plan.source_problems[0].message, 'no network')

    def test_a16_a_released_level_that_the_coverage_job_uses_is_a_collision(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['37.1'], coverage=['37.1', '25'])
        self.assert_plan(plan, [], ['android-level-collision:37.1'])

    def test_a17_two_new_majors_are_inserted_in_one_run_after_the_same_line(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['38.0', '39.0'])
        self.assert_plan(plan, [insertion('38.0', 417, FOUR_KB), insertion('39.0', 417, FOUR_KB)], [])
        self.assertEqual(len(plan.notes), 1)
        self.assertIn('New Android majors 38 and 39', plan.notes[0])
        self.assertIn('now boot 39.0', plan.notes[0])

    def test_a18_a_new_major_at_a_coverage_level_is_a_collision_not_a_duplicate(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['38.0'], coverage=['38.0', '25'])
        self.assert_plan(plan, [], ['android-level-collision:38.0'])

    def test_a19_a_four_kb_image_arriving_for_a_sixteen_kb_only_major_adds_the_four_kb_leg(self) -> None:
        plan = plan_android_changes(
            pinned_versions(AFTER_38_0_SIXTEEN_KB_MERGED),
            android_json(TODAY_RELEASED_FOUR_KB | {'38.1'}, TODAY_RELEASED_SIXTEEN_KB | {'38.0'}))
        self.assert_plan(plan, [insertion('38.1', 419, FOUR_KB)], ['android-16kb-leg-has-4kb-image:38.0:38.1'])
        self.assertEqual(len(plan.notes), 1)
        self.assertIn('Android major 38', plan.notes[0])
        self.assertIn('now boot 38.1', plan.notes[0])

    def test_a20_a_four_kb_image_at_the_sixteen_kb_leg_level_is_a_collision(self) -> None:
        plan = plan_android_changes(
            pinned_versions(AFTER_38_0_SIXTEEN_KB_MERGED),
            android_json(TODAY_RELEASED_FOUR_KB | {'38.0'}, TODAY_RELEASED_SIXTEEN_KB | {'38.0'}))
        self.assert_plan(plan, [], ['android-level-collision:38.0', 'android-16kb-leg-has-4kb-image:38.0:38.0'])

    def test_both_new_levels_in_one_night_keep_the_order_the_editor_needs(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['38.0'], extra_sixteen_kb=['38.0', '38.1'])
        self.assert_plan(plan, [insertion('38.0', 417, FOUR_KB), insertion('38.1', 417, SIXTEEN_KB)], [])

    def test_a_sixteen_kb_only_level_with_no_four_kb_image_in_that_major_at_all(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_sixteen_kb=['38.1'])
        self.assert_plan(plan, [insertion('38.1', 417, SIXTEEN_KB)], [])

    def test_a_late_sixteen_kb_level_after_a_merged_new_major(self) -> None:
        plan = plan_android_changes(
            pinned_versions(AFTER_38_0_FOUR_KB_MERGED),
            android_json(TODAY_RELEASED_FOUR_KB | {'38.0'}, TODAY_RELEASED_SIXTEEN_KB | {'38.0', '37.3'}))
        self.assert_plan(plan, [replacement('37.2', '37.3', 415, SIXTEEN_KB)], [])

    def test_a_sixteen_kb_level_that_the_coverage_job_uses_is_a_collision(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_sixteen_kb=['37.3'], coverage=['37.3', '25'])
        self.assert_plan(plan, [], ['android-level-collision:37.3'])

    def test_the_plan_is_deterministic(self) -> None:
        first = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['38.0', '39.0'], extra_sixteen_kb=['38.1'])
        second = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['38.0', '39.0'], extra_sixteen_kb=['38.1'])
        self.assertEqual(first.to_json(), second.to_json())

    def test_the_plan_survives_a_json_round_trip(self) -> None:
        plan = plan_with(LEGS_TODAY_AT_FIXTURE_LINES, extra_four_kb=['38.0'], extra_sixteen_kb=['38.0'])
        self.assertEqual(FamilyPlan.from_json(plan.to_json()), plan)


class EndToEndTests(unittest.TestCase):
    def test_a_synthetic_sixteen_kb_release_flows_from_the_xml_to_a_safe_change(self) -> None:
        index = fixture('android/google_apis-sys-img2-4.xml').decode('utf-8').replace(
            '</sys-img:sdk-sys-img>',
            system_image_entry('system-images;android-37.3;google_apis_ps16k;x86_64', '37.3')
            + '</sys-img:sdk-sys-img>')
        platforms = fixture('android/repository2-3.xml').decode('utf-8').replace(
            '</sdk:sdk-repository>',
            '<remotePackage path="platforms;android-37.3"><type-details><codename></codename></type-details>'
            '</remotePackage></sdk:sdk-repository>')
        downloader = FakeDownloader({SYSTEM_IMAGES_URL: index, PLATFORMS_URL: platforms})
        plan = plan_android_changes(pinned_versions(LEGS_TODAY_AT_FIXTURE_LINES), read_android_index(downloader))
        self.assertEqual(plan.safe_changes, [replacement('37.2', '37.3', 415, SIXTEEN_KB)])


class RealReaderTests(unittest.TestCase):
    def test_the_legs_written_here_match_what_the_pinned_versions_reader_finds(self) -> None:
        from ensure_ci_always_tests_latest_operating_systems.pinned_versions import read_pinned_versions_from_texts
        pinned = read_pinned_versions_from_texts(
            fixture('repo/device-tests.yml').decode('utf-8'), fixture('repo/dotnet-build.yml').decode('utf-8'),
            fixture('repo/shared.env').decode('utf-8'))
        for expected, actual in zip(LEGS_TODAY_AT_FIXTURE_LINES, pinned.android_legs):
            self.assertEqual((expected.level.text, expected.target, expected.api_level_line, expected.entry_end),
                             (actual.level.text, actual.target, actual.api_level_line, actual.entry_end))
        self.assertEqual(len(LEGS_TODAY_AT_FIXTURE_LINES), len(pinned.android_legs))
        self.assertEqual(pinned.android_dash_prefix, DASH_PREFIX)
        plan = plan_android_changes(pinned, android_json(TODAY_RELEASED_FOUR_KB | {'38.0'}, TODAY_RELEASED_SIXTEEN_KB))
        self.assertEqual(plan.safe_changes, [insertion('38.0', 417, FOUR_KB)])


class SourceStaysAsciiTests(unittest.TestCase):
    def test_module_has_no_locale_dependence(self) -> None:
        with open(android_levels.__file__, encoding='utf-8') as source:
            text = source.read()
        self.assertNotIn('import locale', text)


if __name__ == '__main__':
    unittest.main()
