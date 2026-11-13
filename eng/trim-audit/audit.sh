#!/usr/bin/env bash
# Trim and NativeAOT audit of the toolkit's NuGet packages (macOS and Linux: Android, iOS simulator, Mac Catalyst; Windows: audit-win.ps1).
#
# Why: apps consume the packed libraries, not the projects. A library build can pass its analyzers and still hand an app trim or AOT
# warnings, because the trimmer and ILC see the whole app: callers, generic instantiations and MAUI's own annotations. So this builds the
# Sample against the nupkgs, with the trim settings an app can choose, and `audit.sh gate` fails when the toolkit adds a warning that is not allow-listed.
#
#   eng/trim-audit/audit.sh <android|ios|maccatalyst> <none|partial|full|rootlibs|aot|aotroot> [out-root]
#       Release build of the Sample against the nupkgs in eng/trim-audit/feed (or $CTM_TRIM_AUDIT_FEED) instead of the toolkit projects,
#       with the variant's trim settings (Directory.Build.targets), into <out-root>/<platform>-<variant>/:
#       build.binlog, build.log, warnings.log, warnings-summary.txt, warnings-unique.tsv, warnings-counts.json, assets-check.txt,
#       roots-check.txt (aotroot), linked-sizes.txt, app.txt (path of the .apk or .app), result.json.
#       Exit code: the build's, 3 when the assets check (or, for aotroot, the roots check) fails, 4 when warnings.log has IL warnings that
#       the parser could not read. 'none' is refused on iOS (exit 2): the iOS SDK requires PublishTrimmed=true, and an untrimmed iOS build
#       needs MtouchLink=None, which this leg does not run.
#       'aot' and 'aotroot' are NativeAOT and run `dotnet publish` (PublishAot needs it; iOS builds with _IsPublishing); the others run
#       `dotnet build`. aotroot roots the four toolkit assemblies so that ILC analyzes all toolkit code.
#   eng/trim-audit/audit.sh gate <leg-dir> [--github] [--max-annotations <n>] [--summary <file>] [--report-only]   (n: this gate's share)
#       the CI check on a finished leg (<out-root>/<platform>-<variant>): fails on a toolkit or toolkit-api warning that allow.tsv does
#       not list. Exit: 0 clean, 1 new warnings, 2 usage or a bad allow-list, 3 assets or roots check not OK, 4 build failed, result.json,
#       warnings.log or the warning list missing, or the parser could not read every IL line. --report-only: same report, exit 0 (not 2).
#   eng/trim-audit/audit.sh run <platform[,platform]> <variant> (--repo <source-repo> [--version <v>] | --feed <folder-of-nupkgs>)
#                               [--out-root <dir>] [--github] [--max-annotations <n>] [--summary <file>] [--report-only]
#       the one command a CI step runs: with --repo it packs that checkout first (into <out-root>/feed); with --feed it uses the nupkgs
#       a CI pack step left in <folder> (the four libraries, one version each, any other files are ignored). Then it builds the leg and
#       gates it. A platform list (`run android,ios aotroot --feed ...`) packs once, then builds and gates every platform even when an
#       earlier one fails, and exits with the first non-zero code. Exit: the gate's (0, 1, 3, 4 above), 2 for a usage error, 5 when packing
#       failed. --github prints ::error annotations. GitHub shows only 10 error annotations per step, so --max-annotations <n> (default
#       10) is one budget for the whole call: each gate gets an equal share of what is left (a leg that needs fewer leaves more for the
#       next), so the failure of a later leg is never hidden behind the warnings of an earlier one. --summary appends a Markdown report
#       (give it $GITHUB_STEP_SUMMARY; it lists every leg); a failure before the gate runs is annotated and summarized too.
#   eng/trim-audit/audit.sh pack <source-repo> [version] [feed]
#       packs the four libraries of <source-repo> (any checkout of the toolkit; Release; its bin and obj are written to) into the feed
#       (default $CTM_TRIM_AUDIT_FEED or eng/trim-audit/feed) as version [version] (default $CTM_TRIM_AUDIT_VERSION or 99.0.0-trim1),
#       and writes feed/SHA256SUMS. Use one feed, one version and one out-root per source tree so that two trees can never mix.
#   eng/trim-audit/audit.sh env [out-root]
#       writes the NuGet config and prints the variables that put any other build (eng/devices/run.ps1) on the same packages:
#       eval "$(eng/trim-audit/audit.sh env)"; CtmTrimAuditMode=full pwsh eng/devices/run.ps1 ...
#
# out-root: $CTM_TRIM_AUDIT_OUT, else <repo>/artifacts/trim-audit (ignored by git). The NuGet config and the packages folder are shared
# by all variants under <out-root>/nuget. Env: CTM_TRIM_AUDIT_MAUI_VERSION (default: MauiPackageVersion of this repo's
# Directory.Build.props), ANDROID_HOME, JAVA_HOME.
set -uo pipefail
here=$(cd "$(dirname "$0")" && pwd); repo=$(cd "$here/../.." && pwd)
sample=$repo/samples/CommunityToolkit.Maui.Sample/CommunityToolkit.Maui.Sample.csproj
feed=${CTM_TRIM_AUDIT_FEED:-$here/feed}

usage() { awk 'NR == 1 { next } /^#/ { print; next } { exit }' "$0" >&2; exit 2; }
[ $# -ge 1 ] || usage
if command -v python3 > /dev/null 2>&1; then python=python3; else python=python; fi
sha256() { if command -v shasum > /dev/null 2>&1; then shasum -a 256 "$@"; else sha256sum "$@"; fi; }
file_size() { wc -c < "$1" | tr -d ' '; }

# $feed holds one nupkg per library; Camera and MediaElement have their own version on release tags.
version_of() {
  local id=$1 found=() file name
  for file in "$feed/$id".[0-9]*.nupkg; do [ -e "$file" ] && found+=("$file"); done
  [ ${#found[@]} -eq 1 ] || { echo "expected exactly one $id nupkg in $feed, found ${#found[@]}" >&2; return 1; }
  name=$(basename "${found[0]}"); name=${name#"$id."}; echo "${name%.nupkg}"
}
resolve_versions() {
  version_maui=$(version_of CommunityToolkit.Maui) && version_core=$(version_of CommunityToolkit.Maui.Core) &&
  version_camera=$(version_of CommunityToolkit.Maui.Camera) && version_media=$(version_of CommunityToolkit.Maui.MediaElement) || exit 2
  version=$version_core
}

if [ "$1" = run ]; then
  shift
  [ $# -ge 2 ] || usage
  platform_list=$1; variant=$2; shift 2
  source_repo=""; feed_folder=""; pack_version=""; run_root=${CTM_TRIM_AUDIT_OUT:-$repo/artifacts/trim-audit}; gate_flags=(); report_only=false
  github=false; summary_file=""
  annotation_budget=10; annotations_used=0
  # Annotate a usage error even when --github comes after the bad argument.
  for argument in "$@"; do [ "$argument" = --github ] && github=true; done
  run_error() {
    echo "$1" >&2
    if $github; then
      escaped=${1//%/%25}; escaped=${escaped//$'\r'/%0D}; escaped=${escaped//$'\n'/%0A}
      echo "::error::$escaped"
      annotations_used=$((annotations_used + 1))
    fi
    [ -n "$summary_file" ] && printf '### Trim and AOT gate: FAIL\n\n%s\n' "$1" >> "$summary_file"
    return 0
  }
  usage_error() { run_error "$1"; usage; }
  while [ $# -gt 0 ]; do
    case $1 in
      --repo) [ $# -ge 2 ] || usage_error "--repo needs a value"; source_repo=$2; shift 2 ;;
      --feed) [ $# -ge 2 ] || usage_error "--feed needs a value"; feed_folder=$2; shift 2 ;;
      --version) [ $# -ge 2 ] || usage_error "--version needs a value"; pack_version=$2; shift 2 ;;
      --out-root) [ $# -ge 2 ] || usage_error "--out-root needs a value"; run_root=$2; shift 2 ;;
      --summary) [ $# -ge 2 ] || usage_error "--summary needs a value"; summary_file=$2; gate_flags+=(--summary "$2"); shift 2 ;;
      --max-annotations) [ $# -ge 2 ] || usage_error "--max-annotations needs a value"
                         case $2 in ''|*[!0-9]*) usage_error "--max-annotations needs a number, got '$2'" ;; esac
                         annotation_budget=$2; shift 2 ;;
      --github) github=true; gate_flags+=(--github); shift ;;
      --report-only) gate_flags+=(--report-only); report_only=true; shift ;;
      *) usage_error "unknown argument '$1'" ;;
    esac
  done
  IFS=, read -ra platforms <<< "$platform_list"
  [ ${#platforms[@]} -ge 1 ] || usage_error "give at least one platform"
  for platform in "${platforms[@]}"; do case $platform in android|ios|maccatalyst) ;; *) usage_error "unknown platform '$platform' (android, ios or maccatalyst)" ;; esac; done
  case $variant in none|partial|full|rootlibs|aot|aotroot) ;; *) usage_error "unknown variant '$variant' (none, partial, full, rootlibs, aot or aotroot)" ;; esac
  # MSBuild resolves the out-root against the Sample's folder, so a relative path must be made absolute here.
  mkdir -p "$run_root" && run_root=$(cd "$run_root" && pwd) || { run_error "cannot create the out-root $run_root"; exit 2; }
  if [ -n "$source_repo" ] && [ -z "$feed_folder" ]; then run_feed=$run_root/feed
  elif [ -z "$source_repo" ] && [ -n "$feed_folder" ] && [ -z "$pack_version" ]; then
    run_feed=$(cd "$feed_folder" 2> /dev/null && pwd) || { run_error "no such feed folder: $feed_folder"; exit 2; }
  else usage_error "give exactly one of --repo or --feed; --version only goes with --repo"; fi
  if [ -n "$source_repo" ]; then
    bash "$here/audit.sh" pack "$source_repo" "${pack_version:-${CTM_TRIM_AUDIT_VERSION:-99.0.0-audit}}" "$run_feed" || { run_error "packing $source_repo failed (exit 5)"; exit 5; }
  fi
  exit_code=0; leg_index=0
  # With --report-only the last gate's overflow ::warning comes on top of the budget, so a step can print budget + 1 warnings
  # (more only when the budget is smaller than the number of legs).
  annotation_level=error; $report_only && annotation_level=warning
  for platform in "${platforms[@]}"; do
    legs_left=$(( ${#platforms[@]} - leg_index )); leg_index=$((leg_index + 1))
    CTM_TRIM_AUDIT_FEED=$run_feed bash "$here/audit.sh" "$platform" "$variant" "$run_root"
    leg_rc=$?
    if [ $leg_rc -eq 2 ]; then
      run_error "leg $platform-$variant could not start (exit 2: usage, feed or target framework error; see the step log)"
      [ $exit_code -eq 0 ] && exit_code=2
      continue
    fi
    share=$(( (annotation_budget - annotations_used) / legs_left )); [ $share -lt 0 ] && share=0
    gate_output=$run_root/$platform-$variant/gate-output.txt
    bash "$here/audit.sh" gate "$run_root/$platform-$variant" --max-annotations "$share" ${gate_flags[@]+"${gate_flags[@]}"} > "$gate_output" 2>&1
    gate_rc=$?
    cat "$gate_output"
    annotations_used=$(( annotations_used + $(grep -c "^::$annotation_level" "$gate_output") ))
    [ $gate_rc -eq 0 ] && [ $leg_rc -ne 0 ] && ! $report_only && gate_rc=4
    [ $exit_code -eq 0 ] && exit_code=$gate_rc
  done
  exit $exit_code
fi
if [ "$1" = gate ]; then
  [ $# -ge 2 ] || usage
  leg=$2; shift 2
  exec "$python" -I "$here/ilwarn.py" gate "$leg/warnings-unique.tsv" --allow "$here/allow.tsv" --log "$leg/warnings.log" \
    --assets-check "$leg/assets-check.txt" --result "$leg/result.json" "$@"
fi
if [ "$1" = pack ]; then
  [ $# -ge 2 ] || usage
  source_repo=$(cd "$2" && pwd) || exit 2
  version=${3:-${CTM_TRIM_AUDIT_VERSION:-99.0.0-trim1}}
  feed=${4:-$feed}
  [ -f "$source_repo/src/CommunityToolkit.Maui.Core/CommunityToolkit.Maui.Core.csproj" ] || { echo "$source_repo is not a toolkit checkout" >&2; exit 2; }
  mkdir -p "$feed"; feed=$(cd "$feed" && pwd); rm -f "$feed"/*.nupkg "$feed"/*.snupkg "$feed"/SHA256SUMS
  # The libraries pack the analyzers and source generators from their Release output, as CI builds them first.
  for p in CommunityToolkit.Maui.Analyzers CommunityToolkit.Maui.Analyzers.CodeFixes CommunityToolkit.Maui.Camera.Analyzers \
           CommunityToolkit.Maui.Camera.Analyzers.CodeFixes CommunityToolkit.Maui.MediaElement.Analyzers \
           CommunityToolkit.Maui.MediaElement.Analyzers.CodeFixes CommunityToolkit.Maui.SourceGenerators \
           CommunityToolkit.Maui.MediaElement.SourceGenerators; do
    dotnet build "$source_repo/src/$p/$p.csproj" -c Release -v q -nologo || exit 1
  done
  for p in CommunityToolkit.Maui.Core CommunityToolkit.Maui CommunityToolkit.Maui.Camera CommunityToolkit.Maui.MediaElement; do
    dotnet pack "$source_repo/src/$p/$p.csproj" -c Release "-p:Version=$version" "-p:PackageVersion=$version" -o "$feed" -nologo || exit 1
  done
  rm -f "$feed"/*.snupkg
  (cd "$feed" && sha256 ./*.nupkg | sed 's#\./##' > SHA256SUMS && cat SHA256SUMS)
  echo "packed $(git -C "$source_repo" rev-parse HEAD 2>/dev/null) as $version into $feed" | tee "$feed/SOURCE.txt"
  exit 0
fi
if [ "$1" = env ]; then
  platform=env; variant=; root=${2:-${CTM_TRIM_AUDIT_OUT:-$repo/artifacts/trim-audit}}
  mkdir -p "$root" && root=$(cd "$root" && pwd) || { echo "cannot create the out-root $root" >&2; exit 2; }
else
  [ $# -ge 2 ] || usage
  platform=$1; variant=$2; root=${3:-${CTM_TRIM_AUDIT_OUT:-$repo/artifacts/trim-audit}}
  # MSBuild resolves CtmTrimAuditOutput against the Sample's folder, so a relative out-root must be made absolute here.
  mkdir -p "$root" && root=$(cd "$root" && pwd) || { echo "cannot create the out-root $root" >&2; exit 2; }
  case $platform in android|ios|maccatalyst) ;; *) usage ;; esac
  case $variant in none|partial|full|rootlibs|aot|aotroot) ;; *) usage ;; esac
  if [ "$platform-$variant" = ios-none ]; then echo "iOS requires PublishTrimmed=true; an untrimmed iOS build needs MtouchLink=None, which this leg does not run" >&2; exit 2; fi
fi

resolve_versions
nuget=$root/nuget; packages=$nuget/packages; config=$nuget/NuGet.config
mkdir -p "$packages"

# The user's package cache serves as a read-only feed for everything that is not the toolkit (MAUI, AndroidX, xunit), so nothing has to
# be downloaded again; the mapping lets only the audit feed serve the toolkit ids, so a cached package of the same version is never used.
{
  echo '<?xml version="1.0" encoding="utf-8"?>'
  echo '<configuration>'
  echo "  <config><add key=\"globalPackagesFolder\" value=\"$packages\" /></config>"
  echo '  <packageSources>'
  echo '    <clear />'
  echo "    <add key=\"ctm-audit-feed\" value=\"$feed\" />"
  [ -d "$HOME/.nuget/packages" ] && echo "    <add key=\"usercache\" value=\"$HOME/.nuget/packages\" />"
  echo '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />'
  echo '    <add key="dotnet-eng" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json" protocolVersion="3" />'
  echo '  </packageSources>'
  echo '  <fallbackPackageFolders><clear /></fallbackPackageFolders>'
  echo '  <packageSourceMapping>'
  echo '    <packageSource key="ctm-audit-feed">'
  for id in CommunityToolkit.Maui CommunityToolkit.Maui.Core CommunityToolkit.Maui.Camera CommunityToolkit.Maui.MediaElement; do
    echo "      <package pattern=\"$id\" />"
  done
  echo '    </packageSource>'
  [ -d "$HOME/.nuget/packages" ] && echo '    <packageSource key="usercache"><package pattern="*" /></packageSource>'
  echo '    <packageSource key="nuget.org"><package pattern="*" /></packageSource>'
  echo '    <packageSource key="dotnet-eng"><package pattern="Microsoft.DotNet.XHarness.*" /></packageSource>'
  echo '  </packageSourceMapping>'
  echo '</configuration>'
} > "$config"

props=(
  "-p:DirectoryBuildPropsPath=$here/Directory.Build.props"
  "-p:DirectoryBuildTargetsPath=$here/Directory.Build.targets"
  "-p:CtmTrimAuditPackageVersion=$version"
  "-p:CtmTrimAuditVersionMaui=$version_maui" "-p:CtmTrimAuditVersionCore=$version_core"
  "-p:CtmTrimAuditVersionCamera=$version_camera" "-p:CtmTrimAuditVersionMediaElement=$version_media"
  "-p:CtmTrimAuditNuGetConfig=$config"
  "-p:CtmTrimAuditPackagesPath=$packages"
)

if [ "$platform" = env ]; then
  # MSBuild reads environment variables as properties, so these reach every project of a build that the caller cannot pass -p: to.
  echo "export DirectoryBuildPropsPath='$here/Directory.Build.props' DirectoryBuildTargetsPath='$here/Directory.Build.targets'"
  echo "export CtmTrimAuditPackageVersion='$version' CtmTrimAuditNuGetConfig='$config' CtmTrimAuditPackagesPath='$packages'"
  echo "export CtmTrimAuditVersionMaui='$version_maui' CtmTrimAuditVersionCore='$version_core' CtmTrimAuditVersionCamera='$version_camera' CtmTrimAuditVersionMediaElement='$version_media'"
  echo "# then set CtmTrimAuditMode=none|partial|full (full roots the test harness in the device-test app)"
  exit 0
fi

export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDTERMINALLOGGER=off MSBUILDDISABLENODEREUSE=1
out=$root/$platform-$variant
rm -rf "$out"; mkdir -p "$out"

tfm=$(dotnet msbuild "$sample" -nologo -getProperty:TargetFrameworks 2>/dev/null | tr ';' '\n' | grep -E "^net[0-9.]+-$platform\$" | head -1)
[ -n "$tfm" ] || { echo "the Sample has no $platform target framework" >&2; exit 2; }
case $platform in
  android) rid=android-arm64
           extra=(-p:AndroidPackageFormat=apk)
           [ -n "${ANDROID_HOME:-}" ] && extra+=("-p:AndroidSdkDirectory=$ANDROID_HOME")
           [ -n "${JAVA_HOME:-}" ] && extra+=("-p:JavaSdkDirectory=$JAVA_HOME") ;;
  ios) rid=iossimulator-arm64; extra=() ;;
  maccatalyst) rid=maccatalyst-arm64; extra=() ;;
esac
maui=${CTM_TRIM_AUDIT_MAUI_VERSION:-$(sed -n 's#.*<MauiPackageVersion>\(.*\)</MauiPackageVersion>.*#\1#p' "$repo/Directory.Build.props" | head -1)}

start=$(date +%s)
echo "trim audit: $platform $variant $tfm $rid packages $version (feed $feed) MAUI $maui -> $out"
verb=build
# PublishAot is a global property, not set in Directory.Build.targets: the Apple and Android SDKs decide NativeAOT in their props files.
# The Apple publish targets refuse a simulator RID, but NativeAOT compiles for a simulator in a build that sets _IsPublishing, which
# gives the same ILC warnings without needing a signing identity.
if [ "$variant" = aot ] || [ "$variant" = aotroot ]; then
  extra+=(-p:PublishAot=true)
  case $platform in
    ios) extra+=(-p:_IsPublishing=true) ;;
    *) verb=publish ;;
  esac
fi
dotnet $verb "$sample" -c Release -f "$tfm" "-p:RuntimeIdentifier=$rid" \
  "${props[@]}" "-p:CtmTrimAuditMode=$variant" "-p:CtmTrimAuditOutput=$out" "-p:CtmTrimAuditMauiVersion=$maui" -p:SuppressTrimAnalysisWarnings=false ${extra[@]+"${extra[@]}"} \
  -nodeReuse:false "-bl:$out/build.binlog" "-flp1:logfile=$out/warnings.log;warningsonly" "-flp2:logfile=$out/build.log;verbosity=normal" \
  "-clp:Summary;ErrorsOnly"
rc=$?
end=$(date +%s)

assets_rc=0
"$python" -I "$here/verify-assets.py" "$out/obj/project.assets.json" "$feed" "$packages" > "$out/assets-check.txt" 2>&1 || assets_rc=$?
cat "$out/assets-check.txt"
roots_rc=0
if [ "$variant" = aotroot ]; then
  # Without --root lines, an aotroot leg would silently be a plain aot leg and miss the toolkit code the Sample does not reach.
  rsp=$(find "$out/obj" -name '*ilc.rsp' 2> /dev/null | head -1)
  {
    echo "ilc.rsp: ${rsp:-not found}"
    missing=0
    for id in CommunityToolkit.Maui CommunityToolkit.Maui.Core CommunityToolkit.Maui.Camera CommunityToolkit.Maui.MediaElement; do
      if [ -n "$rsp" ] && grep -Fxq -e "--root:$id" -e "--root:$id.dll" "$rsp"; then echo "root $id: yes"; else echo "root $id: MISSING"; missing=1; fi
    done
    [ $missing -eq 0 ] && echo ROOTS-CHECK=OK || echo ROOTS-CHECK=FAIL
  } > "$out/roots-check.txt"
  cat "$out/roots-check.txt"
  grep -q 'ROOTS-CHECK=OK' "$out/roots-check.txt" || roots_rc=3
fi
parse_rc=0
"$python" -I "$here/ilwarn.py" summarize "$out/warnings.log" "$out" "$packages" > /dev/null 2>"$out/summarize.err" || parse_rc=$?
head -40 "$out/warnings-summary.txt" 2>/dev/null

app=""
case $platform in
  android) app=$(find "$out/bin" -name '*-Signed.apk' 2>/dev/null | head -1)
           [ -n "$app" ] || app=$(find "$out/bin" -name '*.apk' 2>/dev/null | head -1) ;;
  *) app=$(find "$out/bin" -maxdepth 5 -name '*.app' -type d 2>/dev/null | head -1) ;;
esac
echo "$app" > "$out/app.txt"

# Sizes show whether a leg trimmed at all: an untrimmed toolkit DLL equals the package's, so a leg that silently trimmed nothing is a bad baseline.
{
  echo "# assembly | package bytes | trimmed bytes (none: not trimmed)"
  for id in CommunityToolkit.Maui CommunityToolkit.Maui.Core CommunityToolkit.Maui.Camera CommunityToolkit.Maui.MediaElement; do
    lower=$(echo "$id" | LC_ALL=C tr '[:upper:]' '[:lower:]')
    case $id in
      CommunityToolkit.Maui) id_version=$version_maui ;;
      CommunityToolkit.Maui.Core) id_version=$version_core ;;
      CommunityToolkit.Maui.Camera) id_version=$version_camera ;;
      *) id_version=$version_media ;;
    esac
    original=$(find "$packages/$lower/$id_version/lib" -path "*$platform*" -name "$id.dll" 2>/dev/null | head -1)
    linked=$(find "$out/obj" -path '*link*' -name "$id.dll" 2>/dev/null | head -1)
    original_size=""; [ -n "$original" ] && original_size=$(file_size "$original")
    linked_size=none; [ -n "$linked" ] && linked_size=$(file_size "$linked")
    echo "$id | $original_size | $linked_size"
  done
  if [ -n "$app" ] && [ -e "$app" ]; then
    echo "# app: $app"
    echo "app_kib | $(du -sk "$app" | cut -f1)"
    if [ -d "$app" ]; then
      # The shipped managed assemblies tell whether the build trimmed: a trimmed Microsoft.Maui.Controls.dll is much smaller than the package's.
      for dll in CommunityToolkit.Maui.dll CommunityToolkit.Maui.Core.dll Microsoft.Maui.Controls.dll Microsoft.Maui.dll System.Private.CoreLib.dll; do
        shipped=$(find "$app" -name "$dll" 2>/dev/null | head -1)
        echo "shipped $dll | $( [ -n "$shipped" ] && file_size "$shipped" || echo none )"
      done
      echo "shipped_managed_dll_bytes | $(find "$app" -name '*.dll' -exec wc -c {} + 2>/dev/null | tail -1 | awk '{print $1}')"
    fi
  fi
} > "$out/linked-sizes.txt"

"$python" -I - "$out/result.json" "$platform" "$variant" "$tfm" "$rid" "$version" "$maui" "$rc" "$assets_rc" "$start" "$end" "$app" \
  "$(git -C "$repo" rev-parse HEAD)" <<'PY'
import json, os, sys
keys = ['platform', 'variant', 'tfm', 'rid', 'packages', 'maui', 'build_exit', 'assets_exit', 'start', 'end', 'app', 'head']
d = dict(zip(keys, sys.argv[2:]))
for k in ('build_exit', 'assets_exit', 'start', 'end'):
    d[k] = int(d[k])
d['minutes'] = round((d['end'] - d['start']) / 60, 1)
out = os.path.dirname(sys.argv[1])
tsv = os.path.join(out, 'warnings-unique.tsv')
if os.path.exists(tsv):
    rows = [l.split('\t') for l in open(tsv, encoding='utf-8') if l.strip()]
    d['distinct_warnings'] = len(rows)
    for bucket in ('toolkit', 'toolkit-api', 'sample', 'maui', 'other'):
        d['warnings_' + bucket.replace('-', '_')] = sum(1 for r in rows if r[0] == bucket)
    d['warnings_by_code'] = {}
    for r in rows:
        d['warnings_by_code'][r[2]] = d['warnings_by_code'].get(r[2], 0) + 1
json.dump(d, open(sys.argv[1], 'w'), indent=1)
print(json.dumps(d))
PY
[ $rc -ne 0 ] && exit $rc
[ $assets_rc -ne 0 ] && exit 3
[ $roots_rc -ne 0 ] && exit 3
[ $parse_rc -ne 0 ] && exit 4
exit 0
