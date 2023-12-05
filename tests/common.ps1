using namespace System.IO
using namespace System.Security.Principal

$moduleName = (Get-Item ([IO.Path]::Combine($PSScriptRoot, '..', 'module', '*.psd1'))).BaseName
$manifestPath = [IO.Path]::Combine($PSScriptRoot, '..', 'output', $moduleName)

if (-not (Get-Module -Name $moduleName -ErrorAction SilentlyContinue)) {
    Import-Module $manifestPath
}

if (-not (Get-Variable IsWindows -ErrorAction SilentlyContinue)) {
    # Running WinPS so guaranteed to be Windows.
    Set-Variable -Name IsWindows -Value $true -Scope Global
}

$global:IsAdmin = ([WindowsPrincipal][WindowsIdentity]::GetCurrent()).IsInRole([WindowsBuiltInRole]::Administrator)

Function Global:Complete {
    [OutputType([System.Management.Automation.CompletionResult])]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]
        $Expression
    )

    [System.Management.Automation.CommandCompletion]::CompleteInput(
        $Expression,
        $Expression.Length,
        $null).CompletionMatches
}

Function Global:Install-TestEtwProvider {
    $etwOutput = [Path]::Combine($PSScriptRoot, 'PSEtwProvider', 'bin', 'Release', 'netstandard2.0')

    Get-ChildItem -Path $etwOutput -File -Filter *.etwManifest.man | ForEach-Object {
        $assemblyPath = $_.FullName -replace '\.man$', '.dll'

        wevtutil.exe install-manifest $_.FullName /rf:$assemblyPath /mf:$assemblyPath
        if ($LASTEXITCODE) {
            throw "Failed to install ETW manifest '$($_.FullName)': $LASTEXITCODE"
        }
    }
}

Function Global:Uninstall-TestEtwProvider {
    $etwOutput = [Path]::Combine($PSScriptRoot, 'PSEtwProvider', 'bin', 'Release', 'netstandard2.0')
    Get-ChildItem -Path $etwOutput -File -Filter *.etwManifest.man | ForEach-Object {

        wevtutil.exe uninstall-manifest $_.FullName
        if ($LASTEXITCODE) {
            throw "Failed to uninstall ETW manifest '$($_.FullName)': $LASTEXITCODE"
        }
    }

}
