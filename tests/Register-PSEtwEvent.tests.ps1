. (Join-Path $PSScriptRoot common.ps1)

Describe "Register-PSEtwEvent" -Skip:(-not $IsAdmin) {
    BeforeAll {
        Install-TestEtwProvider

        $providerGuid = (New-PSEtwEventInfo -Provider PSEtw-Event).Provider
    }

    AfterAll {
        Uninstall-TestEtwProvider
        if (Test-PSEtwSession -Default) {
            Remove-PSEtwSession -Default
        }
    }

    It "Receives simple event" {
        $eventHandler = Register-PSEtwEvent -Provider PSEtw-Event
        try {
            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.BasicEvent(1)
            }

            $actual = Wait-Event -SourceIdentifier $eventHandler.SourceIdentifier -Timeout 5
            $actual | Should -Not -BeNullOrEmpty

            $actual | Remove-Event
            $actual.Sender | Should -Be $eventHandler.EtwTrace

            $ea = $actual.SourceEventArgs
            $ea | Should -BeOfType ([PSEtw.Shared.EtwEventArgs])

            $header = $ea.Header
            $header | Should -BeOfType ([PSEtw.Shared.EventHeader])
            $header.ThreadId | Should -BeOfType ([int])
            $header.ProcessId | Should -BeOfType ([int])
            $header.TimeStamp | Should -BeOfType ([DateTime])
            $header.TimeStamp.Kind | Should -Be Utc
            $header.ProviderId | Should -BeOfType ([guid])
            $header.ActivityId | Should -BeOfType ([guid])

            $descriptor = $header.Descriptor
            $descriptor | Should -BeOfType ([PSEtw.Shared.EventDescriptor])

            $descriptor.Id | Should -Be 0
            $descriptor.Version | Should -Be 2
            $descriptor.Channel | Should -Be 0
            $descriptor.Level | Should -Be 0
            $descriptor.Opcode | Should -Be 32
            $descriptor.Task | Should -Be 0
            $descriptor.Keyword | Should -Be 0
        }
        finally {
            $eventHandler.Dispose()
        }
    }
}
