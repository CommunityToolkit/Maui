<#
.SYNOPSIS
    Writes one code coverage report from the Cobertura files of the unit tests and the device tests, with ReportGenerator.

.DESCRIPTION
    The unit tests (Microsoft.Testing.Platform code coverage) and the device tests (coverlet, eng/devices/*.ps1 -Coverage) report
    the same source files from different tools, machines and target frameworks, under names that do not match. Each report is
    rewritten first, so that ReportGenerator merges them line by line:
    * Every file path becomes repository-relative (src/<Project>/...), whatever the checkout folder, the path separator or the
      /_/ prefix of a deterministic (CI) build.
    * Generated files (src/<Project>/obj/<Configuration>/<TargetFramework>/...) are merged across target frameworks under
      src/<Project>/obj/generated/, without the checkout path that MAUI's binding source generator puts in its file names.
    * Each source file becomes one class, named after its path, that lists each line once (the most hits, the best branch
      coverage). The tools name nested, generic and file-local classes differently, and ReportGenerator merges by class.
    A line then counts as covered when any test run covered it, and as coverable when any build of its file has it.

    The unit test coverage must come from a Debug build, as the device tests use: a Release build has fewer coverable lines (no
    sequence points on braces), so the lines covered only by unit tests would show as uncovered device test lines.

    Writes to -Output:
    * summary.md: the line and branch coverage of all assemblies together, then ReportGenerator's folded section of each assembly
      (alphabetical, labeled with its line and branch coverage), then, folded, the coverage of each input and the notes; in GitHub
      Actions it is also added to the job summary. Each line and branch coverage (totals, assemblies, classes) gets a flag on Google's
      scale (https://testing.googleblog.com/2020/08/code-coverage-best-practices.html): red below -Acceptable (needs work now), yellow
      from -Acceptable (acceptable, could be better), green from -Commendable (commendable), blue from -Exemplary (exemplary: more tests
      rarely pay off). Lines and branches are flagged separately, as tools such as Istanbul and lcov do.
    * report/: ReportGenerator's HTML report (index.html), Summary.txt, Summary.json and SummaryGithub.md.
    * inputs/: the rewritten Cobertura files.

.PARAMETER Report
    Cobertura files, or folders that are searched for *.cobertura.xml. Comma-separated values are accepted, because "pwsh -File" passes
    "-Report a,b" as a single string.

.PARAMETER Output
    The folder to write to; emptied first. Defaults to artifacts/coverage-report.

.PARAMETER Note
    Lines for the notes of summary.md, for example the test runs that collect no coverage.

.PARAMETER Acceptable
    The coverage (percent) from which summary.md flags yellow instead of red: Google's "acceptable". Defaults to 60.

.PARAMETER Commendable
    The coverage (percent) from which summary.md flags green instead of yellow: Google's "commendable". Defaults to 75.

.PARAMETER Exemplary
    The coverage (percent) from which summary.md flags blue instead of green: Google's "exemplary". Defaults to 90.

.EXAMPLE
    dotnet test --project src/CommunityToolkit.Maui.UnitTests -c Debug --coverage --coverage-output-format cobertura --coverage-output "$PWD/artifacts/coverage/unit-tests.cobertura.xml"
    pwsh eng/devices/ios.ps1 -Project All -Coverage
    pwsh eng/coverage-report.ps1 -Report artifacts/coverage,artifacts/device-tests/ios/coverage
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Report,

    [string]$Output = (Join-Path $PSScriptRoot '..' 'artifacts' 'coverage-report'),

    [string[]]$Note = @(),

    [double]$Acceptable = 60,

    [double]$Commendable = 75,

    [double]$Exemplary = 90
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

# The last 'src/CommunityToolkit.Maui<...>/' folder of a path starts its repository-relative part (the last one, because a checkout
# folder can have that name too: ~/src/CommunityToolkit.Maui/src/CommunityToolkit.Maui.Core/...).
$repositoryPathPattern = [regex]'^(?:.*/)?(src/CommunityToolkit\.Maui[^/]*/.*)$'
$generatedPathPattern = [regex]'^(src/[^/]+/obj)/[^/]+/net\d+\.\d+(?:-[a-z]+[\d.]*)?/(.*)$'
# MAUI's binding source generator names its files after the source file's full path ('-Users-me-repo-src-CommunityToolkit.Maui-...',
# 'D--a-Maui-Maui-src-CommunityToolkit.Maui-...'); greedy, because the checkout path can contain '-src-CommunityToolkit.Maui' too.
$checkoutInFileNamePattern = [regex]'(^|/)[^/]*-src-(CommunityToolkit\.Maui)'
$conditionCoveragePattern = [regex]'\((\d+)/(\d+)\)'

function ConvertTo-RepositoryPath {
    # Returns the repository-relative path of a Cobertura class's file name, or $null when it is not a toolkit source file.
    param([string]$FileName, [string[]]$Sources)

    $file = $FileName.Replace('\', '/')
    $candidates = @($Sources | ForEach-Object { $_.Replace('\', '/').TrimEnd('/') + '/' + $file.TrimStart('/') }) + $file
    foreach ($candidate in $candidates) {
        $match = $repositoryPathPattern.Match($candidate)
        if (-not $match.Success) {
            continue
        }

        $path = $match.Groups[1].Value
        $generated = $generatedPathPattern.Match($path)
        if ($generated.Success) {
            $path = "$($generated.Groups[1].Value)/generated/$($checkoutInFileNamePattern.Replace($generated.Groups[2].Value, '$1src-$2'))"
        }

        return $path
    }

    return $null
}

function ConvertTo-FileCobertura {
    # Rewrites a Cobertura file into one class per repository-relative source file and returns the line counts of each assembly.
    param([string]$Path, [string]$Destination)

    $document = New-Object System.Xml.XmlDocument
    $document.Load($Path)
    $sources = @($document.SelectNodes('/coverage/sources/source') | ForEach-Object { $_.InnerText.Trim() } | Where-Object { $_ })

    # package name -> file -> line number -> @(hits, isBranch, coveredConditions, conditions)
    $packages = [ordered]@{}
    $unmapped = New-Object System.Collections.Generic.HashSet[string]
    foreach ($package in $document.SelectNodes('/coverage/packages/package')) {
        $name = $package.GetAttribute('name')
        if (-not $packages.Contains($name)) {
            $packages[$name] = @{}
        }

        $files = $packages[$name]
        foreach ($class in $package.SelectNodes('classes/class')) {
            $file = ConvertTo-RepositoryPath -FileName $class.GetAttribute('filename') -Sources $sources
            if ($null -eq $file) {
                [void]$unmapped.Add($class.GetAttribute('filename'))
                continue
            }

            if (-not $files.ContainsKey($file)) {
                $files[$file] = @{}
            }

            $lines = $files[$file]
            # Only the class's own <lines>: the <lines> under <methods> repeat them.
            foreach ($line in $class.SelectNodes('lines/line')) {
                $number = [int]$line.GetAttribute('number')
                $hits = [long]::Parse($line.GetAttribute('hits'), $invariant)
                $isBranch = $line.GetAttribute('branch') -eq 'true'
                $covered = 0
                $conditions = 0
                $condition = $conditionCoveragePattern.Match($line.GetAttribute('condition-coverage'))
                if ($condition.Success) {
                    $covered = [int]$condition.Groups[1].Value
                    $conditions = [int]$condition.Groups[2].Value
                }

                $existing = $lines[$number]
                if ($null -eq $existing) {
                    $lines[$number] = @($hits, $isBranch, $covered, $conditions)
                    continue
                }

                $existing[0] = [System.Math]::Max($existing[0], $hits)
                $existing[1] = $existing[1] -or $isBranch
                if ($conditions -gt 0 -and ($existing[3] -eq 0 -or ($covered / $conditions) -gt ($existing[2] / $existing[3]))) {
                    $existing[2] = $covered
                    $existing[3] = $conditions
                }
            }
        }
    }

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $writer = [System.Xml.XmlWriter]::Create($Destination, $settings)
    $totals = New-Object System.Collections.Generic.List[object]
    try {
        $writer.WriteStartElement('coverage')
        $writer.WriteAttributeString('version', '1.9')
        $writer.WriteAttributeString('timestamp', $document.DocumentElement.GetAttribute('timestamp'))
        $writer.WriteStartElement('packages')
        foreach ($packageName in $packages.Keys) {
            $total = New-Object PSObject -Property @{ Assembly = $packageName; Files = 0; Lines = 0; Covered = 0 }
            $totals.Add($total)
            $writer.WriteStartElement('package')
            $writer.WriteAttributeString('name', $packageName)
            $writer.WriteStartElement('classes')
            foreach ($file in ($packages[$packageName].Keys | Sort-Object)) {
                # ReportGenerator reads '/', '+' and '$' in class names as nesting: the class is the path with dots.
                $className = ($file -replace '^src/', '' -replace '\.cs$', '').Replace('/', '.').Replace('+', '.').Replace('$', '.')
                $writer.WriteStartElement('class')
                $writer.WriteAttributeString('name', $className)
                $writer.WriteAttributeString('filename', $file)
                $writer.WriteStartElement('methods')
                $writer.WriteEndElement()
                $writer.WriteStartElement('lines')
                $lines = $packages[$packageName][$file]
                foreach ($number in ($lines.Keys | Sort-Object)) {
                    $line = $lines[$number]
                    $writer.WriteStartElement('line')
                    $writer.WriteAttributeString('number', $number.ToString($invariant))
                    $writer.WriteAttributeString('hits', $line[0].ToString($invariant))
                    if ($line[1] -and $line[3] -gt 0) {
                        $writer.WriteAttributeString('branch', 'true')
                        $writer.WriteAttributeString('condition-coverage', "$([int][System.Math]::Round(100 * $line[2] / $line[3]))% ($($line[2])/$($line[3]))")
                    }
                    else {
                        $writer.WriteAttributeString('branch', 'false')
                    }

                    $writer.WriteEndElement()
                    $total.Lines++
                    if ($line[0] -gt 0) {
                        $total.Covered++
                    }
                }

                $writer.WriteEndElement()
                $writer.WriteEndElement()
                $total.Files++
            }

            $writer.WriteEndElement()
            $writer.WriteEndElement()
        }

        $writer.WriteEndElement()
        $writer.WriteEndElement()
    }
    finally {
        $writer.Dispose()
    }

    if ($unmapped.Count -gt 0) {
        Write-Warning "$($Path): $($unmapped.Count) file(s) outside src/CommunityToolkit.Maui*/ left out, for example $(@($unmapped)[0])"
    }

    return , $totals.ToArray()
}

function Format-Percent {
    param([long]$Part, [long]$Whole)

    if ($Whole -eq 0) {
        return 'n/a'
    }

    return ([System.Math]::Round(100.0 * $Part / $Whole, 1)).ToString($invariant) + '%'
}

$reportFiles = New-Object System.Collections.Generic.List[object]
foreach ($item in ($Report | ForEach-Object { $_ -split ',' } | Where-Object { $_.Trim() })) {
    $full = [System.IO.Path]::GetFullPath($item.Trim(), (Get-Location).Path)
    if (Test-Path -LiteralPath $full -PathType Container) {
        foreach ($file in (Get-ChildItem -LiteralPath $full -Recurse -File -Filter '*.cobertura.xml' | Sort-Object FullName)) {
            $reportFiles.Add((New-Object PSObject -Property @{ Path = $file.FullName; Label = ($file.Name -replace '\.cobertura\.xml$', '') }))
        }
    }
    elseif (Test-Path -LiteralPath $full -PathType Leaf) {
        $reportFiles.Add((New-Object PSObject -Property @{ Path = $full; Label = ([System.IO.Path]::GetFileName($full) -replace '\.cobertura\.xml$', '') }))
    }
    else {
        Write-Warning "No coverage report at $item."
    }
}

# A report is named after its file; reports with the same file name are told apart by their path.
foreach ($group in ($reportFiles | Group-Object Label | Where-Object { $_.Count -gt 1 })) {
    foreach ($reportFile in $group.Group) {
        $reportFile.Label = [System.IO.Path]::GetRelativePath((Get-Location).Path, $reportFile.Path).Replace('\', '/') -replace '\.cobertura\.xml$', ''
    }
}

$Output = [System.IO.Path]::GetFullPath($Output, (Get-Location).Path)
if (Test-Path -LiteralPath $Output) {
    Remove-Item -LiteralPath $Output -Recurse -Force
}

$inputFolder = Join-Path $Output 'inputs'
$reportFolder = Join-Path $Output 'report'
New-Item -ItemType Directory -Force -Path $inputFolder, $reportFolder | Out-Null

$summary = New-Object System.Collections.Generic.List[string]
# The coverage of each input: the last, folded part of the summary.
$sources = New-Object System.Collections.Generic.List[string]

if ($reportFiles.Count -eq 0) {
    $summary.Add('## Code coverage')
    $summary.Add('')
    $summary.Add('No coverage report was found, so there is nothing to merge.')
}
else {
    $sources.Add('| Coverage report | Assemblies it covers (CommunityToolkit.*) | Files | Covered lines | Coverable lines | Line coverage |')
    $sources.Add('|---|---|---:|---:|---:|---:|')
    $rewritten = New-Object System.Collections.Generic.List[string]
    $index = 0
    foreach ($reportFile in $reportFiles) {
        $index++
        $destination = Join-Path $inputFolder ('{0:00}-{1}.cobertura.xml' -f $index, ($reportFile.Label -replace '[^A-Za-z0-9.-]+', '-'))
        $assemblies = ConvertTo-FileCobertura -Path $reportFile.Path -Destination $destination
        $rewritten.Add($destination)
        # The assemblies a report covers nothing of were only loaded by its tests (the analyzer tests load the toolkit assemblies, for
        # example): they still count in the merged report, but not in the report's row.
        $tested = @($assemblies | Where-Object { $_.Covered -gt 0 })
        $files = ($tested | Measure-Object -Property Files -Sum).Sum
        $covered = ($tested | Measure-Object -Property Covered -Sum).Sum
        $coverable = ($tested | Measure-Object -Property Lines -Sum).Sum
        $sources.Add("| ``$($reportFile.Label)`` | $(($tested | ForEach-Object { $_.Assembly -replace '^CommunityToolkit\.', '' } | Sort-Object) -join ', ') | $files | $covered | $coverable | $(Format-Percent $covered $coverable) |")
        foreach ($assembly in $assemblies) {
            Write-Host "$($reportFile.Label): $($assembly.Assembly): $($assembly.Covered) of $($assembly.Lines) lines in $($assembly.Files) files"
        }
    }

    Push-Location $repositoryRoot
    try {
        & dotnet tool restore | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool restore failed with exit code $LASTEXITCODE."
        }

        # The log lists every generated file (obj/generated/...) as missing: they are not in the repository, so the HTML report has no
        # source for them.
        $log = Join-Path $Output 'reportgenerator.log'
        $arguments = @('reportgenerator', "-reports:$($rewritten -join ';')", "-targetdir:$reportFolder", '-reporttypes:Html;MarkdownSummaryGithub;TextSummary;JsonSummary',
            "-sourcedirs:$repositoryRoot", '-title:CommunityToolkit.Maui')
        & dotnet @arguments *> $log
        if ($LASTEXITCODE -ne 0) {
            Get-Content -LiteralPath $log -Tail 30 | ForEach-Object { Write-Host $_ }
            throw "ReportGenerator failed with exit code $LASTEXITCODE (log: $log)."
        }
    }
    finally {
        Pop-Location
    }

    $merged = Get-Content -LiteralPath (Join-Path $reportFolder 'Summary.json') -Raw | ConvertFrom-Json
    $total = $merged.summary
    $assemblies = @{}
    foreach ($assembly in $merged.coverage.assemblies) {
        $assemblies[$assembly.name] = $assembly
    }

    # ReportGenerator's percentages, so that the labels match the tables inside the sections and the HTML report.
    function Format-Coverage([object]$Percent) { if ($null -eq $Percent) { 'n/a' } else { ([double]$Percent).ToString($invariant) + '%' } }
    function Format-Count([long]$Count) { $Count.ToString('N0', $invariant) }

    # The flag of one line or branch coverage on Google's scale (no value: nothing to cover, no flag).
    $blue, $green, $yellow, $red = 0x1F535, 0x1F7E2, 0x1F7E1, 0x1F534 | ForEach-Object { [char]::ConvertFromUtf32($_) }
    function Get-CoverageFlag([object]$Percent) {
        if ($null -eq $Percent -or "$Percent" -eq '') { return '' }
        $value = [double]$Percent
        if ($value -ge $Exemplary) { $blue } elseif ($value -ge $Commendable) { $green } elseif ($value -ge $Acceptable) { $yellow } else { $red }
    }
    function Format-Flagged([object]$Percent) { "$(Get-CoverageFlag $Percent) $(Format-Coverage $Percent)" }
    $acceptableText, $commendableText, $exemplaryText = $Acceptable, $Commendable, $Exemplary | ForEach-Object { $_.ToString($invariant) + '%' }

    # First what matters most: the line and branch coverage of every assembly together.
    $summary.Add("## Code coverage: $(Format-Flagged $total.linecoverage) of lines, $(Format-Flagged $total.branchcoverage) of branches")
    $summary.Add('')
    $summary.Add("All $($total.assemblies) toolkit assemblies, unit tests and device tests together: $(Format-Count $total.coveredlines) of $(Format-Count $total.coverablelines) lines and $(Format-Count $total.coveredbranches) of $(Format-Count $total.totalbranches) branches.")
    $summary.Add('')
    $summary.Add("$red below ${acceptableText}: needs work now · $yellow $acceptableText to ${commendableText}: acceptable, could be better · $green $commendableText to ${exemplaryText}: commendable · $blue $exemplaryText or more: exemplary, more tests rarely pay off. The scale of [Google's code coverage guidelines](https://testing.googleblog.com/2020/08/code-coverage-best-practices.html), for lines and branches each.")
    $summary.Add('')

    # Then ReportGenerator's section of each assembly, folded and alphabetical, with the branch coverage in its label too. Its summary table
    # above them repeats the totals (and the reports are merged per file, so they have no method coverage): it is left out.
    $inAssemblies = $false
    foreach ($line in (Get-Content -LiteralPath (Join-Path $reportFolder 'SummaryGithub.md'))) {
        if (-not $inAssemblies) {
            $inAssemblies = $line -eq '## Coverage'
            continue
        }

        $label = [regex]::Match($line, '^<details><summary>(?<name>.+) - [\d.]+%</summary>$')
        if ($label.Success -and $assemblies.ContainsKey($label.Groups['name'].Value)) {
            $assembly = $assemblies[$label.Groups['name'].Value]
            $branches = if ($assembly.totalbranches -gt 0) { "$(Format-Flagged $assembly.branchcoverage) of branches" } else { 'no branches' }
            $line = "<details><summary>$($assembly.name): $(Format-Flagged $assembly.coverage) of lines, $branches</summary>"
        }
        else {
            # A row of an assembly's table (|name|line%|branch%|, the assembly's own row in bold): a flag before each coverage.
            $row = [regex]::Match($line, '^\|(?<name>[^|]+)\|(?<line>\**(?<linevalue>[\d.]+)%\**)\|(?<branch>(?:\**(?<branchvalue>[\d.]+)%\**)?)\|$')
            if ($row.Success) {
                $branchCell = if ($row.Groups['branchvalue'].Success) { "$(Get-CoverageFlag $row.Groups['branchvalue'].Value) $($row.Groups['branch'].Value)" } else { '' }
                $line = "|$($row.Groups['name'].Value)|$(Get-CoverageFlag $row.Groups['linevalue'].Value) $($row.Groups['line'].Value)|$branchCell|"
            }
        }

        $summary.Add($line)
    }

    # Last, folded: where the numbers come from.
    $summary.Add('')
    $summary.Add('<details><summary>How this report is made</summary>')
    $summary.Add('')
    $summary.AddRange($sources)
    $summary.Add('')
    $summary.Add('A line counts as covered when any test run above covered it, and as coverable when any build of its file has it (each report covers the files its target framework compiles). A row counts only the assemblies its tests cover; the merged report counts every assembly of every report. The full report, down to each line, is the HTML report of the code-coverage-report artifact (report/index.html).')
    foreach ($line in $Note) {
        $summary.Add('')
        $summary.Add($line)
    }

    $summary.Add('')
    $summary.Add('</details>')
}

$text = ($summary -join "`n") + "`n"
$encoding = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $Output 'summary.md'), $text, $encoding)
if ($env:GITHUB_STEP_SUMMARY) {
    [System.IO.File]::AppendAllText($env:GITHUB_STEP_SUMMARY, $text, $encoding)
}

if (Test-Path -LiteralPath (Join-Path $reportFolder 'Summary.txt')) {
    Get-Content -LiteralPath (Join-Path $reportFolder 'Summary.txt') | Select-Object -First 14 | ForEach-Object { Write-Host $_ }
}

Write-Host "Coverage report: $(Join-Path $reportFolder 'index.html'); summary: $(Join-Path $Output 'summary.md')"
