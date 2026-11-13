<#
.SYNOPSIS
    Trim audit of the toolkit's NuGet packages on Windows (the macOS side is audit.sh, which explains the variants).

.DESCRIPTION
    Why: apps consume the packed libraries, not the projects. A library build can pass its analyzers and still hand an app trim or AOT
    warnings, because the trimmer and ILC see the whole app. So this builds the Sample against the nupkgs with the trim settings an app
    can choose, then runs `ilwarn.py gate`, which fails on toolkit and toolkit-api warnings that allow.tsv does not list.

    One command per leg: -Repo packs a checkout first (into <OutRoot>\feed); -Feed uses the nupkgs that a CI pack step left in a folder
    (the four libraries, one version each, other files are ignored). Then it publishes the Sample (unpackaged, self-contained) in Release
    against those nupkgs instead of the toolkit projects, with the variant's trim settings (Directory.Build.targets), into
    <OutRoot>\windows-<Variant>\: publish\ (the app), build.binlog, build.log, warnings.log, assets.json (the restore's project.assets.json),
    linked-sizes.txt, result.json, assets-check.txt, warnings-unique.tsv, warnings-summary.txt and warnings-counts.json, and
    <OutRoot>\windows-<Variant>.zip with everything but publish\.

    Exit code: the gate's (0 clean, 1 new toolkit or toolkit-api warnings, 3 assets check not OK, 4 build failed or a log or result is
    missing or unreadable, 2 usage or a bad allow-list), 5 when packing failed. Usage and feed errors (bad -Variant, -Feed with -Repo or
    -Version, a missing or duplicate nupkg, no Windows target framework) are exit 2 as in audit.sh, never 1. -Github prints ::error
    annotations (at most -MaxAnnotations, default 10: GitHub shows 10 per step, and this script is the only gate of its step),
    -Summary appends a Markdown report to a file
    (give it $env:GITHUB_STEP_SUMMARY).

    Variant 'aot' (NativeAOT) is report only and is never gated: .NET MAUI apps published with NativeAOT on Windows crash at run
    time (dotnet/maui#39053), so its result can't gate the build. Its warnings and its log are kept, and the exit code is 0 unless
    the call itself is wrong.
    A failing 'full' publish is a result of the audit too, not something to hide.

    Untested on Windows: checked on macOS pwsh (parse, preamble, simulated publish) only. The first fork run is its proof.

.EXAMPLE
    pwsh -NoProfile -File eng\trim-audit\audit-win.ps1 -Variant rootlibs -Feed D:\a\nuget -Github -Summary $env:GITHUB_STEP_SUMMARY
.EXAMPLE
    pwsh -NoProfile -File eng\trim-audit\audit-win.ps1 -Variant full -Repo C:\src\CommunityToolkit.Maui
#>
[CmdletBinding()]
param(
    # Checked in the script body (not ValidateSet) so that a bad value is exit 2, like every other usage error.
    [string]$Variant = 'full',

    # Keep it short: the Sample's obj paths are long and Windows PowerShell 5.1 has no long-path support. Use one root per package set.
    [string]$OutRoot = 'C:\ta',

    [string]$RuntimeIdentifier = $(if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }),

    # Default: MauiPackageVersion of this repo's Directory.Build.props.
    [string]$MauiVersion = '',

    # The folder of nupkgs to build against. Give this or -Repo; neither: eng\trim-audit\feed.
    [string]$Feed = '',

    # A checkout of the toolkit to pack first, into <OutRoot>\feed, as version -Version.
    [string]$Repo = '',
    [string]$Version = '99.0.0-audit',

    [switch]$Github,
    [string]$Summary = '',
    [int]$MaxAnnotations = 10
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $here '..\..')).Path
$sample = Join-Path $repoRoot 'samples\CommunityToolkit.Maui.Sample\CommunityToolkit.Maui.Sample.csproj'
$reportOnly = $Variant -eq 'aot'

# Failures before the build are the caller's to fix: exit 2 (5 for packing) with an annotation and a summary line, never a bare PowerShell exit 1.
function Exit-WithAuditError([string]$Message, [int]$Code = 2) {
    Write-Host "GATE: $Message"
    if ($Github) {
        $escaped = $Message.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        Write-Host "::$(if ($Code -eq 0) { 'warning' } else { 'error' })::$escaped"
    }
    if ($Summary) { Add-Content -Path $Summary -Value "### Trim and AOT gate (windows-$Variant): FAIL (exit $Code)`n`n$Message" }
    exit $Code
}
# Camera and MediaElement have their own version on release tags, so each library's version is read from its own nupkg.
function Get-FeedVersion([string]$Id) {
    $found = @(Get-ChildItem $feedFolder -Filter "$Id.*.nupkg" | Where-Object { $_.Name -match ('^' + [regex]::Escape($Id) + '\.\d') })
    if ($found.Count -ne 1) { throw "expected exactly one $Id nupkg in $feedFolder, found $($found.Count)" }
    $found[0].Name.Substring($Id.Length + 1) -replace '\.nupkg$', ''
}

# PowerShell variable names ignore case: a local named repo, feed or version would silently overwrite the -Repo, -Feed or -Version parameter.
try {
    # MSBuild resolves the out-root against the Sample's folder and NuGet against the config's folder, so a relative path must be made absolute here.
    New-Item -ItemType Directory -Force $OutRoot | Out-Null
    $OutRoot = (Resolve-Path $OutRoot).Path
    if ($Variant -notin 'none', 'full', 'rootlibs', 'aot') { throw "unknown -Variant '$Variant' (none, full, rootlibs or aot)" }
    if ($Feed -and $Repo) { throw 'give -Feed or -Repo, not both' }
    if ($Feed -and $PSBoundParameters.ContainsKey('Version')) { throw '-Version only goes with -Repo; a -Feed already has its version' }
    if ($Repo) {
        $sourceRepo = (Resolve-Path $Repo).Path
        $feedFolder = Join-Path $OutRoot 'feed'
        New-Item -ItemType Directory -Force $feedFolder | Out-Null
        Get-ChildItem $feedFolder -File | Remove-Item -Force
        # The libraries pack the analyzers and source generators from their Release output, as CI builds them first.
        $analyzerProjects = 'Analyzers', 'Analyzers.CodeFixes', 'Camera.Analyzers', 'Camera.Analyzers.CodeFixes', 'MediaElement.Analyzers', 'MediaElement.Analyzers.CodeFixes', 'SourceGenerators', 'MediaElement.SourceGenerators'
        foreach ($suffix in $analyzerProjects) {
            $name = "CommunityToolkit.Maui.$suffix"
            & dotnet build (Join-Path $sourceRepo "src\$name\$name.csproj") -c Release -v q -nologo
            if ($LASTEXITCODE -ne 0) { Exit-WithAuditError "packing failed: building $name" 5 }
        }
        foreach ($name in 'CommunityToolkit.Maui.Core', 'CommunityToolkit.Maui', 'CommunityToolkit.Maui.Camera', 'CommunityToolkit.Maui.MediaElement') {
            & dotnet pack (Join-Path $sourceRepo "src\$name\$name.csproj") -c Release "-p:Version=$Version" "-p:PackageVersion=$Version" -o $feedFolder -nologo
            if ($LASTEXITCODE -ne 0) { Exit-WithAuditError "packing failed: packing $name" 5 }
        }
        Get-ChildItem $feedFolder -Filter '*.snupkg' | Remove-Item -Force
    }
    else {
        $feedFolder = if ($Feed) { (Resolve-Path $Feed).Path } else { Join-Path $here 'feed' }
    }
    if (-not $MauiVersion) {
        $MauiVersion = ([xml](Get-Content (Join-Path $repoRoot 'Directory.Build.props') -Raw)).SelectSingleNode('//MauiPackageVersion').InnerText
    }
    $versionMaui = Get-FeedVersion 'CommunityToolkit.Maui'
    $versionCore = Get-FeedVersion 'CommunityToolkit.Maui.Core'
    $versionCamera = Get-FeedVersion 'CommunityToolkit.Maui.Camera'
    $versionMediaElement = Get-FeedVersion 'CommunityToolkit.Maui.MediaElement'
}
catch { Exit-WithAuditError "$_" }
$coreVersion = $versionCore
$versionOf = @{ 'CommunityToolkit.Maui' = $versionMaui; 'CommunityToolkit.Maui.Core' = $versionCore; 'CommunityToolkit.Maui.Camera' = $versionCamera; 'CommunityToolkit.Maui.MediaElement' = $versionMediaElement }

$nuget = Join-Path $OutRoot 'nuget'
$packages = Join-Path $nuget 'packages'
$config = Join-Path $nuget 'NuGet.config'
New-Item -ItemType Directory -Force $packages | Out-Null
$userCache = Join-Path $env:USERPROFILE '.nuget\packages'
$ids = 'CommunityToolkit.Maui', 'CommunityToolkit.Maui.Core', 'CommunityToolkit.Maui.Camera', 'CommunityToolkit.Maui.MediaElement'
# Same layout as audit.sh: only the audit feed may serve the toolkit ids; the user cache is a read-only feed for everything else.
$lines = @('<?xml version="1.0" encoding="utf-8"?>', '<configuration>',
    "  <config><add key=`"globalPackagesFolder`" value=`"$packages`" /></config>",
    '  <packageSources>', '    <clear />', "    <add key=`"ctm-audit-feed`" value=`"$feedFolder`" />")
if (Test-Path $userCache) { $lines += "    <add key=`"usercache`" value=`"$userCache`" />" }
$lines += '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />'
$lines += '    <add key="dotnet-eng" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json" protocolVersion="3" />'
$lines += '  </packageSources>', '  <fallbackPackageFolders><clear /></fallbackPackageFolders>', '  <packageSourceMapping>', '    <packageSource key="ctm-audit-feed">'
$lines += $ids | ForEach-Object { "      <package pattern=`"$_`" />" }
$lines += '    </packageSource>'
if (Test-Path $userCache) { $lines += '    <packageSource key="usercache"><package pattern="*" /></packageSource>' }
$lines += '    <packageSource key="nuget.org"><package pattern="*" /></packageSource>'
$lines += '    <packageSource key="dotnet-eng"><package pattern="Microsoft.DotNet.XHarness.*" /></packageSource>'
$lines += '  </packageSourceMapping>', '</configuration>'
Set-Content -Path $config -Value $lines -Encoding UTF8

$env:DOTNET_NOLOGO = '1'; $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:MSBUILDDISABLENODEREUSE = '1'
$out = Join-Path $OutRoot "windows-$Variant"
if (Test-Path $out) { Remove-Item -Recurse -Force $out }
New-Item -ItemType Directory -Force $out | Out-Null

$frameworks = (& dotnet msbuild $sample -nologo -getProperty:TargetFrameworks) -split ';'
$tfm = $frameworks | Where-Object { $_ -match '^net[\d.]+-windows' } | Select-Object -First 1
if (-not $tfm) { Exit-WithAuditError 'the Sample has no Windows target framework on this machine' }

$publishAot = if ($Variant -eq 'aot') { @('-p:PublishAot=true') } else { @() }
$start = Get-Date
"trim audit: windows $Variant $tfm $RuntimeIdentifier packages $coreVersion (feed $feedFolder) MAUI $MauiVersion SuppressTrimAnalysisWarnings=false -> $out"
& dotnet publish $sample -c Release -f $tfm -r $RuntimeIdentifier -p:SelfContained=true -p:SuppressTrimAnalysisWarnings=false -o (Join-Path $out 'publish') `
    "-p:DirectoryBuildPropsPath=$here\Directory.Build.props" "-p:DirectoryBuildTargetsPath=$here\Directory.Build.targets" `
    "-p:CtmTrimAuditPackageVersion=$coreVersion" "-p:CtmTrimAuditNuGetConfig=$config" "-p:CtmTrimAuditPackagesPath=$packages" `
    "-p:CtmTrimAuditVersionMaui=$versionMaui" "-p:CtmTrimAuditVersionCore=$versionCore" "-p:CtmTrimAuditVersionCamera=$versionCamera" "-p:CtmTrimAuditVersionMediaElement=$versionMediaElement" `
    @publishAot `
    "-p:CtmTrimAuditMode=$Variant" "-p:CtmTrimAuditOutput=$out" "-p:CtmTrimAuditMauiVersion=$MauiVersion" `
    -nodeReuse:false "-bl:$out\build.binlog" "-flp1:logfile=$out\warnings.log;warningsonly" "-flp2:logfile=$out\build.log;verbosity=normal" `
    '-clp:Summary;ErrorsOnly'
$rc = $LASTEXITCODE
$end = Get-Date

$assets = Join-Path $out 'obj\project.assets.json'
if (Test-Path $assets) { Copy-Item $assets (Join-Path $out 'assets.json') }

# The Microsoft Store's python.exe stub is on PATH of a machine without Python and fails when run, so each candidate is probed.
function Find-Python {
    # Windows PowerShell 5.1 ends the script on the first stderr line of the Store stub, which is the case this probe exists for.
    $ErrorActionPreference = 'Continue'
    foreach ($name in 'python', 'python3') {
        $command = Get-Command $name -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $command) { continue }
        & $command.Source -c 'import sys' 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) { return $command.Source }
    }
    return $null
}
$python = Find-Python
$assetsExit = 0
$assetsCheck = Join-Path $out 'assets-check.txt'
if ($python) {
    # Windows PowerShell 5.1 ends the script on the first stderr line of a native command unless the preference is Continue.
    $ErrorActionPreference = 'Continue'
    if (Test-Path $assets) {
        & $python -I (Join-Path $here 'verify-assets.py') $assets $feedFolder $packages 2>&1 | ForEach-Object { "$_" } | Set-Content $assetsCheck
        $assetsExit = $LASTEXITCODE
    }
    else {
        'ASSETS-CHECK=FAIL (no project.assets.json: the restore failed)' | Set-Content $assetsCheck
        $assetsExit = 1
    }
    & $python -I (Join-Path $here 'ilwarn.py') summarize (Join-Path $out 'warnings.log') $out $packages | Out-Null
    $ErrorActionPreference = 'Stop'
}
$sizes = @('# assembly | package bytes | trimmed bytes (none: not trimmed)')
foreach ($id in $ids) {
    $original = Get-ChildItem (Join-Path $packages "$($id.ToLowerInvariant())\$($versionOf[$id])\lib") -Recurse -Filter "$id.dll" -ErrorAction SilentlyContinue | Where-Object FullName -Match 'windows' | Select-Object -First 1
    $linked = Get-ChildItem (Join-Path $out 'obj') -Recurse -Filter "$id.dll" -ErrorAction SilentlyContinue | Where-Object FullName -Match 'link' | Select-Object -First 1
    $sizes += "$id | $($original.Length) | $(if ($linked) { $linked.Length } else { 'none' })"
}
Set-Content -Path (Join-Path $out 'linked-sizes.txt') -Value $sizes
$exe = Get-ChildItem (Join-Path $out 'publish') -Filter 'CommunityToolkit.Maui.Sample.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
@{ platform = 'windows'; variant = $Variant; tfm = $tfm; rid = $RuntimeIdentifier; packages = $coreVersion; maui = $MauiVersion; build_exit = $rc; assets_exit = $assetsExit
   minutes = [math]::Round(($end - $start).TotalMinutes, 1); app = $(if ($exe) { $exe.FullName } else { '' }) } |
    ConvertTo-Json | Set-Content (Join-Path $out 'result.json')

$zip = Join-Path $OutRoot "windows-$Variant.zip"
if (Test-Path $zip) { Remove-Item $zip }
Get-ChildItem $out -File | Compress-Archive -DestinationPath $zip
"AUDIT-WIN-EXIT=$rc ZIP=$zip ZIP-BYTES=$((Get-Item $zip).Length) APP=$(if ($exe) { $exe.FullName })"

if (-not $python) {
    Exit-WithAuditError 'no working Python on PATH; the gate cannot run' $(if ($reportOnly) { 0 } else { 4 })
}
$gateArguments = @('-I', (Join-Path $here 'ilwarn.py'), 'gate', (Join-Path $out 'warnings-unique.tsv'), '--allow', (Join-Path $here 'allow.tsv'),
    '--log', (Join-Path $out 'warnings.log'), '--assets-check', $assetsCheck, '--result', (Join-Path $out 'result.json'))
if ($Github) { $gateArguments += '--github', '--max-annotations', $MaxAnnotations }
if ($Summary) { $gateArguments += '--summary', $Summary }
if ($reportOnly) { $gateArguments += '--report-only' }
$ErrorActionPreference = 'Continue'
& $python @gateArguments
exit $LASTEXITCODE
