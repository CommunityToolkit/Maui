<#
.SYNOPSIS
    Checks that the toolkit's NuGet packages are trim-safe and NativeAOT-safe, with one command.

.DESCRIPTION
    Packs the four libraries from this checkout (or uses packages that are already packed), builds the Sample against those packages for
    every platform this machine can build, in the modes CI gates on, and fails when the toolkit adds a trim or NativeAOT warning that
    eng/trim-audit/allow.tsv does not list. It prints a short verdict at the end: PASS or FAIL, the warnings per bucket for every leg,
    and the folder of the logs and warning lists. The exit code is the gate's, or 0 when every leg passed.

    Without -Platform it builds, on
      macOS    iOS and Android, with NativeAOT (aotroot: ILC analyzes all toolkit code)
      Windows  Windows, with full trimming (rootlibs: all toolkit code analyzed)
      Linux    Android, with NativeAOT (aotroot)
    A default platform that this machine cannot build (no Xcode, no Android SDK) is skipped and the plan says why; with -CI nothing is
    skipped. Mac Catalyst builds with -Platform maccatalyst. On macOS and Linux it first runs the gate's own Python tests (a few
    seconds, then again under a Turkish locale) unless -SkipSelfTest. Windows skips them: the Turkish pass sets POSIX locale
    variables, and CI runs them on the macOS leg.

    The plan (platforms, modes, packages and out-root) is printed first. -DryRun prints it and stops.

    This script drives audit.sh (macOS, Linux) and audit-win.ps1 (Windows); their headers document every flag for advanced use, such as
    building one leg or gating a result elsewhere. Python 3 is needed to judge the warnings: without it
    the script stops before any build with exit 4 and says what to install.

    Exit codes: 0 every leg passed; 1 new toolkit or toolkit-api warnings; 2 usage error or a bad feed; 3 the build did not
    use the packages (assets check) or the NativeAOT roots check failed; 4 a build failed, its logs are unreadable or Python 3 is missing; 5 packing failed;
    6 the gate's own tests failed (macOS, Linux); 71 no platform can run here. With several legs, the first non-zero code of the legs.

.PARAMETER Platform
    android, ios, maccatalyst (or catalyst) and windows. Comma-separated values are fine. A platform named here that this machine cannot
    build is an error, not a skip.

.PARAMETER Mode
    aot (NativeAOT: aotroot, but plain aot on Windows, which only reports because MAUI apps published with NativeAOT crash there,
    dotnet/maui#39053) or full (full trimming with all toolkit code analyzed: rootlibs). Default: aot, and full on Windows.

.PARAMETER Variant
    One harness variant for every platform, in place of -Mode: none, partial, full, rootlibs, aot or aotroot (see audit.sh).

.PARAMETER Feed
    A folder of the four packed nupkgs (one version each), so that nothing is packed. 'auto' finds the nupkgs a CI pack step left:
    $env:GITHUB_WORKSPACE/nuget when it has them, else src/*/bin/Release. Default: pack this checkout.

.PARAMETER Version
    The version that packing gives the nupkgs. Default: a new one for every run, so that a changed library never meets a cached package.

.PARAMETER OutRoot
    Where the packages, builds and logs go. Default: artifacts/trim-audit (ignored by git); with -CI $env:RUNNER_TEMP/trim-audit; C:\ta
    on Windows (the Sample's paths are long).

.PARAMETER CI
    For GitHub Actions: ::error annotations, a job summary in $env:GITHUB_STEP_SUMMARY, an AUDIT-EXIT=<code> line at the end (the pwsh
    wrapper of a step reports any non-zero exit as 1, so the log must say the code) and no skipped platforms.

.PARAMETER SkipSelfTest
    Do not run the gate's own tests first (they never run on Windows).

.PARAMETER DryRun
    Prints the plan and runs nothing. Also available as -WhatIf.

.PARAMETER Help
    Prints this help.

.EXAMPLE
    pwsh eng/trim-audit/run.ps1
    Every platform this machine can build, on this checkout.

.EXAMPLE
    pwsh eng/trim-audit/run.ps1 -DryRun
    Shows what the first example would run.

.EXAMPLE
    pwsh eng/trim-audit/run.ps1 -Platform android -Mode full
    One platform with full trimming.

.EXAMPLE
    pwsh eng/trim-audit/run.ps1 -Feed ~/packages -SkipSelfTest
    Packages that are already packed.

.EXAMPLE
    pwsh eng/trim-audit/run.ps1 -Feed auto -CI
    What the Build Library job of CI runs.
#>
[CmdletBinding()]
param(
    [string[]]$Platform,
    [string]$Mode,
    [string]$Variant,
    [string]$Feed,
    [string]$Version,
    [string]$OutRoot,
    [switch]$CI,
    [switch]$SkipSelfTest,
    [Alias('WhatIf')][switch]$DryRun,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'
if ($Help) { Get-Help $PSCommandPath -Detailed; exit 0 }

# Windows PowerShell 5.1 has no $IsWindows or $IsMacOS.
$onWindows = $env:OS -eq 'Windows_NT'
$hostKind = if ($onWindows) { 'windows' } elseif ((& uname) -eq 'Darwin') { 'macos' } else { 'linux' }
$harnessFolder = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $harnessFolder '..\..')).Path
$powershellExecutable = (Get-Process -Id $PID).Path
$summaryFile = if ($CI -and $env:GITHUB_STEP_SUMMARY) { $env:GITHUB_STEP_SUMMARY } else { '' }
$knownVariants = 'none', 'partial', 'full', 'rootlibs', 'aot', 'aotroot'
$buildablePlatforms = @{ macos = 'ios', 'android', 'maccatalyst'; linux = @('android'); windows = @('windows') }

function Write-Annotation([string]$Text) {
    if ($CI) { Write-Host ('::error::' + $Text.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')) }
}
function Stop-Run([string]$Message, [int]$Code) {
    Write-Host "trim audit: $Message"
    Write-Annotation $Message
    if ($summaryFile) { Add-Content -Path $summaryFile -Value "### Trim and AOT gate: FAIL (exit $Code)`n`n$Message" }
    if ($CI) {
        Write-Host "AUDIT-EXIT=$Code"
        Write-Host "GATE-EXIT=$Code"
    }
    exit $Code
}

function Find-Python {
    # The Microsoft Store's python.exe stub is on PATH of a machine without Python and fails when run, so each candidate is probed.
    $ErrorActionPreference = 'Continue'
    foreach ($name in 'python3', 'python') {
        $command = Get-Command $name -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $command) { continue }
        & $command.Source -c 'import sys' 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) { return $command.Source }
    }
    return $null
}
function Test-AndroidSdkPresent {
    foreach ($candidate in $env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, (Join-Path $HOME 'Library/Android/sdk'), (Join-Path $HOME 'Android/Sdk')) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) { return $true }
    }
    return $false
}
function Test-XcodePresent {
    $ErrorActionPreference = 'Continue'
    & xcode-select -p 2>&1 | Out-Null
    return $LASTEXITCODE -eq 0
}
function Get-VariantFor([string]$PlatformName) {
    if ($Variant) { return $Variant }
    if ($Mode -eq 'full') { return 'rootlibs' }
    if ($PlatformName -eq 'windows') { return $(if ($Mode -eq 'aot') { 'aot' } else { 'rootlibs' }) }
    return 'aotroot'
}

if ($Mode) { $Mode = $Mode.ToLowerInvariant() }
if ($Variant) { $Variant = $Variant.ToLowerInvariant() }
if ($Mode -and $Mode -notin 'aot', 'full') { Stop-Run "unknown -Mode '$Mode' (aot or full)" 2 }
if ($Variant -and $Mode) { Stop-Run '-Variant replaces -Mode; give one of them' 2 }
if ($Variant -and $Variant -notin $knownVariants) { Stop-Run "unknown -Variant '$Variant' (none, partial, full, rootlibs, aot or aotroot)" 2 }
if ($Feed -and $Version) { Stop-Run '-Version only goes with packing; a -Feed already has its version' 2 }

function Invoke-SelfTest {
    $ErrorActionPreference = 'Continue'
    foreach ($selfTestRun in @(@{ Label = 'default locale'; Locale = '' }, @{ Label = 'tr_TR.UTF-8'; Locale = 'tr_TR.UTF-8' })) {
        $previousLang = $env:LANG; $previousLcAll = $env:LC_ALL
        if ($selfTestRun.Locale) { $env:LANG = $selfTestRun.Locale; $env:LC_ALL = $selfTestRun.Locale }
        Write-Host "gate self-test ($($selfTestRun.Label))"
        Push-Location $repoRoot
        $selfTestOutput = @(& $python -I -m unittest discover -s eng/trim-audit -p 'test_*.py' 2>&1 | ForEach-Object { "$_" })
        $selfTestExit = $LASTEXITCODE
        Pop-Location
        $env:LANG = $previousLang; $env:LC_ALL = $previousLcAll
        if ($selfTestExit -ne 0) {
            $selfTestOutput | Write-Host
            Stop-Run "the gate's own tests failed ($($selfTestRun.Label), exit $selfTestExit): fix the gate before trusting a verdict" 6
        }
        $selfTestOutput | Select-Object -Last 4 | Write-Host
    }
    $ErrorActionPreference = 'Stop'
}
function Remove-StaleLegResults {
    foreach ($name in $platformPlan) {
        $legFolder = Join-Path $OutRoot "$name-$(Get-VariantFor $name)"
        if (Test-Path -LiteralPath $legFolder) { Remove-Item -LiteralPath $legFolder -Recurse -Force }
    }
}
function Remove-StalePackedVersions {
    $packageCache = Join-Path $OutRoot 'nuget/packages'
    if (-not (Test-Path -LiteralPath $packageCache)) { return }
    Get-ChildItem -LiteralPath $packageCache -Directory -Filter 'communitytoolkit.maui*' |
        ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -Directory -Filter '99.0.0-audit*' } | Remove-Item -Recurse -Force
}
function Get-ExitHint([int]$Code) {
    switch ($Code) {
        1 { return "fix the new toolkit or toolkit-api warnings in the library; an allow.tsv line needs a maintainer's approval (see eng/trim-audit/README.md)." }
        2 { return 'usage error or a bad feed: see the message above.' }
        3 { return 'the build did not use the packed packages, or the NativeAOT roots check failed: see <leg>/assets-check.txt.' }
        4 { return 'a build failed, its logs are unreadable or Python 3 is missing: see the message above or <leg>/build.log.' }
        5 { return 'packing failed: see the dotnet pack errors above.' }
        default { return '' }
    }
}

$platformPlan = @()
$skipped = @()
if ($Platform) {
    foreach ($requested in ($Platform | ForEach-Object { $_ -split ',' } | Where-Object { $_ })) {
        $name = $requested.Trim().ToLowerInvariant()
        if ($name -eq 'catalyst') { $name = 'maccatalyst' }
        if ($name -notin 'android', 'ios', 'maccatalyst', 'windows') { Stop-Run "unknown platform '$requested' (android, ios, maccatalyst or windows)" 2 }
        if ($name -notin $buildablePlatforms[$hostKind]) { Stop-Run "this machine ($hostKind) cannot build $name" 2 }
        if ($name -notin $platformPlan) { $platformPlan += $name }
    }
}
else {
    foreach ($name in 'ios', 'android', 'windows', 'maccatalyst') {
        if ($name -notin $buildablePlatforms[$hostKind] -or $name -eq 'maccatalyst') { continue }
        if (-not $CI -and $name -eq 'ios' -and -not (Test-XcodePresent)) { $skipped += 'ios: no Xcode (xcode-select -p fails)'; continue }
        if (-not $CI -and $name -eq 'android' -and -not (Test-AndroidSdkPresent)) { $skipped += 'android: no Android SDK (set ANDROID_HOME)'; continue }
        $platformPlan += $name
    }
}
if ($platformPlan.Count -eq 0) { Stop-Run ("no platform can be built on this machine: " + ($skipped -join '; ')) 71 }

if (-not $OutRoot) {
    $OutRoot = if ($onWindows) { 'C:\ta' } elseif ($CI -and $env:RUNNER_TEMP) { Join-Path $env:RUNNER_TEMP 'trim-audit' } else { Join-Path $repoRoot 'artifacts/trim-audit' }
}
$feedMode = if (-not $Feed) { 'pack' } elseif ($Feed -eq 'auto') { 'auto' } else { 'folder' }
$packVersion = if ($Version) { $Version } else { '99.0.0-audit.' + ([DateTime]::UtcNow.ToString('yyyyMMddHHmmss', [Globalization.CultureInfo]::InvariantCulture)) }
$runsSelfTest = -not $SkipSelfTest -and -not $onWindows

Write-Host "trim audit plan ($hostKind)"
foreach ($name in $platformPlan) { Write-Host ("  leg      {0}-{1}" -f $name, (Get-VariantFor $name)) }
foreach ($line in $skipped) { Write-Host "  skipped  $line" }
switch ($feedMode) {
    'pack' { Write-Host "  packages pack this checkout ($repoRoot) as $packVersion" }
    'auto' { Write-Host '  packages the nupkgs of the CI pack step ($env:GITHUB_WORKSPACE/nuget, else src/*/bin/Release)' }
    'folder' { Write-Host "  packages $Feed" }
}
Write-Host "  out-root $OutRoot"
Write-Host ("  self-test {0}" -f $(if ($runsSelfTest) { 'python -m unittest on eng/trim-audit (default locale, then tr_TR.UTF-8)' } elseif ($onWindows) { 'skipped on Windows: the Turkish pass needs POSIX locales, and CI runs them on the macOS leg' } else { 'skipped' }))
if ($DryRun) {
    Write-Host 'dry run: nothing was run'
    exit 0
}

$python = Find-Python
if (-not $python) {
    $installHint = if ($onWindows) { 'winget install Python.Python.3.13, then reopen the shell' } else { 'brew install python, or apt install python3' }
    Stop-Run "needs Python 3 to judge the warnings: install it ($installHint) and run again" 4
}

if ($runsSelfTest) { Invoke-SelfTest }

New-Item -ItemType Directory -Force $OutRoot | Out-Null
Remove-StaleLegResults
Remove-StalePackedVersions
$OutRoot = (Resolve-Path $OutRoot).Path
if ($CI -and $env:GITHUB_OUTPUT) { Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "out-root=$($OutRoot.Replace('\', '/'))" }
$feedFolder = ''
if ($feedMode -eq 'folder') {
    if (-not (Test-Path -LiteralPath $Feed -PathType Container)) { Stop-Run "no such feed folder: $Feed" 2 }
    $feedFolder = (Resolve-Path $Feed).Path
}
elseif ($feedMode -eq 'auto') {
    $workspaceFeed = if ($env:GITHUB_WORKSPACE) { Join-Path $env:GITHUB_WORKSPACE 'nuget' } else { '' }
    if ($workspaceFeed -and (Test-Path $workspaceFeed) -and @(Get-ChildItem $workspaceFeed -Filter 'CommunityToolkit.Maui*.nupkg').Count -gt 0) {
        $feedFolder = $workspaceFeed
    }
    else {
        $feedFolder = Join-Path $OutRoot 'feed-auto'
        New-Item -ItemType Directory -Force $feedFolder | Out-Null
        Get-ChildItem $feedFolder -File | Remove-Item -Force
        Get-ChildItem (Join-Path $repoRoot 'src') -Recurse -Filter 'CommunityToolkit.Maui*.nupkg' |
            Where-Object { $_.FullName -match '[\\/]bin[\\/]Release[\\/]' } | Copy-Item -Destination $feedFolder
        if (@(Get-ChildItem $feedFolder -Filter '*.nupkg').Count -eq 0) { Stop-Run "-Feed auto found no nupkgs in src/*/bin/Release: pack the libraries first or give -Feed <folder>" 2 }
    }
}

$legExits = @()
$ErrorActionPreference = 'Continue'
if ($hostKind -eq 'windows') {
    foreach ($name in $platformPlan) {
        $windowsVariant = Get-VariantFor $name
        $windowsArguments = @('-NoProfile', '-File', (Join-Path $harnessFolder 'audit-win.ps1'), '-Variant', $windowsVariant, '-OutRoot', $OutRoot)
        if ($feedFolder) { $windowsArguments += '-Feed', $feedFolder } else { $windowsArguments += '-Repo', $repoRoot, '-Version', $packVersion }
        if ($CI) { $windowsArguments += '-Github' }
        if ($summaryFile) { $windowsArguments += '-Summary', $summaryFile }
        & $powershellExecutable @windowsArguments
        $legExits += $LASTEXITCODE
    }
}
else {
    $shellArguments = @((Join-Path $harnessFolder 'audit.sh'), 'run', ($platformPlan -join ','), (Get-VariantFor $platformPlan[0]), '--out-root', $OutRoot)
    if ($feedFolder) { $shellArguments += '--feed', $feedFolder } else { $shellArguments += '--repo', $repoRoot, '--version', $packVersion }
    if ($CI) { $shellArguments += '--github' }
    if ($summaryFile) { $shellArguments += '--summary', $summaryFile }
    # audit.sh run takes one variant for all its platforms; the default modes never mix variants on a non-Windows host.
    & bash @shellArguments
    $legExits += $LASTEXITCODE
}
$ErrorActionPreference = 'Stop'
$overallExit = 0
foreach ($legExit in $legExits) { if ($overallExit -eq 0 -and $legExit -ne 0) { $overallExit = $legExit } }

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    try { return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json } catch { return $null }
}
function Get-LegVerdict([string]$LegFolder) {
    $gateOutputPath = Join-Path $LegFolder 'gate-output.txt'
    if (-not (Test-Path -LiteralPath $gateOutputPath)) { return 'NOT RUN' }
    $gateText = Get-Content -LiteralPath $gateOutputPath -Raw
    if ($gateText -match '(?m)^GATE: OK') { return 'PASS' }
    if ($gateText -match '(?m)^GATE: REPORT ONLY') { return 'REPORT' }
    return 'FAIL'
}
$verdict = if ($overallExit -eq 0) { 'PASS' } else { 'FAIL' }
$rows = @()
foreach ($name in $platformPlan) {
    $legName = "$name-$(Get-VariantFor $name)"
    $legFolder = Join-Path $OutRoot $legName
    $result = Read-Json (Join-Path $legFolder 'result.json')
    $buckets = Read-Json (Join-Path $legFolder 'warnings-counts.json')
    $count = {
        param($key)
        if ($result -and $null -ne $result.("warnings_$key")) { return [string]$result.("warnings_$key") }
        if ($buckets -and $buckets.by_bucket -and $null -ne $buckets.by_bucket.($key.Replace('_', '-'))) { return [string]$buckets.by_bucket.($key.Replace('_', '-')) }
        if ($buckets) { return '0' }
        return '-'
    }
    $rows += [pscustomobject]@{
        Leg = $legName; Verdict = Get-LegVerdict $legFolder
        Toolkit = & $count 'toolkit'; ToolkitApi = & $count 'toolkit_api'; Sample = & $count 'sample'; Maui = & $count 'maui'; Other = & $count 'other'
    }
}
Write-Host ''
Write-Host "== Trim and AOT audit: $verdict (exit $overallExit) =="
$rows | Format-Table -AutoSize @{ n = 'leg'; e = 'Leg' }, @{ n = 'verdict'; e = 'Verdict' }, @{ n = 'toolkit'; e = 'Toolkit' }, @{ n = 'toolkit-api'; e = 'ToolkitApi' },
    @{ n = 'sample'; e = 'Sample' }, @{ n = 'maui'; e = 'Maui' }, @{ n = 'other'; e = 'Other' } | Out-String | Write-Host
Write-Host "Logs and warning lists: $(Join-Path $OutRoot '<leg>')$([IO.Path]::DirectorySeparatorChar) (build.log, warnings.log, warnings-summary.txt, warnings-unique.tsv, gate-output.txt)"
$exitHint = Get-ExitHint $overallExit
if ($exitHint) { Write-Host $exitHint }
if ($summaryFile) { Add-Content -Path $summaryFile -Value "`n**Trim and AOT audit: $verdict (exit $overallExit)**" }
if ($CI) {
    Write-Host "AUDIT-EXIT=$overallExit"
    Write-Host "GATE-EXIT=$overallExit"
}
exit $overallExit