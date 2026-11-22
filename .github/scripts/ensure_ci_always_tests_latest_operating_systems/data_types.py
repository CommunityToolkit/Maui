"""Records every unit passes around; each converts to and from plain JSON so jobs can hand them over as files."""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, NamedTuple, Optional, Tuple

from .version_numbers import AndroidLevel, parse_android_level


def _level_to_json(level: AndroidLevel) -> str:
    return level.text


def _level_from_json(value: Any) -> AndroidLevel:
    if isinstance(value, dict):
        value = value['text']
    return parse_android_level(value)


@dataclass
class EnvValue:
    value: str
    line: int

    def to_json(self) -> Dict[str, Any]:
        return {'value': self.value, 'line': self.line}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'EnvValue':
        return cls(value=data['value'], line=data['line'])


@dataclass
class AndroidLeg:
    level: AndroidLevel
    target: str
    api_level_line: int
    entry_start: int
    entry_end: int

    def to_json(self) -> Dict[str, Any]:
        return {'level': _level_to_json(self.level), 'target': self.target, 'api_level_line': self.api_level_line,
                'entry_start': self.entry_start, 'entry_end': self.entry_end}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'AndroidLeg':
        return cls(level=_level_from_json(data['level']), target=data['target'],
                   api_level_line=data['api_level_line'], entry_start=data['entry_start'], entry_end=data['entry_end'])


@dataclass
class IosLeg:
    ios_version: str
    runner: str
    xcode: str
    install_runtime: bool
    entry_start: int
    entry_end: int

    def to_json(self) -> Dict[str, Any]:
        return {'ios_version': self.ios_version, 'runner': self.runner, 'xcode': self.xcode,
                'install_runtime': self.install_runtime, 'entry_start': self.entry_start, 'entry_end': self.entry_end}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'IosLeg':
        return cls(ios_version=data['ios_version'], runner=data['runner'], xcode=data['xcode'],
                   install_runtime=data['install_runtime'], entry_start=data['entry_start'],
                   entry_end=data['entry_end'])


class LabelOccurrence(NamedTuple):
    file: str
    line: int
    job_id: str


@dataclass
class AppleLabel:
    label: str
    occurrences: List[LabelOccurrence]

    def to_json(self) -> Dict[str, Any]:
        return {'label': self.label, 'occurrences': [list(entry) for entry in self.occurrences]}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'AppleLabel':
        return cls(label=data['label'], occurrences=[LabelOccurrence(entry[0], entry[1], entry[2]) for entry in data['occurrences']])


@dataclass
class PinnedVersions:
    shared_env: Dict[str, EnvValue]
    android_legs: List[AndroidLeg]
    coverage_levels: List[AndroidLevel]
    ios_legs: List[IosLeg]
    apple_label: AppleLabel
    runner_labels: Dict[str, List[str]]
    android_include_line: int
    ios_include_line: int
    android_dash_prefix: str
    ios_dash_prefix: str

    def to_json(self) -> Dict[str, Any]:
        return {
            'shared_env': {key: value.to_json() for key, value in self.shared_env.items()},
            'android_legs': [leg.to_json() for leg in self.android_legs],
            'coverage_levels': [_level_to_json(level) for level in self.coverage_levels],
            'ios_legs': [leg.to_json() for leg in self.ios_legs],
            'apple_label': self.apple_label.to_json(),
            'runner_labels': {family: list(labels) for family, labels in self.runner_labels.items()},
            'android_include_line': self.android_include_line,
            'ios_include_line': self.ios_include_line,
            'android_dash_prefix': self.android_dash_prefix,
            'ios_dash_prefix': self.ios_dash_prefix,
        }

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'PinnedVersions':
        return cls(
            shared_env={key: EnvValue.from_json(value) for key, value in data['shared_env'].items()},
            android_legs=[AndroidLeg.from_json(leg) for leg in data['android_legs']],
            coverage_levels=[_level_from_json(level) for level in data['coverage_levels']],
            ios_legs=[IosLeg.from_json(leg) for leg in data['ios_legs']],
            apple_label=AppleLabel.from_json(data['apple_label']),
            runner_labels={family: list(labels) for family, labels in data['runner_labels'].items()},
            android_include_line=data['android_include_line'],
            ios_include_line=data['ios_include_line'],
            android_dash_prefix=data['android_dash_prefix'],
            ios_dash_prefix=data['ios_dash_prefix'],
        )


@dataclass
class SystemImage:
    path: str
    level_text: str
    tag: str
    abi: str
    codename: Optional[str]
    obsolete: bool

    def to_json(self) -> Dict[str, Any]:
        return {'path': self.path, 'level_text': self.level_text, 'tag': self.tag, 'abi': self.abi,
                'codename': self.codename, 'obsolete': self.obsolete}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'SystemImage':
        return cls(path=data['path'], level_text=data['level_text'], tag=data['tag'], abi=data['abi'],
                   codename=data['codename'], obsolete=data['obsolete'])


@dataclass
class RunnerImageRow:
    name: str
    arch: str
    labels: List[str]
    badges: List[str]
    rollout_state_key: Optional[str]

    def to_json(self) -> Dict[str, Any]:
        return {'name': self.name, 'arch': self.arch, 'labels': list(self.labels), 'badges': list(self.badges),
                'rollout_state_key': self.rollout_state_key}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'RunnerImageRow':
        return cls(name=data['name'], arch=data['arch'], labels=list(data['labels']), badges=list(data['badges']),
                   rollout_state_key=data['rollout_state_key'])


@dataclass
class RolloutState:
    deployed_build: Optional[str]
    message: str
    deploying_to: Optional[str]

    def to_json(self) -> Dict[str, Any]:
        return {'deployed_build': self.deployed_build, 'message': self.message, 'deploying_to': self.deploying_to}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'RolloutState':
        return cls(deployed_build=data['deployed_build'], message=data['message'], deploying_to=data['deploying_to'])


@dataclass
class SafeChange:
    """kind is set-xcode-version, replace-api-level, insert-android-leg, insert-ios-leg or replace-runner-label."""

    family: str
    kind: str
    file: str
    line: int
    old: Optional[str]
    new: Optional[str]
    inserted_lines: List[str]
    pin: str  # stable across nights so rejections and carried changes match the same change
    description: str
    source: str

    def to_json(self) -> Dict[str, Any]:
        return {'family': self.family, 'kind': self.kind, 'file': self.file, 'line': self.line, 'old': self.old,
                'new': self.new, 'inserted_lines': list(self.inserted_lines), 'pin': self.pin,
                'description': self.description, 'source': self.source}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'SafeChange':
        return cls(family=data['family'], kind=data['kind'], file=data['file'], line=data['line'], old=data['old'],
                   new=data['new'], inserted_lines=list(data['inserted_lines']), pin=data['pin'],
                   description=data['description'], source=data['source'])


@dataclass
class ChangeForAPerson:
    key: str
    title: str
    why: str
    steps: List[str]
    changes: List[SafeChange]
    patch_file: str

    def to_json(self) -> Dict[str, Any]:
        return {'key': self.key, 'title': self.title, 'why': self.why, 'steps': list(self.steps),
                'changes': [change.to_json() for change in self.changes], 'patch_file': self.patch_file}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'ChangeForAPerson':
        return cls(key=data['key'], title=data['title'], why=data['why'], steps=list(data['steps']),
                   changes=[SafeChange.from_json(change) for change in data['changes']],
                   patch_file=data['patch_file'])


@dataclass
class ThingToCheck:
    key: str
    text: str

    def to_json(self) -> Dict[str, Any]:
        return {'key': self.key, 'text': self.text}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'ThingToCheck':
        return cls(key=data['key'], text=data['text'])


@dataclass
class SourceProblem:
    key: str
    source: str
    message: str
    family: str

    def to_json(self) -> Dict[str, Any]:
        return {'key': self.key, 'source': self.source, 'message': self.message, 'family': self.family}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'SourceProblem':
        return cls(key=data['key'], source=data['source'], message=data['message'], family=data['family'])


@dataclass
class RunFailure:
    key: str
    text: str

    def to_json(self) -> Dict[str, Any]:
        return {'key': self.key, 'text': self.text}

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'RunFailure':
        return cls(key=data['key'], text=data['text'])


@dataclass
class FamilyPlan:
    """deferred holds one plain-words reason per change that waits for a later night."""

    family: str
    safe_changes: List[SafeChange] = field(default_factory=list)
    changes_for_a_person: List[ChangeForAPerson] = field(default_factory=list)
    things_to_check: List[ThingToCheck] = field(default_factory=list)
    notes: List[str] = field(default_factory=list)
    source_problems: List[SourceProblem] = field(default_factory=list)
    deferred: List[str] = field(default_factory=list)

    def merge(self, other: 'FamilyPlan') -> 'FamilyPlan':
        family = self.family if self.family == other.family else '%s+%s' % (self.family, other.family)
        return FamilyPlan(
            family=family,
            safe_changes=self.safe_changes + other.safe_changes,
            changes_for_a_person=self.changes_for_a_person + other.changes_for_a_person,
            things_to_check=self.things_to_check + other.things_to_check,
            notes=self.notes + other.notes,
            source_problems=self.source_problems + other.source_problems,
            deferred=self.deferred + other.deferred,
        )

    def to_json(self) -> Dict[str, Any]:
        return {
            'family': self.family,
            'safe_changes': [change.to_json() for change in self.safe_changes],
            'changes_for_a_person': [change.to_json() for change in self.changes_for_a_person],
            'things_to_check': [thing.to_json() for thing in self.things_to_check],
            'notes': list(self.notes),
            'source_problems': [problem.to_json() for problem in self.source_problems],
            'deferred': list(self.deferred),
        }

    @classmethod
    def from_json(cls, data: Dict[str, Any]) -> 'FamilyPlan':
        return cls(
            family=data['family'],
            safe_changes=[SafeChange.from_json(change) for change in data.get('safe_changes', [])],
            changes_for_a_person=[ChangeForAPerson.from_json(change) for change in data.get('changes_for_a_person', [])],
            things_to_check=[ThingToCheck.from_json(thing) for thing in data.get('things_to_check', [])],
            notes=list(data.get('notes', [])),
            source_problems=[SourceProblem.from_json(problem) for problem in data.get('source_problems', [])],
            deferred=list(data.get('deferred', [])),
        )
