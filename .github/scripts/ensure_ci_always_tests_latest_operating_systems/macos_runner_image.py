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
XCODE_APP_NAME = re.compile(r'^Xcode.*\.app$')
IMAGE_LINE = re.compile(r'^Image:\s*(.+?)\s*$', re.MULTILINE)
VERSION_LINE = re.compile(r'^Version:\s*(.+?)\s*$', re.MULTILINE)

RunCommand = Callable[[List[str]], Tuple[int, str]]


def run_command(argv: List[str]) -> Tuple[int, str]:
    try:
        completed = subprocess.run(argv, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, encoding='utf-8',
                                   errors='replace', timeout=COMMAND_TIMEOUT_SECONDS, check=False)
    except (OSError, subprocess.TimeoutExpired):
        return 127, ''
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


def list_xcode_apps(apps_dir: str = '/Applications') -> List[Dict[str, Any]]:
    """Only real folders count: a beta answers to Xcode_27.3.app through a symlink, and setup-xcode ignores symlinks too."""
    try:
        names = sorted(os.listdir(apps_dir))
    except OSError:
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


def list_simulator_runtimes(run: RunCommand) -> List[Dict[str, Any]]:
    code, output = run(['xcrun', 'simctl', 'list', 'runtimes', '-j'])
    if code != 0 or not output.strip():
        return []
    try:
        data = json.loads(output)
    except ValueError:
        return []
    runtimes = []
    for runtime in data.get('runtimes', []) if isinstance(data, dict) else []:
        runtimes.append({
            'platform': runtime.get('platform'),
            'version': runtime.get('version'),
            'name': runtime.get('name'),
            'identifier': runtime.get('identifier'),
            'available': bool(runtime.get('isAvailable')),
        })
    return runtimes


def list_dotnet_sdks(run: RunCommand) -> List[str]:
    code, output = run(['dotnet', '--list-sdks'])
    if code != 0:
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
    listing: Dict[str, Any] = {
        'status': 'ok',
        'label': label,
        'image': {'name': None, 'version': None},
        'xcode_apps': list_xcode_apps(apps_dir),
        'runtimes': list_simulator_runtimes(run),
        'dotnet_sdks': list_dotnet_sdks(run),
    }
    try:
        listing['image'] = read_runner_image(home, environ)
    except RunnerSoftwareError as error:
        listing['status'] = 'error'
        listing['error'] = str(error)
    return listing
