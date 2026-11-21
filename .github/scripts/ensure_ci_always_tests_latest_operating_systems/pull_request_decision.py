"""Decides, before any write, whether the pull request step may push, and closes a pull request that main no longer needs."""
from __future__ import annotations

import urllib.parse
from dataclasses import dataclass, field
from datetime import datetime
from typing import Any, Dict, List, Optional

from .data_types import ThingToCheck
from .errors import GitHubApiError
from .markers import CLOSED_BY_BOT_MARKER
from .token_owner import TokenStatus

CLOSED_BY_BOT_NOTE = 'Closed by the bot: main no longer needs these changes.'


@dataclass
class Decision:
    status: str
    token: TokenStatus
    action: str
    open_or_update: bool = False
    main_moved: bool = False
    closed_pull_request: Optional[int] = None
    things_to_check: List[ThingToCheck] = field(default_factory=list)
    notes: List[str] = field(default_factory=list)

    @property
    def identity(self) -> str:
        return self.token.identity

    def to_json(self) -> Dict[str, Any]:
        return {
            'status': self.status,
            'token': {'usable': self.token.usable, 'reason': self.token.reason, 'login': self.token.login,
                      'user_id': self.token.user_id,
                      'expires_at': self.token.expires_at.isoformat() if self.token.expires_at is not None else None},
            'identity': self.identity,
            'action': self.action,
            'open_or_update': self.open_or_update,
            'main_moved': self.main_moved,
            'closed_pull_request': self.closed_pull_request,
            'things_to_check': [thing.to_json() for thing in self.things_to_check],
            'notes': list(self.notes),
        }


def _plan_fields(plan: Any) -> Dict[str, Any]:
    return plan if isinstance(plan, dict) else plan.to_json()


def _newest_commit_of_base(github: Any, base: str) -> str:
    path = '/repos/%s/git/ref/heads/%s' % (github.repo, urllib.parse.quote(base, safe=''))
    status, _, data = github.get(path)
    if status != 200 or not isinstance(data, dict):
        raise GitHubApiError('GET', path, status, 'could not read the newest commit of %s' % base)
    return str(data['object']['sha'])


def _close_pull_request(github: Any, number: int) -> None:
    path = '/repos/%s/pulls/%d' % (github.repo, number)
    status, _, data = github.get(path)
    if status != 200 or not isinstance(data, dict):
        raise GitHubApiError('GET', path, status, 'could not read the pull request before closing it')
    body = '%s\n\n%s\n%s' % (data.get('body') or '', CLOSED_BY_BOT_MARKER, CLOSED_BY_BOT_NOTE)
    status, _, answer = github.patch(path, {'state': 'closed', 'body': body})
    if not 200 <= status < 300:
        message = answer.get('message', '') if isinstance(answer, dict) else ''
        raise GitHubApiError('PATCH', path, status, message)


def decide_pull_request_update(plan: Any, token_status: TokenStatus, github: Any, base: str, base_sha: str,
                               now: datetime) -> Decision:
    """The maintainer token never reaches this code: only check_token uses it, so nothing here can push as the maintainer."""
    plan_fields = _plan_fields(plan)
    action = plan_fields.get('pull_request_action', 'nothing')
    decision = Decision(status='ok', token=token_status, action=action,
                        things_to_check=list(token_status.things_to_check))

    newest_commit = _newest_commit_of_base(github, base)
    if newest_commit != base_sha:
        decision.main_moved = True
        decision.notes.append('main moved during this run (now `%s`); the bot tries again next night.' % newest_commit[:7])
        return decision

    if action == 'close':
        number = plan_fields.get('open_pull_request')
        _close_pull_request(github, int(number))
        decision.closed_pull_request = int(number)
        decision.open_or_update = token_status.usable
    elif action == 'open-or-update':
        decision.open_or_update = token_status.usable
    return decision


def step_outputs(decision: Decision, plan: Any) -> Dict[str, str]:
    plan_fields = _plan_fields(plan)
    return {
        'open-or-update': 'true' if decision.open_or_update else 'false',
        'identity': decision.identity,
        'title': str(plan_fields.get('title', '')),
        'commit-message': str(plan_fields.get('commit_message', '')),
    }
