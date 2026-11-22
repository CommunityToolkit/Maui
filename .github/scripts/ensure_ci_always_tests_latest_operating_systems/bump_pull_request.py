"""Reads the bot's own pull requests, so a night's plan knows what is open, what a person touched and what was rejected."""
from __future__ import annotations

import re
from typing import Any, Dict, List, Optional, Tuple

from . import markers
from .data_types import PinnedVersions, SafeChange, ThingToCheck
from .errors import GitHubApiError
from .line_editor import android_leg_lines
from .markdown_text import markdown_code
from .pinned_versions import XCODE_VERSION_KEYS
from .version_numbers import parse_android_level

SOURCE_KEY = 'source:bump-pull-request'
_ANDROID_FILE = markers.PIN_FILES[1]
_SHARED_ENV_FILE = markers.PIN_FILES[0]
_CARRIED_SOURCE = 'carried-forward-from-the-open-pull-request'
_ANDROID_LEVEL_KINDS = ('replace-api-level', 'insert-android-leg')
_XCODE_VERSION_KINDS = ('set-xcode-version',)

_READ_CHECKS_QUERY = """
query ReadBumpPullRequestChecks($owner: String!, $name: String!, $number: Int!) {
  repository(owner: $owner, name: $name) {
    pullRequest(number: $number) {
      commits(last: 1) {
        nodes { commit { oid statusCheckRollup { state } } }
      }
    }
  }
}
"""


def read_bump_pull_request_state(github: Any, base: str, bot_branch: str) -> Dict[str, Any]:
    """On any API failure the state is an error and the caller leaves the pull request alone: nothing is known about it."""
    try:
        return _read_state(github, base, bot_branch)
    except GitHubApiError as error:
        return _error_state('could not read the bump pull requests: %s' % error)


def _error_state(message: str) -> Dict[str, Any]:
    return {'status': 'error', 'error': message, 'open': None, 'rejected': [], 'things_to_check': []}


def _read_state(github: Any, base: str, bot_branch: str) -> Dict[str, Any]:
    owner = github.repo.split('/')[0]
    head_filter = '%s:%s' % (owner, bot_branch)
    open_pulls = github.paginate('/repos/%s/pulls' % github.repo,
                                 {'state': 'open', 'head': head_filter, 'base': base, 'per_page': 100})
    if len(open_pulls) > 1:
        numbers = sorted(pull['number'] for pull in open_pulls)
        if len(numbers) == 2:
            return _error_state('two open bump PRs: #%d and #%d; close one' % (numbers[0], numbers[1]))
        return _error_state('several open bump PRs (%s); close all but one' % ', '.join('#%d' % n for n in numbers))
    closed_pulls = github.paginate('/repos/%s/pulls' % github.repo,
                                   {'state': 'closed', 'head': head_filter, 'base': base, 'per_page': 100})
    open_state: Optional[Dict[str, Any]] = None
    things_to_check: List[ThingToCheck] = []
    if open_pulls:
        open_state, things_to_check = _read_open_pull_request(github, open_pulls[0])
    return {'status': 'ok', 'open': open_state,
            'rejected': _find_rejected_changes(open_pulls, closed_pulls),
            'things_to_check': [thing.to_json() for thing in things_to_check]}


def _read_open_pull_request(github: Any, pull: Dict[str, Any]) -> Tuple[Dict[str, Any], List[ThingToCheck]]:
    number = pull['number']
    author = (pull.get('user') or {}).get('login')
    head_sha = pull['head']['sha']
    commits = github.paginate('/repos/%s/pulls/%d/commits' % (github.repo, number))
    person_commits = [_describe_commit(commit) for commit in commits if not _is_bot_commit(commit, author)]
    checks_failed = _read_checks_failed(github, number, head_sha)
    change_list = markers.read_change_list(pull.get('body'))
    state = {'number': number, 'author': author, 'head_sha': head_sha,
             'change_list': change_list if change_list is not None else [],
             'person_commits': person_commits, 'checks_failed': checks_failed}
    things: List[ThingToCheck] = []
    if person_commits:
        first = person_commits[0]
        things.append(ThingToCheck(
            key='pull-request-has-a-person-commit:%d' % number,
            text=('#%d has commit %s by %s (%s), which the bot didn\'t make, so it no longer updates #%d. '
                  'Close #%d and the bot opens a fresh PR next night, or merge it yourself.') % (
                      number, markdown_code(first['sha'][:7]), markdown_code(first['login']),
                      markdown_code(first['subject']), number, number)))
    if checks_failed:
        things.append(ThingToCheck(
            key='pull-request-checks-failed:%d' % number,
            text=('The checks on #%d failed, so it can\'t auto-merge. Fix the cause, or close #%d and the bot '
                  'opens a fresh PR next night.') % (number, number)))
    return state, things


def _is_bot_commit(commit: Dict[str, Any], pull_request_author: Optional[str]) -> bool:
    """The trailer alone can be typed by anyone, so the commit's author must also be the account that opened the PR."""
    if len(commit.get('parents') or []) > 1:
        return False
    message = (commit.get('commit') or {}).get('message') or ''
    has_trailer = markers.COMMIT_TRAILER in [line.strip() for line in message.splitlines()]
    login = (commit.get('author') or {}).get('login')
    return has_trailer and login is not None and login == pull_request_author


def _describe_commit(commit: Dict[str, Any]) -> Dict[str, str]:
    message = (commit.get('commit') or {}).get('message') or ''
    lines = message.splitlines()
    return {'sha': commit.get('sha') or '',
            'login': (commit.get('author') or {}).get('login') or ((commit.get('commit') or {}).get('author') or {}).get('name') or 'unknown',
            'subject': lines[0] if lines else ''}


def _read_checks_failed(github: Any, number: int, head_sha: str) -> bool:
    owner, name = github.repo.split('/', 1)
    data = github.graphql(_READ_CHECKS_QUERY, {'owner': owner, 'name': name, 'number': number})
    pull = ((data.get('repository') or {}).get('pullRequest')) or {}
    nodes = (pull.get('commits') or {}).get('nodes') or []
    if not nodes:
        return False
    commit = nodes[-1].get('commit') or {}
    if commit.get('oid') not in (None, head_sha):
        return False
    state = (commit.get('statusCheckRollup') or {}).get('state')
    return state in ('FAILURE', 'ERROR')


def _find_rejected_changes(open_pulls: List[Dict[str, Any]], closed_pulls: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    bump_pulls = [pull for pull in open_pulls + closed_pulls if markers.has_marker(pull.get('body'), markers.PULL_REQUEST_MARKER)]
    pairs_listed_by: Dict[Tuple[str, str], List[int]] = {}
    for pull in bump_pulls:
        for pair in _change_pairs(pull):
            pairs_listed_by.setdefault(pair, []).append(pull['number'])
    rejected: Dict[Tuple[str, str], int] = {}
    for pull in bump_pulls:
        if not _is_closed_by_a_person(pull):
            continue
        for pair in _change_pairs(pull):
            reopened_later = any(number > pull['number'] for number in pairs_listed_by[pair])
            if not reopened_later:
                rejected[pair] = max(rejected.get(pair, 0), pull['number'])
    return [{'pin': pin, 'new': new, 'pull_request': number}
            for (pin, new), number in sorted(rejected.items(), key=lambda item: (item[1], item[0]))]


def _is_closed_by_a_person(pull: Dict[str, Any]) -> bool:
    body = pull.get('body') or ''
    return (pull.get('state') == 'closed' and not pull.get('merged_at')
            and markers.CLOSED_BY_BOT_MARKER not in body)


def _change_pairs(pull: Dict[str, Any]) -> List[Tuple[str, str]]:
    changes = markers.read_change_list(pull.get('body')) or []
    return [(str(change.get('pin', '')), str(change.get('new', ''))) for change in changes if change.get('pin')]


def rebuild_change(pinned: PinnedVersions, change: Dict[str, Any]) -> Optional[SafeChange]:
    """Places a change of the open PR against today's main; None when main moved past the value it replaced."""
    kind = change.get('kind')
    pin = change.get('pin') or ''
    new = change.get('new')
    if not isinstance(pin, str) or not isinstance(new, str) or not new:
        return None
    old = change.get('old') or None
    inserted_lines = list(change.get('inserted_lines') or [])
    if kind in _ANDROID_LEVEL_KINDS and not _parse_as_android_levels(new, old):
        return None
    if kind in _XCODE_VERSION_KINDS and not _is_xcode_version_text(new, old):
        return None
    rebuilders = {
        'set-xcode-version': _rebuild_xcode_version,
        'replace-api-level': _rebuild_api_level,
        'insert-android-leg': _rebuild_android_leg_insert,
        'insert-ios-leg': _rebuild_ios_leg_insert,
        'replace-runner-label': _rebuild_runner_label,
    }
    rebuild = rebuilders.get(kind)
    if rebuild is None or change.get('file') not in markers.PIN_FILES:
        return None
    return rebuild(pinned, change, pin, old, new, inserted_lines)


def _is_xcode_version_text(*texts: Optional[str]) -> bool:
    return all(re.fullmatch(r'\d+\.\d+', text, re.ASCII) for text in texts if text)


def _parse_as_android_levels(*texts: Optional[str]) -> bool:
    """The change list comes out of a pull request body that a person could have edited."""
    try:
        for text in texts:
            if text:
                parse_android_level(text)
    except ValueError:
        return False
    return True


def _carried(change: Dict[str, Any], line: int, old: Optional[str], new: str, inserted_lines: List[str],
             pin: str, description: str) -> SafeChange:
    return SafeChange(family=change.get('family') or '', kind=change['kind'], file=change['file'], line=line,
                      old=old, new=new, inserted_lines=inserted_lines, pin=pin, description=description,
                      source=_CARRIED_SOURCE)


def _rebuild_xcode_version(pinned: PinnedVersions, change: Dict[str, Any], pin: str, old: Optional[str], new: str,
                           inserted_lines: List[str]) -> Optional[SafeChange]:
    key = pin.split(':', 1)[1] if pin.startswith('env:') else ''
    current = pinned.shared_env.get(key)
    if key not in XCODE_VERSION_KEYS or current is None or change['file'] != _SHARED_ENV_FILE:
        return None
    if old is None or current.value != old:
        return None
    return _carried(change, current.line, old, new, [], pin, 'Use Xcode %s for %s' % (new, key))


def _parse_android_pin(pin: str) -> Optional[Tuple[str, int]]:
    if not pin.startswith('android-leg:'):
        return None
    target, _, major_text = pin[len('android-leg:'):].rpartition(':')
    if not target or not major_text.isdigit():
        return None
    return target, int(major_text)


def _rebuild_api_level(pinned: PinnedVersions, change: Dict[str, Any], pin: str, old: Optional[str], new: str,
                       inserted_lines: List[str]) -> Optional[SafeChange]:
    parsed = _parse_android_pin(pin)
    if parsed is None or change['file'] != _ANDROID_FILE or old is None:
        return None
    target, major = parsed
    for leg in pinned.android_legs:
        if leg.target == target and leg.level.major == major:
            if leg.level.text != old:
                return None
            return _carried(change, leg.api_level_line, old, new, [], pin,
                            'Move the Android leg from API %s to %s' % (old, new))
    return None


def _rebuild_android_leg_insert(pinned: PinnedVersions, change: Dict[str, Any], pin: str, old: Optional[str],
                                new: str, inserted_lines: List[str]) -> Optional[SafeChange]:
    parsed = _parse_android_pin(pin)
    if parsed is None or change['file'] != _ANDROID_FILE or not inserted_lines:
        return None
    target, major = parsed
    new_level = parse_android_level(new)
    main_already_has_the_leg = any(leg.level == new_level or (leg.target == target and leg.level.major == major)
                                   for leg in pinned.android_legs)
    if main_already_has_the_leg:
        return None
    lower_legs = [leg for leg in pinned.android_legs if leg.level < new_level]
    line = max(lower_legs, key=lambda leg: leg.level).entry_end if lower_legs else pinned.android_include_line
    return _carried(change, line, None, new, android_leg_lines(pinned.android_dash_prefix, new_level.text, target), pin,
                    'Add an Android leg for API %s' % new)


def _rebuild_ios_leg_insert(pinned: PinnedVersions, change: Dict[str, Any], pin: str, old: Optional[str], new: str,
                            inserted_lines: List[str]) -> Optional[SafeChange]:
    if not pin.startswith('ios-leg:') or change['file'] != _ANDROID_FILE or not inserted_lines:
        return None
    if any(leg.ios_version == new for leg in pinned.ios_legs):
        return None
    return _carried(change, pinned.ios_include_line, None, new, inserted_lines, pin,
                    'Keep an iOS %s leg' % new)


def _rebuild_runner_label(pinned: PinnedVersions, change: Dict[str, Any], pin: str, old: Optional[str], new: str,
                          inserted_lines: List[str]) -> Optional[SafeChange]:
    if old is None or pinned.apple_label.label != old:
        return None
    for file, line, job_id in pinned.apple_label.occurrences:
        if pin == 'apple-label:%s:%s' % (file, job_id) and file == change['file']:
            return _carried(change, line, old, new, [], pin, 'Run the Apple job `%s` of %s on `%s`' % (job_id, file, new))
    return None
