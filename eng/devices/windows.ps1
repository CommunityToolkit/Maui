<#
.SYNOPSIS
    Builds the CommunityToolkit.Maui device test apps for Windows, packaged (MSIX) or unpackaged, and runs them.

.DESCRIPTION
    Mirrors dotnet/maui's eng/devices/windows.cake and run-windows-devicetests.cmd. Windows does not use xharness;
    the app is its own headless runner (HeadlessRunner/Windows in the runner project):
      App.exe "<results.xml>"           runs every test and writes xUnit XML to <results.xml>
      App.exe "<results.xml>" -1        writes the test categories to devicetestcategories.txt next to <results.xml>
      App.exe "<results.xml>" <index>   runs only that category and writes <results>_<Category>.xml
    Without -TestFilter one launch runs every test (the apps skip the human-only category themselves). A TestFilter
    (dotnet/maui syntax, Category=<name> or SkipCategories=<a>,<b>) is applied the way dotnet/maui runs
    Controls.DeviceTests: discover the categories, then launch the app once per selected category.

    packaged:   'dotnet publish' signs the MSIX with a self-signed certificate whose subject is the manifest
                Publisher (created in CurrentUser\My and trusted in LocalMachine\TrustedPeople, which needs an elevated
                shell the first time), installs it with its framework dependencies and launches it through
                Run-PackagedAppAndWait.ps1 (vendored from dotnet/maui), which waits for the real app process.
    unpackaged: 'dotnet publish -p:WindowsPackageType=None' with the Windows App SDK bundled, launched directly.

    The process exit code of the app carries no information (it kills itself when done), so the results gate in
    devices-shared.ps1 decides: no results file, 0 tests or a failure fail the run. The app also writes its log next to
    the results file (<results>.log, written through at once, also for packaged runs); when a launch crashes or hangs,
    the verdict names, from that log, the last test that started, the unhandled exceptions and the last exception
    thrown. Must run in Windows PowerShell 5.1 (the CI uses 'shell: powershell'); PowerShell 7 works too.

    Exit codes use xharness's vocabulary: 0 passed, 1 test failures, 3 invalid arguments, 4 app not built,
    70 timed out, 71 build or script failure, 78 install failure, 80 app crash, 83 launch failure,
    94 no results or no tests ran. See eng/devices/README.md.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File eng\devices\windows.ps1 -Packaging unpackaged

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File eng\devices\windows.ps1 -Packaging packaged -Project Maui -TestFilter "Category=IconTintColorBehavior"
#>
[CmdletBinding()]
param(
    [ValidateSet('buildOnly', 'testOnly', 'buildAndTest')]
    [string]$Target = 'buildAndTest',

    [Parameter(Mandatory = $true)]
    [ValidateSet('packaged', 'unpackaged')]
    [string]$Packaging,

    # Device test app(s): CommunityToolkit.Maui.DeviceTests (default), MediaElement, Camera, All, or a .csproj path.
    [string[]]$Project = @('CommunityToolkit.Maui.DeviceTests'),

    [string]$Configuration = 'Debug',

    # Defaults to artifacts\device-tests\windows-<packaging>; each app writes to a sub-folder named after the project.
    # Keep it outside %LOCALAPPDATA%, %APPDATA% and %TEMP%: MSIX redirects a packaged app's writes there.
    [string]$Results,

    # Defaults to artifacts\logs.
    [string]$BinlogDir,

    # dotnet/maui syntax: 'Category=<name>' or 'SkipCategories=<name>,<name>'. Defaults to $env:TEST_FILTER.
    [string]$TestFilter,

    # Capabilities this machine lacks, such as VideoRendering: their tests are skipped (DeviceTestCapabilities). Defaults to
    # $env:DEVICE_TEST_SKIP_CAPABILITIES, which only the CI jobs set.
    [string[]]$SkipCapabilities,

    # Capabilities this machine must offer, such as MediaPlayback: their tests fail instead of skipping when it does not. Defaults to
    # $env:DEVICE_TEST_REQUIRE_CAPABILITIES, which only the CI jobs set.
    [string[]]$RequireCapabilities,

    # Time budget for all launches of one app.
    [string]$Timeout = '00:30:00',

    # Time limit of one category launch (dotnet/maui uses 480 s).
    [string]$CategoryTimeout = '00:08:00',

    # Retries for infrastructure failures only. Defaults to 1 on CI and 0 locally.
    [int]$InfraRetries = -1,

    # Subject of the MSIX signing certificate. Defaults to the Publisher in Platforms\Windows\Package.appxmanifest.
    [string]$CertificateSubject,

    # Leave the packaged app installed after the run.
    [switch]$KeepDevice,

    # Extra arguments for 'dotnet publish', for example '/p:Name=Value' (a value starting with -p: is read as a parameter name).
    [string[]]$BuildArgs = @()
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
. (Join-Path $PSScriptRoot 'devices-shared.ps1')

$script:CertificateFriendlyName = 'CommunityToolkit.Maui device tests'
$script:AppxModuleImported = $false

function Import-AppxCmdlets {
    if ($script:AppxModuleImported) {
        return
    }

    if ($PSVersionTable.PSEdition -eq 'Core') {
        # The Appx cmdlets are Windows PowerShell modules; PowerShell 7 loads them through a compatibility session.
        Import-Module Appx -UseWindowsPowerShell -WarningAction SilentlyContinue
    }

    $script:AppxModuleImported = $true
}

function Get-ManifestPublisher {
    param($Project)

    $manifestPath = Join-Path $Project.Directory (Join-Path 'Platforms' (Join-Path 'Windows' 'Package.appxmanifest'))
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "$manifestPath not found; pass -CertificateSubject."
    }

    $manifest = New-Object System.Xml.XmlDocument
    $manifest.Load($manifestPath)
    $publisher = $manifest.DocumentElement.Identity.Publisher
    if (-not $publisher) {
        throw "No Identity Publisher in $manifestPath; pass -CertificateSubject."
    }

    return $publisher
}

function Get-SigningCertificateThumbprint {
    # The certificate is made the way dotnet/maui's GenerateMsixCert task makes it. Its subject must equal the manifest Publisher,
    # and trusting it in LocalMachine\TrustedPeople needs admin once.
    param([string]$Subject)

    $certificate = Get-ChildItem -Path 'Cert:\CurrentUser\My' |
        Where-Object { $_.Subject -eq $Subject -and $_.FriendlyName -eq $script:CertificateFriendlyName -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date).AddDays(1) } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if ($null -eq $certificate) {
        Write-Host "Creating self-signed certificate '$Subject' in Cert:\CurrentUser\My"
        $certificate = New-SelfSignedCertificate -Type Custom -Subject $Subject -KeyUsage DigitalSignature `
            -FriendlyName $script:CertificateFriendlyName -CertStoreLocation 'Cert:\CurrentUser\My' -NotAfter (Get-Date).AddYears(1) `
            -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
    }

    if (-not (Test-Path -LiteralPath "Cert:\LocalMachine\TrustedPeople\$($certificate.Thumbprint)")) {
        $publicCertificate = Join-Path ([System.IO.Path]::GetTempPath()) "CommunityToolkit.Maui.DeviceTests-$($certificate.Thumbprint).cer"
        Export-Certificate -Cert $certificate -FilePath $publicCertificate | Out-Null
        try {
            Import-Certificate -FilePath $publicCertificate -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
        }
        catch {
            throw "Could not trust the signing certificate in Cert:\LocalMachine\TrustedPeople ($($_.Exception.Message)). Run this once from an elevated shell."
        }
        finally {
            Remove-Item -LiteralPath $publicCertificate -Force -ErrorAction SilentlyContinue
        }
    }

    return $certificate.Thumbprint
}

function Get-WindowsBuildProperties {
    # The same properties are used to build and to find the build output.
    param([string]$Architecture, [string]$Thumbprint)

    $properties = @("RuntimeIdentifierOverride=win-$Architecture", 'SelfContained=true')
    if ($Packaging -eq 'packaged') {
        $properties += @('WindowsPackageType=MSIX', 'AppxPackageSigningEnabled=true')
        if ($Thumbprint) {
            $properties += "PackageCertificateThumbprint=$Thumbprint"
        }
    }
    else {
        # _CtmDeviceTestUnpackaged makes the app project (only) bundle the Windows App SDK, like dotnet/maui's
        # _MauiDeviceTestUnpackaged; WindowsAppSDKSelfContained itself must not be global (it would reach every library).
        $properties += @('WindowsPackageType=None', '_CtmDeviceTestUnpackaged=true')
    }

    return , $properties
}

function Find-AppPackage {
    # A recursive search also finds the framework packages under Dependencies, which dotnet/maui's **/AppPackages/*/*.msix does not reach.
    # Every configuration packages into the same AppPackages folder (<name>_<version>_<arch>_Debug.msix, but <name>_<version>_<arch>.msix
    # for Release), so the newest package alone can be another configuration's.
    param($Project, [string]$Configuration)

    $suffix = if ($Configuration -eq 'Release') { '' } else { "_$Configuration" }
    $packages = @(Get-ChildItem -LiteralPath $Project.Directory -Recurse -Filter '*.msix' -File -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '[\\/]AppPackages[\\/]' -and $_.FullName -notmatch '[\\/]Dependencies[\\/]' } |
            Where-Object { $_.BaseName -match "_(x86|x64|arm64)$([regex]::Escape($suffix))$" } |
            Sort-Object LastWriteTimeUtc -Descending)
    if ($packages.Count -eq 0) {
        return $null
    }

    return $packages[0]
}

function Get-PackageIdentityName {
    # MAUI derives the package identity from ApplicationIdGuid or ApplicationId, so it is read from the MSIX's AppxManifest.xml.
    param([string]$PackagePath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entry = $archive.GetEntry('AppxManifest.xml')
        if ($null -eq $entry) {
            throw "$PackagePath has no AppxManifest.xml."
        }

        $reader = New-Object System.IO.StreamReader($entry.Open())
        try {
            $manifest = New-Object System.Xml.XmlDocument
            $manifest.LoadXml($reader.ReadToEnd())
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }

    return $manifest.DocumentElement.Identity.Name
}

function Invoke-WindowsProcess {
    param(
        [string]$FilePath,
        [string]$Arguments,
        [string]$WorkingDirectory,
        [int]$TimeoutSeconds,
        [string]$LogPrefix
    )

    $standardOutput = "$LogPrefix.stdout.txt"
    $standardError = "$LogPrefix.stderr.txt"
    Write-Host "> $FilePath $Arguments"
    $startArguments = @{
        FilePath               = $FilePath
        ArgumentList           = $Arguments
        PassThru               = $true
        NoNewWindow            = $true
        RedirectStandardOutput = $standardOutput
        RedirectStandardError  = $standardError
    }
    if ($WorkingDirectory) {
        $startArguments['WorkingDirectory'] = $WorkingDirectory
    }

    $process = Start-Process @startArguments
    # Reading Handle keeps the process handle open so ExitCode is still available after the process exits.
    $null = $process.Handle
    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) {
        Write-DeviceTestWarning "$FilePath did not exit within $TimeoutSeconds s; killing it."
        try {
            $process.Kill()
        }
        catch {
            Write-Verbose "Kill failed: $($_.Exception.Message)"
        }
    }

    $process.WaitForExit()
    $output = ''
    foreach ($file in @($standardOutput, $standardError)) {
        if (Test-Path -LiteralPath $file) {
            $text = Get-Content -LiteralPath $file -Raw
            if ($text) {
                $output += $text
            }
        }
    }

    if ($output) {
        Write-Host $output.TrimEnd()
    }

    $exitCode = $null
    if (-not $timedOut) {
        $exitCode = $process.ExitCode
    }

    return New-Object PSObject -Property @{ TimedOut = $timedOut; ExitCode = $exitCode; Output = $output }
}

function Invoke-AppLaunch {
    param($Run, [string]$AppArguments, [int]$TimeoutSeconds, [string]$Name)

    $logPrefix = Join-Path $Run.Output "launch-$Name"
    if ($Packaging -eq 'packaged') {
        # Run-PackagedAppAndWait.ps1 activates the package and waits for the real app process: exit 0 = exited ('exited
        # with code <n>'), 2 = timed out (it kills the app), 4 = the app was gone right after activation (a crash at
        # startup), anything else = launch failure.
        $launcher = Join-Path $PSScriptRoot 'Run-PackagedAppAndWait.ps1'
        $arguments = '-NoProfile -ExecutionPolicy Bypass -File ' + (ConvertTo-WindowsCommandLineArgument $launcher) +
            ' -PackageName ' + (ConvertTo-WindowsCommandLineArgument $Run.Identity) +
            ' -AppArguments ' + (ConvertTo-WindowsCommandLineArgument $AppArguments) +
            " -TimeoutSeconds $TimeoutSeconds"
        $launch = Invoke-WindowsProcess -FilePath 'powershell.exe' -Arguments $arguments -TimeoutSeconds ($TimeoutSeconds + 120) -LogPrefix $logPrefix
        $appExitCode = $null
        if ($launch.Output -match 'exited with code (-?\d+)') {
            $appExitCode = [int]$Matches[1]
        }

        if ($launch.TimedOut -or $launch.ExitCode -eq 2) {
            Stop-Process -Name $Run.ProcessName -Force -ErrorAction SilentlyContinue
            return New-Object PSObject -Property @{ Status = 'timeout'; AppExitCode = $null }
        }

        if ($launch.ExitCode -eq 4) {
            return New-Object PSObject -Property @{ Status = 'exited-at-launch'; AppExitCode = $null }
        }

        if ($launch.ExitCode -ne 0) {
            return New-Object PSObject -Property @{ Status = 'launch-failure'; AppExitCode = $null }
        }

        return New-Object PSObject -Property @{ Status = 'ok'; AppExitCode = $appExitCode }
    }

    $launch = Invoke-WindowsProcess -FilePath $Run.Executable -Arguments $AppArguments -WorkingDirectory (Split-Path -Parent $Run.Executable) -TimeoutSeconds $TimeoutSeconds -LogPrefix $logPrefix
    if ($launch.TimedOut) {
        Stop-Process -Name $Run.ProcessName -Force -ErrorAction SilentlyContinue
        return New-Object PSObject -Property @{ Status = 'timeout'; AppExitCode = $null }
    }

    return New-Object PSObject -Property @{ Status = 'ok'; AppExitCode = $launch.ExitCode }
}

function Get-AppExitDescription {
    # The app ends itself with Process.Kill (-1) or a normal exit (0); anything else means it crashed.
    param($ExitCode)

    if ($null -eq $ExitCode) {
        return New-Object PSObject -Property @{ Crashed = $false; Text = 'exit code unknown' }
    }

    $hex = '0x{0:X8}' -f ([int64]$ExitCode -band 0xFFFFFFFFL)
    $crashed = -not ($ExitCode -eq 0 -or $ExitCode -eq -1)
    return New-Object PSObject -Property @{ Crashed = $crashed; Text = "exit code $ExitCode ($hex)" }
}

function Enable-CrashDumps {
    # Needs admin; the dumps are skipped without it.
    param([string]$ExecutableName, [string]$Folder)

    $key = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\$ExecutableName"
    if (Test-Path -LiteralPath $key) {
        return $null
    }

    try {
        New-Item -Path $key -Force | Out-Null
        New-ItemProperty -Path $key -Name 'DumpFolder' -PropertyType ExpandString -Value $Folder -Force | Out-Null
        New-ItemProperty -Path $key -Name 'DumpType' -PropertyType DWord -Value 2 -Force | Out-Null
        New-ItemProperty -Path $key -Name 'DumpCount' -PropertyType DWord -Value 3 -Force | Out-Null
        return $key
    }
    catch {
        Write-Verbose "Crash dumps not enabled: $($_.Exception.Message)"
        return $null
    }
}

function Save-WindowsDiagnostics {
    param([string]$Folder, [datetime]$Since, [bool]$TakeScreenshot)

    try {
        Get-WinEvent -FilterHashtable @{ LogName = 'Application'; Level = 1, 2, 3; StartTime = $Since } -ErrorAction SilentlyContinue |
            Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message |
            Format-List |
            Out-String -Width 400 |
            Set-Content -LiteralPath (Join-Path $Folder 'application-events.txt') -Encoding UTF8
    }
    catch {
        Write-Verbose "Event log not saved: $($_.Exception.Message)"
    }

    if ($TakeScreenshot) {
        try {
            Add-Type -AssemblyName System.Windows.Forms, System.Drawing
            $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
            $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            $graphics.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $bitmap.Size)
            $bitmap.Save((Join-Path $Folder 'screen.png'), [System.Drawing.Imaging.ImageFormat]::Png)
            $graphics.Dispose()
            $bitmap.Dispose()
        }
        catch {
            Write-Verbose "Screenshot not saved: $($_.Exception.Message)"
        }
    }
}

function Install-AppPackage {
    param($Run, [string]$Architecture)

    Import-AppxCmdlets
    $existing = Get-AppxPackage -Name $Run.Identity
    if ($null -ne $existing) {
        Write-Host "Removing installed $($Run.Identity)"
        $existing | Remove-AppxPackage
    }

    $packageFolder = Split-Path -Parent $Run.Package
    $dependencyFolder = Join-Path $packageFolder (Join-Path 'Dependencies' $Architecture)
    if (Test-Path -LiteralPath $dependencyFolder) {
        foreach ($dependency in (Get-ChildItem -LiteralPath $dependencyFolder -File | Where-Object { $_.Extension -eq '.msix' -or $_.Extension -eq '.appx' })) {
            Write-Host "Installing dependency $($dependency.Name)"
            try {
                Add-AppxPackage -Path $dependency.FullName
            }
            catch {
                # Usually 'a higher version is already installed', which is fine.
                Write-Host "  $($_.Exception.Message.Trim())"
            }
        }
    }

    # On a machine that did not build the package, trust the certificate published next to it (as Helix does).
    $certificateFile = Get-ChildItem -LiteralPath $packageFolder -Filter '*.cer' -File | Select-Object -First 1
    if ($null -ne $certificateFile) {
        $publicCertificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $certificateFile.FullName
        if (-not (Test-Path -LiteralPath "Cert:\LocalMachine\TrustedPeople\$($publicCertificate.Thumbprint)")) {
            try {
                Import-Certificate -FilePath $certificateFile.FullName -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
            }
            catch {
                Write-DeviceTestWarning "Could not trust $($certificateFile.Name): $($_.Exception.Message)"
            }
        }
    }

    Write-Host "Installing $($Run.Package)"
    try {
        Add-AppxPackage -Path $Run.Package
    }
    catch {
        return "Add-AppxPackage failed: $($_.Exception.Message)"
    }

    if ($null -eq (Get-AppxPackage -Name $Run.Identity)) {
        return "$($Run.Identity) is not installed after Add-AppxPackage."
    }

    return $null
}

function Select-TestCategories {
    # Follows the semantics of dotnet/maui's DeviceTestSharedHelpers.GetExcludedTestCategories (case-sensitive names).
    # The human-only category is never selected: a category launch skips only the other discovered categories, not the
    # app's own headless skip list.
    param([string[]]$Categories, [string]$Filter)

    $runnable = @($Categories | Where-Object { $_ -cne $script:HumanOnlyTestCategory })
    if ($Filter.StartsWith('Category=', [System.StringComparison]::Ordinal)) {
        $wanted = $Filter.Substring('Category='.Length)
        return , @($runnable | Where-Object { $_ -ceq $wanted })
    }

    $skipped = @($Filter.Substring('SkipCategories='.Length) -split ',')
    return , @($runnable | Where-Object { -not ($skipped -ccontains $_) })
}

function Invoke-WindowsAttempt {
    param($Context, $Run, [int]$Attempt, [string]$Architecture)

    $outcome = New-Object PSObject -Property @{
        Runner          = 0
        Problems        = (New-Object System.Collections.Generic.List[string])
        ProblemExitCode = 94
    }
    $started = Get-Date

    if ($Packaging -eq 'packaged') {
        $installError = Install-AppPackage -Run $Run -Architecture $Architecture
        if ($installError) {
            Save-WindowsDiagnostics -Folder $Run.Output -Since $started -TakeScreenshot $false
            return Get-DeviceTestVerdict -Platform 'windows' -Directory $Run.Output -RunnerExitCode 78 -Problems @($installError) -Attempt $Attempt
        }
    }

    # A packaged app does not inherit this script's environment, so the capability declarations reach the app in a file next to its results
    # (DeviceTestCapabilities reads it). Every attempt writes it: a retry moves the previous attempt's files into attempt-<n>.
    $declarations = @(Get-DeviceTestCapabilityDeclarations -Context $Context)
    if ($declarations.Count -gt 0) {
        [System.IO.File]::WriteAllLines((Join-Path $Run.Output 'devicetestcapabilities.txt'), [string[]]$declarations, (New-Object System.Text.UTF8Encoding($false)))
    }

    $dumpKey = Enable-CrashDumps -ExecutableName "$($Run.ProcessName).exe" -Folder (Join-Path $Run.Output 'dumps')
    try {
        if ($Context.TestFilter -eq "SkipCategories=$script:HumanOnlyTestCategory") {
            # The default filter only skips the human-only category, which the apps skip on their own in headless runs,
            # so one launch runs everything (dotnet/maui does the same for every project except Controls.DeviceTests).
            Invoke-WholeRun -Context $Context -Run $Run -Outcome $outcome
        }
        else {
            Invoke-CategoryRuns -Context $Context -Run $Run -Outcome $outcome
        }
    }
    finally {
        if ($dumpKey) {
            Remove-Item -LiteralPath $dumpKey -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    $verdict = Get-DeviceTestVerdict -Platform 'windows' -Directory $Run.Output -RunnerExitCode $outcome.Runner -Problems @($outcome.Problems) -ProblemExitCode $outcome.ProblemExitCode -Attempt $Attempt
    if ($verdict.ExitCode -ne 0) {
        $onCi = $env:CI -eq 'true' -or $env:GITHUB_ACTIONS -eq 'true'
        Save-WindowsDiagnostics -Folder $Run.Output -Since $started -TakeScreenshot ($onCi -and $outcome.Runner -eq 70)
    }

    return $verdict
}

function Test-ResultsWritten {
    # The runner creates its results file when the tests start and fills it when they end, so an app that died during the run
    # leaves an empty file behind.
    param([string]$Path)

    $item = Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue
    return ($null -ne $item) -and -not $item.PSIsContainer -and $item.Length -gt 0
}

function Get-RunLogDiagnosis {
    # The last exception thrown is reported because a failing WinRT callback, such as a DispatcherQueue handler, ends the app
    # with a stowed exception that no unhandled-exception event reports. The app writes the log next to its results file,
    # written through at once (TestRunLog in the runner project's HeadlessRunner/windows), so it survives a crash and exists
    # for packaged runs too.
    param([string]$ResultsPath)

    $logPath = [System.IO.Path]::ChangeExtension($ResultsPath, '.log')
    if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
        return ''
    }

    $lines = @(Get-Content -LiteralPath $logPath -Encoding UTF8 -ErrorAction SilentlyContinue)
    $parts = @()
    $lastStartedIndex = -1
    for ($index = $lines.Count - 1; $index -ge 0; $index--) {
        if ($lines[$index] -match '\[STRT\]\S*\s+(.+)$') {
            $parts += "the last test that started is $($Matches[1].Trim())"
            $lastStartedIndex = $index
            break
        }
    }

    foreach ($line in @($lines | Where-Object { $_.StartsWith('[UNHANDLED]') } | Select-Object -First 3)) {
        $parts += $line.Trim()
    }

    $lastThrown = @($lines | Select-Object -Skip ($lastStartedIndex + 1) | Where-Object { $_.StartsWith('[THROWN]') }) | Select-Object -Last 1
    if ($lastThrown) {
        $when = 'the last exception thrown'
        if ($lastStartedIndex -ge 0) {
            $when = 'the last exception thrown after it started'
        }

        $parts += "$($when): $($lastThrown.Substring('[THROWN] '.Length).Trim())"
    }

    if ($parts.Count -eq 0) {
        return ''
    }

    return " Run log ($(Split-Path -Leaf $logPath)): $($parts -join '; ')"
}

function Add-LaunchOutcome {
    param($Outcome, $Launch, [string]$ExpectedResults, [string]$What, [int]$TimeoutSeconds)

    if ($Launch.Status -eq 'timeout') {
        $Outcome.Runner = 70
        $Outcome.Problems.Add("$What did not finish within $TimeoutSeconds s.$(Get-RunLogDiagnosis -ResultsPath $ExpectedResults)")
    }
    elseif ($Launch.Status -eq 'launch-failure') {
        if ($Outcome.Runner -eq 0) {
            $Outcome.Runner = 83
        }

        $Outcome.Problems.Add("$What could not be launched.")
    }
    elseif (-not (Test-ResultsWritten $ExpectedResults)) {
        $wrote = 'wrote no'
        if (Test-Path -LiteralPath $ExpectedResults -PathType Leaf) {
            $wrote = 'left an empty'
        }

        $diagnosis = Get-RunLogDiagnosis -ResultsPath $ExpectedResults
        if ($Launch.Status -eq 'exited-at-launch') {
            $Outcome.Problems.Add("$What $wrote $(Split-Path -Leaf $ExpectedResults): the app exited right after it was activated (a crash at startup).$diagnosis")
            $Outcome.ProblemExitCode = 80
            return
        }

        $exit = Get-AppExitDescription $Launch.AppExitCode
        $Outcome.Problems.Add("$What $wrote $(Split-Path -Leaf $ExpectedResults) ($($exit.Text)).$diagnosis")
        if ($exit.Crashed) {
            $Outcome.ProblemExitCode = 80
        }
    }
}

function Get-ResultsArgument {
    # The results path is always quoted, as dotnet/maui passes it; a Windows path cannot contain a quote.
    param($Run)

    return '"' + $Run.ResultsFile + '"'
}

function Invoke-WholeRun {
    param($Context, $Run, $Outcome)

    $launch = Invoke-AppLaunch -Run $Run -AppArguments (Get-ResultsArgument $Run) -TimeoutSeconds $Context.TimeoutSeconds -Name 'all'
    Add-LaunchOutcome -Outcome $Outcome -Launch $launch -ExpectedResults $Run.ResultsFile -What 'The test run' -TimeoutSeconds $Context.TimeoutSeconds
}

function Invoke-CategoryRuns {
    # dotnet/maui's category protocol (ControlsHeadlessTestRunner): -1 lists the categories, an index runs one category.
    param($Context, $Run, $Outcome)

    $deadline = (Get-Date).AddSeconds($Context.TimeoutSeconds)
    $categoriesFile = Join-Path $Run.Output 'devicetestcategories.txt'
    $discoveryTimeout = [Math]::Min(120, $Context.TimeoutSeconds)
    $discovery = Invoke-AppLaunch -Run $Run -AppArguments ((Get-ResultsArgument $Run) + ' -1') -TimeoutSeconds $discoveryTimeout -Name 'discovery'
    Add-LaunchOutcome -Outcome $Outcome -Launch $discovery -ExpectedResults $categoriesFile -What 'Category discovery' -TimeoutSeconds $discoveryTimeout
    if (-not (Test-Path -LiteralPath $categoriesFile -PathType Leaf)) {
        return
    }

    $categories = @(Get-Content -LiteralPath $categoriesFile -Encoding UTF8 | Where-Object { $_.Trim().Length -gt 0 })
    if ($categories.Count -eq 0) {
        $Outcome.Problems.Add('Category discovery found no categories (every test class needs a [Category]).')
        return
    }

    $selected = Select-TestCategories -Categories $categories -Filter $Context.TestFilter
    Write-Host "Categories: $($categories.Count) discovered, $($selected.Count) selected by '$($Context.TestFilter)'"
    if ($selected.Count -eq 0) {
        $Outcome.Problems.Add("TestFilter '$($Context.TestFilter)' selects none of the discovered categories: $($categories -join ', ').")
        return
    }

    $resultsBase = $Run.ResultsFile.Substring(0, $Run.ResultsFile.Length - '.xml'.Length)
    foreach ($category in $selected) {
        $remaining = [int]($deadline - (Get-Date)).TotalSeconds
        if ($remaining -le 0) {
            $Outcome.Runner = 70
            $Outcome.Problems.Add("Category $category not run: the $($Context.Timeout) time budget is used up.")
            continue
        }

        $index = [Array]::IndexOf($categories, $category)
        $limit = [Math]::Min($Context.CategoryTimeoutSeconds, $remaining)
        Write-DeviceTestHeader "Category $category ($index)"
        $launch = Invoke-AppLaunch -Run $Run -AppArguments ((Get-ResultsArgument $Run) + " $index") -TimeoutSeconds $limit -Name $category
        Add-LaunchOutcome -Outcome $Outcome -Launch $launch -ExpectedResults "$($resultsBase)_$category.xml" -What "Category $category" -TimeoutSeconds $limit
    }
}

function Invoke-WindowsDeviceTests {
    param($Context, $Project, [string]$Architecture)

    $windowsOutput = Join-Path $Context.Results $Project.Name
    Initialize-DeviceTestResultsDirectory -Path $windowsOutput

    $framework = Get-DeviceTestTargetFramework -Project $Project -Platform 'windows'
    # RuntimeIdentifier makes the evaluated PublishDir match where RuntimeIdentifierOverride puts the published app.
    $evaluation = @("TargetFramework=$framework", "Configuration=$($Context.Configuration)", "RuntimeIdentifier=win-$Architecture") + (Get-WindowsBuildProperties -Architecture $Architecture)
    $properties = Get-DeviceTestMSBuildProperties -ProjectPath $Project.Path -Name @('ApplicationId', 'AssemblyName', 'PublishDir') -Property $evaluation

    $run = New-Object PSObject -Property @{
        Output      = $windowsOutput
        ResultsFile = (Join-Path $windowsOutput ("TestResults-$($properties['ApplicationId'].Replace('.', '_')).xml"))
        ProcessName = $properties['AssemblyName']
        Identity    = ''
        Package     = ''
        Executable  = ''
    }

    if ($Packaging -eq 'packaged') {
        $package = Find-AppPackage -Project $Project -Configuration $Context.Configuration
        if ($null -eq $package) {
            $verdict = New-DeviceTestVerdict -ExitCode 4 -Directory $windowsOutput -Reason "No $($Context.Configuration) MSIX under $($Project.Directory)\**\AppPackages. Build it first with -Target buildOnly or buildAndTest."
            $verdict.Project = $Project.Name
            return $verdict
        }

        $run.Package = $package.FullName
        $run.Identity = Get-PackageIdentityName -PackagePath $package.FullName
        Write-DeviceTestHeader "Test $($Project.Name) (package $($run.Identity), $($package.Name))"
    }
    else {
        $executableName = "$($properties['AssemblyName']).exe"
        $executable = Join-Path (Get-DeviceTestOutputDirectory -Project $Project -Path $properties['PublishDir']) $executableName
        if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
            $binFolder = Join-Path $Project.Directory (Join-Path 'bin' (Join-Path $Context.Configuration $framework))
            $found = Get-ChildItem -LiteralPath $binFolder -Recurse -Filter $executableName -File -ErrorAction SilentlyContinue |
                Where-Object { $_.DirectoryName -match '[\\/]publish$' } |
                Sort-Object LastWriteTimeUtc -Descending |
                Select-Object -First 1
            if ($null -ne $found) {
                $executable = $found.FullName
            }
        }

        if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
            $verdict = New-DeviceTestVerdict -ExitCode 4 -Directory $windowsOutput -Reason "$executable not found. Build it first with -Target buildOnly or buildAndTest."
            $verdict.Project = $Project.Name
            return $verdict
        }

        $run.Executable = $executable
        Write-DeviceTestHeader "Test $($Project.Name) ($executable)"
    }

    try {
        return Invoke-DeviceTestWithRetries -Context $Context -Project $Project -Directory $windowsOutput -Attempt {
            param($attemptNumber)
            Invoke-WindowsAttempt -Context $Context -Run $run -Attempt $attemptNumber -Architecture $Architecture
        }
    }
    finally {
        if ($Packaging -eq 'packaged' -and -not $KeepDevice -and $run.Identity) {
            try {
                Import-AppxCmdlets
                Get-AppxPackage -Name $run.Identity | Remove-AppxPackage
            }
            catch {
                Write-DeviceTestWarning "Could not uninstall $($run.Identity): $($_.Exception.Message)"
            }
        }
    }
}

$exitCode = 71
try {
    if (-not $script:DeviceTestsOnWindows) {
        throw (New-Object System.ArgumentException 'Windows device tests need Windows.')
    }

    $context = Initialize-DeviceTestRun -Platform 'windows' -Leg "windows-$Packaging" -Target $Target -Project $Project -Configuration $Configuration `
        -Results $Results -BinlogDir $BinlogDir -TestFilter $TestFilter -TestFilterIsBound $PSBoundParameters.ContainsKey('TestFilter') `
        -SkipCapabilities $SkipCapabilities -SkipCapabilitiesIsBound $PSBoundParameters.ContainsKey('SkipCapabilities') `
        -RequireCapabilities $RequireCapabilities -RequireCapabilitiesIsBound $PSBoundParameters.ContainsKey('RequireCapabilities') `
        -Timeout $Timeout -LaunchTimeout $CategoryTimeout -InfraRetries $InfraRetries
    $context | Add-Member -NotePropertyName 'CategoryTimeoutSeconds' -NotePropertyValue $context.LaunchSeconds

    $architecture = Get-DeviceTestHostArchitecture
    $context.DeviceDescription = "$env:COMPUTERNAME, Windows $([System.Environment]::OSVersion.Version), $Packaging, win-$architecture"

    if ($Target -ne 'testOnly') {
        foreach ($item in $context.Projects) {
            $framework = Get-DeviceTestTargetFramework -Project $item -Platform 'windows'
            $thumbprint = ''
            if ($Packaging -eq 'packaged') {
                $subject = $CertificateSubject
                if (-not $subject) {
                    $subject = Get-ManifestPublisher -Project $item
                }

                $thumbprint = Get-SigningCertificateThumbprint -Subject $subject

                # Old packages would otherwise be picked up by Find-AppPackage.
                Get-ChildItem -LiteralPath $item.Directory -Recurse -Directory -Filter 'AppPackages' -ErrorAction SilentlyContinue |
                    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            }

            [void](Invoke-DeviceTestBuild -Context $context -Project $item -TargetFramework $framework -Command 'publish' -BuildArgs $BuildArgs `
                    -Property (Get-WindowsBuildProperties -Architecture $architecture -Thumbprint $thumbprint))
        }
    }

    if ($Target -ne 'buildOnly') {
        foreach ($item in $context.Projects) {
            if ($context.BuildFailures.ContainsKey($item.Name)) {
                $verdict = New-DeviceTestVerdict -ExitCode 71 -Directory (Join-Path $context.Results $item.Name) -Reason "Not run: $($context.BuildFailures[$item.Name])."
            }
            else {
                $verdict = Invoke-WindowsDeviceTests -Context $context -Project $item -Architecture $architecture
            }

            $verdict.Project = $item.Name
            New-Item -ItemType Directory -Force -Path $verdict.Directory | Out-Null
            Add-DeviceTestVerdict -Context $context -Verdict $verdict
        }
    }

    $exitCode = Complete-DeviceTestRun -Context $context
}
catch {
    $exitCode = Write-DeviceTestException $_
}

Write-Host "Exit code: $exitCode ($(Get-DeviceTestExitCodeName $exitCode))"
exit $exitCode
