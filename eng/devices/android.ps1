<#
.SYNOPSIS
    Builds the CommunityToolkit.Maui device test apps for Android and runs them on an emulator or device with XHarness.

.DESCRIPTION
    Mirrors dotnet/maui's eng/devices/android.cake: 'dotnet build -f <tfm> -p:EmbedAssembliesIntoApk=true', then
    'xharness android test --app --package-name --instrumentation <ApplicationId>.TestInstrumentation --device-arch
    --arg TestFilter=<filter>'. After every run the results gate in devices-shared.ps1 decides the exit code.

    The device is, in this order: the emulator booted from -Avd (stopped again afterwards unless -KeepDevice),
    -DeviceId, or the only attached device. With several devices attached, pass -DeviceId.

    Exit codes use xharness's vocabulary: 0 passed, 1 test failures, 3 invalid arguments, 4 app not built,
    70 timed out, 71 build or script failure, 80 app crash, 81 no device, 94 no results or no tests ran,
    other 7x/8x/9x codes are xharness infrastructure failures. See eng/devices/README.md.

.EXAMPLE
    pwsh eng/devices/android.ps1 -Avd CTM_DeviceTests_API36 -EmulatorPort 5560

.EXAMPLE
    pwsh eng/devices/android.ps1 -Target testOnly -Project Maui -DeviceId emulator-5554 -TestFilter 'Category=IconTintColorBehavior'
#>
[CmdletBinding()]
param(
    [ValidateSet('buildOnly', 'testOnly', 'buildAndTest')]
    [string]$Target = 'buildAndTest',

    # Device test app(s): CommunityToolkit.Maui.DeviceTests (default), MediaElement, Camera, All, or a .csproj path.
    [string[]]$Project = @('CommunityToolkit.Maui.DeviceTests'),

    [string]$Configuration = 'Debug',

    # Defaults to artifacts/device-tests/android; each app writes to a sub-folder named after the project.
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

    [string]$DeviceId,

    # Name of an existing AVD to boot for this run (see 'avdmanager list avd').
    [string]$Avd,

    [int]$EmulatorPort = 5554,

    # ABI passed to xharness --device-arch, for example x86_64 or arm64-v8a. Defaults to the device's ABI.
    [string]$DeviceArch,

    [int]$BootTimeoutSeconds = 600,

    # Leave an emulator started by -Avd running.
    [switch]$KeepDevice,

    # Extra arguments for 'dotnet build', for example '/p:UseMonoRuntime=false' (a value starting with -p: is read as a parameter name).
    [string[]]$BuildArgs = @(),

    # Extra arguments for xharness, for example '--set-env=NAME=value'.
    [string[]]$XHarnessArgs = @()
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'devices-shared.ps1')
. (Join-Path $PSScriptRoot 'devices-coverage.ps1')

function Get-AndroidSdkRoot {
    $candidates = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT)
    if ($script:DeviceTestsOnWindows) {
        $candidates += (Join-Path $env:LOCALAPPDATA (Join-Path 'Android' 'Sdk'))
    }
    elseif ($IsMacOS) {
        $candidates += (Join-Path $HOME (Join-Path 'Library' (Join-Path 'Android' 'sdk')))
    }
    else {
        $candidates += (Join-Path $HOME (Join-Path 'Android' 'Sdk'))
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath (Join-Path $candidate 'platform-tools') -PathType Container)) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    return ''
}

function Get-AndroidTool {
    param([string]$SdkRoot, [string]$Folder, [string]$Name)

    $fileName = $Name
    if ($script:DeviceTestsOnWindows) {
        $fileName = "$Name.exe"
    }

    return Join-Path $SdkRoot (Join-Path $Folder $fileName)
}

function Get-AndroidDevices {
    param([string]$Adb)

    $listing = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('devices') -TimeoutSeconds 60
    $devices = @()
    foreach ($line in ($listing.Output -split "`r?`n")) {
        if ($line -match '^(\S+)\s+(device|offline|unauthorized|bootloader|recovery|sideload|no permissions.*)$') {
            $devices += New-Object PSObject -Property @{ Serial = $Matches[1]; State = $Matches[2] }
        }
    }

    return , $devices
}

function Test-AndroidDeviceListed {
    # An emulator whose process ended (it aborted, or something killed it) is no longer listed, and waiting does not bring it back.
    param([string]$Adb, [string]$Serial)

    return @(Get-AndroidDevices -Adb $Adb | Where-Object { $_.Serial -eq $Serial }).Count -gt 0
}

function Get-AndroidDeviceGoneReason {
    param([string]$Serial)

    return "$Serial is gone: adb no longer lists it, so its emulator process ended (an emulator that aborts says why in its output, for example in a FATAL line). Nothing here restarts it, so this app and the ones after it do not run on it."
}

function Invoke-AndroidShell {
    param([string]$Adb, [string]$Serial, [string[]]$Command, [int]$TimeoutSeconds = 30)

    $result = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList (@('-s', $Serial, 'shell') + $Command) -TimeoutSeconds $TimeoutSeconds
    if ($result.ExitCode -ne 0) {
        return $null
    }

    return $result.Output.Trim()
}

function Wait-AndroidDevice {
    param([string]$Adb, [string]$Serial, [int]$TimeoutSeconds, [System.Diagnostics.Process]$Process)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ($null -ne $Process -and $Process.HasExited) {
            return "the emulator process exited with code $($Process.ExitCode) before $Serial booted"
        }

        if ((Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('getprop', 'sys.boot_completed') -TimeoutSeconds 15) -eq '1') {
            $packageManager = Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('pm', 'path', 'android') -TimeoutSeconds 30
            if ($packageManager -match 'package:') {
                return $null
            }
        }

        Start-Sleep -Seconds 3
    }

    return "$Serial was not ready after $TimeoutSeconds s"
}

function Start-AndroidEmulator {
    param([string]$SdkRoot, [string]$Adb, [string]$Name, [int]$Port, [int]$TimeoutSeconds, [string]$LogDirectory)

    $serial = "emulator-$Port"
    $running = @(Get-AndroidDevices -Adb $Adb | Where-Object { $_.Serial -eq $serial })
    if ($running.Count -gt 0) {
        $avdName = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $serial, 'emu', 'avd', 'name') -TimeoutSeconds 30
        $runningName = ($avdName.Output -split "`r?`n")[0].Trim()
        if ($runningName -ne $Name) {
            return New-Object PSObject -Property @{ Error = "$serial is already used by AVD '$runningName'. Pick another -EmulatorPort." }
        }

        Write-Host "AVD $Name is already running as $serial; it will be left running."
        $waitError = Wait-AndroidDevice -Adb $Adb -Serial $serial -TimeoutSeconds $TimeoutSeconds
        return New-Object PSObject -Property @{ Serial = $serial; Process = $null; Started = $false; Error = $waitError }
    }

    $emulator = Get-AndroidTool -SdkRoot $SdkRoot -Folder 'emulator' -Name 'emulator'
    $avds = Invoke-DeviceTestProcess -FilePath $emulator -ArgumentList @('-list-avds') -TimeoutSeconds 60
    if (-not (($avds.Output -split "`r?`n") -contains $Name)) {
        return New-Object PSObject -Property @{ Error = "AVD '$Name' does not exist. Create it with avdmanager (see eng/devices/README.md)." }
    }

    # Headless flags from dotnet/maui's android.cake; -no-snapshot-save keeps the AVD's snapshot unchanged. Both cameras are emulated, as on
    # CI: AVDs that avdmanager creates have no front camera, which the two-camera tests need.
    $arguments = @('-avd', $Name, '-port', "$Port", '-no-window', '-no-audio', '-no-boot-anim', '-no-snapshot-save', '-gpu', 'swiftshader_indirect', '-camera-back', 'emulated', '-camera-front', 'emulated')
    Write-Host "> $emulator $(Format-DeviceTestCommandLine $arguments)"
    $process = Start-Process -FilePath $emulator -ArgumentList $arguments -PassThru `
        -RedirectStandardOutput (Join-Path $LogDirectory 'emulator.log') `
        -RedirectStandardError (Join-Path $LogDirectory 'emulator-error.log')

    $waitError = Wait-AndroidDevice -Adb $Adb -Serial $serial -TimeoutSeconds $TimeoutSeconds -Process $process
    return New-Object PSObject -Property @{ Serial = $serial; Process = $process; Started = $true; Error = $waitError }
}

function Stop-AndroidEmulator {
    param([string]$Adb, $Emulator)

    Write-Host "Stopping $($Emulator.Serial)"
    [void](Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Emulator.Serial, 'emu', 'kill') -TimeoutSeconds 60)
    if ($null -ne $Emulator.Process -and -not $Emulator.Process.WaitForExit(60000)) {
        Stop-Process -Id $Emulator.Process.Id -Force -ErrorAction SilentlyContinue
    }
}

function Find-AndroidPackage {
    param($Project, [string]$Framework, [string]$Configuration, [string]$ApplicationId, [string]$OutputPath)

    $apk = Join-Path (Get-DeviceTestOutputDirectory -Project $Project -Path $OutputPath) "$ApplicationId-Signed.apk"
    if (Test-Path -LiteralPath $apk -PathType Leaf) {
        return $apk
    }

    # A RuntimeIdentifier set in the project moves the package into a RID folder.
    $searchRoot = Join-Path $Project.Directory (Join-Path 'bin' (Join-Path $Configuration $Framework))
    if (Test-Path -LiteralPath $searchRoot -PathType Container) {
        $found = Get-ChildItem -LiteralPath $searchRoot -Recurse -Filter "$ApplicationId-Signed.apk" -File |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($null -ne $found) {
            return $found.FullName
        }
    }

    return $null
}

function Invoke-AndroidDeviceTests {
    param($Context, $Project, [string]$Adb, [string]$Serial, [string]$Abi, $CoverageSession)

    $androidOutput = Join-Path $Context.Results $Project.Name
    Initialize-DeviceTestResultsDirectory -Path $androidOutput

    $framework = Get-DeviceTestTargetFramework -Project $Project -Platform 'android'
    $properties = Get-DeviceTestMSBuildProperties -ProjectPath $Project.Path -Name @('ApplicationId', 'OutputPath') -Property @("TargetFramework=$framework", "Configuration=$($Context.Configuration)")
    $applicationId = $properties['ApplicationId']
    $apk = Find-AndroidPackage -Project $Project -Framework $framework -Configuration $Context.Configuration -ApplicationId $applicationId -OutputPath $properties['OutputPath']
    if (-not $apk) {
        $verdict = New-DeviceTestVerdict -ExitCode 4 -Directory $androidOutput -Reason "$applicationId-Signed.apk was not found for $framework ($($Context.Configuration)). Build it first with -Target buildOnly or buildAndTest."
        $verdict.Project = $Project.Name
        return $verdict
    }

    Write-DeviceTestHeader "Test $($Project.Name) ($applicationId) on $Serial ($Abi)"

    # Same arguments as dotnet/maui's android.cake; --device-id pins the run to one device.
    $androidArguments = @(
        'android', 'test',
        "--app=$apk",
        "--package-name=$applicationId",
        "--instrumentation=$applicationId.TestInstrumentation",
        "--device-id=$Serial",
        "--device-arch=$Abi",
        "--output-directory=$androidOutput",
        "--timeout=$($Context.Timeout)",
        "--launch-timeout=$($Context.LaunchTimeout)",
        '--verbosity=Debug',
        "--arg=TestFilter=$($Context.TestFilter)"
    )
    $androidArguments += @(Get-DeviceTestCapabilityDeclarations -Context $Context | ForEach-Object { "--arg=$_" })
    $androidArguments += Get-DeviceTestCoverageArguments -Session $CoverageSession -Project $Project -ApplicationId $applicationId
    $androidArguments += $XHarnessArgs

    return Invoke-DeviceTestWithRetries -Context $Context -Project $Project -Directory $androidOutput -Attempt {
        param($attemptNumber)

        # A device that is gone is not handed to XHarness: it would look for it for minutes, then try restarts that only work on its own lab
        # machines (systemctl restart android-emulator, adb emu restart), and end with 81 without saying what happened.
        if (-not (Test-AndroidDeviceListed -Adb $Adb -Serial $Serial)) {
            $goneVerdict = New-DeviceTestVerdict -ExitCode 81 -Directory $androidOutput -Reason (Get-AndroidDeviceGoneReason -Serial $Serial)
            $goneVerdict.Attempt = $attemptNumber
            $goneVerdict | Add-Member -NotePropertyName 'NoRetry' -NotePropertyValue $true
            return $goneVerdict
        }

        $androidExitCode = Invoke-XHarness -ArgumentList $androidArguments -OutputDirectory $androidOutput
        $verdict = Get-DeviceTestVerdict -Platform 'android' -Directory $androidOutput -RunnerExitCode $androidExitCode -Attempt $attemptNumber
        if ($verdict.ExitCode -ne 0 -and -not (Test-AndroidDeviceListed -Adb $Adb -Serial $Serial)) {
            # Another attempt on a device that is gone cannot help
            $verdict.Reasons = @($verdict.Reasons) + (Get-AndroidDeviceGoneReason -Serial $Serial)
            $verdict | Add-Member -NotePropertyName 'NoRetry' -NotePropertyValue $true -Force
        }

        $verdict
    } -BeforeRetry {
        # Waiting only helps a device that is still listed (offline, or booting again)
        if (Test-AndroidDeviceListed -Adb $Adb -Serial $Serial) {
            $retryError = Wait-AndroidDevice -Adb $Adb -Serial $Serial -TimeoutSeconds 120
            if ($retryError) {
                Write-DeviceTestWarning $retryError
            }
        }
    }
}

$exitCode = 71
$emulatorInstance = $null
$adbPath = ''
$coverageSession = $null
try {
    $context = Initialize-DeviceTestRun -Platform 'android' -Leg 'android' -Target $Target -Project $Project -Configuration $Configuration `
        -Results $Results -BinlogDir $BinlogDir -TestFilter $TestFilter -TestFilterIsBound $PSBoundParameters.ContainsKey('TestFilter') `
        -SkipCapabilities $SkipCapabilities -SkipCapabilitiesIsBound $PSBoundParameters.ContainsKey('SkipCapabilities') `
        -RequireCapabilities $RequireCapabilities -RequireCapabilitiesIsBound $PSBoundParameters.ContainsKey('RequireCapabilities') `
        -Timeout $Timeout -LaunchTimeout $LaunchTimeout -InfraRetries $InfraRetries `
        -Coverage $Coverage.IsPresent -CoverageIsBound $PSBoundParameters.ContainsKey('Coverage')

    $sdkRoot = Get-AndroidSdkRoot
    if ($sdkRoot) {
        Write-Host "Android SDK:   $sdkRoot"
    }

    # EmbedAssembliesIntoApk: Debug builds otherwise rely on fast deployment, which xharness's plain adb install does not do.
    $buildProperties = @('EmbedAssembliesIntoApk=true')
    if ($sdkRoot) {
        $buildProperties += "AndroidSdkDirectory=$sdkRoot"
    }

    if ($Target -ne 'testOnly') {
        foreach ($item in $context.Projects) {
            $framework = Get-DeviceTestTargetFramework -Project $item -Platform 'android'
            [void](Invoke-DeviceTestBuild -Context $context -Project $item -TargetFramework $framework -Property $buildProperties -BuildArgs $BuildArgs)
        }
    }

    if ($Target -ne 'buildOnly') {
        Invoke-DeviceTestToolRestore

        $deviceError = $null
        $serial = ''
        if (-not $sdkRoot) {
            $deviceError = 'Android SDK not found. Set ANDROID_HOME.'
        }
        else {
            $adbPath = Get-AndroidTool -SdkRoot $sdkRoot -Folder 'platform-tools' -Name 'adb'
            # Makes xharness use the SDK's adb instead of starting its bundled one, which would restart the adb server.
            $env:ADB_EXE_PATH = $adbPath

            if ($Avd) {
                $emulatorInstance = Start-AndroidEmulator -SdkRoot $sdkRoot -Adb $adbPath -Name $Avd -Port $EmulatorPort -TimeoutSeconds $BootTimeoutSeconds -LogDirectory $context.Results
                $deviceError = $emulatorInstance.Error
                $serial = $emulatorInstance.Serial
            }
            else {
                $devices = Get-AndroidDevices -Adb $adbPath
                if ($DeviceId) {
                    $matching = @($devices | Where-Object { $_.Serial -eq $DeviceId })
                    if ($matching.Count -eq 0) {
                        $deviceError = "Device $DeviceId is not attached."
                    }
                    elseif ($matching[0].State -ne 'device') {
                        $deviceError = "Device $DeviceId is $($matching[0].State)."
                    }
                    else {
                        $serial = $DeviceId
                    }
                }
                else {
                    $online = @($devices | Where-Object { $_.State -eq 'device' })
                    if ($online.Count -eq 1) {
                        $serial = $online[0].Serial
                    }
                    elseif ($online.Count -eq 0) {
                        $deviceError = 'No Android device or emulator is attached. Pass -Avd <name> to boot one or -DeviceId <serial>.'
                    }
                    else {
                        $deviceError = "Several Android devices are attached ($(($online | ForEach-Object { $_.Serial }) -join ', ')). Pass -DeviceId <serial>."
                    }
                }

                if (-not $deviceError) {
                    $deviceError = Wait-AndroidDevice -Adb $adbPath -Serial $serial -TimeoutSeconds $BootTimeoutSeconds
                }
            }
        }

        $abi = $DeviceArch
        if (-not $deviceError) {
            if (-not $abi) {
                $abi = Invoke-AndroidShell -Adb $adbPath -Serial $serial -Command @('getprop', 'ro.product.cpu.abi')
            }

            if (-not $abi) {
                $abi = 'x86_64'
                if ((Get-DeviceTestHostArchitecture) -eq 'arm64') {
                    $abi = 'arm64-v8a'
                }
            }

            $apiLevel = Invoke-AndroidShell -Adb $adbPath -Serial $serial -Command @('getprop', 'ro.build.version.sdk')
            $context.DeviceDescription = "$serial, API $apiLevel, $abi"
        }

        if ($context.Coverage -and -not $deviceError) {
            $coverageSession = Start-DeviceTestCoverage -Context $context -Platform 'android' -Property $buildProperties -BuildArgs $BuildArgs
        }

        foreach ($item in $context.Projects) {
            if ($context.BuildFailures.ContainsKey($item.Name)) {
                $verdict = New-DeviceTestVerdict -ExitCode 71 -Directory (Join-Path $context.Results $item.Name) -Reason "Not run: $($context.BuildFailures[$item.Name])."
            }
            elseif ($deviceError) {
                $verdict = New-DeviceTestVerdict -ExitCode 81 -Directory (Join-Path $context.Results $item.Name) -Reason $deviceError
            }
            else {
                $verdict = Invoke-AndroidDeviceTests -Context $context -Project $item -Adb $adbPath -Serial $serial -Abi $abi -CoverageSession $coverageSession
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

    if ($null -ne $emulatorInstance -and $emulatorInstance.Started -and -not $KeepDevice -and $adbPath) {
        Stop-AndroidEmulator -Adb $adbPath -Emulator $emulatorInstance
    }
}

Write-Host "Exit code: $exitCode ($(Get-DeviceTestExitCodeName $exitCode))"
exit $exitCode
