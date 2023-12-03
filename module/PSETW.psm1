$importModule = Get-Command -Name Import-Module -Module Microsoft.PowerShell.Core
$moduleName = [System.IO.Path]::GetFileNameWithoutExtension($PSCommandPath)

if ($IsCoreClr) {
    $isReload = $true
    if (-not ('PSETW.Shared.LoadContext' -as [type])) {
        $isReload = $false

        Add-Type -Path ([System.IO.Path]::Combine($PSScriptRoot, 'bin', 'net6.0', "$moduleName.Shared.dll"))
    }

    $mainModule = [PSETW.Shared.LoadContext]::Initialize()
    $innerMod = &$importModule -Assembly $mainModule -PassThru:$isReload
}
else {
    $innerMod = if ('PSETW.PSETWGlobals' -as [type]) {
        $modAssembly = [PSETW.PSETWGlobals].Assembly
        &$importModule -Assembly $modAssembly -Force -PassThru
    }
    else {
        $modPath = [System.IO.Path]::Combine($PSScriptRoot, 'bin', 'net472', "$moduleName.dll")
        &$importModule -Name $modPath -ErrorAction Stop -PassThru
    }
}

if ($innerMod) {
    # Bug in pwsh, Import-Module in an assembly will pick up a cached instance
    # and not call the same path to set the nested module's cmdlets to the
    # current module scope. This is only technically needed if someone is
    # calling 'Import-Module -Name PSETW -Force' a second time. The first
    # import is still fine.
    # https://github.com/PowerShell/PowerShell/issues/20710
    $addExportedCmdlet = [System.Management.Automation.PSModuleInfo].GetMethod(
        'AddExportedCmdlet',
        [System.Reflection.BindingFlags]'Instance, NonPublic'
    )
    foreach ($cmd in $innerMod.ExportedCmdlets.Values) {
        $addExportedCmdlet.Invoke($ExecutionContext.SessionState.Module, @(, $cmd))
    }
}
