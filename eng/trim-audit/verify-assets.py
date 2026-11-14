#!/usr/bin/env python3
"""verify-assets.py <project.assets.json> <feed-dir> <packages-dir>

Proves that an audit build used the feed's nupkgs and no toolkit project: every CommunityToolkit.Maui library in the assets file must be
a package of the feed's version, restored into the audit's own packages folder from a nupkg that is byte-identical to the feed's.
Prints one line per check and exits 1 on the first kind of failure it finds (all failures are still printed)."""
import base64
import glob
import hashlib
import json
import os
import re
import sys

LIBRARIES = ('CommunityToolkit.Maui', 'CommunityToolkit.Maui.Core', 'CommunityToolkit.Maui.Camera',
             'CommunityToolkit.Maui.MediaElement')


def main(assets_path, feed, packages):
    assets = json.load(open(assets_path, encoding='utf-8'))
    failures = []
    feed_versions = {}
    for nupkg in glob.glob(os.path.join(feed, '*.nupkg')):
        match = re.match(r'(CommunityToolkit\.Maui(?:\.[A-Za-z]+)?)\.(\d+\.\d+\.\d+.*)\.nupkg$', os.path.basename(nupkg))
        if match and match.group(1) in LIBRARIES:
            feed_versions[match.group(1)] = (match.group(2), nupkg)

    seen = set()
    for key, library in assets['libraries'].items():
        name, version = key.split('/', 1)
        if name.startswith('CommunityToolkit.Maui.DeviceTests') or name not in LIBRARIES:
            if library.get('type') == 'project' and name.startswith('CommunityToolkit.Maui') and not name.startswith('CommunityToolkit.Maui.DeviceTests'):
                failures.append('project reference to toolkit code: %s' % key)
            continue
        seen.add(name)
        if library.get('type') != 'package':
            failures.append('%s is a %s, not a package' % (key, library.get('type')))
            continue
        expected = feed_versions.get(name)
        if expected is None or expected[0] != version:
            failures.append('%s: the feed has %s' % (key, expected[0] if expected else 'no such package'))
            continue
        restored = os.path.join(packages, name.lower(), version.lower(), '%s.%s.nupkg.sha512' % (name.lower(), version.lower()))
        if not os.path.exists(restored):
            failures.append('%s: not restored into %s' % (key, packages))
            continue
        feed_sha = base64.b64encode(hashlib.sha512(open(expected[1], 'rb').read()).digest()).decode()
        if open(restored, encoding='utf-8').read().strip() != feed_sha:
            failures.append('%s: the restored nupkg differs from the feed (stale packages folder? delete %s)' % (key, packages))
            continue
        print('OK %s package from the feed (sha512 matches)' % key)

    folders = [os.path.normpath(f) for f in assets.get('packageFolders', {})]
    if os.path.normpath(packages) not in folders:
        failures.append('packages folder %s is not the restore folder (%s)' % (packages, ', '.join(folders)))
    if 'CommunityToolkit.Maui' not in seen:
        failures.append('CommunityToolkit.Maui is not in the assets file')

    for failure in failures:
        print('FAIL ' + failure)
    print('ASSETS-CHECK=%s' % ('FAIL' if failures else 'OK'))
    return 1 if failures else 0


if __name__ == '__main__':
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    sys.exit(main(*sys.argv[1:]))
