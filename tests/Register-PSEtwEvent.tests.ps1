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
            # Ignore EventSource init events
            while ($true) {
                $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
                $actual | Should -Not -BeNullOrEmpty
                $actual | Remove-Event

                if ($actual.SourceEventArgs.Header.Descriptor.Id -gt 0) {
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
            $descriptor.Keyword | Should -Be 0x0000F00000000000

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
            $info.Properties[0].ToString() | Should -Be "myId=10"
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Registers an action" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier $sourceId -Action {
            if ($EventArgs.Header.Descriptor.Id -eq 1) {
                New-Event -SourceIdentifier PSEtw-Wait -MessageData $EventArgs
            }
        }
        try {
            $registerOutput | Should -BeOfType ([System.Management.Automation.PSEventJob])

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.BasicEvent(10)
            }

            $actual = Wait-Event -SourceIdentifier PSEtw-Wait -Timeout 5

            $actual | Should -Not -BeNullOrEmpty
            $actual.MessageData.Header.Descriptor.Id | Should -Be 1
            $actual.MessageData.Info.Properties[0].Name | Should -Be myId
            $actual.MessageData.Info.Properties[0].Value | Should -Be 10

            $actual | Remove-Event
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Filters by level" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier $sourceId -Level Error
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.LevelWarning(10)
                $logger.LevelError(20)
            }
            # Ignore EventSource init events
            while ($true) {
                $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
                $actual | Should -Not -BeNullOrEmpty
                $actual | Remove-Event

                if ($actual.SourceEventArgs.Header.Descriptor.Id -gt 0) {
                    break
                }
            }

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 4
            $actual.SourceEventArgs.Header.Descriptor.Level | Should -Be 2
            $actual.SourceEventArgs.Info.Level | Should -Be Error
            $actual.SourceEventArgs.Info.Properties[0].Name | Should -Be myId
            $actual.SourceEventArgs.Info.Properties[0].Value | Should -Be 20
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Filters by keywords any" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier $sourceId -KeywordsAny Bar
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.KeywordCustomFoo(10)
                $logger.KeywordCustomBar(20)
            }
            # Ignore EventSource init events
            while ($true) {
                $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
                $actual | Should -Not -BeNullOrEmpty
                $actual | Remove-Event

                if ($actual.SourceEventArgs.Header.Descriptor.Id -gt 0) {
                    break
                }
            }

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 9
            $actual.SourceEventArgs.Header.Descriptor.Keyword | Should -Be 0x0000F00000000002
            $actual.SourceEventArgs.Info.Keywords.Count | Should -Be 1
            $actual.SourceEventArgs.Info.Keywords[0] | Should -Be Bar
            $actual.SourceEventArgs.Info.Properties[0].Name | Should -Be myId
            $actual.SourceEventArgs.Info.Properties[0].Value | Should -Be 20
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Filters by keywords all" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier $sourceId -KeywordsAll Foo, Bar
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.KeywordCustomFoo(10)
                $logger.KeywordCustomFooBar(20)
            }
            # Ignore EventSource init events
            while ($true) {
                $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
                $actual | Should -Not -BeNullOrEmpty
                $actual | Remove-Event

                if ($actual.SourceEventArgs.Header.Descriptor.Id -gt 0) {
                    break
                }
            }

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 10
            $actual.SourceEventArgs.Header.Descriptor.Keyword | Should -Be 0x0000F00000000003
            $actual.SourceEventArgs.Info.Keywords.Count | Should -Be 2
            $actual.SourceEventArgs.Info.Keywords[0] | Should -Be Foo
            $actual.SourceEventArgs.Info.Keywords[1] | Should -Be Bar
            $actual.SourceEventArgs.Info.Properties[0].Name | Should -Be myId
            $actual.SourceEventArgs.Info.Properties[0].Value | Should -Be 20
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Uses a custom ETW session" {
        $session = New-PSEtwSession -Name PSEtw-Temp
        try {
            $sourceId = [Guid]::NewGuid()
            $registerOutput = Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier $sourceId -SessionName $session.Name
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.BasicEvent(10)
            }
            # Ignore EventSource init events
            while ($true) {
                $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
                $actual | Should -Not -BeNullOrEmpty
                $actual | Remove-Event

                if ($actual.SourceEventArgs.Header.Descriptor.Id -gt 0) {
                    break
                }
            }

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 1
            $actual.SourceEventArgs.Info.Properties[0].Name | Should -Be myId
            $actual.SourceEventArgs.Info.Properties[0].Value | Should -Be 10
        }
        finally {
            if ($sourceId) { Unregister-Event -SourceIdentifier $sourceId }
            $session | Remove-PSEtwSession
        }
    }

    It "Fails with Forward and Action set" {
        {
            Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier foo -Forward -Action {'foo'}
        } | Should -Throw "The action is not supported when you are forwarding events."
    }
}
