"""Reads the OS and Xcode versions that the three pin files hold, line by line, without a YAML library.

The reader fails loudly (PinnedVersionsFormatError) instead of guessing: a layout it doesn't understand is a source problem a person
fixes, never an input for an edit. Line numbers are 1-based everywhere, as in the editor and the validator.
"""
from __future__ import annotations

import os
import re
from dataclasses import dataclass, field
from typing import Dict, List, NamedTuple, Optional, Tuple

from .data_types import AndroidLeg, AppleLabel, EnvValue, IosLeg, LabelOccurrence, PinnedVersions
from .errors import PinnedVersionsFormatError
from .markers import PIN_FILES
from .version_numbers import AndroidLevel, parse_android_level

SHARED_ENV_FILE = PIN_FILES[0]
DEVICE_TESTS_FILE = PIN_FILES[1]
DOTNET_BUILD_FILE = PIN_FILES[2]

ANDROID_RUNTIME_JOB = 'device_tests_android_runtime'
ANDROID_COVERAGE_JOB = 'device_tests_android'
IOS_RUNTIME_JOB = 'device_tests_ios_runtime'
DEVICE_TESTS_APPLE_JOBS = ('device_tests_ios', 'device_tests_ios_app', 'device_tests_maccatalyst')
DOTNET_BUILD_APPLE_JOBS = ('build_sample', 'build_library')

DEFAULT_ANDROID_TARGET = 'google_apis'
SIXTEEN_KB_TARGET = 'google_apis_ps16k'
XCODE_VERSION_KEYS = ('CommunityToolkitLibrary_Xcode_Version', 'CommunityToolkitSample_Xcode_Version')
REQUIRED_ENV_KEYS = ('TOOLKIT_NET_VERSION', 'LATEST_NET_VERSION') + XCODE_VERSION_KEYS

_JOBS_HEADER = re.compile(r'^jobs:\s*$')
_JOB_HEADER = re.compile(r'^    ([A-Za-z0-9_-]+):\s*$')
_INCLUDE_HEADER = re.compile(r'^\s+include:\s*$')
_DASH_PREFIX = re.compile(r'^(\s*-\s+)')
_KEY_AND_VALUE = re.compile(r'^\s*([A-Za-z0-9_-]+):[ \t]*(.*?)\s*$')
_SINGLE_QUOTED = re.compile(r"^'((?:[^']|'')*)'\s*(?:#.*)?$")
_TRAILING_COMMENT = re.compile(r'\s+#.*$')
_ANDROID_LEVEL_TEXT = re.compile(r'\d+(?:\.\d+)?', re.ASCII)
_COVERAGE_LEVEL = re.compile(r'"api-level"\s*:\s*"(\d+(?:\.\d+)?)"', re.ASCII)
_XCODE_VERSION = re.compile(r'\d+\.\d+', re.ASCII)
_APPLE_LABEL = re.compile(r'(?:xcode|macos)-\d+', re.ASCII)
_RUNS_ON_VALUE = re.compile(r'^\s*runs-on:[ \t]*(.+?)\s*$')
_RUNNER_VALUE = re.compile(r'^\s*(?:-\s+)?runner:[ \t]*(.+?)\s*$')
_OS_FLOW_LIST = re.compile(r'^\s*(?:-\s+)?os:[ \t]*\[(.*)\]\s*(?:#.*)?$')
_RUNNER_FAMILIES = (
    ('ubuntu', re.compile(r'ubuntu-\d+\.\d+', re.ASCII)),
    ('windows', re.compile(r'windows-\d{4}', re.ASCII)),
    ('macos', re.compile(r'macos-\d+', re.ASCII)),
    ('xcode', re.compile(r'xcode-\d+', re.ASCII)),
)
_UNSUPPORTED_SCALAR_START = '"[{|>&*!%@`'


@dataclass
class MatrixEntry:
    """One '-' item of a matrix include list; end is its last content line, before the next entry's leading comments."""

    start: int
    end: int
    leading_comment_start: int
    values: Dict[str, str] = field(default_factory=dict)
    value_lines: Dict[str, int] = field(default_factory=dict)
    quoted: Dict[str, bool] = field(default_factory=dict)


class JobBlock(NamedTuple):
    first_line: int
    last_line: int


class ParsedScalar(NamedTuple):
    value: str
    was_single_quoted: bool


class MatrixInclude(NamedTuple):
    include_line: int
    dash_prefix: str
    entries: List[MatrixEntry]


class AndroidLegsRead(NamedTuple):
    legs: List[AndroidLeg]
    include_line: int
    dash_prefix: str


class IosLegsRead(NamedTuple):
    legs: List[IosLeg]
    include_line: int
    dash_prefix: str


def split_into_lines(text: str) -> List[str]:
    """Lines without their endings; splits on the line feed only, so numbering matches the editor's."""
    lines = [line[:-1] if line.endswith('\r') else line for line in text.split('\n')]
    if lines and lines[-1] == '':
        lines.pop()
    return lines


def _indent_of(line: str) -> int:
    return len(line) - len(line.lstrip(' '))


def _is_blank(line: str) -> bool:
    return line.strip() == ''


def _is_comment(line: str) -> bool:
    return line.lstrip().startswith('#')


def find_job_block(lines: List[str], job_id: str) -> JobBlock:
    """The block ends before blank lines and comments that belong to the next job."""
    jobs_index = None
    for index, line in enumerate(lines):
        if _JOBS_HEADER.match(line):
            jobs_index = index
            break
    if jobs_index is None:
        raise PinnedVersionsFormatError("the workflow file has no 'jobs:' line at column 0")
    header_index = None
    section_end = len(lines)
    for index in range(jobs_index + 1, len(lines)):
        line = lines[index]
        if not _is_blank(line) and not _is_comment(line) and _indent_of(line) == 0:
            section_end = index
            break
        match = _JOB_HEADER.match(line)
        if match is not None and match.group(1) == job_id:
            header_index = index
    if header_index is None:
        raise PinnedVersionsFormatError("the workflow file has no job '%s'" % job_id)
    block_end = section_end
    for index in range(header_index + 1, section_end):
        line = lines[index]
        if not _is_blank(line) and not _is_comment(line) and _indent_of(line) <= 4:
            block_end = index
            break
    while block_end - 1 > header_index and (
            _is_blank(lines[block_end - 1]) or (_is_comment(lines[block_end - 1]) and _indent_of(lines[block_end - 1]) <= 4)):
        block_end -= 1
    return JobBlock(header_index + 1, block_end)


def _parse_scalar(raw: str, line_number: int, where: str) -> ParsedScalar:
    raw = raw.strip()
    if raw == '':
        raise PinnedVersionsFormatError('%s line %d: the value is empty or a nested block' % (where, line_number))
    if raw.startswith("'"):
        match = _SINGLE_QUOTED.match(raw)
        if match is None:
            raise PinnedVersionsFormatError('%s line %d: the single-quoted value is malformed' % (where, line_number))
        return ParsedScalar(match.group(1).replace("''", "'"), True)
    if raw[0] in _UNSUPPORTED_SCALAR_START:
        raise PinnedVersionsFormatError(
            '%s line %d: only plain and single-quoted values are supported, found %s' % (where, line_number, raw))
    return ParsedScalar(_TRAILING_COMMENT.sub('', raw), False)


def _parse_entry_line(entry: MatrixEntry, line: str, line_number: int, where: str) -> None:
    text = _DASH_PREFIX.sub('', line, count=1) if line.lstrip().startswith('-') else line
    match = _KEY_AND_VALUE.match(text)
    if match is None:
        raise PinnedVersionsFormatError('%s line %d: expected "key: value" but found %s' % (where, line_number, line.strip()))
    key = match.group(1)
    if key in entry.values:
        raise PinnedVersionsFormatError('%s line %d: the key %s appears twice in one entry' % (where, line_number, key))
    value, quoted = _parse_scalar(match.group(2), line_number, where)
    entry.values[key] = value
    entry.value_lines[key] = line_number
    entry.quoted[key] = quoted


def read_matrix_include(lines: List[str], block: Tuple[int, int], where: str = 'the matrix') -> MatrixInclude:
    first, last = block
    include_lines = [number for number in range(first, last + 1) if _INCLUDE_HEADER.match(lines[number - 1])]
    if len(include_lines) != 1:
        raise PinnedVersionsFormatError(
            '%s: expected exactly one "include:" line with a list below it, found %d' % (where, len(include_lines)))
    include_line = include_lines[0]

    next_line_index = include_line
    while next_line_index < last and (_is_blank(lines[next_line_index]) or _is_comment(lines[next_line_index])):
        next_line_index += 1
    if next_line_index >= last or not lines[next_line_index].lstrip().startswith('-'):
        raise PinnedVersionsFormatError('%s: the include list has no entries' % where)
    first_entry_line = lines[next_line_index]
    dash_column = _indent_of(first_entry_line)
    prefix_match = _DASH_PREFIX.match(first_entry_line)
    if prefix_match is None:
        raise PinnedVersionsFormatError('%s line %d: the entry must start with "- "' % (where, next_line_index + 1))
    dash_prefix = prefix_match.group(1)

    entries: List[MatrixEntry] = []
    current: Optional[MatrixEntry] = None
    pending_comment_start: Optional[int] = None
    last_content_line = 0
    while next_line_index < last:
        line = lines[next_line_index]
        number = next_line_index + 1
        next_line_index += 1
        if _is_blank(line):
            continue
        indent = _indent_of(line)
        if _is_comment(line):
            if indent < dash_column:
                break
            if indent == dash_column or pending_comment_start is not None:
                if pending_comment_start is None:
                    pending_comment_start = number
            else:
                last_content_line = number
            continue
        if indent < dash_column:
            break
        if indent == dash_column:
            if not line.lstrip().startswith('-'):
                break
            if current is not None:
                current.end = last_content_line
            current = MatrixEntry(start=number, end=number,
                                  leading_comment_start=pending_comment_start or number)
            entries.append(current)
            pending_comment_start = None
        else:
            pending_comment_start = None
        if current is None:
            raise PinnedVersionsFormatError('%s line %d: unexpected line before the first entry' % (where, number))
        _parse_entry_line(current, line, number, where)
        last_content_line = number
    if current is not None:
        current.end = last_content_line
    return MatrixInclude(include_line, dash_prefix, entries)


def _android_level_of(entry: MatrixEntry, where: str) -> AndroidLevel:
    if 'api-level' not in entry.values:
        raise PinnedVersionsFormatError('%s line %d: the entry has no api-level' % (where, entry.start))
    line = entry.value_lines['api-level']
    if not entry.quoted['api-level']:
        raise PinnedVersionsFormatError(
            "%s line %d: api-level must be single-quoted, as in api-level: '37.0'" % (where, line))
    text = entry.values['api-level']
    if _ANDROID_LEVEL_TEXT.fullmatch(text) is None:
        raise PinnedVersionsFormatError('%s line %d: %r is not an Android API level' % (where, line, text))
    return parse_android_level(text)


def read_android_legs(device_tests_text: str) -> AndroidLegsRead:
    where = '%s job %s' % (DEVICE_TESTS_FILE, ANDROID_RUNTIME_JOB)
    lines = split_into_lines(device_tests_text)
    include_line, dash_prefix, entries = read_matrix_include(lines, find_job_block(lines, ANDROID_RUNTIME_JOB), where)
    legs = []
    for entry in entries:
        legs.append(AndroidLeg(level=_android_level_of(entry, where),
                               target=entry.values.get('target', DEFAULT_ANDROID_TARGET),
                               api_level_line=entry.value_lines['api-level'],
                               entry_start=entry.start, entry_end=entry.end))
    if not legs:
        raise PinnedVersionsFormatError('%s: the include list has no entries' % where)
    _reject_duplicate_legs(legs, where)
    return AndroidLegsRead(legs, include_line, dash_prefix)


def _reject_duplicate_legs(legs: List[AndroidLeg], where: str) -> None:
    seen_levels: Dict[Tuple[int, int], int] = {}
    seen_majors: Dict[Tuple[int, str], int] = {}
    for leg in legs:
        key = (leg.level.major, leg.level.minor)
        if key in seen_levels:
            raise PinnedVersionsFormatError(
                '%s line %d: API level %s appears twice (also on line %d)' % (where, leg.api_level_line, leg.level.text,
                                                                              seen_levels[key]))
        seen_levels[key] = leg.api_level_line
        if leg.target in (DEFAULT_ANDROID_TARGET, SIXTEEN_KB_TARGET):
            major_key = (leg.level.major, leg.target)
            if major_key in seen_majors:
                raise PinnedVersionsFormatError(
                    '%s line %d: two legs of Android %d use the target %s (also line %d)'
                    % (where, leg.api_level_line, leg.level.major, leg.target, seen_majors[major_key]))
            seen_majors[major_key] = leg.api_level_line


def read_coverage_levels(device_tests_text: str) -> List[AndroidLevel]:
    """The levels that job device_tests_android lists in its JSON 'include:' line; they count for collisions and are never edited."""
    lines = split_into_lines(device_tests_text)
    first, last = find_job_block(lines, ANDROID_COVERAGE_JOB)
    for number in range(first, last + 1):
        line = lines[number - 1]
        if re.match(r'^\s+include:', line) and not _is_comment(line):
            levels = [parse_android_level(text) for text in _COVERAGE_LEVEL.findall(line)]
            if not levels:
                raise PinnedVersionsFormatError(
                    '%s line %d: the include line of job %s lists no "api-level":"..." values'
                    % (DEVICE_TESTS_FILE, number, ANDROID_COVERAGE_JOB))
            return levels
    raise PinnedVersionsFormatError(
        '%s: job %s has no include line' % (DEVICE_TESTS_FILE, ANDROID_COVERAGE_JOB))


def _read_ios_legs(device_tests_text: str) -> IosLegsRead:
    where = '%s job %s' % (DEVICE_TESTS_FILE, IOS_RUNTIME_JOB)
    lines = split_into_lines(device_tests_text)
    include_line, dash_prefix, entries = read_matrix_include(lines, find_job_block(lines, IOS_RUNTIME_JOB), where)
    legs = []
    for entry in entries:
        for key in ('ios-version', 'runner', 'xcode'):
            if key not in entry.values:
                raise PinnedVersionsFormatError('%s line %d: the entry has no %s' % (where, entry.start, key))
        install_text = entry.values.get('install-runtime', 'false').lower()
        if install_text not in ('true', 'false'):
            raise PinnedVersionsFormatError(
                '%s line %d: install-runtime must be true or false' % (where, entry.value_lines['install-runtime']))
        legs.append(IosLeg(ios_version=entry.values['ios-version'], runner=entry.values['runner'],
                           xcode=entry.values['xcode'], install_runtime=install_text == 'true',
                           entry_start=entry.start, entry_end=entry.end))
    if not legs:
        raise PinnedVersionsFormatError('%s: the include list has no entries' % where)
    return IosLegsRead(legs, include_line, dash_prefix)


def _device_tests_apple_labels(lines: List[str]) -> Tuple[List[LabelOccurrence], List[str]]:
    found = []
    for job_id in DEVICE_TESTS_APPLE_JOBS:
        first, last = find_job_block(lines, job_id)
        matches = []
        for number in range(first, last + 1):
            line = lines[number - 1]
            match = re.match(r'^        runs-on:[ \t]*(\S+?)[ \t]*(?:#.*)?$', line)
            if match is not None:
                matches.append((number, match.group(1)))
        if len(matches) != 1:
            raise PinnedVersionsFormatError(
                '%s job %s: expected one runs-on line, found %d' % (DEVICE_TESTS_FILE, job_id, len(matches)))
        found.append((DEVICE_TESTS_FILE, matches[0][0], job_id, matches[0][1]))
    return [LabelOccurrence(file, number, job_id) for file, number, job_id, _ in found], [label for *_, label in found]


def _dotnet_build_apple_labels(lines: List[str]) -> Tuple[List[LabelOccurrence], List[str]]:
    occurrences = []
    labels = []
    for job_id in DOTNET_BUILD_APPLE_JOBS:
        first, last = find_job_block(lines, job_id)
        matches = []
        for number in range(first, last + 1):
            match = _OS_FLOW_LIST.match(lines[number - 1])
            if match is not None:
                tokens = [token.strip().strip('\'"') for token in match.group(1).split(',')]
                apple = [token for token in tokens if token != '' and not token.startswith('windows-')]
                matches.append((number, apple))
        if len(matches) != 1 or len(matches[0][1]) != 1:
            raise PinnedVersionsFormatError(
                '%s job %s: expected one os list with one non-Windows label' % (DOTNET_BUILD_FILE, job_id))
        occurrences.append(LabelOccurrence(DOTNET_BUILD_FILE, matches[0][0], job_id))
        labels.append(matches[0][1][0])
    return occurrences, labels


def _read_apple_label(device_tests_text: str, dotnet_build_text: str) -> AppleLabel:
    device_occurrences, device_labels = _device_tests_apple_labels(split_into_lines(device_tests_text))
    build_occurrences, build_labels = _dotnet_build_apple_labels(split_into_lines(dotnet_build_text))
    occurrences = device_occurrences + build_occurrences
    labels = device_labels + build_labels
    if len(set(labels)) != 1:
        raise PinnedVersionsFormatError(
            'the Apple jobs must all use one label, found %s' % ', '.join(sorted(set(labels))))
    if _APPLE_LABEL.fullmatch(labels[0]) is None:
        raise PinnedVersionsFormatError('the Apple label %s doesn\'t look like xcode-NN or macos-NN' % labels[0])
    return AppleLabel(label=labels[0], occurrences=occurrences)


def _read_runner_labels(*texts: str) -> Dict[str, List[str]]:
    families: Dict[str, List[str]] = {name: [] for name, _ in _RUNNER_FAMILIES}

    def remember(raw: str) -> None:
        label = raw.strip().strip('\'"')
        for name, pattern in _RUNNER_FAMILIES:
            if pattern.fullmatch(label) is not None and label not in families[name]:
                families[name].append(label)

    for text in texts:
        for line in split_into_lines(text):
            if _is_comment(line):
                continue
            match = _OS_FLOW_LIST.match(line)
            if match is not None:
                for token in match.group(1).split(','):
                    remember(token)
                continue
            match = _RUNS_ON_VALUE.match(line) or _RUNNER_VALUE.match(line)
            if match is not None and '${{' not in match.group(1):
                remember(_TRAILING_COMMENT.sub('', match.group(1)))
    return families


def _read_shared_env(shared_env_text: str) -> Dict[str, EnvValue]:
    values: Dict[str, EnvValue] = {}
    for number, line in enumerate(split_into_lines(shared_env_text), start=1):
        if line.strip() == '' or line.lstrip().startswith('#'):
            continue
        if '=' not in line:
            raise PinnedVersionsFormatError('%s line %d: expected NAME=value' % (SHARED_ENV_FILE, number))
        key, value = line.split('=', 1)
        if key in REQUIRED_ENV_KEYS and key in values:
            raise PinnedVersionsFormatError(
                '%s line %d: %s appears twice (also on line %d)' % (SHARED_ENV_FILE, number, key, values[key].line))
        values[key] = EnvValue(value=value, line=number)
    for key in REQUIRED_ENV_KEYS:
        if key not in values:
            raise PinnedVersionsFormatError('%s has no %s' % (SHARED_ENV_FILE, key))
    for key in XCODE_VERSION_KEYS:
        if _XCODE_VERSION.fullmatch(values[key].value) is None:
            raise PinnedVersionsFormatError(
                '%s line %d: %s must look like 27.2' % (SHARED_ENV_FILE, values[key].line, key))
    return values


def read_pinned_versions_from_texts(device_tests: str, dotnet_build: str, shared_env: str) -> PinnedVersions:
    android_legs, android_include_line, android_dash_prefix = read_android_legs(device_tests)
    ios_legs, ios_include_line, ios_dash_prefix = _read_ios_legs(device_tests)
    return PinnedVersions(
        shared_env=_read_shared_env(shared_env),
        android_legs=android_legs,
        coverage_levels=read_coverage_levels(device_tests),
        ios_legs=ios_legs,
        apple_label=_read_apple_label(device_tests, dotnet_build),
        runner_labels=_read_runner_labels(device_tests, dotnet_build),
        android_include_line=android_include_line,
        ios_include_line=ios_include_line,
        android_dash_prefix=android_dash_prefix,
        ios_dash_prefix=ios_dash_prefix,
    )


def _read_file(repo_root: str, relative_path: str) -> str:
    with open(os.path.join(repo_root, *relative_path.split('/')), encoding='utf-8', newline='') as file:
        return file.read()


def read_pinned_versions(repo_root: str) -> PinnedVersions:
    return read_pinned_versions_from_texts(
        device_tests=_read_file(repo_root, DEVICE_TESTS_FILE),
        dotnet_build=_read_file(repo_root, DOTNET_BUILD_FILE),
        shared_env=_read_file(repo_root, SHARED_ENV_FILE))
