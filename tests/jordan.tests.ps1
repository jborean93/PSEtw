. (Join-Path $PSScriptRoot common.ps1)

Context "C" {
    It "Does test" {
        [PSEtw.Jordan]::MyMethod2() | Should -Be foo
    }
}
