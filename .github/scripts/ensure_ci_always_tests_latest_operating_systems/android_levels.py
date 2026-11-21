"""Reads Google's Android system image index and plans which Android API levels the device tests should run on."""
from __future__ import annotations

import re
import xml.etree.ElementTree as ElementTree
from typing import Any, Dict, Iterable, List, Optional, Set, Tuple

from . import markers
from .data_types import (AndroidLeg, FamilyPlan, PinnedVersions, SafeChange, SourceProblem, SystemImage,
                         ThingToCheck)
from .errors import AndroidIndexFormatError
from .line_editor import android_leg_lines
from .version_numbers import AndroidLevel, parse_android_level

# The index that cmdline-tools 20.0 reads, which is the version the emulator action installs. It lists both google_apis
# and google_apis_ps16k images; -5 is unreadable to 20.0 and -1/-2 stopped at API 34.
SYSTEM_IMAGES_URL = 'https://dl.google.com/android/repository/sys-img/google_apis/sys-img2-4.xml'
# The emulator action also installs platforms;android-<level>, so a level without that package can't boot.
PLATFORMS_URL = 'https://dl.google.com/android/repository/repository2-3.xml'

FAMILY = 'android'
SOURCE_NAME = 'android-system-images'
FOUR_KB_TARGET = 'google_apis'
SIXTEEN_KB_TARGET = 'google_apis_ps16k'
DEVICE_TESTS_FILE = markers.PIN_FILES[1]
_PAGE_SIZE_LABEL = {FOUR_KB_TARGET: '4 KB', SIXTEEN_KB_TARGET: '16 KB'}
_IMAGE_SIZE_LIMIT_IN_BYTES = 20_000_000

_STABLE_IMAGE_PATH = re.compile(
    r'system-images;android-(\d+(?:\.\d+)?);(google_apis|google_apis_ps16k);x86_64', re.ASCII)
_PLATFORM_PATH = re.compile(r'platforms;android-(\d+(?:\.\d+)?)', re.ASCII)


def _local_name(tag: str) -> str:
    return tag.rsplit('}', 1)[-1]


def _remote_packages(data: bytes, expected_root: str) -> List[ElementTree.Element]:
    try:
        root = ElementTree.fromstring(data)
    except ElementTree.ParseError as error:
        raise AndroidIndexFormatError('the Android index is not valid XML: %s' % error)
    if _local_name(root.tag) != expected_root:
        raise AndroidIndexFormatError(
            'the Android index starts with <%s>, expected <%s>' % (_local_name(root.tag), expected_root))
    packages = [child for child in root if _local_name(child.tag) == 'remotePackage']
    if not packages:
        raise AndroidIndexFormatError('the Android index <%s> has no remotePackage' % expected_root)
    return packages


def _codename_of(package: ElementTree.Element) -> Optional[str]:
    for details in package:
        if _local_name(details.tag) != 'type-details':
            continue
        for element in details:
            if _local_name(element.tag) == 'codename':
                text = (element.text or '').strip()
                return text or None
    return None


def _is_obsolete(package: ElementTree.Element) -> bool:
    return package.get('obsolete', '').strip().lower() == 'true'


def parse_system_images(data: bytes) -> List[SystemImage]:
    """Keeps only x86_64 images whose path names a plain level; ext, beta and canary images carry other path shapes."""
    images = []
    for package in _remote_packages(data, 'sdk-sys-img'):
        path = package.get('path', '')
        match = _STABLE_IMAGE_PATH.fullmatch(path)
        if match is None:
            continue
        images.append(SystemImage(path=path, level_text=match.group(1), tag=match.group(2), abi='x86_64',
                                  codename=_codename_of(package), obsolete=_is_obsolete(package)))
    return images


def parse_platforms(data: bytes) -> Set[str]:
    levels = set()
    for package in _remote_packages(data, 'sdk-repository'):
        match = _PLATFORM_PATH.fullmatch(package.get('path', ''))
        if match is None or _is_obsolete(package) or _codename_of(package) is not None:
            continue
        levels.add(match.group(1))
    return levels


def stable_levels(images: Iterable[SystemImage], tag: str) -> Set[str]:
    """A preview image carries a codename, so only blank-codename, non-obsolete images count as released."""
    return {image.level_text for image in images
            if image.tag == tag and image.codename is None and not image.obsolete}


def _sorted_texts(level_texts: Iterable[str]) -> List[str]:
    """When the index spells one level two ways (38 and 38.0), the dotted spelling wins so a run never depends on set order."""
    spelling_of: Dict[Tuple[int, int], str] = {}
    for text in sorted(set(level_texts)):
        level = parse_android_level(text)
        key = (level.major, level.minor)
        if key not in spelling_of or ('.' in text, text) > ('.' in spelling_of[key], spelling_of[key]):
            spelling_of[key] = text
    return [spelling_of[key] for key in sorted(spelling_of)]


def read_android_index(downloader: Any) -> Dict[str, Any]:
    images = parse_system_images(downloader.get(SYSTEM_IMAGES_URL, max_bytes=_IMAGE_SIZE_LIMIT_IN_BYTES))
    platform_levels = {parse_android_level(text) for text in
                       parse_platforms(downloader.get(PLATFORMS_URL, max_bytes=_IMAGE_SIZE_LIMIT_IN_BYTES))}
    stable: Dict[str, List[str]] = {}
    with_platform: Dict[str, List[str]] = {}
    for tag in (FOUR_KB_TARGET, SIXTEEN_KB_TARGET):
        levels = _sorted_texts(stable_levels(images, tag))
        stable[tag] = levels
        with_platform[tag] = [text for text in levels if parse_android_level(text) in platform_levels]
    return {'status': 'ok', 'stable': stable, 'with_platform': with_platform}


def _levels_from_json(texts: Iterable[str]) -> Set[AndroidLevel]:
    return {parse_android_level(text) for text in texts}


def _in_major(levels: Iterable[AndroidLevel], major: int) -> List[AndroidLevel]:
    return sorted(level for level in levels if level.major == major)


def _highest(levels: Iterable[AndroidLevel]) -> Optional[AndroidLevel]:
    return max(levels, default=None)


class _AndroidPlanner:
    def __init__(self, pinned: PinnedVersions, android_json: Dict[str, Any]) -> None:
        self.pinned = pinned
        self.plan = FamilyPlan(family=FAMILY)
        stable = android_json['stable']
        with_platform = android_json['with_platform']
        self.released = {FOUR_KB_TARGET: _levels_from_json(stable[FOUR_KB_TARGET]),
                         SIXTEEN_KB_TARGET: _levels_from_json(stable[SIXTEEN_KB_TARGET])}
        self.bootable = {FOUR_KB_TARGET: _levels_from_json(with_platform[FOUR_KB_TARGET]),
                         SIXTEEN_KB_TARGET: _levels_from_json(with_platform[SIXTEEN_KB_TARGET])}
        self.legs_by_target = {
            target: [leg for leg in pinned.android_legs if leg.target == target]
            for target in (FOUR_KB_TARGET, SIXTEEN_KB_TARGET)}
        self.taken = {leg.level for leg in pinned.android_legs} | set(pinned.coverage_levels)
        self.levels_planned_for_each_target = {FOUR_KB_TARGET: [], SIXTEEN_KB_TARGET: []}
        self.majors_needing_trimmed_leg_note: List[int] = []
        self.majors_without_four_kb_image: List[int] = []

    def build(self) -> FamilyPlan:
        problem = self._find_problem_with_the_index()
        if problem is not None:
            self.plan.source_problems.append(SourceProblem(
                key='source:' + SOURCE_NAME, source=SOURCE_NAME, message=problem, family=FAMILY))
            return self.plan
        self._plan_existing_majors()
        self._plan_new_majors()
        self._add_trimmed_leg_notes()
        self._note_levels_waiting_for_platforms()
        self._report_legs_missing_from_the_index()
        return self.plan

    def _find_problem_with_the_index(self) -> Optional[str]:
        four_kb_released = self.released[FOUR_KB_TARGET]
        if not four_kb_released:
            return 'the index lists no released 4 KB Android system image'
        for target in (FOUR_KB_TARGET, SIXTEEN_KB_TARGET):
            pinned_levels = [leg.level for leg in self.legs_by_target[target]]
            if not pinned_levels:
                continue
            newest_released = _highest(self.released[target])
            if newest_released is None or newest_released < max(pinned_levels):
                return ('the index looks truncated or frozen: its newest released %s image is %s but the device tests '
                        'already use %s' % (_PAGE_SIZE_LABEL[target],
                                            newest_released.text if newest_released else 'missing',
                                            max(pinned_levels).text))
        return None

    def _leg_of(self, target: str, major: int) -> Optional[AndroidLeg]:
        for leg in self.legs_by_target[target]:
            if leg.level.major == major:
                return leg
        return None

    def _bootable_in_major(self, target: str, major: int) -> List[AndroidLevel]:
        return _in_major(self.bootable[target], major)

    def _highest_free_level(self, target: str, major: int, above: Optional[AndroidLevel]) -> Optional[AndroidLevel]:
        return _highest(level for level in self._bootable_in_major(target, major)
                        if level not in self.taken and (above is None or level > above))

    def _plan_existing_majors(self) -> None:
        majors = sorted({leg.level.major for target in self.legs_by_target for leg in self.legs_by_target[target]})
        for major in majors:
            four_kb_leg = self._leg_of(FOUR_KB_TARGET, major)
            sixteen_kb_leg = self._leg_of(SIXTEEN_KB_TARGET, major)
            newest_four_kb = _highest(self._bootable_in_major(FOUR_KB_TARGET, major))
            newest_sixteen_kb = _highest(self._bootable_in_major(SIXTEEN_KB_TARGET, major))

            if four_kb_leg is not None and newest_four_kb is not None and newest_four_kb > four_kb_leg.level:
                level = self._highest_free_level(FOUR_KB_TARGET, major, four_kb_leg.level)
                self._report_collision_when_not_newest(level, newest_four_kb)
                if level is not None:
                    self._replace_level(four_kb_leg, level)
            elif four_kb_leg is None and newest_four_kb is not None:
                # A major that arrived with a 16 KB image only gets its 4 KB leg once Google ships a 4 KB image of it.
                level = self._highest_free_level(FOUR_KB_TARGET, major, None)
                self._report_collision_when_not_newest(level, newest_four_kb)
                if level is not None:
                    self._insert_leg(FOUR_KB_TARGET, level)
                    self.majors_needing_trimmed_leg_note.append(major)

            sixteen_kb_level_after = sixteen_kb_leg.level if sixteen_kb_leg is not None else None
            if sixteen_kb_leg is not None and newest_sixteen_kb is not None and newest_sixteen_kb > sixteen_kb_leg.level:
                level = self._highest_free_level(SIXTEEN_KB_TARGET, major, sixteen_kb_leg.level)
                self._report_collision_when_not_newest(level, newest_sixteen_kb)
                if level is not None:
                    self._replace_level(sixteen_kb_leg, level)
                    sixteen_kb_level_after = level
            elif (sixteen_kb_leg is None and newest_sixteen_kb is not None
                  and (newest_four_kb is None or newest_sixteen_kb > newest_four_kb)):
                level = self._highest_free_level(SIXTEEN_KB_TARGET, major, newest_four_kb)
                if level is not None:
                    self._insert_leg(SIXTEEN_KB_TARGET, level)

            if sixteen_kb_level_after is not None:
                self._report_four_kb_image_at_or_above_sixteen_kb_leg(major, sixteen_kb_level_after)

    def _plan_new_majors(self) -> None:
        top_major = max((leg.level.major for target in self.legs_by_target for leg in self.legs_by_target[target]),
                        default=0)
        new_majors = sorted({level.major for target in self.bootable for level in self.bootable[target]
                             if level.major > top_major})
        for major in new_majors:
            newest_four_kb = _highest(self._bootable_in_major(FOUR_KB_TARGET, major))
            newest_sixteen_kb = _highest(self._bootable_in_major(SIXTEEN_KB_TARGET, major))
            # A taken level here can only be a coverage level: a duplicate would make the validator drop every change.
            if newest_four_kb is not None:
                if newest_four_kb in self.taken:
                    self._add_collision(newest_four_kb)
                else:
                    self._insert_leg(FOUR_KB_TARGET, newest_four_kb)
                    self.majors_needing_trimmed_leg_note.append(major)
            if newest_sixteen_kb is not None and (newest_four_kb is None or newest_sixteen_kb > newest_four_kb):
                if newest_sixteen_kb in self.taken:
                    self._add_collision(newest_sixteen_kb)
                else:
                    self._insert_leg(SIXTEEN_KB_TARGET, newest_sixteen_kb)
                    if newest_four_kb is None:
                        self.majors_without_four_kb_image.append(major)

    def _add_trimmed_leg_notes(self) -> None:
        trimmed_level = _highest(self.levels_planned_for_each_target[FOUR_KB_TARGET] + [leg.level for leg in self.legs_by_target[FOUR_KB_TARGET]])
        if trimmed_level is None:
            return
        if self.majors_needing_trimmed_leg_note:
            majors = sorted(self.majors_needing_trimmed_leg_note)
            listed = ' and '.join([', '.join(str(major) for major in majors[:-1]), str(majors[-1])] if len(majors) > 1
                                  else [str(majors[0])])
            self.plan.notes.append(
                'New Android major%s %s: the trimmed Android legs (`device_tests_android`) now boot %s. Their emulator '
                'features (step `api`, `case "$level" in 37|37.*`) and RAM default are set in that job; check them if '
                'CI fails on the new leg.' % ('s' if len(majors) > 1 else '', listed, trimmed_level.text))
        for major in self.majors_without_four_kb_image:
            self.plan.notes.append(
                'New Android major %d has no 4 KB image yet, so the trimmed Android legs (`device_tests_android`) '
                'stay on %s.' % (major, trimmed_level.text))

    def _note_levels_waiting_for_platforms(self) -> None:
        top_major = max((leg.level.major for leg in self.pinned.android_legs), default=0)
        for target in (FOUR_KB_TARGET, SIXTEEN_KB_TARGET):
            for level in sorted(self.released[target] - self.bootable[target]):
                if self._planner_would_use(target, level, top_major):
                    self.plan.notes.append('Android %s (%s) waits for its platforms package.' % (level.text, target))

    def _planner_would_use(self, target: str, level: AndroidLevel, top_major: int) -> bool:
        """The same rules _plan_existing_majors and _plan_new_majors apply once the level has its platforms package."""
        leg = self._leg_of(target, level.major)
        if leg is not None:
            return level > leg.level
        if level.major > top_major:
            return True
        if target == FOUR_KB_TARGET:
            return True
        newest_four_kb = _highest(self._bootable_in_major(FOUR_KB_TARGET, level.major))
        return newest_four_kb is None or level > newest_four_kb

    def _report_legs_missing_from_the_index(self) -> None:
        for target in (FOUR_KB_TARGET, SIXTEEN_KB_TARGET):
            for leg in self.legs_by_target[target]:
                if leg.level not in self.released[target]:
                    self._add_thing_to_check(
                        'android-leg-missing:%s:%s' % (target, leg.level.text),
                        'The %s Android %s leg is no longer in Google\'s system image index. Remove the leg or pick '
                        'another level.' % (_PAGE_SIZE_LABEL[target], leg.level.text))

    def _report_four_kb_image_at_or_above_sixteen_kb_leg(self, major: int, sixteen_kb_level: AndroidLevel) -> None:
        released_four_kb_levels = [level for level in _in_major(self.released[FOUR_KB_TARGET], major)
                                   if level >= sixteen_kb_level]
        newest = _highest(released_four_kb_levels)
        if newest is not None:
            self._add_thing_to_check(
                'android-16kb-leg-has-4kb-image:%s:%s' % (sixteen_kb_level.text, newest.text),
                'The 16 KB leg Android %s now has a 4 KB image (%s). Decide whether the 16 KB leg should move to a '
                'newer level or the 4 KB leg should take this one.' % (sixteen_kb_level.text, newest.text))

    def _report_collision_when_not_newest(self, level_used: Optional[AndroidLevel], newest: AndroidLevel) -> None:
        if level_used != newest:
            self._add_collision(newest)

    def _add_collision(self, level: AndroidLevel) -> None:
        self._add_thing_to_check(
            'android-level-collision:' + level.text,
            'Android %s is released but already used by another leg or by the coverage job, and two jobs must not '
            'share a level. Choose which job keeps it.' % level.text)

    def _add_thing_to_check(self, key: str, text: str) -> None:
        if all(thing.key != key for thing in self.plan.things_to_check):
            self.plan.things_to_check.append(ThingToCheck(key=key, text=text))

    def _replace_level(self, leg: AndroidLeg, new_level: AndroidLevel) -> None:
        page_size = _PAGE_SIZE_LABEL[leg.target]
        self.plan.safe_changes.append(SafeChange(
            family=FAMILY, kind='replace-api-level', file=DEVICE_TESTS_FILE, line=leg.api_level_line,
            old=leg.level.text, new=new_level.text, inserted_lines=[],
            pin='android-leg:%s:%d' % (leg.target, leg.level.major),
            description='Move the %s Android leg from API %s to %s' % (page_size, leg.level.text, new_level.text),
            source=SOURCE_NAME))
        self.taken.add(new_level)
        self.levels_planned_for_each_target[leg.target].append(new_level)

    def _insert_leg(self, target: str, level: AndroidLevel) -> None:
        after_line = self._line_to_insert_after(level)
        self.plan.safe_changes.append(SafeChange(
            family=FAMILY, kind='insert-android-leg', file=DEVICE_TESTS_FILE, line=after_line,
            old=None, new=level.text,
            inserted_lines=android_leg_lines(self.pinned.android_dash_prefix, level.text, target),
            pin='android-leg:%s:%d' % (target, level.major),
            description='Add a %s Android leg for API %s' % (_PAGE_SIZE_LABEL[target], level.text),
            source=SOURCE_NAME))
        self.taken.add(level)
        self.levels_planned_for_each_target[target].append(level)

    def _line_to_insert_after(self, level: AndroidLevel) -> int:
        # Only legs already in the file count: the editor orders several inserts after one line by level.
        lower_legs = [leg for leg in self.pinned.android_legs if leg.level < level]
        if not lower_legs:
            return self.pinned.android_include_line
        return max(lower_legs, key=lambda leg: leg.level).entry_end


def plan_android_changes(pinned: PinnedVersions, android_json: Dict[str, Any]) -> FamilyPlan:
    if android_json.get('status') != 'ok':
        message = android_json.get('error') or 'the Android system image index could not be read'
        plan = FamilyPlan(family=FAMILY)
        plan.source_problems.append(SourceProblem(
            key='source:' + SOURCE_NAME, source=SOURCE_NAME, message=str(message), family=FAMILY))
        return plan
    return _AndroidPlanner(pinned, android_json).build()
