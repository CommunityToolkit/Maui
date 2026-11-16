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

    # Defaults to artifacts/device-tests/android; the app writes to a sub-folder named after the project.
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

function Enable-AndroidRootShell {
    # debuggerd, kill -3 and the binder logs need root. adbd restarts to get it, which would end a running xharness, so this runs before xharness starts.
    param([string]$Adb, [string]$Serial)

    try {
        if ((Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('id', '-u') -TimeoutSeconds 10) -ceq '0') {
            return $true
        }

        $started = Get-Date
        $root = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'root') -TimeoutSeconds 30
        [void](Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'wait-for-device') -TimeoutSeconds 60)
        $rootDeadline = (Get-Date).AddSeconds(10)
        do {
            if ((Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('id', '-u') -TimeoutSeconds 10) -ceq '0') {
                $seconds = [int][Math]::Round(((Get-Date) - $started).TotalSeconds)
                $script:AndroidRootedSerial = $Serial
                Write-Host ([string]::Format([System.Globalization.CultureInfo]::InvariantCulture, 'adbd on {0} runs as root ({1} s)', $Serial, $seconds))
                return $true
            }

            Start-Sleep -Seconds 1
        } while ((Get-Date) -lt $rootDeadline)

        Write-Host "adbd on $Serial stays unrooted ($(($root.Output + $root.Error).Trim())); the device evidence of a failing run will be partial."
    }
    catch {
        Write-Host "adbd on $Serial stays unrooted ($($_.Exception.Message)); the device evidence of a failing run will be partial."
    }

    return $false
}

function Start-AndroidEvidenceWatch {
    # A job and not a timer event: the main runspace is blocked in xharness's pipeline, which prints nothing while the app hangs.
    param([string]$Adb, [string]$Serial, [string]$PackageName, [string]$Directory, [datetime]$DeadlineUtc)

    try {
        return Start-Job -ArgumentList @((Join-Path $PSScriptRoot 'devices-shared.ps1'), $Adb, $Serial, $PackageName, $Directory, $DeadlineUtc.Ticks) -ScriptBlock {
            param($sharedScript, $adb, $serial, $packageName, $directory, $deadlineTicks)
            . $sharedScript
            $wait = ([datetime]::new([long]$deadlineTicks, [System.DateTimeKind]::Utc) - [datetime]::UtcNow).TotalSeconds
            if ($wait -gt 0) { Start-Sleep -Seconds ([int][Math]::Ceiling($wait)) }
            [void](Save-AndroidDeviceEvidence -Adb $adb -Serial $serial -PackageName $packageName -Directory $directory -Phase 'at -Timeout, while xharness still waited for the app')
        }
    }
    catch {
        Write-DeviceTestWarning "The device evidence watch did not start: $($_.Exception.Message)"
        return $null
    }
}

function Stop-AndroidEvidenceWatch {
    # Lets a watch that is due finish, and says out loud when it failed, because its output and errors are otherwise lost.
    param($Job, [bool]$DeadlinePassed)

    if ($null -eq $Job) {
        return
    }

    if ($DeadlinePassed) {
        [void](Wait-Job -Job $Job -Timeout $script:AndroidEvidenceGraceSeconds -ErrorAction SilentlyContinue)
    }

    $stillRunning = $Job.State -eq 'Running'
    Stop-Job -Job $Job -ErrorAction SilentlyContinue
    Receive-Job -Job $Job -ErrorAction SilentlyContinue -ErrorVariable jobErrors 6>$null
    $failed = $Job.State -eq 'Failed' -or @($jobErrors).Count -gt 0
    $stillRunningPastDeadline = $DeadlinePassed -and $stillRunning
    if ($failed -or $stillRunningPastDeadline) {
        $detail = if (@($jobErrors).Count -gt 0) { "$($jobErrors[0])" } elseif ($stillRunningPastDeadline) { 'it was still running when the grace period ended' } else { "job state $($Job.State)" }
        Write-DeviceTestWarning "The device evidence watch did not finish: $detail"
    }

    Remove-Job -Job $Job -Force -ErrorAction SilentlyContinue
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

function Get-AndroidFocusedWindowTitle {
    param([string]$Adb, [string]$Serial)

    $windows = Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('dumpsys', 'window') -TimeoutSeconds 60
    # Android 7 prints mCurrentFocus twice and only the later line names the dialog, so every match is checked.
    $focusMatches = [regex]::Matches([string]$windows, 'mCurrentFocus=Window\{[0-9a-f]+ u\d+ (?<title>[^}]*)\}', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
    $titles = @($focusMatches | ForEach-Object { $_.Groups['title'].Value })
    $dialogTitle = $titles | Where-Object { Get-AndroidErrorDialogProcess -Title $_ } | Select-Object -First 1
    if ($dialogTitle) { return $dialogTitle }
    return [string]($titles | Select-Object -First 1)
}

function Get-AndroidErrorDialogProcess {
    param([string]$Title)

    $match = [regex]::Match($Title, '^(Application Not Responding|Application Error): (?<process>\S+)$', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if ($match.Success) { return $match.Groups['process'].Value }
    return ''
}

function Wait-AndroidErrorDialogGone {
    param([string]$Adb, [string]$Serial, [int]$TimeoutSeconds)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if (-not (Get-AndroidErrorDialogProcess -Title (Get-AndroidFocusedWindowTitle -Adb $Adb -Serial $Serial))) { return $true }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    return $false
}

# A system ANR or crash dialog keeps the window focus, so the keyboard, dialogs and system-bar requests of the test app never take effect.
function Clear-AndroidErrorDialog {
    param([string]$Adb, [string]$Serial)

    $result = [pscustomobject]@{ Warning = ''; Failure = '' }
    $dialogProcess = Get-AndroidErrorDialogProcess -Title (Get-AndroidFocusedWindowTitle -Adb $Adb -Serial $Serial)
    if (-not $dialogProcess) { return $result }

    [void](Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('am', 'broadcast', '-a', 'android.intent.action.CLOSE_SYSTEM_DIALOGS') -TimeoutSeconds 30)
    if (-not (Wait-AndroidErrorDialogGone -Adb $Adb -Serial $Serial -TimeoutSeconds 5)) {
        # Killing the process that owns the dialog removes it; system_server cannot be killed.
        $package = $dialogProcess.Split(':')[0]
        if ($package -ne 'system' -and $package -ne 'android') {
            [void](Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('am', 'force-stop', $package) -TimeoutSeconds 30)
        }
        if (-not (Wait-AndroidErrorDialogGone -Adb $Adb -Serial $Serial -TimeoutSeconds 30)) {
            $result.Failure = "The error dialog of $dialogProcess keeps the window focus on $Serial, so keyboard, dialog and system-bar tests cannot pass. Closing it and stopping $package did not remove it."
            return $result
        }
    }

    $result.Warning = "Closed the error dialog of $dialogProcess that had the window focus before the tests started."
    return $result
}

# Tests that start while the boot broadcasts of a fresh emulator are still queued get their activity relaunched when the OS refreshes its overlays
# (the CI logcat shows idmap2d starting right before the relaunch), which fails whatever camera or window test runs at that moment.
# 'am wait-for-broadcast-idle' is hidden (not in 'am help') and exists from API 26 (AOSP oreo-release); older versions poll the dumpsys queues.
function Wait-AndroidBroadcastsIdle {
    param([string]$Adb, [string]$Serial, [int]$TimeoutSeconds = 120)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $started = Get-Date
    $apiLevel = 0
    [void][int]::TryParse((Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('getprop', 'ro.build.version.sdk')), [ref]$apiLevel)

    while ((Get-Date) -lt $deadline) {
        $booted = (Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('getprop', 'sys.boot_completed')) -eq '1' -and (Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('getprop', 'dev.bootcomplete')) -eq '1'
        if ($booted) { break }
        Start-Sleep -Seconds 2
    }

    if ($apiLevel -ge 26) {
        $remaining = [int][math]::Ceiling(($deadline - (Get-Date)).TotalSeconds)
        if ($remaining -gt 0) {
            $idle = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'am', 'wait-for-broadcast-idle') -TimeoutSeconds $remaining
            if ($idle.ExitCode -eq 0 -and $idle.Output -match 'idle' -and $idle.Output -notmatch '(?m)^usage:') {
                return $null
            }
            if ($idle.ExitCode -eq -1) {
                return "$Serial still had queued broadcasts after a $TimeoutSeconds s wait; the tests start anyway and may see the runner's activity relaunched."
            }
        }
    }

    while ((Get-Date) -lt $deadline) {
        $broadcasts = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'dumpsys', 'activity', 'broadcasts') -TimeoutSeconds 30
        if ($broadcasts.ExitCode -eq 0 -and $broadcasts.Output -notmatch '(?m)^\s*Active (Ordered )?Broadcast ') {
            return $null
        }
        Start-Sleep -Seconds 2
    }

    return "$Serial still had queued broadcasts after a $TimeoutSeconds s wait; the tests start anyway and may see the runner's activity relaunched."
}

# A test that Android relaunched mid-run reruns itself once and then passes, so only the logcat shows that the OS interfered.
function Find-AndroidSessionRetriesInLog {
    param([string]$Directory)

    $count = 0
    foreach ($log in Get-ChildItem -Path $Directory -Filter 'adb-logcat-*.log' -File -ErrorAction SilentlyContinue) {
        $count += @(Select-String -Path $log.FullName -Pattern 'CameraAndroidTests.*runs once more in the new activity' -CaseSensitive -ErrorAction SilentlyContinue).Count
    }

    if ($count -eq 0) { return '' }
    return "$count camera test(s) ran once more because Android relaunched the runner's activity mid-test; see 'runs once more' in the logcat."
}

function Find-AndroidErrorDialogInLog {
    param([string]$Directory)

    foreach ($log in Get-ChildItem -Path $Directory -Filter 'adb-logcat-*.log' -File -ErrorAction SilentlyContinue) {
        $match = Select-String -Path $log.FullName -Pattern 'ANR in (\S+)' -CaseSensitive -List -ErrorAction SilentlyContinue
        if ($match) { return $match.Matches[0].Groups[1].Value }
    }

    return ''
}

function Find-AndroidUiThreadWatchdogInLog {
    param([string]$Directory)

    # The watchdog kills the app, which reaches XHarness as a crash (80), so the logcat line is the only place that names the hang.
    foreach ($log in Get-ChildItem -Path $Directory -Filter 'adb-logcat-*.log' -File -ErrorAction SilentlyContinue) {
        $match = Select-String -Path $log.FullName -Pattern 'CtmUiThreadWatchdog: (The UI thread has not answered[^\r\n]*)' -CaseSensitive -List -ErrorAction SilentlyContinue
        if ($match) { return $match.Matches[0].Groups[1].Value }
    }

    return ''
}

function Invoke-AndroidDeviceTests {
    param($Context, $Project, [string]$Adb, [string]$Serial, [string]$Abi, $CoverageSession, [int]$ApiLevel)

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
        # The evidence watch needs the app alive at -Timeout, and xharness uninstalls it at its own timeout.
        "--timeout=$([System.TimeSpan]::FromSeconds($Context.TimeoutSeconds + $script:AndroidEvidenceGraceSeconds).ToString('c', [System.Globalization.CultureInfo]::InvariantCulture))",
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

        [void](Enable-AndroidRootShell -Adb $Adb -Serial $Serial)

        $settleStart = Get-Date
        $settleWarning = Wait-AndroidBroadcastsIdle -Adb $Adb -Serial $Serial
        if ($settleWarning) {
            Write-DeviceTestWarning $settleWarning
        }
        else {
            Write-Host ("Broadcast queues of {0} settled after {1:N0} s" -f $Serial, ((Get-Date) - $settleStart).TotalSeconds)
        }

        $dialog = Clear-AndroidErrorDialog -Adb $Adb -Serial $Serial
        if ($dialog.Warning) { Write-DeviceTestWarning $dialog.Warning }
        if ($dialog.Failure) {
            $dialogVerdict = New-DeviceTestVerdict -ExitCode 81 -Directory $androidOutput -Reason $dialog.Failure
            $dialogVerdict.Attempt = $attemptNumber
            return $dialogVerdict
        }

        # The default main log buffer holds only a few minutes of a long run, so the logs of a test that failed early were already rotated out
        # when the run ended. Each attempt sets it again because the size does not survive a restart of the device.
        [void](Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'logcat', '-G', '64M') -TimeoutSeconds 30)

        $evidenceDirectory = Join-Path $androidOutput 'device-evidence'
        $watchDeadlineUtc = [datetime]::UtcNow.AddSeconds($Context.TimeoutSeconds)
        $evidenceWatch = Start-AndroidEvidenceWatch -Adb $Adb -Serial $Serial -PackageName $applicationId -Directory $evidenceDirectory -DeadlineUtc $watchDeadlineUtc
        try {
            $androidExitCode = Invoke-XHarness -ArgumentList $androidArguments -OutputDirectory $androidOutput
        }
        finally {
            $ranPastTimeout = [datetime]::UtcNow -ge $watchDeadlineUtc
            Stop-AndroidEvidenceWatch -Job $evidenceWatch -DeadlinePassed $ranPastTimeout
        }

        $verdict = Get-DeviceTestVerdict -Platform 'android' -Directory $androidOutput -RunnerExitCode $androidExitCode -Attempt $attemptNumber
        # The grace period beyond -Timeout must never turn a run that was still going at -Timeout into anything but a timeout.
        if ($ranPastTimeout -and $verdict.ExitCode -ne 70) {
            $xharnessResult = "$($verdict.ExitCode) ($($verdict.ExitCodeName))"
            $verdict.ExitCode = 70
            $verdict.ExitCodeName = Get-DeviceTestExitCodeName 70
            $verdict.Reasons = @($verdict.Reasons) + "the run was still going at -Timeout ($($Context.Timeout)), and it counts as timed out whatever XHarness reports afterwards (it ended with $xharnessResult within the $($script:AndroidEvidenceGraceSeconds) s it waits longer)"
        }

        if ($dialog.Warning) {
            $verdict.Warnings = @($verdict.Warnings) + $dialog.Warning
        }
        if ($settleWarning) {
            $verdict.Warnings = @($verdict.Warnings) + $settleWarning
        }
        $sessionRetries = Find-AndroidSessionRetriesInLog -Directory $androidOutput
        if ($sessionRetries) {
            $verdict.Warnings = @($verdict.Warnings) + $sessionRetries
        }
        if ($verdict.ExitCode -ne 0) {
            $anrProcess = Find-AndroidErrorDialogInLog -Directory $androidOutput
            if ($anrProcess) {
                $verdict.Warnings = @($verdict.Warnings) + "An ANR of $anrProcess appears in the logcat; failures that need window focus (keyboard, dialogs, system bars) may come from its dialog."
            }
            $watchdogLine = Find-AndroidUiThreadWatchdogInLog -Directory $androidOutput
            if ($watchdogLine) {
                $verdict.Reasons = @($verdict.Reasons) + "The UI-thread watchdog ended the app: $watchdogLine"
            }
        }
        if ($verdict.ExitCode -ne 0 -and -not (Test-AndroidDeviceListed -Adb $Adb -Serial $Serial)) {
            # Another attempt on a device that is gone cannot help
            $verdict.Reasons = @($verdict.Reasons) + (Get-AndroidDeviceGoneReason -Serial $Serial)
            $verdict | Add-Member -NotePropertyName 'NoRetry' -NotePropertyValue $true -Force
        }

        # Every failure, test failures included: an in-app SIGQUIT and a cameraserver watchdog abort leave their stacks only in /data/anr and
        # /data/tombstones. This runs before the retry's reinstall and before the emulator stops.
        if ($verdict.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $evidenceDirectory) -and (Test-AndroidDeviceListed -Adb $Adb -Serial $Serial)) {
            try {
                [void](Enable-AndroidRootShell -Adb $Adb -Serial $Serial)
                [void](Save-AndroidDeviceEvidence -Adb $Adb -Serial $Serial -PackageName $applicationId -Directory $evidenceDirectory -Phase "after exit code $($verdict.ExitCode) ($($verdict.ExitCodeName))")
            }
            catch {
                Write-DeviceTestWarning "Collecting device evidence failed: $($_.Exception.Message)"
            }
        }

        if (Test-Path -LiteralPath $evidenceDirectory) {
            $verdict | Add-Member -NotePropertyName 'Evidence' -NotePropertyValue $evidenceDirectory -Force
        }

        $verdict
    } -BeforeRetry {
        param($platformCrash)

        # Waiting only helps a device that is still listed (offline, or booting again)
        if (Test-AndroidDeviceListed -Adb $Adb -Serial $Serial) {
            $retryError = Wait-AndroidDevice -Adb $Adb -Serial $Serial -TimeoutSeconds 120
            if ($retryError) {
                Write-DeviceTestWarning $retryError
            }

            # A stuck camera close or lost CameraX state lives in the emulator's camera services, which outlive the app.
            if ($null -ne $platformCrash -and $platformCrash.Id -ceq 'camera-wedge-api32-33') {
                Restart-AndroidCameraStack -Adb $Adb -Serial $Serial
            }
        }
    } -FindPlatformCrash {
        param($attemptVerdict)

        Get-AndroidPlatformCrash -Verdict $attemptVerdict -Directory $androidOutput -ApiLevel $ApiLevel -PackageName $applicationId
    }
}

function Restart-AndroidCameraStack {
    # Best effort: init restarts both services after the kill. The retry runs either way, and its own result decides.
    param([string]$Adb, [string]$Serial, [int]$TimeoutSeconds = 60)

    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    $getProcesses = { Get-AndroidEvidenceProcesses -PsOutput (Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('ps', '-A') -TimeoutSeconds 20) }
    $getServerId = {
        param($Processes)
        $server = @($Processes | Where-Object { $_.Name -ceq 'cameraserver' } | Select-Object -First 1)
        if ($server.Count -gt 0) { return $server[0].Id }
        return 0
    }

    try {
        [void](Enable-AndroidRootShell -Adb $Adb -Serial $Serial)
        $processes = & $getProcesses
        $oldServerId = & $getServerId $processes
        $victims = @($processes | Where-Object { $_.Name.IndexOf('camera.provider', [System.StringComparison]::Ordinal) -ge 0 -or $_.Name -ceq 'cameraserver' })
        foreach ($victim in $victims) {
            [void](Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('kill', '-9', "$($victim.Id)") -TimeoutSeconds 10)
        }

        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Seconds 2
            $serverId = & $getServerId (& $getProcesses)
            $dump = [string](Invoke-AndroidShell -Adb $Adb -Serial $Serial -Command @('dumpsys', '-t', '10', 'media.camera') -TimeoutSeconds 20)
            $count = 0
            $found = [regex]::Match($dump, 'Number of camera devices: (\d+)', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
            if ($serverId -ne 0 -and $serverId -ne $oldServerId -and $found.Success -and [int]::TryParse($found.Groups[1].Value, [System.Globalization.NumberStyles]::None, $invariant, [ref]$count) -and $count -ge 1) {
                Write-Host ([string]::Format($invariant, 'Camera stack restarted: cameraserver pid {0} -> {1}, {2} camera device(s)', $oldServerId, $serverId, $count))
                return
            }
        }

        Write-DeviceTestWarning "The camera stack did not report a camera again within $TimeoutSeconds s of its restart; the retry runs anyway."
    }
    catch {
        Write-DeviceTestWarning "Restarting the camera stack failed ($($_.Exception.Message)); the retry runs anyway."
    }
}

$exitCode = 71
$script:AndroidRootedSerial = ''
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

    $coverageSession = Invoke-DeviceTestAppBuilds -Context $context -Platform 'android' -Target $Target -Property $buildProperties -BuildArgs $BuildArgs

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
            $apiLevelNumber = 0
            [void][int]::TryParse([string]$apiLevel, [System.Globalization.NumberStyles]::None, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$apiLevelNumber)
            $context.DeviceDescription = "$serial, API $apiLevel, $abi"
        }

        foreach ($item in $context.Projects) {
            if ($context.BuildFailures.ContainsKey($item.Name)) {
                $verdict = New-DeviceTestVerdict -ExitCode 71 -Directory (Join-Path $context.Results $item.Name) -Reason "Not run: $($context.BuildFailures[$item.Name])."
            }
            elseif ($deviceError) {
                $verdict = New-DeviceTestVerdict -ExitCode 81 -Directory (Join-Path $context.Results $item.Name) -Reason $deviceError
            }
            else {
                $verdict = Invoke-AndroidDeviceTests -Context $context -Project $item -Adb $adbPath -Serial $serial -Abi $abi -CoverageSession $coverageSession -ApiLevel $apiLevelNumber
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

    # Leaves a developer's emulator as it was; an emulator this script stops anyway needs no unroot.
    $stoppingEmulator = $null -ne $emulatorInstance -and $emulatorInstance.Started -and -not $KeepDevice
    if ($script:AndroidRootedSerial -and $adbPath -and -not $stoppingEmulator -and (Test-AndroidDeviceListed -Adb $adbPath -Serial $script:AndroidRootedSerial)) {
        [void](Invoke-DeviceTestProcess -FilePath $adbPath -ArgumentList @('-s', $script:AndroidRootedSerial, 'unroot') -TimeoutSeconds 30)
    }

    if ($null -ne $emulatorInstance -and $emulatorInstance.Started -and -not $KeepDevice -and $adbPath) {
        Stop-AndroidEmulator -Adb $adbPath -Emulator $emulatorInstance
    }
}

Write-Host "Exit code: $exitCode ($(Get-DeviceTestExitCodeName $exitCode))"
exit $exitCode
