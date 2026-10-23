<#
.SYNOPSIS
    Runs the CommunityToolkit.Maui device tests on every platform this machine can run, with one command.

.DESCRIPTION
    Builds and runs the device test apps with the platform scripts (ios.ps1, catalyst.ps1, android.ps1, windows.ps1) and prints one summary at
    the end: the passed, failed and skipped tests per platform and app, and the folder of their results. The exit code is the first non-zero
    exit code of the platform scripts (xharness's vocabulary, see eng/devices/README.md), or 0 when every app passed.

    Without -Platform it runs, on
      macOS    the iOS simulator and Mac Catalyst (both need Xcode), and Android
      Windows  the packaged (MSIX) app and the unpackaged app, and Android
      Linux    Android
    Android runs on an emulator that is already running, or else on an AVD that this machine's CPU can run, which the script boots and stops
    again. A physical device is only used when -DeviceId names it. A platform that this machine cannot run is skipped, and the plan and the
    summary say why. When nothing can run, the exit code is 71.

    The plan (the platforms, the apps and the commands) is printed first. -DryRun prints it and stops.

    The platforms run one after the other, in this PowerShell process, and the output of the platform scripts is shown as they print it.

.PARAMETER Platform
    The platforms to run: ios, maccatalyst (or catalyst), android, windows-packaged (or packaged), windows-unpackaged (or unpackaged) and
    windows (both Windows platforms). Comma-separated values are fine. Without it, every platform this machine can run. A platform named here
    that cannot run is an error, not a skip.

.PARAMETER Project
    The app(s) to build and run: Maui, MediaElement, Camera, All (the default), a project name or a .csproj path. Comma-separated values are
    fine. With -TestFilter 'Category=<name>' and no -Project, only the apps that declare that category run, because an app that runs
    0 tests fails with 94.

.PARAMETER TestFilter
    dotnet/maui syntax: 'Category=<name>' or 'SkipCategories=<name>,<name>'. Defaults to $env:TEST_FILTER. The tests that need a person
    (RequiresUserInteraction) never run here.

.PARAMETER Configuration
    The build configuration. Debug by default, as in the platform scripts; CI runs Release.

.PARAMETER Target
    buildOnly, testOnly or buildAndTest (the default). Build once and test many times with buildOnly and then testOnly.

.PARAMETER Results
    The folder that holds one results folder per platform. Defaults to artifacts/device-tests, where each platform writes
    to <platform>/<app>/.

.PARAMETER BinlogDir
    Where the build logs go. Defaults to artifacts/logs.

.PARAMETER SkipCapabilities
    Capabilities this machine lacks, such as Camera: their tests are skipped (see eng/devices/README.md). Defaults to
    $env:DEVICE_TEST_SKIP_CAPABILITIES.

.PARAMETER RequireCapabilities
    Capabilities this machine must offer: their tests fail instead of skipping without them. Defaults to
    $env:DEVICE_TEST_REQUIRE_CAPABILITIES.

.PARAMETER Timeout
    The limit for one app's test run, as hh:mm:ss. The platform scripts' default is 00:30:00.

.PARAMETER InfraRetries
    Retries for infrastructure failures only. The platform scripts' default is 0 locally.

.PARAMETER Avd
    Android: boot this AVD instead of the one the script picks (the newest API level that this machine's CPU can run).

.PARAMETER EmulatorPort
    Android: the port of the emulator that is booted for -Avd or for the picked AVD. Defaults to the first free one.

.PARAMETER DeviceId
    Android: run on this attached device or emulator (adb serial), also a physical device.

.PARAMETER Udid
    iOS: use this simulator instead of creating one. The scripts grant nothing on it (see eng/devices/LOCAL.md).

.PARAMETER Xcode
    iOS and Mac Catalyst: the Xcode.app to use instead of the one that xcode-select -p selects.

.PARAMETER KeepDevice
    Keep what the scripts started: the created iOS simulator, the booted AVD, the installed packaged Windows app.

.PARAMETER BuildArgs
    Extra arguments for dotnet build and publish; write properties as /p:Name=Value.

.PARAMETER DryRun
    Prints the plan and runs nothing. Also available as -WhatIf.

.EXAMPLE
    pwsh eng/devices/run.ps1
    Every platform this machine can run, all three apps.

.EXAMPLE
    pwsh eng/devices/run.ps1 -DryRun
    Shows what the first example would run.

.EXAMPLE
    pwsh eng/devices/run.ps1 -Platform ios,maccatalyst -TestFilter 'Category=IconTintColorBehavior'
    One category on the Apple platforms; it runs in the app that declares it.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File eng\devices\run.ps1 -Platform unpackaged -Project MediaElement
    The MediaElement app, unpackaged, on Windows.
#>
[CmdletBinding()]
param(
    [string[]]$Platform,

    [string[]]$Project = @('All'),

    [string]$TestFilter,

    [string]$Configuration = 'Debug',

    [ValidateSet('buildOnly', 'testOnly', 'buildAndTest')]
    [string]$Target = 'buildAndTest',

    [string]$Results,

    [string]$BinlogDir,

    [string[]]$SkipCapabilities,

    [string[]]$RequireCapabilities,

    [string]$Timeout,

    [int]$InfraRetries,

    [string]$Avd,

    [int]$EmulatorPort,

    [string]$DeviceId,

    [string]$Udid,

    [string]$Xcode,

    [switch]$KeepDevice,

    [string[]]$BuildArgs = @(),

    [Alias('WhatIf')]
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'devices-shared.ps1')

# The order is the order in which the platforms run. The packaged Windows run comes first: when this shell is not elevated and the signing
# certificate is not trusted yet, it fails within seconds instead of after the unpackaged run.
$script:RunPlatforms = [ordered]@{
    'ios'                = @{ Title = 'iOS simulator'; ScriptName = 'ios.ps1'; OperatingSystem = 'macos' }
    'maccatalyst'        = @{ Title = 'Mac Catalyst'; ScriptName = 'catalyst.ps1'; OperatingSystem = 'macos' }
    'windows-packaged'   = @{ Title = 'Windows, packaged (MSIX)'; ScriptName = 'windows.ps1'; OperatingSystem = 'windows' }
    'windows-unpackaged' = @{ Title = 'Windows, unpackaged'; ScriptName = 'windows.ps1'; OperatingSystem = 'windows' }
    'android'            = @{ Title = 'Android emulator'; ScriptName = 'android.ps1'; OperatingSystem = 'any' }
}

$script:RunPlatformAliases = @{
    'catalyst'   = @('maccatalyst')
    'packaged'   = @('windows-packaged')
    'unpackaged' = @('windows-unpackaged')
    'windows'    = @('windows-packaged', 'windows-unpackaged')
}

$script:RunOperatingSystemNames = @{ 'macos' = 'macOS'; 'windows' = 'Windows'; 'linux' = 'Linux' }

$script:RunArgumentOrder = @('Packaging', 'Target', 'Project', 'Configuration', 'TestFilter', 'Results', 'BinlogDir', 'SkipCapabilities', 'RequireCapabilities',
    'Timeout', 'InfraRetries', 'Avd', 'EmulatorPort', 'DeviceId', 'Udid', 'Xcode', 'KeepDevice', 'BuildArgs')

# Emulator images that are not for phones and tablets: the apps are not made for them.
$script:RunUnsuitableAvdTags = 'wear|tv|automotive|desktop|xr'

function Get-RunOperatingSystem {
    if ($script:DeviceTestsOnWindows) {
        return 'windows'
    }

    if ($IsMacOS) {
        return 'macos'
    }

    return 'linux'
}

function Resolve-RunPlatformNames {
    # Comma-separated values are accepted because "pwsh -File" passes "-Platform a,b" as a single string.
    param([string[]]$Value)

    $requested = New-Object System.Collections.Generic.List[string]
    foreach ($entry in $Value) {
        foreach ($token in ($entry -split ',')) {
            $name = $token.Trim().ToLowerInvariant()
            if ($name.Length -eq 0) {
                continue
            }

            $ids = @()
            if ($script:RunPlatforms.Contains($name)) {
                $ids = @($name)
            }
            elseif ($script:RunPlatformAliases.ContainsKey($name)) {
                $ids = $script:RunPlatformAliases[$name]
            }
            else {
                throw (New-Object System.ArgumentException "Unknown platform '$($token.Trim())'. Use one or more of: ios, maccatalyst (catalyst), android, windows-packaged (packaged), windows-unpackaged (unpackaged), windows.")
            }

            foreach ($id in $ids) {
                if (-not $requested.Contains($id)) {
                    $requested.Add($id)
                }
            }
        }
    }

    return $requested.ToArray()
}

function Format-RunPath {
    param([string]$Path)

    $root = $script:DeviceTestsRepoRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if ($Path.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($root.Length)
    }

    return $Path
}

function Format-RunArgument {
    param($Value)

    $formatted = foreach ($item in @($Value)) {
        if ("$item" -match '^[A-Za-z0-9_./\\:=+@][A-Za-z0-9_./\\:=+@-]*$') {
            "$item"
        }
        else {
            "'" + "$item".Replace("'", "''") + "'"
        }
    }

    return ($formatted -join ',')
}

function Format-RunCommandLine {
    param([string]$ScriptName, [hashtable]$Arguments)

    $parts = @("eng/devices/$ScriptName")
    foreach ($name in $script:RunArgumentOrder) {
        if (-not $Arguments.ContainsKey($name)) {
            continue
        }

        $value = $Arguments[$name]
        if ($value -is [System.Management.Automation.SwitchParameter] -or $value -is [bool]) {
            if ($value) {
                $parts += "-$name"
            }

            continue
        }

        if ($name -eq 'BuildArgs' -and @($value).Count -eq 0) {
            continue
        }

        $parts += "-$name $(Format-RunArgument $value)"
    }

    return ($parts -join ' ')
}

function Test-RunProjectDeclaresCategory {
    # The categories of an app are the constants of its TestCategory*.cs files (src/<app>/**/TestCategory*.cs).
    param($Project, [string]$Category)

    $files = @(Get-ChildItem -LiteralPath $Project.Directory -Filter 'TestCategory*.cs' -File)
    # bin and obj are left out: a built app has thousands of files there.
    foreach ($directory in @(Get-ChildItem -LiteralPath $Project.Directory -Directory | Where-Object { $_.Name -ne 'bin' -and $_.Name -ne 'obj' })) {
        $files += @(Get-ChildItem -LiteralPath $directory.FullName -Recurse -Filter 'TestCategory*.cs' -File)
    }

    $pattern = '\bconst\s+string\s+' + [regex]::Escape($Category) + '\s*='
    foreach ($file in $files) {
        if (Select-String -LiteralPath $file.FullName -Pattern $pattern -Quiet) {
            return $true
        }
    }

    return $false
}

function Select-RunProjectsForFilter {
    # A 'Category=' filter selects the tests of one app. An app that declares no such category runs 0 tests and fails with 94, so the
    # apps that do not declare it are left out when the person did not choose the apps.
    param($Projects, [string]$Filter)

    if (-not $Filter.StartsWith('Category=', [System.StringComparison]::Ordinal)) {
        return $Projects
    }

    $category = $Filter.Substring('Category='.Length)
    $declaring = @($Projects | Where-Object { Test-RunProjectDeclaresCategory -Project $_ -Category $category })
    if ($declaring.Count -eq 0) {
        throw (New-Object System.ArgumentException "No device test app declares the category '$category' (a constant of TestCategory in src/<app>/**/TestCategory*.cs).")
    }

    return $declaring
}

function Get-RunXcodeProblem {
    # The command line tools alone cannot build or run the iOS and Mac Catalyst apps.
    param([string]$Xcode)

    try {
        $root = Get-DeviceTestXcodeRoot -Xcode $Xcode
    }
    catch {
        return "xcode-select did not answer: $($_.Exception.Message)"
    }

    if ($Xcode -and -not (Test-Path -LiteralPath $root -PathType Container)) {
        return "-Xcode '$Xcode' does not exist."
    }

    $developerDirectory = ''
    if ($root) {
        $root = $root.TrimEnd('/')
        $developerDirectory = Join-Path $root (Join-Path 'Contents' 'Developer')
    }

    if (-not $root -or -not $root.EndsWith('.app') -or -not (Test-Path -LiteralPath $developerDirectory -PathType Container)) {
        return 'Xcode is not selected (xcode-select -p does not point into an Xcode.app). Install Xcode and run: sudo xcode-select -s /Applications/Xcode.app'
    }

    return ''
}

function Test-RunIosRuntimeInstalled {
    # $null when simctl does not answer: that does not say that no runtime is installed, and ios.ps1 says so if there is none.
    try {
        $listing = Invoke-DeviceTestProcess -FilePath 'xcrun' -ArgumentList @('simctl', 'list', '-j', 'runtimes') -TimeoutSeconds 180
        if ($listing.TimedOut -or $listing.ExitCode -ne 0) {
            return $null
        }

        $runtimes = @(($listing.Output | ConvertFrom-Json).runtimes | Where-Object { $_.platform -eq 'iOS' -and $_.isAvailable })
        return $runtimes.Count -gt 0
    }
    catch {
        return $null
    }
}

function Get-RunAvdRoot {
    if ($env:ANDROID_AVD_HOME) {
        return $env:ANDROID_AVD_HOME
    }

    if ($env:ANDROID_USER_HOME) {
        return Join-Path $env:ANDROID_USER_HOME 'avd'
    }

    $userFolder = $HOME
    if ($script:DeviceTestsOnWindows) {
        $userFolder = $env:USERPROFILE
    }

    return Join-Path $userFolder (Join-Path '.android' 'avd')
}

function Read-RunIniFile {
    param([string]$Path)

    $values = @{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $values
    }

    foreach ($line in (Get-Content -LiteralPath $Path)) {
        $separator = $line.IndexOf('=')
        if ($separator -gt 0 -and -not $line.TrimStart().StartsWith('#')) {
            $values[$line.Substring(0, $separator).Trim()] = $line.Substring($separator + 1).Trim()
        }
    }

    return $values
}

function Get-RunAvds {
    # An AVD is a <name>.ini file in the AVD folder that points to the folder of its config.ini. Reading the files lists the AVDs without
    # starting the emulator tool.
    param([string]$Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return
    }

    foreach ($file in (Get-ChildItem -LiteralPath $Root -Filter '*.ini' -File)) {
        $pointer = Read-RunIniFile $file.FullName
        $directory = $pointer['path']
        if (-not $directory -or -not (Test-Path -LiteralPath $directory -PathType Container)) {
            $directory = Join-Path $Root "$($file.BaseName).avd"
        }

        $config = Read-RunIniFile (Join-Path $directory 'config.ini')
        if ($config.Count -eq 0) {
            continue
        }

        # The API level can be fractional (android-36.1) and is absent from preview images (android-Baklava).
        $api = [version]'0.0'
        $apiText = 'unknown'
        if ($config['image.sysdir.1'] -match 'android-(\d+(?:\.\d+)?)') {
            $apiText = $Matches[1]
            $level = $apiText
            if ($level -notmatch '\.') {
                $level = "$level.0"
            }

            $api = [version]$level
        }

        New-Object PSObject -Property @{ Name = $file.BaseName; Api = $api; ApiText = $apiText; Abi = $config['abi.type']; Tag = $config['tag.id'] }
    }
}

function Get-RunHostAbis {
    # An emulator cannot run the image of another CPU architecture than the host's.
    if ((Get-DeviceTestHostArchitecture) -eq 'arm64') {
        return @('arm64-v8a')
    }

    return @('x86_64', 'x86')
}

function Select-RunAvd {
    param($Avds, [string[]]$HostAbis)

    $suitable = @($Avds | Where-Object { $HostAbis -contains $_.Abi -and $_.Tag -notmatch $script:RunUnsuitableAvdTags })
    if ($suitable.Count -eq 0) {
        return $null
    }

    # An AVD named like the one in eng/devices/README.md wins, then the newest API level.
    return $suitable |
        Sort-Object @{ Expression = { $_.Name -like 'CTM_DeviceTests*' }; Descending = $true }, @{ Expression = { $_.Api }; Descending = $true }, @{ Expression = { $_.Name }; Descending = $false } |
        Select-Object -First 1
}

function Get-RunFreeEmulatorPort {
    param($Devices)

    $used = @($Devices | ForEach-Object { $_.Serial })
    for ($port = 5554; $port -le 5584; $port += 2) {
        if (-not ($used -contains "emulator-$port")) {
            return $port
        }
    }

    return 5554
}

function New-RunPlanEntry {
    param([string]$Id, [bool]$Available, [string]$Detail, [hashtable]$Arguments = @{})

    $definition = $script:RunPlatforms[$Id]
    return New-Object PSObject -Property @{
        Id         = $Id
        Title      = $definition.Title
        ScriptName = $definition.ScriptName
        Available  = $Available
        Detail     = $Detail
        Arguments  = $Arguments
    }
}

function Get-RunAppleEntry {
    param([string]$Id, [string]$Xcode, [string]$Udid, [bool]$KeepDevice)

    $problem = Get-RunXcodeProblem -Xcode $Xcode
    if ($problem) {
        return New-RunPlanEntry -Id $Id -Available $false -Detail $problem
    }

    $arguments = @{}
    if ($Xcode) {
        $arguments['Xcode'] = $Xcode
    }

    if ($Id -eq 'maccatalyst') {
        return New-RunPlanEntry -Id $Id -Available $true -Detail 'runs on this Mac; the app opens a window while it runs' -Arguments $arguments
    }

    if ($KeepDevice) {
        $arguments['KeepDevice'] = $true
    }

    if ($Udid) {
        $arguments['Udid'] = $Udid
        return New-RunPlanEntry -Id $Id -Available $true -Detail "runs on simulator $Udid; nothing is granted on it" -Arguments $arguments
    }

    if ((Test-RunIosRuntimeInstalled) -eq $false) {
        return New-RunPlanEntry -Id $Id -Available $false -Detail 'no iOS simulator runtime is installed. Install one in Xcode (Settings > Components).'
    }

    $detail = 'creates a simulator (newest iOS runtime, newest iPhone) and deletes it afterwards'
    if ($KeepDevice) {
        $detail = 'creates a simulator (newest iOS runtime, newest iPhone) and keeps it'
    }

    return New-RunPlanEntry -Id $Id -Available $true -Detail $detail -Arguments $arguments
}

function Get-RunAndroidEntry {
    param([string]$Avd, [int]$EmulatorPort, [string]$DeviceId, [bool]$KeepDevice)

    $sdkRoot = Get-AndroidSdkRoot
    if (-not $sdkRoot) {
        return New-RunPlanEntry -Id 'android' -Available $false -Detail 'the Android SDK was not found. Install it and set ANDROID_HOME (eng/devices/README.md, Prerequisites).'
    }

    $adb = Get-AndroidTool -SdkRoot $sdkRoot -Folder 'platform-tools' -Name 'adb'
    if (-not (Test-Path -LiteralPath $adb -PathType Leaf)) {
        return New-RunPlanEntry -Id 'android' -Available $false -Detail "adb was not found in $sdkRoot (install the SDK's platform-tools)."
    }

    if ($DeviceId -and -not $Avd) {
        return New-RunPlanEntry -Id 'android' -Available $true -Detail "runs on device $DeviceId" -Arguments @{ DeviceId = $DeviceId }
    }

    $devices = Get-AndroidDevices -Adb $adb
    # A device that nobody named is not touched, as eng/devices/README.md promises; an emulator that is running is.
    $emulators = @($devices | Where-Object { $_.State -eq 'device' -and $_.Serial -like 'emulator-*' })
    if (-not $Avd -and $emulators.Count -gt 0) {
        $serial = $emulators[0].Serial
        $detail = "runs on the emulator that is already running ($serial) and leaves it running"
        if ($emulators.Count -gt 1) {
            $detail = "$detail; it is the first of $($emulators.Count) running emulators, choose another with -DeviceId"
        }

        return New-RunPlanEntry -Id 'android' -Available $true -Detail $detail -Arguments @{ DeviceId = $serial }
    }

    $avds = @(Get-RunAvds -Root (Get-RunAvdRoot))
    $picked = $null
    if ($Avd) {
        $picked = $avds | Where-Object { $_.Name -eq $Avd } | Select-Object -First 1
        if ($null -eq $picked) {
            $names = ($avds | ForEach-Object { $_.Name }) -join ', '
            return New-RunPlanEntry -Id 'android' -Available $false -Detail "the AVD '$Avd' does not exist (AVDs: $names)."
        }
    }
    else {
        $picked = Select-RunAvd -Avds $avds -HostAbis (Get-RunHostAbis)
    }

    if ($null -eq $picked) {
        if ($avds.Count -eq 0) {
            return New-RunPlanEntry -Id 'android' -Available $false -Detail 'no emulator is running and no AVD exists. Create an AVD (eng/devices/README.md, Quick start) or start an emulator.'
        }

        $abis = (Get-RunHostAbis) -join ' or '
        return New-RunPlanEntry -Id 'android' -Available $false -Detail "no emulator is running and none of the AVDs ($(($avds | ForEach-Object { $_.Name }) -join ', ')) is a phone or tablet image for this CPU ($abis)."
    }

    $port = $EmulatorPort
    if ($port -le 0) {
        $port = Get-RunFreeEmulatorPort -Devices $devices
    }

    $arguments = @{ Avd = $picked.Name; EmulatorPort = $port }
    $detail = "boots the AVD $($picked.Name) (API $($picked.ApiText), $($picked.Abi)) as emulator-$port and stops it afterwards"
    if ($KeepDevice) {
        $arguments['KeepDevice'] = $true
        $detail = "boots the AVD $($picked.Name) (API $($picked.ApiText), $($picked.Abi)) as emulator-$port and keeps it running"
    }

    return New-RunPlanEntry -Id 'android' -Available $true -Detail $detail -Arguments $arguments
}

function Get-RunWindowsEntry {
    param([string]$Id, [bool]$KeepDevice)

    $arguments = @{}
    if ($Id -eq 'windows-packaged') {
        $arguments['Packaging'] = 'packaged'
        $detail = 'signs an MSIX with a test certificate, installs it and removes it afterwards; the first run needs an elevated shell to trust the certificate'
        if ($KeepDevice) {
            $arguments['KeepDevice'] = $true
            $detail = 'signs an MSIX with a test certificate and installs it, and keeps it installed; the first run needs an elevated shell to trust the certificate'
        }

        return New-RunPlanEntry -Id $Id -Available $true -Detail $detail -Arguments $arguments
    }

    $arguments['Packaging'] = 'unpackaged'
    return New-RunPlanEntry -Id $Id -Available $true -Detail 'publishes a self-contained app and starts it' -Arguments $arguments
}

function Get-RunPlan {
    param(
        [string]$OperatingSystem,
        [string[]]$Requested,
        [string]$Avd,
        [int]$EmulatorPort,
        [string]$DeviceId,
        [string]$Udid,
        [string]$Xcode,
        [bool]$KeepDevice
    )

    foreach ($id in $script:RunPlatforms.Keys) {
        $needs = $script:RunPlatforms[$id].OperatingSystem
        $supported = $needs -eq 'any' -or $needs -eq $OperatingSystem
        if ($Requested.Count -gt 0) {
            if (-not ($Requested -contains $id)) {
                continue
            }

            if (-not $supported) {
                throw (New-Object System.ArgumentException "-Platform $id needs $($script:RunOperatingSystemNames[$needs]); this machine runs $($script:RunOperatingSystemNames[$OperatingSystem]).")
            }
        }
        elseif (-not $supported) {
            continue
        }

        try {
            switch ($id) {
                'ios' { Get-RunAppleEntry -Id $id -Xcode $Xcode -Udid $Udid -KeepDevice $KeepDevice }
                'maccatalyst' { Get-RunAppleEntry -Id $id -Xcode $Xcode -Udid $Udid -KeepDevice $KeepDevice }
                'android' { Get-RunAndroidEntry -Avd $Avd -EmulatorPort $EmulatorPort -DeviceId $DeviceId -KeepDevice $KeepDevice }
                default { Get-RunWindowsEntry -Id $id -KeepDevice $KeepDevice }
            }
        }
        catch {
            New-RunPlanEntry -Id $id -Available $false -Detail "could not find out whether it can run: $($_.Exception.Message)"
        }
    }
}

function Get-RunResultsRoot {
    param([string]$Id, [string]$Results)

    if ($Results) {
        return Join-Path (Resolve-DeviceTestPath $Results) $Id
    }

    return Join-Path $script:DeviceTestsRepoRoot (Join-Path 'artifacts' (Join-Path 'device-tests' $Id))
}

function Write-RunPlan {
    param($Plan, $Projects, [string]$Filter, [string]$ProjectNote, [string]$Target, [string]$Configuration, [string]$OperatingSystem, [string]$Results, [bool]$IsDryRun)

    $title = 'Device tests: plan'
    if ($IsDryRun) {
        $title = 'Device tests: plan (dry run, nothing is built or started)'
    }

    Write-DeviceTestHeader $title
    Write-Host "Machine:       $([System.Environment]::MachineName), $($script:RunOperatingSystemNames[$OperatingSystem]), $(Get-DeviceTestHostArchitecture)"
    Write-Host "Apps:          $(($Projects | ForEach-Object { $_.Name }) -join ', ')$ProjectNote"
    Write-Host "Configuration: $Configuration ($Target)"
    Write-Host "TestFilter:    $Filter"
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $resultsFolder = Format-RunPath (Split-Path -Parent (Get-RunResultsRoot -Id 'platform' -Results $Results))
    Write-Host "Results:       $resultsFolder$separator<platform>$separator<app>"
    Write-Host ''
    foreach ($entry in $Plan) {
        if ($entry.Available) {
            Write-Host ('RUN   {0,-20} {1}' -f $entry.Id, $entry.Detail)
            Write-Host ('      {0}' -f (Format-RunCommandLine -ScriptName $entry.ScriptName -Arguments $entry.CommandArguments))
        }
        else {
            Write-Host ('SKIP  {0,-20} {1}' -f $entry.Id, $entry.Detail) -ForegroundColor Yellow
        }
    }
}

function Write-RunScriptOutput {
    # Write-Host writes to the information stream. Invoke-RunPlatform redirects that stream to read the ERROR lines of the script for the
    # summary, so the text goes to the host from here, with the colors the script gave it.
    param($Item, $ErrorLines)

    if ($Item -isnot [System.Management.Automation.InformationRecord]) {
        Write-Host "$Item"
        return
    }

    $data = $Item.MessageData
    if ($data -isnot [System.Management.Automation.HostInformationMessage]) {
        Write-Host "$data"
        return
    }

    if ($data.Message -match '^(?:ERROR: |::error[^:]*::)(.*)$') {
        $ErrorLines.Add($Matches[1])
    }

    $arguments = @{ Object = $data.Message; NoNewline = $data.NoNewLine }
    if ($null -ne $data.ForegroundColor -and [int]$data.ForegroundColor -ge 0) {
        $arguments['ForegroundColor'] = $data.ForegroundColor
    }

    if ($null -ne $data.BackgroundColor -and [int]$data.BackgroundColor -ge 0) {
        $arguments['BackgroundColor'] = $data.BackgroundColor
    }

    Write-Host @arguments
}

function Get-RunAppResult {
    # The platform scripts decide their exit codes from the results on disk and keep each app's verdict in gate.json, so the summary reads
    # those verdicts instead of counting the tests again.
    param($Project, [string]$ResultsRoot)

    $folder = Join-Path $ResultsRoot $Project.Name
    $name = $Project.Name
    if ($script:DeviceTestProjects.Contains($Project.Name)) {
        $name = $script:DeviceTestProjects[$Project.Name]
    }

    $result = New-Object PSObject -Property @{
        Project      = $Project.Name
        Name         = $name
        Folder       = $folder
        HasVerdict   = $false
        ExitCode     = 0
        ExitCodeName = ''
        Total        = 0
        Passed       = 0
        Failed       = 0
        Skipped      = 0
        Reasons      = @()
    }

    $gatePath = Join-Path $folder 'gate.json'
    if (-not (Test-Path -LiteralPath $gatePath -PathType Leaf)) {
        return $result
    }

    try {
        $gate = Get-Content -LiteralPath $gatePath -Raw | ConvertFrom-Json
        $result.ExitCode = [int]$gate.ExitCode
        $result.ExitCodeName = [string]$gate.ExitCodeName
        $result.Total = [int]$gate.Total
        $result.Passed = [int]$gate.Passed
        $result.Failed = [int]$gate.Failed + [int]$gate.Errors
        $result.Skipped = [int]$gate.Skipped
        $result.Reasons = @($gate.Reasons | Where-Object { $_ })
        $result.HasVerdict = $true
    }
    catch {
        Write-DeviceTestWarning "Could not read $($gatePath): $($_.Exception.Message)"
    }

    return $result
}

function Remove-RunPreviousVerdicts {
    # A script that ends before it writes its verdicts, for example on a missing certificate, must not leave the summary with the numbers
    # of an earlier run.
    param($Projects, [string]$ResultsRoot)

    foreach ($item in $Projects) {
        Remove-Item -LiteralPath (Join-Path (Join-Path $ResultsRoot $item.Name) 'gate.json') -Force -ErrorAction SilentlyContinue
    }

    Remove-Item -LiteralPath (Join-Path $ResultsRoot 'summary.md') -Force -ErrorAction SilentlyContinue
}

function Invoke-RunPlatform {
    param($Entry, [hashtable]$Arguments, $Projects, [string]$ResultsRoot)

    Write-DeviceTestHeader "$($Entry.Id): $($Entry.Title)"

    if ($Arguments['Target'] -ne 'buildOnly') {
        Remove-RunPreviousVerdicts -Projects $Projects -ResultsRoot $ResultsRoot
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $errorLines = New-Object System.Collections.Generic.List[string]
    $exitCode = 71
    try {
        # A script that ends with exit only ends itself when it is called with &, and the arrays and switches in $Arguments reach it as they are.
        $global:LASTEXITCODE = -1
        & (Join-Path $PSScriptRoot $Entry.ScriptName) @Arguments 6>&1 | ForEach-Object { Write-RunScriptOutput -Item $_ -ErrorLines $errorLines }
        if ($LASTEXITCODE -ge 0) {
            $exitCode = $LASTEXITCODE
        }
    }
    catch {
        $exitCode = Write-DeviceTestException $_
        $errorLines.Add($_.Exception.Message)
    }

    $stopwatch.Stop()
    $apps = @()
    if ($Arguments['Target'] -ne 'buildOnly') {
        foreach ($item in $Projects) {
            $apps += Get-RunAppResult -Project $item -ResultsRoot $ResultsRoot
        }
    }

    $summaryFile = Join-Path $ResultsRoot 'summary.md'
    if (-not (Test-Path -LiteralPath $summaryFile -PathType Leaf)) {
        $summaryFile = ''
    }

    return New-Object PSObject -Property @{
        Id          = $Entry.Id
        Title       = $Entry.Title
        ExitCode    = $exitCode
        Duration    = $stopwatch.Elapsed
        Apps        = $apps
        ErrorLines  = @($errorLines)
        ResultsRoot = $ResultsRoot
        SummaryFile = $summaryFile
    }
}

function New-RunSummaryRow {
    param([string]$Platform, [string]$App, [string]$Result, [string[]]$Counts, [string]$Time, [string]$Folder)

    return New-Object PSObject -Property @{
        Platform = $Platform
        App      = $App
        Result   = $Result
        Total    = $Counts[0]
        Passed   = $Counts[1]
        Failed   = $Counts[2]
        Skipped  = $Counts[3]
        Time     = $Time
        Folder   = $Folder
    }
}

function Get-RunSummaryRows {
    param($PlatformResults, [string]$Target)

    $noCounts = @('-', '-', '-', '-')
    foreach ($result in $PlatformResults) {
        $time = Format-DeviceTestDuration $result.Duration
        $verdicts = @($result.Apps | Where-Object { $_.HasVerdict })

        # Without a verdict per app (a build-only run, or a script that ended before it wrote them) one row says it for the platform.
        if ($Target -eq 'buildOnly' -or $verdicts.Count -eq 0) {
            $word = 'FAIL'
            if ($result.ExitCode -eq 0) {
                $word = 'BUILT'
                if ($Target -ne 'buildOnly') {
                    $word = '?'
                }
            }

            New-RunSummaryRow -Platform $result.Id -App '(all)' -Result $word -Counts $noCounts -Time $time -Folder (Format-RunPath $result.ResultsRoot)
            continue
        }

        $first = $true
        foreach ($app in $result.Apps) {
            $word = 'PASS'
            $counts = @("$($app.Total)", "$($app.Passed)", "$($app.Failed)", "$($app.Skipped)")
            if (-not $app.HasVerdict) {
                $counts = $noCounts
                $word = '?'
                if ($result.ExitCode -ne 0) {
                    $word = 'FAIL'
                }
            }
            elseif ($app.ExitCode -ne 0) {
                $word = 'FAIL'
            }

            $platformCell = ''
            $timeCell = ''
            if ($first) {
                $platformCell = $result.Id
                $timeCell = $time
                $first = $false
            }

            New-RunSummaryRow -Platform $platformCell -App $app.Name -Result $word -Counts $counts -Time $timeCell -Folder (Format-RunPath $app.Folder)
        }
    }
}

function Write-RunSummary {
    param($Plan, $PlatformResults, [string]$Filter, [string]$Configuration, [string]$Target, [System.TimeSpan]$Duration)

    Write-DeviceTestHeader "Device tests summary: $Configuration, TestFilter $Filter"
    $columns = '{0,-19} {1,-13} {2,-6} {3,7} {4,7} {5,7} {6,7}  {7,-7}  {8}'
    Write-Host ($columns -f 'Platform', 'App', 'Result', 'Total', 'Passed', 'Failed', 'Skipped', 'Time', 'Results')
    $rows = @(Get-RunSummaryRows -PlatformResults $PlatformResults -Target $Target)
    foreach ($row in $rows) {
        $color = 'Yellow'
        if ($row.Result -eq 'PASS' -or $row.Result -eq 'BUILT') {
            $color = 'Green'
        }
        elseif ($row.Result -eq 'FAIL') {
            $color = 'Red'
        }

        Write-Host ('{0,-19} {1,-13} ' -f $row.Platform, $row.App) -NoNewline
        Write-Host ('{0,-6}' -f $row.Result) -NoNewline -ForegroundColor $color
        Write-Host (' {0,7} {1,7} {2,7} {3,7}  {4,-7}  {5}' -f $row.Total, $row.Passed, $row.Failed, $row.Skipped, $row.Time, $row.Folder)
    }

    $problems = @()
    foreach ($result in $PlatformResults) {
        if ($result.ExitCode -eq 0) {
            continue
        }

        $details = ''
        if ($result.SummaryFile) {
            $details = " Details: $(Format-RunPath $result.SummaryFile)"
        }

        $failedApps = @($result.Apps | Where-Object { $_.HasVerdict -and $_.ExitCode -ne 0 })
        foreach ($app in $failedApps) {
            $reason = ($app.Reasons | Select-Object -First 3) -join '; '
            $problems += "$($result.Id) / $($app.Name): exit code $($app.ExitCode) ($($app.ExitCodeName)). $reason$details"
        }

        if ($failedApps.Count -eq 0) {
            $reason = ($result.ErrorLines | Select-Object -First 2) -join ' '
            if (-not $reason) {
                $reason = 'The script printed no error; see its output above.'
            }

            $problems += "$($result.Id): exit code $($result.ExitCode) ($(Get-DeviceTestExitCodeName $result.ExitCode)). $reason$details"
        }
    }

    $passed = @($rows | Where-Object { $_.Result -eq 'PASS' -or $_.Result -eq 'BUILT' }).Count
    $verb = 'passed'
    if ($Target -eq 'buildOnly') {
        $verb = 'built'
    }

    $skipped = @($Plan | Where-Object { -not $_.Available })
    $totals = "$passed of $($rows.Count) $verb in $(Format-DeviceTestDuration $Duration)"

    if ($skipped.Count -gt 0) {
        $totals = "$totals, $($skipped.Count) platform(s) skipped"
    }

    Write-Host ''
    Write-Host $totals
    if ($problems.Count -gt 0) {
        Write-Host ''
        Write-Host 'Failed:' -ForegroundColor Red
        foreach ($problem in $problems) {
            Write-Host "  $problem"
        }
    }

    if ($skipped.Count -gt 0) {
        Write-Host ''
        Write-Host 'Skipped, this machine cannot run them:' -ForegroundColor Yellow
        foreach ($entry in $skipped) {
            Write-Host "  $($entry.Id): $($entry.Detail)"
        }
    }
}

$exitCode = 71
try {
    Assert-DeviceTestCommand 'dotnet'

    $operatingSystem = Get-RunOperatingSystem
    $projects = Resolve-DeviceTestProjects -Project $Project
    $filter = Resolve-DeviceTestFilter -TestFilter $TestFilter -IsBound $PSBoundParameters.ContainsKey('TestFilter')

    $projectArguments = $Project
    $projectNote = ''
    if (-not $PSBoundParameters.ContainsKey('Project')) {
        $declaring = @(Select-RunProjectsForFilter -Projects $projects -Filter $filter)
        if ($declaring.Count -lt $projects.Count) {
            $projects = $declaring
            $projectArguments = @($projects | ForEach-Object { $_.Name })
            $projectNote = " (the apps that declare the category; -Project chooses others)"
        }
    }

    $requested = @()
    if ($PSBoundParameters.ContainsKey('Platform')) {
        $requested = @(Resolve-RunPlatformNames -Value $Platform)
    }

    $plan = @(Get-RunPlan -OperatingSystem $operatingSystem -Requested $requested -Avd $Avd -EmulatorPort $EmulatorPort -DeviceId $DeviceId `
        -Udid $Udid -Xcode $Xcode -KeepDevice $KeepDevice.IsPresent)

    $common = @{ Target = $Target; Project = $projectArguments; Configuration = $Configuration }
    foreach ($name in @('TestFilter', 'BinlogDir', 'SkipCapabilities', 'RequireCapabilities', 'Timeout', 'InfraRetries', 'BuildArgs')) {
        if ($PSBoundParameters.ContainsKey($name)) {
            $common[$name] = $PSBoundParameters[$name]
        }
    }

    foreach ($entry in $plan) {
        $arguments = @{}
        foreach ($name in $common.Keys) {
            $arguments[$name] = $common[$name]
        }

        foreach ($name in $entry.Arguments.Keys) {
            $arguments[$name] = $entry.Arguments[$name]
        }

        if ($PSBoundParameters.ContainsKey('Results')) {
            $arguments['Results'] = Get-RunResultsRoot -Id $entry.Id -Results $Results
        }

        $entry | Add-Member -NotePropertyName 'CommandArguments' -NotePropertyValue $arguments
    }

    Write-RunPlan -Plan $plan -Projects $projects -Filter $filter -ProjectNote $projectNote -Target $Target -Configuration $Configuration `
        -OperatingSystem $operatingSystem -Results $Results -IsDryRun $DryRun.IsPresent

    $runnable = @($plan | Where-Object { $_.Available })
    $blocked = @($plan | Where-Object { -not $_.Available -and $requested -contains $_.Id })
    if ($blocked.Count -gt 0) {
        $exitCode = 71
        if ($blocked[0].Id -eq 'android') {
            $exitCode = 81
        }

        Write-DeviceTestError "Cannot run what -Platform names: $(($blocked | ForEach-Object { "$($_.Id) ($($_.Detail))" }) -join '; ')"
    }
    elseif ($runnable.Count -eq 0) {
        $exitCode = 71
        Write-DeviceTestError 'Nothing to run: this machine can run none of the platforms (see the reasons above).'
    }
    elseif ($DryRun) {
        $exitCode = 0
    }
    else {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $platformResults = @()
        foreach ($entry in $runnable) {
            $resultsRoot = Get-RunResultsRoot -Id $entry.Id -Results $Results
            $platformResults += Invoke-RunPlatform -Entry $entry -Arguments $entry.CommandArguments -Projects $projects -ResultsRoot $resultsRoot
        }

        Write-RunSummary -Plan $plan -PlatformResults $platformResults -Filter $filter -Configuration $Configuration -Target $Target -Duration $stopwatch.Elapsed

        $exitCode = 0
        foreach ($result in $platformResults) {
            if ($result.ExitCode -ne 0 -and $exitCode -eq 0) {
                $exitCode = $result.ExitCode
            }
        }
    }
}
catch {
    $exitCode = Write-DeviceTestException $_
}

Write-Host "Exit code: $exitCode ($(Get-DeviceTestExitCodeName $exitCode))"
exit $exitCode
