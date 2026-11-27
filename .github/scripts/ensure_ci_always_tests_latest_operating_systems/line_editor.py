"""Applies safe changes to the pin files by editing single lines, and renders the result as a patch.

Every byte a change doesn't touch stays as it was: line endings, the final newline and non-ASCII comments. A change whose
line doesn't hold the value the planner saw is a bug in the plan, so it raises ValueError instead of editing something else.
"""
from __future__ import annotations

import difflib
import hashlib
import re
from typing import Dict, Iterable, List, Union

from .data_types import SafeChange
from .pinned_versions import DEFAULT_ANDROID_TARGET, XCODE_VERSION_KEYS
from .version_numbers import AndroidLevel, parse_android_level

_API_LEVEL_IN_LINE = re.compile(r"api-level: '(\d+(?:\.\d+)?)'", re.ASCII)
_NO_NEWLINE_NOTE = '\\ No newline at end of file\n'


def _split_keeping_endings(text: str) -> List[str]:
    """Splits on the line feed only, like the reader, so line numbers agree."""
    parts = text.split('\n')
    lines = [part + '\n' for part in parts[:-1]]
    if parts[-1] != '':
        lines.append(parts[-1])
    return lines


def _without_ending(line: str) -> str:
    return line.rstrip('\n').rstrip('\r') if line.endswith('\n') else line


def _ending_of(line: str) -> str:
    if line.endswith('\r\n'):
        return '\r\n'
    return '\n' if line.endswith('\n') else ''


def _line_at(lines: List[str], change: SafeChange) -> str:
    if change.line < 1 or change.line > len(lines):
        raise ValueError('%s: line %d is outside the file (%d lines)' % (change.file, change.line, len(lines)))
    return lines[change.line - 1]


def _replace_xcode_version(lines: List[str], change: SafeChange) -> None:
    line = _line_at(lines, change)
    key = change.pin.split(':', 1)[1] if change.pin.startswith('env:') else ''
    if key not in XCODE_VERSION_KEYS:
        raise ValueError('%s line %d: %r is not one of the Xcode version keys' % (change.file, change.line, key))
    expected = '%s=%s' % (key, change.old)
    if _without_ending(line) != expected:
        raise ValueError('%s line %d: expected %r but the line is %r' % (change.file, change.line, expected,
                                                                      _without_ending(line)))
    lines[change.line - 1] = '%s=%s%s' % (key, change.new, _ending_of(line))


def _replace_api_level(lines: List[str], change: SafeChange) -> None:
    line = _line_at(lines, change)
    old_text = "api-level: '%s'" % change.old
    if line.count(old_text) != 1:
        raise ValueError('%s line %d: expected %s exactly once but the line is %r'
                         % (change.file, change.line, old_text, _without_ending(line)))
    lines[change.line - 1] = line.replace(old_text, "api-level: '%s'" % change.new, 1)


def _replace_runner_label(lines: List[str], change: SafeChange) -> None:
    line = _line_at(lines, change)
    label = re.compile(r'(?<![\w.-])' + re.escape(change.old or '') + r'(?![\w.-])', re.ASCII)
    occurrences = label.findall(line)
    if len(occurrences) != 1:
        raise ValueError('%s line %d: expected the label %s once but found it %d times in %r'
                         % (change.file, change.line, change.old, len(occurrences), _without_ending(line)))
    lines[change.line - 1] = label.sub(change.new or '', line, count=1)


def _insertion_order_key(change: SafeChange) -> AndroidLevel:
    """Inserts after one line go in ascending level order; applying the highest first leaves the lowest nearest the line."""
    for inserted in change.inserted_lines:
        match = _API_LEVEL_IN_LINE.search(inserted)
        if match is not None:
            return parse_android_level(match.group(1))
    return AndroidLevel(0, 0, '0')


def _insert_after(lines: List[str], change: SafeChange) -> None:
    anchor = _line_at(lines, change)
    ending = _ending_of(anchor) or '\n'
    if not _ending_of(anchor):
        lines[change.line - 1] = anchor + ending
    lines[change.line:change.line] = [text + ending for text in change.inserted_lines]


_REPLACERS = {
    'set-xcode-version': _replace_xcode_version,
    'replace-api-level': _replace_api_level,
    'replace-runner-label': _replace_runner_label,
}
_INSERTERS = ('insert-android-leg', 'insert-ios-leg')


def apply_changes(text: str, changes: Iterable[SafeChange]) -> str:
    changes = list(changes)
    lines = _split_keeping_endings(text)

    replaced_lines: Dict[int, SafeChange] = {}
    for change in changes:
        if change.kind in _REPLACERS:
            if change.line in replaced_lines:
                raise ValueError('%s line %d is replaced by two changes (%s and %s)'
                                 % (change.file, change.line, replaced_lines[change.line].pin, change.pin))
            replaced_lines[change.line] = change
        elif change.kind not in _INSERTERS:
            raise ValueError('unknown change kind %r' % change.kind)
    for change in changes:
        if change.kind in _REPLACERS:
            _REPLACERS[change.kind](lines, change)

    inserts = [change for change in changes if change.kind in _INSERTERS]
    inserts.sort(key=lambda change: (change.line, _insertion_order_key(change)), reverse=True)
    for change in inserts:
        _insert_after(lines, change)
    return ''.join(lines)


def apply_changes_to_texts(texts: Dict[str, str], changes: Iterable[SafeChange]) -> Dict[str, str]:
    changes = list(changes)
    for change in changes:
        if change.file not in texts:
            raise ValueError('no text was given for %s' % change.file)
    return {path: apply_changes(text, [change for change in changes if change.file == path])
            for path, text in texts.items()}


def _level_text(level: Union[AndroidLevel, str]) -> str:
    return level.text if isinstance(level, AndroidLevel) else level


def android_leg_lines(dash_prefix: str, level: Union[AndroidLevel, str], target: str) -> List[str]:
    lines = ["%sapi-level: '%s'" % (dash_prefix, _level_text(level))]
    if target != DEFAULT_ANDROID_TARGET:
        lines.append('%starget: %s' % (' ' * len(dash_prefix), target))
    return lines


def ios_leg_lines(dash_prefix: str, ios_version: str, runner: str, xcode: str) -> List[str]:
    padding = ' ' * len(dash_prefix)
    return ["%sios-version: '%s'" % (dash_prefix, ios_version),
            '%srunner: %s' % (padding, runner),
            "%sxcode: '%s'" % (padding, xcode)]


def git_blob_id(text: str) -> str:
    data = text.encode('utf-8')
    return hashlib.sha1(b'blob %d\0' % len(data) + data).hexdigest()


def unified_diff(path: str, before: str, after: str) -> str:
    """A patch that `git apply` accepts; the blob ids on the index line let `git apply -3` fall back to a three-way merge."""
    if before == after:
        return ''
    body = []
    for line in difflib.unified_diff(_split_keeping_endings(before), _split_keeping_endings(after),
                                     fromfile='a/%s' % path, tofile='b/%s' % path, n=3):
        body.append(line if line.endswith('\n') else line + '\n' + _NO_NEWLINE_NOTE)
    header = 'diff --git a/%s b/%s\nindex %s..%s 100644\n' % (path, path, git_blob_id(before), git_blob_id(after))
    return header + ''.join(body)
