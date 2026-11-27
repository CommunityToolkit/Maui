from __future__ import annotations

import unittest
from typing import Any, Dict, List, Optional

from ensure_ci_always_tests_latest_operating_systems import bump_pull_request as module
from ensure_ci_always_tests_latest_operating_systems.errors import GitHubApiError
from ensure_ci_always_tests_latest_operating_systems.markers import (CLOSED_BY_BOT_MARKER, COMMIT_TRAILER,
                                                                     PULL_REQUEST_MARKER, change_list_marker)
from ensure_ci_always_tests_latest_operating_systems_tests.helpers import FakeGitHub
from ensure_ci_always_tests_latest_operating_systems_tests.helpers import fixture
from ensure_ci_always_tests_latest_operating_systems_tests.test_pinned_versions import read_with, replaced_once

BRANCH = 'automation/ensure-ci-always-tests-latest-operating-systems'
PULLS = '/repos/o/r/pulls'
BOT_LOGIN = 'maintainer'


class FakeRepoGitHub(FakeGitHub):
    repo = 'o/r'


def change(pin: str, new: str, old: str = '', kind: str = 'replace-api-level', family: str = 'android',
           file: str = '.github/workflows/device-tests.yml', inserted_lines: Optional[List[str]] = None) -> Dict[str, Any]:
    return {'family': family, 'kind': kind, 'file': file, 'pin': pin, 'old': old, 'new': new,
            'inserted_lines': inserted_lines or []}


def body_of(*changes: Dict[str, Any], closed_by_bot: bool = False, marker: bool = True) -> str:
    parts = [PULL_REQUEST_MARKER] if marker else []
    parts.append(change_list_marker(changes))
    if closed_by_bot:
        parts.append(CLOSED_BY_BOT_MARKER)
    return '\n'.join(parts)


def pull(number: int, body: str, state: str = 'open', merged_at: Optional[str] = None, login: str = BOT_LOGIN,
         sha: Optional[str] = None) -> Dict[str, Any]:
    return {'number': number, 'state': state, 'body': body, 'merged_at': merged_at, 'user': {'login': login},
            'head': {'sha': sha or 'a' * 40}}


def bot_commit(sha: str = 'b' * 40, login: str = BOT_LOGIN) -> Dict[str, Any]:
    return {'sha': sha, 'author': {'login': login}, 'parents': [{'sha': 'p'}],
            'commit': {'message': 'Update the device tests\n\n' + COMMIT_TRAILER + '\n'}}


def graphql_answer(state: Optional[str], oid: str = 'a' * 40) -> Dict[str, Any]:
    rollup = None if state is None else {'state': state}
    return {'repository': {'pullRequest': {'commits': {'nodes': [{'commit': {'oid': oid, 'statusCheckRollup': rollup}}]}}}}


def fake(open_pulls: List[Dict[str, Any]], closed_pulls: Optional[List[Dict[str, Any]]] = None,
         commits: Optional[Dict[int, List[Dict[str, Any]]]] = None, checks: Optional[str] = 'SUCCESS') -> FakeRepoGitHub:
    responses: Dict[str, Any] = {
        'GET ' + PULLS + '?state=open&head=o%3A' + BRANCH.replace('/', '%2F') + '&base=main&per_page=100': open_pulls,
        'GET ' + PULLS + '?state=closed&head=o%3A' + BRANCH.replace('/', '%2F') + '&base=main&per_page=100':
            closed_pulls or [],
        'graphql ReadBumpPullRequestChecks': graphql_answer(checks),
    }
    for number, listed in (commits or {}).items():
        responses['GET %s/%d/commits' % (PULLS, number)] = listed
    return FakeRepoGitHub(responses)


def read(github: FakeRepoGitHub) -> Dict[str, Any]:
    return module.read_bump_pull_request_state(github, 'main', BRANCH)


class OpenPullRequestTests(unittest.TestCase):
    def test_no_open_pull_request(self) -> None:
        state = read(fake([]))
        self.assertEqual(state, {'status': 'ok', 'open': None, 'rejected': [], 'things_to_check': []})

    def test_one_open_pull_request_made_only_by_the_bot(self) -> None:
        listed = change('android-leg:google_apis_ps16k:37', '37.3', '37.2')
        github = fake([pull(12, body_of(listed))], commits={12: [bot_commit()]})
        state = read(github)
        self.assertEqual(state['status'], 'ok')
        self.assertEqual(state['open']['number'], 12)
        self.assertEqual(state['open']['author'], BOT_LOGIN)
        self.assertEqual(state['open']['head_sha'], 'a' * 40)
        self.assertEqual(state['open']['change_list'], [listed])
        self.assertEqual(state['open']['person_commits'], [])
        self.assertFalse(state['open']['checks_failed'])
        self.assertEqual(state['things_to_check'], [])

    def test_two_open_pull_requests_are_a_source_problem(self) -> None:
        state = read(fake([pull(12, body_of()), pull(9, body_of())]))
        self.assertEqual(state['status'], 'error')
        self.assertEqual(state['error'], 'two open bump PRs: #9 and #12; close one')
        self.assertIsNone(state['open'])

    def test_a_body_whose_change_list_does_not_parse_carries_no_changes(self) -> None:
        github = fake([pull(12, PULL_REQUEST_MARKER + '\n<!-- ensure-ci-always-tests-latest-operating-systems:changes=oops -->')],
                      commits={12: [bot_commit()]})
        self.assertEqual(read(github)['open']['change_list'], [])

    def test_no_write_call_is_ever_made(self) -> None:
        github = fake([pull(12, body_of())], commits={12: [bot_commit()]})
        read(github)
        self.assertEqual(github.write_calls(), [])


class PersonCommitTests(unittest.TestCase):
    def read_with_commits(self, commits: List[Dict[str, Any]]) -> Dict[str, Any]:
        return read(fake([pull(12, body_of())], commits={12: commits}))

    def test_commit_without_the_trailer(self) -> None:
        commit = bot_commit()
        commit['commit']['message'] = 'Fix the thing by hand'
        state = self.read_with_commits([bot_commit('c' * 40), dict(commit, sha='d' * 40)])
        self.assertEqual(state['open']['person_commits'], [{'sha': 'd' * 40, 'login': BOT_LOGIN, 'subject': 'Fix the thing by hand'}])
        self.assertEqual([thing['key'] for thing in state['things_to_check']], ['pull-request-has-a-person-commit:12'])
        text = state['things_to_check'][0]['text']
        self.assertIn('`ddddddd`', text)
        self.assertIn('`Fix the thing by hand`', text)
        self.assertIn('Close #12 and run the workflow with `reopen-rejected` for a fresh PR, or merge it yourself.', text)

    def test_commit_by_another_login_even_with_the_trailer(self) -> None:
        state = self.read_with_commits([bot_commit(login='someone-else')])
        self.assertEqual([commit['login'] for commit in state['open']['person_commits']], ['someone-else'])

    def test_commit_without_a_login(self) -> None:
        commit = bot_commit()
        commit['author'] = None
        state = self.read_with_commits([commit])
        self.assertEqual(len(state['open']['person_commits']), 1)
        self.assertEqual(state['open']['person_commits'][0]['login'], 'unknown')

    def test_update_branch_merge_commit_counts_even_with_the_trailer(self) -> None:
        merge = bot_commit('e' * 40)
        merge['parents'] = [{'sha': 'p1'}, {'sha': 'p2'}]
        merge['commit']['message'] = "Merge branch 'main' into %s\n\n%s" % (BRANCH, COMMIT_TRAILER)
        state = self.read_with_commits([bot_commit(), merge])
        self.assertEqual([commit['sha'] for commit in state['open']['person_commits']], ['e' * 40])

    def test_trailer_must_be_a_line_of_its_own(self) -> None:
        commit = bot_commit()
        commit['commit']['message'] = 'Subject mentioning ' + COMMIT_TRAILER + ' inline'
        self.assertEqual(len(self.read_with_commits([commit])['open']['person_commits']), 1)

    def test_outside_text_is_shown_as_code_only(self) -> None:
        commit = bot_commit()
        commit['author'] = {'login': 'evil'}
        commit['commit']['message'] = 'ping @everyone `x`'
        text = self.read_with_commits([commit])['things_to_check'][0]['text']
        self.assertIn("`ping @everyone 'x'`", text)

    def test_commits_are_read_from_every_page(self) -> None:
        github = fake([pull(12, body_of())], commits={12: [bot_commit() for _ in range(130)]})
        read(github)
        self.assertEqual(len(github.calls_to('GET', PULLS + '/12/commits')), 1)


class ChecksTests(unittest.TestCase):
    def state_with_checks(self, checks: Optional[str], oid: str = 'a' * 40) -> Dict[str, Any]:
        github = fake([pull(12, body_of())], commits={12: [bot_commit()]}, checks=checks)
        github.responses['graphql ReadBumpPullRequestChecks'] = graphql_answer(checks, oid)
        return read(github)

    def test_failure_and_error_are_failed_checks(self) -> None:
        for rollup in ('FAILURE', 'ERROR'):
            with self.subTest(rollup):
                state = self.state_with_checks(rollup)
                self.assertTrue(state['open']['checks_failed'])
                self.assertEqual([thing['key'] for thing in state['things_to_check']], ['pull-request-checks-failed:12'])

    def test_other_states_are_not_failures(self) -> None:
        for rollup in ('SUCCESS', 'PENDING', 'EXPECTED', None):
            with self.subTest(rollup):
                self.assertFalse(self.state_with_checks(rollup)['open']['checks_failed'])

    def test_a_rollup_of_an_older_commit_is_ignored(self) -> None:
        self.assertFalse(self.state_with_checks('FAILURE', oid='f' * 40)['open']['checks_failed'])

    def test_person_commit_and_failed_checks_give_two_items(self) -> None:
        commit = bot_commit(login='someone-else')
        github = fake([pull(12, body_of())], commits={12: [commit]}, checks='FAILURE')
        keys = [thing['key'] for thing in read(github)['things_to_check']]
        self.assertEqual(keys, ['pull-request-has-a-person-commit:12', 'pull-request-checks-failed:12'])


class RejectedChangesTests(unittest.TestCase):
    LEG = change('android-leg:google_apis_ps16k:37', '37.3', '37.2')
    LABEL = change('env:CommunityToolkitLibrary_Xcode_Version', '27.3', '27.2', 'set-xcode-version', 'xcode',
                   '.github/workflows/shared.env')

    def rejected_by(self, closed: List[Dict[str, Any]], open_pulls: Optional[List[Dict[str, Any]]] = None) -> List[Dict[str, Any]]:
        commits = {pull_['number']: [bot_commit()] for pull_ in (open_pulls or [])}
        return read(fake(open_pulls or [], closed, commits))['rejected']

    def test_person_closed_pull_request_rejects_each_change(self) -> None:
        closed = [pull(7, body_of(self.LEG, self.LABEL), state='closed')]
        self.assertEqual(self.rejected_by(closed), [
            {'pin': 'android-leg:google_apis_ps16k:37', 'new': '37.3', 'pull_request': 7},
            {'pin': 'env:CommunityToolkitLibrary_Xcode_Version', 'new': '27.3', 'pull_request': 7}])

    def test_bot_closed_pull_request_rejects_nothing(self) -> None:
        self.assertEqual(self.rejected_by([pull(7, body_of(self.LEG, closed_by_bot=True), state='closed')]), [])

    def test_merged_pull_request_rejects_nothing(self) -> None:
        merged = pull(7, body_of(self.LEG), state='closed', merged_at='2026-11-20T01:00:00Z')
        self.assertEqual(self.rejected_by([merged]), [])

    def test_closed_pull_request_without_the_bump_marker_is_ignored(self) -> None:
        self.assertEqual(self.rejected_by([pull(7, body_of(self.LEG, marker=False), state='closed')]), [])

    def test_a_higher_numbered_pull_request_listing_the_pair_clears_the_rejection(self) -> None:
        closed = [pull(7, body_of(self.LEG, self.LABEL), state='closed'),
                  pull(9, body_of(self.LEG, closed_by_bot=True), state='closed')]
        self.assertEqual(self.rejected_by(closed), [
            {'pin': 'env:CommunityToolkitLibrary_Xcode_Version', 'new': '27.3', 'pull_request': 7}])

    def test_an_open_higher_numbered_pull_request_clears_it_too(self) -> None:
        closed = [pull(7, body_of(self.LEG), state='closed')]
        self.assertEqual(self.rejected_by(closed, [pull(9, body_of(self.LEG))]), [])

    def test_a_lower_numbered_pull_request_does_not_clear_it(self) -> None:
        closed = [pull(7, body_of(self.LEG), state='closed'), pull(5, body_of(self.LEG, closed_by_bot=True), state='closed')]
        self.assertEqual([item['pull_request'] for item in self.rejected_by(closed)], [7])

    def test_a_different_new_value_is_not_cleared(self) -> None:
        newer = change('android-leg:google_apis_ps16k:37', '37.4', '37.3')
        closed = [pull(7, body_of(self.LEG), state='closed'), pull(9, body_of(newer, closed_by_bot=True), state='closed')]
        self.assertEqual(len(self.rejected_by(closed)), 1)

    def test_the_latest_rejecting_pull_request_is_named(self) -> None:
        closed = [pull(7, body_of(self.LEG), state='closed'), pull(8, body_of(self.LEG), state='closed')]
        self.assertEqual(self.rejected_by(closed), [{'pin': 'android-leg:google_apis_ps16k:37', 'new': '37.3', 'pull_request': 8}])


class ApiErrorTests(unittest.TestCase):
    def test_failing_open_list_is_an_error_state(self) -> None:
        github = fake([])
        key = next(key for key in github.responses if 'state=open' in key)
        github.responses[key] = GitHubApiError('GET', PULLS, 502, 'bad gateway')
        state = read(github)
        self.assertEqual(state['status'], 'error')
        self.assertIn('bad gateway', state['error'])
        self.assertIsNone(state['open'])
        self.assertEqual(state['rejected'], [])

    def test_failing_closed_list_is_an_error_state(self) -> None:
        github = fake([])
        key = next(key for key in github.responses if 'state=closed' in key)
        github.responses[key] = GitHubApiError('GET', PULLS, 500, 'boom')
        self.assertEqual(read(github)['status'], 'error')

    def test_failing_commits_or_checks_are_an_error_state(self) -> None:
        github = fake([pull(12, body_of())], commits={12: GitHubApiError('GET', 'x', 500, 'no commits')})
        self.assertEqual(read(github)['status'], 'error')
        github = fake([pull(12, body_of())], commits={12: [bot_commit()]})
        github.responses['graphql ReadBumpPullRequestChecks'] = GitHubApiError('POST', '/graphql', 200, 'no checks')
        self.assertEqual(read(github)['status'], 'error')

    def test_source_key_names_the_source(self) -> None:
        self.assertEqual(module.SOURCE_KEY, 'source:bump-pull-request')


class RebuildChangeTests(unittest.TestCase):
    def setUp(self) -> None:
        self.pinned = read_with()

    def test_api_level_follows_its_line_after_lines_moved(self) -> None:
        listed = change('android-leg:google_apis_ps16k:37', '37.3', '37.2')
        rebuilt = module.rebuild_change(self.pinned, listed)
        self.assertEqual((rebuilt.line, rebuilt.old, rebuilt.new, rebuilt.kind), (415, '37.2', '37.3', 'replace-api-level'))
        self.assertEqual(rebuilt.pin, 'android-leg:google_apis_ps16k:37')
        self.assertEqual(rebuilt.family, 'android')

    def test_a_line_that_moved_on_main_is_found_where_it_is_now(self) -> None:
        text = fixture('repo/device-tests.yml').decode('utf-8')
        moved = replaced_once(text, "                    # Android 17 with 4 KB pages.\n",
                              "                    # a\n                    # b\n                    # c\n"
                              "                    # Android 17 with 4 KB pages.\n")
        rebuilt = module.rebuild_change(read_with(device_tests=moved),
                                        change('android-leg:google_apis_ps16k:37', '37.3', '37.2'))
        self.assertEqual(rebuilt.line, 415 + 3)

    def test_a_carried_insert_is_dropped_when_main_already_has_a_leg_of_that_major_for_the_target(self) -> None:
        listed = change('android-leg:google_apis_ps16k:37', '37.5', '', 'insert-android-leg', inserted_lines=['x'])
        self.assertIsNone(module.rebuild_change(self.pinned, listed))

    def test_a_carried_insert_is_dropped_when_main_has_the_same_level_written_differently(self) -> None:
        listed = change('android-leg:google_apis:37', '37', '', 'insert-android-leg', inserted_lines=['x'])
        self.assertIsNone(module.rebuild_change(self.pinned, listed))

    def test_a_carried_insert_gets_its_lines_from_main_not_from_the_pull_request_body(self) -> None:
        listed = change('android-leg:google_apis_ps16k:38', '38.0', '', 'insert-android-leg',
                        inserted_lines=['            -   run: rm -rf /'])
        rebuilt = module.rebuild_change(self.pinned, listed)
        self.assertNotIn('rm -rf', '\n'.join(rebuilt.inserted_lines))
        self.assertIn("api-level: '38.0'", '\n'.join(rebuilt.inserted_lines))

    def test_a_malformed_level_or_version_is_dropped_without_a_crash(self) -> None:
        for kind, pin, old, new in (('replace-api-level', 'android-leg:google_apis_ps16k:37', '37.2', '38.x'),
                                    ('replace-api-level', 'android-leg:google_apis_ps16k:37', 'x', '37.3'),
                                    ('insert-android-leg', 'android-leg:google_apis:38', '', '38.x')):
            with self.subTest(new=new):
                self.assertIsNone(module.rebuild_change(self.pinned, change(pin, new, old, kind, inserted_lines=['x'])))
        xcode = change('env:CommunityToolkitLibrary_Xcode_Version', '27.x', '27.2', 'set-xcode-version', 'xcode',
                       '.github/workflows/shared.env')
        self.assertIsNone(module.rebuild_change(self.pinned, xcode))

    def test_api_level_line_is_read_from_main_not_from_the_pull_request(self) -> None:
        listed = change('android-leg:google_apis:37', '37.1', '37.0')
        listed['line'] = 1
        self.assertEqual(module.rebuild_change(self.pinned, listed).line, 411)

    def test_none_when_the_old_value_changed_on_main(self) -> None:
        self.assertIsNone(module.rebuild_change(self.pinned, change('android-leg:google_apis_ps16k:37', '37.4', '37.1')))

    def test_none_when_another_leg_on_main_already_has_the_new_level(self) -> None:
        self.assertIsNone(module.rebuild_change(self.pinned, change('android-leg:google_apis_ps16k:37', '37.0', '37.2')))

    def test_none_when_the_leg_is_gone(self) -> None:
        self.assertIsNone(module.rebuild_change(self.pinned, change('android-leg:google_apis_ps16k:99', '99.1', '99.0')))

    def test_xcode_version_follows_its_line(self) -> None:
        listed = change('env:CommunityToolkitLibrary_Xcode_Version', '27.3', '27.2', 'set-xcode-version', 'xcode',
                        '.github/workflows/shared.env')
        rebuilt = module.rebuild_change(self.pinned, listed)
        self.assertEqual((rebuilt.line, rebuilt.old, rebuilt.new), (18, '27.2', '27.3'))
        self.assertIsNone(module.rebuild_change(self.pinned, dict(listed, old='27.1')))

    def test_xcode_version_rejects_a_key_that_is_not_an_xcode_version(self) -> None:
        listed = change('env:TOOLKIT_NET_VERSION', '11.0.100', '10.0.100', 'set-xcode-version', 'xcode',
                        '.github/workflows/shared.env')
        self.assertIsNone(module.rebuild_change(self.pinned, listed))

    def test_android_leg_insert_goes_after_the_highest_lower_leg(self) -> None:
        listed = change('android-leg:google_apis_ps16k:38', '38.0', '', 'insert-android-leg', inserted_lines=['stale'])
        rebuilt = module.rebuild_change(self.pinned, listed)
        self.assertEqual((rebuilt.line, rebuilt.old, rebuilt.new), (417, None, '38.0'))
        self.assertEqual(rebuilt.inserted_lines, ["                    -   api-level: '38.0'",
                                                  '                        target: google_apis_ps16k'])

    def test_android_leg_insert_is_dropped_once_main_has_that_level(self) -> None:
        listed = change('android-leg:google_apis_ps16k:37', '37.2', '', 'insert-android-leg', inserted_lines=['x'])
        self.assertIsNone(module.rebuild_change(self.pinned, listed))

    def test_android_leg_insert_without_lines_is_dropped(self) -> None:
        self.assertIsNone(module.rebuild_change(self.pinned, change('android-leg:google_apis:38', '38.0', '', 'insert-android-leg')))

    def test_the_kinds_that_only_a_change_for_a_person_holds_are_dropped_when_read_from_a_body(self) -> None:
        lines = ["            -   ios-version: '27.3'"]
        ios_leg = change('ios-leg:27', '27.3', '', 'insert-ios-leg', 'xcode', inserted_lines=lines)
        pin = 'apple-label:.github/workflows/dotnet-build.yml:build_library'
        runner_label = change(pin, 'xcode-28', 'xcode-27', 'replace-runner-label', 'xcode', '.github/workflows/dotnet-build.yml')
        for listed in (ios_leg, runner_label):
            with self.subTest(kind=listed['kind']):
                self.assertIsNone(module.rebuild_change(self.pinned, listed))

    def test_a_pin_whose_major_is_a_digit_that_int_rejects_is_dropped(self) -> None:
        listed = change('android-leg:google_apis_ps16k:\u00b3', '37.3', '37.2')
        self.assertIsNone(module.rebuild_change(self.pinned, listed))

    def test_unknown_kind_file_or_missing_fields_give_none(self) -> None:
        listed = change('android-leg:google_apis_ps16k:37', '37.3', '37.2')
        self.assertIsNone(module.rebuild_change(self.pinned, dict(listed, kind='delete-everything')))
        self.assertIsNone(module.rebuild_change(self.pinned, dict(listed, file='README.md')))
        self.assertIsNone(module.rebuild_change(self.pinned, dict(listed, new='')))
        self.assertIsNone(module.rebuild_change(self.pinned, {'kind': 'replace-api-level'}))

    def test_rebuilt_change_round_trips_through_json(self) -> None:
        rebuilt = module.rebuild_change(self.pinned, change('android-leg:google_apis_ps16k:37', '37.3', '37.2'))
        from ensure_ci_always_tests_latest_operating_systems.data_types import SafeChange
        self.assertEqual(SafeChange.from_json(rebuilt.to_json()), rebuilt)


if __name__ == '__main__':
    unittest.main()
