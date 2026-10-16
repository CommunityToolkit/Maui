<#
.SYNOPSIS
    Builds the CommunityToolkit.Maui device test apps for the iOS simulator and runs them with XHarness.

.DESCRIPTION
    Mirrors dotnet/maui's eng/devices/ios.cake: 'dotnet build -f <tfm> -p:RuntimeIdentifier=iossimulator-<arch>
    -p:CodesignRequireProvisioningProfile=false', then 'xharness apple test --target ios-simulator-64
    --set-env TestFilter=<filter>'. After every run the results gate in devices-shared.ps1 decides the exit code,
    because xharness reports SUCCESS when the app dies before its test runner starts.

    Without -Udid the script creates a dedicated simulator (newest iOS runtime and its newest iPhone unless
    -Runtime/-DeviceType say otherwise), boots it, grants the camera, the microphone and speech recognition to the
    test apps on it, and deletes it afterwards unless -KeepDevice. A simulator that cannot be created or booted is
    replaced as often as infrastructure failures are retried (-InfraRetries). It never resets, shuts down, deletes or
    grants anything on a simulator it did not create, and never passes --reset-simulator to xharness (that kills every
    simulator on the Mac). Every simctl call has a timeout, and a simctl failure is a simulator failure (88), never a
    script error.

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

    # Defaults to artifacts/device-tests/ios; each app writes to a sub-folder named after the project.
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

    # Collect code coverage of the toolkit assemblies with coverlet: the apps are packaged again with instrumented assemblies before they
    # run, and <Results>/coverage/<Results folder name>.cobertura.xml gets the report. Defaults to $env:DEVICE_TEST_COVERAGE, which only the CI
    # jobs set.
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
# failure ends the run with a script error: a simulator that cannot be prepared is an infrastructure verdict (88) for each app.
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

function Select-SimulatorRuntime {
    param([string]$Requested)

    $runtimes = @((Get-SimctlJson -Kind 'runtimes').runtimes | Where-Object { $_.platform -eq 'iOS' -and $_.isAvailable })
    if ($runtimes.Count -eq 0) {
        throw 'No available iOS simulator runtime. Install one in Xcode (Settings > Components).'
    }

    if ($Requested) {
        foreach ($candidate in $runtimes) {
            if ($candidate.identifier -eq $Requested -or $candidate.identifier.EndsWith(".$Requested") -or $candidate.version -eq $Requested -or $candidate.name -eq $Requested) {
                return $candidate
            }
        }

        throw "iOS runtime '$Requested' is not available. Available: $(($runtimes | ForEach-Object { $_.identifier }) -join ', ')."
    }

    return ($runtimes | Sort-Object { [version]$_.version } -Descending | Select-Object -First 1)
}

function Select-SimulatorDeviceType {
    param($SelectedRuntime, [string]$Requested)

    $supported = @($SelectedRuntime.supportedDeviceTypes)
    if ($supported.Count -eq 0) {
        $supported = @((Get-SimctlJson -Kind 'devicetypes').devicetypes)
    }

    if ($Requested) {
        foreach ($candidate in $supported) {
            if ($candidate.identifier -eq $Requested -or $candidate.identifier.EndsWith(".$Requested") -or $candidate.name -eq $Requested) {
                return $candidate
            }
        }

        throw "Device type '$Requested' is not supported by $($SelectedRuntime.identifier)."
    }

    $iPhone = $supported | Where-Object { $_.productFamily -eq 'iPhone' } | Select-Object -First 1
    if ($null -eq $iPhone) {
        throw "$($SelectedRuntime.identifier) supports no iPhone device type."
    }

    return $iPhone
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

function Get-AppBundleIdentifiers {
    param($Context)

    foreach ($item in $Context.Projects) {
        $framework = Get-DeviceTestTargetFramework -Project $item -Platform 'ios'
        (Get-DeviceTestMSBuildProperties -ProjectPath $item.Path -Name @('ApplicationId') -Property @("TargetFramework=$framework", "Configuration=$($Context.Configuration)"))['ApplicationId']
    }
}

function Grant-SimulatorPrivacy {
    # Without these grants, the tests of the camera, microphone and speech recognition paths would skip, because a headless run cannot
    # answer the permission prompts. 'simctl privacy' has no speech recognition service, so that grant goes straight into the simulator's
    # TCC database. Grants made before XHarness installs an app survive the install (seen with Xcode 27.0); erasing the simulator removes
    # them. A grant that is not made only skips the tests that need it, so once simctl stops answering, the remaining simctl grants are
    # left out instead of waiting for each of them.
    param([string]$Identifier, [string[]]$BundleIdentifiers, [int]$GrantTimeoutSeconds = 120)

    $database = Join-Path $HOME "Library/Developer/CoreSimulator/Devices/$Identifier/data/Library/TCC/TCC.db"
    $simctlAnswers = $true
    foreach ($bundle in $BundleIdentifiers) {
        foreach ($service in @('camera', 'microphone')) {
            if (-not $simctlAnswers) {
                continue
            }

            $grant = Invoke-Simctl -ArgumentList @('privacy', $Identifier, 'grant', $service, $bundle) -TimeoutSeconds $GrantTimeoutSeconds
            if ($grant.ExitCode -ne 0) {
                Write-DeviceTestWarning "Could not grant $service to $bundle on simulator $($Identifier): $(Format-SimctlFailure -Command "privacy $Identifier grant $service $bundle" -Result $grant -TimeoutSeconds $GrantTimeoutSeconds)"
                if ($grant.TimedOut) {
                    $simctlAnswers = $false
                    Write-DeviceTestWarning 'simctl does not answer: the remaining camera and microphone grants are left out, and the tests that need them skip.'
                }
            }
        }

        $now = [System.DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        $row = "INSERT OR REPLACE INTO access (service, client, client_type, auth_value, auth_reason, auth_version, csreq, policy_id, indirect_object_identifier_type, indirect_object_identifier, indirect_object_code_identity, flags, last_modified, pid, pid_version, boot_uuid, last_reminded) VALUES ('kTCCServiceSpeechRecognition', '$bundle', 0, 2, 4, 1, NULL, NULL, 0, 'UNUSED', NULL, 0, $now, NULL, NULL, 'UNUSED', $now);"
        $speech = Invoke-DeviceTestProcess -FilePath 'sqlite3' -ArgumentList @('-cmd', '.timeout 5000', $database, $row) -TimeoutSeconds 60
        if ($speech.ExitCode -ne 0) {
            Write-DeviceTestWarning "Could not grant speech recognition to $bundle on simulator $($Identifier): $("$($speech.Error)".Trim())"
        }
    }

    Write-Host "Granted the camera, the microphone and speech recognition to $($BundleIdentifiers -join ', ') on simulator $Identifier (except where a warning says otherwise)"
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

    if ($Target -ne 'testOnly') {
        foreach ($item in $context.Projects) {
            $framework = Get-DeviceTestTargetFramework -Project $item -Platform 'ios'
            [void](Invoke-DeviceTestBuild -Context $context -Project $item -TargetFramework $framework -BuildArgs $BuildArgs -Property $buildProperties)
        }
    }

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
                $selectedRuntime = Select-SimulatorRuntime -Requested $Runtime
                $selectedDeviceType = Select-SimulatorDeviceType -SelectedRuntime $selectedRuntime -Requested $DeviceType
            }
            catch {
                $simulatorError = $_.Exception.Message
            }

            if (-not $simulatorError) {
                $name = $SimulatorName
                if (-not $name) {
                    $name = "CTM DeviceTests $(Get-Date -Format 'yyyyMMdd-HHmmss')"
                }

                # A simulator that cannot be created or booted is replaced by a new one as often as infrastructure failures are retried
                # (once on CI): on the runners about one simulator start in ten never finishes (actions/runner-images #12777).
                for ($attempt = 1; $attempt -le (1 + $context.InfraRetries); $attempt++) {
                    $attemptName = $name
                    if ($attempt -gt 1) {
                        $attemptName = "$name ($attempt)"
                    }

                    $prepared = New-TestSimulator -Name $attemptName -DeviceType $selectedDeviceType -Runtime $selectedRuntime -BootTimeoutSeconds $BootTimeoutSeconds
                    $simulator = $prepared.Simulator
                    $simulatorError = $prepared.Error
                    if (-not $simulatorError -or $attempt -gt $context.InfraRetries) {
                        break
                    }

                    Write-DeviceTestWarning "$simulatorError; replacing the simulator (infrastructure retry)."
                    if ($null -ne $simulator) {
                        Remove-Simulator -Identifier $simulator.Udid
                        $simulator = $null
                    }
                }

                if (-not $simulatorError) {
                    $bundleIdentifiers = @(Get-AppBundleIdentifiers -Context $context)
                    Grant-SimulatorPrivacy -Identifier $simulator.Udid -BundleIdentifiers $bundleIdentifiers
                }
            }
        }

        if ($null -ne $simulator -and $simulator.Description) {
            $context.DeviceDescription = $simulator.Description
        }

        if ($context.Coverage -and -not $simulatorError) {
            $coverageSession = Start-DeviceTestCoverage -Context $context -Platform 'ios' -Property $buildProperties -BuildArgs $BuildArgs
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
                            Grant-SimulatorPrivacy -Identifier $simulator.Udid -BundleIdentifiers $bundleIdentifiers
                        }
                    } `
                    -OnFailure {
                        param($failureOutput)
                        [void](Invoke-Simctl -ArgumentList @('io', $simulator.Udid, 'screenshot', (Join-Path $failureOutput 'screenshot.png')) -TimeoutSeconds 60)
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
