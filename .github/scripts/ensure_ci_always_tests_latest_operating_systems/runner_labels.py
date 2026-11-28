"""Reads the GitHub runner images README and its rollout states.

Moves the device test jobs to the newest GA Ubuntu and Windows labels and checks the other runner labels against the README.
"""
from __future__ import annotations

import json
import re
from typing import Any, Dict, Iterable, List, Optional, Tuple

from .data_types import (FamilyPlan, PinnedVersions, RolloutState, RunnerImageRow, RunnerLabelOccurrence, SafeChange,
                         ThingToCheck)
from .errors import NotFoundError, RunnerImagesReadmeError, UnreadableSourceError
from .markdown_text import markdown_code
from .markers import PIN_FILES, read_file_name
from .pinned_versions import JOB_ID, parse_ubuntu_or_windows_label

README_URL = 'https://raw.githubusercontent.com/actions/runner-images/main/README.md'
ROLLOUT_STATE_URL = ('https://gist.githubusercontent.com/hosted-runners-images-bot/'
                     '79267492faab096d04cdd25ce7014cec/raw/{key}.json')

RUNNER_LABELS_FAMILY = 'runner-labels'
RUNNER_LABEL_PIN_PREFIX = 'runner-label:'
RUNNER_IMAGES_README_SOURCE = 'runner-images-readme'
README_PAGE_URL = 'https://github.com/actions/runner-images#available-images'
_APPLE_LABEL = re.compile(r'(xcode|macos)-\d+', re.ASCII)
_MACOS_LABEL = re.compile(r'macos-(\d+)', re.ASCII)
_XCODE_LABEL = re.compile(r'xcode-(\d+)', re.ASCII)
_LABEL_IN_BACKTICKS = re.compile(r'`([^`]+)`')
_BADGE_WORD = re.compile(r'!\[([^\]]*)\]\(')
_ROLLOUT_STATE_KEY = re.compile(r'raw%2F([A-Za-z0-9._-]+)\.json', re.ASCII)
_DEPLOYING_TO = re.compile(r'Deploying to (\S+)')
_BADGE_MARKDOWN = re.compile(r'\[?!\[[^\]]*\]\([^)]*\)(?:\]\([^)]*\))?')
_NOT_A_BADGE = 'endpoint badge'
_ARM_ARCHITECTURE = re.compile(r'arm', re.IGNORECASE | re.ASCII)
_MOVABLE_FAMILIES = ('ubuntu', 'windows')

DUPLICATE_UBUNTU_OR_WINDOWS_LABEL_NOTE = ("The runner images README lists %s twice, so the bot doesn't move a job to it until it is "
                                          'listed once.')
DUPLICATE_OTHER_LABEL_NOTE = 'The runner images README lists %s twice; the bot used the first row.'
UNMOVED_PLACES_NOTE = ("The bot doesn't move %s to %s: it moves only the runs-on line of a job that `device_tests` needs, "
                       'which the required Device Tests check runs.')
NOT_GA_YET_NOTE = "The runner images README lists %s (%s), so the bot doesn't move the %s jobs to it: it moves only to GA labels."
ARM_IMAGE_NOTE = "The runner images README lists %s on an %s image, so the bot doesn't move the x64 %s jobs to it."
NO_TARGET_REASON = ' The README lists no newer GA %s label that the bot can move to yet; the bot moves the jobs once it does.'
UNMOVABLE_REASON = ' The bot moves only the runs-on line of a job that `device_tests` needs; move %s yourself.'
NOT_IN_TONIGHTS_PULL_REQUEST_REASON = (" The bot isn't moving the jobs to %s tonight: a person closed the bump pull request that held the move "
                                       "(run the workflow with `reopen-rejected` to bring it back), or another item here holds it back.")


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
    first_line_of_label: Dict[str, int] = {}
    duplicate_labels: List[str] = []
    for line_index in range(header_at + 2, len(lines)):
        line = lines[line_index]
        if not line.startswith('|'):
            break
        cells = _table_cells(line)
        if len(cells) <= max(image_column, label_column):
            raise RunnerImagesReadmeError('a row of the runner images README has too few columns: %s' % line.strip())
        image_cell = cells[image_column]
        name = _BADGE_MARKDOWN.sub('', image_cell.split('<br>')[0]).strip()
        labels = list(dict.fromkeys(_LABEL_IN_BACKTICKS.findall(cells[label_column])))
        if not labels:
            raise RunnerImagesReadmeError('the runner images README row "%s" has no YAML label' % name)
        for label in labels:
            if first_line_of_label.setdefault(label, line_index) != line_index and label not in duplicate_labels:
                duplicate_labels.append(label)
        labels = [label for label in labels if first_line_of_label[label] == line_index]
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


def make_runner_label_pin(file: str, job_id: str) -> str:
    return '%s%s:%s' % (RUNNER_LABEL_PIN_PREFIX, file, job_id)


def parse_runner_label_pin(pin: str) -> Optional[Tuple[str, str]]:
    if not pin.startswith(RUNNER_LABEL_PIN_PREFIX):
        return None
    file, _, job_id = pin[len(RUNNER_LABEL_PIN_PREFIX):].rpartition(':')
    if file not in PIN_FILES or JOB_ID.fullmatch(job_id) is None:
        return None
    return file, job_id


def describe_runner_label_move(job_id: str, old: str, new: str) -> str:
    return 'Move the job %s from %s to %s' % (job_id, old, new)


def build_runner_label_move(occurrence: RunnerLabelOccurrence, new: str) -> SafeChange:
    return SafeChange(
        family=RUNNER_LABELS_FAMILY, kind='replace-runner-label', file=occurrence.file, line=occurrence.line,
        old=occurrence.label, new=new, inserted_lines=[], pin=make_runner_label_pin(occurrence.file, occurrence.job_id),
        description=describe_runner_label_move(occurrence.job_id, occurrence.label, new), source=RUNNER_IMAGES_README_SOURCE)


def _is_a_ga_x64_row(row: RunnerImageRow) -> bool:
    return not row.badges and '`' not in row.name and _ARM_ARCHITECTURE.search(row.arch) is None


def _read_label_number(label: str) -> Tuple[int, ...]:
    parsed = parse_ubuntu_or_windows_label(label)
    return parsed.number if parsed is not None else ()


def find_newest_move_target(rows: List[RunnerImageRow], family: str, duplicate_labels: Iterable[str]) -> Optional[str]:
    duplicates = set(duplicate_labels)
    best: Optional[str] = None
    for row in rows:
        if not _is_a_ga_x64_row(row):
            continue
        for label in row.labels:
            parsed = parse_ubuntu_or_windows_label(label)
            if parsed is None or parsed.family != family or label in duplicates:
                continue
            if best is None or parsed.number > _read_label_number(best):
                best = label
    return best


def _describe_places(occurrences: List[RunnerLabelOccurrence]) -> str:
    return ', '.join('%s on %s (%s)' % (markdown_code(occurrence.job_id) if occurrence.job_id else 'line %d' % occurrence.line,
                                        markdown_code(occurrence.label), markdown_code(read_file_name(occurrence.file)))
                     for occurrence in occurrences)


def _is_below(label: str, target: Optional[str]) -> bool:
    return target is not None and _read_label_number(label) < _read_label_number(target)


def _note_duplicates(plan: FamilyPlan, duplicate_labels: List[str]) -> None:
    for label in duplicate_labels:
        if _APPLE_LABEL.fullmatch(label) is not None:
            plan.things_to_check.append(ThingToCheck(
                key='runner-images-readme-duplicate-label:%s' % label,
                text=DUPLICATE_OTHER_LABEL_NOTE % markdown_code(label)))
        elif parse_ubuntu_or_windows_label(label) is not None:
            plan.notes.append(DUPLICATE_UBUNTU_OR_WINDOWS_LABEL_NOTE % markdown_code(label))
        else:
            plan.notes.append(DUPLICATE_OTHER_LABEL_NOTE % markdown_code(label))


def _note_newer_labels_that_are_not_targets(plan: FamilyPlan, rows: List[RunnerImageRow], family: str,
                                            occurrences: List[RunnerLabelOccurrence], target: Optional[str],
                                            duplicates: List[str]) -> None:
    ceiling = max([_read_label_number(occurrence.label) for occurrence in occurrences] + ([_read_label_number(target)] if target else []))
    newer: Dict[str, RunnerImageRow] = {}
    for row in rows:
        for label in row.labels:
            parsed = parse_ubuntu_or_windows_label(label)
            if parsed is not None and parsed.family == family and parsed.number > ceiling and label not in duplicates:
                newer.setdefault(label, row)
    for label in sorted(newer, key=_read_label_number):
        row = newer[label]
        if row.badges or '`' in row.name:
            words = ', '.join(markdown_code(badge) for badge in row.badges) if row.badges else markdown_code(row.name)
            plan.notes.append(NOT_GA_YET_NOTE % (markdown_code(label), words, family))
        elif _ARM_ARCHITECTURE.search(row.arch) is not None:
            plan.notes.append(ARM_IMAGE_NOTE % (markdown_code(label), markdown_code(row.arch), family))


def _read_family_of_label(label: str) -> Optional[str]:
    parsed = parse_ubuntu_or_windows_label(label)
    return parsed.family if parsed is not None else None


def _find_move_targets(pinned: PinnedVersions, rows: List[RunnerImageRow], duplicates: List[str]) -> Dict[str, Optional[str]]:
    families_in_use = {_read_family_of_label(occurrence.label) for occurrence in pinned.ubuntu_and_windows_label_occurrences}
    return {family: find_newest_move_target(rows, family, duplicates) for family in _MOVABLE_FAMILIES if family in families_in_use}


def _plan_moves_and_notes_for_ubuntu_and_windows(plan: FamilyPlan, pinned: PinnedVersions, rows: List[RunnerImageRow],
                                                 duplicates: List[str], targets: Dict[str, Optional[str]]) -> None:
    for family, target in targets.items():
        occurrences = [occurrence for occurrence in pinned.ubuntu_and_windows_label_occurrences
                       if _read_family_of_label(occurrence.label) == family]
        for occurrence in occurrences:
            if occurrence.is_movable and target is not None and _is_below(occurrence.label, target):
                plan.safe_changes.append(build_runner_label_move(occurrence, target))
        unmoved = [occurrence for occurrence in occurrences if not occurrence.is_movable and _is_below(occurrence.label, target)]
        if unmoved and target is not None:
            plan.notes.append(UNMOVED_PLACES_NOTE % (_describe_places(unmoved), markdown_code(target)))
        _note_newer_labels_that_are_not_targets(plan, rows, family, occurrences, target, duplicates)


def _explain_why_a_pinned_label_is_not_moved(pinned: PinnedVersions, family: str, label: str, targets: Dict[str, Optional[str]]) -> str:
    target = targets.get(family)
    if target is None or not _is_below(label, target):
        return NO_TARGET_REASON % family
    unmovable = [occurrence for occurrence in pinned.ubuntu_and_windows_label_occurrences
                 if occurrence.label == label and not occurrence.is_movable]
    if unmovable:
        return UNMOVABLE_REASON % _describe_places(unmovable)
    return NOT_IN_TONIGHTS_PULL_REQUEST_REASON % markdown_code(target)


def plan_runner_label_changes(pinned: PinnedVersions, rows: List[RunnerImageRow],
                              duplicate_labels: Optional[List[str]] = None) -> FamilyPlan:
    plan = FamilyPlan(family=RUNNER_LABELS_FAMILY)
    duplicates = list(duplicate_labels or [])
    _note_duplicates(plan, duplicates)
    targets = _find_move_targets(pinned, rows, duplicates)
    _plan_moves_and_notes_for_ubuntu_and_windows(plan, pinned, rows, duplicates, targets)
    pinned_labels = [label for family in sorted(pinned.runner_labels) for label in pinned.runner_labels[family]]
    for label in pinned_labels:
        row = _row_of(rows, label)
        parsed = parse_ubuntu_or_windows_label(label)
        reason = _explain_why_a_pinned_label_is_not_moved(pinned, parsed.family, label, targets) if parsed is not None else ''
        if row is None:
            plan.things_to_check.append(ThingToCheck(
                key='runner-label-missing:%s' % label,
                text='The runner images README no longer lists %s, which the workflows use.' % markdown_code(label) + reason))
        elif 'deprecated' in row.badges:
            plan.things_to_check.append(ThingToCheck(
                key='runner-label-deprecated:%s' % label,
                text='The runner images README marks %s (%s) as deprecated, and the workflows use it.'
                     % (markdown_code(label), markdown_code(row.name)) + reason))
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


def drop_items_that_the_planned_moves_fix(things: List[ThingToCheck], safe_changes: List[SafeChange],
                                          pinned: PinnedVersions) -> List[ThingToCheck]:
    moved = {(change.file, change.line, change.old) for change in safe_changes
             if change.family == RUNNER_LABELS_FAMILY and change.kind == 'replace-runner-label'
             and change.pin.startswith(RUNNER_LABEL_PIN_PREFIX)}
    fixed_labels = set()
    for label in {occurrence.label for occurrence in pinned.ubuntu_and_windows_label_occurrences}:
        uses = [occurrence for occurrence in pinned.ubuntu_and_windows_label_occurrences if occurrence.label == label]
        if all((use.file, use.line, use.label) in moved for use in uses):
            fixed_labels.add(label)
    dropped_keys = {'%s:%s' % (prefix, label) for label in fixed_labels
                    for prefix in ('runner-label-deprecated', 'runner-label-missing')}
    return [thing for thing in things if thing.key not in dropped_keys]
