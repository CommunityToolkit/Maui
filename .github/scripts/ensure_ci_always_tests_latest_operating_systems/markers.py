"""The only home of the strings that name the workflow, its branch markers, labels and secret."""
from __future__ import annotations

import json
import re
from typing import Any, Dict, List, Optional

WORKFLOW_FILE = 'ensure-ci-always-tests-latest-operating-systems.yml'
MARKER_PREFIX = 'ensure-ci-always-tests-latest-operating-systems'
COMMIT_TRAILER = 'Generated-by: ensure-ci-always-tests-latest-operating-systems.yml'
PULL_REQUEST_MARKER = '<!-- ensure-ci-always-tests-latest-operating-systems:pr -->'
ISSUE_MARKER = '<!-- ensure-ci-always-tests-latest-operating-systems:issue -->'
CLOSED_BY_BOT_MARKER = '<!-- ensure-ci-always-tests-latest-operating-systems:closed-by-bot -->'
TOKEN_SECRET_NAME = 'ENSURE_CI_ALWAYS_TESTS_LATEST_OPERATING_SYSTEMS_TOKEN'
TOKEN_ENVIRONMENT = 'ensure-ci-always-tests-latest-operating-systems'
ISSUE_TITLE = 'Ensure CI always tests the latest operating systems: a person needs to act'
# U+267B is the recycling symbol of the repo's housekeeping label; the escape keeps this source ASCII.
LABELS = ('requirements updated', '\u267b housekeeping')
PIN_FILES = (
    '.github/workflows/shared.env',
    '.github/workflows/device-tests.yml',
    '.github/workflows/dotnet-build.yml',
)
# The issue job finds the log of the job with this name; test_workflow_file checks the workflow uses the same text.
PULL_REQUEST_JOB_NAME = 'Open or update the bump pull request, then turn on auto-merge and approve it'

_ITEM_KEY = re.compile(r'[a-z0-9][a-z0-9._:+_-]*', re.ASCII)
_ITEM_MARKER_IN_BODY = re.compile(re.escape('<!-- ' + MARKER_PREFIX + ':item=') + r'(\S+?) -->')
_CHANGES_MARKER_IN_BODY = re.compile(re.escape('<!-- ' + MARKER_PREFIX + ':changes=') + r'(.*?) -->', re.DOTALL)
_CHANGE_LIST_FIELDS = ('family', 'kind', 'file', 'pin', 'old', 'new', 'inserted_lines')


def is_valid_item_key(key: str) -> bool:
    return isinstance(key, str) and _ITEM_KEY.fullmatch(key) is not None and '--' not in key


def item_marker(key: str) -> str:
    if not is_valid_item_key(key):
        raise ValueError('not a valid item key: %r' % (key,))
    return '<!-- %s:item=%s -->' % (MARKER_PREFIX, key)


def read_item_keys(body: Optional[str]) -> List[str]:
    if not body:
        return []
    return [key for key in _ITEM_MARKER_IN_BODY.findall(body) if is_valid_item_key(key)]


def _change_as_dict(change: Any) -> Dict[str, Any]:
    source = change if isinstance(change, dict) else {name: getattr(change, name) for name in _CHANGE_LIST_FIELDS}
    entry: Dict[str, Any] = {}
    for name in _CHANGE_LIST_FIELDS:
        value = source.get(name)
        if name == 'inserted_lines':
            lines = [] if value is None else list(value)
            if not all(isinstance(line, str) for line in lines):
                raise ValueError('inserted_lines must be strings')
            entry[name] = lines
        else:
            value = '' if value is None else value
            if not isinstance(value, str):
                raise ValueError('%s must be a string, got %r' % (name, value))
            entry[name] = value
    return entry


def change_list_marker(changes: Any) -> str:
    """Hyphens and angle brackets are escaped so no value can end the HTML comment early."""
    text = json.dumps([_change_as_dict(change) for change in changes],
                      sort_keys=True, separators=(',', ':'), ensure_ascii=True)
    text = text.replace('-', '\\u002d').replace('>', '\\u003e')
    return '<!-- %s:changes=%s -->' % (MARKER_PREFIX, text)


def read_change_list(body: Optional[str]) -> Optional[List[Dict[str, Any]]]:
    """None when the body has no change list or it doesn't parse: the pull request then counts as carrying no changes."""
    if not body:
        return None
    match = _CHANGES_MARKER_IN_BODY.search(body)
    if match is None:
        return None
    try:
        parsed = json.loads(match.group(1))
    except ValueError:
        return None
    if not isinstance(parsed, list) or not all(isinstance(entry, dict) for entry in parsed):
        return None
    return parsed


def has_marker(body: Optional[str], marker: str) -> bool:
    return bool(body) and marker in body
