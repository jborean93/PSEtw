@{
    InvokeBuildVersion = '5.10.4'
    BuildRequirements = @(
        @{
            ModuleName = 'OpenAuthenticode'
            RequiredVersion = '0.4.0'
        }
        @{
            ModuleName = 'platyPS'
            RequiredVersion = '0.14.2'
        }
    )
    TestRequirements = @()
}
