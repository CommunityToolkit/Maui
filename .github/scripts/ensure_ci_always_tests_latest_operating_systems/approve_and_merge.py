"""Approves the bump pull request and turns on auto-merge, but only for a commit that holds exactly the planned changes.

Every call here uses the GITHUB_TOKEN client. That identity is not the pull request's author or pusher, so its approval
counts under "approval of the most recent push", and it is not a ruleset bypass actor, so GitHub still enforces every rule.
"""
from __future__ import annotations

import base64
import binascii
import re
import time
from dataclasses import dataclass, field
from typing import Any, Callable, Dict, List, Optional, Tuple

from .change_validator import validate_changes
from .data_types import SafeChange, ThingToCheck
from .errors import GitHubApiError
from .line_editor import apply_changes_to_texts
from .markers import COMMIT_TRAILER, PIN_FILES
from .pinned_versions import DEFAULT_ANDROID_TARGET, read_android_legs
from .required_checks import read_branch_rules
from .version_numbers import AndroidLevel, parse_android_level, parse_utc

APPROVER_LOGIN = 'github-actions[bot]'
HEAD_READ_ATTEMPTS = 3
SECONDS_BETWEEN_HEAD_READS = 5
MERGEABLE_NOW_STATES = ('CLEAN', 'HAS_HOOKS', 'UNSTABLE')
MERGEABLE_NOW_ERROR_WORDS = ('clean status', 'unstable status', 'has_hooks status')
AUTO_MERGE_EVENTS_ON = ('auto_merge_enabled', 'auto_squash_enabled', 'auto_rebase_enabled')
AUTO_MERGE_EVENT_OFF = 'auto_merge_disabled'
REFUSALS_THAT_KEEP_AUTO_MERGE_ON = ('threads', 'conflicts', 'hold')

REFUSAL_REASONS: Dict[str, str] = {
    'state': 'the pull request is not open, is a draft, or does not target the base branch',
    'author': "the pull request was not opened by the token owner",
    'branch': "the pull request does not come from the bot's branch in this repository",
    'head': "the pull request's newest commit is not the one the push step reported",
    'commits': 'the pull request does not hold exactly one commit made by the bot with its trailer',
    'base': 'the commit is not built directly on the base commit this run planned from',
    'files': 'the pull request changes a file that is not one of the three version files',
    'exact-changes': 'the files differ from the planned version changes',
    'validator': 'an edit is not one the bot is allowed to make on its own',
    'levels-rise': 'an Android API level does not rise above the existing ones',
    'threads': 'a review conversation is not resolved',
    'conflicts': 'the pull request has merge conflicts',
    'last-push-rule': 'the base branch no longer requires approval of the most recent push',
    'hold': 'a person turned auto-merge off',
}

_API_LEVEL_IN_INSERTED_LINE = re.compile(r"api-level: '(\d+(?:\.\d+)?)'", re.ASCII)
_TARGET_IN_INSERTED_LINE = re.compile(r'^\s*(?:-\s+)?target: (\S+)$', re.ASCII)

_READ_PULL_REQUEST_FACTS = """
query ReadPullRequestFacts($owner: String!, $name: String!, $number: Int!, $after: String) {
  repository(owner: $owner, name: $name) {
    pullRequest(number: $number) {
      id
      isDraft
      mergeable
      mergeStateStatus
      autoMergeRequest { enabledAt }
      commits(last: 1) { nodes { commit { committedDate } } }
      reviewThreads(first: 100, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes { isResolved }
      }
    }
  }
}
"""
_TURN_ON_AUTO_MERGE = """
mutation TurnOnAutoMerge($id: ID!, $sha: GitObjectID!) {
  enablePullRequestAutoMerge(input: {pullRequestId: $id, mergeMethod: SQUASH, expectedHeadOid: $sha}) {
    pullRequest { autoMergeRequest { enabledAt } }
  }
}
"""
_TURN_OFF_AUTO_MERGE = """
mutation TurnOffAutoMerge($id: ID!) {
  disablePullRequestAutoMerge(input: {pullRequestId: $id}) {
    pullRequest { autoMergeRequest { enabledAt } }
  }
}
"""


@dataclass
class CheckResult:
    refusals: List[str]
    pull_request_facts: Dict[str, Any]
    refusal_details: Dict[str, List[str]] = field(default_factory=dict)


def _get_ok(github: Any, path: str, params: Optional[Dict[str, Any]] = None) -> Any:
    status, _, data = github.get(path, params)
    if status != 200:
        message = data.get('message', '') if isinstance(data, dict) else ''
        raise GitHubApiError('GET', path, status, message)
    return data


def _read_pull_request(github: Any, number: int, head_sha: str, sleep: Callable[[float], None]) -> Dict[str, Any]:
    """GitHub can move a pull request's head a moment after the push, and a refusal here would cost a whole night."""
    path = '/repos/%s/pulls/%d' % (github.repo, number)
    pull_request: Dict[str, Any] = {}
    for attempt in range(HEAD_READ_ATTEMPTS):
        if attempt > 0:
            sleep(SECONDS_BETWEEN_HEAD_READS)
        pull_request = _get_ok(github, path)
        if (pull_request.get('head') or {}).get('sha') == head_sha:
            break
    return pull_request


def _read_graphql_facts(github: Any, number: int) -> Dict[str, Any]:
    owner, name = github.repo.split('/', 1)
    unresolved_threads = 0
    after: Optional[str] = None
    while True:
        data = github.graphql(_READ_PULL_REQUEST_FACTS, {'owner': owner, 'name': name, 'number': number, 'after': after})
        pull_request = data['repository']['pullRequest']
        threads = pull_request['reviewThreads']
        unresolved_threads += sum(1 for thread in threads['nodes'] if not thread['isResolved'])
        if not threads['pageInfo']['hasNextPage']:
            break
        after = threads['pageInfo']['endCursor']
    commits = (pull_request.get('commits') or {}).get('nodes') or []
    auto_merge = pull_request.get('autoMergeRequest')
    return {
        'node_id': pull_request['id'],
        'is_draft': bool(pull_request.get('isDraft')),
        'mergeable': pull_request.get('mergeable'),
        'merge_state_status': pull_request.get('mergeStateStatus'),
        'auto_merge_enabled_at': auto_merge['enabledAt'] if auto_merge else None,
        'head_committed_date': commits[0]['commit']['committedDate'] if commits else None,
        'unresolved_threads': unresolved_threads,
    }


def _read_pin_file(github: Any, path: str, ref: str) -> str:
    data = _get_ok(github, '/repos/%s/contents/%s' % (github.repo, path), {'ref': ref})
    try:
        return base64.b64decode(data['content']).decode('utf-8')
    except (KeyError, TypeError, binascii.Error, UnicodeDecodeError) as error:
        raise GitHubApiError('GET', path, 200, 'could not decode the file at %s: %s' % (ref, error))


def _read_pin_files(github: Any, ref: str) -> Dict[str, str]:
    return {path: _read_pin_file(github, path, ref) for path in PIN_FILES}


def _latest_auto_merge_event_is_a_persons_turn_off(timeline: List[Dict[str, Any]]) -> bool:
    latest: Optional[Dict[str, Any]] = None
    for event in timeline:
        if event.get('event') in AUTO_MERGE_EVENTS_ON or event.get('event') == AUTO_MERGE_EVENT_OFF:
            latest = event
    if latest is None or latest.get('event') != AUTO_MERGE_EVENT_OFF:
        return False
    return (latest.get('actor') or {}).get('type') != 'Bot'


def _planned_changes(plan: Any) -> List[SafeChange]:
    plan_fields = plan if isinstance(plan, dict) else plan.to_json()
    return [change if isinstance(change, SafeChange) else SafeChange.from_json(change)
            for change in plan_fields.get('safe_changes', [])]


def _inserted_leg_level_and_target(change: SafeChange) -> Tuple[Optional[AndroidLevel], str]:
    level: Optional[AndroidLevel] = None
    target = DEFAULT_ANDROID_TARGET
    for line in change.inserted_lines:
        level_match = _API_LEVEL_IN_INSERTED_LINE.search(line)
        if level_match is not None:
            level = parse_android_level(level_match.group(1))
        target_match = _TARGET_IN_INSERTED_LINE.match(line)
        if target_match is not None:
            target = target_match.group(1)
    return level, target


def _level_problems(changes: List[SafeChange], base_device_tests: str) -> List[str]:
    problems: List[str] = []
    legs, _, _ = read_android_legs(base_device_tests)
    for change in changes:
        if change.kind == 'replace-api-level':
            if parse_android_level(change.new or '') <= parse_android_level(change.old or ''):
                problems.append('API %s does not rise above %s' % (change.new, change.old))
        elif change.kind == 'insert-android-leg':
            level, target = _inserted_leg_level_and_target(change)
            if level is None:
                problems.append('an inserted Android leg has no api-level line')
                continue
            for leg in legs:
                if leg.level.major == level.major and leg.target == target and level <= leg.level:
                    problems.append('API %s is not above the existing API %s of Android %d (%s)'
                                    % (level.text, leg.level.text, level.major, target))
    return problems


def _commit_problems(commits: List[Dict[str, Any]], owner_login: str, base_sha: str) -> Tuple[List[str], List[str]]:
    """(commits problems, base problems) for the commits of the pull request."""
    commit_problems: List[str] = []
    base_problems: List[str] = []
    if len(commits) != 1:
        return ['the pull request has %d commits, not one' % len(commits)], []
    commit = commits[0]
    message_lines = [line.rstrip('\r') for line in str((commit.get('commit') or {}).get('message', '')).split('\n')]
    if COMMIT_TRAILER not in message_lines:
        commit_problems.append('the commit message has no "%s" line' % COMMIT_TRAILER)
    author_login = (commit.get('author') or {}).get('login') or ''
    if author_login.lower() != owner_login.lower():
        commit_problems.append('the commit was made by %r, not by %r' % (author_login, owner_login))
    parents = [parent.get('sha') for parent in commit.get('parents') or []]
    if parents != [base_sha]:
        base_problems.append('the commit has parents %s, not only %s' % (parents, base_sha))
    return commit_problems, base_problems


def _last_push_rule_is_on(rules: Dict[str, Any]) -> bool:
    return (rules.get('status') == 'ok' and bool(rules.get('require_last_push_approval'))
            and int(rules.get('required_approving_review_count') or 0) >= 1)


def check_before_approving(github: Any, plan: Any, decision: Any, pull_request: int, head_sha: str, operation: str,
                           base: str, base_sha: str, bot_branch: str,
                           sleep: Callable[[float], None] = time.sleep) -> CheckResult:
    """pull_request is the number. Reads everything fresh; the result lists one code of REFUSAL_REASONS per failed check."""
    owner_login = decision.token.login if hasattr(decision, 'token') else decision['token']['login']
    repo = github.repo
    refusal_details: Dict[str, List[str]] = {}

    def refuse(code: str, *details: str) -> None:
        refusal_details.setdefault(code, []).extend(details or (REFUSAL_REASONS[code],))

    pull = _read_pull_request(github, pull_request, head_sha, sleep)
    commits = github.paginate('/repos/%s/pulls/%d/commits' % (repo, pull_request))
    compare = _get_ok(github, '/repos/%s/compare/%s...%s' % (repo, base, head_sha))
    reviews = github.paginate('/repos/%s/pulls/%d/reviews' % (repo, pull_request))
    timeline = github.paginate('/repos/%s/issues/%d/timeline' % (repo, pull_request))
    rules = read_branch_rules(github, base)
    graph = _read_graphql_facts(github, pull_request)
    base_texts = _read_pin_files(github, base_sha)
    head_texts = _read_pin_files(github, head_sha)

    head_repository = ((pull.get('head') or {}).get('repo') or {}).get('full_name')
    if pull.get('state') != 'open' or graph['is_draft'] or pull.get('draft') or (pull.get('base') or {}).get('ref') != base:
        refuse('state')
    if ((pull.get('user') or {}).get('login') or '').lower() != owner_login.lower():
        refuse('author', 'the pull request was opened by %r, not by the token owner %r'
               % ((pull.get('user') or {}).get('login'), owner_login))
    if (pull.get('head') or {}).get('ref') != bot_branch or head_repository != repo:
        refuse('branch', "the pull request comes from %r in %r, not from %r in %r"
               % ((pull.get('head') or {}).get('ref'), head_repository, bot_branch, repo))
    if (pull.get('head') or {}).get('sha') != head_sha:
        refuse('head', "the pull request's newest commit is %r, not %r" % ((pull.get('head') or {}).get('sha'), head_sha))

    commit_problems, parent_problems = _commit_problems(commits, owner_login, base_sha)
    if commit_problems:
        refuse('commits', *commit_problems)
    merge_base = (compare.get('merge_base_commit') or {}).get('sha')
    if parent_problems or merge_base != base_sha:
        refuse('base', *(parent_problems or ['the merge base is %r, not %r' % (merge_base, base_sha)]))

    changed_files = compare.get('files') or []
    unexpected_files = [entry.get('filename') for entry in changed_files
                        if entry.get('filename') not in PIN_FILES or entry.get('status') != 'modified']
    if unexpected_files:
        refuse('files', 'the pull request changes %s' % ', '.join(str(name) for name in unexpected_files))

    changes = _planned_changes(plan)
    try:
        expected_texts = apply_changes_to_texts(base_texts, changes)
    except ValueError as error:
        refuse('exact-changes', 'the planned changes do not apply to %s: %s' % (base_sha[:7], error))
    else:
        differing = [path for path in PIN_FILES if expected_texts[path] != head_texts[path]]
        if differing:
            refuse('exact-changes', 'differs from the planned changes: %s' % ', '.join(differing))

    validator_problems = validate_changes(base_texts, head_texts)
    if validator_problems:
        refuse('validator', *validator_problems)

    try:
        level_problems = _level_problems(changes, base_texts['.github/workflows/device-tests.yml'])
    except ValueError as error:
        level_problems = ['the Android levels can not be read: %s' % error]
    if level_problems:
        refuse('levels-rise', *level_problems)

    if graph['unresolved_threads']:
        refuse('threads', '%d review conversation(s) are not resolved' % graph['unresolved_threads'])
    if graph['mergeable'] in (False, 'CONFLICTING'):
        refuse('conflicts')
    if not _last_push_rule_is_on(rules):
        refuse('last-push-rule')
    if _latest_auto_merge_event_is_a_persons_turn_off(timeline):
        refuse('hold')

    approved_at_head = any((review.get('user') or {}).get('login') == APPROVER_LOGIN
                           and review.get('state') == 'APPROVED' and review.get('commit_id') == head_sha
                           for review in reviews)
    armed_for_this_head = False
    if graph['auto_merge_enabled_at'] and graph['head_committed_date']:
        armed_for_this_head = parse_utc(graph['auto_merge_enabled_at']) > parse_utc(graph['head_committed_date'])
    facts = dict(graph)
    facts.update({'number': pull_request, 'head_sha': head_sha, 'operation': operation,
                  'auto_merge_is_on': graph['auto_merge_enabled_at'] is not None,
                  'auto_merge_armed_for_this_head': armed_for_this_head,
                  'approved_at_head': approved_at_head})
    return CheckResult(refusals=[code for code in REFUSAL_REASONS if code in refusal_details], pull_request_facts=facts,
                       refusal_details=refusal_details)


def turn_off_auto_merge(github: Any, node_id: str) -> None:
    github.graphql(_TURN_OFF_AUTO_MERGE, {'id': node_id})


def turn_on_auto_merge(github: Any, node_id: str, head_sha: str) -> str:
    """'turned-on', or 'mergeable-now' when GitHub says the pull request can already merge and so can not be armed."""
    try:
        github.graphql(_TURN_ON_AUTO_MERGE, {'id': node_id, 'sha': head_sha})
    except GitHubApiError as error:
        text = error.message.lower()
        if any(words in text for words in MERGEABLE_NOW_ERROR_WORDS):
            return 'mergeable-now'
        raise
    return 'turned-on'


def approve(github: Any, number: int, head_sha: str, run_url: str, base: str = 'main', base_sha: str = '') -> None:
    body = ('Approved by ensure-ci-always-tests-latest-operating-systems.yml: this pull request holds exactly the planned '
            'version changes, on %s %s. %s' % (base, base_sha[:7], run_url))
    status, _, data = github.post('/repos/%s/pulls/%d/reviews' % (github.repo, number),
                                  {'commit_id': head_sha, 'event': 'APPROVE', 'body': body})
    if not 200 <= status < 300:
        raise GitHubApiError('POST', '/repos/%s/pulls/%d/reviews' % (github.repo, number), status,
                             data.get('message', '') if isinstance(data, dict) else '')


def merge_now(github: Any, number: int, head_sha: str) -> Tuple[bool, str]:
    """sha makes GitHub refuse a merge of any commit other than the one the checks vouched for."""
    path = '/repos/%s/pulls/%d/merge' % (github.repo, number)
    status, _, data = github.put(path, {'sha': head_sha, 'merge_method': 'squash'})
    message = str(data.get('message', '')) if isinstance(data, dict) else ''
    if 200 <= status < 300:
        return True, message
    if status in (403, 405, 409, 422):
        return False, message or 'status %d' % status
    raise GitHubApiError('PUT', path, status, message)


def _refusal_things_to_check(number: int, result: CheckResult) -> List[ThingToCheck]:
    things: List[ThingToCheck] = []
    if 'hold' in result.refusals:
        things.append(ThingToCheck('pull-request-auto-merge-turned-off:%d' % number,
                                   'A person turned off auto-merge on #%d, so the bot leaves it off. Turn it back on, or close #%d.'
                                   % (number, number)))
    if 'last-push-rule' in result.refusals:
        things.append(ThingToCheck('last-push-approval-rule-off',
                                   "The main branch rules no longer require approval of the most recent push, so the bot doesn't "
                                   'approve its own pull requests: a push after its approval could merge without review.'))
    others = [code for code in result.refusals if code not in ('hold', 'last-push-rule')]
    if others:
        listed = '; '.join('%s: %s' % (code, '; '.join(result.refusal_details[code])) for code in others)
        things.append(ThingToCheck('pull-request-not-approved:%d:%s' % (number, others[0]),
                                   "The bot did not approve #%d. %s" % (number, listed)))
    return things


def approve_and_turn_on_auto_merge(github: Any, plan: Any, decision: Any, pull_request: int, head_sha: str, operation: str,
                                   base: str, base_sha: str, bot_branch: str, run_url: str = '',
                                   sleep: Callable[[float], None] = time.sleep) -> Dict[str, Any]:
    """Returns the approval.json content. A refusal is reported in it, never raised."""
    result = check_before_approving(github, plan, decision, pull_request, head_sha, operation, base, base_sha, bot_branch,
                                    sleep)
    facts = result.pull_request_facts
    node_id = facts['node_id']
    approval: Dict[str, Any] = {
        'status': 'ok', 'pull_request': pull_request, 'operation': operation, 'head_sha': head_sha,
        'refusals': list(result.refusals), 'auto_merge': 'not-turned-on', 'approved': 'no', 'merged_directly': False,
        'merge_refused_message': '', 'things_to_check': [],
    }

    if result.refusals:
        keeps_auto_merge_on = all(code in REFUSALS_THAT_KEEP_AUTO_MERGE_ON for code in result.refusals)
        if facts['auto_merge_is_on'] and not keeps_auto_merge_on:
            turn_off_auto_merge(github, node_id)
        approval['things_to_check'] = [thing.to_json() for thing in _refusal_things_to_check(pull_request, result)]
        return approval

    turned_off_for_older_head = False
    if facts['auto_merge_is_on'] and not facts['auto_merge_armed_for_this_head']:
        turn_off_auto_merge(github, node_id)
        turned_off_for_older_head = True
    armed_for_this_head = facts['auto_merge_armed_for_this_head']

    merge_directly = facts['merge_state_status'] in MERGEABLE_NOW_STATES
    if merge_directly:
        approval['auto_merge'] = 'not-turned-on'
    elif armed_for_this_head:
        approval['auto_merge'] = 'already-on'
    else:
        outcome = turn_on_auto_merge(github, node_id, head_sha)
        if outcome == 'mergeable-now':
            merge_directly = True
        else:
            approval['auto_merge'] = 'turned-on-again' if turned_off_for_older_head else 'turned-on'

    if facts['approved_at_head']:
        approval['approved'] = 'existing'
    else:
        approve(github, pull_request, head_sha, run_url, base, base_sha)
        approval['approved'] = 'new'

    if merge_directly:
        merged, message = merge_now(github, pull_request, head_sha)
        approval['merged_directly'] = merged
        if merged:
            approval['auto_merge'] = 'merged-directly'
        else:
            approval['merge_refused_message'] = message
            approval['things_to_check'] = [ThingToCheck(
                'pull-request-ready-to-merge:%d' % pull_request,
                "#%d is approved and green, but GitHub refused GitHub Actions' merge: `%s`. A maintainer merges it."
                % (pull_request, message)).to_json()]
    return approval
