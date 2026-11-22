from __future__ import annotations

import copy
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from typing import Any, Dict, List, Optional
from unittest import mock

from ensure_ci_always_tests_latest_operating_systems import change_plan, markers, pull_request_text, runner_labels
from ensure_ci_always_tests_latest_operating_systems.change_plan import (
    CLOSE, LEAVE_ALONE, NOTHING, OPEN_OR_UPDATE, Plan, build_plan, compute_change_list_key, write_plan_files)
from ensure_ci_always_tests_latest_operating_systems.change_validator import validate_changes
from ensure_ci_always_tests_latest_operating_systems.line_editor import apply_changes_to_texts

from .helpers import fixture
from .test_android_levels import TODAY_RELEASED_FOUR_KB, TODAY_RELEASED_SIXTEEN_KB, android_json
from .test_xcode_changes import (LIBRARY_KEY, NOW, SAMPLE_KEY, branch_rules, listing, listings, required_xcode, runner_images,
                                 with_ga_xcode, xcode_28_listing)

BASE = 'main'
BASE_SHA = '0123456789abcdef0123456789abcdef01234567'
BOT_BRANCH = 'automation/ensure-ci-always-tests-latest-operating-systems'
RUN_URL = 'https://github.com/CommunityToolkit/Maui/actions/runs/42'
BANNED_JARGON_WORD = 'fl' + 'ag'
SIXTEEN_KB_PIN = 'android-leg:google_apis_ps16k:37'
FOUR_KB_PIN = 'android-leg:google_apis:37'
SCRIPTS_FOLDER = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GIT_IS_MISSING = shutil.which('git') is None and not os.environ.get('CI')
NO_PULL_REQUEST: Dict[str, Any] = {'status': 'ok', 'open': None, 'rejected': [], 'things_to_check': []}


def open_pull_request(change_list: List[Dict[str, Any]], number: int = 5, person_commits: Optional[list] = None,
                      **extra: Any) -> Dict[str, Any]:
    state = copy.deepcopy(NO_PULL_REQUEST)
    state['open'] = {'number': number, 'author': 'a-maintainer', 'head_sha': 'f' * 40, 'change_list': change_list,
                     'person_commits': person_commits or [], 'checks_failed': False}
    state.update(extra)
    return state


def listed_change(family: str, kind: str, file: str, pin: str, old: Optional[str], new: str,
                  inserted_lines: Optional[List[str]] = None) -> Dict[str, Any]:
    return {'family': family, 'kind': kind, 'file': file, 'pin': pin, 'old': old or '', 'new': new,
            'inserted_lines': inserted_lines or []}


SIXTEEN_KB_37_3_IN_THE_PULL_REQUEST = listed_change(
    'android', 'replace-api-level', '.github/workflows/device-tests.yml', SIXTEEN_KB_PIN, '37.2', '37.3')
XCODE_27_3_IN_THE_PULL_REQUEST = listed_change(
    'xcode', 'set-xcode-version', '.github/workflows/shared.env', 'env:' + SAMPLE_KEY, '27.2', '27.3')


def android_with_sixteen_kb_37_3() -> Dict[str, Any]:
    return android_json(TODAY_RELEASED_FOUR_KB, TODAY_RELEASED_SIXTEEN_KB | {'37.3'})


def readme_rows_with_xcode_28() -> List[Dict[str, Any]]:
    rows = [row.to_json() for row in runner_labels.parse_runner_images_readme(
        fixture('runners/README-0e63d4f9d.md').decode('utf-8'))]
    rows.insert(0, {'name': 'Xcode 28', 'arch': 'arm64', 'labels': ['xcode-28'], 'badges': [],
                    'rollout_state_key': 'xcode-28-arm64'})
    return rows


def todays_runner_images(with_xcode_28: bool = False) -> Dict[str, Any]:
    rows = [row.to_json() for row in runner_labels.parse_runner_images_readme(
        fixture('runners/README-0e63d4f9d.md').decode('utf-8'))]
    states = runner_images(with_xcode_28=with_xcode_28)['rollout_states']
    if with_xcode_28:
        rows = readme_rows_with_xcode_28()
    return {'status': 'ok', 'rows': rows, 'rollout_states': states, 'rollout_states_error': None}


def read_file(path: str) -> str:
    with open(path, 'r', encoding='utf-8', newline='') as file:
        return file.read()


def write_json(folder: str, name: str, data: Any) -> None:
    os.makedirs(folder, exist_ok=True)
    with open(os.path.join(folder, name), 'w', encoding='utf-8') as file:
        json.dump(data, file)


class Night:
    """One night's inputs on disk, the way the earlier jobs leave them; every argument replaces today's value."""

    def __init__(self, test: unittest.TestCase, android: Optional[Dict[str, Any]] = None,
                 required: Optional[Dict[str, Any]] = None, images: Optional[Dict[str, Any]] = None,
                 rules: Optional[Dict[str, Any]] = None, software_lists: Optional[Dict[str, Any]] = None,
                 labels_to_inspect: Optional[List[str]] = None) -> None:
        self.folder = tempfile.mkdtemp()
        test.addCleanup(shutil.rmtree, self.folder, True)
        self.latest = os.path.join(self.folder, 'latest-versions')
        self.software = os.path.join(self.folder, 'macos-runner-images')
        self.repo = os.path.join(self.folder, 'repo')
        self.out = os.path.join(self.folder, 'plan')
        write_json(self.latest, 'android.json', android or android_json(TODAY_RELEASED_FOUR_KB, TODAY_RELEASED_SIXTEEN_KB))
        write_json(self.latest, 'required-xcode.json', required or required_xcode())
        write_json(self.latest, 'runner-images.json', images or todays_runner_images())
        write_json(self.latest, 'branch-rules.json', rules or branch_rules())
        labels = labels_to_inspect if labels_to_inspect is not None else ['xcode-27', 'macos-26', 'macos-15']
        write_json(self.latest, 'macos-labels-to-inspect.json', {'status': 'ok', 'labels': labels})
        for label, software_list in (software_lists or listings()).items():
            write_json(self.software, label + '.json', software_list)
        for name in ('device-tests.yml', 'dotnet-build.yml', 'shared.env'):
            self.write_repo_file(name, fixture('repo/' + name))

    def write_repo_file(self, name: str, content: bytes) -> None:
        path = os.path.join(self.repo, '.github', 'workflows', name)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, 'wb') as file:
            file.write(content)

    def plan(self, bump_state: Optional[Dict[str, Any]] = None, reopen_rejected: bool = False) -> Plan:
        return build_plan(self.latest, self.software, self.repo, bump_state or NO_PULL_REQUEST, BASE, BASE_SHA, BOT_BRANCH,
                          reopen_rejected, RUN_URL, NOW)

    def base_texts(self) -> Dict[str, str]:
        return {path: read_file(os.path.join(self.repo, *path.split('/'))) for path in markers.PIN_FILES}

    def git_apply_check(self, patch_path: str) -> subprocess.CompletedProcess:
        return subprocess.run(['git', 'apply', '--check', patch_path], cwd=self.repo, encoding='utf-8',
                              stdout=subprocess.PIPE, stderr=subprocess.PIPE)


def keys(items: List[Any]) -> List[str]:
    return [item.key for item in items]


def pairs(plan: Plan) -> List[Any]:
    return [(change.pin, change.old, change.new) for change in plan.safe_changes]


class TodayTests(unittest.TestCase):
    def test_today_does_nothing_and_reports_only_the_newer_ubuntu_label(self) -> None:
        plan = Night(self).plan()
        self.assertEqual(plan.pull_request_action, NOTHING)
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.changes_for_a_person, [])
        self.assertEqual(keys(plan.things_to_check), ['ubuntu-label:ubuntu-26.04'])
        self.assertEqual(plan.source_problems, [])
        self.assertEqual(plan.change_list_key, '')
        self.assertIsNone(plan.open_pull_request)
        self.assertEqual({name: state['state'] for name, state in plan.families.items()},
                         {'android': 'planned', 'xcode': 'planned', 'runner-labels': 'planned', 'older-ios-legs': 'planned'})
        self.assertEqual(plan.title, pull_request_text.TITLE_START)

    def test_every_item_key_is_a_valid_marker_key(self) -> None:
        plan = Night(self).plan()
        for key in keys(plan.things_to_check):
            markers.item_marker(key)

    def test_the_plan_round_trips_through_json(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        plan = night.plan(open_pull_request([SIXTEEN_KB_37_3_IN_THE_PULL_REQUEST]))
        self.assertEqual(Plan.from_json(json.loads(json.dumps(plan.to_json()))), plan)


class AndroidChangeTests(unittest.TestCase):
    def test_a_newer_sixteen_kb_level_becomes_a_patch_that_applies_and_validates(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        plan = night.plan()
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)
        self.assertEqual(pairs(plan), [(SIXTEEN_KB_PIN, '37.2', '37.3')])
        self.assertEqual(plan.source_problems, [])

        outputs = write_plan_files(plan, night.repo, night.out)
        self.assertEqual(outputs, {'has-source-problems': 'false', 'pull-request-action': OPEN_OR_UPDATE,
                                   'change-list-key': plan.change_list_key})
        patch_path = os.path.join(night.out, 'changes.patch')
        patch = read_file(patch_path)
        self.assertIn("-                    -   api-level: '37.2'", patch)
        if not GIT_IS_MISSING:
            result = night.git_apply_check(patch_path)
            self.assertEqual(result.returncode, 0, result.stderr)
        head_texts = apply_changes_to_texts(night.base_texts(), plan.safe_changes)
        self.assertEqual(validate_changes(night.base_texts(), head_texts), [])

    def test_the_pull_request_files_carry_the_change_list_title_and_trailer(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        plan = night.plan()
        write_plan_files(plan, night.repo, night.out)
        body = read_file(os.path.join(night.out, 'pull-request-body.md'))
        self.assertTrue(body.startswith(markers.PULL_REQUEST_MARKER + '\n'))
        self.assertEqual([(entry['pin'], entry['old'], entry['new']) for entry in markers.read_change_list(body)],
                         [(SIXTEEN_KB_PIN, '37.2', '37.3')])
        self.assertEqual(read_file(os.path.join(night.out, 'title.txt')),
                         'Update the device tests to the latest operating systems: Android API 37.3 (16 KB)')
        commit_message = read_file(os.path.join(night.out, 'commit-message.txt'))
        self.assertTrue(commit_message.endswith('\n\n' + markers.COMMIT_TRAILER))
        self.assertIn('- Android API 37, 16 KB leg (device-tests.yml): 37.2 -> 37.3', commit_message)
        self.assertIn('Android API 37, 16 KB leg', read_file(os.path.join(night.out, 'summary.md')))

    def test_the_change_list_key_is_stable_across_two_runs_and_follows_the_changes(self) -> None:
        first = Night(self, android=android_with_sixteen_kb_37_3()).plan()
        second = Night(self, android=android_with_sixteen_kb_37_3()).plan()
        other = Night(self, android=android_json(TODAY_RELEASED_FOUR_KB | {'37.1'}, TODAY_RELEASED_SIXTEEN_KB)).plan()
        self.assertRegex(first.change_list_key, r'^[0-9a-f]{12}$')
        self.assertEqual(first.change_list_key, second.change_list_key)
        self.assertNotEqual(first.change_list_key, other.change_list_key)
        self.assertEqual(compute_change_list_key([]), '')

    def test_the_change_list_key_does_not_depend_on_the_order_of_the_changes(self) -> None:
        plan = Night(self, android=android_json(TODAY_RELEASED_FOUR_KB | {'37.1'},
                                                TODAY_RELEASED_SIXTEEN_KB | {'37.3'})).plan()
        self.assertEqual(len(plan.safe_changes), 2)
        self.assertEqual(compute_change_list_key(plan.safe_changes), compute_change_list_key(plan.safe_changes[::-1]))

    def newest_android_api_level_after(self, plan: Plan, night: Night) -> str:
        head_texts = apply_changes_to_texts(night.base_texts(), plan.safe_changes)
        path = os.path.join(night.folder, 'edited-device-tests.yml')
        with open(path, 'w', encoding='utf-8', newline='') as file:
            file.write(head_texts['.github/workflows/device-tests.yml'])
        result = subprocess.run([sys.executable, '-I', os.path.join(SCRIPTS_FOLDER, 'newest-android-api-level.py'), path],
                                encoding='utf-8', stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip()

    def test_m15_the_trimmed_legs_follow_the_new_four_kb_level_after_the_inserts(self) -> None:
        night = Night(self, android=android_json(TODAY_RELEASED_FOUR_KB | {'38.0'}, TODAY_RELEASED_SIXTEEN_KB))
        plan = night.plan()
        self.assertIn(('android-leg:google_apis:38', None, '38.0'), pairs(plan))
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)
        self.assertEqual(self.newest_android_api_level_after(plan, night), '38.0')

    def test_m15_a_sixteen_kb_leg_above_the_four_kb_level_does_not_move_it(self) -> None:
        night = Night(self, android=android_json(TODAY_RELEASED_FOUR_KB | {'38.0'},
                                                 TODAY_RELEASED_SIXTEEN_KB | {'38.1'}))
        plan = night.plan()
        self.assertIn(('android-leg:google_apis_ps16k:38', None, '38.1'), pairs(plan))
        self.assertEqual(self.newest_android_api_level_after(plan, night), '38.0')


class FamilyIsolationTests(unittest.TestCase):
    def test_an_android_source_problem_carries_the_open_pull_requests_android_change_forward(self) -> None:
        night = Night(self, android={'status': 'error', 'error': 'the index is gone'})
        plan = night.plan(open_pull_request([SIXTEEN_KB_37_3_IN_THE_PULL_REQUEST]))
        self.assertEqual(pairs(plan), [(SIXTEEN_KB_PIN, '37.2', '37.3')])
        self.assertEqual(plan.safe_changes[0].source, 'carried-forward-from-the-open-pull-request')
        self.assertEqual(keys(plan.source_problems), ['source:android-system-images'])
        self.assertEqual(plan.families['android']['state'], 'problem')
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)
        self.assertEqual(plan.open_pull_request, 5)

    def test_an_android_source_problem_without_an_open_pull_request_makes_no_android_change(self) -> None:
        plan = Night(self, android={'status': 'error', 'error': 'the index is gone'}).plan()
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.pull_request_action, NOTHING)
        self.assertTrue(plan.has_source_problems)

    def test_an_xcode_deferral_does_not_stop_a_new_android_change(self) -> None:
        deploying_build = with_ga_xcode(listing('xcode-27-20261119'), '27.3')
        night = Night(self, android=android_with_sixteen_kb_37_3(), required=required_xcode('27.3', '27.3'),
                      software_lists=listings(**{'xcode-27': deploying_build}))
        plan = night.plan(open_pull_request([XCODE_27_3_IN_THE_PULL_REQUEST]))
        self.assertEqual(plan.families['xcode']['state'], 'deferred')
        self.assertEqual(pairs(plan), [(SIXTEEN_KB_PIN, '37.2', '37.3'), ('env:' + SAMPLE_KEY, '27.2', '27.3')])
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)

    def test_a_readme_problem_does_not_stop_an_android_change(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3(),
                      images={'status': 'error', 'error': 'the table moved', 'rows': [], 'rollout_states': {},
                              'rollout_states_error': None})
        plan = night.plan()
        self.assertEqual(pairs(plan), [(SIXTEEN_KB_PIN, '37.2', '37.3')])
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)
        self.assertIn('source:runner-images-readme', keys(plan.source_problems))
        self.assertEqual(plan.families['runner-labels']['state'], 'problem')
        self.assertEqual(plan.families['android']['state'], 'planned')

    def test_a_branch_rules_problem_carries_both_families_forward(self) -> None:
        night = Night(self, android=android_json(TODAY_RELEASED_FOUR_KB | {'37.1'}, TODAY_RELEASED_SIXTEEN_KB | {'37.3'}),
                      rules={'status': 'error', 'error': 'no access', 'required_checks': []})
        carried = [SIXTEEN_KB_37_3_IN_THE_PULL_REQUEST, XCODE_27_3_IN_THE_PULL_REQUEST]
        plan = night.plan(open_pull_request(carried))
        self.assertEqual(pairs(plan), [(SIXTEEN_KB_PIN, '37.2', '37.3'), ('env:' + SAMPLE_KEY, '27.2', '27.3')])
        self.assertEqual({plan.families['android']['state'], plan.families['xcode']['state']}, {'frozen'})
        self.assertEqual(keys(plan.source_problems), ['source:branch-rules'])
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)
        self.assertTrue(any('keeps its 1 android change.' in note for note in plan.notes), plan.notes)
        self.assertTrue(any('keeps its 1 xcode change.' in note for note in plan.notes), plan.notes)

    def test_a_branch_rules_problem_without_an_open_pull_request_makes_no_change(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3(),
                      rules={'status': 'error', 'error': 'no access', 'required_checks': []})
        plan = night.plan()
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.pull_request_action, NOTHING)

    def test_a_pull_request_read_problem_leaves_the_pull_request_alone_and_freezes_both_families(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        state = {'status': 'error', 'error': 'two open bump PRs: #3 and #4; close one', 'open': None, 'rejected': [],
                 'things_to_check': []}
        plan = night.plan(state)
        self.assertEqual(keys(plan.source_problems), ['source:bump-pull-request'])
        self.assertEqual(plan.pull_request_action, LEAVE_ALONE)
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.families['android']['state'], 'frozen')

    def test_a_carried_insert_that_main_now_has_a_leg_for_is_dropped_and_the_bot_does_not_blame_itself(self) -> None:
        insert = listed_change('android', 'insert-android-leg', '.github/workflows/device-tests.yml',
                               'android-leg:google_apis:37', '', '37.1')
        insert['inserted_lines'] = ['x']
        plan = Night(self, android={'status': 'error', 'error': 'gone'}).plan(open_pull_request([insert]))
        self.assertNotIn('source:change-validator', keys(plan.source_problems))
        self.assertNotEqual(plan.pull_request_action, LEAVE_ALONE)
        self.assertEqual(plan.safe_changes, [])
        self.assertTrue(any('Dropped' in note for note in plan.notes), plan.notes)

    def test_a_carried_change_whose_old_value_moved_on_main_is_dropped_with_a_note(self) -> None:
        moved = listed_change('android', 'replace-api-level', '.github/workflows/device-tests.yml', SIXTEEN_KB_PIN,
                              '37.1', '37.3')
        night = Night(self, android={'status': 'error', 'error': 'gone'})
        plan = night.plan(open_pull_request([moved]))
        self.assertEqual(plan.safe_changes, [])
        self.assertTrue(any('main now has `37.2`' in note for note in plan.notes), plan.notes)
        self.assertFalse(any('keeps its' in note for note in plan.notes), plan.notes)
        self.assertTrue(any('has no android changes to keep' in note for note in plan.notes), plan.notes)
        self.assertEqual(plan.pull_request_action, CLOSE)

    def test_an_android_problem_does_not_carry_the_xcode_change_of_the_open_pull_request(self) -> None:
        night = Night(self, android={'status': 'error', 'error': 'gone'})
        plan = night.plan(open_pull_request([XCODE_27_3_IN_THE_PULL_REQUEST]))
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.families['xcode']['state'], 'planned')
        self.assertEqual(plan.pull_request_action, CLOSE)

    def test_a_missing_software_list_is_a_source_problem_that_holds_the_xcode_changes(self) -> None:
        lists = listings()
        del lists['xcode-27']
        plan = Night(self, software_lists=lists).plan(open_pull_request([XCODE_27_3_IN_THE_PULL_REQUEST]))
        self.assertIn('source:macos-runner-image:xcode-27', keys(plan.source_problems))
        self.assertEqual(plan.families['xcode']['state'], 'problem')
        self.assertEqual(pairs(plan), [('env:' + SAMPLE_KEY, '27.2', '27.3')])

    def test_unreadable_pinned_versions_leave_the_pull_request_alone_and_run_no_planner(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        night.write_repo_file('device-tests.yml', b'name: nothing here\n')
        plan = night.plan(open_pull_request([SIXTEEN_KB_37_3_IN_THE_PULL_REQUEST]))
        self.assertEqual(keys(plan.source_problems), ['source:pinned-versions'])
        self.assertEqual(plan.pull_request_action, LEAVE_ALONE)
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.things_to_check, [])

    def test_a_validator_failure_on_the_bots_own_change_drops_every_change_and_leaves_the_pull_request_alone(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        with mock.patch.object(change_plan, 'validate_changes', return_value=['a line it should not have touched']):
            plan = night.plan(open_pull_request([]))
        self.assertEqual(keys(plan.source_problems), ['source:change-validator'])
        self.assertIn('a line it should not have touched', plan.source_problems[0].message)
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.pull_request_action, LEAVE_ALONE)

    def test_a_change_that_does_not_apply_is_a_validator_problem_not_a_crash(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        with mock.patch.object(change_plan, 'apply_changes_to_texts', side_effect=ValueError('line 9 changed')):
            plan = night.plan()
        self.assertEqual(keys(plan.source_problems), ['source:change-validator'])
        self.assertEqual(plan.pull_request_action, LEAVE_ALONE)

    def test_the_same_source_problem_from_two_planners_is_listed_once(self) -> None:
        lists = listings()
        del lists['macos-15']
        plan = Night(self, software_lists=lists).plan()
        self.assertEqual(keys(plan.source_problems).count('source:macos-runner-image:macos-15'), 1)
        self.assertEqual(plan.families['older-ios-legs']['state'], 'problem')
        self.assertEqual(plan.families['xcode']['state'], 'planned')


class RequiredCheckRoutingTests(unittest.TestCase):
    def rules_requiring_android_37_2(self) -> Dict[str, Any]:
        rules = branch_rules()
        rules['required_checks'] = rules['required_checks'] + ['Device Tests / Device Tests (Android API 37.2)']
        return rules

    def test_a_change_that_renames_a_required_check_goes_to_a_person_as_a_patch(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3(), rules=self.rules_requiring_android_37_2())
        plan = night.plan()
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.pull_request_action, NOTHING)
        self.assertEqual(keys(plan.changes_for_a_person), ['required-check-rename:android-leg:google_apis_ps16k:37:37.3'])
        markers.item_marker(plan.changes_for_a_person[0].key)
        change = plan.changes_for_a_person[0]
        self.assertIn('Device Tests / Device Tests (Android API 37.2)', change.why)
        self.assertIn('Device Tests / Device Tests (Android API 37.3)', change.why)
        self.assertEqual(change.patch_file, 'patches/required-check-rename_android-leg_google_apis_ps16k_37_37.3.patch')

        write_plan_files(plan, night.repo, night.out)
        patch_path = os.path.join(night.out, *change.patch_file.split('/'))
        self.assertIn("api-level: '37.3'", read_file(patch_path))
        if not GIT_IS_MISSING:
            result = night.git_apply_check(patch_path)
            self.assertEqual(result.returncode, 0, result.stderr)

    def test_the_steps_apply_then_replace_the_check_right_before_merging_then_another_maintainer_approves(self) -> None:
        plan = Night(self, android=android_with_sixteen_kb_37_3(), rules=self.rules_requiring_android_37_2()).plan()
        apply_step, replace_step, approve_step = plan.changes_for_a_person[0].steps
        self.assertIn('git apply required-check-rename.patch', apply_step)
        self.assertIn('git apply -3 required-check-rename.patch', apply_step)
        self.assertIn('Right before merging', replace_step)
        self.assertIn('`Device Tests / Device Tests (Android API 37.2)` with `Device Tests / Device Tests (Android API 37.3)`',
                      replace_step)
        self.assertIn('other pull requests report only the old name', replace_step)
        self.assertIn('Another maintainer approves', approve_step)
        self.assertIn('its author cannot', approve_step)

    def test_other_changes_of_the_night_still_go_to_the_pull_request(self) -> None:
        night = Night(self, android=android_json(TODAY_RELEASED_FOUR_KB | {'37.1'}, TODAY_RELEASED_SIXTEEN_KB | {'37.3'}),
                      rules=self.rules_requiring_android_37_2())
        plan = night.plan()
        self.assertEqual(pairs(plan), [(FOUR_KB_PIN, '37.0', '37.1')])
        self.assertEqual(len(plan.changes_for_a_person), 1)
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)

    def test_todays_required_checks_do_not_catch_a_new_sixteen_kb_level(self) -> None:
        plan = Night(self, android=android_with_sixteen_kb_37_3()).plan()
        self.assertEqual(plan.changes_for_a_person, [])

    def test_a_label_switch_stays_one_change_for_a_person_with_a_patch_that_applies(self) -> None:
        old_image = listing('xcode-27-20261104')
        old_image['runtimes'].append({'platform': 'iOS', 'version': '27.3', 'name': 'iOS 27.3', 'identifier': 'x',
                                      'available': True})
        night = Night(self, required=required_xcode('28.0', '28.0'), images=todays_runner_images(with_xcode_28=True),
                      software_lists=listings(**{'xcode-27': old_image, 'xcode-28': xcode_28_listing()}),
                      labels_to_inspect=['xcode-27', 'xcode-28', 'macos-26', 'macos-15'])
        plan = night.plan()
        self.assertEqual(keys(plan.changes_for_a_person), ['xcode-label:xcode-27:xcode-28'])
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.pull_request_action, NOTHING)

        write_plan_files(plan, night.repo, night.out)
        patch_path = os.path.join(night.out, 'patches', 'xcode-label_xcode-27_xcode-28.patch')
        patch = read_file(patch_path)
        self.assertIn('+runs-on: xcode-28', patch.replace('    ', '').replace('        ', ''))
        if not GIT_IS_MISSING:
            result = night.git_apply_check(patch_path)
            self.assertEqual(result.returncode, 0, result.stderr)


class RejectionTests(unittest.TestCase):
    REJECTED = [{'pin': SIXTEEN_KB_PIN, 'new': '37.3', 'pull_request': 7}]

    def state(self) -> Dict[str, Any]:
        state = copy.deepcopy(NO_PULL_REQUEST)
        state['rejected'] = copy.deepcopy(self.REJECTED)
        return state

    def test_a_change_a_person_closed_is_left_out_with_a_note_naming_the_pull_request(self) -> None:
        plan = Night(self, android=android_with_sixteen_kb_37_3()).plan(self.state())
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.pull_request_action, NOTHING)
        self.assertTrue(any('#7' in note and 'reopen-rejected' in note for note in plan.notes), plan.notes)

    def test_reopen_rejected_brings_the_change_back(self) -> None:
        plan = Night(self, android=android_with_sixteen_kb_37_3()).plan(self.state(), reopen_rejected=True)
        self.assertEqual(pairs(plan), [(SIXTEEN_KB_PIN, '37.2', '37.3')])
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)

    def test_a_rejection_of_another_value_does_not_hide_the_change(self) -> None:
        state = self.state()
        state['rejected'][0]['new'] = '37.2'
        plan = Night(self, android=android_with_sixteen_kb_37_3()).plan(state)
        self.assertEqual(pairs(plan), [(SIXTEEN_KB_PIN, '37.2', '37.3')])

    def test_a_rejected_change_is_not_carried_forward_either(self) -> None:
        night = Night(self, android={'status': 'error', 'error': 'gone'})
        state = open_pull_request([SIXTEEN_KB_37_3_IN_THE_PULL_REQUEST], rejected=copy.deepcopy(self.REJECTED))
        plan = night.plan(state)
        self.assertEqual(plan.safe_changes, [])
        self.assertEqual(plan.pull_request_action, CLOSE)


class ActionTests(unittest.TestCase):
    def test_open_or_update_when_safe_changes_remain(self) -> None:
        plan = Night(self, android=android_with_sixteen_kb_37_3()).plan(open_pull_request([]))
        self.assertEqual(plan.pull_request_action, OPEN_OR_UPDATE)
        self.assertEqual(plan.open_pull_request, 5)

    def test_close_when_no_safe_changes_remain_and_a_pull_request_is_open(self) -> None:
        plan = Night(self).plan(open_pull_request([SIXTEEN_KB_37_3_IN_THE_PULL_REQUEST]))
        self.assertEqual(plan.pull_request_action, CLOSE)
        self.assertEqual(plan.open_pull_request, 5)

    def test_nothing_when_no_safe_changes_remain_and_no_pull_request_is_open(self) -> None:
        self.assertEqual(Night(self).plan().pull_request_action, NOTHING)

    def test_leave_alone_when_a_person_committed_to_the_pull_request(self) -> None:
        person_commit = {'sha': 'a' * 40, 'login': 'someone', 'subject': 'fix typo'}
        thing = {'key': 'pull-request-has-a-person-commit:5', 'text': 'the bot no longer updates #5'}
        state = open_pull_request([], person_commits=[person_commit], things_to_check=[thing])
        plan = Night(self, android=android_with_sixteen_kb_37_3()).plan(state)
        self.assertEqual(plan.pull_request_action, LEAVE_ALONE)
        self.assertEqual(plan.safe_changes, [])
        self.assertIn('pull-request-has-a-person-commit:5', keys(plan.things_to_check))
        self.assertTrue(any('pushed nothing' in note for note in plan.notes), plan.notes)

    def test_the_pull_request_items_join_the_things_to_check_once(self) -> None:
        thing = {'key': 'pull-request-checks-failed:5', 'text': 'checks failed'}
        state = open_pull_request([], things_to_check=[thing, thing])
        plan = Night(self).plan(state)
        self.assertEqual(keys(plan.things_to_check).count('pull-request-checks-failed:5'), 1)


class WritePlanFilesTests(unittest.TestCase):
    def test_every_file_is_written_and_a_source_problem_is_reported_in_the_outputs(self) -> None:
        night = Night(self, android={'status': 'error', 'error': 'gone'})
        plan = night.plan()
        outputs = write_plan_files(plan, night.repo, night.out)
        self.assertEqual(outputs['has-source-problems'], 'true')
        self.assertEqual(outputs['pull-request-action'], NOTHING)
        for name in ('plan.json', 'changes.patch', 'pull-request-body.md', 'commit-message.txt', 'title.txt', 'summary.md'):
            self.assertTrue(os.path.exists(os.path.join(night.out, name)), name)
        self.assertEqual(read_file(os.path.join(night.out, 'changes.patch')), '')
        written = json.loads(read_file(os.path.join(night.out, 'plan.json')))
        self.assertEqual(Plan.from_json(written), plan)

    def test_the_run_summary_lists_every_part_of_the_plan(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3(), images=todays_runner_images())
        plan = night.plan()
        write_plan_files(plan, night.repo, night.out)
        summary = read_file(os.path.join(night.out, 'summary.md'))
        for text in ('Pull request action: `open-or-update`', '| android | planned |', 'ubuntu-label:ubuntu-26.04',
                     '<details><summary>changes.patch</summary>', '<details><summary>Pull request body</summary>'):
            self.assertIn(text, summary)

    def test_no_text_uses_the_banned_jargon_word(self) -> None:
        night = Night(self, android=android_with_sixteen_kb_37_3())
        write_plan_files(night.plan(), night.repo, night.out)
        for name in os.listdir(night.out):
            path = os.path.join(night.out, name)
            if os.path.isfile(path):
                self.assertNotIn(BANNED_JARGON_WORD, read_file(path).lower(), name)


if __name__ == '__main__':
    unittest.main()
