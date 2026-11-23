from __future__ import annotations

import base64
import json
import os
import unittest
from typing import Any, Dict, List, Optional

from ensure_ci_always_tests_latest_operating_systems import approve_and_merge
from ensure_ci_always_tests_latest_operating_systems.approve_and_merge import (
    REFUSAL_REASONS, approve_and_turn_on_auto_merge, check_before_approving, merge_now, turn_on_auto_merge)
from ensure_ci_always_tests_latest_operating_systems.data_types import SafeChange
from ensure_ci_always_tests_latest_operating_systems.errors import GitHubApiError
from ensure_ci_always_tests_latest_operating_systems.line_editor import apply_changes
from ensure_ci_always_tests_latest_operating_systems.markers import COMMIT_TRAILER, PIN_FILES, WORKFLOW_FILE

from .helpers import FakeGitHub, InOrder, fixture

REPO = 'CommunityToolkit/Maui'
BASE = 'main'
BASE_SHA = 'a' * 40
HEAD_SHA = 'b' * 40
BRANCH = 'automation/ensure-ci-always-tests-latest-operating-systems'
OWNER = 'TheCodeTraveler'
RUN_URL = 'https://github.com/CommunityToolkit/Maui/actions/runs/1'
COMMITTED = '2026-11-21T08:00:00Z'
ARMED_AFTER_COMMIT = '2026-11-21T08:05:00Z'
ARMED_BEFORE_COMMIT = '2026-11-21T07:00:00Z'
DECISION = {'token': {'usable': True, 'login': OWNER}}
PULL = '/repos/%s/pulls/7' % REPO
SHA_PINNED_TEXT = {path: fixture('repo/' + os.path.basename(path)).decode('utf-8') for path in PIN_FILES}
SHARED_ENV = '.github/workflows/shared.env'


def xcode_change(base_text: str, new: str = '27.3') -> SafeChange:
    line = next(number for number, text in enumerate(base_text.split('\n'), 1)
                if text == 'CommunityToolkitLibrary_Xcode_Version=27.2')
    return SafeChange(family='xcode', kind='set-xcode-version', file=SHARED_ENV, line=line, old='27.2', new=new,
                      inserted_lines=[], pin='env:CommunityToolkitLibrary_Xcode_Version', description='', source='')


def encoded(text: str) -> Dict[str, str]:
    return {'content': base64.b64encode(text.encode('utf-8')).decode('ascii'), 'encoding': 'base64'}


def graphql_facts(merge_state: str = 'BLOCKED', enabled_at: Optional[str] = None, mergeable: str = 'MERGEABLE',
                  threads: Optional[List[bool]] = None, draft: bool = False) -> Dict[str, Any]:
    return {'repository': {'pullRequest': {
        'id': 'PR_node', 'isDraft': draft, 'mergeable': mergeable, 'mergeStateStatus': merge_state,
        'autoMergeRequest': {'enabledAt': enabled_at} if enabled_at else None,
        'commits': {'nodes': [{'commit': {'committedDate': COMMITTED}}]},
        'reviewThreads': {'pageInfo': {'hasNextPage': False, 'endCursor': None},
                          'nodes': [{'isResolved': resolved} for resolved in (threads or [])]}}}}


class Scenario:
    """A pull request that passes every check; a test changes one thing and watches the refusal."""

    def __init__(self, change_new: str = '27.3') -> None:
        self.base_texts = dict(SHA_PINNED_TEXT)
        self.change = xcode_change(self.base_texts[SHARED_ENV], change_new)
        self.plan = {'safe_changes': [self.change.to_json()]}
        self.head_texts = dict(self.base_texts)
        self.head_texts[SHARED_ENV] = apply_changes(self.base_texts[SHARED_ENV], [self.change])
        self.pull: Dict[str, Any] = {
            'state': 'open', 'draft': False, 'base': {'ref': BASE}, 'user': {'login': OWNER},
            'head': {'ref': BRANCH, 'sha': HEAD_SHA, 'repo': {'full_name': REPO}}}
        self.commits: List[Dict[str, Any]] = [{
            'sha': HEAD_SHA, 'commit': {'message': 'Update\n\n' + COMMIT_TRAILER}, 'author': {'login': OWNER},
            'parents': [{'sha': BASE_SHA}]}]
        self.compare: Dict[str, Any] = {'merge_base_commit': {'sha': BASE_SHA},
                                        'files': [{'filename': SHARED_ENV, 'status': 'modified'}]}
        self.reviews: List[Dict[str, Any]] = []
        self.timeline: List[Dict[str, Any]] = []
        self.rules = json.loads(fixture('github/rules-branches-main.json'))
        self.facts = graphql_facts()
        self.extra: Dict[str, Any] = {}

    def github(self, journal: Optional[List[Any]] = None) -> FakeGitHub:
        responses: Dict[str, Any] = {
            'GET ' + PULL: self.pull,
            'GET %s/commits' % PULL: self.commits,
            'GET /repos/%s/compare/%s...%s' % (REPO, BASE, HEAD_SHA): self.compare,
            'GET %s/reviews' % PULL: self.reviews,
            'GET /repos/%s/issues/7/timeline' % REPO: self.timeline,
            'GET /repos/%s/rules/branches/%s' % (REPO, BASE): self.rules,
            'graphql ReadPullRequestFacts': self.facts,
            'graphql TurnOnAutoMerge': {},
            'graphql TurnOffAutoMerge': {},
            'POST %s/reviews' % PULL: {},
            'PUT %s/merge' % PULL: {'merged': True},
        }
        for path in PIN_FILES:
            responses['GET /repos/%s/contents/%s?ref=%s' % (REPO, path, BASE_SHA)] = encoded(self.base_texts[path])
            responses['GET /repos/%s/contents/%s?ref=%s' % (REPO, path, HEAD_SHA)] = encoded(self.head_texts[path])
        responses.update(self.extra)
        client = FakeGitHub(responses, journal=journal)
        client.repo = REPO
        return client

    def run(self, operation: str = 'created', client: Optional[FakeGitHub] = None):
        client = client or self.github()
        approval = approve_and_turn_on_auto_merge(client, self.plan, DECISION, 7, HEAD_SHA, operation, BASE, BASE_SHA,
                                                  BRANCH, RUN_URL, sleep=lambda _: None)
        return approval, client

    def check(self):
        client = self.github()
        return check_before_approving(client, self.plan, DECISION, 7, HEAD_SHA, 'created', BASE, BASE_SHA, BRANCH,
                                      sleep=lambda _: None), client


def write_names(client: FakeGitHub) -> List[str]:
    return [call.operation or '%s %s' % (call.method, call.path) for call in client.write_calls()]


class HappyPathTests(unittest.TestCase):
    def test_created_arms_then_approves(self) -> None:
        approval, client = Scenario().run('created')
        self.assertEqual(write_names(client), ['TurnOnAutoMerge', 'POST %s/reviews' % PULL])
        self.assertEqual((approval['auto_merge'], approval['approved'], approval['refusals']), ('turned-on', 'new', []))

    def test_enable_carries_expected_head_oid_and_review_carries_commit_id(self) -> None:
        _, client = Scenario().run('created')
        enable, review = client.write_calls()
        self.assertEqual(enable.body['variables'], {'id': 'PR_node', 'sha': HEAD_SHA})
        self.assertIn('expectedHeadOid: $sha', enable.body['query'])
        self.assertIn('mergeMethod: SQUASH', enable.body['query'])
        self.assertEqual(review.body['commit_id'], HEAD_SHA)
        self.assertEqual(review.body['event'], 'APPROVE')
        self.assertIn(RUN_URL, review.body['body'])
        self.assertIn('on main aaaaaaa', review.body['body'])

    def test_updated_with_auto_merge_on_for_an_older_head_disables_enables_then_reviews(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(enabled_at=ARMED_BEFORE_COMMIT)
        approval, client = scenario.run('updated')
        self.assertEqual(write_names(client), ['TurnOffAutoMerge', 'TurnOnAutoMerge', 'POST %s/reviews' % PULL])
        self.assertEqual(approval['auto_merge'], 'turned-on-again')

    def test_none_with_auto_merge_armed_for_this_head_and_an_approval_writes_nothing(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(enabled_at=ARMED_AFTER_COMMIT)
        scenario.reviews.append({'user': {'login': 'github-actions[bot]'}, 'state': 'APPROVED', 'commit_id': HEAD_SHA})
        approval, client = scenario.run('none')
        self.assertEqual(client.write_calls(), [])
        self.assertEqual((approval['auto_merge'], approval['approved']), ('already-on', 'existing'))

    def test_updated_with_auto_merge_enabled_after_the_commit_still_turns_it_off_on_and_reviews(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(enabled_at=ARMED_AFTER_COMMIT)
        approval, client = scenario.run('updated')
        self.assertEqual(write_names(client), ['TurnOffAutoMerge', 'TurnOnAutoMerge', 'POST %s/reviews' % PULL])
        self.assertEqual(approval['auto_merge'], 'turned-on-again')

    def test_approval_body_names_the_workflow_file_from_markers(self) -> None:
        _, client = Scenario().run('created')
        self.assertIn(WORKFLOW_FILE, client.write_calls()[-1].body['body'])

    def test_none_with_auto_merge_armed_before_the_head_disables_enables_and_reviews(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(enabled_at=ARMED_BEFORE_COMMIT)
        scenario.reviews.append({'user': {'login': 'github-actions[bot]'}, 'state': 'APPROVED', 'commit_id': 'c' * 40})
        _, client = scenario.run('none')
        self.assertEqual(write_names(client), ['TurnOffAutoMerge', 'TurnOnAutoMerge', 'POST %s/reviews' % PULL])

    def test_an_approval_by_a_person_does_not_count(self) -> None:
        scenario = Scenario()
        scenario.reviews.append({'user': {'login': 'someone'}, 'state': 'APPROVED', 'commit_id': HEAD_SHA})
        _, client = scenario.run()
        self.assertIn('POST %s/reviews' % PULL, write_names(client))

    def test_two_reads_of_a_lagging_head_then_the_head_arrives(self) -> None:
        scenario = Scenario()
        lagging = dict(scenario.pull, head=dict(scenario.pull['head'], sha='c' * 40))
        scenario.extra['GET ' + PULL] = InOrder([lagging, lagging, scenario.pull])
        sleeps: List[float] = []
        client = scenario.github()
        approval = approve_and_turn_on_auto_merge(client, scenario.plan, DECISION, 7, HEAD_SHA, 'created', BASE, BASE_SHA,
                                                  BRANCH, RUN_URL, sleep=sleeps.append)
        self.assertEqual(approval['refusals'], [])
        self.assertEqual(sleeps, [5, 5])

    def test_a_head_that_never_arrives_is_refused_after_three_reads(self) -> None:
        scenario = Scenario()
        scenario.pull['head']['sha'] = 'c' * 40
        approval, client = scenario.run()
        self.assertIn('head', approval['refusals'])
        self.assertEqual(len(client.calls_to('GET', PULL)), 3)


class DirectMergeTests(unittest.TestCase):
    def test_clean_pull_request_is_approved_then_merged_with_sha_and_never_armed(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts('CLEAN')
        approval, client = scenario.run()
        self.assertEqual(write_names(client), ['POST %s/reviews' % PULL, 'PUT %s/merge' % PULL])
        merge = client.write_calls()[-1]
        self.assertEqual(merge.body, {'sha': HEAD_SHA, 'merge_method': 'squash'})
        self.assertEqual((approval['auto_merge'], approval['merged_directly']), ('merged-directly', True))

    def test_has_hooks_and_unstable_count_as_mergeable(self) -> None:
        for state in ('HAS_HOOKS', 'UNSTABLE'):
            scenario = Scenario()
            scenario.facts = graphql_facts(state)
            _, client = scenario.run()
            self.assertEqual(write_names(client)[-1], 'PUT %s/merge' % PULL, state)

    def test_clean_with_an_existing_approval_only_merges(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts('CLEAN')
        scenario.reviews.append({'user': {'login': 'github-actions[bot]'}, 'state': 'APPROVED', 'commit_id': HEAD_SHA})
        _, client = scenario.run()
        self.assertEqual(write_names(client), ['PUT %s/merge' % PULL])

    def test_enable_error_clean_status_falls_back_to_approve_and_merge(self) -> None:
        for message in ('Pull request is in clean status', 'Pull request is in UNSTABLE status',
                        'pull request is in has_hooks status'):
            scenario = Scenario()
            scenario.extra['graphql TurnOnAutoMerge'] = GitHubApiError('POST', '/graphql', 200, 'TurnOnAutoMerge: ' + message)
            approval, client = scenario.run()
            self.assertEqual(write_names(client), ['TurnOnAutoMerge', 'POST %s/reviews' % PULL, 'PUT %s/merge' % PULL], message)
            self.assertTrue(approval['merged_directly'], message)

    def test_any_other_enable_error_fails_the_step(self) -> None:
        scenario = Scenario()
        scenario.extra['graphql TurnOnAutoMerge'] = GitHubApiError('POST', '/graphql', 200, 'TurnOnAutoMerge: boom')
        with self.assertRaises(GitHubApiError):
            scenario.run()

    def test_merge_refused_is_a_thing_to_check_not_a_failure(self) -> None:
        for status in (403, 405, 409, 422):
            scenario = Scenario()
            scenario.facts = graphql_facts('CLEAN')
            scenario.extra['PUT %s/merge' % PULL] = (status, {}, {'message': 'Repository rule violations found'})
            approval, _ = scenario.run()
            self.assertFalse(approval['merged_directly'], status)
            self.assertEqual(approval['approved'], 'new', status)
            self.assertEqual(approval['things_to_check'][0]['key'], 'pull-request-ready-to-merge:7', status)
            self.assertIn('Repository rule violations found', approval['things_to_check'][0]['text'])

    def test_merge_now_raises_on_a_server_error_status(self) -> None:
        client = FakeGitHub({'PUT %s/merge' % PULL: (404, {}, {'message': 'Not Found'})})
        client.repo = REPO
        with self.assertRaises(GitHubApiError):
            merge_now(client, 7, HEAD_SHA)

    def test_merge_is_never_called_on_the_armed_path(self) -> None:
        for operation in ('created', 'updated', 'none'):
            _, client = Scenario().run(operation)
            self.assertEqual(client.calls_to('PUT', '%s/merge' % PULL), [], operation)


class RefusalTests(unittest.TestCase):
    def assert_refused(self, scenario: Scenario, code: str, operation: str = 'created',
                       auto_merge: str = 'not-turned-on') -> Dict[str, Any]:
        approval, client = scenario.run(operation)
        self.assertIn(code, approval['refusals'])
        names = write_names(client)
        self.assertNotIn('TurnOnAutoMerge', names, code)
        self.assertNotIn('POST %s/reviews' % PULL, names, code)
        self.assertNotIn('PUT %s/merge' % PULL, names, code)
        self.assertEqual(approval['auto_merge'], auto_merge)
        self.assertEqual(approval['approved'], 'no')
        return approval

    def test_the_happy_scenario_has_no_refusal(self) -> None:
        result, _ = Scenario().check()
        self.assertEqual(result.refusals, [])

    def test_every_code_has_a_plain_reason_and_a_test(self) -> None:
        test_names = [name for name in dir(RefusalTests) if name.startswith('test_refuses_')]
        for code in REFUSAL_REASONS:
            prefix = 'test_refuses_' + code.replace('-', '_')
            self.assertTrue(any(name == prefix or name.startswith(prefix + '_') for name in test_names), code)
        self.assertTrue(all(REFUSAL_REASONS[code] for code in REFUSAL_REASONS))

    def test_refuses_state_when_closed(self) -> None:
        scenario = Scenario()
        scenario.pull['state'] = 'closed'
        self.assert_refused(scenario, 'state')

    def test_refuses_state_when_draft(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(draft=True)
        self.assert_refused(scenario, 'state')

    def test_refuses_state_when_the_base_is_another_branch(self) -> None:
        scenario = Scenario()
        scenario.pull['base']['ref'] = 'release'
        self.assert_refused(scenario, 'state')

    def test_refuses_author(self) -> None:
        scenario = Scenario()
        scenario.pull['user']['login'] = 'someone-else'
        self.assert_refused(scenario, 'author')

    def test_refuses_branch_for_another_branch_name(self) -> None:
        scenario = Scenario()
        scenario.pull['head']['ref'] = 'a-person-branch'
        self.assert_refused(scenario, 'branch')

    def test_refuses_branch_for_a_fork(self) -> None:
        scenario = Scenario()
        scenario.pull['head']['repo']['full_name'] = 'someone/Maui'
        self.assert_refused(scenario, 'branch')

    def test_refuses_branch_when_the_head_repository_is_gone(self) -> None:
        scenario = Scenario()
        scenario.pull['head']['repo'] = None
        self.assert_refused(scenario, 'branch')

    def test_refuses_head(self) -> None:
        scenario = Scenario()
        scenario.pull['head']['sha'] = 'c' * 40
        self.assert_refused(scenario, 'head')

    def test_refuses_commits_when_there_are_two(self) -> None:
        scenario = Scenario()
        scenario.commits.append(dict(scenario.commits[0]))
        self.assert_refused(scenario, 'commits')

    def test_refuses_commits_when_the_one_commit_is_not_the_head(self) -> None:
        scenario = Scenario()
        scenario.commits[0]['sha'] = 'e' * 40
        approval = self.assert_refused(scenario, 'commits')
        self.assertIn('not the head', approval['things_to_check'][0]['text'])

    def test_refuses_author_when_the_token_owner_login_is_empty(self) -> None:
        scenario = Scenario()
        scenario.pull['user']['login'] = ''
        scenario.commits[0]['author'] = {}
        client = scenario.github()
        approval = approve_and_turn_on_auto_merge(client, scenario.plan, {'token': {'usable': True, 'login': ''}}, 7, HEAD_SHA,
                                                  'created', BASE, BASE_SHA, BRANCH, RUN_URL, sleep=lambda _: None)
        self.assertIn('author', approval['refusals'])
        self.assertNotIn('POST %s/reviews' % PULL, write_names(client))

    def test_refuses_commits_without_the_trailer(self) -> None:
        scenario = Scenario()
        scenario.commits[0]['commit']['message'] = 'Update'
        self.assert_refused(scenario, 'commits')

    def test_refuses_commits_by_another_author(self) -> None:
        scenario = Scenario()
        scenario.commits[0]['author'] = {'login': 'someone-else'}
        self.assert_refused(scenario, 'commits')

    def test_refuses_base_when_the_parent_is_another_commit(self) -> None:
        scenario = Scenario()
        scenario.commits[0]['parents'] = [{'sha': 'd' * 40}]
        self.assert_refused(scenario, 'base')

    def test_refuses_base_when_the_merge_base_differs(self) -> None:
        scenario = Scenario()
        scenario.compare['merge_base_commit']['sha'] = 'd' * 40
        self.assert_refused(scenario, 'base')

    def test_refuses_base_for_a_merge_commit(self) -> None:
        scenario = Scenario()
        scenario.commits[0]['parents'] = [{'sha': BASE_SHA}, {'sha': 'd' * 40}]
        self.assert_refused(scenario, 'base')

    def test_refuses_files_for_another_file(self) -> None:
        scenario = Scenario()
        scenario.compare['files'].append({'filename': 'README.md', 'status': 'modified'})
        self.assert_refused(scenario, 'files')

    def test_refuses_files_for_an_added_pin_file_status(self) -> None:
        scenario = Scenario()
        scenario.compare['files'] = [{'filename': SHARED_ENV, 'status': 'renamed'}]
        self.assert_refused(scenario, 'files')

    def test_refuses_exact_changes_for_one_extra_character(self) -> None:
        scenario = Scenario()
        scenario.head_texts[SHARED_ENV] = scenario.head_texts[SHARED_ENV].replace('=27.3', '=27.33', 1)
        self.assert_refused(scenario, 'exact-changes')

    def test_refuses_exact_changes_for_an_edit_in_a_file_the_plan_leaves_alone(self) -> None:
        scenario = Scenario()
        path = '.github/workflows/dotnet-build.yml'
        scenario.head_texts[path] = scenario.head_texts[path] + '\n'
        self.assert_refused(scenario, 'exact-changes')

    def test_refuses_exact_changes_when_the_plan_does_not_apply_to_the_base(self) -> None:
        scenario = Scenario()
        scenario.plan['safe_changes'][0]['old'] = '26.0'
        self.assert_refused(scenario, 'exact-changes')

    def test_refuses_validator_for_an_edit_outside_the_allowed_kinds(self) -> None:
        scenario = Scenario()
        text = scenario.base_texts[SHARED_ENV].replace('LATEST_NET_VERSION=11.0.x', 'LATEST_NET_VERSION=12.0.x')
        scenario.plan = {'safe_changes': []}
        scenario.head_texts[SHARED_ENV] = text
        approval = self.assert_refused(scenario, 'validator')
        self.assertIn('exact-changes', approval['refusals'])

    def test_refuses_levels_rise_for_a_replacement_that_does_not_rise(self) -> None:
        scenario = Scenario()
        scenario.plan['safe_changes'] = [SafeChange(
            family='android', kind='replace-api-level', file='.github/workflows/device-tests.yml', line=387, old='28',
            new='27', inserted_lines=[], pin='android-leg:default:28', description='', source='').to_json()]
        self.assert_refused(scenario, 'levels-rise')

    def test_refuses_levels_rise_for_an_inserted_leg_below_an_existing_one(self) -> None:
        scenario = Scenario()
        scenario.plan['safe_changes'] = [SafeChange(
            family='android', kind='insert-android-leg', file='.github/workflows/device-tests.yml', line=387, old=None,
            new=None, inserted_lines=["                    -   api-level: '36.0'"], pin='android-leg:google_apis:36',
            description='', source='').to_json()]
        self.assert_refused(scenario, 'levels-rise')

    def test_refuses_threads_with_text_that_leads_with_what_to_do(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(threads=[True, False, False])
        approval = self.assert_refused(scenario, 'threads')
        thing = approval['things_to_check'][0]
        self.assertEqual(thing['key'], 'pull-request-not-approved:7:threads')
        self.assertEqual(thing['text'], 'Resolve the open review conversations on #7 (2 left). The bot approves it on its next run.')

    def test_refuses_conflicts(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(mergeable='CONFLICTING')
        self.assert_refused(scenario, 'conflicts')

    def test_unknown_mergeable_is_not_a_conflict(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(mergeable='UNKNOWN')
        result, _ = scenario.check()
        self.assertEqual(result.refusals, [])

    def test_refuses_last_push_rule_when_the_rule_is_off(self) -> None:
        scenario = Scenario()
        for rule in scenario.rules:
            if rule['type'] == 'pull_request':
                rule['parameters']['require_last_push_approval'] = False
        approval = self.assert_refused(scenario, 'last-push-rule')
        self.assertEqual(approval['things_to_check'][0]['key'], 'last-push-approval-rule-off')

    def test_refuses_last_push_rule_with_its_own_wording_when_the_rules_could_not_be_read(self) -> None:
        scenario = Scenario()
        scenario.rules = {'status': 'error'}
        scenario.extra['GET /repos/%s/rules/branches/%s' % (REPO, BASE)] = GitHubApiError(
            'GET', '/repos/%s/rules/branches/%s' % (REPO, BASE), 500, 'Server Error')
        approval = self.assert_refused(scenario, 'last-push-rule')
        item = approval['things_to_check'][0]
        self.assertEqual(item['key'], 'pull-request-not-approved:7:last-push-rule')
        self.assertIn('could not be read', item['text'])
        self.assertNotIn('no longer require', item['text'])

    def test_refuses_last_push_rule_when_no_approval_is_required(self) -> None:
        scenario = Scenario()
        for rule in scenario.rules:
            if rule['type'] == 'pull_request':
                rule['parameters']['required_approving_review_count'] = 0
        self.assert_refused(scenario, 'last-push-rule')

    def test_refuses_hold_when_a_person_turned_auto_merge_off(self) -> None:
        scenario = Scenario()
        scenario.timeline.extend([{'event': 'auto_merge_enabled', 'actor': {'type': 'Bot'}},
                                  {'event': 'auto_merge_disabled', 'actor': {'type': 'User'}}])
        approval = self.assert_refused(scenario, 'hold')
        self.assertEqual(approval['things_to_check'][0]['key'], 'pull-request-auto-merge-turned-off:7')

    def test_no_hold_when_the_bot_turned_it_off(self) -> None:
        scenario = Scenario()
        scenario.timeline.append({'event': 'auto_merge_disabled', 'actor': {'type': 'Bot'}})
        result, _ = scenario.check()
        self.assertEqual(result.refusals, [])

    def test_no_hold_when_a_person_turned_it_on_again(self) -> None:
        scenario = Scenario()
        scenario.timeline.extend([{'event': 'auto_merge_disabled', 'actor': {'type': 'User'}},
                                  {'event': 'auto_merge_enabled', 'actor': {'type': 'User'}}])
        result, _ = scenario.check()
        self.assertEqual(result.refusals, [])

    def test_other_refusals_give_a_not_approved_item_naming_the_first_code(self) -> None:
        scenario = Scenario()
        scenario.pull['user']['login'] = 'someone-else'
        scenario.pull['head']['ref'] = 'a-person-branch'
        approval = self.assert_refused(scenario, 'author')
        thing = approval['things_to_check'][0]
        self.assertEqual(thing['key'], 'pull-request-not-approved:7:author')
        self.assertTrue(thing['text'].startswith('Review and merge #7 yourself, or close it and run the workflow with `reopen-rejected` for a fresh one. The bot did not approve #7 because author: '))
        self.assertIn('branch', thing['text'])

    def test_every_other_refusal_code_starts_with_an_action(self) -> None:
        self.assertEqual(set(approve_and_merge._REFUSAL_ACTIONS), set(REFUSAL_REASONS) - {'hold'})

    def test_threads_and_conflicts_give_both_actions_and_name_only_conflicts(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(threads=[False, False], mergeable='CONFLICTING')
        approval, _ = scenario.run()
        text = approval['things_to_check'][0]['text']
        self.assertIn('threads', approval['refusals'])
        self.assertIn('conflicts', approval['refusals'])
        self.assertTrue(text.startswith('Resolve the open review conversations on #7 (2 left). Wait for the bot\'s next run'), text)
        self.assertNotIn('The bot approves it on its next run.', text)
        self.assertIn('because conflicts: ', text)
        self.assertNotIn('because threads', text)

    def test_the_same_action_for_author_and_branch_appears_once(self) -> None:
        scenario = Scenario()
        scenario.pull['user']['login'] = 'someone-else'
        scenario.pull['head']['ref'] = 'a-person-branch'
        approval, _ = scenario.run()
        text = approval['things_to_check'][0]['text']
        self.assertEqual(text.count('Review and merge #7 yourself'), 1)

    def test_no_action_promises_a_fresh_pull_request_without_reopen_rejected(self) -> None:
        for code, action in approve_and_merge._REFUSAL_ACTIONS.items():
            if 'fresh' in action:
                self.assertIn('reopen-rejected', action, code)


class AutoMergeKeepsOrDropsTests(unittest.TestCase):
    def test_a_refusal_with_auto_merge_on_turns_it_off(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(enabled_at=ARMED_AFTER_COMMIT)
        scenario.pull['user']['login'] = 'someone-else'
        approval, client = scenario.run()
        self.assertEqual(write_names(client), ['TurnOffAutoMerge'])
        self.assertEqual(approval['auto_merge'], 'turned-off')

    def test_threads_conflicts_and_hold_leave_auto_merge_on(self) -> None:
        cases = {
            'threads': lambda s: setattr(s, 'facts', graphql_facts(enabled_at=ARMED_AFTER_COMMIT, threads=[False])),
            'conflicts': lambda s: setattr(s, 'facts', graphql_facts(enabled_at=ARMED_AFTER_COMMIT, mergeable='CONFLICTING')),
            'hold': lambda s: (setattr(s, 'facts', graphql_facts(enabled_at=ARMED_AFTER_COMMIT)),
                               s.timeline.append({'event': 'auto_merge_disabled', 'actor': {'type': 'User'}})),
        }
        for code, arrange in cases.items():
            scenario = Scenario()
            arrange(scenario)
            approval, client = scenario.run()
            self.assertEqual(approval['refusals'], [code])
            self.assertEqual(client.write_calls(), [], code)
            self.assertEqual(approval['auto_merge'], 'left-on', code)

    def test_a_refusal_with_auto_merge_off_writes_nothing(self) -> None:
        scenario = Scenario()
        scenario.pull['user']['login'] = 'someone-else'
        approval, client = scenario.run()
        self.assertEqual(client.write_calls(), [])
        self.assertEqual(approval['auto_merge'], 'not-turned-on')

    def test_threads_with_another_refusal_turns_it_off(self) -> None:
        scenario = Scenario()
        scenario.facts = graphql_facts(enabled_at=ARMED_AFTER_COMMIT, threads=[False])
        scenario.pull['user']['login'] = 'someone-else'
        _, client = scenario.run()
        self.assertEqual(write_names(client), ['TurnOffAutoMerge'])


class OnlyTheGithubTokenTests(unittest.TestCase):
    def test_every_call_of_every_scenario_uses_the_github_token(self) -> None:
        journal: List[Any] = []
        arrangements = [lambda s: None,
                        lambda s: setattr(s, 'facts', graphql_facts('CLEAN')),
                        lambda s: s.pull['user'].update(login='someone-else')]
        for arrange in arrangements:
            scenario = Scenario()
            arrange(scenario)
            scenario.run(client=scenario.github(journal))
        self.assertTrue(journal)
        self.assertEqual({call.token_name for call in journal}, {'GITHUB_TOKEN'})

    def test_every_review_carries_commit_id_and_every_enable_expected_head_oid_and_merge_only_on_direct_path(self) -> None:
        journal: List[Any] = []
        for arrange in (lambda s: None, lambda s: setattr(s, 'facts', graphql_facts('CLEAN')),
                        lambda s: s.pull['head'].update(ref='x')):
            scenario = Scenario()
            arrange(scenario)
            scenario.run(client=scenario.github(journal))
        reviews = [call for call in journal if call.method == 'POST' and call.path.endswith('/reviews') and call.is_write]
        enables = [call for call in journal if call.operation == 'TurnOnAutoMerge']
        merges = [call for call in journal if call.method == 'PUT']
        self.assertEqual(len(reviews), 2)
        self.assertTrue(all(call.body['commit_id'] == HEAD_SHA for call in reviews))
        self.assertTrue(all('expectedHeadOid: $sha' in call.body['query'] and call.body['variables']['sha'] == HEAD_SHA
                            for call in enables))
        self.assertEqual(len(merges), 1)
        self.assertEqual(merges[0].body['sha'], HEAD_SHA)

    def test_the_module_never_names_the_maintainer_token(self) -> None:
        with open(approve_and_merge.__file__, encoding='utf-8') as source:
            text = source.read()
        self.assertNotIn('ENSURE_CI_ALWAYS_TESTS_LATEST_OPERATING_SYSTEMS_TOKEN', text)
        self.assertNotIn('TOKEN_SECRET_NAME', text)

    def test_turn_on_returns_the_outcome(self) -> None:
        client = Scenario().github()
        self.assertEqual(turn_on_auto_merge(client, 'PR_node', HEAD_SHA), 'turned-on')


class ThreadPaginationTests(unittest.TestCase):
    def test_a_second_page_of_threads_is_read(self) -> None:
        scenario = Scenario()
        first = graphql_facts(threads=[True])
        first['repository']['pullRequest']['reviewThreads']['pageInfo'] = {'hasNextPage': True, 'endCursor': 'c1'}
        second = graphql_facts(threads=[False])
        scenario.extra['graphql ReadPullRequestFacts'] = InOrder([first, second])
        approval, client = scenario.run()
        self.assertIn('threads', approval['refusals'])
        reads = [call for call in client.calls if call.operation == 'ReadPullRequestFacts']
        self.assertEqual([call.body['variables']['after'] for call in reads], [None, 'c1'])


if __name__ == '__main__':
    unittest.main()
