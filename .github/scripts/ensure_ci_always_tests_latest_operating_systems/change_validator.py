"""Decides whether a pull request's edit of a pin file is one the bot is allowed to make on its own.

The bot checks its own plan with this, and the approval step checks the pushed commit with it again, so a hand edit or a bug in the
editor can't get approved. Only safe changes pass; Apple labels, iOS legs and anything else go to a person.
"""
from __future__ import annotations

import difflib
import re
from typing import Dict, List, Optional, Tuple

from .errors import PinnedVersionsFormatError
from .markers import PIN_FILES
from .pinned_versions import (ANDROID_RUNTIME_JOB, DEVICE_TESTS_FILE, MOVABLE_RUNS_ON_LINE, REQUIRED_DEVICE_TESTS_JOB,
                              SHARED_ENV_FILE, SIXTEEN_KB_TARGET, XCODE_VERSION_KEYS, is_a_newer_label_of_the_same_family,
                              parse_ubuntu_or_windows_label, read_android_legs, read_coverage_levels, read_movable_runs_on_lines,
                              split_into_lines)
from .version_numbers import parse_android_level

# A busy night's Android and Xcode edits plus one block per moved runs-on line; every block is still checked on its own.
MAX_CHANGED_BLOCKS = 16

_XCODE_VERSION_LINE = re.compile(r'^(%s)=\d+\.\d+$' % '|'.join(XCODE_VERSION_KEYS), re.ASCII)
_API_LEVEL_LINE = re.compile(r"^(\s+(?:-\s+)?)api-level: '(\d+(?:\.\d+)?)'$", re.ASCII)
_NEW_LEG_LEVEL_LINE = re.compile(r"^(\s+-\s+)api-level: '\d+(?:\.\d+)?'$", re.ASCII)
_NEW_LEG_TARGET_LINE = re.compile(r'^( +)target: %s$' % SIXTEEN_KB_TARGET, re.ASCII)


def _problem(path: str, line: int, reason: str) -> str:
    return '%s line %d: %s' % (path, line, reason)


def _carriage_return_of_each_line(text: str) -> List[bool]:
    raw_lines = text.split('\n')
    if raw_lines and raw_lines[-1] == '':
        raw_lines.pop()
    return [line.endswith('\r') for line in raw_lines]


def _line_ending_problems(path: str, base_text: str, head_text: str,
                          opcodes: List[Tuple[str, int, int, int, int]]) -> List[str]:
    """A file that already mixes endings could otherwise gain or lose a carriage return on one changed line."""
    base_ends = _carriage_return_of_each_line(base_text)
    head_ends = _carriage_return_of_each_line(head_text)
    problems = []
    for _, base_start, base_end, head_start, head_end in opcodes:
        paired = min(base_end - base_start, head_end - head_start)
        inserted = (head_end - head_start) - paired
        if inserted > 0:
            endings_an_insert_may_take = set(base_ends[base_start:base_end]) or {base_ends[max(base_start - 1, 0)]}
            carriage_returns_gained = sum(head_ends[head_start:head_end]) - sum(base_ends[base_start:base_end])
            mismatched = carriage_returns_gained not in {inserted * int(ending) for ending in endings_an_insert_may_take}
        else:
            mismatched = base_ends[base_start:base_start + paired] != head_ends[head_start:head_start + paired]
        if mismatched:
            problems.append(_problem(path, base_start + 1, 'a changed or inserted line has a different line ending than the line it replaces or follows'))
    return problems


def _line_ending_style(text: str) -> str:
    line_feeds = text.count('\n')
    carriage_returns = text.count('\r')
    if carriage_returns == 0:
        return 'lf' if line_feeds else 'none'
    return 'crlf' if text.count('\r\n') == line_feeds == carriage_returns else 'mixed'


def count_changed_blocks(base_text: str, head_text: str) -> int:
    return len(_changed_opcodes(split_into_lines(base_text), split_into_lines(head_text)))


def _changed_opcodes(base_lines: List[str], head_lines: List[str]) -> List[Tuple[str, int, int, int, int]]:
    matcher = difflib.SequenceMatcher(None, base_lines, head_lines, autojunk=False)
    return [opcode for opcode in matcher.get_opcodes() if opcode[0] != 'equal']


def _shared_env_pair_problem(base_line: str, head_line: str) -> Optional[str]:
    base_match = _XCODE_VERSION_LINE.match(base_line)
    head_match = _XCODE_VERSION_LINE.match(head_line)
    if base_match is None or head_match is None:
        return 'only the two Xcode version values (as X.Y) may change in shared.env'
    if base_match.group(1) != head_match.group(1):
        return 'the line changed to another key'
    return None


def _find_runner_label_move_problem(base_line: str, head_line: str) -> Optional[str]:
    base_match = MOVABLE_RUNS_ON_LINE.fullmatch(base_line)
    head_match = MOVABLE_RUNS_ON_LINE.fullmatch(head_line)
    if base_match is None or head_match is None or base_match.group('key') != head_match.group('key') \
            or base_match.group('rest') != head_match.group('rest'):
        return 'a runs-on line may change only its label'
    base_label = base_match.group('label')
    head_label = head_match.group('label')
    parsed_head = parse_ubuntu_or_windows_label(head_label)
    if parsed_head is None:
        return '%s is not an ubuntu-NN.NN or windows-NNNN label' % head_label
    parsed_base = parse_ubuntu_or_windows_label(base_label)
    if parsed_base is None or parsed_base.family != parsed_head.family:
        return 'a runner label may not move to another family (%s to %s)' % (base_label, head_label)
    if not is_a_newer_label_of_the_same_family(base_label, head_label):
        return 'a runner label may only move to a newer one (%s to %s)' % (base_label, head_label)
    return None


class _EditableLinesOfBase:
    def __init__(self, base_text: str) -> None:
        self.movable_runs_on_lines = read_movable_runs_on_lines(base_text)
        legs, include_line, dash_prefix = read_android_legs(base_text)
        self.api_level_lines = {leg.api_level_line: leg for leg in legs}
        self.include_line = include_line
        self.dash_prefix = dash_prefix
        self.insertion_points = {leg.entry_end for leg in legs} | {include_line}

    def replacement_problem(self, base_number: int, base_line: str, head_line: str) -> Optional[str]:
        if base_number in self.movable_runs_on_lines:
            return _find_runner_label_move_problem(base_line, head_line)
        if base_number not in self.api_level_lines:
            return 'only an api-level line of an Android leg in %s, or the runs-on label of a job that %s needs, may change' % (
                ANDROID_RUNTIME_JOB, REQUIRED_DEVICE_TESTS_JOB)
        base_match = _API_LEVEL_LINE.match(base_line)
        head_match = _API_LEVEL_LINE.match(head_line)
        if base_match is None or head_match is None:
            return "the line must stay of the form api-level: '<level>' with the level in single quotes"
        if base_match.group(1) != head_match.group(1):
            return 'the indentation of the api-level line changed'
        base_level = parse_android_level(base_match.group(2))
        head_level = parse_android_level(head_match.group(2))
        if head_level <= base_level:
            return 'Android levels may only rise (%s to %s)' % (base_match.group(2), head_match.group(2))
        if head_level.major != base_level.major:
            return 'an Android level may not move to another major (%s to %s); that drops the leg of Android %d' % (
                base_match.group(2), head_match.group(2), base_level.major)
        return None

    def insertion_problems(self, path: str, after_line: int, inserted: List[str]) -> List[str]:
        if after_line not in self.insertion_points:
            return [_problem(path, after_line + 1, 'a new Android leg may only be inserted right after the last line of an existing leg or after the include: line')]
        problems = []
        index = 0
        while index < len(inserted):
            level_match = _NEW_LEG_LEVEL_LINE.match(inserted[index])
            if level_match is None:
                problems.append(_problem(path, after_line + 1 + index,
                                         "an inserted line must be a new Android leg, api-level: '<level>' after a dash"))
                index += 1
                continue
            if level_match.group(1) != self.dash_prefix:
                problems.append(_problem(path, after_line + 1 + index, 'the dash prefix differs from the list\'s'))
            index += 1
            if index < len(inserted):
                target_match = _NEW_LEG_TARGET_LINE.match(inserted[index])
                if target_match is not None:
                    if len(target_match.group(1)) != len(self.dash_prefix):
                        problems.append(_problem(path, after_line + 1 + index, 'the target line is not aligned with the api-level key'))
                    index += 1
        return problems


def _device_tests_opcode_problems(path: str, base_lines: List[str], head_lines: List[str],
                                  opcodes: List[Tuple[str, int, int, int, int]], base: _EditableLinesOfBase) -> List[str]:
    problems = []
    for tag, base_start, base_end, head_start, head_end in opcodes:
        base_count = base_end - base_start
        head_count = head_end - head_start
        if tag == 'delete' or (tag == 'replace' and head_count < base_count):
            problems.append(_problem(path, base_start + 1, 'a line was deleted'))
            continue
        if tag == 'insert':
            problems.extend(base.insertion_problems(path, base_start, head_lines[head_start:head_end]))
            continue
        problems.extend(_replace_block_problems(path, base_lines, head_lines, base_start, base_end, head_start, head_end, base))
    return problems


def _replace_block_problems(path: str, base_lines: List[str], head_lines: List[str], base_start: int, base_end: int,
                            head_start: int, head_end: int, base: _EditableLinesOfBase) -> List[str]:
    """A replaced level next to an inserted leg is one block for difflib; try each split point of the inserted lines."""
    base_count = base_end - base_start
    inserted_count = (head_end - head_start) - base_count
    failures: List[str] = []
    for paired_before_insert in range(base_count + 1):
        attempt: List[str] = []
        for offset in range(base_count):
            head_offset = offset if offset < paired_before_insert else offset + inserted_count
            reason = base.replacement_problem(base_start + offset + 1, base_lines[base_start + offset],
                                              head_lines[head_start + head_offset])
            if reason is not None:
                attempt.append(_problem(path, base_start + offset + 1, reason))
        if inserted_count > 0:
            inserted_start = head_start + paired_before_insert
            attempt.extend(base.insertion_problems(path, base_start + paired_before_insert,
                                                   head_lines[inserted_start:inserted_start + inserted_count]))
        if not attempt:
            return []
        if not failures:
            failures = attempt
    return failures


def _problems_after_the_edit(path: str, head_text: str) -> List[str]:
    try:
        head_legs, _, _ = read_android_legs(head_text)
        coverage_levels = read_coverage_levels(head_text)
    except PinnedVersionsFormatError as error:
        return [_problem(path, 1, 'the edited file can no longer be read: %s' % error)]
    problems = []
    for leg in head_legs:
        if leg.level in coverage_levels:
            problems.append(_problem(path, leg.api_level_line,
                                     'API %s is also run by the coverage job, so two jobs would share a name' % leg.level.text))
    return problems


def validate_file_change(path: str, base_text: str, head_text: str) -> List[str]:
    if path not in PIN_FILES:
        return ['%s is not one of the files the bot may edit (%s)' % (path, ', '.join(PIN_FILES))]
    if _line_ending_style(base_text) != _line_ending_style(head_text):
        return [_problem(path, 1, 'the line ending style changed')]
    if base_text.endswith('\n') != head_text.endswith('\n'):
        return [_problem(path, 1, 'the final newline was added or removed')]

    base_lines = split_into_lines(base_text)
    head_lines = split_into_lines(head_text)
    opcodes = _changed_opcodes(base_lines, head_lines)
    if len(opcodes) > MAX_CHANGED_BLOCKS:
        return [_problem(path, 1, 'the edit has %d changed blocks, more than the %d the bot makes' % (len(opcodes), MAX_CHANGED_BLOCKS))]

    ending_problems = _line_ending_problems(path, base_text, head_text, opcodes)
    if ending_problems:
        return ending_problems

    if path == DEVICE_TESTS_FILE:
        try:
            base = _EditableLinesOfBase(base_text)
        except PinnedVersionsFormatError as error:
            return [_problem(path, 1, 'the file before the edit can\'t be read: %s' % error)]
        problems = _device_tests_opcode_problems(path, base_lines, head_lines, opcodes, base)
        return problems if problems else _problems_after_the_edit(path, head_text)

    problems = []
    for tag, base_start, base_end, head_start, head_end in opcodes:
        if path == SHARED_ENV_FILE and tag == 'replace' and base_end - base_start == head_end - head_start:
            for offset in range(base_end - base_start):
                reason = _shared_env_pair_problem(base_lines[base_start + offset], head_lines[head_start + offset])
                if reason is not None:
                    problems.append(_problem(path, base_start + offset + 1, reason))
        else:
            reason = 'a person must make this edit' if path != SHARED_ENV_FILE else 'only a one-line replacement of an Xcode version is allowed'
            problems.append(_problem(path, base_start + 1, reason))
    return problems


def validate_changes(base_texts: Dict[str, str], head_texts: Dict[str, str]) -> List[str]:
    problems: List[str] = []
    total_blocks = 0
    for path in sorted(set(base_texts) | set(head_texts)):
        if path not in base_texts or path not in head_texts:
            problems.append('%s was added or removed' % path)
            continue
        problems.extend(validate_file_change(path, base_texts[path], head_texts[path]))
        total_blocks += count_changed_blocks(base_texts[path], head_texts[path])
    if total_blocks > MAX_CHANGED_BLOCKS:
        problems.append('the edits have %d changed blocks in total, more than the %d the bot makes' % (total_blocks, MAX_CHANGED_BLOCKS))
    return problems
