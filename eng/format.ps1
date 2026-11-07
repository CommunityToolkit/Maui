<#
.SYNOPSIS
    Formats the repository with dotnet format, without letting it add [Obsolete] attributes.

.DESCRIPTION
    Runs 'dotnet format' on the samples solution, which includes every project in this repository.

    CS0612, CS0618, CS0672, CS1062 and CS1064 are excluded because, for these compiler warnings, dotnet format applies
    Roslyn's "Add [Obsolete]" code fix: it adds the [Obsolete] attribute to every member that uses (CS0612, CS0618,
    CS1062, CS1064) or overrides (CS0672) an obsolete API (https://github.com/dotnet/format/issues/1230). The exclusion
    only affects dotnet format; the compiler still reports these warnings when building.

    The daily format workflow (.github/workflows/dotnet-format-daily.yml) runs this script, so the exclusion list lives
    in one place.

.PARAMETER Target
    The solution or project to format. Defaults to samples/CommunityToolkit.Maui.Sample.slnx.

.PARAMETER Verify
    Changes nothing and fails when a file needs formatting (dotnet format --verify-no-changes).

.EXAMPLE
    pwsh eng/format.ps1

.EXAMPLE
    pwsh eng/format.ps1 -Verify

.EXAMPLE
    pwsh eng/format.ps1 -Target src/CommunityToolkit.Maui/CommunityToolkit.Maui.csproj --verbosity diagnostic
    Arguments the script does not know go to dotnet format unchanged.
#>
[CmdletBinding()]
param(
    [string] $Target = (Join-Path $PSScriptRoot '..' 'samples' 'CommunityToolkit.Maui.Sample.slnx'),
    [switch] $Verify,
    [Parameter(ValueFromRemainingArguments)] [string[]] $FormatArguments = @()
)

$ErrorActionPreference = 'Stop'

$excludedDiagnostics = @('CS0612', 'CS0618', 'CS0672', 'CS1062', 'CS1064')

$arguments = @('format', $Target, '--exclude-diagnostics') + $excludedDiagnostics
if ($Verify) {
    $arguments += '--verify-no-changes'
}
$arguments += $FormatArguments

Write-Host "dotnet $($arguments -join ' ')"
& dotnet @arguments
exit $LASTEXITCODE
