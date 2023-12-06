. (Join-Path $PSScriptRoot common.ps1)

Describe "Register-PSEtwEvent" -Skip:(-not $IsAdmin) {
    BeforeAll {
        Install-TestEtwProvider
    }

    AfterAll {
        Uninstall-TestEtwProvider
        if (Test-PSEtwSession -Default) {
            Remove-PSEtwSession -Default
        }
    }

    It "Receives simple event" {
        $eventHandler = Register-PSEtwEvent -Provider PSEtw-Event

        Invoke-WithTestEtwProvider -ScriptBlock {
            $logger.BasicEvent(1)
        }

        $actual = Wait-Event -SourceIdentifier $eventHandler.SourceIdentifier -Timeout 5
        $actual | Should -Not -BeNullOrEmpty

        $eventHandler.Dispose()
    }
}
