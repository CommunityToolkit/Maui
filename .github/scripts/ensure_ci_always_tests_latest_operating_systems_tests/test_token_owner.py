from __future__ import annotations

import unittest
from datetime import datetime, timedelta, timezone
from typing import Any, Dict, Optional

from ensure_ci_always_tests_latest_operating_systems.token_owner import check_token, parse_expiration

from .helpers import FakeGitHub

REPO = 'CommunityToolkit/Maui'
NOW = datetime(2026, 11, 21, 12, 0, 0, tzinfo=timezone.utc)


def pat_client_answering(user: Any, repository: Any) -> FakeGitHub:
    client = FakeGitHub({'GET /user': user, 'GET /repos/' + REPO: repository}, token_name='PAT')
    client.repo = REPO
    return client


def classic_user(scopes: str = 'public_repo, workflow', expiration: Optional[str] = None) -> Any:
    headers: Dict[str, str] = {'X-OAuth-Scopes': scopes}
    if expiration:
        headers['GitHub-Authentication-Token-Expiration'] = expiration
    return 200, headers, {'login': 'TheCodeTraveler', 'id': 13558917}


def pushable_repository() -> Any:
    return 200, {}, {'permissions': {'push': True}}


def run_check(user: Any, repository: Any = None, token: str = 'a-token'):
    client = pat_client_answering(user, repository if repository is not None else pushable_repository())
    return check_token(token, lambda _: client, NOW), client


class UnusableTokenTests(unittest.TestCase):
    def test_missing_secret_makes_no_call(self) -> None:
        client = pat_client_answering(classic_user(), pushable_repository())
        status = check_token('', lambda _: client, NOW)
        self.assertEqual((status.usable, status.reason), (False, 'missing'))
        self.assertEqual(client.calls, [])
        self.assertEqual([thing.key for thing in status.things_to_check], ['token-unusable:missing'])

    def test_none_counts_as_missing(self) -> None:
        self.assertEqual(check_token(None, lambda _: None, NOW).reason, 'missing')

    def test_rejected_on_401(self) -> None:
        status, client = run_check((401, {}, {'message': 'Bad credentials'}))
        self.assertEqual((status.usable, status.reason), (False, 'rejected'))
        self.assertEqual(len(client.calls), 1)

    def test_rejected_on_other_repository_error(self) -> None:
        status, _ = run_check(classic_user(), (403, {}, {'message': 'Resource not accessible'}))
        self.assertEqual(status.reason, 'rejected')

    def test_missing_scope_when_workflow_is_absent(self) -> None:
        status, client = run_check(classic_user('public_repo'))
        self.assertEqual((status.usable, status.reason), (False, 'missing-scope'))
        self.assertEqual(status.login, 'TheCodeTraveler')
        self.assertEqual(len(client.calls), 1)

    def test_missing_scope_when_only_workflow_is_present(self) -> None:
        status, _ = run_check(classic_user('workflow'))
        self.assertEqual(status.reason, 'missing-scope')

    def test_no_push_access(self) -> None:
        status, _ = run_check(classic_user(), (200, {}, {'permissions': {'push': False}}))
        self.assertEqual((status.usable, status.reason), (False, 'no-push-access'))
        self.assertEqual([thing.key for thing in status.things_to_check], ['token-unusable:no-push-access'])

    def test_organization_forbids_classic_tokens(self) -> None:
        message = ("The 'CommunityToolkit' organization forbids access via a personal access token (classic). "
                   "Please use a fine-grained personal access token.")
        status, _ = run_check(classic_user(), (403, {}, {'message': message}))
        self.assertEqual((status.usable, status.reason), (False, 'organization-forbids-classic-tokens'))
        self.assertIn('classic', status.things_to_check[0].text)


class UsableTokenTests(unittest.TestCase):
    def test_public_repo_and_workflow_are_usable_with_exactly_two_calls(self) -> None:
        status, client = run_check(classic_user('public_repo, workflow'))
        self.assertTrue(status.usable)
        self.assertEqual([(call.method, call.path) for call in client.calls],
                         [('GET', '/user'), ('GET', '/repos/' + REPO)])
        self.assertEqual({call.token_name for call in client.calls}, {'PAT'})
        self.assertFalse(any(call.is_write for call in client.calls))

    def test_repo_and_workflow_are_usable(self) -> None:
        status, _ = run_check(classic_user('repo, workflow'))
        self.assertTrue(status.usable)

    def test_fine_grained_token_has_no_scopes_header(self) -> None:
        status, _ = run_check((200, {}, {'login': 'TheCodeTraveler', 'id': 13558917}))
        self.assertTrue(status.usable)

    def test_identity_uses_the_noreply_address(self) -> None:
        status, _ = run_check(classic_user())
        self.assertEqual(status.identity, 'TheCodeTraveler <13558917+TheCodeTraveler@users.noreply.github.com>')
        self.assertEqual((status.login, status.user_id), ('TheCodeTraveler', 13558917))

    def test_unusable_token_with_known_owner_still_has_an_identity(self) -> None:
        status, _ = run_check(classic_user('workflow'))
        self.assertIn('13558917+TheCodeTraveler', status.identity)


class ExpirationTests(unittest.TestCase):
    def test_utc_form(self) -> None:
        self.assertEqual(parse_expiration('2026-12-31 23:59:59 UTC'), datetime(2026, 12, 31, 23, 59, 59, tzinfo=timezone.utc))

    def test_offset_form_is_converted_to_utc(self) -> None:
        self.assertEqual(parse_expiration('2026-12-31 15:59:59 -0800'),
                         datetime(2026, 12, 31, 23, 59, 59, tzinfo=timezone.utc))
        self.assertEqual(parse_expiration('2026-12-31 23:59:59 +0100'),
                         datetime(2026, 12, 31, 22, 59, 59, tzinfo=timezone.utc))

    def test_plain_form(self) -> None:
        self.assertEqual(parse_expiration('2026-12-31 23:59:59'), datetime(2026, 12, 31, 23, 59, 59, tzinfo=timezone.utc))

    def test_unknown_forms_count_as_no_expiry(self) -> None:
        for text in (None, '', 'soon', '2026-13-45 00:00:00 UTC'):
            self.assertIsNone(parse_expiration(text), text)

    def test_thirteen_days_warns_and_stays_usable(self) -> None:
        expiry = (NOW + timedelta(days=13)).strftime('%Y-%m-%d %H:%M:%S UTC')
        status, _ = run_check(classic_user(expiration=expiry))
        self.assertTrue(status.usable)
        self.assertEqual([thing.key for thing in status.things_to_check], ['token-expires-soon:2026-12-04'])

    def test_fifteen_days_does_not_warn(self) -> None:
        expiry = (NOW + timedelta(days=15)).strftime('%Y-%m-%d %H:%M:%S UTC')
        status, _ = run_check(classic_user(expiration=expiry))
        self.assertTrue(status.usable)
        self.assertEqual(status.things_to_check, [])

    def test_offset_header_warns_by_the_utc_day(self) -> None:
        status, _ = run_check(classic_user(expiration='2026-12-04 15:00:00 -0800'))
        self.assertEqual([thing.key for thing in status.things_to_check], ['token-expires-soon:2026-12-04'])

    def test_no_header_means_no_expiry(self) -> None:
        status, _ = run_check(classic_user())
        self.assertIsNone(status.expires_at)
        self.assertEqual(status.things_to_check, [])


if __name__ == '__main__':
    unittest.main()
