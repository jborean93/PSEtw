using namespace System.IO
using namespace System.Runtime.InteropServices

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
    $Task = 'Build',

    [Parameter()]
    [Version]
    $PowerShellVersion = $PSVersionTable.PSVersion,

    [Parameter()]
    [Architecture]
    $PowerShellArch = [RuntimeInformation]::ProcessArchitecture,

    [Parameter()]
    [string]
    $ModuleNupkg
)

$ErrorActionPreference = 'Stop'

. ([Path]::Combine($PSScriptRoot, "tools", "common.ps1"))

$manifestPath = ([Path]::Combine($PSScriptRoot, 'manifest.psd1'))
$Manifest = [Manifest]::new($Configuration, $PowerShellVersion, $PowerShellArch, $manifestPath)

if ($ModuleNupkg) {
    Write-Host "Expanding module nupkg to '$($Manifest.ReleasePath)'" -ForegroundColor Cyan
    Expand-Nupkg -Path $ModuleNupkg -DestinationPath $Manifest.ReleasePath
}

Write-Host "Installing PowerShell dependencies" -ForegroundColor Cyan
$deps = $Task -eq 'Build' ? $Manifest.BuildRequirements : $Manifest.TestRequirements
$deps | Install-BuildDependencies

if ($Task -eq 'Test') {
    # This is a special step for PSEtw to setup an ETW provider that can be
    # used for testing. This compiles the dll and generates the manifest files
    # needed to register the provider with wevtutil.exe.
    Write-Host "Compiling test ETW Provider" -ForegroundColor Cyan

    $projectRoot = [Path]::Combine($PSScriptRoot, 'tests', 'PSEtwProvider')
    $binPath = [Path]::Combine($projectRoot, 'bin')
    if (Test-Path -LiteralPath $binPath) {
        Remove-Item -LiteralPath $binPath -Force -Recurse
    }
    $buildArgs = @(
        'publish'
        '--framework', 'netstandard2.0'
        '--configuration', 'Release'
        [Path]::Combine($projectRoot, 'PSEtwProvider.csproj')
    )
    dotnet @buildArgs
    if ($LASTEXITCODE) {
        throw "Failed to compile PSEtwProvider for testing"
    }
}

$buildScript = [Path]::Combine($PSScriptRoot, "tools", "InvokeBuild.ps1")
$invokeBuildSplat = @{
    Task = $Task
    File = $buildScript
    Manifest = $manifest
}
Invoke-Build @invokeBuildSplat
