from __future__ import annotations

import unittest
from typing import Any, Dict

from ensure_ci_always_tests_latest_operating_systems.data_types import ThingToCheck
from ensure_ci_always_tests_latest_operating_systems.markers import CLOSED_BY_BOT_MARKER
from ensure_ci_always_tests_latest_operating_systems.pull_request_decision import decide_pull_request_update, step_outputs
from ensure_ci_always_tests_latest_operating_systems.token_owner import TokenStatus

from .helpers import FakeGitHub

REPO = 'CommunityToolkit/Maui'
BASE_SHA = 'a' * 40
USABLE = TokenStatus(usable=True, reason='', login='TheCodeTraveler', user_id=1,
                     identity='TheCodeTraveler <1+TheCodeTraveler@users.noreply.github.com>')
UNUSABLE = TokenStatus(usable=False, reason='rejected')


def plan_with(action: str, open_pull_request: Any = None) -> Dict[str, Any]:
    return {'pull_request_action': action, 'open_pull_request': open_pull_request, 'title': 'The title',
            'commit_message': 'The message\n\nGenerated-by: x'}


def github_client(newest_commit: str = BASE_SHA) -> FakeGitHub:
    client = FakeGitHub({
        'GET /repos/%s/git/ref/heads/main' % REPO: {'object': {'sha': newest_commit}},
        'GET /repos/%s/pulls/7' % REPO: {'body': 'The old body'},
        'PATCH /repos/%s/pulls/7' % REPO: {},
    })
    client.repo = REPO
    return client


def decide(plan: Dict[str, Any], token: TokenStatus = USABLE, newest_commit: str = BASE_SHA):
    client = github_client(newest_commit)
    return decide_pull_request_update(plan, token, client, 'main', BASE_SHA), client


class OpenOrUpdateTests(unittest.TestCase):
    def test_usable_token_opens_or_updates(self) -> None:
        decision, client = decide(plan_with('open-or-update'))
        self.assertTrue(decision.open_or_update)
        self.assertFalse(decision.main_moved)
        self.assertEqual(client.write_calls(), [])

    def test_unusable_token_pushes_nothing_and_reports_the_reason(self) -> None:
        unusable = TokenStatus(usable=False, reason='missing')
        unusable.things_to_check.append(ThingToCheck('token-unusable:missing', 'text'))
        decision, client = decide(plan_with('open-or-update'), unusable)
        self.assertFalse(decision.open_or_update)
        self.assertEqual([thing.key for thing in decision.things_to_check], ['token-unusable:missing'])
        self.assertEqual(client.write_calls(), [])

    def test_leave_alone_and_nothing_make_no_write(self) -> None:
        for action in ('leave-alone', 'nothing'):
            decision, client = decide(plan_with(action, 7))
            self.assertFalse(decision.open_or_update, action)
            self.assertEqual(client.write_calls(), [], action)


class MainMovedTests(unittest.TestCase):
    def test_open_or_update_is_cancelled_with_a_note(self) -> None:
        decision, client = decide(plan_with('open-or-update'), newest_commit='b' * 40)
        self.assertTrue(decision.main_moved)
        self.assertFalse(decision.open_or_update)
        self.assertIn('main moved during this run (now `bbbbbbb`)', decision.notes[0])
        self.assertEqual(client.write_calls(), [])

    def test_close_is_cancelled_too(self) -> None:
        decision, client = decide(plan_with('close', 7), newest_commit='b' * 40)
        self.assertTrue(decision.main_moved)
        self.assertFalse(decision.open_or_update)
        self.assertIsNone(decision.closed_pull_request)
        self.assertEqual(client.write_calls(), [])


class ClosePathTests(unittest.TestCase):
    def test_close_patches_state_and_marker_through_the_github_token_client(self) -> None:
        decision, client = decide(plan_with('close', 7))
        writes = client.write_calls()
        self.assertEqual(len(writes), 1)
        self.assertEqual((writes[0].method, writes[0].path, writes[0].token_name),
                         ('PATCH', '/repos/%s/pulls/7' % REPO, 'GITHUB_TOKEN'))
        self.assertEqual(writes[0].body['state'], 'closed')
        self.assertTrue(writes[0].body['body'].startswith('The old body\n\n' + CLOSED_BY_BOT_MARKER))
        self.assertEqual(decision.closed_pull_request, 7)
        self.assertTrue(decision.open_or_update)

    def test_close_with_unusable_token_still_closes_but_does_not_push(self) -> None:
        decision, client = decide(plan_with('close', 7), UNUSABLE)
        self.assertEqual(len(client.write_calls()), 1)
        self.assertFalse(decision.open_or_update)
        self.assertEqual(decision.closed_pull_request, 7)


class OutputsTests(unittest.TestCase):
    def test_decision_json_has_the_schema_fields(self) -> None:
        decision, _ = decide(plan_with('open-or-update'))
        self.assertEqual(set(decision.to_json()), {'status', 'token', 'identity', 'action', 'open_or_update', 'main_moved',
                                                   'closed_pull_request', 'things_to_check', 'notes'})
        self.assertEqual(set(decision.to_json()['token']), {'usable', 'reason', 'login', 'user_id', 'expires_at'})

    def test_step_outputs(self) -> None:
        plan = plan_with('open-or-update')
        decision, _ = decide(plan)
        self.assertEqual(step_outputs(decision, plan), {
            'open-or-update': 'true', 'identity': USABLE.identity, 'title': 'The title',
            'commit-message': 'The message\n\nGenerated-by: x'})


if __name__ == '__main__':
    unittest.main()
