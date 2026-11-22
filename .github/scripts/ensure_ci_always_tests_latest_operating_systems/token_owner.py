"""Finds out who owns the maintainer's token and whether it can push, using only two read calls."""
from __future__ import annotations

import re
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from typing import Any, Callable, List, Optional

from .data_types import ThingToCheck

EXPIRES_SOON_WITHIN = timedelta(days=14)
EXPIRATION_HEADER = 'GitHub-Authentication-Token-Expiration'
SCOPES_HEADER = 'X-OAuth-Scopes'

REASON_TEXTS = {
    'missing': 'the secret holding the maintainer token is empty or not set',
    'malformed': 'the secret holding the maintainer token has more than one line or a space in it, so set the secret again with only the token',
    'rejected': 'GitHub rejected the maintainer token (expired, revoked or mistyped)',
    'missing-scope': 'the token lacks the `workflow` scope or the `public_repo` scope',
    'no-push-access': 'the token owner can not push to this repository',
    'organization-forbids-classic-tokens': "the organization doesn't accept classic tokens",
}

_VISIBLE_ASCII = re.compile(r'[\x21-\x7e]+')
_EXPIRATION = re.compile(r'(\d{4})-(\d{2})-(\d{2}) (\d{2}):(\d{2}):(\d{2})(?: (UTC)|\s*([+-])(\d{2}):?(\d{2}))?', re.ASCII)


@dataclass
class TokenStatus:
    usable: bool
    reason: str
    login: str = ''
    user_id: Optional[int] = None
    identity: str = ''
    expires_at: Optional[datetime] = None
    things_to_check: List[ThingToCheck] = field(default_factory=list)

    def to_json(self) -> Any:
        return {'usable': self.usable, 'reason': self.reason, 'login': self.login, 'user_id': self.user_id,
                'identity': self.identity,
                'expires_at': self.expires_at.isoformat() if self.expires_at is not None else None,
                'things_to_check': [thing.to_json() for thing in self.things_to_check]}


def parse_expiration(header: Optional[str]) -> Optional[datetime]:
    """GitHub writes '2026-12-31 23:59:59 UTC' or '2026-12-31 15:59:59 -0800'; an unknown shape counts as no expiry."""
    match = _EXPIRATION.fullmatch((header or '').strip())
    if match is None:
        return None
    year, month, day, hour, minute, second = (int(match.group(index)) for index in range(1, 7))
    try:
        moment = datetime(year, month, day, hour, minute, second, tzinfo=timezone.utc)
    except ValueError:
        return None
    if match.group(8):
        sign = 1 if match.group(8) == '+' else -1
        moment = moment - sign * timedelta(hours=int(match.group(9)), minutes=int(match.group(10)))
    return moment


def _unusable(reason: str, login: str = '', user_id: Optional[int] = None) -> TokenStatus:
    identity = '%s <%s+%s@users.noreply.github.com>' % (login, user_id, login) if login and user_id is not None else ''
    thing = ThingToCheck('token-unusable:' + reason, 'The maintainer token can not be used: %s.' % REASON_TEXTS[reason])
    return TokenStatus(usable=False, reason=reason, login=login, user_id=user_id, identity=identity,
                       things_to_check=[thing])


def _message_of(data: Any) -> str:
    return str(data.get('message', '')) if isinstance(data, dict) else ''


def _scopes_problem(headers: Any) -> bool:
    """A fine-grained token sends no scopes header; the repository call and the push decide for it."""
    if SCOPES_HEADER not in headers:
        return False
    scopes = {scope.strip() for scope in str(headers.get(SCOPES_HEADER)).split(',') if scope.strip()}
    return 'workflow' not in scopes or not ({'public_repo', 'repo'} & scopes)


def check_token(token: Optional[str], make_client: Callable[[str], Any], now: datetime) -> TokenStatus:
    token = (token or '').strip()
    if not token:
        return _unusable('missing')
    if _VISIBLE_ASCII.fullmatch(token) is None:
        return _unusable('malformed')
    client = make_client(token)

    status, headers, data = client.get('/user')
    if status != 200 or not isinstance(data, dict) or not data.get('login'):
        return _unusable('rejected')
    login, user_id = str(data['login']), data.get('id')
    if _scopes_problem(headers):
        return _unusable('missing-scope', login, user_id)

    status, _, repository = client.get('/repos/%s' % client.repo)
    if status == 403 and 'personal access token (classic)' in _message_of(repository).lower():
        return _unusable('organization-forbids-classic-tokens', login, user_id)
    if status != 200 or not isinstance(repository, dict):
        return _unusable('rejected', login, user_id)
    if not (repository.get('permissions') or {}).get('push'):
        return _unusable('no-push-access', login, user_id)

    expires_at = parse_expiration(headers.get(EXPIRATION_HEADER))
    things: List[ThingToCheck] = []
    if expires_at is not None and expires_at - now <= EXPIRES_SOON_WITHIN:
        day = expires_at.astimezone(timezone.utc).date().isoformat()
        things.append(ThingToCheck('token-expires-soon:' + day,
                                   'The maintainer token expires on %s. Create a new one and replace the secret before then.' % day))
    return TokenStatus(usable=True, reason='', login=login, user_id=user_id,
                       identity='%s <%s+%s@users.noreply.github.com>' % (login, user_id, login),
                       expires_at=expires_at, things_to_check=things)
