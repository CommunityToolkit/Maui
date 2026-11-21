"""Plans the Xcode edits: shared.env values the bot may change itself, and the move to a new xcode-NN label a person must make."""
from __future__ import annotations

import re
from datetime import datetime, timedelta
from typing import Any, Dict, List, Mapping, Optional, Set, Tuple

from .data_types import (ChangeForAPerson, FamilyPlan, PinnedVersions, RolloutState, SafeChange, SourceProblem,
                         ThingToCheck)
from .markdown_text import markdown_code
from .version_numbers import feature_band, major, major_minor, numeric_key, parse_utc

FAMILY = 'xcode'
GA_XCODE_APP_NAME = re.compile(r'^Xcode_\d+(\.\d+){0,2}\.app$')
SHARED_ENV_PATH = '.github/workflows/shared.env'
DEVICE_TESTS_PATH = '.github/workflows/device-tests.yml'
LIBRARY_XCODE_KEY = 'CommunityToolkitLibrary_Xcode_Version'
SAMPLE_XCODE_KEY = 'CommunityToolkitSample_Xcode_Version'
XCODE_KEY_OF_SDK = (('library', LIBRARY_XCODE_KEY), ('sample', SAMPLE_XCODE_KEY))
# Seen on 2026-05-12: the newest workload set briefly asked for an older Xcode for 50 minutes before it was replaced.
NEWEST_SET_WITH_OLDER_XCODE_WAIT = timedelta(hours=24)
APPLE_LABEL_PATTERN = re.compile(r'^(xcode|macos)-\d+$')


def ga_xcode_versions(listing: Mapping[str, Any]) -> Set[str]:
    """Same two rules as setup-xcode: a real GA folder name, and a license type that is present and not a beta."""
    versions = set()
    for app in listing.get('xcode_apps', []):
        license_type = app.get('license_type')
        if not GA_XCODE_APP_NAME.match(app.get('folder') or ''):
            continue
        if not license_type or 'beta' in str(license_type).lower():
            continue
        if not app.get('version'):
            continue
        versions.add(major_minor(app['version']))
    return versions


def _as_rollout_state(value: Any) -> Optional[RolloutState]:
    if value is None or isinstance(value, RolloutState):
        return value
    return RolloutState.from_json(value)


def _image_build(listing: Mapping[str, Any]) -> str:
    version = (listing.get('image') or {}).get('version') or ''
    return '.'.join(version.split('.')[:2])


def _rollout_status(listing: Mapping[str, Any], rollout_states: Mapping[str, Any]) -> Tuple[str, str]:
    """Returns ('ready' | 'not-ready' | 'unknown', plain-words reason)."""
    image_name = (listing.get('image') or {}).get('name') or ''
    state = _as_rollout_state((rollout_states or {}).get(image_name))
    if state is None:
        return 'unknown', 'the rollout state of %s is unknown' % image_name
    build = _image_build(listing)
    if state.deployed_build == build:
        return 'ready', '%s is fully deployed on build %s' % (image_name, build)
    return 'not-ready', 'rollout of %s in progress: the listing is on build %s, the deployed build is %s' % (
        image_name, build, state.deployed_build)


def rollout_ready(listing: Mapping[str, Any], rollout_states: Mapping[str, Any]) -> Tuple[bool, str]:
    """A listing on a build that is still deploying counts as ready when it is the deployed build: that build runs wherever
    the new one hasn't replaced it, and a newer build of the same image keeps the GA Xcodes."""
    status, reason = _rollout_status(listing, rollout_states)
    return status == 'ready', reason


def _readme_rows(runner_images: Optional[Mapping[str, Any]]) -> Optional[List[Dict[str, Any]]]:
    if not runner_images or runner_images.get('status') not in (None, 'ok'):
        return None
    rows = runner_images.get('rows')
    if rows is None:
        return None
    return [row if isinstance(row, dict) else row.to_json() for row in rows]


def _readme_lists_label(rows: Optional[List[Dict[str, Any]]], label: str) -> bool:
    return any(label in row.get('labels', []) for row in rows or [])


def _usable_listing(listings: Mapping[str, Any], label: str) -> Optional[Mapping[str, Any]]:
    listing = listings.get(label)
    if listing is None or listing.get('status') != 'ok':
        return None
    return listing


def _missing_listing_problem(listings: Mapping[str, Any], label: str) -> SourceProblem:
    listing = listings.get(label)
    detail = (listing or {}).get('error') or 'no software list was read for %s' % label
    return SourceProblem(key='source:macos-runner-image:%s' % label, source='macos-runner-image:%s' % label,
                         message=detail, family=FAMILY)


def _library_effective_band(required_xcode: Mapping[str, Any], toolkit_net_version: str,
                            apple_listing: Mapping[str, Any], notes: List[str]) -> str:
    """The library's global.json rolls forward to a band the image preinstalls, so that band's workload set decides."""
    pinned_band = required_xcode['library_pinned_band']
    toolkit_minor = major_minor(toolkit_net_version)
    bands = [pinned_band]
    for sdk in apple_listing.get('dotnet_sdks', []):
        try:
            if major_minor(sdk) == toolkit_minor:
                bands.append(feature_band(sdk))
        except ValueError:
            continue
    effective = max(bands, key=numeric_key)
    if effective == pinned_band:
        return pinned_band
    result = (required_xcode.get('bands') or {}).get(effective)
    if not result or result.get('status') != 'ok':
        notes.append('The image preinstalls SDK band %s, which has no workload set the bot could read; using band %s.'
                     % (effective, pinned_band))
        return pinned_band
    notes.append('The library builds with SDK band %s (the image preinstalls it), not %s; its workload set decides.'
                 % (effective, pinned_band))
    return effective


def _ga_versions_on_each_image(listings: Mapping[str, Any]) -> str:
    parts = []
    for label in sorted(listings):
        listing = listings[label]
        if listing.get('status') != 'ok':
            continue
        versions = sorted(ga_xcode_versions(listing), key=numeric_key)
        parts.append('%s has GA %s' % (markdown_code(label), ', '.join(versions) if versions else 'none'))
    return '; '.join(parts)


def _prerelease_apps_of(listings: Mapping[str, Any], wanted: str) -> List[str]:
    found = []
    for label in sorted(listings):
        listing = listings[label]
        if listing.get('status') != 'ok':
            continue
        for app in listing.get('xcode_apps', []):
            if not app.get('version') or major_minor(app['version']) != wanted:
                continue
            is_ga = (GA_XCODE_APP_NAME.match(app.get('folder') or '') and app.get('license_type')
                     and 'beta' not in str(app['license_type']).lower())
            if not is_ga:
                found.append('%s on %s' % (markdown_code(app['folder']), markdown_code(label)))
    return found


def _apple_label_changes(pinned: PinnedVersions, old_label: str, new_label: str, source: str) -> List[SafeChange]:
    changes = []
    for file, line, job_id in pinned.apple_label.occurrences:
        changes.append(SafeChange(
            family=FAMILY, kind='replace-runner-label', file=file, line=line, old=old_label, new=new_label,
            inserted_lines=[], pin='apple-label:%s:%s' % (file, job_id),
            description='Run the Apple job `%s` of %s on `%s`' % (job_id, file, new_label), source=source))
    return changes


def _old_ios_major_leg(pinned: PinnedVersions, old_label: str, listing: Mapping[str, Any],
                       highest_ga_xcode: str, notes: List[str]) -> Optional[SafeChange]:
    """The old label keeps its newest iOS major covered by an iOS leg, because the main Apple jobs leave it."""
    from . import line_editor

    ios_versions = [runtime['version'] for runtime in listing.get('runtimes', [])
                    if runtime.get('platform') == 'iOS' and runtime.get('available') and runtime.get('version')]
    if not ios_versions:
        notes.append('%s lists no iOS simulator runtime, so no iOS leg was added for it.' % markdown_code(old_label))
        return None
    old_major = max(major(version) for version in ios_versions)
    if any(major(leg.ios_version) == old_major for leg in pinned.ios_legs):
        return None
    newest_of_old_major = max((major_minor(version) for version in ios_versions if major(version) == old_major),
                              key=numeric_key)
    lines = line_editor.ios_leg_lines(pinned.ios_dash_prefix, newest_of_old_major, old_label, highest_ga_xcode)
    return SafeChange(
        family=FAMILY, kind='insert-ios-leg', file=DEVICE_TESTS_PATH, line=pinned.ios_include_line, old=None,
        new=newest_of_old_major, inserted_lines=lines, pin='ios-leg:%d' % old_major,
        description='Keep an iOS %s leg on `%s`, whose runtime stays installed there' % (newest_of_old_major, old_label),
        source='macos-runner-image:%s' % old_label)


# The label boundary includes "-" so that xcode-27 never matches inside xcode-27-arm64.
def _checks_to_rename(required_checks: List[str], old_label: str, new_label: str) -> List[Tuple[str, str]]:
    bounded = re.compile(r'(?<![\w.-])%s(?![\w.-])' % re.escape(old_label))
    return [(check, bounded.sub(new_label, check)) for check in required_checks if bounded.search(check)]


def _label_switch_steps(old_label: str, new_label: str, renames: List[Tuple[str, str]], rules_readable: bool) -> List[str]:
    patch_name = 'xcode-label.patch'
    if renames:
        replacements = ', and '.join('%s with %s' % (markdown_code(old), markdown_code(new)) for old, new in renames)
        rename_step = ('Right before merging, a repo admin replaces these required checks in the `main` ruleset: %s. '
                       'Until the change is on `main`, other pull requests report only the old names.' % replacements)
    elif rules_readable:
        rename_step = ('No required check is named after %s, so the `main` ruleset needs no change.'
                       % markdown_code(old_label))
    else:
        rename_step = ('The bot could not read the `main` ruleset. Right before merging, a repo admin replaces every '
                       'required check named after %s with the same name for %s.'
                       % (markdown_code(old_label), markdown_code(new_label)))
    return [
        'Save the patch below as `%s`. On a new branch from `main`, run `git apply %s` (if `main` moved, '
        '`git apply -3 %s`), then commit, push and open a pull request.' % (patch_name, patch_name, patch_name),
        'Wait for its CI. The checks named after %s will not report, so GitHub lists them as expected.'
        % markdown_code(old_label),
        rename_step,
        'Another maintainer approves the pull request (its author cannot), and it merges.',
    ]


class _SdkDecision:
    def __init__(self, name: str, key: str, current: str, required: Optional[str], set_version: Optional[str]) -> None:
        self.name = name
        self.key = key
        self.current = current
        self.required = required
        self.set_version = set_version
        self.safe_change_to: Optional[str] = None
        self.switch_to_label: Optional[str] = None


def plan_xcode_changes(pinned: PinnedVersions, required_xcode: Mapping[str, Any], listings: Mapping[str, Any],
                       runner_images: Optional[Mapping[str, Any]], branch_rules: Optional[Mapping[str, Any]],
                       now: datetime) -> FamilyPlan:
    plan = FamilyPlan(family=FAMILY)
    apple_label = pinned.apple_label.label

    if not required_xcode or required_xcode.get('status') != 'ok':
        message = (required_xcode or {}).get('error') or 'the workload sets were not read'
        plan.source_problems.append(
            SourceProblem(key='source:workload-sets', source='workload-sets', message=message, family=FAMILY))
        return plan
    if runner_images and runner_images.get('rollout_states_error'):
        plan.source_problems.append(SourceProblem(
            key='source:runner-rollout-states', source='runner-rollout-states',
            message=str(runner_images['rollout_states_error']), family=FAMILY))
        return plan
    apple_listing = _usable_listing(listings, apple_label)
    if apple_listing is None:
        plan.source_problems.append(_missing_listing_problem(listings, apple_label))
        return plan

    rollout_states = (runner_images or {}).get('rollout_states') or {}
    readme_rows = _readme_rows(runner_images)
    apple_ga = ga_xcode_versions(apple_listing)
    apple_rollout, _ = _rollout_status(apple_listing, rollout_states)
    apple_image_name = (apple_listing.get('image') or {}).get('name') or apple_label

    toolkit_net_version = pinned.shared_env['TOOLKIT_NET_VERSION'].value
    library_band = _library_effective_band(required_xcode, toolkit_net_version, apple_listing, plan.notes)
    bands = required_xcode.get('bands') or {}
    sample_band = required_xcode['sample_band']
    band_of_sdk = {'library': library_band, 'sample': sample_band}

    decisions: List[_SdkDecision] = []
    rollout_blocked_reasons: List[str] = []
    for name, key in XCODE_KEY_OF_SDK:
        current = pinned.shared_env[key].value
        result = bands.get(band_of_sdk[name])
        if not result or result.get('status') != 'ok':
            plan.notes.append('No workload set for SDK band %s yet, so the %s Xcode version stays %s.'
                              % (band_of_sdk[name], name, current))
            continue
        ios_xcode = (result.get('ios') or {}).get('xcode')
        maccatalyst_xcode = (result.get('maccatalyst') or {}).get('xcode')
        set_version = result.get('set_version') or ''
        if ios_xcode != maccatalyst_xcode:
            plan.things_to_check.append(ThingToCheck(
                key='xcode-ios-and-maccatalyst-differ:%s:%s:%s:%s' % (name, set_version, ios_xcode, maccatalyst_xcode),
                text='Workload set %s needs Xcode %s for iOS but %s for Mac Catalyst, and one value drives both '
                     'builds, so the %s Xcode version stays %s.' % (
                         markdown_code(set_version), markdown_code(ios_xcode), markdown_code(maccatalyst_xcode),
                         name, markdown_code(current))))
            continue
        required = ios_xcode
        decision = _SdkDecision(name, key, current, required, set_version)
        decisions.append(decision)

        if required == current and required in apple_ga:
            continue
        if numeric_key(required) < numeric_key(current):
            published = result.get('published')
            if published and now - parse_utc(published) < NEWEST_SET_WITH_OLDER_XCODE_WAIT:
                _add_once(plan.deferred, 'The newest workload set %s needs Xcode %s, older than the pinned %s, and was '
                          'published less than 24 hours ago; waiting in case it is replaced.'
                          % (set_version, required, current))
                plan.notes.append('The %s Xcode change waits: the newest workload set asks for an older Xcode %s.'
                                  % (name, required))
                decision.required = None
                continue
        if apple_rollout != 'ready':
            reason = 'rollout of %s in progress; Xcode change waits' % apple_image_name
            if apple_rollout == 'unknown':
                reason = 'the rollout state of %s is unknown; Xcode change waits' % apple_image_name
            rollout_blocked_reasons.append(reason)
            decision.required = None
            continue
        if required in apple_ga:
            decision.safe_change_to = required
            continue
        target_label = 'xcode-%d' % major(required)
        if (target_label != apple_label and _readme_lists_label(readme_rows, target_label)
                and _usable_listing(listings, target_label) is not None
                and required in ga_xcode_versions(listings[target_label])):
            decision.switch_to_label = target_label
            continue
        if required == current:
            _add_thing_once(plan, ThingToCheck(
                key='xcode-missing-on-image:%s:%s' % (apple_label, required),
                text='CI needs Xcode %s, which the deployed %s image does not have as a GA release. %s.%s' % (
                    markdown_code(required), markdown_code(apple_label),
                    _ga_versions_on_each_image(listings), _prerelease_suffix(listings, required))))
        else:
            _add_thing_once(plan, ThingToCheck(
                key='xcode-not-on-image:%s:%s' % (required, apple_label),
                text='Workload set %s needs Xcode %s, which %s does not have as a GA release. %s.%s' % (
                    markdown_code(decision.set_version), markdown_code(required), markdown_code(apple_label),
                    _ga_versions_on_each_image(listings), _prerelease_suffix(listings, required))))

    for reason in sorted(set(rollout_blocked_reasons), key=rollout_blocked_reasons.index):
        plan.deferred.append(reason)
        plan.notes.append(reason[0].upper() + reason[1:] + '.')
    if apple_rollout == 'unknown' and rollout_blocked_reasons:
        plan.things_to_check.append(ThingToCheck(
            key='rollout-state-unknown:%s' % apple_image_name,
            text='The rollout state of %s could not be read, so Xcode changes wait until it can.'
                 % markdown_code(apple_image_name)))

    moved_to_a_new_label = _decide_label_switch(plan, pinned, decisions, listings, rollout_states, branch_rules)
    if not moved_to_a_new_label:
        for decision in decisions:
            if decision.safe_change_to is not None and decision.safe_change_to != decision.current:
                plan.safe_changes.append(_set_xcode_version_change(pinned, decision, apple_label))
    return plan


def _add_once(texts: List[str], text: str) -> None:
    if text not in texts:
        texts.append(text)


def _add_thing_once(plan: FamilyPlan, thing: ThingToCheck) -> None:
    """The library and the sample can need the same missing Xcode, and an issue item key may appear only once."""
    if thing.key not in [existing.key for existing in plan.things_to_check]:
        plan.things_to_check.append(thing)


def _prerelease_suffix(listings: Mapping[str, Any], required: str) -> str:
    prerelease = _prerelease_apps_of(listings, required)
    return ' Seen only as a beta or release candidate: %s.' % ', '.join(prerelease) if prerelease else ''


def _set_xcode_version_change(pinned: PinnedVersions, decision: _SdkDecision, source_label: str) -> SafeChange:
    value = pinned.shared_env[decision.key]
    new = decision.safe_change_to if decision.safe_change_to is not None else decision.required
    return SafeChange(
        family=FAMILY, kind='set-xcode-version', file=SHARED_ENV_PATH, line=value.line, old=value.value, new=new,
        inserted_lines=[], pin='env:%s' % decision.key,
        description='Use Xcode %s for the %s build (workload set %s)' % (new, decision.name, decision.set_version),
        source='workload-sets+macos-runner-image:%s' % source_label)


def _decide_label_switch(plan: FamilyPlan, pinned: PinnedVersions, decisions: List[_SdkDecision],
                         listings: Mapping[str, Any], rollout_states: Mapping[str, Any],
                         branch_rules: Optional[Mapping[str, Any]]) -> bool:
    """Returns True when a change for a person now holds every Xcode change of the night."""
    switching = [decision for decision in decisions if decision.switch_to_label]
    if not switching:
        return False
    apple_label = pinned.apple_label.label
    targets = sorted({decision.switch_to_label for decision in switching})
    if len(targets) > 1:
        plan.things_to_check.append(ThingToCheck(
            key='xcode-label-cannot-host-both:%s:%s' % (apple_label, '+'.join(targets)),
            text='The library and the sample need Xcode versions from different images (%s), and one label drives '
                 'both builds.' % ', '.join(markdown_code(target) for target in targets)))
        return False
    target = targets[0]
    target_listing = listings[target]
    target_ga = ga_xcode_versions(target_listing)

    final_values = {}
    for decision in decisions:
        if decision.switch_to_label or decision.safe_change_to is not None:
            final_values[decision.name] = decision.required
        else:
            final_values[decision.name] = decision.current
    missing = {name: value for name, value in final_values.items() if value not in target_ga}
    if missing:
        plan.things_to_check.append(ThingToCheck(
            key='xcode-label-cannot-host-both:%s:%s' % (apple_label, target),
            text='%s does not have GA Xcode %s, which the %s build needs, so the Apple jobs cannot move to it.' % (
                markdown_code(target), ', '.join(markdown_code(value) for value in sorted(missing.values())),
                ' and '.join(sorted(missing)))))
        for decision in switching:
            decision.switch_to_label = None
        return False
    target_rollout, _ = _rollout_status(target_listing, rollout_states)
    if target_rollout != 'ready':
        image_name = (target_listing.get('image') or {}).get('name') or target
        reason = 'rollout of %s in progress; the move to it waits' % image_name
        if target_rollout == 'unknown':
            reason = 'the rollout state of %s is unknown; the move to it waits' % image_name
        plan.deferred.append(reason)
        plan.notes.append(reason[0].upper() + reason[1:] + '.')
        for decision in switching:
            decision.switch_to_label = None
        return False

    source = 'workload-sets+macos-runner-image:%s' % target
    changes: List[SafeChange] = []
    for decision in decisions:
        if (decision.switch_to_label or decision.safe_change_to is not None) and decision.required != decision.current:
            decision.safe_change_to = decision.required
            changes.append(_set_xcode_version_change(pinned, decision, target))
    changes.extend(_apple_label_changes(pinned, apple_label, target, source))
    apple_listing = listings[apple_label]
    highest_ga = max(ga_xcode_versions(apple_listing), key=numeric_key, default=None)
    if highest_ga is not None:
        leg = _old_ios_major_leg(pinned, apple_label, apple_listing, highest_ga, plan.notes)
        if leg is not None:
            changes.append(leg)

    rules = branch_rules or {}
    rules_readable = rules.get('status') in (None, 'ok') and rules.get('required_checks') is not None
    renames = _checks_to_rename(rules.get('required_checks') or [], apple_label, target) if rules_readable else []
    needs = ', '.join('%s needs Xcode %s' % (decision.name, decision.required) for decision in switching)
    key = 'xcode-label:%s:%s' % (apple_label, target)
    plan.changes_for_a_person.append(ChangeForAPerson(
        key=key,
        title='Move the Apple jobs from %s to %s' % (markdown_code(apple_label), markdown_code(target)),
        why='%s. Only %s has that Xcode as a GA release. The bot does not make this change itself: required checks '
            'are named after the label.' % (needs[0].upper() + needs[1:], markdown_code(target)),
        steps=_label_switch_steps(apple_label, target, renames, rules_readable),
        changes=changes,
        patch_file='patches/%s.patch' % key.replace(':', '_')))
    plan.notes.append('Every Xcode change of tonight is in the change for a person %s, so the pull request holds none.'
                      % markdown_code(key))
    return True
