"""Checks that each iOS leg of device-tests.yml still has its Xcode and simulator runtime on its runner image (things to check only)."""
from __future__ import annotations

from typing import Any, List, Mapping, Optional

from .data_types import FamilyPlan, SourceProblem, ThingToCheck
from .ios_runtimes import (can_a_runtime_found_by_ios_script_run_the_tests, is_found_by_ios_script,
                           list_available_ios_runtimes)
from .markdown_text import markdown_code
from .version_numbers import is_dotted_number, major, major_minor, numeric_key
from .xcode_changes import ga_xcode_versions, list_ga_ios_runtime_versions

FAMILY = 'older-ios-legs'


def _find_newest_runtime_of_same_major_that_can_run_the_tests(leg_version: str, listing: Mapping[str, Any]) -> Optional[str]:
    if not is_dotted_number(leg_version):
        return None
    same_major = [version for version in list_ga_ios_runtime_versions(listing, only_versions_that_can_run_the_tests=True)
                  if major(version) == major(leg_version)]
    return max(same_major, key=numeric_key, default=None)


def _find_newer_ga_runtime_of_same_major(leg_version: str, listing: Mapping[str, Any]) -> Optional[str]:
    newest = _find_newest_runtime_of_same_major_that_can_run_the_tests(leg_version, listing)
    if newest is not None and numeric_key(newest) > numeric_key(leg_version):
        return newest
    return None


def _what_to_do_instead(leg_version: str, listing: Mapping[str, Any]) -> str:
    instead = _find_newest_runtime_of_same_major_that_can_run_the_tests(leg_version, listing)
    if instead is None:
        return 'No other runtime of its major on the image can run them, so the leg needs another image or install-runtime.'
    return 'The iOS %s runtime can run them; move the leg to it.' % markdown_code(instead)


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
        if not any(is_found_by_ios_script(runtime, leg.ios_version) for runtime in list_available_ios_runtimes(listing)):
            plan.things_to_check.append(ThingToCheck(
                key='ios-leg-runtime-missing:%s:%s' % (leg.runner, leg.ios_version),
                text='The iOS %s leg needs its simulator runtime, which %s does not have anymore.'
                     % (markdown_code(leg.ios_version), label)))
        elif not can_a_runtime_found_by_ios_script_run_the_tests(listing, leg.ios_version):
            plan.things_to_check.append(ThingToCheck(
                key='ios-leg-runtime-cannot-run-tests:%s:%s' % (leg.runner, leg.ios_version),
                text='The iOS %s runtime of %s lists no iPhone with one built-in screen, which the status bar tests need. %s'
                     % (markdown_code(leg.ios_version), label, _what_to_do_instead(leg.ios_version, listing))))
        newer = _find_newer_ga_runtime_of_same_major(leg.ios_version, listing)
        if newer is not None:
            plan.things_to_check.append(ThingToCheck(
                key='ios-leg-newer-runtime:%s:%s:%s' % (leg.runner, leg.ios_version, newer),
                text='%s now has the newer iOS %s runtime; the iOS %s leg could move to it (a person does it, because '
                     'its required check is named after the runtime).'
                     % (label, markdown_code(newer), markdown_code(leg.ios_version))))
    return plan
