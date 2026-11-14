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
    linked-sizes.txt, result.json, assets-check.txt, warnings-unique.tsv, warnings-summary.txt, warnings-counts.json, gate-output.txt (the gate's
    verdict) and toolkit-dlls\ (the four toolkit DLLs, for gating elsewhere), and <OutRoot>\windows-<Variant>.zip with everything but publish\.

    Exit code: the gate's (0 clean, 1 new toolkit or toolkit-api warnings, 3 assets check not OK, 4 build failed or a log or result is
    missing or unreadable, 2 usage or a bad allow-list), 5 when packing failed. Usage and feed errors (bad -Variant, -Feed with -Repo or
    -Version, a missing or duplicate nupkg, no Windows target framework) are exit 2 as in audit.sh, never 1. -Github prints ::error
    annotations (at most -MaxAnnotations, default 10: GitHub shows 10 error annotations per step and 50 per job, summed over steps, so the job summary is the reliable channel),
    -Summary appends a Markdown report to a file
    (give it $env:GITHUB_STEP_SUMMARY). Every exit prints GATE-EXIT=<code>, because a pwsh step of GitHub Actions reports any non-zero exit as 1.

    Without a working Python the script still builds and writes the logs, result.json and assets-check.txt (checked in PowerShell), prints how
    to gate the zip elsewhere and exits 4 (0 for -Variant aot): build ok, gate not run. On macOS or Linux:
    eng/trim-audit/gate-windows-result.sh <the zip or its folder>

    Variant 'aot' (NativeAOT) is report only and is never gated: .NET MAUI apps published with NativeAOT on Windows crash at run
    time (dotnet/maui#39053), so its result can't gate the build. Its warnings and its log are kept, and the exit code is 0 unless
    the call itself is wrong.
    A failing 'full' publish is a result of the audit too, not something to hide.

    Proven by fork run r1: PowerShell 7 restore, sha512 check, publish, zip and gate on a GitHub Windows runner. Proven on a Windows 11 VM:
    Windows PowerShell 5.1 and the no-Python path (assets check, GATE-EXIT, UTF-8 gate-output.txt). Still unproven on Windows: the
    build-ok branch and the gate's 0/1 verdict.

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

# The wrapper that GitHub puts around a pwsh step can turn any non-zero exit into 1, so every exit prints GATE-EXIT=<code> and the summary says it too.
$script:gateOutputPath = ''
function Write-GateOutput([string]$Text) {
    if ($script:gateOutputPath) { Add-Content -LiteralPath $script:gateOutputPath -Value $Text -Encoding UTF8 }
}
function Write-GateExit([int]$Code) {
    $line = "GATE-EXIT=$Code (windows-$Variant)"
    Write-Host $line
    Write-GateOutput $line
    if ($Summary) { Add-Content -Path $Summary -Value "`nGate exit code: $Code (windows-$Variant)" }
}
# Failures before the build are the caller's to fix: exit 2 (5 for packing) with an annotation and a summary line, never a bare PowerShell exit 1.
function Exit-WithAuditError([string]$Message, [int]$Code = 2) {
    Write-Host "GATE: $Message"
    Write-GateOutput "GATE: $Message"
    if ($Github) {
        $escaped = $Message.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        Write-Host "::$(if ($Code -eq 0) { 'warning' } else { 'error' })::$escaped"
    }
    if ($Summary) { Add-Content -Path $Summary -Value "### Trim and AOT gate (windows-$Variant): FAIL (exit $Code)`n`n$Message" }
    Write-GateExit $Code
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
$script:gateOutputPath = Join-Path $out 'gate-output.txt'

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
# Same checks as verify-assets.py, for a machine without Python: returns 0 (ASSETS-CHECK=OK) or 1.
function Write-AssetsCheckWithoutPython([string]$AssetsPath, [string]$CheckPath) {
    $failures = @()
    $passes = @()
    $seen = @()
    if (-not (Test-Path $AssetsPath)) { $failures += 'no project.assets.json: the restore failed' }
    else {
        try {
            $assetsJson = Get-Content $AssetsPath -Raw | ConvertFrom-Json
            # A file that parses but is not an assets object (empty, null, a number, a string) must fail, never pass.
            if ($assetsJson -isnot [pscustomobject] -or $assetsJson.libraries -isnot [pscustomobject] -or $assetsJson.packageFolders -isnot [pscustomobject]) { throw 'it has no libraries and packageFolders objects' }
            foreach ($library in $assetsJson.libraries.PSObject.Properties) {
                $name, $libraryVersion = $library.Name -split '/', 2
                if ($name -cnotin $ids) {
                    if ($library.Value.type -eq 'project' -and $name.StartsWith('CommunityToolkit.Maui', [StringComparison]::Ordinal) -and -not $name.StartsWith('CommunityToolkit.Maui.DeviceTests', [StringComparison]::Ordinal)) { $failures += "project reference to toolkit code: $($library.Name)" }
                    continue
                }
                $seen += $name
                if ($library.Value.type -ne 'package') { $failures += "$($library.Name) is a $($library.Value.type), not a package"; continue }
                if ($versionOf[$name] -ne $libraryVersion) { $failures += "$($library.Name): the feed has $($versionOf[$name])"; continue }
                $restored = Join-Path $packages "$($name.ToLowerInvariant())\$($libraryVersion.ToLowerInvariant())\$($name.ToLowerInvariant()).$($libraryVersion.ToLowerInvariant()).nupkg.sha512"
                if (-not (Test-Path $restored)) { $failures += "$($library.Name): not restored into $packages"; continue }
                $feedSha = [Convert]::ToBase64String([System.Security.Cryptography.SHA512]::Create().ComputeHash([IO.File]::ReadAllBytes((Join-Path $feedFolder "$name.$libraryVersion.nupkg"))))
                if ((Get-Content $restored -Raw).Trim() -ne $feedSha) { $failures += "$($library.Name): the restored nupkg differs from the feed (stale packages folder? delete $packages)"; continue }
                $passes += "OK $($library.Name) package from the feed (sha512 matches)"
            }
            $restoreFolders = foreach ($folder in $assetsJson.packageFolders.PSObject.Properties) { $folder.Name.TrimEnd('\') }
            if ($packages.TrimEnd('\') -notin @($restoreFolders)) { $failures += "packages folder $packages is not the restore folder" }
            if ('CommunityToolkit.Maui' -notin $seen) { $failures += 'CommunityToolkit.Maui is not in the assets file' }
        }
        catch { $failures += "cannot read ${AssetsPath}: $($_.Exception.Message)" }
    }
    $lines = $passes + @($failures | ForEach-Object { "FAIL $_" }) + "ASSETS-CHECK=$(if ($failures) { 'FAIL' } else { 'OK' })"
    Set-Content -Path $CheckPath -Value $lines
    return [int]($failures.Count -gt 0)
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
else {
    $assetsExit = Write-AssetsCheckWithoutPython $assets $assetsCheck
}
$sizes = @('# assembly | package bytes | trimmed bytes (none: not trimmed)')
# ilwarn.py attributes a warning to a toolkit assembly by reading its DLL from a folder with a lib\ segment: the zip carries the four DLLs so that a
# machine with Python can run `summarize` and the gate on a result that was built without it.
$dllFolder = Join-Path $out 'toolkit-dlls\lib'
New-Item -ItemType Directory -Force $dllFolder | Out-Null
foreach ($id in $ids) {
    $original = Get-ChildItem (Join-Path $packages "$($id.ToLowerInvariant())\$($versionOf[$id])\lib") -Recurse -Filter "$id.dll" -ErrorAction SilentlyContinue | Where-Object FullName -Match 'windows' | Select-Object -First 1
    $linked = Get-ChildItem (Join-Path $out 'obj') -Recurse -Filter "$id.dll" -ErrorAction SilentlyContinue | Where-Object FullName -Match 'link' | Select-Object -First 1
    $sizes += "$id | $($original.Length) | $(if ($linked) { $linked.Length } else { 'none' })"
    if ($original) { Copy-Item $original.FullName $dllFolder }
}
Set-Content -Path (Join-Path $out 'linked-sizes.txt') -Value $sizes
$exe = Get-ChildItem (Join-Path $out 'publish') -Filter 'CommunityToolkit.Maui.Sample.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
@{ platform = 'windows'; variant = $Variant; tfm = $tfm; rid = $RuntimeIdentifier; packages = $coreVersion; maui = $MauiVersion; build_exit = $rc; assets_exit = $assetsExit
   minutes = [math]::Round(($end - $start).TotalMinutes, 1); app = $(if ($exe) { $exe.FullName } else { '' }) } |
    ConvertTo-Json | Set-Content (Join-Path $out 'result.json')

if (-not $python) {
    $gateExit = $(if ($reportOnly) { 0 } else { 4 })
    $assetsLine = @(Get-Content $assetsCheck | Where-Object { $_ -like 'ASSETS-CHECK=*' })[-1]
    $whatHappened = if ($rc -ne 0) { "the build failed (dotnet publish exit $rc); its logs are in the zip; the gate did not run" } else { 'the build finished and its results are in the zip, but the gate did not run' }
    $noPython = "no working Python on PATH: $whatHappened. $assetsLine (checked in PowerShell). Gate it on any machine with Python 3 " +
        '(macOS, Linux or Windows): eng/trim-audit/gate-windows-result.sh <the zip or its folder>, or run in order: ' +
        'python3 -I eng/trim-audit/ilwarn.py summarize <folder>/warnings.log <folder> <folder>/toolkit-dlls, then ' +
        'python3 -I eng/trim-audit/ilwarn.py gate <folder>/warnings-unique.tsv --allow eng/trim-audit/allow.tsv --log <folder>/warnings.log --assets-check <folder>/assets-check.txt --result <folder>/result.json ' +
        "(python on Windows). Exit code $gateExit$(if ($rc -eq 0) { ': build ok, gate not run' })."
    Write-Host "GATE: $noPython"
    Write-GateOutput "GATE: $noPython"
    if ($Github) { Write-Host "::$(if ($reportOnly) { 'warning' } else { 'error' })::$($noPython.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A'))" }
    if ($Summary) { Add-Content -Path $Summary -Value "### Trim and AOT gate (windows-$Variant): not run (exit $gateExit)`n`n$noPython" }
}
else {
    $gateArguments = @('-I', (Join-Path $here 'ilwarn.py'), 'gate', (Join-Path $out 'warnings-unique.tsv'), '--allow', (Join-Path $here 'allow.tsv'),
        '--log', (Join-Path $out 'warnings.log'), '--assets-check', $assetsCheck, '--result', (Join-Path $out 'result.json'))
    if ($Github) { $gateArguments += '--github', '--max-annotations', $MaxAnnotations }
    if ($Summary) { $gateArguments += '--summary', $Summary }
    if ($reportOnly) { $gateArguments += '--report-only' }
    $ErrorActionPreference = 'Continue'
    & $python @gateArguments 2>&1 | ForEach-Object { "$_" } | ForEach-Object { $_; Add-Content -LiteralPath $script:gateOutputPath -Value $_ -Encoding UTF8 }
    $gateExit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
}
Write-GateExit $gateExit

$zip = Join-Path $OutRoot "windows-$Variant.zip"
if (Test-Path $zip) { Remove-Item $zip }
(@(Get-ChildItem $out -File) + @(Get-Item (Join-Path $out 'toolkit-dlls'))) | Compress-Archive -DestinationPath $zip
"AUDIT-WIN-EXIT=$rc ZIP=$zip ZIP-BYTES=$((Get-Item $zip).Length) APP=$(if ($exe) { $exe.FullName })"
exit $gateExit
