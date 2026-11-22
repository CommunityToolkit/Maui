"""Reads the GitHub runner images README and its rollout states, and checks the repo's runner labels against them."""
from __future__ import annotations

import json
import re
from typing import Any, Dict, Iterable, List, Optional, Tuple

from .data_types import FamilyPlan, PinnedVersions, RolloutState, RunnerImageRow, ThingToCheck
from .errors import NotFoundError, RunnerImagesReadmeError, UnreadableSourceError
from .markdown_text import markdown_code

README_URL = 'https://raw.githubusercontent.com/actions/runner-images/main/README.md'
ROLLOUT_STATE_URL = ('https://gist.githubusercontent.com/hosted-runners-images-bot/'
                     '79267492faab096d04cdd25ce7014cec/raw/{key}.json')

RUNNER_LABELS_FAMILY = 'runner-labels'
_APPLE_LABEL = re.compile(r'(xcode|macos)-\d+', re.ASCII)
_UBUNTU_LABEL = re.compile(r'ubuntu-(\d+)\.(\d+)', re.ASCII)
_WINDOWS_LABEL = re.compile(r'windows-(\d{4})', re.ASCII)
_MACOS_LABEL = re.compile(r'macos-(\d+)', re.ASCII)
_XCODE_LABEL = re.compile(r'xcode-(\d+)', re.ASCII)
_LABEL_IN_BACKTICKS = re.compile(r'`([^`]+)`')
_BADGE_WORD = re.compile(r'!\[([^\]]*)\]\(')
_ROLLOUT_STATE_KEY = re.compile(r'raw%2F([A-Za-z0-9._-]+)\.json', re.ASCII)
_DEPLOYING_TO = re.compile(r'Deploying to (\S+)')
_BADGE_MARKDOWN = re.compile(r'\[?!\[[^\]]*\]\([^)]*\)(?:\]\([^)]*\))?')
_NOT_A_BADGE = 'endpoint badge'


def _table_cells(line: str) -> List[str]:
    text = line.strip()
    if text.startswith('|'):
        text = text[1:]
    if text.endswith('|'):
        text = text[:-1]
    return [cell.strip() for cell in text.split('|')]


def parse_runner_images_readme(markdown: str) -> List[RunnerImageRow]:
    return parse_runner_images_readme_and_duplicate_labels(markdown)[0]


def parse_runner_images_readme_and_duplicate_labels(markdown: str) -> Tuple[List[RunnerImageRow], List[str]]:
    """A label listed in two rows keeps its first row: the README is edited by hand, and one slip must not stop the bot."""
    lines = markdown.splitlines()
    header_at = None
    for index, line in enumerate(lines):
        if not line.lstrip().startswith('|'):
            continue
        cells = _table_cells(line)
        if 'Image' in cells and 'YAML Label' in cells:
            header_at = index
            break
    if header_at is None:
        raise RunnerImagesReadmeError('the runner images README has no table with the columns Image and YAML Label')
    header = _table_cells(lines[header_at])
    image_column = header.index('Image')
    label_column = header.index('YAML Label')
    architecture_column = header.index('Architecture') if 'Architecture' in header else None
    rows: List[RunnerImageRow] = []
    seen_labels: Dict[str, str] = {}
    duplicate_labels: List[str] = []
    for line in lines[header_at + 2:]:
        if not line.startswith('|'):
            break
        cells = _table_cells(line)
        if len(cells) <= max(image_column, label_column):
            raise RunnerImagesReadmeError('a row of the runner images README has too few columns: %s' % line.strip())
        image_cell = cells[image_column]
        name = _BADGE_MARKDOWN.sub('', image_cell.split('<br>')[0]).strip()
        labels = _LABEL_IN_BACKTICKS.findall(cells[label_column])
        if not labels:
            raise RunnerImagesReadmeError('the runner images README row "%s" has no YAML label' % name)
        for label in labels:
            if label in seen_labels and label not in duplicate_labels:
                duplicate_labels.append(label)
            seen_labels.setdefault(label, name)
        labels = [label for label in labels if seen_labels[label] == name]
        if not labels:
            continue
        badges = [word.strip().lower() for word in _BADGE_WORD.findall(image_cell)
                  if word.strip() and word.strip().lower() != _NOT_A_BADGE]
        state_key = _ROLLOUT_STATE_KEY.search(image_cell)
        architecture = cells[architecture_column] if architecture_column is not None and architecture_column < len(cells) else ''
        rows.append(RunnerImageRow(name=name, arch=architecture, labels=labels, badges=badges,
                                   rollout_state_key=state_key.group(1) if state_key else None))
    if not rows:
        raise RunnerImagesReadmeError('the runner images README table has no rows')
    return rows, duplicate_labels


def parse_rollout_state(data: bytes) -> RolloutState:
    try:
        parsed = json.loads(data.decode('utf-8-sig'))
    except (ValueError, UnicodeDecodeError) as error:
        raise UnreadableSourceError('a runner rollout state is not JSON: %s' % error)
    if not isinstance(parsed, dict) or not isinstance(parsed.get('message'), str):
        raise UnreadableSourceError('a runner rollout state has no message text')
    deployed_build = parsed.get('label')
    if deployed_build is not None and not isinstance(deployed_build, str):
        raise UnreadableSourceError('a runner rollout state has a label that is not text')
    message = parsed['message']
    deploying = _DEPLOYING_TO.fullmatch(message.strip())
    return RolloutState(deployed_build=deployed_build, message=message,
                        deploying_to=deploying.group(1) if deploying else None)


def _rows_holding(rows: Iterable[RunnerImageRow], labels: Iterable[str]) -> List[RunnerImageRow]:
    wanted = set(labels)
    return [row for row in rows if wanted.intersection(row.labels)]


def read_runner_images(downloader: Any, labels: Iterable[str]) -> Dict[str, Any]:
    try:
        rows, duplicate_labels = parse_runner_images_readme_and_duplicate_labels(
            downloader.get(README_URL).decode('utf-8-sig'))
    except UnreadableSourceError as error:
        return {'status': 'error', 'error': str(error), 'rows': [], 'duplicate_labels': [], 'rollout_states': {},
                'rollout_states_error': None}
    state_keys: List[str] = []
    for row in _rows_holding(rows, labels):
        if row.rollout_state_key and row.rollout_state_key not in state_keys:
            state_keys.append(row.rollout_state_key)
    states: Dict[str, Optional[Dict[str, Any]]] = {}
    failures: List[str] = []
    for key in state_keys:
        try:
            states[key] = parse_rollout_state(downloader.get(ROLLOUT_STATE_URL.format(key=key))).to_json()
        except NotFoundError:
            states[key] = None
        except UnreadableSourceError as error:
            states[key] = None
            failures.append(str(error))
    every_request_failed = bool(state_keys) and len(failures) == len(state_keys)
    return {'status': 'ok', 'rows': [row.to_json() for row in rows], 'duplicate_labels': duplicate_labels,
            'rollout_states': states,
            'rollout_states_error': '; '.join(failures) if every_request_failed else None}


def macos_labels_to_inspect(pinned: PinnedVersions, rows: Optional[List[RunnerImageRow]],
                            required_xcode_majors: Iterable[int]) -> List[str]:
    """rows is None when the README couldn't be read; only the repo's own labels are listed then."""
    candidates = [pinned.apple_label.label]
    candidates.extend(leg.runner for leg in pinned.ios_legs)
    if rows is not None:
        for major in sorted(set(required_xcode_majors)):
            candidates.append('xcode-%d' % major)
    listed_in_readme = None if rows is None else {label for row in rows for label in row.labels}
    chosen: List[str] = []
    for label in candidates:
        if label in chosen or _APPLE_LABEL.fullmatch(label) is None:
            continue
        if listed_in_readme is not None and label not in listed_in_readme:
            continue
        chosen.append(label)
    return chosen


def _row_of(rows: List[RunnerImageRow], label: str) -> Optional[RunnerImageRow]:
    for row in rows:
        if label in row.labels:
            return row
    return None


def _number_of(pattern: 're.Pattern[str]', label: str) -> Optional[Tuple[int, ...]]:
    match = pattern.fullmatch(label)
    return tuple(int(part) for part in match.groups()) if match else None


def _readme_labels_above_every_pinned(rows: List[RunnerImageRow], pinned_labels: List[str],
                                      pattern: 're.Pattern[str]') -> List[Tuple[Tuple[int, ...], str]]:
    pinned_numbers = [number for number in (_number_of(pattern, label) for label in pinned_labels) if number]
    if not pinned_numbers:
        return []
    newest_pinned = max(pinned_numbers)
    higher = {}
    for row in rows:
        for label in row.labels:
            number = _number_of(pattern, label)
            if number is not None and number > newest_pinned:
                higher[label] = number
    return sorted((number, label) for label, number in higher.items())


def check_runner_labels(pinned: PinnedVersions, rows: List[RunnerImageRow],
                        duplicate_labels: Optional[List[str]] = None) -> FamilyPlan:
    plan = FamilyPlan(family=RUNNER_LABELS_FAMILY)
    for label in duplicate_labels or []:
        plan.things_to_check.append(ThingToCheck(
            key='runner-images-readme-duplicate-label:%s' % label,
            text='The runner images README lists %s twice; the bot used the first row.' % markdown_code(label)))
    pinned_labels = [label for family in sorted(pinned.runner_labels) for label in pinned.runner_labels[family]]
    for label in pinned_labels:
        row = _row_of(rows, label)
        if row is None:
            plan.things_to_check.append(ThingToCheck(
                key='runner-label-missing:%s' % label,
                text='The runner images README no longer lists %s, which the workflows use.' % markdown_code(label)))
        elif 'deprecated' in row.badges:
            plan.things_to_check.append(ThingToCheck(
                key='runner-label-deprecated:%s' % label,
                text='The runner images README marks %s (%s) as deprecated, and the workflows use it.'
                     % (markdown_code(label), markdown_code(row.name))))
    for family, pattern in (('ubuntu', _UBUNTU_LABEL), ('windows', _WINDOWS_LABEL)):
        pinned_in_family = pinned.runner_labels.get(family, [])
        for _, label in _readme_labels_above_every_pinned(rows, pinned_in_family, pattern):
            row = _row_of(rows, label)
            badge_words = ', '.join(markdown_code(badge) for badge in row.badges) if row is not None and row.badges else 'GA'
            plan.things_to_check.append(ThingToCheck(
                key='%s-label:%s' % (family, label),
                text='The runner images README lists %s (%s), newer than the %s labels the workflows use (%s).'
                     % (markdown_code(label), badge_words, family, ', '.join(markdown_code(used) for used in pinned_in_family))))
    pinned_macos = pinned.runner_labels.get('macos', [])
    for _, label in _readme_labels_above_every_pinned(rows, pinned_macos, _MACOS_LABEL):
        plan.notes.append('The runner images README lists `%s`, newer than the macos labels the workflows use (%s). '
                          'Those labels exist for older iOS runtimes, so nothing needs to change.'
                          % (label, ', '.join(pinned_macos)))
    pinned_xcode = pinned.runner_labels.get('xcode', [])
    for _, label in _readme_labels_above_every_pinned(rows, pinned_xcode, _XCODE_LABEL):
        plan.notes.append('The runner images README lists `%s`, newer than the xcode labels the workflows use (%s). '
                          'A person moves the Apple jobs to it; the bot prepares that change when a workload set needs '
                          'its Xcode.' % (label, ', '.join(pinned_xcode)))
    return plan
