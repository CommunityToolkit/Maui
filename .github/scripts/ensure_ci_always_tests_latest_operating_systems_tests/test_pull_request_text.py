from __future__ import annotations

import unittest
from typing import Any, List, Optional

from ensure_ci_always_tests_latest_operating_systems import markers
from ensure_ci_always_tests_latest_operating_systems.change_plan import Plan
from ensure_ci_always_tests_latest_operating_systems.data_types import (
    ChangeForAPerson, SafeChange, SourceProblem, ThingToCheck)
from ensure_ci_always_tests_latest_operating_systems.pull_request_text import (
    MAX_BODY, MAX_TITLE, TITLE_START, describe_pin, render_commit_message, render_pull_request_body, render_run_summary,
    render_title)

DEVICE_TESTS = '.github/workflows/device-tests.yml'
SHARED_ENV = '.github/workflows/shared.env'
RUN_URL = 'https://github.com/CommunityToolkit/Maui/actions/runs/42'
BANNED_JARGON_WORD = 'fl' + 'ag'


def sixteen_kb_change(old: str = '37.2', new: str = '37.3') -> SafeChange:
    return SafeChange(family='android', kind='replace-api-level', file=DEVICE_TESTS, line=415, old=old, new=new,
                      inserted_lines=[], pin='android-leg:google_apis_ps16k:37',
                      description='Move the 16 KB Android leg from API %s to %s' % (old, new), source='android-system-images')


def sample_xcode_change(new: str = '27.3') -> SafeChange:
    return SafeChange(family='xcode', kind='set-xcode-version', file=SHARED_ENV, line=21, old='27.2', new=new,
                      inserted_lines=[], pin='env:CommunityToolkitSample_Xcode_Version',
                      description='Use Xcode %s for the sample build' % new,
                      source='workload-sets+macos-runner-image:xcode-27')


def library_xcode_change(new: str = '27.3') -> SafeChange:
    change = sample_xcode_change(new)
    change.pin = 'env:CommunityToolkitLibrary_Xcode_Version'
    change.line = 18
    return change


def new_four_kb_leg() -> SafeChange:
    return SafeChange(family='android', kind='insert-android-leg', file=DEVICE_TESTS, line=412, old=None, new='38.0',
                      inserted_lines=["                    -   api-level: '38.0'"], pin='android-leg:google_apis:38',
                      description='Add a 4 KB Android leg for API 38.0', source='android-system-images')


def runner_move(job: str = 'device_tests_android', old: str = 'ubuntu-24.04', new: str = 'ubuntu-26.04') -> SafeChange:
    return SafeChange(family='runner-labels', kind='replace-runner-label', file=DEVICE_TESTS, line=73, old=old, new=new,
                      inserted_lines=[], pin='runner-label:%s:%s' % (DEVICE_TESTS, job),
                      description='Move the job %s from %s to %s' % (job, old, new), source='runner-images-readme')


def plan_with(changes: List[SafeChange], notes: Optional[List[str]] = None, **extra: Any) -> Plan:
    return Plan(status='ok', run_url=RUN_URL, base='main', base_sha='a' * 40,
                bot_branch='automation/ensure-ci-always-tests-latest-operating-systems', safe_changes=changes,
                notes=notes or [], **extra)


class RunnerLabelTextTests(unittest.TestCase):
    MOVES = [runner_move('device_tests_android'), runner_move('device_tests_android_app'), runner_move('device_tests_android_runtime')]

    def test_the_pin_names_the_job(self) -> None:
        self.assertEqual(describe_pin(runner_move()), 'Runner of job device_tests_android')

    def test_three_moves_make_one_title_part(self) -> None:
        self.assertEqual(render_title(plan_with(self.MOVES)), TITLE_START + ': ubuntu-26.04 runners')

    def test_the_title_of_a_mixed_night_follows_the_family_order(self) -> None:
        plan = plan_with([sixteen_kb_change(), sample_xcode_change()] + self.MOVES)
        self.assertEqual(render_title(plan), TITLE_START + ': Android API 37.3 (16 KB), Xcode 27.3, ubuntu-26.04 runners')

    def test_a_title_with_both_ubuntu_and_windows_moves_is_cut_at_120(self) -> None:
        plan = plan_with([sixteen_kb_change(), new_four_kb_leg(), sample_xcode_change(), runner_move(),
                          runner_move('device_tests_windows', 'windows-2025', 'windows-2028')])
        title = render_title(plan)
        self.assertEqual(len(title), MAX_TITLE)
        self.assertTrue(title.endswith('...'))

    def test_the_commit_line_names_the_job_the_file_and_both_labels(self) -> None:
        self.assertIn('- Runner of job device_tests_android (device-tests.yml): ubuntu-24.04 -> ubuntu-26.04',
                      render_commit_message(plan_with([runner_move()])))

    def test_the_table_row_links_the_runner_images_readme(self) -> None:
        body = render_pull_request_body(plan_with([runner_move()]))
        self.assertIn('| `Runner of job device_tests_android` | `device-tests.yml` | `ubuntu-24.04` | `ubuntu-26.04` | '
                      '[runner images README](https://github.com/actions/runner-images#available-images) |', body)


class TitleTests(unittest.TestCase):
    def test_the_title_lists_each_change_once(self) -> None:
        plan = plan_with([sixteen_kb_change(), library_xcode_change(), sample_xcode_change()])
        self.assertEqual(render_title(plan), TITLE_START + ': Android API 37.3 (16 KB), Xcode 27.3')

    def test_a_new_leg_and_a_four_kb_change_read_plainly(self) -> None:
        self.assertEqual(render_title(plan_with([new_four_kb_leg()])), TITLE_START + ': Android API 38.0')

    def test_the_title_is_cut_at_120_characters_with_dots(self) -> None:
        changes = []
        for index in range(20):
            change = sixteen_kb_change(new='37.%d' % (index + 3))
            change.pin = 'android-leg:google_apis_ps16k:%d' % (37 + index)
            changes.append(change)
        title = render_title(plan_with(changes))
        self.assertLessEqual(len(title), MAX_TITLE)
        self.assertGreater(len(title), MAX_TITLE - 5)
        self.assertTrue(title.endswith('...'))
        self.assertTrue(title.startswith(TITLE_START + ': Android API 37.3 (16 KB)'))

    def test_without_changes_the_title_has_no_list(self) -> None:
        self.assertEqual(render_title(plan_with([])), TITLE_START)


class CommitMessageTests(unittest.TestCase):
    def test_the_message_names_each_pin_and_ends_with_the_trailer(self) -> None:
        message = render_commit_message(plan_with([sixteen_kb_change(), sample_xcode_change(), new_four_kb_leg()]))
        self.assertEqual(message, '\n'.join([
            TITLE_START,
            '',
            '- Android API 37, 16 KB leg (device-tests.yml): 37.2 -> 37.3',
            '- Sample Xcode (shared.env): 27.2 -> 27.3',
            '- Android API 38, 4 KB leg (device-tests.yml): added 38.0',
            '',
            markers.COMMIT_TRAILER]))

    def test_describe_pin_covers_every_kind_of_pin(self) -> None:
        self.assertEqual(describe_pin(library_xcode_change()), 'Library Xcode')
        self.assertEqual(describe_pin(sample_xcode_change()), 'Sample Xcode')
        ios_leg = SafeChange(family='xcode', kind='insert-ios-leg', file=DEVICE_TESTS, line=1, old=None, new='27.3',
                             inserted_lines=['x'], pin='ios-leg:27', description='', source='')
        self.assertEqual(describe_pin(ios_leg), 'iOS 27 leg')
        label = SafeChange(family='xcode', kind='replace-runner-label', file=DEVICE_TESTS, line=1, old='a', new='b',
                           inserted_lines=[], pin='apple-label:%s:device_tests_ios' % DEVICE_TESTS, description='',
                           source='')
        self.assertEqual(describe_pin(label), 'Apple job device_tests_ios')


class BodyTests(unittest.TestCase):
    def test_the_change_list_round_trips_through_the_body(self) -> None:
        changes = [sixteen_kb_change(), new_four_kb_leg(), sample_xcode_change()]
        body = render_pull_request_body(plan_with(changes))
        listed = markers.read_change_list(body)
        self.assertEqual([(entry['pin'], entry['old'], entry['new'], entry['inserted_lines']) for entry in listed],
                         [(change.pin, change.old or '', change.new, change.inserted_lines) for change in changes])

    def test_the_body_follows_the_pull_request_template(self) -> None:
        body = render_pull_request_body(plan_with([sixteen_kb_change()], notes=['A note.']))
        lines = body.split('\n')
        self.assertEqual(lines[0], markers.PULL_REQUEST_MARKER)
        for heading in ('### Description of Change ###', '### Linked Issues ###', '### Additional information ###'):
            self.assertEqual(lines.count(heading), 1, heading)
        self.assertLess(body.index('### Description of Change ###'), body.index('### Linked Issues ###'))
        self.assertLess(body.index('### Linked Issues ###'), body.index('### Additional information ###'))
        self.assertIn('None: an automated bump.', body)
        self.assertIn('- A note.', body)
        self.assertIn('[run](%s)' % RUN_URL, body)
        self.assertIn('(https://github.com/CommunityToolkit/Maui/blob/main/.github/workflows/%s)' % markers.WORKFLOW_FILE, body)
        self.assertIn('| `Android API 37, 16 KB leg` | `device-tests.yml` | `37.2` | `37.3` | [sys-img2-4.xml](', body)
        self.assertNotIn('Fixes #', body)

    def test_table_cells_go_through_markdown_code(self) -> None:
        change = sixteen_kb_change(old='37.2')
        change.new = '3|`7`\n@someone <b>'
        body = render_pull_request_body(plan_with([change]))
        row = [line for line in body.split('\n') if line.startswith('| `Android API 37')][0]
        self.assertIn("`3\\|'7' @someone <b>`", row)
        self.assertEqual(row.count('|') - row.count('\\|'), 6)

    def test_an_unknown_source_is_shown_as_code(self) -> None:
        change = sixteen_kb_change()
        change.source = 'x|y'
        self.assertIn('`x\\|y`', render_pull_request_body(plan_with([change])))

    def test_a_xcode_change_shows_the_workload_set_the_xcode_version_and_the_image_build(self) -> None:
        change = sample_xcode_change()
        change.source = 'workload-sets:11.0.100.2+macos-runner-image:xcode-27@20261204.0400'
        row = [line for line in render_pull_request_body(plan_with([change])).split('\n')
               if line.startswith('| `Sample Xcode')][0]
        self.assertIn('workload set `11.0.100.2`; Xcode 27.3 GA on `xcode-27` `20261204.0400`', row)

    def test_a_carried_change_says_where_it_came_from(self) -> None:
        change = sixteen_kb_change()
        change.source = 'carried-forward-from-the-open-pull-request'
        self.assertIn('carried forward from the open pull request', render_pull_request_body(plan_with([change])))

    def test_a_body_over_the_limit_is_cut_with_a_note_and_keeps_its_markers(self) -> None:
        notes = ['A long note %d: %s' % (index, 'x' * 500) for index in range(200)]
        body = render_pull_request_body(plan_with([sixteen_kb_change()], notes=notes))
        self.assertLessEqual(len(body), MAX_BODY)
        self.assertIn('was cut to fit', body)
        self.assertTrue(body.startswith(markers.PULL_REQUEST_MARKER))
        self.assertEqual(len(markers.read_change_list(body)), 1)

    def test_a_body_under_the_limit_is_not_cut(self) -> None:
        self.assertNotIn('was cut', render_pull_request_body(plan_with([sixteen_kb_change()])))

    def test_a_run_url_without_the_actions_path_gives_a_plain_workflow_name(self) -> None:
        plan = plan_with([sixteen_kb_change()])
        plan.run_url = 'not a url'
        self.assertIn('nightly ' + markers.WORKFLOW_FILE + ' found', render_pull_request_body(plan))

    def test_the_body_mentions_that_the_merge_starts_no_build_on_main(self) -> None:
        body = render_pull_request_body(plan_with([sixteen_kb_change()]))
        self.assertIn('starts no Build run on `main`', body)

    def test_the_body_never_uses_the_banned_jargon_word(self) -> None:
        self.assertNotIn(BANNED_JARGON_WORD, render_pull_request_body(plan_with([sixteen_kb_change()])).lower())


class RunSummaryTests(unittest.TestCase):
    def test_the_summary_lists_each_group_with_its_keys(self) -> None:
        plan = plan_with([sixteen_kb_change()], notes=['A note.'], pull_request_action='open-or-update',
                         open_pull_request=5,
                         families={'android': {'state': 'planned', 'reason': ''},
                                   'xcode': {'state': 'deferred', 'reason': 'rollout in progress'}},
                         things_to_check=[ThingToCheck('runner-label-deprecated:macos-15', 'A deprecated label.')],
                         source_problems=[SourceProblem('source:branch-rules', 'branch-rules', 'no access `x`', '')],
                         changes_for_a_person=[ChangeForAPerson('xcode-label:xcode-27:xcode-28', 'Move to xcode-28', 'why',
                                                                [], [], 'patches/x.patch')])
        summary = render_run_summary(plan, changes_patch='diff text', pull_request_body='body text')
        for text in ('`open-or-update` (open pull request #5)', '| xcode | deferred | `rollout in progress` |',
                     '`runner-label-deprecated:macos-15`: A deprecated label.', "`source:branch-rules`: `no access 'x'`",
                     '`xcode-label:xcode-27:xcode-28`: `Move to xcode-28`', '- A note.', 'diff text', 'body text'):
            self.assertIn(text, summary)

    def test_a_patch_holding_backticks_gets_a_longer_fence(self) -> None:
        summary = render_run_summary(plan_with([]), changes_patch='before\n```\nafter')
        self.assertIn('````diff', summary)

    def test_a_body_that_is_never_posted_stays_out_of_the_summary(self) -> None:
        for action in ('leave-alone', 'nothing'):
            summary = render_run_summary(plan_with([], pull_request_action=action), pull_request_body='body text')
            self.assertNotIn('Pull request body', summary, action)
            self.assertNotIn('body text', summary, action)

    def test_a_body_that_is_posted_shows_in_the_summary(self) -> None:
        summary = render_run_summary(plan_with([], pull_request_action='open-or-update'), pull_request_body='body text')
        self.assertIn('Pull request body', summary)
        self.assertIn('body text', summary)

    def test_an_empty_plan_says_none_everywhere(self) -> None:
        summary = render_run_summary(plan_with([]))
        self.assertEqual(summary.count('None.'), 5)
        self.assertNotIn('<details>', summary)


if __name__ == '__main__':
    unittest.main()
