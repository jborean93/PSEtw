. (Join-Path $PSScriptRoot common.ps1)

Context "C" {
    It "Does test" {
        [PSETW.Jordan]::MyMethod2() | Should -Be foo
    }
}
