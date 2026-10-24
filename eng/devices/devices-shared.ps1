# This file must keep working in Windows PowerShell 5.1 because the Windows jobs run windows.ps1 with it: no ternary,
# no null-coalescing, no pipeline chain operators and no -AsHashtable.
#
# The scripts mirror dotnet/maui's eng/devices/*.cake (targets buildOnly/testOnly/buildAndTest, TestFilter syntax,
# xharness arguments) and add one results gate for every platform, because xharness can report SUCCESS for a run
# that produced no results (see eng/devices/README.md).

$script:DeviceTestsRepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot (Join-Path '..' '..')))
$script:DeviceTestsOnWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT

$script:DeviceTestProjects = [ordered]@{
    'CommunityToolkit.Maui.DeviceTests'              = 'Maui'
    'CommunityToolkit.Maui.MediaElement.DeviceTests' = 'MediaElement'
    'CommunityToolkit.Maui.Camera.DeviceTests'       = 'Camera'
}

# The apps never run this category headless; the scripts also add it to every SkipCategories filter so the CI default
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
# real problem. Get-DeviceTestVerdict never returns one of the codes below for results that contain a failed test, and
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
    # Only the form of each name is checked here: the apps check the names against their capabilities (DeviceTestCapabilities
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
    # The format must match what the apps read (DeviceTestCapabilities).
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
        [string[]]$BuildArgs = @(),
        # Names a second build of the app in the same run (its binlog and its row in the summary), such as 'coverage'.
        [string]$Suffix
    )

    $label = $Project.Name
    if ($Suffix) {
        $label = "$label ($Suffix)"
    }

    Write-DeviceTestHeader "Build $label ($TargetFramework, $($Context.Configuration))"

    $arguments = @($Command, $Project.Path, '-f', $TargetFramework, '-c', $Context.Configuration, "-bl:$(Get-DeviceTestBinlogPath -Context $Context -Project $Project -Suffix $Suffix)")
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
                $crashReport = Get-DeviceTestJsonValue $matched 'name'
                $reasons.Add("xharness matched a crash report for the app: $crashReport")
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
        Directory      = $Directory
        Project        = ''
        Duration       = [System.TimeSpan]::Zero
    }
}

function Invoke-DeviceTestWithRetries {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)]$Project,
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][scriptblock]$Attempt,
        [scriptblock]$BeforeRetry
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $retried = @()
    $number = 1
    while ($true) {
        $verdict = & $Attempt $number
        $isInfrastructureFailure = $script:DeviceTestInfraExitCodes -contains $verdict.ExitCode
        $hasFailedTests = ($verdict.Failed + $verdict.Errors) -gt 0
        # An attempt can say that another one cannot help (NoRetry), for example when the device it ran on is gone.
        $noRetry = $null -ne $verdict.PSObject.Properties['NoRetry'] -and $verdict.NoRetry
        if ($verdict.ExitCode -eq 0 -or -not $isInfrastructureFailure -or $hasFailedTests -or $noRetry -or $number -gt $Context.InfraRetries) {
            break
        }

        Write-DeviceTestWarning -Title "Device tests ($($Context.Leg))" -Message "$($Project.Name): attempt $number ended with $($verdict.ExitCode) ($($verdict.ExitCodeName)), an infrastructure failure; retrying."
        $retried += "$($verdict.ExitCode) ($($verdict.ExitCodeName))"
        Move-DeviceTestAttempt -Path $Directory -Attempt $number
        if ($null -ne $BeforeRetry) {
            & $BeforeRetry
        }

        $number++
    }

    $stopwatch.Stop()
    $verdict.Retried = $retried
    $verdict.Project = $Project.Name
    $verdict.Duration = $stopwatch.Elapsed
    return $verdict
}

function Write-DeviceTestVerdict {
    param($Context, $Verdict)

    $line = "$($Verdict.Project): $($Verdict.ExitCode) ($($Verdict.ExitCodeName)) - total $($Verdict.Total), passed $($Verdict.Passed), failed $($Verdict.Failed), skipped $($Verdict.Skipped), errors $($Verdict.Errors)"
    Write-DeviceTestHeader $line
    foreach ($reason in $Verdict.Reasons) {
        Write-Host "  - $reason"
    }

    $Verdict | Select-Object ExitCode, ExitCodeName, RunnerExitCode, Total, Passed, Failed, Skipped, Errors, Attempt, Retried, Reasons, Warnings, Device, DeviceOs, ResultFiles |
        ConvertTo-Json -Depth 4 |
        ForEach-Object { [System.IO.File]::WriteAllText((Join-Path $Verdict.Directory 'gate.json'), $_, (New-Object System.Text.UTF8Encoding($false))) }

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
        if ($verdict.ExitCode -eq 0 -and $verdict.Warnings.Count -eq 0) {
            continue
        }

        $lines.Add('')
        $lines.Add("#### $($verdict.Project)")
        foreach ($reason in $verdict.Reasons) {
            $lines.Add("- $(Format-DeviceTestMarkdownText $reason 500)")
        }

        foreach ($warning in $verdict.Warnings) {
            $lines.Add("- Warning: $(Format-DeviceTestMarkdownText $warning 500)")
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

    $verdict = Invoke-DeviceTestWithRetries -Context $Context -Project $Project -Directory $appleOutput -BeforeRetry $BeforeRetry -Attempt {
        param($attemptNumber)
        $appleExitCode = Invoke-XHarness -ArgumentList $appleArguments -OutputDirectory $appleOutput
        Get-DeviceTestVerdict -Platform $Platform -Directory $appleOutput -RunnerExitCode $appleExitCode -Attempt $attemptNumber
    }

    if ($verdict.ExitCode -ne 0 -and $null -ne $OnFailure) {
        try {
            & $OnFailure $appleOutput
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
