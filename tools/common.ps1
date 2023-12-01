using namespace System.Collections
using namespace System.Collections.Generic
using namespace System.IO
using namespace System.Management.Automation
using namespace System.Net
using namespace System.Net.Http

#Requires -Version 7.2

class Manifest {
    [PSModuleInfo]$Module

    [string]$DotnetProject
    [string]$InvokeBuildVersion
    [Hashtable[]]$BuildRequirements
    [Hashtable[]]$TestRequirements

    [string]$DotnetRoot
    [string]$OutputRoot
    [string]$PowerShellRoot

    [string[]]$TargetFrameworks

    Manifest([string]$Path) {
        $moduleManifestParams = @{
            Path = [Path]::Combine($PSScriptRoot, "..", "module", "*.psd1")
            # Can emit errors about invalid RootModule which don't matter here
            ErrorAction = 'Ignore'
            WarningAction = 'Ignore'
        }
        $this.Module = Test-ModuleManifest @moduleManifestParams

        $raw = Import-PowerShellDataFile -LiteralPath $Path
        $this.DotnetProject = $raw.DotnetProject ?? $this.Module.Name
        $this.InvokeBuildVersion = $raw.InvokeBuildVersion
        $this.BuildRequirements = $raw.BuildRequirements ?? @()
        $this.TestRequirements = $raw.TestRequirements ?? @()

        $this.DotnetRoot = [Path]::GetFullPath(
            [Path]::Combine($PSScriptRoot, "..", "src", $this.DotnetProject))
        $this.OutputRoot = [Path]::GetFullPath(
            [Path]::Combine($PSScriptRoot, "..", "output"))
        $this.PowerShellRoot = [Path]::GetFullPath(
            [Path]::Combine($PSScriptRoot, "..", "module"))

        $csProjPath = [Path]::Combine($this.DotnetProject, "*.csproj")
        [xml]$csharpProjectInfo = Get-Content $csProjPath
        $this.TargetFrameworks = @(
            @($csharpProjectInfo.Project.PropertyGroup)[0].TargetFrameworks.Split(
                ';', [StringSplitOptions]::RemoveEmptyEntries)
        )
    }
}

Function Assert-ModuleFast {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Version = 'latest'
    )

    $versionPath = if ($Version -eq 'latest') {
        'latest/download'
    }
    else {
        "download/$Version"
    }

    $ModuleName = 'ModuleFast'
    $Uri = "https://github.com/JustinGrote/$ModuleName/releases/$versionPath/$ModuleName.psm1"

    if (Get-Module $ModuleName) {
        Write-Warning "Module $ModuleName already loaded, skipping bootstrap."
        return
    }

    try {
        $httpClient = [HttpClient]::new()
        $httpClient.DefaultRequestHeaders.AcceptEncoding.Add('gzip')
        $response = $httpClient.GetStringAsync($Uri).GetAwaiter().GetResult()
    }
    catch {
        $PSItem.ErrorDetails = "Failed to fetch $ModuleName from $Uri`: $PSItem"
        $PSCmdlet.ThrowTerminatingError($PSItem)
    }

    $scriptBlock = [ScriptBlock]::Create($response)

    New-Module -Name $ModuleName -ScriptBlock $scriptblock | Import-Module
}

Function Assert-PowerShell {
    [OutputType([string])]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Version
    )

    $targetFolder = $PSCmdlet.GetUnresolvedProviderPathFromPSPath(
        [Path]::Combine($PSScriptRoot, "..", "output", "PowerShell-$Version"))
    $pwshExe = [Path]::Combine($targetFolder, "pwsh$nativeExt")

    if (Test-Path -LiteralPath $pwshExe) {
        return
    }

    if ($IsWindows) {
        $releasePath = "PowerShell-$Version-win-x64.zip"
        $fileName = "pwsh-$Version.zip"
        $nativeExt = ".exe"
    }
    else {
        $releasePath = "powershell-$Version-linux-x64.tar.gz"
        $fileName = "pwsh-$Version.tar.gz"
        $nativeExt = ""
    }
    $downloadUrl = "https://github.com/PowerShell/PowerShell/releases/download/v$Version/$releasePath"
    $downloadArchive = [Path]::Combine($targetFolder, $fileName)

    if (-not (Test-Path -LiteralPath $targetFolder)) {
        New-Item $targetFolder -ItemType Directory -Force | Out-Null
    }

    if (-not (Test-Path -LiteralPath $downloadArchive)) {
        Invoke-WebRequest -UseBasicParsing -Uri $downloadUrl -OutFile $downloadArchive
    }

    if ($IsWindows) {
        $oldPreference = $global:ProgressPreference
        try {
            $global:ProgressPreference = 'SilentlyContinue'
            Expand-Archive -LiteralPath $downloadArchive -DestinationPath $targetFolder
        }
        finally {
            $global:ProgressPreference = $oldPreference
        }
    }
    else {
        tar -xf $downloadArchive --directory $targetFolder
        if ($LASTEXITCODE) {
            $err = [ErrorRecord]::new(
                [Exception]::new("Failed to extract pwsh tar for $Version"),
                "FailedToExtractTar",
                [ErrorCategory]::NotSpecified,
                $null
            )
            $PSCmdlet.ThrowTerminatingError($err)
        }

        chmod +x $pwshExe
        if ($LASTEXITCODE) {
            $err = [ErrorRecord]::new(
                [Exception]::new("Failed to set pwsh as executable at '$pwshExe'"),
                "FailedToSetPwshExecutable",
                [ErrorCategory]::NotSpecified,
                $null
            )
            $PSCmdlet.ThrowTerminatingError($err)
        }
    }

    $pwshExe
}

Function Install-BuildDependencies {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [IDictionary[]]
        $Requirements
    )

    begin {
        $modules = [List[IDictionary]]::new()
    }
    process {
        $modules.AddRange($Requirements)
    }
    end {
        if (-not $modules) {
            return
        }

        Assert-ModuleFast -Version v0.0.1

        $modulePath = ([Path]::Combine($PSScriptRoot, "..", "output", "Modules"))
        $installParams = @{
            ModulesToInstall = $modules
            Destination = $modulePath
            NoPSModulePathUpdate = $true
            NoProfileUpdate = $true
            Update = $true
        }
        if (-not (Test-Path -LiteralPath $installParams.Destination)) {
            New-Item -Path $installParams.Destination -ItemType Directory -Force | Out-Null
        }
        Install-ModuleFast @installParams

        Get-ChildItem -LiteralPath $modulePath -Directory |
            ForEach-Object { Import-Module -Name $_.FullName }
    }
}
