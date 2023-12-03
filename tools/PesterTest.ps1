using namespace System.IO

<#
.SYNOPSIS
Run Pester test

.PARAMETER TestPath
The path to the tests to run

.PARAMETER OutputFile
The path to write the Pester test results to.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [String]
    $TestPath,

    [Parameter(Mandatory)]
    [String]
    $OutputFile
)

$ErrorActionPreference = 'Stop'


$manifestPath = [Path]::Combine($PSScriptRoot, "..", "manifest.psd1")
$manifest = Import-PowerShellDataFile -Path $manifestPath
$testModules = @(
    'Pester'
    $manifest.TestRequirements.ModuleName
)

$modPath = [Path]::Combine($PSScriptRoot, "..", "output", "Modules")
Get-ChildItem -LiteralPath $modPath -Directory | ForEach-Object {
    if ($_.Name -in $testModules) {
        Write-Host "Importing $_"
        Import-Module -Name $_.FullName
    }
}

[PSCustomObject]$PSVersionTable |
    Select-Object -Property *, @{N = 'Architecture'; E = {
            switch ([IntPtr]::Size) {
                4 { 'x86' }
                8 { 'x64' }
                default { 'Unknown' }
            }
        }
    } |
    Format-List |
    Out-Host

$configuration = [PesterConfiguration]::Default
$configuration.Output.Verbosity = 'Detailed'
$configuration.Run.Exit = $true
$configuration.Run.Path = $TestPath
$configuration.TestResult.Enabled = $true
$configuration.TestResult.OutputPath = $OutputFile
$configuration.TestResult.OutputFormat = 'NUnitXml'

Invoke-Pester -Configuration $configuration -WarningAction Ignore
