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
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier $sourceId
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.BasicEvent(10)
            }

            # Depending on when this is run we could have multiple preceding events
            # wait until we get the one we want.
            while ($true) {
                $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
                $actual | Should -Not -BeNullOrEmpty
                $actual | Remove-Event

                if ($actual.SourceEventArgs.Header.Descriptor.Id -eq 1) {
                    break
                }
            }

            $actual.Sender | Should -BeOfType ([PSEtw.Shared.EtwTrace])

            $ea = $actual.SourceEventArgs
            $ea | Should -BeOfType ([PSEtw.Shared.EtwEventArgs])

            $header = $ea.Header
            $header | Should -BeOfType ([PSEtw.Shared.EventHeader])
            $header.ThreadId | Should -BeOfType ([int])
            $header.ProcessId | Should -BeOfType ([int])
            $header.TimeStamp | Should -BeOfType ([DateTime])
            $header.TimeStamp.Kind | Should -Be Utc
            $header.ProviderId | Should -Be $providerGuid
            $header.ActivityId | Should -BeOfType ([guid])

            $descriptor = $header.Descriptor
            $descriptor | Should -BeOfType ([PSEtw.Shared.EventDescriptor])

            $descriptor.Id | Should -Be 1
            $descriptor.Version | Should -Be 0
            $descriptor.Channel | Should -Be 0
            $descriptor.Level | Should -Be 4
            $descriptor.Opcode | Should -Be 0
            $descriptor.Task | Should -Be -3
            $descriptor.Keyword | Should -Be 263882790666240

            $info = $ea.Info
            $info | Should -BeOfType ([PSEtw.Shared.EventInfo])
            $info.EventGuid | Should -BeOfType ([guid])
            $info.Provider | Should -Be PSEtw-Event
            $info.Level | Should -Be Information
            $info.Channel | Should -BeNullOrEmpty
            $info.Keywords | Should -BeNullOrEmpty
            $info.Task | Should -Be BasicEvent
            $info.OpCode | Should -BeNullOrEmpty
            $info.EventMessage | Should -BeNullOrEmpty
            $info.ProviderMessage | Should -BeNullOrEmpty
            $info.EventName | Should -BeNullOrEmpty
            $info.RelatedActivityIdName | Should -BeNullOrEmpty
            $info.Properties.Count | Should -Be 1
            $info.Properties[0].Name | Should -Be myId
            $info.Properties[0].Value | Should -Be 10
            $info.Properties[0].DisplayValue | Should -Be 10
            $info.properties[0].Tags | Should -Be 0
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }
}
