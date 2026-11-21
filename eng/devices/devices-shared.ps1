# This file must keep working in Windows PowerShell 5.1 because the Windows jobs run windows.ps1 with it: no ternary,
# no null-coalescing, no pipeline chain operators and no -AsHashtable.
#
# The scripts mirror dotnet/maui's eng/devices/*.cake (targets buildOnly/testOnly/buildAndTest, TestFilter syntax,
# xharness arguments) and add one results gate for every platform, because xharness can report SUCCESS for a run
# that produced no results (see eng/devices/README.md).

$script:DeviceTestsRepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot (Join-Path '..' '..')))
$script:DeviceTestsOnWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT

$script:DeviceTestProjects = [ordered]@{
    'CommunityToolkit.Maui.DeviceTests' = 'Maui'
}

# The app never runs this category headless; the scripts also add it to every SkipCategories filter so the CI default
# can never select it.
$script:HumanOnlyTestCategory = 'RequiresUserInteraction'

# xharness exit codes (Microsoft.DotNet.XHarness.Common.CLI.ExitCode). Every script reports in this vocabulary,
# including Windows, which does not use xharness.
$script:DeviceTestExitCodeNames = @{
    0  = 'SUCCESS'
    1  = 'TESTS_FAILED'
    2  = 'HELP_SHOWN'
    3  = 'INVALID_ARGUMENTS'
    4  = 'PACKAGE_NOT_FOUND'
    70 = 'TIMED_OUT'
    71 = 'GENERAL_FAILURE'
    78 = 'PACKAGE_INSTALLATION_FAILURE'
    79 = 'FAILED_TO_GET_BUNDLE_INFO'
    80 = 'APP_CRASH'
    81 = 'DEVICE_NOT_FOUND'
    82 = 'RETURN_CODE_NOT_SET'
    83 = 'APP_LAUNCH_FAILURE'
    84 = 'DEVICE_FILE_COPY_FAILURE'
    86 = 'PACKAGE_INSTALLATION_TIMEOUT'
    87 = 'APP_NOT_SIGNED'
    88 = 'SIMULATOR_FAILURE'
    89 = 'DEVICE_FAILURE'
    90 = 'APP_LAUNCH_TIMEOUT'
    91 = 'ADB_FAILURE'
    92 = 'TCP_CONNECTION_FAILED'
    93 = 'APP_EXITED_BEFORE_TEST_START'
    94 = 'TEST_RESULTS_MISSING'
}

# Test failures (1), crashes (80, 93), hangs (70) and missing results (94) are never retried, so a retry cannot hide a
# real problem. The one exception is a known Android platform crash that explains the whole failure (Get-AndroidPlatformCrash).
# Get-DeviceTestVerdict never returns one of the codes below for results that contain a failed test, and
# Invoke-DeviceTestWithRetries never retries such a verdict.
$script:DeviceTestInfraExitCodes = @(78, 81, 83, 84, 86, 88, 89, 90, 91, 92)

function Get-DeviceTestExitCodeName {
    param([int]$ExitCode)

    if ($script:DeviceTestExitCodeNames.ContainsKey($ExitCode)) {
        return $script:DeviceTestExitCodeNames[$ExitCode]
    }

    return 'UNKNOWN'
}

function Write-DeviceTestHeader {
    param([string]$Message)

    Write-Host ''
    Write-Host ('=' * 100)
    Write-Host $Message
    Write-Host ('=' * 100)
}

function Write-DeviceTestError {
    param([string]$Message, [string]$Title = 'Device tests')

    if ($env:GITHUB_ACTIONS -eq 'true') {
        $escaped = $Message.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        $escapedTitle = $Title.Replace('%', '%25').Replace("`r", '').Replace("`n", ' ').Replace(',', '%2C').Replace(':', '%3A')
        Write-Host "::error title=$escapedTitle::$escaped"
    }
    else {
        Write-Host "ERROR: $Message" -ForegroundColor Red
    }
}

function Write-DeviceTestWarning {
    param([string]$Message, [string]$Title = 'Device tests')

    if ($env:GITHUB_ACTIONS -eq 'true') {
        $escaped = $Message.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        $escapedTitle = $Title.Replace('%', '%25').Replace("`r", '').Replace("`n", ' ').Replace(',', '%2C').Replace(':', '%3A')
        Write-Host "::warning title=$escapedTitle::$escaped"
    }
    else {
        Write-Host "WARNING: $Message" -ForegroundColor Yellow
    }
}

function Resolve-DeviceTestPath {
    # Unlike Resolve-Path, this accepts a path that does not exist yet.
    param([string]$Path)

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Format-DeviceTestCommandLine {
    param([string[]]$ArgumentList)

    $parts = foreach ($argument in $ArgumentList) {
        if ($argument -match '[\s"]' -or $argument.Length -eq 0) {
            '"' + $argument.Replace('"', '\"') + '"'
        }
        else {
            $argument
        }
    }

    return ($parts -join ' ')
}

function ConvertTo-WindowsCommandLineArgument {
    # The quoting follows CommandLineToArgvW (and so Environment.GetCommandLineArgs), which splits the argument back.
    param([string]$Argument)

    if ($Argument.Length -gt 0 -and $Argument -notmatch '[\s"]') {
        return $Argument
    }

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.Append('"')
    $backslashes = 0
    foreach ($character in $Argument.ToCharArray()) {
        if ($character -eq '\') {
            $backslashes++
            continue
        }

        if ($character -eq '"') {
            [void]$builder.Append('\', ($backslashes * 2) + 1)
            [void]$builder.Append('"')
        }
        else {
            if ($backslashes -gt 0) {
                [void]$builder.Append('\', $backslashes)
            }

            [void]$builder.Append($character)
        }

        $backslashes = 0
    }

    if ($backslashes -gt 0) {
        [void]$builder.Append('\', $backslashes * 2)
    }

    [void]$builder.Append('"')
    return $builder.ToString()
}

function Assert-DeviceTestCommand {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "'$Name' was not found on PATH."
    }
}

function Invoke-DeviceTestNative {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [string]$LogPath
    )

    Assert-DeviceTestCommand $FilePath
    Write-Host "> $FilePath $(Format-DeviceTestCommandLine $ArgumentList)"

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($LogPath) {
            $writer = New-Object System.IO.StreamWriter($LogPath, $true, (New-Object System.Text.UTF8Encoding($false)))
            try {
                & $FilePath @ArgumentList 2>&1 | ForEach-Object {
                    $line = "$_"
                    $writer.WriteLine($line)
                    Write-Host $line
                }
            }
            finally {
                $writer.Dispose()
            }
        }
        else {
            & $FilePath @ArgumentList 2>&1 | ForEach-Object { Write-Host "$_" }
        }

        return [int]$LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Invoke-DeviceTestNativeCapture {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$ArgumentList = @()
    )

    Assert-DeviceTestCommand $FilePath

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $FilePath @ArgumentList 2>$null
        $exitCode = [int]$LASTEXITCODE
        $lines = @()
        if ($null -ne $output) {
            $lines = @($output | ForEach-Object { "$_" })
        }

        return New-Object PSObject -Property @{ ExitCode = $exitCode; Output = $lines }
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Invoke-DeviceTestProcess {
    # Device tools such as adb and simctl can hang, hence the timeout.
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [int]$TimeoutSeconds = 60
    )

    $command = Get-Command $FilePath -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        throw "'$FilePath' was not found."
    }

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $command.Path
    $startInfo.Arguments = (($ArgumentList | ForEach-Object { ConvertTo-WindowsCommandLineArgument $_ }) -join ' ')
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) {
        try {
            $process.Kill()
        }
        catch {
            Write-Verbose "Could not kill $($FilePath): $($_.Exception.Message)"
        }
    }

    $process.WaitForExit()
    $exitCode = -1
    if (-not $timedOut) {
        $exitCode = $process.ExitCode
    }

    # A daemon started by the command (for example the adb server) can inherit the pipes and keep them open.
    $output = ''
    if ($stdout.Wait(5000)) {
        $output = $stdout.Result
    }

    $errorOutput = ''
    if ($stderr.Wait(5000)) {
        $errorOutput = $stderr.Result
    }

    return New-Object PSObject -Property @{
        ExitCode = $exitCode
        TimedOut = $timedOut
        Output   = $output
        Error    = $errorOutput
    }
}

function Resolve-DeviceTestProjects {
    # Comma-separated values are accepted because "pwsh -File" passes "-Project A,B" as a single string.
    param([string[]]$Project)

    $tokens = @()
    foreach ($value in $Project) {
        foreach ($token in ($value -split ',')) {
            if ($token.Trim().Length -gt 0) {
                $tokens += $token.Trim()
            }
        }
    }

    if ($tokens.Count -eq 0) {
        $tokens = @('Maui')
    }

    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($token in $tokens) {
        $names = @()
        if ($token -eq 'All') {
            $names = @($script:DeviceTestProjects.Keys)
        }
        else {
            foreach ($name in $script:DeviceTestProjects.Keys) {
                if ($token -eq $name -or $token -eq $script:DeviceTestProjects[$name]) {
                    $names = @($name)
                }
            }
        }

        $candidates = @()
        if ($names.Count -gt 0) {
            foreach ($name in $names) {
                $candidates += Join-Path $script:DeviceTestsRepoRoot (Join-Path 'src' (Join-Path $name "$name.csproj"))
            }
        }
        elseif (@('MediaElement', 'Camera', 'CommunityToolkit.Maui.MediaElement.DeviceTests', 'CommunityToolkit.Maui.Camera.DeviceTests') -contains $token) {
            throw (New-Object System.ArgumentException "The $token tests are in CommunityToolkit.Maui.DeviceTests: use -TestFilter 'Category=<name>' with a name from src/CommunityToolkit.Maui.DeviceTests/MediaElement/TestCategory.MediaElement.cs or Camera/TestCategory.Camera.cs.")
        }
        else {
            $resolved = Resolve-DeviceTestPath $token
            if (Test-Path -LiteralPath $resolved -PathType Container) {
                $found = @(Get-ChildItem -LiteralPath $resolved -Filter '*.csproj' -File)
                if ($found.Count -ne 1) {
                    throw (New-Object System.ArgumentException "Expected exactly one .csproj in '$resolved' but found $($found.Count).")
                }

                $candidates += $found[0].FullName
            }
            elseif (Test-Path -LiteralPath $resolved -PathType Leaf) {
                $candidates += $resolved
            }
            else {
                $known = (@($script:DeviceTestProjects.Keys) + @($script:DeviceTestProjects.Values) + 'All') -join ', '
                throw (New-Object System.ArgumentException "Unknown device test project '$token'. Use a .csproj path or one of: $known.")
            }
        }

        foreach ($candidate in $candidates) {
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
                throw (New-Object System.ArgumentException "Device test project not found: $candidate")
            }

            $full = [System.IO.Path]::GetFullPath($candidate)
            if (-not $paths.Contains($full)) {
                $paths.Add($full)
            }
        }
    }

    $result = @()
    foreach ($path in $paths) {
        $result += New-Object PSObject -Property @{
            Name      = [System.IO.Path]::GetFileNameWithoutExtension($path)
            Path      = $path
            Directory = [System.IO.Path]::GetDirectoryName($path)
        }
    }

    return , $result
}

function Resolve-DeviceTestFilter {
    param(
        [string]$TestFilter,
        [bool]$IsBound
    )

    $value = $TestFilter
    if (-not $IsBound) {
        $value = $env:TEST_FILTER
    }

    if ($null -eq $value) {
        $value = ''
    }

    $value = $value.Trim()
    # cmd.exe passes single quotes through ("powershell -File ... -TestFilter 'Category=X'").
    if ($value.Length -ge 2 -and (($value.StartsWith("'") -and $value.EndsWith("'")) -or ($value.StartsWith('"') -and $value.EndsWith('"')))) {
        $value = $value.Substring(1, $value.Length - 2).Trim()
    }

    $human = $script:HumanOnlyTestCategory

    if ($value.Length -eq 0) {
        return "SkipCategories=$human"
    }

    if ($value.StartsWith('Category=', [System.StringComparison]::Ordinal)) {
        $category = $value.Substring('Category='.Length).Trim()
        if ($category.Length -eq 0 -or $category -match '[,;=]') {
            throw (New-Object System.ArgumentException "Invalid TestFilter '$value': Category= takes exactly one category name.")
        }

        return "Category=$category"
    }

    if ($value.StartsWith('SkipCategories=', [System.StringComparison]::Ordinal)) {
        $categories = New-Object System.Collections.Generic.List[string]
        foreach ($category in ($value.Substring('SkipCategories='.Length) -split '[,;]')) {
            $trimmed = $category.Trim()
            if ($trimmed.Length -gt 0 -and -not ($categories -contains $trimmed)) {
                $categories.Add($trimmed)
            }
        }

        if (-not ($categories -contains $human)) {
            $categories.Add($human)
        }

        return 'SkipCategories=' + ($categories -join ',')
    }

    throw (New-Object System.ArgumentException "Invalid TestFilter '$value'. Use 'Category=<name>' or 'SkipCategories=<name>,<name>' (dotnet/maui syntax).")
}

function Resolve-DeviceTestCapabilities {
    # Only the form of each name is checked here: the app checks the names against its capabilities (DeviceTestCapabilities
    # in CommunityToolkit.Maui.DeviceTests.Shared).
    param(
        [string[]]$Value,
        [bool]$IsBound,
        [string]$EnvironmentVariable,
        [string]$Name
    )

    $text = $Value -join ','
    if (-not $IsBound) {
        $text = [System.Environment]::GetEnvironmentVariable($EnvironmentVariable)
    }

    $names = New-Object System.Collections.Generic.List[string]
    if ($text) {
        # cmd.exe passes single quotes through ("powershell -File ... -SkipCapabilities 'A,B'").
        foreach ($item in ($text.Trim().Trim("'", '"') -split '[,;\s]+')) {
            if ($item.Length -eq 0) {
                continue
            }

            if ($item -cnotmatch '^[A-Za-z][A-Za-z0-9]*$') {
                throw (New-Object System.ArgumentException "Invalid -$Name '$text': '$item' is not a capability name, such as VideoRendering.")
            }

            if (-not $names.Contains($item)) {
                $names.Add($item)
            }
        }
    }

    return , $names.ToArray()
}

function Get-DeviceTestCapabilityDeclarations {
    # The format must match what the app reads (DeviceTestCapabilities).
    param([Parameter(Mandatory = $true)]$Context)

    if ($Context.SkipCapabilities.Count -gt 0) {
        "SkipCapabilities=$($Context.SkipCapabilities -join ',')"
    }

    if ($Context.RequireCapabilities.Count -gt 0) {
        "RequireCapabilities=$($Context.RequireCapabilities -join ',')"
    }
}

function Format-DeviceTestCapabilities {
    param([Parameter(Mandatory = $true)]$Context)

    if ($Context.SkipCapabilities.Count -eq 0 -and $Context.RequireCapabilities.Count -eq 0) {
        return 'none declared'
    }

    $missing = 'none'
    if ($Context.SkipCapabilities.Count -gt 0) {
        $missing = $Context.SkipCapabilities -join ', '
    }

    $required = 'none'
    if ($Context.RequireCapabilities.Count -gt 0) {
        $required = $Context.RequireCapabilities -join ', '
    }

    return "missing $missing; required $required"
}

function ConvertTo-DeviceTestSeconds {
    param([string]$TimeSpan, [string]$Name)

    $parsed = [System.TimeSpan]::Zero
    if (-not [System.TimeSpan]::TryParse($TimeSpan, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed) -or $parsed.TotalSeconds -le 0) {
        throw (New-Object System.ArgumentException "Invalid -$Name '$TimeSpan'. Use hh:mm:ss, for example 00:30:00.")
    }

    return [int][Math]::Ceiling($parsed.TotalSeconds)
}

function Initialize-DeviceTestRun {
    param(
        [Parameter(Mandatory = $true)][string]$Platform,
        [Parameter(Mandatory = $true)][string]$Leg,
        [Parameter(Mandatory = $true)][string]$Target,
        [string[]]$Project,
        [string]$Configuration,
        [string]$Results,
        [string]$BinlogDir,
        [string]$TestFilter,
        [bool]$TestFilterIsBound,
        [string[]]$SkipCapabilities,
        [bool]$SkipCapabilitiesIsBound,
        [string[]]$RequireCapabilities,
        [bool]$RequireCapabilitiesIsBound,
        [string]$Timeout,
        [string]$LaunchTimeout,
        [int]$InfraRetries,
        [bool]$Coverage,
        [bool]$CoverageIsBound
    )

    $projects = Resolve-DeviceTestProjects -Project $Project
    $skip = Resolve-DeviceTestCapabilities -Value $SkipCapabilities -IsBound $SkipCapabilitiesIsBound -EnvironmentVariable 'DEVICE_TEST_SKIP_CAPABILITIES' -Name 'SkipCapabilities'
    $require = Resolve-DeviceTestCapabilities -Value $RequireCapabilities -IsBound $RequireCapabilitiesIsBound -EnvironmentVariable 'DEVICE_TEST_REQUIRE_CAPABILITIES' -Name 'RequireCapabilities'
    $both = @($skip | Where-Object { $require -ccontains $_ })
    if ($both.Count -gt 0) {
        throw (New-Object System.ArgumentException "Capabilities declared both missing and required: $($both -join ', ').")
    }

    if (-not $Results) {
        $Results = Join-Path $script:DeviceTestsRepoRoot (Join-Path 'artifacts' (Join-Path 'device-tests' $Leg))
    }

    if (-not $BinlogDir) {
        $BinlogDir = Join-Path $script:DeviceTestsRepoRoot (Join-Path 'artifacts' 'logs')
    }

    if ($InfraRetries -lt 0) {
        $InfraRetries = 0
        if ($env:CI -eq 'true' -or $env:GITHUB_ACTIONS -eq 'true') {
            $InfraRetries = 1
        }
    }

    if (-not $CoverageIsBound) {
        $Coverage = $env:DEVICE_TEST_COVERAGE -eq 'true'
    }

    $context = New-Object PSObject -Property @{
        Platform        = $Platform
        Leg             = $Leg
        Target          = $Target
        Projects        = $projects
        Configuration   = $Configuration
        Results         = (Resolve-DeviceTestPath $Results)
        BinlogDir       = (Resolve-DeviceTestPath $BinlogDir)
        TestFilter      = (Resolve-DeviceTestFilter -TestFilter $TestFilter -IsBound $TestFilterIsBound)
        SkipCapabilities    = $skip
        RequireCapabilities = $require
        Timeout         = $Timeout
        TimeoutSeconds  = (ConvertTo-DeviceTestSeconds -TimeSpan $Timeout -Name 'Timeout')
        LaunchTimeout   = $LaunchTimeout
        LaunchSeconds   = (ConvertTo-DeviceTestSeconds -TimeSpan $LaunchTimeout -Name 'LaunchTimeout')
        InfraRetries    = $InfraRetries
        Coverage        = $Coverage
        BuildFailures   = @{}
        Builds          = (New-Object System.Collections.ArrayList)
        Verdicts        = (New-Object System.Collections.ArrayList)
        DeviceDescription = ''
    }

    New-Item -ItemType Directory -Force -Path $context.Results | Out-Null
    New-Item -ItemType Directory -Force -Path $context.BinlogDir | Out-Null

    Write-DeviceTestHeader "Device tests: $Leg ($Target)"
    Write-Host "Projects:      $(($projects | ForEach-Object { $_.Name }) -join ', ')"
    Write-Host "Configuration: $Configuration"
    Write-Host "TestFilter:    $($context.TestFilter)"
    Write-Host "Capabilities:  $(Format-DeviceTestCapabilities -Context $context)"
    Write-Host "Results:       $($context.Results)"
    Write-Host "Binlogs:       $($context.BinlogDir)"
    Write-Host "Timeouts:      run $Timeout, launch $LaunchTimeout, infra retries $InfraRetries"
    if ($Coverage -and $Target -ne 'buildOnly') {
        Write-Host "Coverage:      toolkit assemblies instrumented with coverlet, report in $(Join-Path $context.Results 'coverage')"
    }

    return $context
}

function Get-DeviceTestMSBuildProperties {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string[]]$Name,
        [string[]]$Property = @()
    )

    $arguments = @('msbuild', $ProjectPath, '-nologo')
    foreach ($item in $Name) {
        $arguments += "-getProperty:$item"
    }

    foreach ($item in $Property) {
        $arguments += "-p:$item"
    }

    $result = Invoke-DeviceTestNativeCapture -FilePath 'dotnet' -ArgumentList $arguments
    $text = ($result.Output -join "`n").Trim()
    if ($result.ExitCode -ne 0) {
        throw "dotnet $(Format-DeviceTestCommandLine $arguments) failed with exit code $($result.ExitCode):`n$text"
    }

    $values = @{}
    if ($Name.Count -eq 1) {
        $values[$Name[0]] = $text
        return $values
    }

    $start = $text.IndexOf('{')
    $end = $text.LastIndexOf('}')
    if ($start -lt 0 -or $end -le $start) {
        throw "Unexpected output from dotnet $(Format-DeviceTestCommandLine $arguments):`n$text"
    }

    $json = $text.Substring($start, $end - $start + 1) | ConvertFrom-Json
    foreach ($item in $Name) {
        $values[$item] = ''
        $member = $json.Properties.PSObject.Properties[$item]
        if ($null -ne $member -and $null -ne $member.Value) {
            $values[$item] = [string]$member.Value
        }
    }

    return $values
}

function Get-DeviceTestTargetFramework {
    # The target framework is read from the project, so the scripts follow the repo's NetVersion instead of hard-coding net10.0.
    param(
        [Parameter(Mandatory = $true)]$Project,
        [Parameter(Mandatory = $true)][string]$Platform
    )

    $frameworks = (Get-DeviceTestMSBuildProperties -ProjectPath $Project.Path -Name 'TargetFrameworks')['TargetFrameworks']
    foreach ($framework in ($frameworks -split ';')) {
        $trimmed = $framework.Trim()
        if ($trimmed -match "^net\d+\.\d+-$Platform") {
            return $trimmed
        }
    }

    throw "$($Project.Name) has no $Platform target framework on this machine (TargetFrameworks: '$frameworks')."
}

function Get-DeviceTestBinlogPath {
    param($Context, $Project, [string]$Suffix)

    $name = "$($Project.Name)-$($Context.Leg)-$($Context.Configuration)"
    if ($Suffix) {
        $name = "$name-$Suffix"
    }

    return Join-Path $Context.BinlogDir "$name.binlog"
}

function Invoke-DeviceTestBuild {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)]$Project,
        [Parameter(Mandatory = $true)][string]$TargetFramework,
        [string]$Command = 'build',
        [string[]]$Property = @(),
        [string[]]$BuildArgs = @()
    )

    $label = $Project.Name

    Write-DeviceTestHeader "Build $label ($TargetFramework, $($Context.Configuration))"

    $arguments = @($Command, $Project.Path, '-f', $TargetFramework, '-c', $Context.Configuration, "-bl:$(Get-DeviceTestBinlogPath -Context $Context -Project $Project)")
    foreach ($item in $Property) {
        $arguments += "-p:$item"
    }

    $arguments += $BuildArgs

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $exitCode = Invoke-DeviceTestNative -FilePath 'dotnet' -ArgumentList $arguments
    $stopwatch.Stop()

    [void]$Context.Builds.Add((New-Object PSObject -Property @{ Project = $label; ExitCode = $exitCode; Duration = $stopwatch.Elapsed }))
    if ($exitCode -ne 0) {
        $Context.BuildFailures[$Project.Name] = "dotnet $Command exited with code $exitCode"
        Write-DeviceTestError -Title "Device tests ($($Context.Leg))" -Message "Build of $label failed (exit code $exitCode). See the binlog in $($Context.BinlogDir)."
        return $false
    }

    Write-Host "Built $label in $([int]$stopwatch.Elapsed.TotalSeconds) s"
    return $true
}

function Add-DeviceTestVerdict {
    param([Parameter(Mandatory = $true)]$Context, [Parameter(Mandatory = $true)]$Verdict)

    [void]$Context.Verdicts.Add($Verdict)
    Write-DeviceTestVerdict -Context $Context -Verdict $Verdict
}

function Initialize-DeviceTestResultsDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Move-DeviceTestAttempt {
    # A retry must not overwrite the evidence of the failed attempt.
    param([string]$Path, [int]$Attempt)

    $destination = Join-Path $Path "attempt-$Attempt"
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Get-ChildItem -LiteralPath $Path -Force | Where-Object { $_.Name -notlike 'attempt-*' } | ForEach-Object {
        Move-Item -LiteralPath $_.FullName -Destination $destination -Force
    }
}

function Get-DeviceTestResultFiles {
    param([string]$Platform, [string]$Directory)

    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        return , @()
    }

    $files = @()
    switch ($Platform) {
        'android' {
            $files = @(Get-ChildItem -LiteralPath $Directory -Filter 'testResults.xml' -File)
        }
        'windows' {
            $files = @(Get-ChildItem -LiteralPath $Directory -Filter 'TestResults-*.xml' -File)
        }
        default {
            # dotnet/maui reads xunit-test-*.xml (devices-shared.cake); also accept any xUnit v2 XML the run wrote, except
            # xharness's own vsts-*.xml: when the app cannot be launched, crashes or times out, xharness writes one with a
            # single synthetic failed test (for Azure DevOps), which is not a test result.
            $files = @(Get-ChildItem -LiteralPath $Directory -Filter 'xunit-test-*.xml' -File)
            if ($files.Count -eq 0) {
                $files = @(Get-ChildItem -LiteralPath $Directory -Filter '*.xml' -File | Where-Object { -not $_.Name.StartsWith('vsts-', [System.StringComparison]::OrdinalIgnoreCase) } | Where-Object {
                        $isXunit = $false
                        try {
                            $reader = [System.Xml.XmlReader]::Create($_.FullName)
                            try {
                                [void]$reader.MoveToContent()
                                $isXunit = $reader.LocalName -eq 'assemblies'
                            }
                            finally {
                                $reader.Dispose()
                            }
                        }
                        catch {
                            $isXunit = $false
                        }

                        $isXunit
                    })
            }
        }
    }

    return , $files
}

function ConvertTo-DeviceTestInt {
    param([string]$Value)

    $parsed = 0
    if ([int]::TryParse($Value, [ref]$parsed)) {
        return $parsed
    }

    return 0
}

function Read-DeviceTestResults {
    param([System.IO.FileInfo[]]$Files)

    $summary = New-Object PSObject -Property @{
        Files      = @()
        Assemblies = 0
        Total      = 0
        Passed     = 0
        Failed     = 0
        Skipped    = 0
        Errors     = 0
        Failures   = @()
        Corrupt    = @()
    }

    foreach ($file in $Files) {
        $summary.Files += $file.Name
        $document = New-Object System.Xml.XmlDocument
        try {
            $document.Load($file.FullName)
        }
        catch {
            $summary.Corrupt += "$($file.Name): $($_.Exception.Message)"
            continue
        }

        $assemblies = $document.SelectNodes('/assemblies/assembly')
        if ($assemblies.Count -eq 0) {
            $summary.Corrupt += "$($file.Name): no /assemblies/assembly element"
            continue
        }

        foreach ($assembly in $assemblies) {
            $summary.Assemblies++
            $summary.Total += ConvertTo-DeviceTestInt $assembly.GetAttribute('total')
            $summary.Passed += ConvertTo-DeviceTestInt $assembly.GetAttribute('passed')
            $summary.Failed += ConvertTo-DeviceTestInt $assembly.GetAttribute('failed')
            $summary.Skipped += ConvertTo-DeviceTestInt $assembly.GetAttribute('skipped')
            $summary.Errors += ConvertTo-DeviceTestInt $assembly.GetAttribute('errors')
        }

        foreach ($test in $document.SelectNodes("//test[@result='Fail']")) {
            $message = ''
            $messageNode = $test.SelectSingleNode('failure/message')
            if ($null -ne $messageNode) {
                $message = $messageNode.InnerText
            }

            # The XHarness runner writes only the method name (plus theory arguments) in 'name'; add the class from 'type'.
            $name = $test.GetAttribute('name')
            $type = $test.GetAttribute('type')
            if ($type -and -not $name.StartsWith("$type.", [System.StringComparison]::Ordinal)) {
                $name = "$($type.Substring($type.LastIndexOf('.') + 1)).$name"
            }

            $summary.Failures += New-Object PSObject -Property @{ Name = $name; Message = $message }
        }

        foreach ($errorNode in $document.SelectNodes('/assemblies/assembly/errors/error')) {
            $message = ''
            $messageNode = $errorNode.SelectSingleNode('failure/message')
            if ($null -ne $messageNode) {
                $message = $messageNode.InnerText
            }

            $name = "[$($errorNode.GetAttribute('type')) error] $($errorNode.GetAttribute('name'))"
            $summary.Failures += New-Object PSObject -Property @{ Name = $name.Trim(); Message = $message }
        }
    }

    return $summary
}

function Get-DeviceTestJsonValue {
    param($Object, [string]$Path)

    $current = $Object
    foreach ($segment in ($Path -split '\.')) {
        if ($null -eq $current) {
            return $null
        }

        $member = $current.PSObject.Properties[$segment]
        if ($null -eq $member) {
            return $null
        }

        $current = $member.Value
    }

    return $current
}

function Read-XHarnessResult {
    param([string]$Directory)

    $path = Join-Path $Directory 'xharness-result.json'
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        return $null
    }

    try {
        return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json)
    }
    catch {
        Write-DeviceTestWarning "Could not parse $($path): $($_.Exception.Message)"
        return $null
    }
}

function Get-CrashReportSimulatorId {
    # Returns the simulator UDID a copied .ips belongs to, '' when it names none, and $null when it cannot be read.
    param([string]$Directory, [string]$ReportName)

    $path = Join-Path $Directory $ReportName
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $found = Get-ChildItem -LiteralPath $Directory -Filter "$([System.IO.Path]::GetFileNameWithoutExtension($ReportName))*.ips" -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -eq $found) {
            return $null
        }

        $path = $found.FullName
    }

    try {
        $text = Get-Content -LiteralPath $path -Raw
    }
    catch {
        return $null
    }

    if ($text -match 'com\.apple\.CoreSimulator\.SimDevice\.([0-9A-Fa-f-]{36})' -or $text -match 'CoreSimulator\\?/Devices\\?/([0-9A-Fa-f-]{36})') {
        return $Matches[1]
    }

    return ''
}

function Get-DeviceTestVerdict {
    # The results gate: the exit code comes from what the run left on disk, never from the runner's exit code
    # alone, because xharness reports SUCCESS when an iOS app crashes before the test runner starts and for every
    # Mac Catalyst crash (no results file is treated as a device communication problem).
    param(
        [Parameter(Mandatory = $true)][string]$Platform,
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][int]$RunnerExitCode,
        [string[]]$Problems = @(),
        [int]$ProblemExitCode = 94,
        [int]$Attempt = 1
    )

    $reasons = New-Object System.Collections.Generic.List[string]
    foreach ($problem in $Problems) {
        $reasons.Add($problem)
    }

    $files = Get-DeviceTestResultFiles -Platform $Platform -Directory $Directory
    $results = Read-DeviceTestResults -Files $files
    $xharness = $null
    $crashReport = $null
    $resultFileMissing = $false

    if ($Platform -eq 'ios' -or $Platform -eq 'maccatalyst') {
        $xharness = Read-XHarnessResult -Directory $Directory
        if ($null -ne $xharness) {
            $matched = Get-DeviceTestJsonValue $xharness 'appleLaunch.crashReport.matchedReport'
            if ($null -ne $matched) {
                $matchedName = Get-DeviceTestJsonValue $matched 'name'
                $targetId = Get-DeviceTestJsonValue $xharness 'environment.target.identifier'
                $reportSimulator = $null
                if ($Platform -eq 'ios' -and $targetId) {
                    $reportSimulator = Get-CrashReportSimulatorId -Directory $Directory -ReportName $matchedName
                }

                # Simulators share one DiagnosticReports folder, so xharness can match, by process name, the crash report of
                # another run's app on another simulator. A report that cannot be read still fails the run.
                if ($reportSimulator -and $reportSimulator -ne $targetId) {
                    Write-DeviceTestWarning "Ignored crash report $matchedName of simulator $reportSimulator (another run); this run's simulator is $targetId."
                }
                else {
                    $crashReport = $matchedName
                    $reasons.Add("xharness matched a crash report for the app: $crashReport")
                }
            }

            if ((Get-DeviceTestJsonValue $xharness 'appleLaunch.testResultFile.exists') -eq $false) {
                $resultFileMissing = $true
                $reasons.Add('xharness-result.json: appleLaunch.testResultFile.exists is false')
            }
        }
    }
    elseif ($Platform -eq 'android') {
        $xharness = Read-XHarnessResult -Directory $Directory
    }

    if ($files.Count -eq 0) {
        $reasons.Add("no test results file in $Directory")
    }

    foreach ($corrupt in $results.Corrupt) {
        $reasons.Add("unreadable results: $corrupt")
    }

    # Results count only when they are the runner's: xharness saying that its results file does not exist means that what
    # was found is not this run's.
    $readable = $files.Count -gt 0 -and $results.Corrupt.Count -eq 0 -and -not $resultFileMissing
    $testsFailed = $readable -and ($results.Failed + $results.Errors) -gt 0
    $allSkipped = $readable -and $results.Total -gt 0 -and ($results.Passed + $results.Failed + $results.Errors) -eq 0

    if ($readable -and $results.Total -eq 0) {
        $reasons.Add('the results report 0 tests')
    }

    if ($allSkipped) {
        $reasons.Add("no test ran: all $($results.Total) test(s) were skipped")
    }

    if ($RunnerExitCode -ne 0) {
        $reasons.Add("runner exit code $RunnerExitCode ($(Get-DeviceTestExitCodeName $RunnerExitCode))")
    }

    $exitCode = 0
    if ($null -ne $crashReport) {
        $exitCode = 80
    }
    elseif ($testsFailed -and $script:DeviceTestInfraExitCodes -contains $RunnerExitCode) {
        # A failed test is never reported as an infrastructure failure, which would be retried: a passing retry would hide
        # it. xharness returns 88 when, for example, the logcat dump fails after the results were pulled.
        $exitCode = 1
    }
    elseif ($RunnerExitCode -ne 0 -and $RunnerExitCode -ne 1) {
        $exitCode = $RunnerExitCode
    }
    elseif ($Problems.Count -gt 0) {
        $exitCode = $ProblemExitCode
    }
    elseif (-not $readable -or $results.Total -eq 0) {
        $exitCode = 94
    }
    elseif ($testsFailed) {
        $exitCode = 1
    }
    elseif ($allSkipped) {
        $exitCode = 94
    }
    elseif ($RunnerExitCode -ne 0) {
        $exitCode = $RunnerExitCode
    }

    $warnings = New-Object System.Collections.Generic.List[string]

    $device = ''
    $deviceOs = ''
    if ($null -ne $xharness) {
        $device = [string](Get-DeviceTestJsonValue $xharness 'device')
        $deviceOs = [string](Get-DeviceTestJsonValue $xharness 'deviceOsVersion')
    }

    return New-Object PSObject -Property @{
        ExitCode       = $exitCode
        ExitCodeName   = (Get-DeviceTestExitCodeName $exitCode)
        RunnerExitCode = $RunnerExitCode
        Total          = $results.Total
        Passed         = $results.Passed
        Failed         = $results.Failed
        Skipped        = $results.Skipped
        Errors         = $results.Errors
        Failures       = $results.Failures
        ResultFiles    = $results.Files
        Reasons        = @($reasons)
        Warnings       = @($warnings)
        Device         = $device
        DeviceOs       = $deviceOs
        Attempt        = $Attempt
        Retried        = @()
        Attempts       = @()
        Directory      = $Directory
        Project        = ''
        Duration       = [System.TimeSpan]::Zero
    }
}

function New-DeviceTestVerdict {
    param([int]$ExitCode, [string]$Reason, [string]$Directory)

    return New-Object PSObject -Property @{
        ExitCode       = $ExitCode
        ExitCodeName   = (Get-DeviceTestExitCodeName $ExitCode)
        RunnerExitCode = $ExitCode
        Total          = 0
        Passed         = 0
        Failed         = 0
        Skipped        = 0
        Errors         = 0
        Failures       = @()
        ResultFiles    = @()
        Reasons        = @($Reason)
        Warnings       = @()
        Device         = ''
        DeviceOs       = ''
        Attempt        = 1
        Retried        = @()
        Attempts       = @()
        Directory      = $Directory
        Project        = ''
        Duration       = [System.TimeSpan]::Zero
    }
}

function Get-DeviceTestPlatformCrashMessage {
    param($Verdict, $PlatformCrash, [int]$Number, [string]$Evidence, [bool]$WillRetry, [string]$NotRetriedBecause)

    $outcome = [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, 'Attempt {0} ended with {1} ({2})', $Number, $Verdict.ExitCode, $Verdict.ExitCodeName)
    if ($PlatformCrash.Error) {
        return "$outcome. The check for known platform crashes failed ($($PlatformCrash.Error)); the leg was not retried."
    }

    $known = "$outcome in a known platform crash: $($PlatformCrash.Title). Evidence: $Evidence."
    if ($PlatformCrash.Detail) {
        $known = "$known $($PlatformCrash.Detail)"
    }

    if ($WillRetry) {
        return "$known The leg ran once more; attempt $($Number + 1) decides the result."
    }

    return "$known It was not retried: $NotRetriedBecause."
}

function Invoke-DeviceTestWithRetries {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)]$Project,
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][scriptblock]$Attempt,
        [scriptblock]$BeforeRetry,
        [scriptblock]$FindPlatformCrash
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $retried = @()
    $attempts = @()
    $notes = @()
    $number = 1
    while ($true) {
        $verdict = & $Attempt $number
        $isInfrastructureFailure = $script:DeviceTestInfraExitCodes -contains $verdict.ExitCode
        $hasFailedTests = ($verdict.Failed + $verdict.Errors) -gt 0
        # An attempt can say that another one cannot help (NoRetry), for example when the device it ran on is gone.
        $noRetry = $null -ne $verdict.PSObject.Properties['NoRetry'] -and $verdict.NoRetry
        $hasRetryLeft = $number -le $Context.InfraRetries
        $isFirstRetry = $retried.Count -eq 0

        $platformCrash = $null
        if ($null -ne $FindPlatformCrash -and $verdict.ExitCode -ne 0 -and -not $isInfrastructureFailure -and -not $noRetry) {
            try {
                $found = @(& $FindPlatformCrash $verdict)
                if ($found.Count -eq 1) {
                    $platformCrash = $found[0]
                }
            }
            catch {
                $platformCrash = New-Object PSObject -Property @{ Id = ''; Title = ''; Explained = $false; Blocker = ''; Evidence = @(); Detail = ''; Error = $_.Exception.Message }
            }
        }

        $isPlatformCrashRetry = $null -ne $platformCrash -and $platformCrash.Explained -eq $true -and -not $platformCrash.Error
        $isInfrastructureRetry = $isInfrastructureFailure -and -not $hasFailedTests
        $willRetry = $verdict.ExitCode -ne 0 -and -not $noRetry -and $hasRetryLeft -and ($isInfrastructureRetry -or ($isPlatformCrashRetry -and $isFirstRetry))

        $attemptRecord = New-Object PSObject -Property ([ordered]@{
            Attempt           = $number
            ExitCode          = $verdict.ExitCode
            ExitCodeName      = $verdict.ExitCodeName
            Folder            = '.'
            PlatformCrashId   = ''
            PlatformCrash     = ''
            Evidence          = ''
            Retried           = $willRetry
            NotRetriedBecause = ''
        })

        $platformCrashMessage = ''
        if ($null -ne $platformCrash) {
            $attemptRecord.PlatformCrashId = $platformCrash.Id
            $attemptRecord.PlatformCrash = $platformCrash.Title
            # Every evidence file moves into attempt-N/ with the retry.
            $evidencePrefix = ''
            if ($willRetry) {
                $evidencePrefix = "attempt-$number/"
            }

            $evidencePath = (@($platformCrash.Evidence | Where-Object { $_ } | ForEach-Object { "$evidencePrefix$_" }) -join '; ')

            $attemptRecord.Evidence = $evidencePath
            if (-not $willRetry) {
                if ($platformCrash.Error) {
                    $attemptRecord.NotRetriedBecause = "the check failed: $($platformCrash.Error)"
                }
                elseif (-not $platformCrash.Explained) {
                    $attemptRecord.NotRetriedBecause = $platformCrash.Blocker
                }
                elseif (-not $isFirstRetry) {
                    $attemptRecord.NotRetriedBecause = 'the single retry was already used'
                }
                else {
                    $attemptRecord.NotRetriedBecause = 'it would be retried once on CI (-InfraRetries is 0 here)'
                }
            }

            $platformCrashMessage = Get-DeviceTestPlatformCrashMessage -Verdict $verdict -PlatformCrash $platformCrash -Number $number -Evidence $evidencePath -WillRetry $willRetry -NotRetriedBecause $attemptRecord.NotRetriedBecause
            $notes += $platformCrashMessage
        }

        if (-not $willRetry) {
            $attempts += $attemptRecord
            break
        }

        if ($willRetry -and $isPlatformCrashRetry) {
            Write-DeviceTestWarning -Title "Device tests ($($Context.Leg))" -Message "$($Project.Name): attempt $number ended with $($verdict.ExitCode) ($($verdict.ExitCodeName)) in the known platform crash $($platformCrash.Id); retrying."
            $retried += "$($verdict.ExitCode) ($($verdict.ExitCodeName)), known platform crash $($platformCrash.Id)"
        }
        else {
            Write-DeviceTestWarning -Title "Device tests ($($Context.Leg))" -Message "$($Project.Name): attempt $number ended with $($verdict.ExitCode) ($($verdict.ExitCodeName)), an infrastructure failure; retrying."
            $retried += "$($verdict.ExitCode) ($($verdict.ExitCodeName))"
        }

        Move-DeviceTestAttempt -Path $Directory -Attempt $number
        $attemptRecord.Folder = "attempt-$number"
        $attempts += $attemptRecord
        $verdict.Attempts = $attempts
        if ($platformCrashMessage) {
            $verdict.Reasons = @($verdict.Reasons) + $platformCrashMessage
        }

        Write-DeviceTestGateFile -Verdict $verdict -Directory (Join-Path $Directory "attempt-$number")
        if ($null -ne $BeforeRetry) {
            & $BeforeRetry $platformCrash
        }

        $number++
    }

    $stopwatch.Stop()
    if ($notes.Count -gt 0) {
        $verdict.Reasons = @($verdict.Reasons) + @($notes | Where-Object { @($verdict.Reasons) -notcontains $_ })
    }

    $verdict.Retried = $retried
    $verdict.Attempts = $attempts
    $verdict.Project = $Project.Name
    $verdict.Duration = $stopwatch.Elapsed
    return $verdict
}

function Write-DeviceTestGateFile {
    param($Verdict, [string]$Directory)

    $Verdict | Select-Object ExitCode, ExitCodeName, RunnerExitCode, Total, Passed, Failed, Skipped, Errors, Attempt, Retried, Attempts, Reasons, Warnings, Device, DeviceOs, ResultFiles |
        ConvertTo-Json -Depth 4 |
        ForEach-Object { [System.IO.File]::WriteAllText((Join-Path $Directory 'gate.json'), $_, (New-Object System.Text.UTF8Encoding($false))) }
}

function Write-DeviceTestVerdict {
    param($Context, $Verdict)

    $line = "$($Verdict.Project): $($Verdict.ExitCode) ($($Verdict.ExitCodeName)) - total $($Verdict.Total), passed $($Verdict.Passed), failed $($Verdict.Failed), skipped $($Verdict.Skipped), errors $($Verdict.Errors)"
    Write-DeviceTestHeader $line
    foreach ($reason in $Verdict.Reasons) {
        Write-Host "  - $reason"
    }

    Write-DeviceTestGateFile -Verdict $Verdict -Directory $Verdict.Directory

    if ($Verdict.ExitCode -ne 0) {
        $message = "$($Verdict.Project) on $($Context.Leg): $($Verdict.ExitCode) ($($Verdict.ExitCodeName)). " + ($Verdict.Reasons -join '; ')
        Write-DeviceTestError -Title "Device tests ($($Context.Leg))" -Message $message
    }

    foreach ($warning in $Verdict.Warnings) {
        Write-DeviceTestWarning -Title "Device tests ($($Context.Leg))" -Message "$($Verdict.Project): $warning"
    }
}

function Format-DeviceTestDuration {
    param([System.TimeSpan]$Duration)

    if ($Duration.TotalMinutes -ge 1) {
        return ('{0}m {1:00}s' -f [int][Math]::Floor($Duration.TotalMinutes), $Duration.Seconds)
    }

    return ('{0}s' -f [int][Math]::Round($Duration.TotalSeconds))
}

function Format-DeviceTestMarkdownText {
    param([string]$Text, [int]$MaxLength = 300)

    if ($null -eq $Text) {
        return ''
    }

    $flat = ($Text -replace '\s+', ' ').Trim()
    if ($flat.Length -gt $MaxLength) {
        $flat = $flat.Substring(0, $MaxLength) + '...'
    }

    return $flat.Replace('|', '\|').Replace('<', '&lt;').Replace('>', '&gt;')
}

function Write-DeviceTestSummary {
    param([Parameter(Mandatory = $true)]$Context)

    $lines = New-Object System.Collections.Generic.List[string]
    $title = "Device tests: $($Context.Leg)"
    if ($Context.DeviceDescription) {
        $title = "$title ($($Context.DeviceDescription))"
    }

    $lines.Add("### $title")
    $lines.Add('')

    if ($Context.Builds.Count -gt 0) {
        $lines.Add('| App | Build | Duration |')
        $lines.Add('|---|---|---:|')
        foreach ($build in $Context.Builds) {
            $result = 'succeeded'
            if ($build.ExitCode -ne 0) {
                $result = "FAILED (exit code $($build.ExitCode))"
            }

            $lines.Add("| $($build.Project) | $result | $(Format-DeviceTestDuration $build.Duration) |")
        }

        $lines.Add('')
    }

    if ($Context.Verdicts.Count -gt 0) {
        $lines.Add("TestFilter: ``$($Context.TestFilter)``, capabilities: $(Format-DeviceTestCapabilities -Context $Context), configuration: $($Context.Configuration)")
        $lines.Add('')
        $lines.Add('| App | Result | Total | Passed | Failed | Skipped | Errors | Exit code | Duration |')
        $lines.Add('|---|---|---:|---:|---:|---:|---:|---|---:|')
        foreach ($verdict in $Context.Verdicts) {
            $result = 'FAIL'
            if ($verdict.ExitCode -eq 0) {
                $result = 'PASS'
            }

            if ($verdict.Retried.Count -gt 0) {
                $result = "$result (RETRIED after $($verdict.Retried -join ', '))"
            }

            $lines.Add("| $($verdict.Project) | $result | $($verdict.Total) | $($verdict.Passed) | $($verdict.Failed) | $($verdict.Skipped) | $($verdict.Errors) | $($verdict.ExitCode) ($($verdict.ExitCodeName)) | $(Format-DeviceTestDuration $verdict.Duration) |")
        }
    }

    foreach ($verdict in $Context.Verdicts) {
        if ($verdict.ExitCode -eq 0 -and $verdict.Warnings.Count -eq 0 -and @($verdict.Reasons).Count -eq 0) {
            continue
        }

        $lines.Add('')
        $lines.Add("#### $($verdict.Project)")
        foreach ($reason in $verdict.Reasons) {
            $lines.Add("- $(Format-DeviceTestMarkdownText $reason 1000)")
        }

        foreach ($warning in $verdict.Warnings) {
            $lines.Add("- Warning: $(Format-DeviceTestMarkdownText $warning 500)")
        }

        if ($null -ne $verdict.PSObject.Properties['Evidence'] -and $verdict.Evidence) {
            $lines.Add("- Device evidence: $(Format-DeviceTestMarkdownText $verdict.Evidence 500) (collect.log lists what was saved)")
        }

        if ($verdict.Device) {
            $lines.Add("- Device: $(Format-DeviceTestMarkdownText "$($verdict.Device) $($verdict.DeviceOs)" 200)")
        }

        $failures = @($verdict.Failures)
        if ($failures.Count -gt 0) {
            $lines.Add('')
            $lines.Add("Failed tests ($($failures.Count)):")
            $lines.Add('')
            $shown = 0
            foreach ($failure in $failures) {
                if ($shown -ge 50) {
                    $lines.Add("- ... and $($failures.Count - 50) more")
                    break
                }

                $lines.Add("- **$(Format-DeviceTestMarkdownText $failure.Name 200)**: $(Format-DeviceTestMarkdownText $failure.Message 300)")
                $shown++
            }
        }
    }

    $lines.Add('')
    $text = ($lines -join "`n") + "`n"
    $encoding = New-Object System.Text.UTF8Encoding($false)
    $fileName = 'summary.md'
    if ($Context.Verdicts.Count -eq 0) {
        $fileName = 'build-summary.md'
    }

    [System.IO.File]::WriteAllText((Join-Path $Context.Results $fileName), $text, $encoding)
    if ($env:GITHUB_STEP_SUMMARY) {
        [System.IO.File]::AppendAllText($env:GITHUB_STEP_SUMMARY, $text, $encoding)
    }

    Write-Host ''
    Write-Host $text
}

function Complete-DeviceTestRun {
    param([Parameter(Mandatory = $true)]$Context)

    if ($Context.Builds.Count -gt 0 -or $Context.Verdicts.Count -gt 0) {
        Write-DeviceTestSummary -Context $Context
    }

    foreach ($verdict in $Context.Verdicts) {
        if ($verdict.ExitCode -ne 0) {
            return $verdict.ExitCode
        }
    }

    if ($Context.BuildFailures.Count -gt 0) {
        return 71
    }

    return 0
}

function Get-DeviceTestErrorExitCode {
    param($ErrorRecord)

    $exception = $ErrorRecord
    if ($ErrorRecord -is [System.Management.Automation.ErrorRecord]) {
        $exception = $ErrorRecord.Exception
    }

    if ($exception -is [System.ArgumentException]) {
        return 3
    }

    return 71
}

function Write-DeviceTestException {
    param($ErrorRecord)

    $exitCode = Get-DeviceTestErrorExitCode $ErrorRecord
    $message = $ErrorRecord.Exception.Message
    if ($exitCode -ne 3) {
        $message = "$message`n$($ErrorRecord.ScriptStackTrace)"
    }

    Write-DeviceTestError -Message $message
    return $exitCode
}

function Get-DeviceTestHostArchitecture {
    if ($script:DeviceTestsOnWindows) {
        # RuntimeInformation is not loaded in Windows PowerShell 5.1.
        $architecture = $env:PROCESSOR_ARCHITEW6432
        if (-not $architecture) {
            $architecture = $env:PROCESSOR_ARCHITECTURE
        }
    }
    else {
        $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    }

    if ($architecture -eq 'Arm64' -or $architecture -eq 'ARM64') {
        return 'arm64'
    }

    return 'x64'
}

function Invoke-DeviceTestToolRestore {
    # Runs from the repo root, where the tool manifest is and whose NuGet.config maps xharness to the dotnet-eng feed.
    Push-Location $script:DeviceTestsRepoRoot
    try {
        $exitCode = Invoke-DeviceTestNative -FilePath 'dotnet' -ArgumentList @('tool', 'restore')
    }
    finally {
        Pop-Location
    }

    if ($exitCode -ne 0) {
        throw "dotnet tool restore failed with exit code $exitCode."
    }
}

function Invoke-XHarness {
    # Runs from the repo root because that is where the tool manifest is.
    param(
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [Parameter(Mandatory = $true)][string]$OutputDirectory
    )

    $env:XHARNESS_DISABLE_COLORED_OUTPUT = 'true'
    $env:XHARNESS_LOG_WITH_TIMESTAMPS = 'true'

    Push-Location $script:DeviceTestsRepoRoot
    try {
        return Invoke-DeviceTestNative -FilePath 'dotnet' -ArgumentList (@('xharness') + $ArgumentList) -LogPath (Join-Path $OutputDirectory 'xharness-console.log')
    }
    finally {
        Pop-Location
    }
}

function Get-DeviceTestOutputDirectory {
    # An evaluated OutputPath/PublishDir can be relative to the project and can have Windows separators.
    param([Parameter(Mandatory = $true)]$Project, [string]$Path)

    $normalized = $Path.Replace('\', [System.IO.Path]::DirectorySeparatorChar).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    if ([System.IO.Path]::IsPathRooted($normalized)) {
        return [System.IO.Path]::GetFullPath($normalized)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $Project.Directory $normalized))
}

function Get-DeviceTestXcodeRoot {
    param([string]$Xcode)

    if ($Xcode) {
        return (Resolve-DeviceTestPath $Xcode)
    }

    $selected = Invoke-DeviceTestProcess -FilePath 'xcode-select' -ArgumentList @('-p') -TimeoutSeconds 30
    $developerDirectory = $selected.Output.Trim()
    if ($selected.ExitCode -ne 0 -or -not $developerDirectory) {
        return ''
    }

    if ($developerDirectory.EndsWith('/Contents/Developer')) {
        return $developerDirectory.Substring(0, $developerDirectory.Length - '/Contents/Developer'.Length)
    }

    return $developerDirectory
}

function Find-DeviceTestAppBundle {
    # Mac Catalyst names the bundle after ApplicationTitle and iOS after AssemblyName.
    param([Parameter(Mandatory = $true)][string]$OutputDirectory, [string[]]$Names = @())

    if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
        return $null
    }

    foreach ($name in $Names) {
        if ($name) {
            $candidate = Join-Path $OutputDirectory "$name.app"
            if (Test-Path -LiteralPath $candidate -PathType Container) {
                return $candidate
            }
        }
    }

    $bundle = Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.app' -Directory |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -eq $bundle) {
        return $null
    }

    return $bundle.FullName
}

function Stop-MacCatalystAppInstances {
    param(
        [Parameter(Mandatory = $true)][string]$AppBundle,
        [int]$WaitSeconds = 10
    )

    # XHarness kills only its `open` launcher when the launch times out; the app is a launchd child and keeps running.
    # Matching the bundle's executable directory keeps other apps (and other sessions' apps) untouched.
    $executableDirectory = (Join-Path ([System.IO.Path]::GetFullPath($AppBundle)) 'Contents/MacOS') + '/'
    $findInstances = {
        @(& ps -axo 'pid=,command=' | ForEach-Object {
                if ($_.TrimStart() -match '^(\d+)\s+(.*)$' -and $Matches[2].StartsWith($executableDirectory, [System.StringComparison]::Ordinal) -and [int]$Matches[1] -ne $PID) {
                    [int]$Matches[1]
                }
            })
    }

    $processIds = & $findInstances
    if ($processIds.Count -eq 0) {
        return
    }

    Write-DeviceTestWarning "Stopping the test app that the previous attempt left running (pid $($processIds -join ', ')): $AppBundle"
    & kill -KILL @($processIds) 2>$null
    $deadline = [DateTime]::UtcNow.AddSeconds($WaitSeconds)
    while ((& $findInstances).Count -gt 0 -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 250
    }

    $survivors = & $findInstances
    if ($survivors.Count -gt 0) {
        Write-DeviceTestWarning "The test app is still running after $WaitSeconds s (pid $($survivors -join ', ')); the retry can hang the same way."
    }
}

function Invoke-AppleDeviceTests {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)]$Project,
        [Parameter(Mandatory = $true)][string]$Platform,
        [Parameter(Mandatory = $true)][string]$RuntimeIdentifier,
        [Parameter(Mandatory = $true)][string]$XHarnessTarget,
        [string]$Device,
        [string]$XcodeRoot,
        [string[]]$ExtraArguments = @(),
        [scriptblock]$BeforeRetry,
        [scriptblock]$OnFailure
    )

    $appleOutput = Join-Path $Context.Results $Project.Name
    Initialize-DeviceTestResultsDirectory -Path $appleOutput

    $framework = Get-DeviceTestTargetFramework -Project $Project -Platform $Platform
    $properties = Get-DeviceTestMSBuildProperties -ProjectPath $Project.Path -Name @('ApplicationId', 'AssemblyName', 'ApplicationTitle', 'OutputPath') -Property @("TargetFramework=$framework", "Configuration=$($Context.Configuration)", "RuntimeIdentifier=$RuntimeIdentifier")
    $bundleNames = @($properties['AssemblyName'], $properties['ApplicationTitle'])
    if ($Platform -eq 'maccatalyst') {
        $bundleNames = @($properties['ApplicationTitle'], $properties['AssemblyName'])
    }

    $appBundle = Find-DeviceTestAppBundle -OutputDirectory (Get-DeviceTestOutputDirectory -Project $Project -Path $properties['OutputPath']) -Names $bundleNames
    if (-not $appBundle) {
        $verdict = New-DeviceTestVerdict -ExitCode 4 -Directory $appleOutput -Reason "No .app bundle for $framework ($RuntimeIdentifier, $($Context.Configuration)) under $($properties['OutputPath']). Build it first with -Target buildOnly or buildAndTest."
        $verdict.Project = $Project.Name
        return $verdict
    }

    Write-DeviceTestHeader "Test $($Project.Name) ($($properties['ApplicationId'])) on $XHarnessTarget"

    $appleArguments = @('apple', 'test', "--app=$appBundle", "--target=$XHarnessTarget")
    if ($Device) {
        $appleArguments += "--device=$Device"
    }

    $appleArguments += @("--output-directory=$appleOutput", "--timeout=$($Context.Timeout)", "--launch-timeout=$($Context.LaunchTimeout)")
    if ($XcodeRoot) {
        $appleArguments += "--xcode=$XcodeRoot"
    }

    # dotnet/maui passes TestFilter to Apple apps as an environment variable (eng/devices/ios.cake, catalyst.cake); the capability
    # declarations go the same way.
    $appleArguments += @('--verbosity=Debug', "--set-env=TestFilter=$($Context.TestFilter)")
    $appleArguments += @(Get-DeviceTestCapabilityDeclarations -Context $Context | ForEach-Object { "--set-env=$_" })
    $appleArguments += $ExtraArguments

    $callerBeforeRetry = $BeforeRetry
    $appleBeforeRetry = $BeforeRetry
    if ($Platform -eq 'maccatalyst') {
        $appleBeforeRetry = {
            Stop-MacCatalystAppInstances -AppBundle $appBundle
            if ($null -ne $callerBeforeRetry) {
                & $callerBeforeRetry
            }
        }.GetNewClosure()
    }

    $verdict = Invoke-DeviceTestWithRetries -Context $Context -Project $Project -Directory $appleOutput -BeforeRetry $appleBeforeRetry -Attempt {
        param($attemptNumber)
        $appleExitCode = Invoke-XHarness -ArgumentList $appleArguments -OutputDirectory $appleOutput
        Get-DeviceTestVerdict -Platform $Platform -Directory $appleOutput -RunnerExitCode $appleExitCode -Attempt $attemptNumber
    }

    if ($verdict.ExitCode -ne 0 -and $null -ne $OnFailure) {
        try {
            & $OnFailure $appleOutput $appBundle
        }
        catch {
            Write-DeviceTestWarning "Collecting failure diagnostics failed: $($_.Exception.Message)"
        }
    }

    return $verdict
}

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

# XHarness uninstalls the app as soon as its own timeout ends, so it waits this much longer than -Timeout and the evidence watch saves
# the stacks of an app that is still running at -Timeout first.
$script:AndroidEvidenceGraceSeconds = 180
$script:AndroidEvidenceBudgetSeconds = 150

function Add-AndroidEvidenceLogLine {
    param([string]$Directory, [string]$Message)

    $stamp = [datetime]::UtcNow.ToString('HH:mm:ss.fff', [System.Globalization.CultureInfo]::InvariantCulture)
    [System.IO.File]::AppendAllText((Join-Path $Directory 'collect.log'), "[$stamp] $Message`n", (New-Object System.Text.UTF8Encoding($false)))
}

function Save-AndroidEvidenceOutput {
    param([string]$Adb, [string]$Serial, [string]$Directory, [string]$FileName, [string[]]$Command, [int]$TimeoutSeconds = 30)

    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    $started = [datetime]::UtcNow
    $result = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList (@('-s', $Serial, 'shell') + $Command) -TimeoutSeconds $TimeoutSeconds
    $text = [string]$result.Output
    if ($result.Error) {
        $text = $text + "`n--- stderr ---`n" + $result.Error
    }

    [System.IO.File]::WriteAllText((Join-Path $Directory $FileName), $text, (New-Object System.Text.UTF8Encoding($false)))
    $seconds = [int][Math]::Round(([datetime]::UtcNow - $started).TotalSeconds)
    if ($result.TimedOut) {
        Add-AndroidEvidenceLogLine -Directory $Directory -Message ([string]::Format($invariant, '{0}: timed out after {1} s', $FileName, $TimeoutSeconds))
    }
    else {
        Add-AndroidEvidenceLogLine -Directory $Directory -Message ([string]::Format($invariant, '{0}: exit {1}, {2} chars, {3} s', $FileName, $result.ExitCode, $text.Length, $seconds))
    }

    return $result
}

function Get-AndroidEvidenceProcesses {
    # toybox 'ps -A' (API 26+) and toolbox 'ps' (API 25) both print the user first, the pid second and the name last.
    param([string]$PsOutput)

    $processes = @()
    $lines = @(([string]$PsOutput) -split "`r?`n" | Select-Object -Skip 1)
    foreach ($line in $lines) {
        $tokens = @($line.Trim() -split '\s+')
        $processId = 0
        if ($tokens.Count -ge 3 -and [int]::TryParse($tokens[1], [System.Globalization.NumberStyles]::None, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$processId)) {
            $processes += New-Object PSObject -Property @{ Id = $processId; Name = $tokens[$tokens.Count - 1] }
        }
    }

    return , $processes
}

function Get-AndroidEvidenceListingEntries {
    # Keyed by size and name: toybox pads the other columns to the widest owner, so whole lines change when a new file appears.
    param([string]$Listing)

    $entries = @()
    foreach ($line in @(([string]$Listing) -split "`r?`n")) {
        $tokens = @($line.Trim() -split '\s+')
        if ($tokens.Count -ge 6 -and -not $line.TrimStart().StartsWith('total', [System.StringComparison]::Ordinal)) {
            $entries += New-Object PSObject -Property @{ Key = "$($tokens[$tokens.Count - 4]) $($tokens[$tokens.Count - 1])"; Name = $tokens[$tokens.Count - 1] }
        }
    }

    return $entries
}

function Save-AndroidNativeStacks {
    param([string]$Adb, [string]$Serial, [string]$Directory, [string]$Name, [int]$ProcessId)

    $shortName = @($Name -split '/')[-1]
    $safeName = [regex]::Replace($shortName, '[^A-Za-z0-9._@-]', '_', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
    $fileName = "stacks-$safeName-$ProcessId.txt"
    $stacks = Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName $fileName -Command @('debuggerd', '-b', "$ProcessId") -TimeoutSeconds 30
    if (($stacks.Output + $stacks.Error).IndexOf('not found', [System.StringComparison]::Ordinal) -ge 0) {
        [void](Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName $fileName -Command @('debuggerd64', '-b', "$ProcessId") -TimeoutSeconds 30)
    }
}

function Save-AndroidDeviceEvidence {
    # Best effort: whatever the device still gives is saved, and nothing here fails the job.
    param([string]$Adb, [string]$Serial, [string]$PackageName, [string]$Directory, [string]$Phase)

    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    $started = [datetime]::UtcNow
    $skipped = New-Object System.Collections.Generic.List[string]
    $hasBudget = {
        param([string]$Step)
        if (([datetime]::UtcNow - $started).TotalSeconds -ge $script:AndroidEvidenceBudgetSeconds) {
            $skipped.Add($Step)
            return $false
        }
        return $true
    }

    try {
        New-Item -ItemType Directory -Force -Path $Directory | Out-Null
        Add-AndroidEvidenceLogLine -Directory $Directory -Message "phase: $Phase; device $Serial; package $PackageName; started $($started.ToString('o', $invariant))"

        $userId = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'id', '-u') -TimeoutSeconds 10
        if ($userId.ExitCode -ne 0) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message "device not reachable: $($userId.Error)"
            return $Directory
        }

        if ($userId.Output.Trim() -cne '0') {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message "adbd is not root: /data/anr, /data/tombstones, the binder logs and other processes' stacks cannot be read"
        }

        $processes = @()
        if (& $hasBudget 'process lists') {
            $processList = Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName 'processes.txt' -Command @('ps', '-A')
            if ($processList.ExitCode -ne 0 -or $processList.Output.IndexOf('bad pid', [System.StringComparison]::Ordinal) -ge 0 -or @($processList.Output -split "`r?`n").Count -lt 5) {
                $processList = Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName 'processes.txt' -Command @('ps')
            }

            $threadList = Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName 'threads.txt' -Command @('ps', '-A', '-T')
            if ($threadList.ExitCode -ne 0 -or $threadList.Output.IndexOf('bad pid', [System.StringComparison]::Ordinal) -ge 0 -or @($threadList.Output -split "`r?`n").Count -lt 5) {
                [void](Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName 'threads.txt' -Command @('ps', '-t'))
            }

            $processes = Get-AndroidEvidenceProcesses -PsOutput $processList.Output
        }

        $appProcesses = @($processes | Where-Object { $_.Name -ceq $PackageName })
        $cameraServers = @($processes | Where-Object { $_.Name -ceq 'cameraserver' -or $_.Name.EndsWith('/cameraserver', [System.StringComparison]::Ordinal) })
        $providers = @($processes | Where-Object { $_.Name.IndexOf('camera.provider', [System.StringComparison]::Ordinal) -ge 0 })
        if ($appProcesses.Count -eq 0) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message 'the app is not running'
        }
        else {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message "app pid: $(($appProcesses | ForEach-Object { $_.Id }) -join ', ')"
        }

        if ($cameraServers.Count -eq 0) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message 'no cameraserver process'
        }

        if ($providers.Count -eq 0) {
            Add-AndroidEvidenceLogLine -Directory $Directory -Message 'no camera provider process'
        }

        if (& $hasBudget 'binder logs') {
            $binderBase = '/sys/kernel/debug/binder'
            $binderFs = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'ls', '/dev/binderfs/binder_logs/state') -TimeoutSeconds 10
            if ($binderFs.ExitCode -eq 0) {
                $binderBase = '/dev/binderfs/binder_logs'
            }

            foreach ($name in @('state', 'transactions', 'failed_transaction_log', 'transaction_log')) {
                [void](Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName "binder-$name.txt" -Command @('cat', "$binderBase/$name"))
            }
        }

        # The app's native stacks come first: on API 25 the kill -3 below makes ART abort the app.
        foreach ($app in $appProcesses) {
            if (-not (& $hasBudget "native stacks of $PackageName")) { break }
            Save-AndroidNativeStacks -Adb $Adb -Serial $Serial -Directory $Directory -Name $PackageName -ProcessId $app.Id
        }

        if ($appProcesses.Count -gt 0 -and (& $hasBudget 'app Java stacks')) {
            $appId = $appProcesses[0].Id
            $anrBefore = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'ls', '-l', '/data/anr') -TimeoutSeconds 10
            $knownEntries = @(Get-AndroidEvidenceListingEntries -Listing $anrBefore.Output | ForEach-Object { $_.Key })
            $quit = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'kill', '-3', "$appId") -TimeoutSeconds 10
            Add-AndroidEvidenceLogLine -Directory $Directory -Message "kill -3 ${appId}: exit $($quit.ExitCode)"
            $dumpName = ''
            for ($second = 0; $second -lt 15 -and -not $dumpName; $second++) {
                Start-Sleep -Seconds 1
                # Checked before the scan so a dump written just before the app died is still found.
                $appWasDead = (Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'kill', '-0', "$appId") -TimeoutSeconds 10).ExitCode -ne 0
                $anrNow = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'ls', '-l', '/data/anr') -TimeoutSeconds 10
                # Every pass greps each new file again: a dump may still be growing when it is first listed.
                foreach ($entry in @(Get-AndroidEvidenceListingEntries -Listing $anrNow.Output)) {
                    if ($knownEntries -ccontains $entry.Key) { continue }
                    $match = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'grep', '-c', '-e', "'----- pid $appId '", "/data/anr/$($entry.Name)") -TimeoutSeconds 10
                    if ($match.ExitCode -eq 0) {
                        $dumpName = $entry.Name
                        break
                    }
                }

                if (-not $dumpName -and $appWasDead) {
                    break
                }
            }

            $alive = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'shell', 'kill', '-0', "$appId") -TimeoutSeconds 10
            if ($alive.ExitCode -ne 0) {
                Add-AndroidEvidenceLogLine -Directory $Directory -Message 'kill -3 ended the app (see tombstones/)'
            }

            if ($dumpName) {
                Add-AndroidEvidenceLogLine -Directory $Directory -Message "app Java stacks: /data/anr/$dumpName"
            }
            else {
                Add-AndroidEvidenceLogLine -Directory $Directory -Message "no dump for pid $appId found in /data/anr after kill -3"
            }
        }

        $otherTargets = @($cameraServers) + @($providers)
        foreach ($target in $otherTargets) {
            if (-not (& $hasBudget "native stacks of $($target.Name)")) { break }
            Save-AndroidNativeStacks -Adb $Adb -Serial $Serial -Directory $Directory -Name $target.Name -ProcessId $target.Id
        }

        # A wedged cameraserver must not eat the whole budget, hence the shorter limits.
        if (& $hasBudget 'dumpsys media.camera') {
            [void](Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName 'dumpsys-media.camera.txt' -Command @('dumpsys', '-t', '20', 'media.camera') -TimeoutSeconds 40)
        }

        if (& $hasBudget 'dmesg') {
            [void](Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName 'dmesg.txt' -Command @('dmesg'))
        }

        if (& $hasBudget 'data listing and pulls') {
            [void](Save-AndroidEvidenceOutput -Adb $Adb -Serial $Serial -Directory $Directory -FileName 'data-listing.txt' -Command @('ls', '-la', '/data/anr', '/data/tombstones'))
            foreach ($remote in @('/data/anr', '/data/tombstones')) {
                $pull = Invoke-DeviceTestProcess -FilePath $Adb -ArgumentList @('-s', $Serial, 'pull', $remote, $Directory) -TimeoutSeconds 60
                $lastLine = [string](@(($pull.Output + "`n" + $pull.Error) -split "`r?`n" | Where-Object { $_.Trim() }) | Select-Object -Last 1)
                Add-AndroidEvidenceLogLine -Directory $Directory -Message "pull $($remote): exit $($pull.ExitCode); $lastLine"
            }
        }
    }
    catch {
        Write-DeviceTestWarning "Collecting device evidence failed: $($_.Exception.Message)"
        try { Add-AndroidEvidenceLogLine -Directory $Directory -Message "collection stopped: $($_.Exception.Message)" } catch { Write-Verbose "No collect.log: $($_.Exception.Message)" }
        return $Directory
    }

    if ($skipped.Count -gt 0) {
        Add-AndroidEvidenceLogLine -Directory $Directory -Message "budget of $($script:AndroidEvidenceBudgetSeconds) s used up; skipped: $($skipped -join ', ')"
    }

    $elapsed = [int][Math]::Round(([datetime]::UtcNow - $started).TotalSeconds)
    Add-AndroidEvidenceLogLine -Directory $Directory -Message ([string]::Format($invariant, 'done in {0} s', $elapsed))
    Write-Host "Saved device evidence ($Phase) to $Directory"
    return $Directory
}

# Known Android platform crashes that a failed leg may retry once (see Invoke-DeviceTestWithRetries).
# Strings are matched ordinally: Turkish casing would corrupt these ASCII keywords.
$script:AndroidCrashDeadlineChecks = 5000
$script:AndroidCrashDebugWindow = 150
$script:AndroidRunnerStartMarker = '[Runner executing:'
$script:AndroidFailedTestPattern = ' DOTNET\s*: \t\[FAIL\] (.*)$'
$script:AndroidFatalSignalPattern = ' F libc\s*: Fatal signal 11 \(SIGSEGV\), code (-?\d+)(?: \([A-Z_]+\))?, fault addr 0x([0-9a-f]+) in tid (\d+) \(([^)]*)\), pid \d+ \(([^)]*)\)'

# Android 11 lacks the null check of AOSP 162f643 (https://android.googlesource.com/platform/frameworks/native/+/162f643), so Media3's
# Surface.setFrameRate crashes surfaceflinger. Signal code 1 and both backtrace frames are the exact CI tombstone; looser matching could retry an unrelated surfaceflinger crash.
$script:AndroidSurfaceFlingerSignalCode = 1
$script:AndroidSurfaceFlingerRequiresFrames = $true
$script:AndroidSurfaceFlingerConsoleMarker = 'INSTRUMENTATION_ABORTED: System has crashed.'
$script:AndroidSurfaceFlingerFrame = 'libsurfaceflinger.so (android::Layer::setFrameRate('
$script:AndroidSurfaceFlingerTaskFrame = 'SurfaceFlinger12setFrameRate'

# Android 8 updates a SurfaceView's position on a hwui worker, which dereferences a null pointer right after the surface is destroyed.
$script:AndroidRenderWorkerConsoleMarker = 'shortMsg=Process crashed.'
$script:AndroidRenderWorkerThreadPattern = '^(hwuiTask\d+|RenderThread)$'
$script:AndroidSurfaceTeardownMarker = 'SurfaceViewImpl: Surface destroyed.'
$script:AndroidSurfaceTeardownLayerMarker = 'Failed to find layer (SurfaceView - '

# CameraAndroidTestBase writes these when the camera stack of the emulator is stuck (a CameraDevice.close() that never returns, or CameraX
# that lost its cameras); every later camera test then fails at once with the same reason.
$script:AndroidCameraWedgeOrigin = 'The camera stack of this process is wedged; later camera tests fail at once. '
$script:AndroidCameraWedgeFailFast = 'An earlier camera test left the camera stack of this process wedged, so this camera test can not run. '
$script:AndroidCameraWedgeDetectors = @('is still inside CameraDeviceImpl.close', 'CameraX lists no camera in two samples')

function Read-AndroidLogcatMarkers {
    # One streaming pass; the logcat of a long run has about 250,000 lines.
    param([string]$Path, [string]$PackageName, [datetime]$DeadlineUtc)

    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    $ordinal = [System.StringComparison]::Ordinal
    $markers = New-Object PSObject -Property @{
        Path            = $Path
        LineCount       = 0
        RunnerStartLine = 0
        FailedTests     = New-Object System.Collections.Generic.List[string]
        Signals         = New-Object System.Collections.Generic.List[object]
        DebugHeaders    = New-Object System.Collections.Generic.List[object]
        FrameLines      = New-Object System.Collections.Generic.List[int]
        TaskFrameLines  = New-Object System.Collections.Generic.List[int]
        TeardownLines   = New-Object System.Collections.Generic.List[int]
        WedgedLine      = 0
        WatchdogLine    = 0
    }

    if ([datetime]::UtcNow -ge $DeadlineUtc) {
        throw 'the time budget for reading the logcat ran out before it started'
    }

    $layerMarker = $script:AndroidSurfaceTeardownLayerMarker + $PackageName + '/'
    $number = 0
    foreach ($line in [System.IO.File]::ReadLines($Path)) {
        $number++
        if (($number % $script:AndroidCrashDeadlineChecks) -eq 0 -and [datetime]::UtcNow -ge $DeadlineUtc) {
            throw "the time budget for reading the logcat ran out at line $number"
        }

        if ($line.IndexOf(' DOTNET', $ordinal) -ge 0) {
            if ($markers.RunnerStartLine -eq 0 -and $line.IndexOf($script:AndroidRunnerStartMarker, $ordinal) -ge 0) {
                $markers.RunnerStartLine = $number
            }
            elseif ($line.IndexOf("`t[FAIL] ", $ordinal) -ge 0) {
                $failed = [regex]::Match($line, $script:AndroidFailedTestPattern, [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
                if ($failed.Success) {
                    $markers.FailedTests.Add($failed.Groups[1].Value.Trim())
                }
            }
        }
        elseif ($line.IndexOf('Fatal signal 11', $ordinal) -ge 0) {
            $signal = [regex]::Match($line, $script:AndroidFatalSignalPattern, [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
            $address = [uint64]0
            if ($signal.Success -and [uint64]::TryParse($signal.Groups[2].Value, [System.Globalization.NumberStyles]::AllowHexSpecifier, $invariant, [ref]$address)) {
                $markers.Signals.Add((New-Object PSObject -Property @{
                            Line    = $number
                            Code    = [int]$signal.Groups[1].Value
                            Address = $address
                            Tid     = $signal.Groups[3].Value
                            Thread  = $signal.Groups[4].Value
                            Process = $signal.Groups[5].Value
                        }))
            }
        }
        elseif ($line.IndexOf(' DEBUG', $ordinal) -ge 0) {
            if ($line.IndexOf('>>> ', $ordinal) -ge 0) {
                $markers.DebugHeaders.Add((New-Object PSObject -Property @{ Line = $number; Text = $line }))
            }
            elseif ($line.IndexOf(' #00 ', $ordinal) -ge 0 -and $line.IndexOf($script:AndroidSurfaceFlingerFrame, $ordinal) -ge 0) {
                $markers.FrameLines.Add($number)
            }
            elseif ($line.IndexOf($script:AndroidSurfaceFlingerTaskFrame, $ordinal) -ge 0) {
                $markers.TaskFrameLines.Add($number)
            }
        }
        elseif ($line.IndexOf('SurfaceView', $ordinal) -ge 0) {
            if ($line.IndexOf($script:AndroidSurfaceTeardownMarker, $ordinal) -ge 0 -or $line.IndexOf($layerMarker, $ordinal) -ge 0) {
                $markers.TeardownLines.Add($number)
            }
        }
        elseif ($line.IndexOf('CameraWedge', $ordinal) -ge 0) {
            if ($markers.WedgedLine -eq 0 -and $line.IndexOf('WEDGED: ', $ordinal) -ge 0) {
                $markers.WedgedLine = $number
            }
        }
        elseif ($line.IndexOf('CameraServiceWatchdog triggering abort', $ordinal) -ge 0) {
            if ($markers.WatchdogLine -eq 0) {
                $markers.WatchdogLine = $number
            }
        }
    }

    $markers.LineCount = $number
    return $markers
}

function Get-AndroidCrashBlocker {
    param($Markers, $Verdict)

    if ($Markers.RunnerStartLine -eq 0) {
        return 'the logcat has no runner start line, so it may not cover the whole run'
    }

    if ($Markers.FailedTests.Count -gt 0) {
        $names = @($Markers.FailedTests | Select-Object -First 5) -join ', '
        return "$($Markers.FailedTests.Count) test(s) failed before the crash and are not explained by it (first: $names)"
    }

    if (($Verdict.Failed + $Verdict.Errors) -gt 0) {
        return "the results file reports $($Verdict.Failed + $Verdict.Errors) failed test(s)"
    }

    return ''
}

function Test-AndroidTextInFile {
    param([string]$Path, [string]$Text)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    return [System.IO.File]::ReadAllText($Path).IndexOf($Text, [System.StringComparison]::Ordinal) -ge 0
}

function Test-AndroidSurfaceFlingerSetFrameRateCrash {
    param($Markers)

    $ordinal = [System.StringComparison]::Ordinal
    foreach ($signal in $Markers.Signals) {
        if ($signal.Thread -cne 'surfaceflinger' -or $signal.Process -cne 'surfaceflinger' -or $signal.Address -ge 0x10000) {
            continue
        }

        if ($script:AndroidSurfaceFlingerSignalCode -ge 0 -and $signal.Code -ne $script:AndroidSurfaceFlingerSignalCode) {
            continue
        }

        $last = $signal.Line + $script:AndroidCrashDebugWindow
        $isSurfaceFlinger = @($Markers.DebugHeaders | Where-Object { $_.Line -gt $signal.Line -and $_.Line -le $last -and $_.Text.IndexOf('>>> /system/bin/surfaceflinger <<<', $ordinal) -ge 0 }).Count -gt 0
        if (-not $isSurfaceFlinger) {
            continue
        }

        if ($script:AndroidSurfaceFlingerRequiresFrames) {
            $hasFrame = @($Markers.FrameLines | Where-Object { $_ -gt $signal.Line -and $_ -le $last }).Count -gt 0
            $hasTask = @($Markers.TaskFrameLines | Where-Object { $_ -gt $signal.Line -and $_ -le $last }).Count -gt 0
            if (-not ($hasFrame -and $hasTask)) {
                continue
            }
        }

        return $signal
    }

    return $null
}

function Test-AndroidSurfaceViewRenderWorkerCrash {
    param($Markers, [string]$PackageName)

    $ordinal = [System.StringComparison]::Ordinal
    foreach ($signal in $Markers.Signals) {
        if ($signal.Address -ge 0x1000 -or -not [regex]::IsMatch($signal.Thread, $script:AndroidRenderWorkerThreadPattern, [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)) {
            continue
        }

        $last = $signal.Line + $script:AndroidCrashDebugWindow
        $threadText = "tid: $($signal.Tid), name: $($signal.Thread) "
        $appText = ">>> $PackageName <<<"
        $isApp = @($Markers.DebugHeaders | Where-Object { $_.Line -gt $signal.Line -and $_.Line -le $last -and $_.Text.IndexOf($threadText, $ordinal) -ge 0 -and $_.Text.IndexOf($appText, $ordinal) -ge 0 }).Count -gt 0
        if (-not $isApp) {
            continue
        }

        $first = $signal.Line - 300
        $last = $signal.Line + 20
        $afterTeardown = @($Markers.TeardownLines | Where-Object { $_ -ge $first -and $_ -le $last }).Count -gt 0
        if ($afterTeardown) {
            return $signal
        }
    }

    return $null
}

function Test-AndroidCameraWedge {
    param($Verdict)

    $ordinal = [System.StringComparison]::Ordinal
    $failures = @($Verdict.Failures)
    $origins = @($failures | Where-Object { $_.Message.IndexOf($script:AndroidCameraWedgeOrigin, $ordinal) -ge 0 })
    $failFast = @($failures | Where-Object { $_.Message.StartsWith($script:AndroidCameraWedgeFailFast, $ordinal) })
    if ($origins.Count -eq 0 -and $failFast.Count -eq 0) {
        return $null
    }

    $blocker = ''
    $origin = $null
    $detector = @()
    $others = @($failures | Where-Object { $origins -notcontains $_ -and $failFast -notcontains $_ })
    if ($Verdict.Errors -gt 0) {
        $blocker = "the results report $($Verdict.Errors) assembly error(s)"
    }
    elseif ($origins.Count -ne 1) {
        $blocker = "$($origins.Count) tests report the wedge (exactly one is expected)"
    }
    else {
        $origin = $origins[0]
        $detector = @($script:AndroidCameraWedgeDetectors | Where-Object { $origin.Message.IndexOf($_, $ordinal) -ge 0 } | Select-Object -First 1)
        $mismatched = @($failFast | Where-Object {
                $reason = $_.Message.Substring($script:AndroidCameraWedgeFailFast.Length).Trim()
                $reason.Length -eq 0 -or $origin.Message.IndexOf($reason, $ordinal) -lt 0
            })
        if ($detector.Count -eq 0) {
            $blocker = 'the wedge message names no known detector'
        }
        elseif ($others.Count -gt 0) {
            $names = @($others | Select-Object -First 5 | ForEach-Object { $_.Name }) -join ', '
            $blocker = "$($others.Count) failed test(s) do not carry the wedge reason (first: $names)"
        }
        elseif ($mismatched.Count -gt 0) {
            $blocker = "$($mismatched.Count) fail-fast test(s) carry another reason than the first wedge"
        }
        elseif (($Verdict.Failed + $Verdict.Errors) -ne $failures.Count) {
            $blocker = "the results report $($Verdict.Failed + $Verdict.Errors) failed test(s), but only $($failures.Count) are listed"
        }
    }

    return New-Object PSObject -Property @{
        Origin        = $origin
        FailFastCount = $failFast.Count
        Detector      = $(if ($detector.Count -gt 0) { [string]$detector[0] } else { '' })
        Blocker       = $blocker
    }
}

function Get-AndroidCameraWedgeDetail {
    param($Wedge)

    if ($null -eq $Wedge.Origin) {
        return ''
    }

    $message = [string]$Wedge.Origin.Message
    $wedgeStart = $message.IndexOf($script:AndroidCameraWedgeOrigin, [System.StringComparison]::Ordinal)
    $before = $message.Substring(0, [Math]::Max($wedgeStart, 0))
    $before = $before.Replace('System.AggregateException : The test left failures behind (cleanup, or exceptions that escaped from async void code).', '')
    $own = ([regex]::Replace($before, '\s+', ' ')).Trim()
    $own = [regex]::Replace([regex]::Replace($own, '^\(+', ''), '(?:\)\s*)?\(+\s*$', '')
    if ($own.Length -gt 300) {
        $own = $own.Substring(0, 300) + '...'
    }

    $detail = 'The origin test'
    if ($own) {
        $detail += " failed with '$own'."
    }
    else {
        $detail += ' failed with the wedge reason only.'
    }

    return "$detail $($Wedge.FailFastCount) more camera test(s) failed fast on the same reason."
}

function Get-AndroidPlatformCrash {
    # Never throws and never changes the verdict: an error here must not turn into a retry or a pass.
    param($Verdict, [string]$Directory, [int]$ApiLevel, [string]$PackageName, [int]$TimeoutSeconds = 60)

    try {
        $noRetry = $null -ne $Verdict.PSObject.Properties['NoRetry'] -and $Verdict.NoRetry
        if ($noRetry -or $Verdict.ExitCode -eq 0) {
            return $null
        }

        $deadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
        $exitCode = [int]$Verdict.ExitCode
        $consolePath = Join-Path $Directory 'xharness-console.log'
        $logcat = @(Get-ChildItem -LiteralPath $Directory -Filter 'adb-logcat-*.log' -File -ErrorAction SilentlyContinue | Select-Object -First 1)

        $isSurfaceFlinger = $ApiLevel -eq 30 -and ($exitCode -eq 80 -or $exitCode -eq 82) -and (Test-AndroidTextInFile -Path $consolePath -Text $script:AndroidSurfaceFlingerConsoleMarker)
        $isRenderWorker = ($ApiLevel -eq 26 -or $ApiLevel -eq 27) -and $exitCode -eq 80 -and (Test-AndroidTextInFile -Path $consolePath -Text $script:AndroidRenderWorkerConsoleMarker)
        if (($isSurfaceFlinger -or $isRenderWorker) -and $logcat.Count -gt 0) {
            $markers = Read-AndroidLogcatMarkers -Path $logcat[0].FullName -PackageName $PackageName -DeadlineUtc $deadline
            if ($isSurfaceFlinger) {
                $signal = Test-AndroidSurfaceFlingerSetFrameRateCrash -Markers $markers
                if ($null -ne $signal) {
                    return New-AndroidPlatformCrash -Id 'surfaceflinger-setframerate-api30' `
                        -Title 'Android 11 SurfaceFlinger crash in Layer::setFrameRate (the API 30 image lacks AOSP 162f643)' `
                        -Blocker (Get-AndroidCrashBlocker -Markers $markers -Verdict $Verdict) -Evidence @("$($logcat[0].Name) line $($signal.Line)")
                }
            }
            else {
                $signal = Test-AndroidSurfaceViewRenderWorkerCrash -Markers $markers -PackageName $PackageName
                if ($null -ne $signal) {
                    return New-AndroidPlatformCrash -Id 'surfaceview-renderworker-api26-27' `
                        -Title "Android 8 SurfaceView render-worker crash (SIGSEGV on $($signal.Thread) right after a SurfaceView teardown)" `
                        -Blocker (Get-AndroidCrashBlocker -Markers $markers -Verdict $Verdict) -Evidence @("$($logcat[0].Name) line $($signal.Line)")
                }
            }

            return $null
        }

        if (($ApiLevel -eq 32 -or $ApiLevel -eq 33) -and $exitCode -eq 1) {
            $wedge = Test-AndroidCameraWedge -Verdict $Verdict
            if ($null -eq $wedge) {
                return $null
            }

            $evidence = @('testResults.xml')
            if ($null -ne $wedge.Origin) {
                $evidence = @("testResults.xml: $($wedge.Origin.Name)")
            }

            # The wedge text comes from the results; the logcat only corroborates it and may have rotated.
            if ($logcat.Count -gt 0) {
                try {
                    $markers = Read-AndroidLogcatMarkers -Path $logcat[0].FullName -PackageName $PackageName -DeadlineUtc $deadline
                    if ($markers.WedgedLine -gt 0) {
                        $evidence += "$($logcat[0].Name) line $($markers.WedgedLine) (CameraWedge: WEDGED)"
                    }
                    if ($markers.WatchdogLine -gt 0) {
                        $evidence += "$($logcat[0].Name) line $($markers.WatchdogLine) (CameraServiceWatchdog triggering abort)"
                    }
                }
                catch {
                    $evidence += "$($logcat[0].Name) (could not be read for corroboration)"
                }
            }

            $cause = 'CameraX lost its cameras'
            if ($wedge.Detector -ceq $script:AndroidCameraWedgeDetectors[0]) {
                $cause = 'stuck CameraDevice.close'
            }

            return New-AndroidPlatformCrash -Id 'camera-wedge-api32-33' -Title "Android 12/13 emulator camera wedge ($cause)" -Blocker $wedge.Blocker -Evidence $evidence -Detail (Get-AndroidCameraWedgeDetail -Wedge $wedge)
        }
    }
    catch {
        return New-Object PSObject -Property @{
            Id        = ''
            Title     = 'the check for known platform crashes'
            Explained = $false
            Blocker   = 'the check failed'
            Evidence  = @()
            Detail    = ''
            Error     = $_.Exception.Message
        }
    }

    return $null
}

function New-AndroidPlatformCrash {
    param([string]$Id, [string]$Title, [string]$Blocker, [string[]]$Evidence, [string]$Detail = '')

    return New-Object PSObject -Property @{
        Id        = $Id
        Title     = $Title
        Explained = [string]::IsNullOrEmpty($Blocker)
        Blocker   = $Blocker
        Evidence  = @($Evidence)
        Detail    = $Detail
        Error     = ''
    }
}
