"""Writes the words of the bump pull request: its title, commit message and body, and the run summary of a night's plan."""
from __future__ import annotations

import re
from typing import Any, List, Optional

from . import markers
from .android_levels import SIXTEEN_KB_TARGET, SYSTEM_IMAGES_URL
from .markdown_text import markdown_code

MAX_BODY = 60000
MAX_TITLE = 120
TITLE_START = 'Update the device tests to the latest operating systems'
SAMPLE_XCODE_KEY = 'CommunityToolkitSample_Xcode_Version'
LIBRARY_XCODE_KEY = 'CommunityToolkitLibrary_Xcode_Version'
_CUT_NOTE = '\n\n_The rest of this text was cut to fit GitHub\'s size limit; the run summary has all of it._\n'
_BACKTICK_RUN = re.compile(r'`+')


def _file_name_of(path: str) -> str:
    return path.rsplit('/', 1)[-1]


def _android_pin_parts(pin: str) -> Optional[List[str]]:
    parts = pin.split(':')
    if pin.startswith('android-leg:') and len(parts) == 3:
        return parts[1:]
    return None


def describe_pin(change: Any) -> str:
    pin = change.pin
    if pin == 'env:' + SAMPLE_XCODE_KEY:
        return 'Sample Xcode'
    if pin == 'env:' + LIBRARY_XCODE_KEY:
        return 'Library Xcode'
    android = _android_pin_parts(pin)
    if android is not None:
        target, major = android
        leg = {'google_apis': '4 KB leg', SIXTEEN_KB_TARGET: '16 KB leg'}.get(target, 'default image leg')
        return 'Android API %s, %s' % (major, leg)
    if pin.startswith('ios-leg:'):
        return 'iOS %s leg' % pin.split(':', 1)[1]
    if pin.startswith('apple-label:'):
        return 'Apple job %s' % pin.rsplit(':', 1)[-1]
    return pin


def _title_part(change: Any) -> str:
    if change.kind in ('replace-api-level', 'insert-android-leg'):
        android = _android_pin_parts(change.pin)
        page_size = ' (16 KB)' if android is not None and android[0] == SIXTEEN_KB_TARGET else ''
        return 'Android API %s%s' % (change.new, page_size)
    if change.kind == 'set-xcode-version':
        return 'Xcode %s' % change.new
    if change.kind == 'insert-ios-leg':
        return 'iOS %s leg' % change.new
    return '%s runners' % change.new


def render_title(plan: Any) -> str:
    parts: List[str] = []
    for change in plan.safe_changes:
        part = _title_part(change)
        if part not in parts:
            parts.append(part)
    if not parts:
        return TITLE_START
    title = '%s: %s' % (TITLE_START, ', '.join(parts))
    return title if len(title) <= MAX_TITLE else title[:MAX_TITLE - 3].rstrip() + '...'


def _commit_line(change: Any) -> str:
    where = '%s (%s)' % (describe_pin(change), _file_name_of(change.file))
    if change.old is None:
        return '- %s: added %s' % (where, change.new)
    return '- %s: %s -> %s' % (where, change.old, change.new)


def render_commit_message(plan: Any) -> str:
    lines = [TITLE_START, '']
    lines.extend(_commit_line(change) for change in plan.safe_changes)
    lines.extend(['', markers.COMMIT_TRAILER])
    return '\n'.join(lines)


def _repository_url_of(run_url: str) -> Optional[str]:
    marker = '/actions/runs/'
    return run_url.split(marker, 1)[0] if marker in run_url else None


def _workflow_link(plan: Any) -> str:
    repository_url = _repository_url_of(plan.run_url)
    if repository_url is None:
        return markers.WORKFLOW_FILE
    return '[%s](%s/blob/%s/.github/workflows/%s)' % (markers.WORKFLOW_FILE, repository_url, plan.base, markers.WORKFLOW_FILE)


_WORKLOAD_SET_AND_IMAGE_SOURCE = re.compile(r'workload-sets:([^+]+)\+macos-runner-image:([^@]+)@(.+)')


def _source_cell(change: Any) -> str:
    source = change.source
    if source == 'android-system-images':
        return '[%s](%s)' % (_file_name_of(SYSTEM_IMAGES_URL), SYSTEM_IMAGES_URL)
    workload_set_and_image = _WORKLOAD_SET_AND_IMAGE_SOURCE.fullmatch(source)
    if workload_set_and_image is not None:
        set_version, label, image_version = workload_set_and_image.groups()
        return 'workload set %s; Xcode %s GA on %s %s' % (
            markdown_code(set_version, in_table=True), change.new, markdown_code(label, in_table=True),
            markdown_code(image_version, in_table=True))
    if source.startswith('workload-sets+macos-runner-image:'):
        return 'the .NET workload set and the %s runner image' % markdown_code(source.split(':', 1)[1], in_table=True)
    if source.startswith('carried-forward'):
        return 'carried forward from the open pull request'
    return markdown_code(source, in_table=True)


def _change_table(plan: Any) -> List[str]:
    rows = ['| Pin | File | Was | Now | Source |', '|---|---|---|---|---|']
    for change in plan.safe_changes:
        was = 'new' if change.old is None else markdown_code(change.old, in_table=True)
        rows.append('| %s | %s | %s | %s | %s |' % (
            markdown_code(describe_pin(change), in_table=True), markdown_code(_file_name_of(change.file), in_table=True),
            was, markdown_code(change.new, in_table=True), _source_cell(change)))
    return rows


def _cut_to_fit(text: str) -> str:
    if len(text) <= MAX_BODY:
        return text
    room = MAX_BODY - len(_CUT_NOTE)
    cut_at = text.rfind('\n', 0, room)
    return text[:cut_at if cut_at > 0 else room] + _CUT_NOTE


def render_pull_request_body(plan: Any) -> str:
    run_link = '[run](%s)' % plan.run_url if plan.run_url else 'run'
    lines = [
        markers.PULL_REQUEST_MARKER,
        markers.change_list_marker(plan.safe_changes),
        '> Don\'t push to this branch or click "Update branch": the bot rebuilds it on the newest `%s` every night, '
        'and stops updating it when it finds a commit it didn\'t make.' % plan.base,
        '',
        '### Description of Change ###',
        '',
        'The nightly %s found newer OS versions (%s). CI on this PR shows whether they work. GitHub Actions approves it '
        'after checking that it holds exactly these changes, and it merges itself when the required checks pass.'
        % (_workflow_link(plan), run_link),
        '',
    ]
    lines.extend(_change_table(plan) if plan.safe_changes else ['No changes tonight.'])
    lines.extend([
        '',
        '### Linked Issues ###',
        '',
        'None: an automated bump.',
        '',
        '### Additional information ###',
        '',
    ])
    lines.extend('- %s' % note for note in plan.notes)
    lines.extend([
        "- Pushed with a maintainer's token, so GitHub shows that maintainer as the author; no person wrote it.",
        "- Like Dependabot's PRs, this merge starts no Build run on `%s` (GitHub starts no workflows for a merge that "
        "GitHub Actions turned on). The next push to `%s` builds it." % (plan.base, plan.base),
        '- When nothing changed since the last run, the bot keeps the existing commit and checks it the same way.',
        '',
    ])
    return _cut_to_fit('\n'.join(lines))


def _fence_around(text: str) -> str:
    longest = max((len(run) for run in _BACKTICK_RUN.findall(text)), default=0)
    return '`' * max(3, longest + 1)


def _collapsible(summary: str, language: str, text: str) -> List[str]:
    fence = _fence_around(text)
    return ['<details><summary>%s</summary>' % summary, '', fence + language, text.rstrip('\n'), fence, '', '</details>', '']


def render_run_summary(plan: Any, changes_patch: Optional[str] = None, pull_request_body: Optional[str] = None) -> str:
    lines = ['## Ensure CI Always Tests Latest Operating Systems', '',
             'Pull request action: `%s`%s. Base `%s` at `%s`.' % (
                 plan.pull_request_action,
                 ' (open pull request #%d)' % plan.open_pull_request if plan.open_pull_request else '',
                 plan.base, plan.base_sha[:7]),
             '', '### Families', '', '| Family | State | Reason |', '|---|---|---|']
    for name, state in plan.families.items():
        reason = state.get('reason', '')
        lines.append('| %s | %s | %s |' % (name, state.get('state', ''), markdown_code(reason, in_table=True) if reason else ''))
    lines.extend(['', '### Changes for the pull request', ''])
    lines.extend(_change_table(plan) if plan.safe_changes else ['None.'])
    lines.extend(['', '### Changes a person must make', ''])
    lines.extend(['- `%s`: %s' % (change.key, markdown_code(change.title)) for change in plan.changes_for_a_person]
                 or ['None.'])
    lines.extend(['', '### Things to check', ''])
    lines.extend(['- `%s`: %s' % (thing.key, thing.text) for thing in plan.things_to_check] or ['None.'])
    lines.extend(['', '### Problems reading sources', ''])
    lines.extend(['- `%s`: %s' % (problem.key, markdown_code(problem.message)) for problem in plan.source_problems]
                 or ['None.'])
    lines.extend(['', '### Notes', ''])
    lines.extend(['- %s' % note for note in plan.notes] or ['None.'])
    lines.append('')
    if changes_patch:
        lines.extend(_collapsible('changes.patch', 'diff', changes_patch))
    if pull_request_body and plan.pull_request_action == 'open-or-update':
        lines.extend(_collapsible('Pull request body', 'markdown', pull_request_body))
    return '\n'.join(lines)
