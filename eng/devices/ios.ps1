<#
.SYNOPSIS
    Builds the CommunityToolkit.Maui device test apps for the iOS simulator and runs them with XHarness.

.DESCRIPTION
    Mirrors dotnet/maui's eng/devices/ios.cake: 'dotnet build -f <tfm> -p:RuntimeIdentifier=iossimulator-<arch>
    -p:CodesignRequireProvisioningProfile=false', then 'xharness apple test --target ios-simulator-64
    --set-env TestFilter=<filter>'. After every run the results gate in devices-shared.ps1 decides the exit code,
    because xharness reports SUCCESS when the app dies before its test runner starts.

    Without -Udid the script creates a dedicated simulator, boots it, grants the camera, the microphone and speech
    recognition to the test apps on it (and checks every grant in its TCC database), and deletes it afterwards unless
    -KeepDevice. A simulator that cannot be created, booted or given the permissions is replaced as often as
    infrastructure failures are retried (-InfraRetries). It never resets, shuts down, deletes or grants anything on a
    simulator it did not create, and never passes --reset-simulator to xharness (that kills every simulator on the
    Mac). Every simctl call has a timeout, and a simctl failure is a simulator failure (88), never a script error.

    The device type of the new simulator is always one that its runtime lists as supported, because 'simctl create'
    refuses any other. Without -DeviceType it is the runtime's first iPhone with one built-in screen: the status bar
    tests read the status bar from the top of the screen, and iPhone Duo (two screens) shows it on their right edge.
    Without -Runtime, the runtime is the newest one that lists such an iPhone (with -DeviceType, the newest one that
    lists that device type). If the runtime lists no iPhone known to have one built-in screen, the simulator is its
    first iPhone whose screens could not be counted, else its first iPhone, with a warning; without -Runtime this
    happens only when no runtime lists such an iPhone, and the runtime is then the newest one that lists any iPhone.

    Exit codes use xharness's vocabulary: 0 passed, 1 test failures, 3 invalid arguments, 4 app not built,
    70 timed out, 71 build or script failure, 80 app crash, 88 simulator failure, 94 no results or no tests ran.
    See eng/devices/README.md.

.EXAMPLE
    pwsh eng/devices/ios.ps1

.EXAMPLE
    pwsh eng/devices/ios.ps1 -Target testOnly -Project Maui -Udid 01234567-89AB-CDEF-0123-456789ABCDEF -TestFilter 'Category=IconTintColorBehavior'
#>
[CmdletBinding()]
param(
    [ValidateSet('buildOnly', 'testOnly', 'buildAndTest')]
    [string]$Target = 'buildAndTest',

    # Device test app(s): CommunityToolkit.Maui.DeviceTests (default), MediaElement, Camera, All, or a .csproj path.
    [string[]]$Project = @('CommunityToolkit.Maui.DeviceTests'),

    [string]$Configuration = 'Debug',

    # Defaults to artifacts/device-tests/ios; the app writes to a sub-folder named after the project.
    [string]$Results,

    # Defaults to artifacts/logs.
    [string]$BinlogDir,

    # dotnet/maui syntax: 'Category=<name>' or 'SkipCategories=<name>,<name>'. Defaults to $env:TEST_FILTER.
    [string]$TestFilter,

    # Capabilities this machine lacks, such as VideoRendering: their tests are skipped (DeviceTestCapabilities). Defaults to
    # $env:DEVICE_TEST_SKIP_CAPABILITIES, which only the CI jobs set.
    [string[]]$SkipCapabilities,

    # Capabilities this machine must offer, such as MediaPlayback: their tests fail instead of skipping when it does not. Defaults to
    # $env:DEVICE_TEST_REQUIRE_CAPABILITIES, which only the CI jobs set.
    [string[]]$RequireCapabilities,

    [string]$Timeout = '00:30:00',

    [string]$LaunchTimeout = '00:10:00',

    # Retries for infrastructure failures only. Defaults to 1 on CI and 0 locally.
    [int]$InfraRetries = -1,

    # Collect code coverage of the toolkit assemblies with coverlet: the app is built with instrumented assemblies (a -Target testOnly run
    # builds it too), and <Results>/coverage/<Results folder name>.cobertura.xml gets the report. Defaults to $env:DEVICE_TEST_COVERAGE,
    # which only the CI jobs set.
    [switch]$Coverage,

    # Use this existing simulator instead of creating one.
    [string]$Udid,

    # Simulator device type for a new simulator: identifier, short form (iPhone-17) or name ('iPhone 17').
    [string]$DeviceType,

    # Simulator runtime for a new simulator: identifier, short form (iOS-27-0) or version (27.0).
    [string]$Runtime,

    [string]$SimulatorName,

    # Xcode.app to use. Defaults to the one selected with xcode-select.
    [string]$Xcode,

    [int]$BootTimeoutSeconds = 600,

    # Keep a simulator created by this script.
    [switch]$KeepDevice,

    # Extra arguments for 'dotnet build', for example '/p:Name=Value' (a value starting with -p: is read as a parameter name).
    [string[]]$BuildArgs = @(),

    # Extra arguments for xharness, for example '--set-env=NAME=value'.
    [string[]]$XHarnessArgs = @()
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'devices-shared.ps1')
. (Join-Path $PSScriptRoot 'devices-coverage.ps1')

# simctl talks to CoreSimulatorService, which can take minutes to answer on a busy CI runner (3 cores): the first device listing loads every
# simulator of the runner image, and a create or a delete waits for the service. Every simctl call therefore has a timeout, and no simctl
# failure ends the run with a script error: a simulator that cannot be prepared is an infrastructure verdict (88) for the app.
function Invoke-Simctl {
    param([string[]]$ArgumentList, [int]$TimeoutSeconds = 120)

    return Invoke-DeviceTestProcess -FilePath 'xcrun' -ArgumentList (@('simctl') + $ArgumentList) -TimeoutSeconds $TimeoutSeconds
}

function Format-SimctlFailure {
    # A call that was killed at its timeout has no exit code and usually printed nothing.
    param([string]$Command, $Result, [int]$TimeoutSeconds)

    if ($Result.TimedOut) {
        return "xcrun simctl $Command did not answer within $TimeoutSeconds s"
    }

    return "xcrun simctl $Command failed ($($Result.ExitCode)): $("$($Result.Error)".Trim())"
}

function Get-SimctlJson {
    # Each try gets the 6 minutes that XHarness gives its own simulator listing (SimulatorLoader).
    param([string]$Kind, [int]$TimeoutSeconds = 360, [int]$Attempts = 2)

    $failure = $null
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        $listing = Invoke-Simctl -ArgumentList @('list', '-j', $Kind) -TimeoutSeconds $TimeoutSeconds
        if ($listing.ExitCode -eq 0) {
            try {
                return ($listing.Output | ConvertFrom-Json)
            }
            catch {
                $failure = "xcrun simctl list -j $Kind printed no valid JSON: $($_.Exception.Message)"
            }
        }
        else {
            $failure = Format-SimctlFailure -Command "list -j $Kind" -Result $listing -TimeoutSeconds $TimeoutSeconds
        }

        if ($attempt -lt $Attempts) {
            Write-DeviceTestWarning "$failure; listing again."
        }
    }

    throw $failure
}

function Format-SimulatorDescription {
    param([string]$Name, [string]$DeviceTypeIdentifier, [string]$RuntimeIdentifier, [string]$Identifier)

    return "$Name, $($DeviceTypeIdentifier -replace '^.*\.', ''), $($RuntimeIdentifier -replace '^.*\.', ''), $Identifier"
}

function Find-SimulatorDevice {
    param([string]$Identifier)

    $devices = Get-SimctlJson -Kind 'devices'
    foreach ($runtimeDevices in $devices.devices.PSObject.Properties) {
        foreach ($device in $runtimeDevices.Value) {
            if ($device.udid -eq $Identifier) {
                $device | Add-Member -NotePropertyName 'runtimeIdentifier' -NotePropertyValue $runtimeDevices.Name -Force
                return $device
            }
        }
    }

    return $null
}

function Get-SimulatorRuntimes {
    $runtimes = @((Get-SimctlJson -Kind 'runtimes').runtimes | Where-Object { $_.platform -eq 'iOS' -and $_.isAvailable })
    if ($runtimes.Count -eq 0) {
        throw 'No available iOS simulator runtime. Install one in Xcode (Settings > Components).'
    }

    return $runtimes
}

function Select-SimulatorRuntime {
    # There is no default runtime here: the newest one may not list the device type that is needed (iOS 27.1 lists only iPhone Duo), so
    # Select-SimulatorRuntimeAndDeviceType chooses the runtime and the device type together, and only a requested runtime is resolved here.
    param([string]$Requested)

    $runtimes = @(Get-SimulatorRuntimes)
    foreach ($candidate in $runtimes) {
        if ($candidate.identifier -eq $Requested -or $candidate.identifier.EndsWith(".$Requested") -or $candidate.version -eq $Requested -or $candidate.name -eq $Requested) {
            return $candidate
        }
    }

    throw "iOS runtime '$Requested' is not available. Available: $(($runtimes | ForEach-Object { $_.identifier }) -join ', ')."
}

function Get-SimulatorBuiltInScreenCount {
    param($DeviceType)

    if (-not $DeviceType.bundlePath) {
        return $null
    }

    $capabilities = Join-Path ([string]$DeviceType.bundlePath) 'Contents/Resources/capabilities.plist'
    if (-not (Test-Path -LiteralPath $capabilities -PathType Leaf)) {
        return $null
    }

    $converted = Invoke-DeviceTestNativeCapture -FilePath 'plutil' -ArgumentList @('-convert', 'json', '-o', '-', $capabilities)
    if ($converted.ExitCode -ne 0) {
        return $null
    }

    try {
        $displays = (($converted.Output -join "`n") | ConvertFrom-Json -AsHashtable)['capabilities']['displays']
    }
    catch {
        return $null
    }

    if ($null -eq $displays) {
        return $null
    }

    # The list also has the TV out, CarPlay and resizable scene outputs. iPhone Duo's profile names its screens primary and primary-1.
    return @($displays | Where-Object { $_['displayType'] -eq 'integrated' -or "$($_['deviceName'])" -like 'primary*' }).Count
}

function Get-SimulatorSupportedDeviceTypes {
    # simctl builds supportedDeviceTypes with the same check that 'simctl create' makes, which refuses any other device type (SimError 403,
    # Incompatible device), and a runtime made for particular devices lists only those: the iOS 27.1 runtime of Xcode 27.1 lists only iPhone
    # Duo. A missing or null supportedDeviceTypes lists none, which @($null) alone would count as one.
    param($SelectedRuntime)

    return @($SelectedRuntime.supportedDeviceTypes | Where-Object { $null -ne $_ })
}

function Find-SupportedSimulatorDeviceType {
    param($SelectedRuntime, [string]$Requested)

    foreach ($candidate in (Get-SimulatorSupportedDeviceTypes -SelectedRuntime $SelectedRuntime)) {
        if ($candidate.identifier -eq $Requested -or $candidate.identifier.EndsWith(".$Requested") -or $candidate.name -eq $Requested) {
            return $candidate
        }
    }
}

function Get-SimulatorIPhones {
    # $ScreenCounts is shared between the runtimes, so that plutil reads each device type's profile once.
    param([object[]]$DeviceTypes, [hashtable]$ScreenCounts)

    foreach ($deviceType in $DeviceTypes) {
        if ($deviceType.productFamily -eq 'iPhone') {
            $identifier = [string]$deviceType.identifier
            if (-not $ScreenCounts.ContainsKey($identifier)) {
                $ScreenCounts[$identifier] = Get-SimulatorBuiltInScreenCount -DeviceType $deviceType
            }

            New-Object PSObject -Property @{ DeviceType = $deviceType; Screens = $ScreenCounts[$identifier] }
        }
    }
}

function Format-SimulatorIPhones {
    param([object[]]$IPhones)

    if ($IPhones.Count -eq 0) {
        return 'none'
    }

    $descriptions = foreach ($iPhone in $IPhones) {
        $screens = $iPhone.Screens
        if ($null -eq $screens) {
            $screens = 'unknown'
        }

        "$($iPhone.DeviceType.name) ($screens)"
    }

    return ($descriptions -join ', ')
}

function Get-SimulatorIPhoneCandidates {
    param($SelectedRuntime, [hashtable]$ScreenCounts)

    $supported = @(Get-SimulatorSupportedDeviceTypes -SelectedRuntime $SelectedRuntime)
    $iPhones = @(Get-SimulatorIPhones -DeviceTypes $supported -ScreenCounts $ScreenCounts)
    Write-Host "iPhones for $($SelectedRuntime.name) ($($SelectedRuntime.buildversion)), with their built-in screens: $(Format-SimulatorIPhones $iPhones)"
    return New-Object PSObject -Property @{ Runtime = $SelectedRuntime; IPhones = $iPhones }
}

function Select-SimulatorIPhone {
    param($Candidates)

    $iPhones = $Candidates.IPhones
    if ($iPhones.Count -eq 0) {
        throw "$($Candidates.Runtime.identifier) supports no iPhone device type."
    }

    # The status bar tests read the status bar from the top of the screen. iPhone Duo has two built-in screens and shows the status bar on their
    # right edge.
    foreach ($iPhone in $iPhones) {
        if ($iPhone.Screens -eq 1) {
            return $iPhone.DeviceType
        }

        if ($iPhone.Screens -gt 1) {
            Write-Host "Passing over $($iPhone.DeviceType.name): it has $($iPhone.Screens) built-in screens (-DeviceType selects it)."
        }
    }

    # An iPhone whose screens could not be counted may have one; an iPhone known to have more does not.
    $fallback = (@($iPhones | Where-Object { $null -eq $_.Screens }) + $iPhones)[0]
    Write-DeviceTestWarning "No iPhone for $($Candidates.Runtime.identifier) is known to have one built-in screen: the status bar tests read the status bar from the top of the screen."
    return $fallback.DeviceType
}

function Select-SimulatorDeviceType {
    param($SelectedRuntime, [string]$Requested)

    if ($Requested) {
        $found = Find-SupportedSimulatorDeviceType -SelectedRuntime $SelectedRuntime -Requested $Requested
        if ($null -eq $found) {
            throw "Device type '$Requested' is not supported by $($SelectedRuntime.identifier)."
        }

        return $found
    }

    $candidates = Get-SimulatorIPhoneCandidates -SelectedRuntime $SelectedRuntime -ScreenCounts @{}
    return (Select-SimulatorIPhone -Candidates $candidates)
}

function Select-SimulatorRuntimeAndDeviceType {
    param([string]$Runtime, [string]$DeviceType)

    if ($Runtime) {
        $selectedRuntime = Select-SimulatorRuntime -Requested $Runtime
        return New-Object PSObject -Property @{ Runtime = $selectedRuntime; DeviceType = (Select-SimulatorDeviceType -SelectedRuntime $selectedRuntime -Requested $DeviceType) }
    }

    $runtimes = @(Get-SimulatorRuntimes | Sort-Object { [version]$_.version } -Descending)
    if ($DeviceType) {
        # The newest runtime may not list a device type that an older one lists: the iOS 27.1 runtime of Xcode 27.1 lists only iPhone Duo.
        foreach ($candidateRuntime in $runtimes) {
            $found = Find-SupportedSimulatorDeviceType -SelectedRuntime $candidateRuntime -Requested $DeviceType
            if ($null -ne $found) {
                return New-Object PSObject -Property @{ Runtime = $candidateRuntime; DeviceType = $found }
            }
        }

        throw "Device type '$DeviceType' is not supported by any available iOS runtime ($(($runtimes | ForEach-Object { $_.identifier }) -join ', '))."
    }

    # The status bar tests need an iPhone with one built-in screen, which an older runtime may list when the newest does not: the iOS 27.1
    # runtime of Xcode 27.1 lists only iPhone Duo.
    $screenCounts = @{}
    $examined = @()
    foreach ($candidateRuntime in $runtimes) {
        $candidates = Get-SimulatorIPhoneCandidates -SelectedRuntime $candidateRuntime -ScreenCounts $screenCounts
        if (@($candidates.IPhones | Where-Object { $_.Screens -eq 1 }).Count -gt 0) {
            return New-Object PSObject -Property @{ Runtime = $candidateRuntime; DeviceType = (Select-SimulatorIPhone -Candidates $candidates) }
        }

        Write-Host "$($candidateRuntime.name) ($($candidateRuntime.buildversion)) lists no iPhone with one built-in screen."
        $examined += $candidates
    }

    $chosen = (@($examined | Where-Object { $_.IPhones.Count -gt 0 }) + $examined)[0]
    return New-Object PSObject -Property @{ Runtime = $chosen.Runtime; DeviceType = (Select-SimulatorIPhone -Candidates $chosen) }
}

function Wait-Simulator {
    # 'simctl bootstatus -b' boots the simulator if needed, so there is no separate boot step.
    param([string]$Identifier, [int]$TimeoutSeconds)

    $status = Invoke-Simctl -ArgumentList @('bootstatus', $Identifier, '-b') -TimeoutSeconds $TimeoutSeconds
    if ($status.TimedOut) {
        return "Simulator $Identifier did not finish booting within $TimeoutSeconds s."
    }

    if ($status.ExitCode -ne 0) {
        return (Format-SimctlFailure -Command "bootstatus $Identifier -b" -Result $status -TimeoutSeconds $TimeoutSeconds)
    }

    return $null
}

function Remove-Simulator {
    param([string]$Identifier)

    Write-Host "Deleting simulator $Identifier"
    [void](Invoke-Simctl -ArgumentList @('shutdown', $Identifier))
    $deleted = Invoke-Simctl -ArgumentList @('delete', $Identifier) -TimeoutSeconds 300
    if ($deleted.ExitCode -ne 0) {
        Write-DeviceTestWarning "Could not delete simulator $($Identifier): $(Format-SimctlFailure -Command "delete $Identifier" -Result $deleted -TimeoutSeconds 300)"
    }
}

function New-TestSimulator {
    # The summary's device line comes from what was created, so it needs no device listing.
    param([string]$Name, $DeviceType, $Runtime, [int]$BootTimeoutSeconds, [int]$CreateTimeoutSeconds = 300)

    Write-Host "Creating simulator '$Name' ($($DeviceType.name), $($Runtime.name))"
    $created = Invoke-Simctl -ArgumentList @('create', $Name, $DeviceType.identifier, $Runtime.identifier) -TimeoutSeconds $CreateTimeoutSeconds
    $udid = "$($created.Output)".Trim()
    if ($created.ExitCode -ne 0 -or -not $udid) {
        $simulator = $null
        if ($created.TimedOut) {
            # simctl may have created it all the same. The name is this run's own, so a simulator with it is deleted with the run.
            try {
                $devices = Get-SimctlJson -Kind 'devices' -Attempts 1
                $orphan = @($devices.devices.PSObject.Properties | ForEach-Object { $_.Value } | Where-Object { $_.name -eq $Name }) | Select-Object -First 1
                if ($null -ne $orphan) {
                    $simulator = New-Object PSObject -Property @{ Udid = $orphan.udid; Created = $true; BootedByScript = $true; Name = $Name; Description = '' }
                }
            }
            catch {
                Write-DeviceTestWarning "Could not look for a simulator named '$Name' after its creation timed out: $($_.Exception.Message)"
            }
        }

        return New-Object PSObject -Property @{ Simulator = $simulator; Error = (Format-SimctlFailure -Command 'create' -Result $created -TimeoutSeconds $CreateTimeoutSeconds) }
    }

    $description = Format-SimulatorDescription -Name $Name -DeviceTypeIdentifier $DeviceType.identifier -RuntimeIdentifier $Runtime.identifier -Identifier $udid
    $simulator = New-Object PSObject -Property @{ Udid = $udid; Created = $true; BootedByScript = $true; Name = $Name; Description = $description }
    return New-Object PSObject -Property @{ Simulator = $simulator; Error = (Wait-Simulator -Identifier $udid -TimeoutSeconds $BootTimeoutSeconds) }
}

# The device log of a failed leg is often lost: on iOS 27 launchd_sim kills logd when the app starts and the xharness log stream dies with it.
# The simulator's unified log, its process table and a sample of its media daemons are therefore saved before the simulator is deleted.
$script:SimulatorEvidenceBudgetSeconds = 170
$script:SimulatorSampleBudgetSeconds = 40
$script:AppCrashReportWaitSeconds = 60
$script:SimulatorMediaDaemons = @('mediaplaybackd', 'mediaserverd', 'audiomxd', 'mediaremoted', 'mediaparserd')
$script:SimulatorMediaLogPredicate = 'subsystem BEGINSWITH "com.apple.coremedia" OR subsystem == "com.apple.avfoundation" OR process IN {"mediaplaybackd","mediaserverd","audiomxd","mediaremoted","mediaparserd"}'
$script:SimulatorErrorLogPredicate = 'messageType == error OR messageType == fault'

function Save-SimulatorEvidenceCommand {
    param([string]$Directory, [string]$FileName, [string]$FilePath, [string[]]$ArgumentList, [int]$TimeoutSeconds, [datetime]$Deadline)

    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    try {
        $remaining = [int][Math]::Floor(($Deadline - [datetime]::UtcNow).TotalSeconds)
        if ($remaining -lt 5) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message "$($FileName): skipped, the evidence time budget is used up"
            return $null
        }

        $timeout = [Math]::Min($TimeoutSeconds, $remaining)
        $started = [datetime]::UtcNow
        $result = Invoke-DeviceTestProcess -FilePath $FilePath -ArgumentList $ArgumentList -TimeoutSeconds $timeout
        $text = [string]$result.Output
        if ($result.Error) {
            $text = $text + "`n--- stderr ---`n" + $result.Error
        }

        [System.IO.File]::WriteAllText((Join-Path $Directory $FileName), $text, (New-Object System.Text.UTF8Encoding($false)))
        $seconds = [int][Math]::Round(([datetime]::UtcNow - $started).TotalSeconds)
        if ($result.TimedOut) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message ([string]::Format($invariant, '{0}: timed out after {1} s, {2} chars saved', $FileName, $timeout, $text.Length))
        }
        else {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message ([string]::Format($invariant, '{0}: exit {1}, {2} chars, {3} s', $FileName, $result.ExitCode, $text.Length, $seconds))
        }

        return $result
    }
    catch {
        try {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message "$($FileName): failed: $($_.Exception.Message)"
        }
        catch {
            Write-Verbose "Could not write collect.log: $($_.Exception.Message)"
        }

        return $null
    }
}

function Get-SimulatorProcessLines {
    param([string]$PsOutput, [string]$Udid)

    # Only launchd_sim has the UDID in its command line; the daemons are its descendants and run from the runtime root shared by all simulators.
    $parsed = @()
    foreach ($line in @(([string]$PsOutput) -split "`r?`n" | Select-Object -Skip 1)) {
        $fields = $line.Trim() -split '\s+', 3
        $processId = 0
        $parentId = 0
        if ($fields.Count -ge 3 -and
            [int]::TryParse($fields[0], [System.Globalization.NumberStyles]::None, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$processId) -and
            [int]::TryParse($fields[1], [System.Globalization.NumberStyles]::None, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parentId)) {
            $parsed += [pscustomobject]@{ ProcessId = $processId; ParentId = $parentId; Line = $line }
        }
    }

    $owned = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($entry in $parsed) {
        if ($entry.Line.IndexOf("/Devices/$Udid/", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            [void]$owned.Add($entry.ProcessId)
        }
    }

    $changed = $owned.Count -gt 0
    while ($changed) {
        $changed = $false
        foreach ($entry in $parsed) {
            if ($owned.Contains($entry.ParentId) -and $owned.Add($entry.ProcessId)) {
                $changed = $true
            }
        }
    }

    return @($parsed | Where-Object { $owned.Contains($_.ProcessId) })
}

function Get-HostProcessTable {
    param([string]$Directory, [datetime]$Deadline)

    try {
        $remaining = [int][Math]::Floor(($Deadline - [datetime]::UtcNow).TotalSeconds)
        if ($remaining -lt 5) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message 'ps: skipped, the evidence time budget is used up'
            return $null
        }

        $result = Invoke-DeviceTestProcess -FilePath 'ps' -ArgumentList @('-axo', 'pid,ppid,lstart,etime,command') -TimeoutSeconds ([Math]::Min(40, $remaining))
        if ($result.TimedOut) {
            return $null
        }

        return $result
    }
    catch {
        return $null
    }
}

function Save-MediaDaemonSamples {
    param([string]$Directory, $Owned, [datetime]$Deadline)

    $sampleDeadline = [datetime]::UtcNow.AddSeconds($script:SimulatorSampleBudgetSeconds)
    if ($Deadline -lt $sampleDeadline) {
        $sampleDeadline = $Deadline
    }

    foreach ($daemon in $script:SimulatorMediaDaemons) {
        $matching = @($Owned | Where-Object { $_.Line -cmatch "/$([regex]::Escape($daemon))(\s|$)" })
        if ($matching.Count -eq 0) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message "sample-$($daemon): not running in this simulator"
            continue
        }

        foreach ($entry in $matching) {
            $result = Save-SimulatorEvidenceCommand -Directory $Directory -FileName "sample-$daemon-$($entry.ProcessId).txt" -FilePath 'sample' -ArgumentList @("$($entry.ProcessId)", '3') -TimeoutSeconds 30 -Deadline $sampleDeadline
            # sample prints the path of its own copy in the host's /tmp on stderr, which would litter local Macs.
            if ($null -ne $result -and (([string]$result.Output + "`n" + [string]$result.Error) -match 'written to file ((?:/private)?/tmp/\S+)')) {
                Remove-Item -LiteralPath $Matches[1] -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

function Save-AppCrashReport {
    param([string]$Directory, [string]$Udid, [string]$ExecutableName, [datetime]$LaunchedAfter, [datetime]$Deadline)

    # XHarness stops waiting for crash reports once any new .ips exists, and simulator daemons always write some on iOS 27,
    # so the app's own report can arrive after XHarness has finished looking.
    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    try {
        if (-not $ExecutableName) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message 'app crash report: not looked for, the app executable name is unknown'
            return
        }

        $reportsDirectory = Join-Path $HOME (Join-Path 'Library' (Join-Path 'Logs' 'DiagnosticReports'))
        $started = [datetime]::UtcNow
        $ignored = New-Object 'System.Collections.Generic.HashSet[string]'
        while ($true) {
            $candidates = @(Get-ChildItem -LiteralPath $reportsDirectory -Filter '*.ips' -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Name.StartsWith("$ExecutableName-", [System.StringComparison]::Ordinal) -and $_.LastWriteTimeUtc -gt $LaunchedAfter } |
                Sort-Object LastWriteTimeUtc)
            $copied = 0
            foreach ($report in $candidates) {
                $owner = Get-CrashReportSimulatorId -Directory $reportsDirectory -ReportName $report.Name
                if ($owner -and $owner -ne $Udid) {
                    if ($ignored.Add($report.Name)) {
                        Add-AndroidEvidenceLogLine -Directory $Directory -Message "app crash report: ignored $($report.Name), it belongs to simulator $owner"
                    }

                    continue
                }

                Copy-Item -LiteralPath $report.FullName -Destination (Join-Path $Directory $report.Name) -Force
                Add-AndroidEvidenceLogLine -Directory $Directory -Message ([string]::Format($invariant, 'app crash report: copied {0} of simulator {1} after {2} s', $report.Name, $Udid, [int][Math]::Round(([datetime]::UtcNow - $started).TotalSeconds)))
                $copied++
            }

            if ($copied -gt 0) {
                return
            }

            if ([datetime]::UtcNow.AddSeconds(3) -gt $Deadline) {
                break
            }

            Start-Sleep -Seconds 3
        }

        Add-AndroidEvidenceLogLine -Directory $Directory -Message ([string]::Format($invariant, 'app crash report: no report of {0} for this simulator after {1} s', $ExecutableName, [int][Math]::Round(([datetime]::UtcNow - $started).TotalSeconds)))
    }
    catch {
        try {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message "app crash report: failed: $($_.Exception.Message)"
        }
        catch {
            Write-Verbose "Could not write collect.log: $($_.Exception.Message)"
        }
    }
}

function Save-SimulatorEvidence {
    param([string]$Udid, [string]$ResultsDirectory, [string]$ExecutableName, [datetime]$LaunchedAfter)

    $directory = Join-Path $ResultsDirectory 'device-evidence'
    try {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
        $deadline = [datetime]::UtcNow.AddSeconds($script:SimulatorEvidenceBudgetSeconds)
        $crashReportDeadline = [datetime]::UtcNow.AddSeconds($script:AppCrashReportWaitSeconds)
        Add-AndroidEvidenceLogLine -Directory $directory -Message "collecting evidence of simulator $Udid (budget $($script:SimulatorEvidenceBudgetSeconds) s)"

        # The logs are the main evidence, so they run first and the slower, less certain steps use what is left.
        [void](Save-SimulatorEvidenceCommand -Directory $directory -FileName 'log-media.txt' -FilePath 'xcrun' `
            -ArgumentList @('simctl', 'spawn', $Udid, 'log', 'show', '--last', '60m', '--style', 'compact', '--predicate', $script:SimulatorMediaLogPredicate) -TimeoutSeconds 50 -Deadline $deadline)
        [void](Save-SimulatorEvidenceCommand -Directory $directory -FileName 'log-errors.txt' -FilePath 'xcrun' `
            -ArgumentList @('simctl', 'spawn', $Udid, 'log', 'show', '--last', '60m', '--style', 'compact', '--predicate', $script:SimulatorErrorLogPredicate) -TimeoutSeconds 50 -Deadline $deadline)

        $processes = Get-HostProcessTable -Directory $directory -Deadline $deadline
        if ($null -eq $processes) {
            Add-AndroidEvidenceLogLine -Directory $directory -Message 'processes.txt: not saved, ps failed or timed out'
        }
        else {
            $owned = @(Get-SimulatorProcessLines -PsOutput $processes.Output -Udid $Udid)
            $header = "pid ppid lstart etime command (launchd_sim of this simulator and its descendants with start time and age)`n"
            [System.IO.File]::WriteAllText((Join-Path $directory 'processes.txt'), ($header + (($owned | ForEach-Object { $_.Line }) -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding($false)))
            Add-AndroidEvidenceLogLine -Directory $directory -Message "processes.txt: $($owned.Count) processes of this simulator"
            Save-MediaDaemonSamples -Directory $directory -Owned $owned -Deadline $deadline
        }

        Save-AppCrashReport -Directory $directory -Udid $Udid -ExecutableName $ExecutableName -LaunchedAfter $LaunchedAfter -Deadline $crashReportDeadline
    }
    catch {
        Write-DeviceTestWarning "Collecting simulator evidence failed: $($_.Exception.Message)"
    }
}

function Get-AppExecutableName {
    param([string]$AppBundle)

    try {
        $result = Invoke-DeviceTestProcess -FilePath 'plutil' -ArgumentList @('-extract', 'CFBundleExecutable', 'raw', '-o', '-', (Join-Path $AppBundle 'Info.plist')) -TimeoutSeconds 15
        if ($result.ExitCode -eq 0 -and ([string]$result.Output).Trim()) {
            return ([string]$result.Output).Trim()
        }
    }
    catch {
        Write-Verbose "Could not read CFBundleExecutable: $($_.Exception.Message)"
    }

    return [System.IO.Path]::GetFileNameWithoutExtension($AppBundle.TrimEnd('/'))
}

function Get-AppBundleIdentifiers {
    param($Context)

    foreach ($item in $Context.Projects) {
        $framework = Get-DeviceTestTargetFramework -Project $item -Platform 'ios'
        (Get-DeviceTestMSBuildProperties -ProjectPath $item.Path -Name @('ApplicationId') -Property @("TargetFramework=$framework", "Configuration=$($Context.Configuration)"))['ApplicationId']
    }
}

function Invoke-SimulatorTccQuery {
    param([string]$Database, [string]$Sql, [switch]$Write)

    # No -readonly: it cannot open a WAL database whose -wal and -shm files are missing.
    $arguments = @('-cmd', '.timeout 5000')
    return Invoke-DeviceTestProcess -FilePath 'sqlite3' -ArgumentList ($arguments + @($Database, $Sql)) -TimeoutSeconds 60
}

function Wait-SimulatorTccDatabase {
    # Test-Path comes first: sqlite3 can create a missing file, and an empty one where tccd expects its own would break the permissions.
    param([string]$Database, [int]$TimeoutSeconds = 60)

    $deadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ($true) {
        if (Test-Path -LiteralPath $Database -PathType Leaf) {
            $tables = Invoke-SimulatorTccQuery -Database $Database -Sql "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='access';"
            if ($tables.ExitCode -eq 0 -and "$($tables.Output)".Trim() -eq '1') {
                return $true
            }
        }

        if ([datetime]::UtcNow -ge $deadline) {
            return $false
        }

        Start-Sleep -Seconds 2
    }
}

function Get-SimulatorTccGrantedServices {
    # An object, not a bare array: PowerShell turns a returned empty array into $null.
    param([string]$Database, [string]$Bundle)

    $result = Invoke-SimulatorTccQuery -Database $Database -Sql "SELECT service, auth_value FROM access WHERE client='$Bundle' AND client_type=0;"
    if ($result.ExitCode -ne 0) {
        return [pscustomobject]@{ Services = @(); Error = "Could not read the TCC database: $("$($result.Error)".Trim())" }
    }

    $authValues = @{}
    foreach ($line in @("$($result.Output)" -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
        $fields = $line -split '\|'
        $authValues[$fields[0]] = $fields[1]
    }

    $services = @($authValues.Keys | Where-Object { $authValues[$_] -eq '2' })
    return [pscustomobject]@{ Services = $services; AuthValues = $authValues; Error = $null }
}

function Add-SimulatorTccGrant {
    # The access table's columns differ by iOS version (iOS 15 and 16 have no pid; iOS 27 adds two), so only the columns it has are written.
    param([string]$Database, [string]$Bundle, [string]$TccService)

    if ($Bundle -cnotmatch '^[A-Za-z0-9.-]+\z') {
        return "'$Bundle' is not a valid bundle identifier."
    }

    $columns = Invoke-SimulatorTccQuery -Database $Database -Sql "SELECT name FROM pragma_table_info('access');"
    if ($columns.ExitCode -ne 0) {
        return "Could not read the columns of the TCC database's access table: $("$($columns.Error)".Trim())"
    }

    $tableColumns = @("$($columns.Output)" -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $now = [System.DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $values = [ordered]@{
        service                          = "'$TccService'"
        client                           = "'$Bundle'"
        client_type                      = '0'
        auth_value                       = '2'
        auth_reason                      = '4'
        auth_version                     = '1'
        csreq                            = 'NULL'
        policy_id                        = 'NULL'
        indirect_object_identifier_type  = '0'
        indirect_object_identifier       = "'UNUSED'"
        indirect_object_code_identity    = 'NULL'
        flags                            = '0'
        last_modified                    = "$now"
        pid                              = 'NULL'
        pid_version                      = 'NULL'
        boot_uuid                        = "'UNUSED'"
        last_reminded                    = "$now"
    }

    $columnsToWrite = @($values.Keys | Where-Object { $tableColumns -contains $_ })
    $sql = "INSERT OR REPLACE INTO access ($($columnsToWrite -join ', ')) VALUES ($(($columnsToWrite | ForEach-Object { $values[$_] }) -join ', '));"
    $written = Invoke-SimulatorTccQuery -Database $Database -Sql $sql -Write
    if ($written.ExitCode -ne 0) {
        return "$("$($written.Error)".Trim())"
    }

    return $null
}

function Save-PrivacyGrantEvidence {
    # Best effort: why simctl privacy stops answering is unknown, so a timeout keeps what the host and the simulator were doing.
    param([string]$Udid, [string]$Path)

    try {
        $processes = Invoke-DeviceTestProcess -FilePath 'ps' -ArgumentList @('-axo', 'pid,ppid,etime,command') -TimeoutSeconds 30
        if ($processes.TimedOut -or $processes.ExitCode -ne 0) {
            return
        }

        $lines = @("$($processes.Output)" -split "`r?`n")
        $hostLines = @($lines | Where-Object { $_ -match 'simctl|CoreSimulatorService' })
        $owned = @(Get-SimulatorProcessLines -PsOutput $processes.Output -Udid $Udid | ForEach-Object { $_.Line })
        $text = "Host simctl and CoreSimulatorService processes:`n$($hostLines -join "`n")`n`nProcesses of simulator $($Udid):`n$($owned -join "`n")`n"
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Path) | Out-Null
        [System.IO.File]::AppendAllText($Path, $text, (New-Object System.Text.UTF8Encoding($false)))
    }
    catch {
        Write-Verbose "Could not save the privacy grant evidence: $($_.Exception.Message)"
    }
}

function Grant-SimulatorPrivacy {
    # simctl has no speech service and hung for 120 s in run 44928036772, so the rest goes into TCC.db and every grant is read back;
    # a simulator missing one is replaced. Grants made before XHarness installs the app survive it; an erase removes them.
    param([string]$Identifier, [string[]]$BundleIdentifiers, [int]$GrantTimeoutSeconds = 120, [string]$EvidencePath)

    $database = Join-Path $HOME "Library/Developer/CoreSimulator/Devices/$Identifier/data/Library/TCC/TCC.db"
    $requiredGrants = @(
        [pscustomobject]@{ Name = 'camera'; TccService = 'kTCCServiceCamera'; Simctl = $true },
        [pscustomobject]@{ Name = 'microphone'; TccService = 'kTCCServiceMicrophone'; Simctl = $true },
        [pscustomobject]@{ Name = 'speech recognition'; TccService = 'kTCCServiceSpeechRecognition'; Simctl = $false }
    )

    $simctlAnswers = $true
    $grantMethods = @{}
    foreach ($bundle in $BundleIdentifiers) {
        if ($bundle -cnotmatch '^[A-Za-z0-9.-]+\z') {
            return "'$bundle' is not a valid bundle identifier, so nothing was granted on simulator $Identifier."
        }

        foreach ($service in @($requiredGrants | Where-Object { $_.Simctl })) {
            if (-not $simctlAnswers) {
                continue
            }

            $started = [datetime]::UtcNow
            $grant = Invoke-Simctl -ArgumentList @('privacy', $Identifier, 'grant', $service.Name, $bundle) -TimeoutSeconds $GrantTimeoutSeconds
            $seconds = [int][Math]::Round(([datetime]::UtcNow - $started).TotalSeconds)
            if ($grant.ExitCode -eq 0) {
                $grantMethods["$bundle|$($service.Name)"] = "simctl, $seconds s"
                continue
            }

            Write-DeviceTestWarning "Could not grant $($service.Name) to $bundle with simctl on simulator $($Identifier): $(Format-SimctlFailure -Command "privacy $Identifier grant $($service.Name) $bundle" -Result $grant -TimeoutSeconds $GrantTimeoutSeconds); writing it into the simulator's TCC database instead."
            if ($grant.TimedOut) {
                $simctlAnswers = $false
                if ($EvidencePath) {
                    Save-PrivacyGrantEvidence -Udid $Identifier -Path $EvidencePath
                }
            }
        }
    }

    $tccTimeoutSeconds = 60
    if (-not (Wait-SimulatorTccDatabase -Database $database -TimeoutSeconds $tccTimeoutSeconds)) {
        return "The TCC database of simulator $Identifier ($database) did not appear with an access table within $tccTimeoutSeconds s, so the camera, microphone and speech recognition grants cannot be checked."
    }

    $missing = @()
    foreach ($bundle in $BundleIdentifiers) {
        $granted = Get-SimulatorTccGrantedServices -Database $database -Bundle $bundle
        if ($granted.Error) {
            return $granted.Error
        }

        foreach ($service in $requiredGrants) {
            if ($granted.Services -contains $service.TccService) {
                continue
            }

            if ($grantMethods.ContainsKey("$bundle|$($service.Name)")) {
                $authValue = $granted.AuthValues[$service.TccService]
                if ($null -eq $authValue) {
                    $state = 'has no row for it'
                }
                else {
                    $state = "does not grant it (auth_value $authValue)"
                }

                Write-DeviceTestWarning "simctl granted $($service.Name) to $bundle, but the TCC database of simulator $Identifier $state; writing it."
                $grantMethods.Remove("$bundle|$($service.Name)")
            }

            $failure = Add-SimulatorTccGrant -Database $database -Bundle $bundle -TccService $service.TccService
            if ($failure) {
                Write-DeviceTestWarning "Could not write the $($service.Name) grant for $bundle into the TCC database of simulator $($Identifier): $failure"
            }

            $grantMethods["$bundle|$($service.Name)"] = 'TCC database'
        }

        $verified = Get-SimulatorTccGrantedServices -Database $database -Bundle $bundle
        if ($verified.Error) {
            return $verified.Error
        }

        $gaps = @($requiredGrants | Where-Object { $verified.Services -notcontains $_.TccService } | ForEach-Object { $_.Name })
        if ($gaps.Count -gt 0) {
            $missing += "$bundle (missing: $($gaps -join ', '))"
            continue
        }

        $grantDescriptions = @($requiredGrants | ForEach-Object {
                $method = $grantMethods["$bundle|$($_.Name)"]
                if (-not $method) {
                    $method = 'already present'
                }

                "$($_.Name) ($method)"
            })
        Write-Host "Granted to $bundle on simulator $Identifier, found in its TCC database: $($grantDescriptions -join ', ')"
    }

    if ($missing.Count -gt 0) {
        return "Simulator $Identifier could not be given the camera, microphone and speech recognition permissions: $($missing -join '; ')."
    }

    return $null
}

$exitCode = 71
$simulator = $null
$coverageSession = $null
$bundleIdentifiers = @()
try {
    if (-not $IsMacOS) {
        throw (New-Object System.ArgumentException 'iOS device tests need macOS with Xcode.')
    }

    $context = Initialize-DeviceTestRun -Platform 'ios' -Leg 'ios' -Target $Target -Project $Project -Configuration $Configuration `
        -Results $Results -BinlogDir $BinlogDir -TestFilter $TestFilter -TestFilterIsBound $PSBoundParameters.ContainsKey('TestFilter') `
        -SkipCapabilities $SkipCapabilities -SkipCapabilitiesIsBound $PSBoundParameters.ContainsKey('SkipCapabilities') `
        -RequireCapabilities $RequireCapabilities -RequireCapabilitiesIsBound $PSBoundParameters.ContainsKey('RequireCapabilities') `
        -Timeout $Timeout -LaunchTimeout $LaunchTimeout -InfraRetries $InfraRetries `
        -Coverage $Coverage.IsPresent -CoverageIsBound $PSBoundParameters.ContainsKey('Coverage')

    $runtimeIdentifier = "iossimulator-$(Get-DeviceTestHostArchitecture)"
    $buildProperties = @("RuntimeIdentifier=$runtimeIdentifier", 'CodesignRequireProvisioningProfile=false')

    $coverageSession = Invoke-DeviceTestAppBuilds -Context $context -Platform 'ios' -Target $Target -Property $buildProperties -BuildArgs $BuildArgs

    if ($Target -ne 'buildOnly') {
        Invoke-DeviceTestToolRestore
        $xcodeRoot = Get-DeviceTestXcodeRoot -Xcode $Xcode
        Write-Host "Xcode:         $xcodeRoot"

        $simulatorError = $null
        if ($Udid) {
            $existing = $null
            $lookupError = $null
            try {
                $existing = Find-SimulatorDevice -Identifier $Udid
            }
            catch {
                $lookupError = $_.Exception.Message
            }

            if ($lookupError) {
                # Without the listing the simulator may still be there: booting it says so. Whether it was booted before is unknown, so it is
                # left running afterwards.
                Write-DeviceTestWarning "Could not look up simulator $($Udid): $lookupError"
                $simulator = New-Object PSObject -Property @{ Udid = $Udid; Created = $false; BootedByScript = $false; Name = $Udid; Description = $Udid }
                $simulatorError = Wait-Simulator -Identifier $Udid -TimeoutSeconds $BootTimeoutSeconds
            }
            elseif ($null -eq $existing) {
                $simulatorError = "Simulator $Udid does not exist."
            }
            else {
                $description = Format-SimulatorDescription -Name $existing.name -DeviceTypeIdentifier $existing.deviceTypeIdentifier -RuntimeIdentifier $existing.runtimeIdentifier -Identifier $Udid
                $simulator = New-Object PSObject -Property @{ Udid = $Udid; Created = $false; BootedByScript = ($existing.state -ne 'Booted'); Name = $existing.name; Description = $description }
                $simulatorError = Wait-Simulator -Identifier $Udid -TimeoutSeconds $BootTimeoutSeconds
            }
        }
        else {
            try {
                $selected = Select-SimulatorRuntimeAndDeviceType -Runtime $Runtime -DeviceType $DeviceType
                $selectedRuntime = $selected.Runtime
                $selectedDeviceType = $selected.DeviceType
            }
            catch {
                $simulatorError = $_.Exception.Message
            }

            if (-not $simulatorError) {
                $name = $SimulatorName
                if (-not $name) {
                    $name = "CTM DeviceTests $(Get-Date -Format 'yyyyMMdd-HHmmss')"
                }

                $bundleIdentifiers = @(Get-AppBundleIdentifiers -Context $context)
                $privacyEvidencePath = Join-Path $context.Results 'privacy-grant-evidence.txt'

                # A simulator that cannot be created, booted or given the permissions is replaced as often as infrastructure failures are
                # retried (once on CI): about 1 in 10 simulator starts never finishes (actions/runner-images #12777).
                for ($attempt = 1; $attempt -le (1 + $context.InfraRetries); $attempt++) {
                    $attemptName = $name
                    if ($attempt -gt 1) {
                        $attemptName = "$name ($attempt)"
                    }

                    $prepared = New-TestSimulator -Name $attemptName -DeviceType $selectedDeviceType -Runtime $selectedRuntime -BootTimeoutSeconds $BootTimeoutSeconds
                    $simulator = $prepared.Simulator
                    $simulatorError = $prepared.Error
                    if (-not $simulatorError) {
                        $simulatorError = Grant-SimulatorPrivacy -Identifier $simulator.Udid -BundleIdentifiers $bundleIdentifiers -EvidencePath $privacyEvidencePath
                    }

                    if (-not $simulatorError -or $attempt -gt $context.InfraRetries) {
                        break
                    }

                    Write-DeviceTestWarning "$($simulatorError.TrimEnd('.')); replacing the simulator (infrastructure retry)."
                    if ($null -ne $simulator) {
                        Remove-Simulator -Identifier $simulator.Udid
                        $simulator = $null
                    }
                }
            }
        }

        if ($null -ne $simulator -and $simulator.Description) {
            $context.DeviceDescription = $simulator.Description
        }

        foreach ($item in $context.Projects) {
            $itemOutput = Join-Path $context.Results $item.Name
            if ($context.BuildFailures.ContainsKey($item.Name)) {
                $verdict = New-DeviceTestVerdict -ExitCode 71 -Directory $itemOutput -Reason "Not run: $($context.BuildFailures[$item.Name])."
            }
            elseif ($simulatorError) {
                $verdict = New-DeviceTestVerdict -ExitCode 88 -Directory $itemOutput -Reason $simulatorError
            }
            else {
                $launchStarted = [datetime]::UtcNow.AddSeconds(-5)
                $verdict = Invoke-AppleDeviceTests -Context $context -Project $item -Platform 'ios' -RuntimeIdentifier $runtimeIdentifier `
                    -XHarnessTarget 'ios-simulator-64' -Device $simulator.Udid -XcodeRoot $xcodeRoot `
                    -ExtraArguments ($XHarnessArgs + (Get-DeviceTestCoverageArguments -Session $coverageSession -Project $item)) `
                    -BeforeRetry {
                        if ($simulator.Created) {
                            [void](Invoke-Simctl -ArgumentList @('shutdown', $simulator.Udid))
                            $erased = Invoke-Simctl -ArgumentList @('erase', $simulator.Udid) -TimeoutSeconds 300
                            if ($erased.ExitCode -ne 0) {
                                Write-DeviceTestWarning (Format-SimctlFailure -Command "erase $($simulator.Udid)" -Result $erased -TimeoutSeconds 300)
                            }
                        }

                        $retryError = Wait-Simulator -Identifier $simulator.Udid -TimeoutSeconds $BootTimeoutSeconds
                        if ($retryError) {
                            Write-DeviceTestWarning $retryError
                        }
                        elseif ($simulator.Created) {
                            # The erase removed the grants.
                            $grantError = Grant-SimulatorPrivacy -Identifier $simulator.Udid -BundleIdentifiers $bundleIdentifiers -EvidencePath $privacyEvidencePath
                            if ($grantError) {
                                Write-DeviceTestWarning $grantError
                            }
                        }
                    } `
                    -OnFailure {
                        param($failureOutput, $failedAppBundle)
                        [void](Invoke-Simctl -ArgumentList @('io', $simulator.Udid, 'screenshot', (Join-Path $failureOutput 'screenshot.png')) -TimeoutSeconds 60)
                        Save-SimulatorEvidence -Udid $simulator.Udid -ResultsDirectory $failureOutput -ExecutableName (Get-AppExecutableName -AppBundle $failedAppBundle) -LaunchedAfter $launchStarted
                    }
            }

            $verdict.Project = $item.Name
            New-Item -ItemType Directory -Force -Path $verdict.Directory | Out-Null
            Add-DeviceTestVerdict -Context $context -Verdict $verdict
        }

        Complete-DeviceTestCoverage -Session $coverageSession
        $coverageSession = $null
    }

    $exitCode = Complete-DeviceTestRun -Context $context
}
catch {
    $exitCode = Write-DeviceTestException $_
}
finally {
    # Only when an error stopped the run: reports the coverage recorded so far and puts the original toolkit assemblies back.
    Complete-DeviceTestCoverage -Session $coverageSession

    if ($null -ne $simulator -and -not $KeepDevice) {
        if ($simulator.Created) {
            Remove-Simulator -Identifier $simulator.Udid
        }
        elseif ($simulator.BootedByScript) {
            [void](Invoke-Simctl -ArgumentList @('shutdown', $simulator.Udid))
        }
    }
}

Write-Host "Exit code: $exitCode ($(Get-DeviceTestExitCodeName $exitCode))"
exit $exitCode
