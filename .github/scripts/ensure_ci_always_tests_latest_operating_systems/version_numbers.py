"""Parsing and ordering of the version strings the bot reads (Android levels, .NET SDK and workload versions)."""
from __future__ import annotations

import re
from collections import namedtuple
from datetime import datetime, timedelta, timezone
from typing import Optional, Tuple

_LEVEL = re.compile(r'(\d+)(?:\.(\d+))?', re.ASCII)
_DOTTED_NUMBERS = re.compile(r'\d+(?:\.\d+)*', re.ASCII)
_SDK_VERSION = re.compile(r'(\d+)\.(\d+)\.(\d+)(?:[-+].*)?', re.ASCII | re.DOTALL)
_PACKAGE_VERSION = re.compile(r'(\d+)\.(\d+)\.(\d+)([-+].*)?', re.ASCII | re.DOTALL)
_CHANNEL = re.compile(r'(\d+)\.(\d+)\.x', re.ASCII)
_UTC_TIMESTAMP = re.compile(
    r'(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2}):(\d{2})(?:\.(\d{1,7}))?(Z|[+-]\d{2}:\d{2})?', re.ASCII)


class AndroidLevel(namedtuple('AndroidLevel', ['major', 'minor', 'text'])):
    """Compared by (major, minor) only, so '37' equals '37.0' while the text keeps what the file said."""

    __slots__ = ()

    def _order_key(self) -> Tuple[int, int]:
        return (self.major, self.minor)

    def __eq__(self, other: object) -> bool:
        if not isinstance(other, AndroidLevel):
            return NotImplemented
        return self._order_key() == other._order_key()

    def __ne__(self, other: object) -> bool:
        result = self.__eq__(other)
        return result if result is NotImplemented else not result

    def __lt__(self, other: 'AndroidLevel') -> bool:
        return self._order_key() < other._order_key()

    def __le__(self, other: 'AndroidLevel') -> bool:
        return self._order_key() <= other._order_key()

    def __gt__(self, other: 'AndroidLevel') -> bool:
        return self._order_key() > other._order_key()

    def __ge__(self, other: 'AndroidLevel') -> bool:
        return self._order_key() >= other._order_key()

    def __hash__(self) -> int:
        return hash(self._order_key())


def parse_android_level(text: str) -> AndroidLevel:
    match = _LEVEL.fullmatch(text) if isinstance(text, str) else None
    if match is None:
        raise ValueError('not an Android API level: %r' % (text,))
    return AndroidLevel(int(match.group(1)), int(match.group(2) or 0), text)


def numeric_key(version: str) -> Tuple[int, ...]:
    if not isinstance(version, str) or _DOTTED_NUMBERS.fullmatch(version) is None:
        raise ValueError('not a dotted number: %r' % (version,))
    return tuple(int(part) for part in version.split('.'))


def feature_band(sdk_version: str) -> str:
    """'11.0.203' -> '11.0.200'; a prerelease suffix is ignored because the bot only reads GA SDKs."""
    match = _SDK_VERSION.fullmatch(sdk_version) if isinstance(sdk_version, str) else None
    if match is None:
        raise ValueError('not an SDK version: %r' % (sdk_version,))
    patch_hundreds = int(match.group(3)) // 100 * 100
    return '%s.%s.%d' % (int(match.group(1)), int(match.group(2)), patch_hundreds)


def channel_of(latest_net_version: str) -> Optional[str]:
    match = _CHANNEL.fullmatch(latest_net_version) if isinstance(latest_net_version, str) else None
    if match is None:
        return None
    return '%s.%s' % (match.group(1), match.group(2))


def workload_set_version(package_version: str) -> str:
    """The workload set package '11.100.1' is the workload set version '11.0.100.1'; revision 0 is left out."""
    match = _PACKAGE_VERSION.fullmatch(package_version) if isinstance(package_version, str) else None
    if match is None:
        raise ValueError('not a workload set package version: %r' % (package_version,))
    major, band_patch, revision, suffix = match.group(1), match.group(2), match.group(3), match.group(4) or ''
    core = '%s.0.%s' % (int(major), int(band_patch))
    if int(revision) != 0:
        core += '.%s' % int(revision)
    return core + suffix


def major_minor(version: str) -> str:
    numbers = numeric_key(version.split('-', 1)[0].split('+', 1)[0])
    minor = numbers[1] if len(numbers) > 1 else 0
    return '%d.%d' % (numbers[0], minor)


def major(version: str) -> int:
    return numeric_key(version.split('-', 1)[0].split('+', 1)[0])[0]


def parse_utc(text: str) -> datetime:
    """A timestamp without an offset counts as UTC, as NuGet and GitHub always send one."""
    match = _UTC_TIMESTAMP.fullmatch(text.strip()) if isinstance(text, str) else None
    if match is None:
        raise ValueError('not a timestamp: %r' % (text,))
    year, month, day, hour, minute, second = (int(match.group(index)) for index in range(1, 7))
    microseconds = int((match.group(7) or '0').ljust(7, '0')[:6])
    moment = datetime(year, month, day, hour, minute, second, microseconds, tzinfo=timezone.utc)
    offset = match.group(8)
    if offset and offset != 'Z':
        sign = 1 if offset[0] == '+' else -1
        shift = timedelta(hours=int(offset[1:3]), minutes=int(offset[4:6]))
        moment = moment - sign * shift
    return moment
