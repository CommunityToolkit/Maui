#!/usr/bin/env bash
# Runs the trim gate on a result of audit-win.ps1 (the windows-<variant>.zip or its unzipped folder) on any machine with Python 3.
#
#   eng/trim-audit/gate-windows-result.sh <windows-variant.zip | folder> [--github] [--summary <file>] [--report-only]
#
# Why: a Windows machine without Python still builds and writes the result, but cannot parse the warnings or judge them. A zip is unpacked
# next to itself into <zip without .zip>/ . Then this parses warnings.log (the zip carries the toolkit DLLs that name each warning's assembly)
# and runs the gate with eng/trim-audit/allow.tsv. The assets check was done by audit-win.ps1 itself (assets-check.txt).
# Exit code: the gate's (ilwarn.py gate: 0, 1, 3, 4), 2 for a usage error.
set -uo pipefail
here=$(cd "$(dirname "$0")" && pwd)
if command -v python3 > /dev/null 2>&1; then python=python3; else python=python; fi
command -v "$python" > /dev/null 2>&1 || { echo "needs Python 3 (python3 or python on PATH)" >&2; exit 2; }
[ $# -ge 1 ] || { awk 'NR == 1 { next } /^#/ { print; next } { exit }' "$0" >&2; exit 2; }
target=$1; shift
if [ -f "$target" ]; then
  folder=${target%.zip}
  [ "$folder" != "$target" ] || { echo "$target is not a .zip" >&2; exit 2; }
  rm -rf "$folder" && mkdir -p "$folder" && "$python" -I - "$target" "$folder" <<'PY' || { echo "cannot unzip $target" >&2; exit 2; }
import os, sys, zipfile
# Windows PowerShell 5.1 writes backslash entry names, which unzip only extracts with a warning (exit 1).
target, folder = sys.argv[1], os.path.realpath(sys.argv[2])
with zipfile.ZipFile(target) as archive:
    for entry in archive.infolist():
        name = entry.filename.replace('\\', '/')
        destination = os.path.realpath(os.path.join(folder, name))
        if name.startswith('/') or os.path.commonpath([folder, destination]) != folder:
            sys.exit('refusing the entry %r: it leaves the target folder' % entry.filename)
        if name.endswith('/'):
            os.makedirs(destination, exist_ok=True)
            continue
        os.makedirs(os.path.dirname(destination), exist_ok=True)
        with archive.open(entry) as source, open(destination, 'wb') as out:
            out.write(source.read())
PY
elif [ -d "$target" ]; then
  folder=${target%/}
else
  echo "no such zip or folder: $target" >&2; exit 2
fi
[ -f "$folder/warnings.log" ] || { echo "$folder has no warnings.log: not a result of audit-win.ps1" >&2; exit 2; }
"$python" -I "$here/ilwarn.py" summarize "$folder/warnings.log" "$folder" "$folder/toolkit-dlls" > /dev/null || { echo "ilwarn.py summarize failed" >&2; exit 4; }
"$python" -I "$here/ilwarn.py" gate "$folder/warnings-unique.tsv" --allow "$here/allow.tsv" --log "$folder/warnings.log" \
  --assets-check "$folder/assets-check.txt" --result "$folder/result.json" "$@" | tee "$folder/gate-output.txt"
gate_rc=${PIPESTATUS[0]}
echo "GATE-EXIT=$gate_rc ($folder)"
exit "$gate_rc"
