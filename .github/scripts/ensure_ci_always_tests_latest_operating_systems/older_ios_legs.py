"""Checks that each iOS leg of device-tests.yml still has its Xcode and simulator runtime on its runner image (things to check only)."""
from __future__ import annotations

from typing import Any, List, Mapping, Optional

from .data_types import FamilyPlan, SourceProblem, ThingToCheck
from .markdown_text import markdown_code
from .version_numbers import major, major_minor, numeric_key
from .xcode_changes import ga_xcode_versions

FAMILY = 'older-ios-legs'


def _available_ios_versions(listing: Mapping[str, Any]) -> List[str]:
    return [major_minor(runtime['version']) for runtime in listing.get('runtimes', [])
            if runtime.get('platform') == 'iOS' and runtime.get('available') and runtime.get('version')]


def _newer_runtime_of_same_major(leg_version: str, available: List[str]) -> Optional[str]:
    same_major = [version for version in available if major(version) == major(leg_version)]
    newest = max(same_major, key=numeric_key, default=None)
    if newest is not None and numeric_key(newest) > numeric_key(major_minor(leg_version)):
        return newest
    return None


def check_older_ios_legs(pinned: Any, listings: Mapping[str, Any]) -> FamilyPlan:
    """Moving a leg to a newer runtime renames a required check, so a person does it; the bot only says so."""
    plan = FamilyPlan(family=FAMILY)
    reported_missing_listings = set()
    # Four legs share one runner and Xcode, and a key can appear only once in the issue.
    reported_xcode_keys = set()
    for leg in pinned.ios_legs:
        listing = listings.get(leg.runner)
        if listing is None or listing.get('status') != 'ok':
            if leg.runner not in reported_missing_listings:
                reported_missing_listings.add(leg.runner)
                message = (listing or {}).get('error') or 'no software list was read for %s' % leg.runner
                plan.source_problems.append(SourceProblem(
                    key='source:macos-runner-image:%s' % leg.runner, source='macos-runner-image:%s' % leg.runner,
                    message=message, family=FAMILY))
            continue
        label = markdown_code(leg.runner)
        xcode_missing_key = 'ios-leg-xcode-missing:%s:%s' % (leg.runner, leg.xcode)
        if major_minor(leg.xcode) not in ga_xcode_versions(listing) and xcode_missing_key not in reported_xcode_keys:
            reported_xcode_keys.add(xcode_missing_key)
            plan.things_to_check.append(ThingToCheck(
                key=xcode_missing_key,
                text='Xcode %s, which an iOS leg uses, is not a GA release on %s.'
                     % (markdown_code(leg.xcode), label)))
        if leg.install_runtime:
            continue
        available = _available_ios_versions(listing)
        leg_version = major_minor(leg.ios_version)
        if leg_version not in available:
            plan.things_to_check.append(ThingToCheck(
                key='ios-leg-runtime-missing:%s:%s' % (leg.runner, leg.ios_version),
                text='The iOS %s leg needs its simulator runtime, which %s does not have anymore.'
                     % (markdown_code(leg.ios_version), label)))
            continue
        newer = _newer_runtime_of_same_major(leg.ios_version, available)
        if newer is not None:
            plan.things_to_check.append(ThingToCheck(
                key='ios-leg-newer-runtime:%s:%s:%s' % (leg.runner, leg.ios_version, newer),
                text='%s now has the newer iOS %s runtime; the iOS %s leg could move to it (a person does it, because '
                     'its required check is named after the runtime).'
                     % (label, markdown_code(newer), markdown_code(leg.ios_version))))
    return plan
