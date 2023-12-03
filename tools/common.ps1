using namespace System.Collections
using namespace System.Collections.Generic
using namespace System.IO
using namespace System.Management.Automation
using namespace System.Net
using namespace System.Net.Http
using namespace System.Runtime.InteropServices

#Requires -Version 7.2

class Manifest {
    [PSModuleInfo]$Module

    [ValidateSet("Debug", "Release")]
    [string]$Configuration

    [string]$DocsPath
    [string]$DotnetPath
    [string]$OutputPath
    [string]$PowerShellPath
    [string]$ReleasePath
    [string]$TestPath
    [string]$TestResultsPath

    [string]$DotnetProject
    [Hashtable[]]$BuildRequirements
    [Hashtable[]]$TestRequirements
    [Version]$PowerShellVersion
    [Architecture]$PowerShellArch
    [string[]]$TargetFrameworks
    [string]$TestFramework

    Manifest(
        [string]$Configuration,
        [Version]$PowerShellVersion,
        [Architecture]$PowerShellArch,
        [string]$ManifestPath
    ) {
        $moduleManifestParams = @{
            Path = [Path]::Combine($PSScriptRoot, "..", "module", "*.psd1")
            # Can emit errors about invalid RootModule which don't matter here
            ErrorAction = 'Ignore'
            WarningAction = 'Ignore'
        }
        $this.Module = Test-ModuleManifest @moduleManifestParams

        $this.Configuration = $Configuration

        $raw = Import-PowerShellDataFile -LiteralPath $ManifestPath
        $this.DotnetProject = $raw.DotnetProject ?? $this.Module.Name

        $this.DocsPath = [Path]::GetFullPath(
            [Path]::Combine($PSScriptRoot, "..", "docs"))
        $this.DotnetPath = [Path]::GetFullPath(
            [Path]::Combine($PSScriptRoot, "..", "src", $this.DotnetProject))
        $this.OutputPath = [Path]::GetFullPath(
            [Path]::Combine($PSScriptRoot, "..", "output"))
        $this.PowerShellPath = [Path]::GetFullPath(
            [Path]::Combine($PSScriptRoot, "..", "module"))
        $this.ReleasePath = [Path]::GetFullPath(
            [Path]::Combine($this.OutputPath, $this.Module.Name, $this.Module.Version))
        $this.TestPath = [Path]::GetFullPath(
            [Path]::Combine($PSScriptRoot, "..", "tests"))
        $this.TestResultsPath = [Path]::GetFullPath(
            [Path]::Combine($this.OutputPath, "TestResults"))

        if (-not (Test-Path -LiteralPath $this.ReleasePath)) {
            New-Item -Path $this.ReleasePath -ItemType Directory -Force | Out-Null
        }

        if (-not (Test-Path -LiteralPath $this.TestResultsPath)) {
            New-Item -Path $this.TestResultsPath -ItemType Directory -Force | Out-Null
        }

        $invokeBuildReq = @{
            ModuleName = 'InvokeBuild'
            RequiredVersion = $raw.InvokeBuildVersion
        }
        $pesterReq = @{
            ModuleName = 'Pester'
            RequiredVersion = $raw.PesterVersion
        }
        $this.BuildRequirements = @(
            $invokeBuildReq
            $raw.BuildRequirements
        )
        $this.TestRequirements = @(
            $invokeBuildReq
            $pesterReq
            $raw.TestRequirements
        )

        if ($PowerShellVersion.Major -lt 6) {
            $this.PowerShellVersion = "5.1"
        }
        else {
            $build = $PowerShellVersion.Build
            if ($build -eq -1) {
                $build = 0
            }
            $this.PowerShellVersion = "$($PowerShellVersion.Major).$($PowerShellVersion.Minor).$build"
        }
        $this.PowerShellArch = $PowerShellArch

        $csProjPath = [Path]::Combine($this.DotnetPath, "*.csproj")
        [xml]$csharpProjectInfo = Get-Content $csProjPath
        $this.TargetFrameworks = @(
            @($csharpProjectInfo.Project.PropertyGroup)[0].TargetFrameworks.Split(
                ';', [StringSplitOptions]::RemoveEmptyEntries)
        )

        $availableFrameworks = @(
            if ($this.PowerShellVersion -eq '5.1') {
                'net48'
                foreach ($minor in '7', '6', '5') {
                    foreach ($build in '2', '1', '') {
                        "net4$minor$build"
                    }
                }
            }
            else {
                # Minor releases + 4 correspond to the highest framework
                # available. e.g. 7.1 runs on net5.0 or lower, 7.2, on net6.0
                # or lower, etc.
                $netFrameworks = @(
                    for ($i = 5; $i -le $this.PowerShellVersion.Minor + 4; $i++) {
                        "net$i.0"
                    }
                )
                [Array]::Reverse($netFrameworks)

                $netFrameworks
                'netstandard2.1'
            }

            # WinPS and PS are compatible with netstandard to 2.0
            '2.0', '1.6', '1.5', '1.4', '1.3', '1.2', '1.1', '1.0' |
                ForEach-Object { "netstandard$_" }
        )

        foreach ($framework in $availableFrameworks) {
            if ($framework -in $this.TargetFrameworks) {
                $this.TestFramework = $framework
                break
            }
        }

        if (-not $this.TestFramework) {
            throw "No available target framework for PowerShell '$($this.PowerShellVersion)'"
        }
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
        [Version]$Version,

        [Parameter()]
        [Architecture]
        $Arch = [RuntimeInformation]::ProcessArchitecture
    )

    $releaseArch = switch ($Arch) {
        X64 { 'x64' }
        X86 { 'x86' }
        default {
            $err = [ErrorRecord]::new(
                [Exception]::new("Unsupported archecture requests '$_'"),
                "UnknownArch",
                [ErrorCategory]::InvalidArgument,
                $_
            )
            $PSCmdlet.ThrowTerminatingError($err)
        }
    }

    $osArch = [RuntimeInformation]::OSArchitecture
    $procArch = [RuntimeInformation]::ProcessArchitecture
    if ($Version -eq '5.1') {
        if ($IsCoreCLR -and -not $IsWindows) {
            $err = [ErrorRecord]::new(
                [Exception]::new("Cannot use PowerShell 5.1 on non-Windows hosts"),
                "WinPSNotAvailable",
                [ErrorCategory]::InvalidArgument,
                $Version
            )
            $PSCmdlet.ThrowTerminatingError($err)
        }

        $system32 = if ($Arch -eq [Architecture]::X64) {
            if ($osArch -ne [Architecture]::X64) {
                $err = [ErrorRecord]::new(
                    [Exception]::new("Cannot use PowerShell 5.1 $Arch on Windows $osArch"),
                    "WinPSNoAvailableArch",
                    [ErrorCategory]::InvalidArgument,
                    $Arch
                )
                $PSCmdlet.ThrowTerminatingError($err)
            }

            ($procArch -eq [Architecture]::X64) ? 'System32' : 'SystemNative'
        }
        else {
            ($procArch -eq [Architecture]::X86) ? 'System32' : 'SysWow64'
        }

        return [Path]::Combine($env:SystemRoot, $system32, "WindowsPowerShell", "v1.0", "powershell.exe")
    }
    elseif (
        $PSVersionTable.PSVersion.Major -eq $Version.Major -and
        $PSVersionTable.PSVersion.Minor -eq $Version.Minor -and
        $PSVersionTable.PSVersion.Patch -eq $Version.Build -and
        $procArch -eq $Arch
    ) {
        return [Environment]::GetCommandLineArgs()[0] -replace '\.dll$', ''
    }

    $targetFolder = $PSCmdlet.GetUnresolvedProviderPathFromPSPath(
        [Path]::Combine($PSScriptRoot, "..", "output", "PowerShell-$Version-$releaseArch"))
    $pwshExe = [Path]::Combine($targetFolder, "pwsh$nativeExt")

    if (Test-Path -LiteralPath $pwshExe) {
        return
    }

    if ($IsWindows) {
        $releasePath = "PowerShell-$Version-win-$releaseArch.zip"
        $fileName = "pwsh-$Version-$releaseArch.zip"
        $nativeExt = ".exe"
    }
    else {
        $os = $IsLinux ? "linux" : "osx"
        $releasePath = "powershell-$Version-$os-$releaseArch.tar.gz"
        $fileName = "pwsh-$Version-$releaseArch.tar.gz"
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

    if (-not (Test-Path -LiteralPath $pwshExe)) {
        if ($IsWindows) {
            $oldPreference = $global:ProgressPreference
            try {
                $global:ProgressPreference = 'SilentlyContinue'
                Expand-Archive -LiteralPath $downloadArchive -DestinationPath $targetFolder -Force
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
    }

    $pwshExe
}

function Expand-Nupkg {
    param (
        [Parameter(Mandatory)]
        [string]
        $Path,

        [Parameter(Mandatory)]
        [string]
        $DestinationPath
    )

    $Path = (Resolve-Path -Path $Path).Path

    # WinPS doesn't support extracting from anything without a .zip extension
    # so it needs to be renamed there
    $renamed = $false
    try {
        if ($PSVersionTable.PSVersion.Major -lt 6) {
            $zipPath = $Path -replace '.nupkg$', '.zip'
            Move-Item -LiteralPath $Path -Destination $zipPath
            $renamed = $true
        }
        else {
            $zipPath = $Path
        }

        $oldPreference = $global:ProgressPreference
        try {
            $global:ProgressPreference = 'SilentlyContinue'
            Expand-Archive -LiteralPath $zipPath -DestinationPath $DestinationPath -Force
        }
        finally {
            $global:ProgressPreference = $oldPreference
        }
    }
    finally {
        if ($renamed) {
            Move-Item -LiteralPath $zipPath -Destination $Path
        }
    }

    '`[Content_Types`].xml', '*.nuspec', '_rels', 'package' | ForEach-Object -Process {
        $uneededPath = [Path]::Combine($DestinationPath, $_)
        Remove-Item -Path $uneededPath -Recurse -Force
    }
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
        $modulePath = [Path]::Combine($PSScriptRoot, "..", "output", "Modules")
    }
    process {
        foreach ($dep in $Requirements) {
            $currentModPath = [Path]::Combine($modulePath, $dep.ModuleName)
            if (Test-Path -LiteralPath $currentModPath) {
                Import-Module -Name $currentModPath
                continue
            }
            $modules.Add($dep)
        }
    }
    end {
        if (-not $modules) {
            return
        }

        Assert-ModuleFast -Version v0.0.1

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