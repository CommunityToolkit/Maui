"""The one issue that tells a person what the bot can't decide alone, kept up to date every night."""
from __future__ import annotations

import re
from dataclasses import dataclass, field
from datetime import datetime
from typing import Any, Dict, List, Mapping, Optional, Set

from .errors import GitHubApiError
from .markdown_text import markdown_code
from .markers import (CLOSED_BY_BOT_MARKER, ISSUE_MARKER, ISSUE_TITLE, LABELS, TOKEN_ENVIRONMENT, TOKEN_SECRET_NAME,
                      WORKFLOW_FILE, has_marker, item_marker, read_item_keys)

try:
    from .pull_request_text import MAX_BODY
except ImportError:  # pull_request_text.py is written by another unit; the design fixes its value
    MAX_BODY = 60000

SECTION_CHANGES = 'Changes a person must make'
SECTION_THINGS_TO_CHECK = 'Things to check'
SECTION_SOURCE_PROBLEMS = 'Problems reading sources'
SECTION_FAILURES = 'Jobs that failed'
BOT_LOGIN = 'github-actions[bot]'
MAX_PUSH_ERROR_LENGTH = 300
PATCH_NOTICE = 'Too long for the issue: download the `plan` artifact of [run %s](%s)'

JOB_NAMES = {
    'run-unit-tests': 'Run the unit tests of the scripts',
    'read-latest-versions': 'Read the newest Android, Xcode and runner versions',
    'inspect-macos-runner-image': 'Inspect the macOS runner image',
    'plan-changes': 'Plan the version changes',
    'open-or-update-pull-request': 'Open or update the bump pull request, then turn on auto-merge and approve it',
}
STEP_NAMES = {
    'decide': 'Check the token and decide what to do with the pull request',
    'apply': 'Apply the planned changes',
    'open_or_update': 'Open or update the pull request',
    'approve': 'Check the pull request, turn on auto-merge and approve it',
}
SOURCE_NAMES = {
    'android-system-images': 'Android system images',
    'workload-sets': 'Workload sets',
    'runner-images-readme': 'Runner images README',
    'runner-rollout-states': 'Runner rollout states',
    'branch-rules': 'Branch rules',
    'pinned-versions': 'Pinned versions',
    'bump-pull-request': 'Bump pull request state',
    'change-validator': 'Change validator',
}
TOKEN_PROBLEMS = {
    'missing': 'is missing',
    'rejected': 'was rejected',
    'missing-scope': 'lacks the `workflow` or `public_repo` scope',
    'no-push-access': 'has no push access',
    'organization-forbids-classic-tokens': "is a classic token, which CommunityToolkit doesn't accept",
}
PATCH_FILE_OF_UNOPENED_BUMP = 'changes.patch'
_UPDATED_BY_LINE = re.compile(r'Updated by \[run [^\]]*\]\([^)]*\) on [^\n]*? UTC\.')
_FAILURES_SECTION = re.compile(r'\n*### ' + re.escape(SECTION_FAILURES) + r' ###\n.*\Z', re.DOTALL)


@dataclass
class IssueItem:
    """For a change a person must make, text is the reason and steps are numbered; elsewhere text is the whole bullet."""

    key: str
    section: str
    text: str
    title: str = ''
    steps: List[str] = field(default_factory=list)
    patch: Optional[str] = None

    def summary(self) -> str:
        words = self.title or self.text
        first_sentence = re.split(r'(?<=[.!?])\s', words.strip(), maxsplit=1)[0]
        return first_sentence.replace('\n', ' ')


@dataclass
class Items:
    changes_for_a_person: List[IssueItem] = field(default_factory=list)
    things_to_check: List[IssueItem] = field(default_factory=list)
    source_problems: List[IssueItem] = field(default_factory=list)
    failures: List[IssueItem] = field(default_factory=list)
    plan_available: bool = True

    def everything(self) -> List[IssueItem]:
        return self.changes_for_a_person + self.things_to_check + self.source_problems + self.failures

    def without_keys(self, dismissed_keys: Set[str]) -> 'Items':
        def keep(items: List[IssueItem]) -> List[IssueItem]:
            return [item for item in items if item.key not in dismissed_keys]
        return Items(keep(self.changes_for_a_person), keep(self.things_to_check), keep(self.source_problems),
                     keep(self.failures), self.plan_available)

    def reports_a_source_problem_or_failure(self) -> bool:
        return bool(self.source_problems or self.failures)


def _text_of(token_status: Mapping[str, Any]) -> str:
    return TOKEN_PROBLEMS.get(str(token_status.get('reason')), 'is unusable')


def _unopened_bump_item(plan: Mapping[str, Any], key_prefix: str, title: str, cause: str,
                        patches: Mapping[str, str]) -> IssueItem:
    safe_changes = plan.get('safe_changes') or []
    rows = ['| File | Change |', '|---|---|']
    for change in safe_changes:
        rows.append('| %s | %s |' % (markdown_code(change.get('file'), in_table=True),
                                    markdown_code(change.get('description'), in_table=True)))
    why = '%s, so these changes didn\'t become a pull request:\n\n%s' % (cause, '\n'.join(rows))
    steps = [
        'Renew the token: a classic personal access token with the scopes `public_repo` and `workflow`. Store it with '
        '`gh secret set %s -R CommunityToolkit/Maui --env %s`, then run the workflow again '
        '(Actions > Ensure CI Always Tests Latest Operating Systems > Run workflow).' % (TOKEN_SECRET_NAME,
                                                                                       TOKEN_ENVIRONMENT),
        'Or apply the patch by hand, open a pull request, and have another maintainer approve it.',
    ]
    return IssueItem(key=key_prefix + ':' + str(plan.get('change_list_key') or ''), section=SECTION_CHANGES,
                     text=why, title=title, steps=steps, patch=patches.get(PATCH_FILE_OF_UNOPENED_BUMP))


def _items_from_plan(plan: Mapping[str, Any], patches: Mapping[str, str]) -> Items:
    items = Items()
    for change in plan.get('changes_for_a_person') or []:
        items.changes_for_a_person.append(IssueItem(
            key=change['key'], section=SECTION_CHANGES, text=change.get('why', ''), title=change.get('title', ''),
            steps=list(change.get('steps') or []), patch=patches.get(change.get('patch_file', ''))))
    for thing in plan.get('things_to_check') or []:
        items.things_to_check.append(IssueItem(key=thing['key'], section=SECTION_THINGS_TO_CHECK, text=thing['text']))
    for problem in plan.get('source_problems') or []:
        source = str(problem.get('source', ''))
        label = SOURCE_NAMES.get(source)
        if label is None and source.startswith('macos-runner-image:'):
            label = 'macOS runner image ' + source.split(':', 1)[1]
        text = '%s: %s. Nothing was changed from this source tonight.' % (
            label or source, markdown_code(problem.get('message')))
        items.source_problems.append(IssueItem(key=problem['key'], section=SECTION_SOURCE_PROBLEMS, text=text))
    return items


def _add_things_to_check(items: Items, report: Optional[Mapping[str, Any]]) -> None:
    already_listed = {item.key for item in items.things_to_check}
    for thing in (report or {}).get('things_to_check') or []:
        if thing['key'] not in already_listed:
            already_listed.add(thing['key'])
            items.things_to_check.append(IssueItem(key=thing['key'], section=SECTION_THINGS_TO_CHECK,
                                                   text=thing['text']))


def _failed_step_key(outcome_name: str) -> str:
    return 'failed-step:' + outcome_name.replace('_', '-')


def collect_items(plan: Optional[Mapping[str, Any]], decision: Optional[Mapping[str, Any]],
                  approval: Optional[Mapping[str, Any]], step_outcomes: Optional[Mapping[str, Any]],
                  job_results: Optional[Mapping[str, Any]], push_error: Optional[str],
                  patches: Optional[Mapping[str, str]] = None) -> Items:
    """patches maps a patch_file of the plan (and 'changes.patch') to its text; the plan folder holds those files."""
    patches = patches or {}
    items = _items_from_plan(plan, patches) if plan else Items(plan_available=False)
    _add_things_to_check(items, decision)
    _add_things_to_check(items, approval)

    safe_changes = (plan or {}).get('safe_changes') or []
    token_status = (decision or {}).get('token') or {}
    if plan and safe_changes and decision and token_status.get('usable') is False:
        cause = 'The token in `%s` %s' % (TOKEN_SECRET_NAME, _text_of(token_status))
        items.changes_for_a_person.append(_unopened_bump_item(
            plan, 'bump-not-opened', "Open the bump the bot couldn't open", cause, patches))

    outcomes = step_outcomes or {}
    if plan and safe_changes and outcomes.get('open_or_update') == 'failure':
        if push_error:
            quoted = markdown_code(push_error)
        else:
            quoted = 'see the job log'
        cause = "create-pull-request couldn't push the branch or open the pull request: %s. " \
                "A token without the `workflow` scope fails this way" % quoted
        items.changes_for_a_person.append(_unopened_bump_item(
            plan, 'bump-not-pushed', "Open the bump the bot couldn't push", cause, patches))

    for job_id, result in (job_results or {}).items():
        outcome = result.get('result') if isinstance(result, Mapping) else result
        if outcome == 'failure':
            items.failures.append(IssueItem(
                key='failed-job:' + job_id, section=SECTION_FAILURES,
                text='The job %s failed (%%(log)s). Nothing was changed.' % markdown_code(JOB_NAMES.get(job_id, job_id))))
    for outcome_name, step_title in STEP_NAMES.items():
        if outcomes.get(outcome_name) == 'failure':
            items.failures.append(IssueItem(
                key=_failed_step_key(outcome_name), section=SECTION_FAILURES,
                text='The step %s of the bump job failed (%%(log)s).' % markdown_code(step_title)))
    return items


def read_push_error_from_job_log(github: Any, run_id: Any, attempt: Any, job_name: str) -> Optional[str]:
    repo = github.repo
    try:
        jobs = github.paginate('/repos/%s/actions/runs/%s/attempts/%s/jobs' % (repo, run_id, attempt))
        job = next((entry for entry in jobs if entry.get('name') == job_name), None)
        if job is None:
            return None
        log = github.get_text_following_redirect('/repos/%s/actions/jobs/%s/logs' % (repo, job['id']))
    except GitHubApiError:
        return None
    inside_create_pull_request_step = False
    for raw_line in log.splitlines():
        line = re.sub(r'^\S+Z ', '', raw_line)
        if line.startswith('##[group]Run '):
            inside_create_pull_request_step = line.startswith('##[group]Run peter-evans/create-pull-request')
            continue
        if not inside_create_pull_request_step:
            continue
        if '##[error]' in line or 'refusing to allow' in line or line.startswith('remote:'):
            cleaned = re.sub(r'\x1b\[[0-9;]*m', '', line.replace('##[error]', '')).strip()
            return cleaned[:MAX_PUSH_ERROR_LENGTH]
    return None


def _run_id_of(run_url: str) -> str:
    return run_url.rstrip('/').rsplit('/', 1)[-1]


def _workflow_url_of(run_url: str) -> str:
    return run_url.split('/actions/runs/')[0] + '/actions/workflows/' + WORKFLOW_FILE


def _timestamp(now: datetime) -> str:
    return '%04d-%02d-%02d %02d:%02d' % (now.year, now.month, now.day, now.hour, now.minute)


def _fence_around(text: str) -> str:
    longest_run = max([len(run) for run in re.findall(r'`+', text)] or [0])
    return '`' * max(3, longest_run + 1)


def _render_change(item: IssueItem, replaced_patch_notice: Optional[str]) -> str:
    lines = [item_marker(item.key), '#### %s ####' % item.title, '', item.text]
    if item.steps:
        lines.append('')
        lines.extend('%d. %s' % (number, step) for number, step in enumerate(item.steps, 1))
    if item.patch is not None:
        lines.append('')
        if replaced_patch_notice is not None:
            lines.append(replaced_patch_notice)
        else:
            fence = _fence_around(item.patch)
            lines.extend(['<details><summary>Patch</summary>', '', fence + 'diff', item.patch.rstrip('\n'), fence,
                          '', '</details>'])
    return '\n'.join(lines)


def _render_bullet(item: IssueItem, run_url: str) -> str:
    text = item.text.replace('%(log)s', '[log](%s)' % run_url)
    return '- %s %s' % (item_marker(item.key), text)


def _render(items: Items, run_url: str, now: datetime, notice_for_keys: Set[str]) -> str:
    run_id = _run_id_of(run_url)
    parts = [
        ISSUE_MARKER,
        'The nightly [%s](%s) found things a person needs to act on. Updated by [run %s](%s) on %s UTC.' % (
            WORKFLOW_FILE, _workflow_url_of(run_url), run_id, run_url, _timestamp(now)),
        '',
        'Closing this issue dismisses the items listed here: the bot won\'t list them again, and opens a new issue '
        'for anything new. Problems reading sources and failed jobs come back while they last.',
    ]
    if items.changes_for_a_person:
        parts.extend(['', '### %s ###' % SECTION_CHANGES])
        for item in items.changes_for_a_person:
            notice = PATCH_NOTICE % (run_id, run_url) if item.key in notice_for_keys else None
            parts.extend(['', _render_change(item, notice)])
    for section, entries in ((SECTION_THINGS_TO_CHECK, items.things_to_check),
                             (SECTION_SOURCE_PROBLEMS, items.source_problems),
                             (SECTION_FAILURES, items.failures)):
        if entries:
            parts.extend(['', '### %s ###' % section, ''])
            parts.extend(_render_bullet(entry, run_url) for entry in entries)
    return '\n'.join(parts) + '\n'


def render_issue_body(items: Items, run_url: str, now: datetime, max_body: Optional[int] = None) -> str:
    limit = MAX_BODY if max_body is None else max_body
    notice_for_keys: Set[str] = set()
    body = _render(items, run_url, now, notice_for_keys)
    patched = sorted((item for item in items.changes_for_a_person if item.patch is not None),
                     key=lambda item: len(item.patch or ''), reverse=True)
    for item in patched:
        if len(body) <= limit:
            break
        notice_for_keys.add(item.key)
        body = _render(items, run_url, now, notice_for_keys)
    if len(body) > limit:
        # Last resort: GitHub rejects a longer body, and a cut-off issue is better than none.
        body = body[:limit - 1].rstrip() + '\n'
    return body


def _resolved_body(run_url: str) -> str:
    return 'Nothing needs a person as of [run %s](%s).\n\n%s\n%s\n' % (
        _run_id_of(run_url), run_url, ISSUE_MARKER, CLOSED_BY_BOT_MARKER)


def _body_after_early_failure(old_body: str, items: Items, run_url: str, now: datetime) -> str:
    """The plan is missing, so the old item blocks stay as they were and only the failures and timestamp change."""
    updated_by = 'Updated by [run %s](%s) on %s UTC.' % (_run_id_of(run_url), run_url, _timestamp(now))
    kept = _FAILURES_SECTION.sub('', old_body).rstrip('\n')
    kept = _UPDATED_BY_LINE.sub(lambda _: updated_by, kept)
    if items.failures:
        kept += '\n\n### %s ###\n\n%s' % (SECTION_FAILURES, '\n'.join(_render_bullet(item, run_url)
                                                                      for item in items.failures))
    return kept + '\n'


def _issues_path(github: Any) -> str:
    return '/repos/%s/issues' % github.repo


def _find_candidates(github: Any) -> List[Dict[str, Any]]:
    entries = github.paginate(_issues_path(github), {
        'state': 'all', 'labels': LABELS[0], 'creator': 'github-actions[bot]', 'per_page': 100})
    return [entry for entry in entries
            if 'pull_request' not in entry and has_marker(entry.get('body'), ISSUE_MARKER)]


def _dismissed_keys(candidates: List[Dict[str, Any]]) -> Set[str]:
    dismissed: Set[str] = set()
    for candidate in candidates:
        body = candidate.get('body')
        if candidate.get('state') != 'closed' or has_marker(body, CLOSED_BY_BOT_MARKER):
            continue
        for key in read_item_keys(body):
            if not key.startswith('source:') and not key.startswith('failed-'):
                dismissed.add(key)
    return dismissed


def _comment_about_new_items(items: List[IssueItem], run_url: str) -> str:
    lines = ['New since the last run ([run %s](%s)):' % (_run_id_of(run_url), run_url), '']
    lines.extend('- ' + item.summary().replace('%(log)s', 'log') for item in items)
    return '\n'.join(lines)


def sync_issue(github: Any, items: Items, run_url: str, now: datetime) -> Dict[str, Any]:
    result: Dict[str, Any] = {'action': 'none', 'number': None, 'commented': False, 'closed': False,
                              'exit_code': 1 if items.reports_a_source_problem_or_failure() else 0}
    candidates = _find_candidates(github)
    current = items.without_keys(_dismissed_keys(candidates))
    open_candidates = sorted((entry for entry in candidates if entry.get('state') == 'open'),
                             key=lambda entry: entry.get('number', 0))
    issues_path = _issues_path(github)

    if not open_candidates:
        if current.everything():
            body = render_issue_body(current, run_url, now)
            _, _, created = github.post(issues_path, {'title': ISSUE_TITLE, 'body': body, 'labels': list(LABELS)})
            result.update(action='created', number=(created or {}).get('number'))
        return result

    issue = open_candidates[-1]
    number = issue['number']
    path = '%s/%s' % (issues_path, number)
    old_body = issue.get('body') or ''
    result['number'] = number

    if not items.plan_available:
        body = _body_after_early_failure(old_body, current, run_url, now)
    elif not current.everything():
        github.patch(path, {'body': _resolved_body(run_url), 'state': 'closed', 'state_reason': 'completed'})
        result.update(action='resolved', closed=True)
        return result
    else:
        body = render_issue_body(current, run_url, now)

    github.patch(path, {'body': body})
    result['action'] = 'updated'
    old_keys = set(read_item_keys(old_body))
    new_items = [item for item in current.everything() if item.key not in old_keys]
    if new_items:
        github.post(path + '/comments', {'body': _comment_about_new_items(new_items, run_url)})
        result['commented'] = True
    return result
