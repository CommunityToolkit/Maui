#!/usr/bin/env bash
# Usage: check-trimmed-device-test-app.sh full|nativeaot
# Run after an eng/devices buildOnly with /p:CtmDeviceTestsTrimming=<mode>. The build already fails when the SDK would not trim or compile
# with NativeAOT; this checks the outputs too, so an app that skipped the trimmer or ILC cannot pass the tests as a trimmed one.
set -euo pipefail

mode=${1:?full or nativeaot}
app="$(cd "$(dirname "$0")/../.." && pwd)/src/CommunityToolkit.Maui.DeviceTests"
obj="$app/obj"

fail() {
    echo "::error::$1"
    exit 1
}

case "$mode" in
    full|nativeaot) ;;
    *) fail "Unknown mode '$mode': use full or nativeaot." ;;
esac

# Android's default Release build also has a linked/ folder (TrimMode partial), so output alone cannot tell a full build from a default one.
markers=$(find "$obj" -name ctm-device-tests-trimming.txt -exec grep -lx -- "$mode" {} + 2>/dev/null || true)
[ -n "$markers" ] || fail "No ctm-device-tests-trimming.txt with '$mode' under $obj: the build did not use /p:CtmDeviceTestsTrimming=$mode."

case "$mode" in
    nativeaot)
        rsps=$(find "$obj" -name '*.ilc.rsp' 2>/dev/null || true)
        [ -n "$rsps" ] || fail "No ILC response file under $obj: the device-test app was not compiled with NativeAOT."
        while IFS= read -r rsp; do
            object=${rsp%.ilc.rsp}.o
            echo "$rsp"
            grep -E '^--(targetos|targetarch):' "$rsp" || fail "$rsp names no target OS or architecture."
            # Without its roots, xunit finds no tests in a NativeAOT app and the run would end with nothing to compare.
            grep -qx -- '--root:xunit.core' "$rsp" || fail "$rsp has no --root:xunit.core: the TrimmerRootAssembly items of CtmDeviceTestsTrimming did not reach ILC."
            [ -s "$object" ] || fail "ILC wrote no $object."
            ls -l "$object"
        done <<< "$rsps"
        ;;
    full)
        linked=$(find "$obj" -path '*/linked/CommunityToolkit.Maui.Core.dll' 2>/dev/null || true)
        [ -n "$linked" ] || fail "No trimmed CommunityToolkit.Maui.Core.dll under $obj: the trimmer did not run on the device-test app."
        while IFS= read -r dll; do
            ls -l "$(dirname "$dll")"/CommunityToolkit.Maui*.dll
        done <<< "$linked"
        # With built-in COM off, the offline speech-to-text tests skip instead of failing, so a lost setting would only show as fewer tests.
        configs=$(find "$app/bin" -path '*-windows*' -name 'CommunityToolkit.Maui.DeviceTests.runtimeconfig.json' 2>/dev/null || true)
        if [[ "$linked" == *-windows* ]]; then
            [ -n "$configs" ] || fail "No CommunityToolkit.Maui.DeviceTests.runtimeconfig.json under $app/bin: cannot check that built-in COM is on."
        fi
        if [ -n "$configs" ]; then
            while IFS= read -r config; do
                grep -Eq '"System.Runtime.InteropServices.BuiltInComInterop.IsSupported": *true' "$config" || fail "$config does not turn built-in COM on."
                echo "$config: built-in COM on"
            done <<< "$configs"
        fi
        ;;
esac
