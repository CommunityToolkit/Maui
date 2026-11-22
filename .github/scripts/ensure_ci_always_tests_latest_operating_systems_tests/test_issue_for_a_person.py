from __future__ import annotations

import unittest
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from ensure_ci_always_tests_latest_operating_systems import issue_for_a_person as module
from ensure_ci_always_tests_latest_operating_systems import token_owner
from ensure_ci_always_tests_latest_operating_systems.errors import GitHubApiError
from ensure_ci_always_tests_latest_operating_systems.markers import (CLOSED_BY_BOT_MARKER, ISSUE_MARKER, ISSUE_TITLE,
                                                                     LABELS, PULL_REQUEST_JOB_NAME, item_marker,
                                                                     read_item_keys)
from ensure_ci_always_tests_latest_operating_systems_tests.helpers import FakeGitHub

NOW = datetime(2026, 11, 22, 3, 7, tzinfo=timezone.utc)
RUN_URL = 'https://github.com/CommunityToolkit/Maui/actions/runs/4242'
ISSUES = '/repos/o/r/issues'


class FakeRepoGitHub(FakeGitHub):
    repo = 'o/r'


def issue_entry(number: int, body: str, state: str = 'open', **extra: Any) -> Dict[str, Any]:
    return dict({'number': number, 'state': state, 'body': body}, **extra)


def body_with(*keys: str, closed_by_bot: bool = False) -> str:
    lines = [ISSUE_MARKER] + [item_marker(key) + ' text' for key in keys]
    if closed_by_bot:
        lines.append(CLOSED_BY_BOT_MARKER)
    return '\n'.join(lines)


def thing(key: str, text: str = 'Something to check.') -> Dict[str, str]:
    return {'key': key, 'text': text}


def person_change(key: str = 'xcode-label:xcode-27:xcode-28') -> Dict[str, Any]:
    return {'key': key, 'title': 'Move the Apple jobs from `xcode-27` to `xcode-28`', 'why': 'Needs Xcode 28.0.',
            'steps': ['Save the patch.', 'Wait for CI.'], 'changes': [],
            'patch_file': 'patches/%s.patch' % key.replace(':', '_')}


def plan_of(**parts: Any) -> Dict[str, Any]:
    plan = {'safe_changes': [], 'changes_for_a_person': [], 'things_to_check': [], 'source_problems': [],
            'change_list_key': 'abcdef012345'}
    plan.update(parts)
    return plan


def fake(candidates: List[Dict[str, Any]]) -> FakeRepoGitHub:
    return FakeRepoGitHub({'GET ' + ISSUES: candidates, 'POST ' + ISSUES: {'number': 77},
                           'PATCH %s/5' % ISSUES: {}, 'POST %s/5/comments' % ISSUES: {},
                           'PATCH %s/6' % ISSUES: {}, 'POST %s/6/comments' % ISSUES: {}})


class CollectItemsTests(unittest.TestCase):
    def test_plan_items_land_in_their_sections(self) -> None:
        plan = plan_of(changes_for_a_person=[person_change()], things_to_check=[thing('ubuntu-label:ubuntu-26.04')],
                       source_problems=[{'key': 'source:android-system-images', 'source': 'android-system-images',
                                         'message': 'bad `xml`', 'family': 'android'}])
        patches = {'patches/xcode-label_xcode-27_xcode-28.patch': 'diff --git a b\n'}
        items = module.collect_items(plan, None, None, None, None, None, patches)
        self.assertEqual([i.key for i in items.changes_for_a_person], ['xcode-label:xcode-27:xcode-28'])
        self.assertEqual(items.changes_for_a_person[0].patch, 'diff --git a b\n')
        self.assertEqual([i.key for i in items.things_to_check], ['ubuntu-label:ubuntu-26.04'])
        self.assertIn("`bad 'xml'`", items.source_problems[0].text)
        self.assertTrue(items.reports_a_problem_that_turns_the_run_red())

    def test_token_items_from_decision_and_approval_are_merged_without_duplicates(self) -> None:
        decision = {'token': {'usable': True}, 'things_to_check': [thing('token-expires-soon:2026-12-01')]}
        approval = {'things_to_check': [thing('pull-request-not-approved:9:threads'),
                                        thing('token-expires-soon:2026-12-01')]}
        items = module.collect_items(plan_of(), decision, approval, None, None, None)
        self.assertEqual([i.key for i in items.things_to_check],
                         ['token-expires-soon:2026-12-01', 'pull-request-not-approved:9:threads'])

    def test_unusable_token_turns_safe_changes_into_bump_not_opened(self) -> None:
        plan = plan_of(safe_changes=[{'file': '.github/workflows/shared.env', 'description': 'Xcode 27.2 -> 27.3'}])
        decision = {'token': {'usable': False, 'reason': 'missing-scope'}, 'things_to_check': []}
        items = module.collect_items(plan, decision, None, None, None, None, {'changes.patch': 'PATCH'})
        item = items.changes_for_a_person[0]
        self.assertEqual(item.key, 'bump-not-opened:abcdef012345')
        self.assertIn('lacks the `workflow` or `public_repo` scope', item.text)
        self.assertIn('`Xcode 27.2 -> 27.3`', item.text)
        self.assertEqual(item.patch, 'PATCH')
        self.assertIn('gh secret set ENSURE_CI_ALWAYS_TESTS_LATEST_OPERATING_SYSTEMS_TOKEN', item.steps[0])

    def test_every_reason_a_token_can_be_unusable_has_a_phrase_for_the_issue_item(self) -> None:
        self.assertEqual(set(token_owner.REASON_TEXTS) - set(module.TOKEN_PROBLEMS), set())

    def test_a_malformed_token_turns_safe_changes_into_bump_not_opened_with_its_own_phrase(self) -> None:
        plan = plan_of(safe_changes=[{'file': '.github/workflows/shared.env', 'description': 'Xcode 27.2 -> 27.3'}])
        decision = {'token': {'usable': False, 'reason': 'malformed'}, 'things_to_check': []}
        items = module.collect_items(plan, decision, None, None, None, None, {'changes.patch': 'PATCH'})
        self.assertIn('has a line break, a space or another character no token has', items.changes_for_a_person[0].text)

    def test_usable_token_and_no_safe_changes_gives_no_bump_item(self) -> None:
        decision = {'token': {'usable': False, 'reason': 'missing'}}
        self.assertEqual(module.collect_items(plan_of(), decision, None, None, None, None).changes_for_a_person, [])

    def test_failed_create_pull_request_gives_bump_not_pushed_with_error_line(self) -> None:
        plan = plan_of(safe_changes=[{'file': 'f', 'description': 'd'}])
        items = module.collect_items(plan, None, None, {'open_or_update': 'failure'}, None,
                                     'remote: refusing to allow a token to update workflow')
        item = next(i for i in items.changes_for_a_person if i.key.startswith('bump-not-pushed'))
        self.assertIn("`remote: refusing to allow a token to update workflow`", item.text)
        self.assertEqual([i.key for i in items.failures], ['failed-step:open-or-update'])

    def test_missing_error_line_says_to_see_the_job_log(self) -> None:
        plan = plan_of(safe_changes=[{'file': 'f', 'description': 'd'}])
        items = module.collect_items(plan, None, None, {'open_or_update': 'failure'}, None, None)
        self.assertIn('see the job log', items.changes_for_a_person[0].text)

    def test_only_failures_count_not_cancelled_or_skipped(self) -> None:
        results = {'run-unit-tests': {'result': 'failure'}, 'read-latest-versions': {'result': 'cancelled'},
                   'plan-changes': {'result': 'skipped'}, 'inspect-macos-runner-image': {'result': 'success'}}
        outcomes = {'decide': 'cancelled', 'apply': 'skipped', 'open_or_update': '', 'approve': 'failure'}
        items = module.collect_items(None, None, None, outcomes, results, None)
        self.assertEqual([i.key for i in items.failures], ['failed-job:run-unit-tests', 'failed-step:approve'])
        self.assertFalse(items.plan_available)


class ItemKeyAndSummaryTests(unittest.TestCase):
    def test_an_upper_case_key_is_cleaned_instead_of_crashing_the_render(self) -> None:
        plan = plan_of(things_to_check=[thing('runner-label-missing:macOS-15'), thing('rollout-state-unknown:Image Name_1')])
        items = module.collect_items(plan, None, None, None, None, None)
        self.assertEqual([item.key for item in items.things_to_check],
                         ['runner-label-missing:macos-15', 'rollout-state-unknown:image-name_1'])
        body = module.render_issue_body(items, RUN_URL, NOW)
        self.assertEqual(read_item_keys(body), ['runner-label-missing:macos-15', 'rollout-state-unknown:image-name_1'])

    def test_the_summary_never_cuts_inside_a_code_span(self) -> None:
        item = module.IssueItem(key='a', section='x', text='A commit `ping @octocat. thanks` was pushed. More.')
        self.assertEqual(item.summary(), 'A commit `ping @octocat. thanks` was pushed.')

    def test_the_summary_of_a_text_without_a_sentence_end_is_the_whole_text(self) -> None:
        self.assertEqual(module.IssueItem(key='a', section='x', text='Just `one. two`').summary(), 'Just `one. two`')

    def test_the_summary_of_plain_text_stops_at_the_first_sentence(self) -> None:
        self.assertEqual(module.IssueItem(key='a', section='x', text='First one. Second one.').summary(), 'First one.')


class ReadPushErrorTests(unittest.TestCase):
    JOBS = '/repos/o/r/actions/runs/4242/attempts/2/jobs'

    def github(self, log: Any, jobs: Optional[List[Dict[str, Any]]] = None) -> FakeRepoGitHub:
        jobs = jobs if jobs is not None else [{'id': 1, 'name': 'other'}, {'id': 9, 'name': PULL_REQUEST_JOB_NAME}]
        return FakeRepoGitHub({'GET ' + self.JOBS: jobs, 'GET /repos/o/r/actions/jobs/9/logs': log})

    def test_first_error_line_inside_the_create_pull_request_step(self) -> None:
        log = '\n'.join([
            '2026-11-22T03:00:00.0000000Z ##[group]Run something/else',
            '2026-11-22T03:00:01.0000000Z ##[error]not this one',
            '2026-11-22T03:00:02.0000000Z ##[group]Run peter-evans/create-pull-request@abc',
            '2026-11-22T03:00:03.0000000Z ##[endgroup]',
            '2026-11-22T03:00:04.0000000Z remote: ! refusing to allow a Personal Access Token to create or update workflow',
            '2026-11-22T03:00:05.0000000Z ##[error]Process completed',
            '2026-11-22T03:00:06.0000000Z ##[group]Run next step',
            '2026-11-22T03:00:07.0000000Z ##[error]not this either'])
        github = self.github(log)
        error = module.read_push_error_from_job_log(github, 4242, 2, PULL_REQUEST_JOB_NAME)
        self.assertEqual(error, 'remote: ! refusing to allow a Personal Access Token to create or update workflow')
        self.assertEqual(github.calls[-1].path, '/repos/o/r/actions/jobs/9/logs')

    def test_error_marker_is_removed_and_line_cut_to_300_characters(self) -> None:
        log = '##[group]Run peter-evans/create-pull-request@abc\n##[error]' + 'x' * 400
        error = module.read_push_error_from_job_log(self.github(log), 4242, 2, PULL_REQUEST_JOB_NAME)
        self.assertEqual(error, 'x' * 300)

    def test_none_when_no_line_job_or_log_is_missing(self) -> None:
        quiet = '##[group]Run peter-evans/create-pull-request@abc\nall fine'
        self.assertIsNone(module.read_push_error_from_job_log(self.github(quiet), 4242, 2, PULL_REQUEST_JOB_NAME))
        self.assertIsNone(module.read_push_error_from_job_log(self.github(quiet, []), 4242, 2, PULL_REQUEST_JOB_NAME))
        broken = GitHubApiError('GET', '/x', 410, 'gone')
        self.assertIsNone(module.read_push_error_from_job_log(self.github(broken), 4242, 2, PULL_REQUEST_JOB_NAME))


class RenderIssueBodyTests(unittest.TestCase):
    def items(self) -> module.Items:
        plan = plan_of(changes_for_a_person=[person_change()], things_to_check=[thing('ubuntu-label:ubuntu-26.04')],
                       source_problems=[{'key': 'source:branch-rules', 'source': 'branch-rules', 'message': 'boom',
                                         'family': 'x'}])
        return module.collect_items(plan, None, None, None, {'plan-changes': {'result': 'failure'}}, None,
                                    {'patches/xcode-label_xcode-27_xcode-28.patch': 'diff --git a/x b/x\n+line\n'})

    def test_four_sections_in_order_with_markers_and_header(self) -> None:
        body = module.render_issue_body(self.items(), RUN_URL, NOW)
        self.assertTrue(body.startswith(ISSUE_MARKER + '\n'))
        self.assertIn('Updated by [run 4242](%s) on 2026-11-22 03:07 UTC.' % RUN_URL, body)
        positions = [body.index('### %s ###' % name) for name in (
            'Changes a person must make', 'Things to check', 'Problems reading sources', 'Jobs that failed')]
        self.assertEqual(positions, sorted(positions))
        self.assertEqual(read_item_keys(body), ['xcode-label:xcode-27:xcode-28', 'ubuntu-label:ubuntu-26.04',
                                                'source:branch-rules', 'failed-job:plan-changes'])
        self.assertIn('1. Save the patch.\n2. Wait for CI.', body)
        self.assertIn('<details><summary>Patch</summary>\n\n```diff\ndiff --git a/x b/x', body)
        self.assertIn('Nothing was changed from this source tonight.', body)
        self.assertIn('[log](%s)' % RUN_URL, body)

    def test_empty_sections_are_left_out(self) -> None:
        items = module.collect_items(plan_of(things_to_check=[thing('a')]), None, None, None, None, None)
        body = module.render_issue_body(items, RUN_URL, NOW)
        self.assertNotIn('### Changes a person must make ###', body)
        self.assertNotIn('### Jobs that failed ###', body)

    def test_patch_containing_backticks_gets_a_longer_fence(self) -> None:
        item = module.IssueItem('k', module.SECTION_CHANGES, 'why', 'title', [], 'a ``` b')
        body = module.render_issue_body(module.Items(changes_for_a_person=[item]), RUN_URL, NOW)
        self.assertIn('````diff', body)

    def test_too_long_body_replaces_the_largest_patch_first(self) -> None:
        small = module.IssueItem('small', module.SECTION_CHANGES, 'why', 'Small', [], 'S' * 100)
        large = module.IssueItem('large', module.SECTION_CHANGES, 'why', 'Large', [], 'L' * 5000)
        items = module.Items(changes_for_a_person=[small, large])
        body = module.render_issue_body(items, RUN_URL, NOW, max_body=3000)
        self.assertIn('Too long for the issue: download the `plan` artifact of [run 4242](%s)' % RUN_URL, body)
        self.assertNotIn('LLLL', body)
        self.assertIn('S' * 100, body)
        self.assertLessEqual(len(body), 3000)

    def test_body_is_cut_as_a_last_resort(self) -> None:
        items = module.Items(things_to_check=[module.IssueItem('a', module.SECTION_THINGS_TO_CHECK, 'x' * 5000)])
        self.assertLessEqual(len(module.render_issue_body(items, RUN_URL, NOW, max_body=1000)), 1000)

    def test_default_limit_is_the_pull_request_limit(self) -> None:
        self.assertEqual(module.MAX_BODY, 60000)


class SyncIssueTests(unittest.TestCase):
    def things(self, *keys: str) -> module.Items:
        return module.Items(things_to_check=[module.IssueItem(k, module.SECTION_THINGS_TO_CHECK, 'Check %s.' % k)
                                             for k in keys])

    def test_creates_the_issue_with_title_and_labels(self) -> None:
        github = fake([])
        result = module.sync_issue(github, self.things('a'), RUN_URL, NOW)
        post = github.write_calls()[0]
        self.assertEqual((post.method, post.path), ('POST', ISSUES))
        self.assertEqual(post.body['title'], ISSUE_TITLE)
        self.assertEqual(post.body['labels'], list(LABELS))
        self.assertEqual((result['action'], result['number'], result['exit_code']), ('created', 77, 0))

    def test_nothing_to_report_and_no_open_issue_writes_nothing(self) -> None:
        github = fake([])
        result = module.sync_issue(github, module.Items(), RUN_URL, NOW)
        self.assertEqual(github.write_calls(), [])
        self.assertEqual(result['action'], 'none')

    def test_candidate_lookup_filters_and_paginates(self) -> None:
        github = fake([issue_entry(5, body_with('a'), state='closed', pull_request={'url': 'x'}),
                       issue_entry(6, 'unrelated body'), issue_entry(5, body_with('a'))])
        module.sync_issue(github, self.things('a'), RUN_URL, NOW)
        first = github.calls[0]
        self.assertEqual(first.path, ISSUES)
        self.assertEqual(first.params, {'state': 'all', 'labels': 'requirements updated',
                                        'creator': 'github-actions[bot]', 'per_page': 100})
        self.assertEqual([c.path for c in github.write_calls()], ['%s/5' % ISSUES])

    def test_update_without_new_keys_posts_no_comment(self) -> None:
        github = fake([issue_entry(5, body_with('a', 'b'))])
        result = module.sync_issue(github, self.things('a', 'b'), RUN_URL, NOW)
        self.assertEqual([(c.method, c.path) for c in github.write_calls()], [('PATCH', '%s/5' % ISSUES)])
        self.assertEqual((result['action'], result['commented']), ('updated', False))

    def test_new_items_get_exactly_one_comment(self) -> None:
        github = fake([issue_entry(5, body_with('a'))])
        result = module.sync_issue(github, self.things('a', 'b', 'c'), RUN_URL, NOW)
        comments = [c for c in github.write_calls() if c.path.endswith('/comments')]
        self.assertEqual(len(comments), 1)
        self.assertIn('Check b.', comments[0].body['body'])
        self.assertIn('Check c.', comments[0].body['body'])
        self.assertNotIn('Check a.', comments[0].body['body'])
        self.assertTrue(result['commented'])

    def test_newest_open_issue_is_the_one_updated(self) -> None:
        github = fake([issue_entry(5, body_with('a')), issue_entry(6, body_with('a'))])
        module.sync_issue(github, self.things('a'), RUN_URL, NOW)
        self.assertIn('%s/6' % ISSUES, [c.path for c in github.write_calls()])

    def test_older_open_duplicates_are_closed_by_the_bot_so_they_dismiss_nothing(self) -> None:
        github = fake([issue_entry(5, body_with('a')), issue_entry(6, body_with('a'))])
        module.sync_issue(github, self.things('a'), RUN_URL, NOW)
        older_patch = [c for c in github.write_calls() if c.path == '%s/5' % ISSUES][0]
        self.assertEqual(older_patch.body['state'], 'closed')
        self.assertEqual(older_patch.body['state_reason'], 'not_planned')
        self.assertIn(CLOSED_BY_BOT_MARKER, older_patch.body['body'])
        self.assertEqual(module._dismissed_keys([issue_entry(5, older_patch.body['body'], state='closed')]), set())

    def test_person_closed_issue_dismisses_its_items(self) -> None:
        github = fake([issue_entry(4, body_with('a'), state='closed')])
        result = module.sync_issue(github, self.things('a', 'b'), RUN_URL, NOW)
        body = github.write_calls()[0].body['body']
        self.assertEqual(read_item_keys(body), ['b'])
        self.assertEqual(result['action'], 'created')

    def test_everything_dismissed_creates_nothing(self) -> None:
        github = fake([issue_entry(4, body_with('a'), state='closed')])
        self.assertEqual(module.sync_issue(github, self.things('a'), RUN_URL, NOW)['action'], 'none')
        self.assertEqual(github.write_calls(), [])

    def test_bot_closed_issue_dismisses_nothing(self) -> None:
        github = fake([issue_entry(4, body_with('a', closed_by_bot=True), state='closed')])
        module.sync_issue(github, self.things('a'), RUN_URL, NOW)
        self.assertEqual(read_item_keys(github.write_calls()[0].body['body']), ['a'])

    def test_source_and_failed_keys_are_never_dismissed(self) -> None:
        closed = issue_entry(4, body_with('source:branch-rules', 'failed-job:plan-changes', 'failed-step:approve', 'a'),
                             state='closed')
        items = module.Items(
            things_to_check=[module.IssueItem('a', module.SECTION_THINGS_TO_CHECK, 'A.')],
            source_problems=[module.IssueItem('source:branch-rules', module.SECTION_SOURCE_PROBLEMS, 'S.')],
            failures=[module.IssueItem('failed-job:plan-changes', module.SECTION_FAILURES, 'J.'),
                      module.IssueItem('failed-step:approve', module.SECTION_FAILURES, 'T.')])
        github = fake([closed])
        result = module.sync_issue(github, items, RUN_URL, NOW)
        self.assertEqual(read_item_keys(github.write_calls()[0].body['body']),
                         ['source:branch-rules', 'failed-job:plan-changes', 'failed-step:approve'])
        self.assertEqual(result['exit_code'], 1)

    def test_a_closed_issue_does_not_silence_a_token_that_is_still_unusable(self) -> None:
        closed = issue_entry(4, body_with('token-unusable:malformed', 'a'), state='closed')
        items = module.Items(things_to_check=[
            module.IssueItem('token-unusable:malformed', module.SECTION_THINGS_TO_CHECK, 'Token.'),
            module.IssueItem('a', module.SECTION_THINGS_TO_CHECK, 'A.')])
        github = fake([closed])
        result = module.sync_issue(github, items, RUN_URL, NOW)
        self.assertEqual((result['action'], result['exit_code']), ('created', 1))
        self.assertEqual(read_item_keys(github.write_calls()[0].body['body']), ['token-unusable:malformed'])

    def test_a_closed_issue_still_dismisses_a_token_that_only_expires_soon(self) -> None:
        closed = issue_entry(4, body_with('token-expires-soon:2026-12-01'), state='closed')
        items = module.Items(things_to_check=[
            module.IssueItem('token-expires-soon:2026-12-01', module.SECTION_THINGS_TO_CHECK, 'Soon.')])
        github = fake([closed])
        result = module.sync_issue(github, items, RUN_URL, NOW)
        self.assertEqual((result['action'], result['exit_code']), ('none', 0))
        self.assertEqual(github.write_calls(), [])

    def test_an_unusable_token_turns_the_run_red_but_one_expiring_soon_does_not(self) -> None:
        for key, red in (('token-unusable:missing', True), ('token-expires-soon:2026-12-01', False)):
            with self.subTest(key=key):
                items = module.Items(things_to_check=[module.IssueItem(key, module.SECTION_THINGS_TO_CHECK, 'T.')])
                self.assertEqual(items.reports_a_problem_that_turns_the_run_red(), red)
                self.assertEqual(module.sync_issue(fake([]), items, RUN_URL, NOW)['exit_code'], int(red))

    def test_the_issue_says_which_items_come_back_while_their_problem_lasts(self) -> None:
        body = module.render_issue_body(module.Items(things_to_check=[
            module.IssueItem('a', module.SECTION_THINGS_TO_CHECK, 'A.')]), RUN_URL, NOW)
        self.assertIn('an unusable token come back while they last', body)

    def test_all_clear_resolves_and_closes_the_open_issue(self) -> None:
        github = fake([issue_entry(5, body_with('a'))])
        result = module.sync_issue(github, module.Items(), RUN_URL, NOW)
        patch = github.write_calls()[0]
        self.assertEqual(patch.body['state'], 'closed')
        self.assertEqual(patch.body['state_reason'], 'completed')
        self.assertIn('Nothing needs a person as of [run 4242](%s).' % RUN_URL, patch.body['body'])
        self.assertIn(CLOSED_BY_BOT_MARKER, patch.body['body'])
        self.assertIn(ISSUE_MARKER, patch.body['body'])
        self.assertEqual((result['action'], result['closed'], result['exit_code']), ('resolved', True, 0))

    def test_without_a_plan_the_old_items_stay_and_only_failures_change(self) -> None:
        old = module.render_issue_body(self.things('a'), 'https://github.com/o/r/actions/runs/1', NOW)
        old += '\n### Jobs that failed ###\n\n- %s old failure\n' % item_marker('failed-job:old')
        github = fake([issue_entry(5, old)])
        items = module.collect_items(None, None, None, None, {'plan-changes': {'result': 'failure'}}, None)
        result = module.sync_issue(github, items, RUN_URL, NOW)
        body = github.write_calls()[0].body['body']
        self.assertEqual(read_item_keys(body), ['a', 'failed-job:plan-changes'])
        self.assertIn('Updated by [run 4242](%s) on 2026-11-22 03:07 UTC.' % RUN_URL, body)
        self.assertNotIn('old failure', body)
        self.assertEqual(result['exit_code'], 1)
        self.assertTrue(result['commented'])

    def test_without_a_plan_and_no_open_issue_creates_one_with_the_failures_alone(self) -> None:
        github = fake([])
        items = module.collect_items(None, None, None, None, {'plan-changes': {'result': 'failure'}}, None)
        module.sync_issue(github, items, RUN_URL, NOW)
        self.assertEqual(read_item_keys(github.write_calls()[0].body['body']), ['failed-job:plan-changes'])

    def test_exit_code_zero_without_problems_and_one_with_a_source_problem(self) -> None:
        self.assertEqual(module.sync_issue(fake([]), self.things('a'), RUN_URL, NOW)['exit_code'], 0)
        items = module.Items(source_problems=[module.IssueItem('source:branch-rules', module.SECTION_SOURCE_PROBLEMS, 'S')])
        self.assertEqual(module.sync_issue(fake([]), items, RUN_URL, NOW)['exit_code'], 1)

    def test_oversized_body_is_cut_before_it_is_sent(self) -> None:
        huge = module.IssueItem('k', module.SECTION_CHANGES, 'why', 'Title', [], 'P' * 70000)
        github = fake([])
        module.sync_issue(github, module.Items(changes_for_a_person=[huge]), RUN_URL, NOW)
        body = github.write_calls()[0].body['body']
        self.assertLessEqual(len(body), module.MAX_BODY)
        self.assertIn('Too long for the issue', body)


if __name__ == '__main__':
    unittest.main()
