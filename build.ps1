using namespace System.IO

#Requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Build', 'Test')]
    [string]
    $Task = 'Build'
)

$ErrorActionPreference = 'Stop'

. ([Path]::Combine($PSScriptRoot, "tools", "common.ps1"))

$manifestPath = ([Path]::Combine($PSScriptRoot, 'manifest.psd1'))
$Manifest = [Manifest]::new($manifestPath)

Write-Host "Installing build dependencies" -ForegroundColor Cyan
@(
    @{
        ModuleName = 'InvokeBuild'
        RequiredVersion = $Manifest.InvokeBuildVersion
    }
    $Task -eq 'Build' ? $Manifest.BuildRequirements : $Manifest.TestRequirements
) | Install-BuildDependencies

if ($Task -eq 'Build') {
    # Install coverlet
}

$buildScript = [Path]::Combine($PSScriptRoot, "tools", "InvokeBuild.ps1")
$invokeBuildSplat = @{
    Task = $Task
    File = $buildScript
    Manifest = $manifest
    Configuration = $Configuration
}
Invoke-Build @invokeBuildSplat
