using namespace System.IO
using namespace System.Management.Automation
using namespace System.Management.Automation.Runspaces
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

Function Global:Invoke-WithTestEtwProvider {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, Position = 0)]
        [ScriptBlock]
        $ScriptBlock
    )

    $etwAssembly = [Path]::Combine($PSScriptRoot, 'PSEtwProvider', 'bin', 'Release', 'netstandard2.0', 'publish', 'PSEtwProvider.dll')

    $params = @{
        FilePath = (Get-Process -Id $pid).Path
        WindowStyle = 'Hidden'
        WorkingDirectory = $pwd.Path
        PassThru = $true
    }
    $proc = Start-Process @params
    try {
        $connInfo = [NamedPipeConnectionInfo]::new($proc.Id)
        $rs = [RunspaceFactory]::CreateRunspace($connInfo)
        $rs.Open()

        $ps = [PowerShell]::Create()
        $ps.Runspace = $rs
        [void]$ps.AddCommand("Add-Type").AddParameter("LiteralPath", $etwAssembly).AddStatement()
        [void]$ps.AddScript(@'
param ([Parameter(Mandatory)][string]$ScriptBlock)
$logger = [PSEtwProvider.PSEtwEvent]::new()
try {
    . ([ScriptBlock]::Create($ScriptBlock))
}
finally {
    $logger.Dispose()
}
'@)
        [void]$ps.AddParameter('ScriptBlock', $ScriptBlock)
        [void]$ps.Invoke()
    }
    catch {
        $PSCmdlet.WriteError($_)
    }
    finally {
        $proc | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}
