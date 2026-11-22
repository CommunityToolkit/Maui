"""Lists what a macOS runner image has installed, discovering Xcode the way maxim-lobanov/setup-xcode does so the bot and CI agree."""
from __future__ import annotations

import json
import os
import plistlib
import re
import subprocess
from typing import Any, Callable, Dict, List, Mapping, Optional, Tuple

from .errors import RunnerSoftwareError

COMMAND_TIMEOUT_SECONDS = 120
# A cold CoreSimulator service can need minutes to answer its first request on a fresh runner.
RETRY_TIMEOUT_SECONDS = 240
COMMAND_COULD_NOT_START_CODE = 127
# The exit code of the `timeout` tool, so a person who reads it elsewhere already knows it.
COMMAND_TIMED_OUT_CODE = 124
XCODE_APP_NAME = re.compile(r'^Xcode.*\.app$')
IMAGE_LINE = re.compile(r'^Image:\s*(.+?)\s*$', re.MULTILINE)
VERSION_LINE = re.compile(r'^Version:\s*(.+?)\s*$', re.MULTILINE)

RunCommand = Callable[[List[str], int], Tuple[int, str]]


def run_command(argv: List[str], timeout_seconds: int = COMMAND_TIMEOUT_SECONDS) -> Tuple[int, str]:
    try:
        completed = subprocess.run(argv, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, encoding='utf-8',
                                   errors='replace', timeout=timeout_seconds, check=False)
    except subprocess.TimeoutExpired:
        return COMMAND_TIMED_OUT_CODE, ''
    except OSError:
        return COMMAND_COULD_NOT_START_CODE, ''
    return completed.returncode, completed.stdout


def _read_plist(path: str) -> Optional[Dict[str, Any]]:
    try:
        with open(path, 'rb') as file:
            data = plistlib.load(file)
    except (OSError, ValueError, plistlib.InvalidFileException):
        return None
    return data if isinstance(data, dict) else None


def _symlinks_pointing_at(apps_dir: str, folder: str) -> List[str]:
    names = []
    for name in sorted(os.listdir(apps_dir)):
        path = os.path.join(apps_dir, name)
        if not os.path.islink(path):
            continue
        if os.path.basename(os.path.realpath(path)) == folder:
            names.append(name)
    return names


def list_xcode_apps(apps_dir: str = '/Applications', problems: Optional[List[str]] = None) -> List[Dict[str, Any]]:
    """Only real folders count: a beta answers to Xcode_27.3.app through a symlink, and setup-xcode ignores symlinks too."""
    try:
        names = sorted(os.listdir(apps_dir))
    except OSError as error:
        _report(problems, 'could not list %s: %s' % (apps_dir, error))
        return []
    apps = []
    for folder in names:
        path = os.path.join(apps_dir, folder)
        if not XCODE_APP_NAME.match(folder) or os.path.islink(path) or not os.path.isdir(path):
            continue
        version_plist = _read_plist(os.path.join(path, 'Contents', 'version.plist')) or {}
        license_plist = _read_plist(os.path.join(path, 'Contents', 'Resources', 'LicenseInfo.plist')) or {}
        apps.append({
            'folder': folder,
            'version': version_plist.get('CFBundleShortVersionString'),
            'build': version_plist.get('ProductBuildVersion'),
            'license_type': license_plist.get('licenseType'),
            'symlinks': _symlinks_pointing_at(apps_dir, folder),
        })
    return apps


def _report(problems: Optional[List[str]], message: str) -> None:
    """An empty list from a failed command would look like "nothing installed" and drop legs the bot should keep."""
    if problems is not None:
        problems.append(message)


def _run_retrying_a_timeout(run: RunCommand, command: List[str]) -> Tuple[int, str, List[int]]:
    """One slow first answer must not freeze a night; other failures repeat the same way, so they are not retried."""
    code, output = run(command, COMMAND_TIMEOUT_SECONDS)
    timeouts = []
    if code == COMMAND_TIMED_OUT_CODE:
        timeouts.append(COMMAND_TIMEOUT_SECONDS)
        code, output = run(command, RETRY_TIMEOUT_SECONDS)
        if code == COMMAND_TIMED_OUT_CODE:
            timeouts.append(RETRY_TIMEOUT_SECONDS)
    return code, output, timeouts


def _command_failure(command: List[str], code: int, timeouts: Optional[List[int]] = None) -> str:
    text = ' '.join(command)
    if code == COMMAND_TIMED_OUT_CODE:
        seconds = timeouts or [COMMAND_TIMEOUT_SECONDS]
        return '`%s` timed out after %s' % (text, ' and again after '.join('%d s' % value for value in seconds))
    if code == COMMAND_COULD_NOT_START_CODE:
        return '`%s` could not be started' % text
    return '`%s` failed with exit code %d' % (text, code)


def list_simulator_runtimes(run: RunCommand, problems: Optional[List[str]] = None) -> List[Dict[str, Any]]:
    command = ['xcrun', 'simctl', 'list', 'runtimes', '-j']
    code, output, timeouts = _run_retrying_a_timeout(run, command)
    if code != 0:
        _report(problems, _command_failure(command, code, timeouts))
        return []
    try:
        data = json.loads(output)
    except ValueError:
        _report(problems, '`%s` printed text that is not JSON' % ' '.join(command))
        return []
    if not isinstance(data, dict) or not isinstance(data.get('runtimes'), list):
        _report(problems, '`%s` printed JSON without a `runtimes` list' % ' '.join(command))
        return []
    runtimes = []
    for runtime in data['runtimes']:
        runtimes.append({
            'platform': runtime.get('platform'),
            'version': runtime.get('version'),
            'name': runtime.get('name'),
            'identifier': runtime.get('identifier'),
            'available': bool(runtime.get('isAvailable')),
        })
    return runtimes


def list_dotnet_sdks(run: RunCommand, problems: Optional[List[str]] = None) -> List[str]:
    command = ['dotnet', '--list-sdks']
    code, output, timeouts = _run_retrying_a_timeout(run, command)
    if code != 0:
        _report(problems, _command_failure(command, code, timeouts))
        return []
    return [line.split()[0] for line in output.splitlines() if line.strip()]


def read_runner_image(home: str, environ: Mapping[str, str]) -> Dict[str, str]:
    """The image name is the key of the rollout state, so only imagedata.json can give it."""
    imagedata_path = os.path.join(home, 'imagedata.json')
    try:
        with open(imagedata_path, 'r', encoding='utf-8') as file:
            groups = json.load(file)
    except (OSError, ValueError):
        groups = None
    if isinstance(groups, list):
        for group in groups:
            if not isinstance(group, dict) or group.get('group') != 'Runner Image':
                continue
            detail = str(group.get('detail', ''))
            name = IMAGE_LINE.search(detail)
            version = VERSION_LINE.search(detail)
            if name and version:
                return {'name': name.group(1), 'version': version.group(1)}
    environment_version = environ.get('ImageVersion')
    if environment_version:
        return {'name': environ.get('ImageOS', ''), 'version': environment_version}
    raise RunnerSoftwareError(
        'no runner image information: %s is missing or has no "Runner Image" group, and ImageVersion is not set'
        % imagedata_path)


def inspect_macos_runner_image(label: str, run: RunCommand = run_command, apps_dir: str = '/Applications',
                               home: Optional[str] = None,
                               environ: Optional[Mapping[str, str]] = None) -> Dict[str, Any]:
    """Lists everything even when the image information is missing, so a local run still shows what it found."""
    home = home if home is not None else os.path.expanduser('~')
    environ = environ if environ is not None else os.environ
    problems: List[str] = []
    listing: Dict[str, Any] = {
        'status': 'ok',
        'label': label,
        'image': {'name': None, 'version': None},
        'xcode_apps': list_xcode_apps(apps_dir, problems),
        'runtimes': list_simulator_runtimes(run, problems),
        'dotnet_sdks': list_dotnet_sdks(run, problems),
    }
    try:
        listing['image'] = read_runner_image(home, environ)
    except RunnerSoftwareError as error:
        problems.append(str(error))
    if problems:
        listing['status'] = 'error'
        listing['error'] = '; '.join(problems)
    return listing
