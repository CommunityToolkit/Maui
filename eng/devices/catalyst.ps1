<#
.SYNOPSIS
    Builds the CommunityToolkit.Maui device test apps for Mac Catalyst and runs them on this Mac with XHarness.

.DESCRIPTION
    Mirrors dotnet/maui's eng/devices/catalyst.cake: 'dotnet build -f <tfm> -p:RuntimeIdentifier=maccatalyst-<arch>',
    then 'xharness apple test --target maccatalyst --set-env TestFilter=<filter>'. After every run the results gate in
    devices-shared.ps1 decides the exit code, because xharness reports SUCCESS for every Mac Catalyst crash.

    The app opens a window on this Mac while it runs, so it needs a logged-in GUI session (GitHub's macOS runners
    have one). Run one Mac Catalyst test app at a time.

    Exit codes use xharness's vocabulary: 0 passed, 1 test failures, 3 invalid arguments, 4 app not built,
    70 timed out, 71 build or script failure, 80 app crash, 94 no results or no tests ran. See eng/devices/README.md.

.EXAMPLE
    pwsh eng/devices/catalyst.ps1 -Project All
#>
[CmdletBinding()]
param(
    [ValidateSet('buildOnly', 'testOnly', 'buildAndTest')]
    [string]$Target = 'buildAndTest',

    # Device test app(s): CommunityToolkit.Maui.DeviceTests (default), MediaElement, Camera, All, or a .csproj path.
    [string[]]$Project = @('CommunityToolkit.Maui.DeviceTests'),

    [string]$Configuration = 'Debug',

    # Defaults to artifacts/device-tests/maccatalyst; each app writes to a sub-folder named after the project.
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

    # Xcode.app to use. Defaults to the one selected with xcode-select.
    [string]$Xcode,

    # Extra arguments for 'dotnet build', for example '/p:Name=Value' (a value starting with -p: is read as a parameter name).
    [string[]]$BuildArgs = @(),

    # Extra arguments for xharness, for example '--set-env=NAME=value'.
    [string[]]$XHarnessArgs = @()
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'devices-shared.ps1')
. (Join-Path $PSScriptRoot 'devices-coverage.ps1')

$exitCode = 71
$coverageSession = $null
try {
    if (-not $IsMacOS) {
        throw (New-Object System.ArgumentException 'Mac Catalyst device tests need macOS with Xcode.')
    }

    $context = Initialize-DeviceTestRun -Platform 'maccatalyst' -Leg 'maccatalyst' -Target $Target -Project $Project -Configuration $Configuration `
        -Results $Results -BinlogDir $BinlogDir -TestFilter $TestFilter -TestFilterIsBound $PSBoundParameters.ContainsKey('TestFilter') `
        -SkipCapabilities $SkipCapabilities -SkipCapabilitiesIsBound $PSBoundParameters.ContainsKey('SkipCapabilities') `
        -RequireCapabilities $RequireCapabilities -RequireCapabilitiesIsBound $PSBoundParameters.ContainsKey('RequireCapabilities') `
        -Timeout $Timeout -LaunchTimeout $LaunchTimeout -InfraRetries $InfraRetries `
        -Coverage $Coverage.IsPresent -CoverageIsBound $PSBoundParameters.ContainsKey('Coverage')

    # Build for this Mac's architecture only, as dotnet/maui does; an x64 app would run under Rosetta on arm64.
    $runtimeIdentifier = "maccatalyst-$(Get-DeviceTestHostArchitecture)"
    $buildProperties = @("RuntimeIdentifier=$runtimeIdentifier")

    if ($Target -ne 'testOnly') {
        foreach ($item in $context.Projects) {
            $framework = Get-DeviceTestTargetFramework -Project $item -Platform 'maccatalyst'
            [void](Invoke-DeviceTestBuild -Context $context -Project $item -TargetFramework $framework -BuildArgs $BuildArgs -Property $buildProperties)
        }
    }

    if ($Target -ne 'buildOnly') {
        Invoke-DeviceTestToolRestore
        $xcodeRoot = Get-DeviceTestXcodeRoot -Xcode $Xcode
        Write-Host "Xcode:         $xcodeRoot"
        $context.DeviceDescription = "$([System.Environment]::MachineName), macOS $([System.Environment]::OSVersion.Version), $runtimeIdentifier"

        if ($context.Coverage) {
            $coverageSession = Start-DeviceTestCoverage -Context $context -Platform 'maccatalyst' -Property $buildProperties -BuildArgs $BuildArgs
        }

        foreach ($item in $context.Projects) {
            $itemOutput = Join-Path $context.Results $item.Name
            if ($context.BuildFailures.ContainsKey($item.Name)) {
                $verdict = New-DeviceTestVerdict -ExitCode 71 -Directory $itemOutput -Reason "Not run: $($context.BuildFailures[$item.Name])."
            }
            else {
                $verdict = Invoke-AppleDeviceTests -Context $context -Project $item -Platform 'maccatalyst' -RuntimeIdentifier $runtimeIdentifier `
                    -XHarnessTarget 'maccatalyst' -XcodeRoot $xcodeRoot -ExtraArguments ($XHarnessArgs + (Get-DeviceTestCoverageArguments -Session $coverageSession -Project $item)) `
                    -OnFailure {
                        param($failureOutput)
                        # A screenshot shows a hung permission prompt or crash dialog. Only on CI: locally it would capture the user's screen.
                        if ($env:CI -eq 'true' -or $env:GITHUB_ACTIONS -eq 'true') {
                            [void](Invoke-DeviceTestProcess -FilePath 'screencapture' -ArgumentList @('-x', (Join-Path $failureOutput 'screen.png')) -TimeoutSeconds 60)
                        }
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
}

Write-Host "Exit code: $exitCode ($(Get-DeviceTestExitCodeName $exitCode))"
exit $exitCode
