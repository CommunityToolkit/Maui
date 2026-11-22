"""Turns everything the earlier jobs read into one night's plan: what the pull request holds, what a person must do, what to check.

The plan is the only thing the publishing jobs and the issue job read, so every decision about families, carry-forward,
rejections and required checks is made here, once.
"""
from __future__ import annotations

import hashlib
import json
import os
from dataclasses import dataclass, field
from datetime import datetime
from typing import Any, Dict, List, Mapping, Optional, Tuple

from . import android_levels, bump_pull_request, markers, older_ios_legs, pull_request_text, runner_labels, xcode_changes
from .change_validator import validate_changes
from .data_types import (ChangeForAPerson, FamilyPlan, PinnedVersions, RunnerImageRow, SafeChange, SourceProblem,
                         ThingToCheck)
from .errors import UnreadableSourceError
from .line_editor import apply_changes_to_texts, unified_diff
from .markdown_text import markdown_code
from .pinned_versions import read_pinned_versions
from .required_checks import renamed_required_checks

ANDROID_FAMILY = android_levels.FAMILY
XCODE_FAMILY = xcode_changes.FAMILY
CHANGE_FAMILIES = (ANDROID_FAMILY, XCODE_FAMILY)
INFORMATIONAL_FAMILIES = (runner_labels.RUNNER_LABELS_FAMILY, older_ios_legs.FAMILY)

PINNED_VERSIONS_PROBLEM_KEY = 'source:pinned-versions'
BRANCH_RULES_PROBLEM_KEY = 'source:branch-rules'
RUNNER_IMAGES_README_PROBLEM_KEY = 'source:runner-images-readme'
CHANGE_VALIDATOR_PROBLEM_KEY = 'source:change-validator'
PROBLEMS_THAT_LEAVE_THE_PULL_REQUEST_ALONE = (PINNED_VERSIONS_PROBLEM_KEY, bump_pull_request.SOURCE_KEY,
                                              CHANGE_VALIDATOR_PROBLEM_KEY)

OPEN_OR_UPDATE = 'open-or-update'
CLOSE = 'close'
LEAVE_ALONE = 'leave-alone'
NOTHING = 'nothing'

CHANGE_LIST_KEY_LENGTH = 12
MAX_DESCRIPTIONS_IN_A_NOTE = 5


@dataclass
class Plan:
    status: str
    run_url: str
    base: str
    base_sha: str
    bot_branch: str
    families: Dict[str, Dict[str, str]] = field(default_factory=dict)
    safe_changes: List[SafeChange] = field(default_factory=list)
    changes_for_a_person: List[ChangeForAPerson] = field(default_factory=list)
    things_to_check: List[ThingToCheck] = field(default_factory=list)
    source_problems: List[SourceProblem] = field(default_factory=list)
    notes: List[str] = field(default_factory=list)
    pull_request_action: str = NOTHING
    open_pull_request: Optional[int] = None
    change_list_key: str = ''
    title: str = ''
    commit_message: str = ''

    @property
    def has_source_problems(self) -> bool:
        return bool(self.source_problems)

    def to_json(self) -> Dict[str, Any]:
        return {
            'status': self.status,
            'run_url': self.run_url,
            'base': self.base,
            'base_sha': self.base_sha,
            'bot_branch': self.bot_branch,
            'families': {name: dict(state) for name, state in self.families.items()},
            'safe_changes': [change.to_json() for change in self.safe_changes],
            'changes_for_a_person': [change.to_json() for change in self.changes_for_a_person],
            'things_to_check': [thing.to_json() for thing in self.things_to_check],
            'source_problems': [problem.to_json() for problem in self.source_problems],
            'notes': list(self.notes),
            'pull_request_action': self.pull_request_action,
            'open_pull_request': self.open_pull_request,
            'change_list_key': self.change_list_key,
            'title': self.title,
            'commit_message': self.commit_message,
        }

    @classmethod
    def from_json(cls, data: Mapping[str, Any]) -> 'Plan':
        return cls(
            status=data['status'], run_url=data['run_url'], base=data['base'], base_sha=data['base_sha'],
            bot_branch=data['bot_branch'],
            families={name: dict(state) for name, state in data.get('families', {}).items()},
            safe_changes=[SafeChange.from_json(change) for change in data.get('safe_changes', [])],
            changes_for_a_person=[ChangeForAPerson.from_json(change) for change in data.get('changes_for_a_person', [])],
            things_to_check=[ThingToCheck.from_json(thing) for thing in data.get('things_to_check', [])],
            source_problems=[SourceProblem.from_json(problem) for problem in data.get('source_problems', [])],
            notes=list(data.get('notes', [])),
            pull_request_action=data.get('pull_request_action', NOTHING),
            open_pull_request=data.get('open_pull_request'),
            change_list_key=data.get('change_list_key', ''),
            title=data.get('title', ''),
            commit_message=data.get('commit_message', ''),
        )


def _read_json_file(folder: str, name: str) -> Dict[str, Any]:
    path = os.path.join(folder, name)
    try:
        with open(path, 'r', encoding='utf-8-sig') as file:
            data = json.load(file)
    except (OSError, ValueError) as error:
        return {'status': 'error', 'error': '%s could not be read: %s' % (name, error)}
    if not isinstance(data, dict):
        return {'status': 'error', 'error': '%s does not hold a JSON object' % name}
    return data


def _read_software_lists(folder: str, labels_to_inspect: Mapping[str, Any]) -> Dict[str, Any]:
    """A label whose list job failed has no file; it gets an error entry so the planners report that source."""
    listings: Dict[str, Any] = {}
    if os.path.isdir(folder):
        for name in sorted(os.listdir(folder)):
            if not name.endswith('.json'):
                continue
            data = _read_json_file(folder, name)
            listings[str(data.get('label') or name[:-len('.json')])] = data
    if labels_to_inspect.get('status') == 'ok':
        for label in labels_to_inspect.get('labels') or []:
            if label not in listings:
                listings[label] = {'status': 'error', 'label': label,
                                   'error': 'the software list of %s was not produced by the inspect job' % label}
    return listings


def _read_base_texts(repo_root: str) -> Dict[str, str]:
    texts: Dict[str, str] = {}
    for path in markers.PIN_FILES:
        with open(os.path.join(repo_root, *path.split('/')), 'r', encoding='utf-8', newline='') as file:
            texts[path] = file.read()
    return texts


def _patch_file_of(key: str) -> str:
    return 'patches/%s.patch' % key.replace(':', '_')


def _add_once_by_key(existing: List[Any], additions: List[Any]) -> None:
    keys = {item.key for item in existing}
    for item in additions:
        if item.key not in keys:
            keys.add(item.key)
            existing.append(item)


def _current_value_of(pinned: PinnedVersions, change: Mapping[str, Any]) -> str:
    pin = str(change.get('pin') or '')
    if pin.startswith('env:'):
        value = pinned.shared_env.get(pin[len('env:'):])
        return value.value if value is not None else 'no such value'
    android_pin = bump_pull_request.parse_android_pin(pin)
    if android_pin is not None:
        target, major = android_pin
        for leg in pinned.android_legs:
            if leg.target == target and leg.level.major == major:
                return leg.level.text
    if pin.startswith('apple-label:'):
        return pinned.apple_label.label
    return 'a different value'


@dataclass
class _FamilyState:
    state: str = 'planned'
    reason: str = ''

    @property
    def is_frozen(self) -> bool:
        return self.state != 'planned'


def _describe_problems(problems: List[SourceProblem]) -> str:
    return 'could not read ' + ', '.join(sorted({problem.source for problem in problems}))


def _freeze_state_of(family_plan: FamilyPlan, reason_for_everything: Optional[str]) -> _FamilyState:
    if family_plan.source_problems:
        return _FamilyState('problem', _describe_problems(family_plan.source_problems))
    if family_plan.deferred:
        return _FamilyState('deferred', '; '.join(family_plan.deferred))
    if reason_for_everything:
        return _FamilyState('frozen', reason_for_everything)
    return _FamilyState()


def _carry_forward(pinned: PinnedVersions, base_texts: Mapping[str, str], family: str,
                   open_pull_request: Optional[Mapping[str, Any]], notes: List[str]) -> List[SafeChange]:
    carried: List[SafeChange] = []
    if not open_pull_request:
        return carried
    for entry in open_pull_request.get('change_list') or []:
        if entry.get('family') != family:
            continue
        rebuilt = bump_pull_request.rebuild_change(pinned, entry)
        if rebuilt is None:
            notes.append(_dropped_note(pinned, entry, None))
            continue
        problem = _validate_safe_changes(base_texts, [rebuilt])
        if problem is not None:
            notes.append(_dropped_note(pinned, entry, 'it no longer fits main (%s)' % problem))
            continue
        carried.append(rebuilt)
    return carried


def _dropped_note(pinned: PinnedVersions, entry: Mapping[str, Any], reason: Optional[str]) -> str:
    old, new = entry.get('old'), entry.get('new')
    change = '(new leg %s)' % markdown_code(new) if entry.get('kind') == 'insert-android-leg' else (
        '(%s to %s)' % (markdown_code(old), markdown_code(new)))
    if reason is None:
        current = _current_value_of(pinned, entry)
        reason = 'main now has %s' % markdown_code(current) if old and current != old else 'it no longer applies to main'
    return 'Dropped %s %s from the open pull request: %s.' % (markdown_code(entry.get('pin')), change, reason)


def _route_required_check_renames(safe_changes: List[SafeChange], required_checks: List[str]) -> Tuple[List[SafeChange], List[ChangeForAPerson]]:
    """A change that renames a required check can't merge itself: the rule that requires the old name would wait forever."""
    staying: List[SafeChange] = []
    for_a_person: List[ChangeForAPerson] = []
    for change in safe_changes:
        renames = renamed_required_checks(change, required_checks)
        if not renames:
            staying.append(change)
            continue
        key = 'required-check-rename:%s:%s' % (markers.clean_item_key_part(change.pin), markers.clean_item_key_part(change.new or ''))
        patch_name = 'required-check-rename.patch'
        renamed_lines = ['%s becomes %s' % (markdown_code(old), markdown_code(new)) for old, new in renames]
        for_a_person.append(ChangeForAPerson(
            key=key,
            title='Rename the required check%s for %s' % ('s' if len(renames) > 1 else '', markdown_code(change.description)),
            why='%s would rename a check that the base branch requires: %s. A pull request that renames it can never '
                'satisfy the old name, so the bot does not open it.' % (markdown_code(change.description),
                                                                         '; '.join(renamed_lines)),
            steps=_required_check_rename_steps(renames, patch_name),
            changes=[change],
            patch_file=_patch_file_of(key)))
    return staying, for_a_person


def _required_check_rename_steps(renames: List[Tuple[str, str]], patch_name: str) -> List[str]:
    replacements = ', and '.join('%s with %s' % (markdown_code(old), markdown_code(new)) for old, new in renames)
    return [
        'Save the patch below as `%s`. On a new branch from `main`, run `git apply %s` (if `main` moved, '
        '`git apply -3 %s`), then commit, push and open a pull request.' % (patch_name, patch_name, patch_name),
        'Right before merging, a repo admin replaces the required check%s in the `main` ruleset: %s. Until the change '
        'is on `main`, other pull requests report only the old name%s.' % (
            's' if len(renames) > 1 else '', replacements, 's' if len(renames) > 1 else ''),
        'Another maintainer approves the pull request (its author cannot), and it merges.',
    ]


def _drop_rejected(safe_changes: List[SafeChange], rejected: List[Mapping[str, Any]], reopen_rejected: bool,
                   notes: List[str]) -> List[SafeChange]:
    if reopen_rejected or not rejected:
        return safe_changes
    rejected_by = {(item['pin'], item['new']): item['pull_request'] for item in rejected}
    staying: List[SafeChange] = []
    for change in safe_changes:
        number = rejected_by.get((change.pin, change.new))
        if number is None:
            staying.append(change)
            continue
        notes.append('Left out %s: a person closed #%d without merging it. Run the workflow with `reopen-rejected` to '
                     'bring it back.' % (markdown_code(change.description), number))
    return staying


def compute_change_list_key(safe_changes: List[SafeChange]) -> str:
    if not safe_changes:
        return ''
    lines = sorted('%s=%s' % (change.pin, change.new) for change in safe_changes)
    return hashlib.sha256('\n'.join(lines).encode('utf-8')).hexdigest()[:CHANGE_LIST_KEY_LENGTH]


def _validate_safe_changes(base_texts: Mapping[str, str], safe_changes: List[SafeChange]) -> Optional[str]:
    """None when the editor's output passes the same validator the approval step runs on the pushed commit."""
    try:
        head_texts = apply_changes_to_texts(dict(base_texts), safe_changes)
    except ValueError as error:
        return 'the planned changes do not apply to the pin files: %s' % error
    problems = validate_changes(dict(base_texts), head_texts)
    if problems:
        return '; '.join(problems[:3])
    return None


def _decide_action(plan: Plan, open_pull_request: Optional[Mapping[str, Any]]) -> str:
    person_commits = (open_pull_request or {}).get('person_commits') or []
    hit_problem = {problem.key for problem in plan.source_problems}.intersection(PROBLEMS_THAT_LEAVE_THE_PULL_REQUEST_ALONE)
    if person_commits or hit_problem:
        return LEAVE_ALONE
    if plan.safe_changes:
        return OPEN_OR_UPDATE
    return CLOSE if open_pull_request else NOTHING


def _unreadable_pinned_versions_plan(plan: Plan, error: Exception) -> Plan:
    plan.source_problems.append(SourceProblem(
        key=PINNED_VERSIONS_PROBLEM_KEY, source='pinned-versions', message=str(error), family=''))
    for family in CHANGE_FAMILIES + INFORMATIONAL_FAMILIES:
        plan.families[family] = {'state': 'problem', 'reason': 'the pinned versions could not be read'}
    plan.pull_request_action = LEAVE_ALONE
    return plan


def build_plan(latest_versions_dir: str, macos_runner_images_dir: str, repo_root: str, bump_state: Mapping[str, Any],
               base: str, base_sha: str, bot_branch: str, reopen_rejected: bool, run_url: str,
               now: datetime) -> Plan:
    plan = Plan(status='ok', run_url=run_url, base=base, base_sha=base_sha, bot_branch=bot_branch)
    try:
        pinned = read_pinned_versions(repo_root)
        base_texts = _read_base_texts(repo_root)
    except (UnreadableSourceError, OSError) as error:
        return _finish(_unreadable_pinned_versions_plan(plan, error))

    open_pull_request = bump_state.get('open') if bump_state.get('status') == 'ok' else None
    branch_rules = _read_json_file(latest_versions_dir, 'branch-rules.json')
    reason_for_everything = _reason_every_change_family_is_frozen(bump_state, branch_rules)
    family_plans = _run_planners(plan, pinned, latest_versions_dir, macos_runner_images_dir, branch_rules, now)
    _collect_problems_and_things(plan, bump_state, branch_rules, family_plans)

    states = {family: _freeze_state_of(family_plans[family], reason_for_everything) for family in CHANGE_FAMILIES}
    for family in INFORMATIONAL_FAMILIES:
        problems = family_plans[family].source_problems
        states[family] = _FamilyState('problem', _describe_problems(problems)) if problems else _FamilyState()
    plan.families = {family: {'state': state.state, 'reason': state.reason} for family, state in states.items()}

    safe_changes: List[SafeChange] = []
    carried_pins_of_frozen_families: Dict[str, List[str]] = {}
    for family in CHANGE_FAMILIES:
        if states[family].is_frozen:
            carried = _carry_forward(pinned, base_texts, family, open_pull_request, plan.notes)
            safe_changes.extend(carried)
            carried_pins_of_frozen_families[family] = [change.pin for change in carried]
        else:
            safe_changes.extend(family_plans[family].safe_changes)

    required_checks = (branch_rules.get('required_checks') or []) if branch_rules.get('status') == 'ok' else []
    safe_changes, renames_for_a_person = _route_required_check_renames(safe_changes, required_checks)
    _add_once_by_key(plan.changes_for_a_person, renames_for_a_person)
    safe_changes = _drop_rejected(safe_changes, bump_state.get('rejected') or [], reopen_rejected, plan.notes)

    if safe_changes:
        validator_problem = _validate_safe_changes(base_texts, safe_changes)
        if validator_problem is not None:
            plan.source_problems.append(SourceProblem(key=CHANGE_VALIDATOR_PROBLEM_KEY, source='change-validator',
                                                      message=validator_problem, family=''))
            safe_changes = []
    plan.safe_changes = safe_changes

    pins_kept = {change.pin for change in plan.safe_changes}
    for family, carried_pins in carried_pins_of_frozen_families.items():
        kept_count = len([pin for pin in carried_pins if pin in pins_kept])
        plan.notes.append(_frozen_note(family, states[family], open_pull_request, kept_count))

    plan.open_pull_request = open_pull_request['number'] if open_pull_request else None
    plan.pull_request_action = _decide_action(plan, open_pull_request)
    if plan.pull_request_action == LEAVE_ALONE and plan.safe_changes:
        _note_findings_not_pushed(plan)
        plan.safe_changes = []
    return _finish(plan)


def _reason_every_change_family_is_frozen(bump_state: Mapping[str, Any], branch_rules: Mapping[str, Any]) -> Optional[str]:
    reasons = []
    if bump_state.get('status') != 'ok':
        reasons.append('the bot could not read its own pull requests')
    if branch_rules.get('status') != 'ok':
        reasons.append('the base branch rules could not be read')
    return ' and '.join(reasons) if reasons else None


def _run_planners(plan: Plan, pinned: PinnedVersions, latest_versions_dir: str, macos_runner_images_dir: str,
                  branch_rules: Mapping[str, Any], now: datetime) -> Dict[str, FamilyPlan]:
    android_json = _read_json_file(latest_versions_dir, 'android.json')
    required_xcode = _read_json_file(latest_versions_dir, 'required-xcode.json')
    runner_images = _read_json_file(latest_versions_dir, 'runner-images.json')
    labels_to_inspect = _read_json_file(latest_versions_dir, 'macos-labels-to-inspect.json')
    listings = _read_software_lists(macos_runner_images_dir, labels_to_inspect)

    family_plans = {
        ANDROID_FAMILY: android_levels.plan_android_changes(pinned, android_json),
        XCODE_FAMILY: xcode_changes.plan_xcode_changes(pinned, required_xcode, listings, runner_images, branch_rules, now),
        older_ios_legs.FAMILY: older_ios_legs.check_older_ios_legs(pinned, listings),
    }
    if runner_images.get('status') == 'ok':
        rows = [RunnerImageRow.from_json(row) for row in runner_images.get('rows') or []]
        family_plans[runner_labels.RUNNER_LABELS_FAMILY] = runner_labels.check_runner_labels(
            pinned, rows, list(runner_images.get('duplicate_labels') or []))
    else:
        unreadable = FamilyPlan(family=runner_labels.RUNNER_LABELS_FAMILY)
        unreadable.source_problems.append(SourceProblem(
            key=RUNNER_IMAGES_README_PROBLEM_KEY, source='runner-images-readme',
            message=str(runner_images.get('error') or 'the runner images README was not read'),
            family=runner_labels.RUNNER_LABELS_FAMILY))
        family_plans[runner_labels.RUNNER_LABELS_FAMILY] = unreadable
    return family_plans


def _collect_problems_and_things(plan: Plan, bump_state: Mapping[str, Any], branch_rules: Mapping[str, Any],
                                 family_plans: Mapping[str, FamilyPlan]) -> None:
    problems: List[SourceProblem] = []
    for family_plan in family_plans.values():
        problems.extend(family_plan.source_problems)
    if bump_state.get('status') != 'ok':
        problems.append(SourceProblem(key=bump_pull_request.SOURCE_KEY, source='bump-pull-request',
                                      message=str(bump_state.get('error') or 'the bump pull requests were not read'),
                                      family=''))
    if branch_rules.get('status') != 'ok':
        problems.append(SourceProblem(key=BRANCH_RULES_PROBLEM_KEY, source='branch-rules',
                                      message=str(branch_rules.get('error') or 'the branch rules were not read'),
                                      family=''))
    _add_once_by_key(plan.source_problems, problems)

    things: List[ThingToCheck] = []
    for family_plan in family_plans.values():
        _add_once_by_key(plan.changes_for_a_person, family_plan.changes_for_a_person)
        things.extend(family_plan.things_to_check)
        for note in family_plan.notes:
            if note not in plan.notes:
                plan.notes.append(note)
    things.extend(ThingToCheck.from_json(thing) for thing in bump_state.get('things_to_check') or [])
    _add_once_by_key(plan.things_to_check, things)


def _frozen_note(family: str, state: _FamilyState, open_pull_request: Optional[Mapping[str, Any]],
                 carried_count: int) -> str:
    if open_pull_request and carried_count:
        return 'The %s changes are on hold tonight (%s); the open pull request keeps its %d %s change%s.' % (
            family, state.reason, carried_count, family, '' if carried_count == 1 else 's')
    if open_pull_request:
        return 'The %s changes are on hold tonight (%s); the open pull request has no %s changes to keep.' % (
            family, state.reason, family)
    return 'The %s changes are on hold tonight (%s), and no pull request is open, so none were made.' % (
        family, state.reason)


def _note_findings_not_pushed(plan: Plan) -> None:
    descriptions = [markdown_code(change.description) for change in plan.safe_changes[:MAX_DESCRIPTIONS_IN_A_NOTE]]
    more = len(plan.safe_changes) - len(descriptions)
    plan.notes.append('Found %d change%s but pushed nothing, because the open pull request is left alone: %s%s.' % (
        len(plan.safe_changes), '' if len(plan.safe_changes) == 1 else 's', ', '.join(descriptions),
        ' and %d more' % more if more > 0 else ''))


def _finish(plan: Plan) -> Plan:
    plan.change_list_key = compute_change_list_key(plan.safe_changes)
    plan.title = pull_request_text.render_title(plan)
    plan.commit_message = pull_request_text.render_commit_message(plan)
    return plan


def _write_text(path: str, text: str) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8', newline='') as file:
        file.write(text)


def patch_of(changes: List[SafeChange], base_texts: Mapping[str, str]) -> str:
    touched = {change.file for change in changes}
    head_texts = apply_changes_to_texts({path: base_texts[path] for path in touched}, changes)
    return ''.join(unified_diff(path, base_texts[path], head_texts[path]) for path in markers.PIN_FILES if path in touched)


def write_plan_files(plan: Plan, repo_root: str, out_dir: str) -> Dict[str, str]:
    base_texts = _read_base_texts(repo_root)
    changes_patch = patch_of(plan.safe_changes, base_texts) if plan.safe_changes else ''
    body = pull_request_text.render_pull_request_body(plan)
    _write_text(os.path.join(out_dir, 'plan.json'), json.dumps(plan.to_json(), indent=2, sort_keys=True) + '\n')
    _write_text(os.path.join(out_dir, 'changes.patch'), changes_patch)
    _write_text(os.path.join(out_dir, 'pull-request-body.md'), body)
    _write_text(os.path.join(out_dir, 'commit-message.txt'), plan.commit_message)
    _write_text(os.path.join(out_dir, 'title.txt'), plan.title)
    _write_text(os.path.join(out_dir, 'summary.md'),
                pull_request_text.render_run_summary(plan, changes_patch=changes_patch, pull_request_body=body))
    for change in plan.changes_for_a_person:
        _write_text(os.path.join(out_dir, *change.patch_file.split('/')), patch_of(change.changes, base_texts))
    return {
        'has-source-problems': 'true' if plan.has_source_problems else 'false',
        'pull-request-action': plan.pull_request_action,
        'change-list-key': plan.change_list_key,
    }
