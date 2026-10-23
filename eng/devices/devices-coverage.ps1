# Used by android.ps1, ios.ps1 and catalyst.ps1 with -Coverage (see eng/devices/README.md, "Code coverage"). Dot-source it after
# devices-shared.ps1. PowerShell 7 only: the Windows device tests collect no coverage.
#
# coverlet (pinned in .config/dotnet-tools.json) instruments assemblies on disk, and writes its report once the command it started
# (its target) exits. The apps run on a device in between, so the target only waits until this script is done with them. The instrumented
# assemblies replace the toolkit's build outputs, and the apps are packaged again with them (BuildProjectReferences=false keeps the toolkit
# projects from being rebuilt). The packaged apps keep the instrumented assemblies until the apps are built again: that build packages the
# original ones (Restore-DeviceTestCoverageAssemblies).
# The headless runners write the hits files when the tests have run (CoverageRecorder in CommunityToolkit.Maui.DeviceTests.Runners); on
# Android xharness copies them from the device to the host before it uninstalls the app.
# Coverage never changes a run's exit code: a run whose coverage fails reports a warning and has no coverage report.

$script:DeviceTestCoverageAssemblies = @(
    'CommunityToolkit.Maui'
    'CommunityToolkit.Maui.Core'
    'CommunityToolkit.Maui.MediaElement'
    'CommunityToolkit.Maui.Camera'
    'CommunityToolkit.Maui.Maps'
)

# Must match CoverageRecorder.DirectoryVariable, which the headless runners read.
$script:DeviceTestCoverageVariable = 'CTM_COVERAGE_DIRECTORY'

# A hits file of coverlet: <assembly name>_<instrumentation identifier>, int32 count and int32[count] hits, little-endian.
$script:DeviceTestCoverageHitsFilePattern = '^(?<assembly>.+)_[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$'

function Get-DeviceTestCoverageHitsDirectory {
    # On Android, xharness copies the device folder (--device-out-folder) into the app's results folder, so both folder names must match.
    param([Parameter(Mandatory = $true)]$Context, [Parameter(Mandatory = $true)]$Project)

    return Join-Path (Join-Path $Context.Results $Project.Name) 'coverage'
}

function Get-DeviceTestReferences {
    param(
        [Parameter(Mandatory = $true)]$Project,
        [Parameter(Mandatory = $true)][string]$TargetFramework,
        [Parameter(Mandatory = $true)][string]$Configuration,
        [string[]]$Property = @()
    )

    # A build for another platform restores the assets file for its own runtime identifier: without -restore the query then fails with
    # NETSDK1047, and the apps are tested without coverage.
    $arguments = @('msbuild', $Project.Path, '-nologo', '-restore', '-target:ResolveReferences', '-getItem:ReferencePath',
        '-p:BuildProjectReferences=false', "-p:TargetFramework=$TargetFramework", "-p:Configuration=$Configuration")
    foreach ($item in $Property) {
        $arguments += "-p:$item"
    }

    $result = Invoke-DeviceTestNativeCapture -FilePath 'dotnet' -ArgumentList $arguments
    $text = ($result.Output -join "`n").Trim()
    $start = $text.IndexOf('{')
    if ($result.ExitCode -ne 0 -or $start -lt 0) {
        throw "dotnet $(Format-DeviceTestCommandLine $arguments) failed with exit code $($result.ExitCode):`n$text"
    }

    $json = $text.Substring($start) | ConvertFrom-Json
    return , @($json.Items.ReferencePath)
}

function Copy-DeviceTestAssembly {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Destination)

    Copy-Item -LiteralPath $Path -Destination $Destination -Force
    $symbols = [System.IO.Path]::ChangeExtension($Path, '.pdb')
    # CI builds embed the symbols in the assembly, so a .pdb file may not exist.
    if (Test-Path -LiteralPath $symbols -PathType Leaf) {
        Copy-Item -LiteralPath $symbols -Destination $Destination -Force
    }
}

function Restore-DeviceTestCoverageAssemblies {
    # The restored assemblies and the toolkit projects' intermediate assemblies (obj) get the current time. The next build of the apps
    # packages the original assemblies again only when they are newer than the apps' instrumented copies, and the toolkit projects' part of
    # that build copies each intermediate assembly over the restored one with the intermediate assembly's time.
    param([Parameter(Mandatory = $true)]$Session)

    foreach ($assembly in $Session.Assemblies) {
        if (-not $assembly.Replaced) {
            continue
        }

        foreach ($extension in @('.dll', '.pdb')) {
            $original = Join-Path $Session.Originals "$($assembly.Name)$extension"
            if (Test-Path -LiteralPath $original -PathType Leaf) {
                $destination = [System.IO.Path]::ChangeExtension($assembly.Path, $extension)
                Copy-Item -LiteralPath $original -Destination $destination -Force
                (Get-Item -LiteralPath $destination).LastWriteTimeUtc = [System.DateTime]::UtcNow
            }

            if ($assembly.Intermediate) {
                $intermediate = [System.IO.Path]::ChangeExtension($assembly.Intermediate, $extension)
                if (Test-Path -LiteralPath $intermediate -PathType Leaf) {
                    (Get-Item -LiteralPath $intermediate).LastWriteTimeUtc = [System.DateTime]::UtcNow
                }
            }
        }

        $assembly.Replaced = $false
    }
}

function Stop-DeviceTestCoverageProcess {
    param([Parameter(Mandatory = $true)]$Session, [int]$TimeoutSeconds = 600)

    if ($null -eq $Session.Process) {
        return $false
    }

    [System.IO.File]::WriteAllText($Session.DoneMarker, '')
    if (-not $Session.Process.WaitForExit($TimeoutSeconds * 1000)) {
        try {
            $Session.Process.Kill($true)
        }
        catch {
            Write-Verbose "Could not stop coverlet: $($_.Exception.Message)"
        }

        Write-DeviceTestWarning "Coverage: coverlet did not exit within $TimeoutSeconds s and was stopped. Its log: $($Session.Log)"
        return $false
    }

    $Session.Process.WaitForExit()
    if ($Session.Process.ExitCode -ne 0) {
        $tail = ''
        if (Test-Path -LiteralPath $Session.Log -PathType Leaf) {
            $tail = (Get-Content -LiteralPath $Session.Log -Tail 20) -join "`n"
        }

        Write-DeviceTestWarning "Coverage: coverlet exited with code $($Session.Process.ExitCode). The end of its log ($($Session.Log)):`n$tail"
        return $false
    }

    return $true
}

function Close-DeviceTestCoverage {
    param([Parameter(Mandatory = $true)]$Session)

    try {
        if ($null -ne $Session.Process -and -not $Session.Process.HasExited) {
            [void](Stop-DeviceTestCoverageProcess -Session $Session)
        }
    }
    finally {
        Restore-DeviceTestCoverageAssemblies -Session $Session
        Remove-Item -LiteralPath $Session.Work -Recurse -Force -ErrorAction SilentlyContinue
        if ((Test-Path -LiteralPath $Session.ErrorLog -PathType Leaf) -and (Get-Item -LiteralPath $Session.ErrorLog).Length -eq 0) {
            Remove-Item -LiteralPath $Session.ErrorLog -Force
        }
    }
}

function Start-DeviceTestCoverage {
    # The caller completes the session it gets with Complete-DeviceTestCoverage.
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Platform,
        # The properties the platform script builds the apps with: packaging them again needs the same.
        [string[]]$Property = @(),
        [string[]]$BuildArgs = @()
    )

    Write-DeviceTestHeader 'Coverage: instrument the toolkit assemblies with coverlet'

    $directory = Join-Path $Context.Results 'coverage'
    Initialize-DeviceTestResultsDirectory -Path $directory
    $work = Join-Path ([System.IO.Path]::GetTempPath()) "ctm-device-coverage-$([System.Guid]::NewGuid().ToString('N'))"
    $session = New-Object PSObject -Property @{
        Context     = $Context
        Platform    = $Platform
        Directory   = $directory
        # Named after the results folder (android, ios, maccatalyst by default), so that reports of several runs can sit side by side.
        Report      = (Join-Path $directory "$(Split-Path -Leaf $Context.Results).cobertura.xml")
        Log         = (Join-Path $directory 'coverlet.log')
        ErrorLog    = (Join-Path $directory 'coverlet-error.log')
        Work        = $work
        Stage       = (Join-Path $work 'stage')
        Originals   = (Join-Path $work 'originals')
        StartMarker = (Join-Path $work 'target-started')
        DoneMarker  = (Join-Path $work 'tests-done')
        Process     = $null
        Assemblies  = (New-Object System.Collections.ArrayList)
        Projects    = (New-Object System.Collections.ArrayList)
    }

    $handedOver = $false
    try {
        New-Item -ItemType Directory -Force -Path $session.Stage, $session.Originals | Out-Null

        # coverlet resolves what the toolkit assemblies reference while it instruments them, so the staging folder gets every assembly the
        # apps compile against. The toolkit assemblies are the toolkit projects' build outputs, which packaging takes.
        $toolkit = [ordered]@{}
        foreach ($item in $Context.Projects) {
            if ($Context.BuildFailures.ContainsKey($item.Name)) {
                continue
            }

            $framework = Get-DeviceTestTargetFramework -Project $item -Platform $Platform
            foreach ($reference in (Get-DeviceTestReferences -Project $item -TargetFramework $framework -Configuration $Context.Configuration -Property $Property)) {
                $path = [string]$reference.FullPath
                $name = [System.IO.Path]::GetFileNameWithoutExtension($path)
                if ($script:DeviceTestCoverageAssemblies -contains $name) {
                    $toolkit[$name] = $reference
                }
                elseif (-not (Test-Path -LiteralPath (Join-Path $session.Stage ([System.IO.Path]::GetFileName($path))))) {
                    Copy-Item -LiteralPath $path -Destination $session.Stage
                }
            }

            [void]$session.Projects.Add((New-Object PSObject -Property @{ Project = $item; Framework = $framework }))
        }

        if ($toolkit.Count -eq 0) {
            Write-DeviceTestWarning 'Coverage: the apps reference no toolkit assembly to instrument.'
            return $null
        }

        $includes = @()
        foreach ($name in $toolkit.Keys) {
            $path = [string]$toolkit[$name].FullPath
            # A project reference's CopyUpToDateMarker is in the project's IntermediateOutputPath, beside the intermediate assembly that
            # Restore-DeviceTestCoverageAssemblies gives the current time.
            $intermediate = $null
            $marker = [string]$toolkit[$name].CopyUpToDateMarker
            if ($marker) {
                $intermediate = Join-Path ([System.IO.Path]::GetDirectoryName($marker)) ([System.IO.Path]::GetFileName($path))
            }

            Copy-DeviceTestAssembly -Path $path -Destination $session.Stage
            Copy-DeviceTestAssembly -Path $path -Destination $session.Originals
            [void]$session.Assemblies.Add((New-Object PSObject -Property @{ Name = $name; Path = $path; Intermediate = $intermediate; Replaced = $false }))
            $includes += @('--include', "[$name]*")
            Write-Host "Instrumenting $path"
        }

        # The target only waits: until this script is done with the apps, or is gone. It is passed encoded, because coverlet hands
        # --targetargs to the target as one command line.
        $quote = { param([string]$Value) "'" + $Value.Replace("'", "''") + "'" }
        $wait = "[System.IO.File]::WriteAllText($(& $quote $session.StartMarker), ''); " +
            "while (-not [System.IO.File]::Exists($(& $quote $session.DoneMarker)) -and (Get-Process -Id $PID -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 500 }"
        $encoded = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($wait))
        $pwsh = (Get-Process -Id $PID).Path

        # --exclude-assemblies-without-sources None: CI builds use deterministic source paths (/_/...), which do not exist on disk.
        $arguments = @('coverlet', $session.Stage, '--target', $pwsh, '--targetargs', "-NoLogo -NoProfile -NonInteractive -EncodedCommand $encoded") +
            $includes + @('--exclude-assemblies-without-sources', 'None', '--format', 'cobertura', '--output', $session.Report, '--verbosity', 'normal')
        Write-Host "> dotnet $(Format-DeviceTestCommandLine $arguments)"
        $commandLine = ($arguments | ForEach-Object { ConvertTo-WindowsCommandLineArgument $_ }) -join ' '
        $session.Process = Start-Process -FilePath 'dotnet' -ArgumentList $commandLine -WorkingDirectory $script:DeviceTestsRepoRoot -NoNewWindow -PassThru `
            -RedirectStandardOutput $session.Log -RedirectStandardError $session.ErrorLog

        $deadline = [System.DateTime]::UtcNow.AddMinutes(10)
        while (-not (Test-Path -LiteralPath $session.StartMarker) -and -not $session.Process.HasExited -and [System.DateTime]::UtcNow -lt $deadline) {
            Start-Sleep -Milliseconds 500
        }

        if (-not (Test-Path -LiteralPath $session.StartMarker)) {
            $tail = ''
            if (Test-Path -LiteralPath $session.Log -PathType Leaf) {
                $tail = (Get-Content -LiteralPath $session.Log -Tail 20) -join "`n"
            }

            Write-DeviceTestWarning "Coverage: coverlet did not instrument the toolkit assemblies; the apps are tested without coverage. The end of its log ($($session.Log)):`n$tail"
            return $null
        }

        foreach ($assembly in $session.Assemblies) {
            $assembly.Replaced = $true
            Copy-DeviceTestAssembly -Path (Join-Path $session.Stage "$($assembly.Name).dll") -Destination ([System.IO.Path]::GetDirectoryName($assembly.Path))
        }

        foreach ($entry in $session.Projects) {
            Write-DeviceTestHeader "Coverage: package $($entry.Project.Name) with the instrumented toolkit assemblies"
            [void](Invoke-DeviceTestBuild -Context $Context -Project $entry.Project -TargetFramework $entry.Framework -BuildArgs $BuildArgs `
                    -Property ($Property + @('BuildProjectReferences=false')) -Suffix 'coverage')
        }

        $handedOver = $true
        return $session
    }
    catch {
        Write-DeviceTestWarning "Coverage: $($_.Exception.Message) The apps are tested without coverage."
        return $null
    }
    finally {
        # Also when the run is stopped (Ctrl+C), which skips the catch block: the caller has no session to complete yet.
        if (-not $handedOver) {
            Close-DeviceTestCoverage -Session $session
        }
    }
}

function Get-DeviceTestCoverageArguments {
    param($Session, [Parameter(Mandatory = $true)]$Project, [string]$ApplicationId)

    if ($null -eq $Session) {
        return , @()
    }

    if ($Session.Platform -eq 'android') {
        # The app's external files folder: the app needs no permission to write there, adb can read it, and it goes away with the app.
        $deviceFolder = "/sdcard/Android/data/$ApplicationId/files/coverage"
        return , @("--arg=$script:DeviceTestCoverageVariable=$deviceFolder", "--device-out-folder=$deviceFolder")
    }

    return , @("--set-env=$script:DeviceTestCoverageVariable=$(Get-DeviceTestCoverageHitsDirectory -Context $Session.Context -Project $Project)")
}

function Read-DeviceTestCoverageHits {
    param([Parameter(Mandatory = $true)][string]$Path)

    $reader = New-Object System.IO.BinaryReader([System.IO.File]::OpenRead($Path))
    try {
        $count = $reader.ReadInt32()
        $hits = New-Object 'int[]' $count
        for ($index = 0; $index -lt $count; $index++) {
            $hits[$index] = $reader.ReadInt32()
        }

        return , $hits
    }
    finally {
        $reader.Dispose()
    }
}

function Add-DeviceTestCoverageHits {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Destination)

    $hits = Read-DeviceTestCoverageHits -Path $Path
    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        $existing = Read-DeviceTestCoverageHits -Path $Destination
        if ($existing.Length -ne $hits.Length) {
            throw "$Path has $($hits.Length) hit locations, but $Destination has $($existing.Length)."
        }

        for ($index = 0; $index -lt $hits.Length; $index++) {
            $hits[$index] += $existing[$index]
        }
    }

    $writer = New-Object System.IO.BinaryWriter([System.IO.File]::Create($Destination))
    try {
        $writer.Write([int]$hits.Length)
        foreach ($value in $hits) {
            $writer.Write([int]$value)
        }
    }
    finally {
        $writer.Dispose()
    }
}

function Complete-DeviceTestCoverage {
    param($Session)

    if ($null -eq $Session) {
        return
    }

    try {
        Write-DeviceTestHeader 'Coverage: collect the hits files and write the report'
        $assemblyNames = @($Session.Assemblies | ForEach-Object { $_.Name })
        $coverletFolder = [System.IO.Path]::GetTempPath()
        foreach ($entry in $Session.Projects) {
            $folder = Get-DeviceTestCoverageHitsDirectory -Context $Session.Context -Project $entry.Project
            $files = @()
            if (Test-Path -LiteralPath $folder -PathType Container) {
                $files = @(Get-ChildItem -LiteralPath $folder -File | Where-Object {
                        $_.Name -match $script:DeviceTestCoverageHitsFilePattern -and $assemblyNames -contains $Matches['assembly']
                    })
            }

            if ($files.Count -eq 0) {
                Write-DeviceTestWarning "Coverage: $($entry.Project.Name) wrote no hits files (in $folder), so its tests add nothing to the coverage report."
                continue
            }

            foreach ($file in $files) {
                Add-DeviceTestCoverageHits -Path $file.FullName -Destination (Join-Path $coverletFolder $file.Name)
            }

            Write-Host "$($entry.Project.Name): $($files.Count) hits file(s) ($(($files | ForEach-Object { $_.Name -replace '_[^_]+$', '' }) -join ', '))"
        }

        $stopped = Stop-DeviceTestCoverageProcess -Session $Session
        if (Test-Path -LiteralPath $Session.Log -PathType Leaf) {
            Get-Content -LiteralPath $Session.Log | ForEach-Object { Write-Host $_ }
        }

        if ($stopped -and (Test-Path -LiteralPath $Session.Report -PathType Leaf)) {
            $coverage = [xml](Get-Content -LiteralPath $Session.Report -Raw)
            $rate = [double]::Parse($coverage.coverage.'line-rate', [System.Globalization.CultureInfo]::InvariantCulture)
            Write-Host "Coverage report: $($Session.Report) ($($coverage.coverage.'lines-covered') of $($coverage.coverage.'lines-valid') lines, $([System.Math]::Round($rate * 100, 1))%)"
        }
        elseif (-not (Test-Path -LiteralPath $Session.Report -PathType Leaf)) {
            Write-DeviceTestWarning "Coverage: coverlet wrote no report ($($Session.Report)). Its log: $($Session.Log)"
        }
    }
    catch {
        Write-DeviceTestWarning "Coverage: $($_.Exception.Message)"
    }
    finally {
        Close-DeviceTestCoverage -Session $Session
    }
}
