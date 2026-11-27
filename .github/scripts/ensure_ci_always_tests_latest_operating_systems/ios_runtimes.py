"""Which simulator runtimes of a listing eng/devices/ios.ps1 can run the device tests on."""
from __future__ import annotations

from typing import Any, List, Mapping

LISTS_IPHONE_WITH_ONE_SCREEN = 'lists_iphone_with_one_screen'


def list_available_ios_runtimes(listing: Mapping[str, Any]) -> List[Mapping[str, Any]]:
    return [runtime for runtime in listing.get('runtimes', [])
            if runtime.get('platform') == 'iOS' and runtime.get('available') and runtime.get('version')]


def _is_equal_ignoring_case(value: Any, requested: str) -> bool:
    """PowerShell's -eq, which eng/devices/ios.ps1 uses, ignores letter case; its EndsWith for the identifier suffix does not."""
    return isinstance(value, str) and value.lower() == requested.lower()


def is_found_by_ios_script(runtime: Mapping[str, Any], requested: str) -> bool:
    """Matches a runtime the way Get-MatchingSimulatorRuntimes in eng/devices/ios.ps1 does."""
    return (_is_equal_ignoring_case(runtime.get('version'), requested) or _is_equal_ignoring_case(runtime.get('identifier'), requested)
            or _is_equal_ignoring_case(runtime.get('name'), requested) or (runtime.get('identifier') or '').endswith('.' + requested))


def is_known_to_list_no_iphone_with_one_screen(runtime: Mapping[str, Any]) -> bool:
    """A listing without the field (an old one, or a runner image README) does not know, so its runtimes stay."""
    return runtime.get(LISTS_IPHONE_WITH_ONE_SCREEN) is False


def can_a_runtime_found_by_ios_script_run_the_tests(listing: Mapping[str, Any], requested: str) -> bool:
    """ios.ps1 takes a match that lists an iPhone with one screen, else one that lists an iPhone whose screens could not be counted."""
    found = [runtime for runtime in list_available_ios_runtimes(listing) if is_found_by_ios_script(runtime, requested)]
    return any(not is_known_to_list_no_iphone_with_one_screen(runtime) for runtime in found)
