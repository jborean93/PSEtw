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

    It "Filters using pipeline input" {
        $eventInfo = New-PSEtwEventInfo -Provider PSEtw-Event -Level Error

        $sourceId = [Guid]::NewGuid()
        $registerOutput = $eventInfo | Register-PSEtwEvent -SourceIdentifier $sourceId
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

    It "Parses multiple value types" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier $sourceId
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.TypeTest(
                    $true,
                    [byte]128,
                    [byte[]]@(0, 1, 2, 3),
                    [char]1,
                    [DateTime]::new(1970, 1, 1, 0, 0, 0, [DateTimeKind]::Utc),
                    [DateTime]::new(1970, 1, 1, 0, 0, 0, [DateTimeKind]::Local),
                    [DateTime]::new(1970, 1, 1, 0, 0, 0, [DateTimeKind]::Unspecified),
                    [Double]'10.32',
                    [PSEtwProvider.IntEnum]::Value2,
                    [PSEtwProvider.IntFlags]'Value2, Value3',
                    [Guid]"40fc67b6-6d25-45d5-b7ee-9ebc87ee247e",
                    -1,
                    -2,
                    -3,
                    [IntPtr]-4,
                    -5,
                    [float]'102.01',
                    [UInt16]"32768",
                    [UInt32]"2147483648",
                    [UInt64]"9223372036854775808"
                )
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

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 11
            $actual.SourceEventArgs.Info.Properties.Count | Should -Be 21

            $prop = $actual.SourceEventArgs.Info.Properties[0]
            $prop.Name | Should -Be boolValue
            $prop.Value | Should -BeOfType ([bool])
            $prop.Value | Should -BeTrue
            $prop.DisplayValue | Should -Be true
            $prop.ToString() | Should -Be boolValue=true

            $prop = $actual.SourceEventArgs.Info.Properties[1]
            $prop.Name | Should -Be byteValue
            $prop.Value | Should -BeOfType ([byte])
            $prop.Value | Should -Be 128
            $prop.DisplayValue | Should -Be 128
            $prop.ToString() | Should -Be byteValue=128

            $prop = $actual.SourceEventArgs.Info.Properties[2]
            $prop.Name | Should -Be byteArraySize
            $prop.Value | Should -BeOfType ([UInt32])
            $prop.Value | Should -Be 4
            $prop.DisplayValue | Should -Be 4
            $prop.ToString() | Should -Be byteArraySize=4

            $prop = $actual.SourceEventArgs.Info.Properties[3]
            $prop.Name | Should -Be byteArray
            , $prop.Value | Should -BeOfType ([byte[]])
            [Convert]::ToBase64String($prop.Value) | Should -Be "AAECAw=="
            $prop.DisplayValue | Should -Be "0x00010203"
            $prop.ToString() | Should -Be "byteArray=0x00010203"

            $prop = $actual.SourceEventArgs.Info.Properties[4]
            $prop.Name | Should -Be charValue
            $prop.Value | Should -BeOfType ([UInt16])
            $prop.Value | Should -Be ([UInt16]1)
            $prop.DisplayValue | Should -Be 1
            $prop.ToString() | Should -Be charValue=1

            $prop = $actual.SourceEventArgs.Info.Properties[5]
            $prop.Name | Should -Be dateTimeUtc
            $prop.Value | Should -BeOfType ([DateTime])
            $prop.Value | Should -Be ([DateTime]::new(1970, 1, 1, 0, 0, 0, [DateTimeKind]::Utc))
            $prop.DisplayValue | Should -Be "$([char]0x200E)1970$([char]0x200E)-$([char]0x200E)01$([char]0x200E)-$([char]0x200E)01T00:00:00.000000000Z"
            $prop.ToString() | Should -Be "dateTimeUtc=$([char]0x200E)1970$([char]0x200E)-$([char]0x200E)01$([char]0x200E)-$([char]0x200E)01T00:00:00.000000000Z"

            $prop = $actual.SourceEventArgs.Info.Properties[6]
            $prop.Name | Should -Be dateTimeLocal
            $prop.Value | Should -BeOfType ([DateTime])
            $prop.Value | Should -Be ([DateTime]::new(1970, 1, 1, 0, 0, 0, [DateTimeKind]::Utc))
            $prop.DisplayValue | Should -Be "$([char]0x200E)1970$([char]0x200E)-$([char]0x200E)01$([char]0x200E)-$([char]0x200E)01T00:00:00.000000000Z"
            $prop.ToString() | Should -Be "dateTimeLocal=$([char]0x200E)1970$([char]0x200E)-$([char]0x200E)01$([char]0x200E)-$([char]0x200E)01T00:00:00.000000000Z"

            $prop = $actual.SourceEventArgs.Info.Properties[7]
            $prop.Name | Should -Be dateTimeUnspecified
            $prop.Value | Should -BeOfType ([DateTime])
            $prop.Value | Should -Be ([DateTime]::new(1970, 1, 1, 0, 0, 0, [DateTimeKind]::Utc))
            $prop.DisplayValue | Should -Be "$([char]0x200E)1970$([char]0x200E)-$([char]0x200E)01$([char]0x200E)-$([char]0x200E)01T00:00:00.000000000Z"
            $prop.ToString() | Should -Be "dateTimeUnspecified=$([char]0x200E)1970$([char]0x200E)-$([char]0x200E)01$([char]0x200E)-$([char]0x200E)01T00:00:00.000000000Z"

            $prop = $actual.SourceEventArgs.Info.Properties[8]
            $prop.Name | Should -Be doubleValue
            $prop.Value | Should -BeOfType ([double])
            $prop.Value | Should -Be "10.32"
            $prop.DisplayValue | Should -Be '10.320000'
            $prop.ToString() | Should -Be "doubleValue=10.320000"

            $prop = $actual.SourceEventArgs.Info.Properties[9]
            $prop.Name | Should -Be enumValue
            $prop.Value | Should -BeOfType ([uint])
            $prop.Value | Should -Be 1
            $prop.DisplayValue | Should -Be 'Value2 '
            $prop.ToString() | Should -Be 'enumValue=Value2 '

            $prop = $actual.SourceEventArgs.Info.Properties[10]
            $prop.Name | Should -Be enumFlags
            $prop.Value | Should -BeOfType ([uint])
            $prop.Value | Should -Be 3
            $prop.DisplayValue | Should -Be 'Value2 |Value3 '
            $prop.ToString() | Should -Be 'enumFlags=Value2 |Value3 '

            $prop = $actual.SourceEventArgs.Info.Properties[11]
            $prop.Name | Should -Be guid
            $prop.Value | Should -BeOfType ([guid])
            $prop.Value | Should -Be 40fc67b6-6d25-45d5-b7ee-9ebc87ee247e
            $prop.DisplayValue | Should -Be '{40fc67b6-6d25-45d5-b7ee-9ebc87ee247e}'
            $prop.ToString() | Should -Be 'guid={40fc67b6-6d25-45d5-b7ee-9ebc87ee247e}'

            $prop = $actual.SourceEventArgs.Info.Properties[12]
            $prop.Name | Should -Be int16
            $prop.Value | Should -BeOfType ([int16])
            $prop.Value | Should -Be -1
            $prop.DisplayValue | Should -Be -1
            $prop.ToString() | Should -Be int16=-1

            $prop = $actual.SourceEventArgs.Info.Properties[13]
            $prop.Name | Should -Be int32
            $prop.Value | Should -BeOfType ([int32])
            $prop.Value | Should -Be -2
            $prop.DisplayValue | Should -Be -2
            $prop.ToString() | Should -Be int32=-2

            $prop = $actual.SourceEventArgs.Info.Properties[14]
            $prop.Name | Should -Be int64
            $prop.Value | Should -BeOfType ([int64])
            $prop.Value | Should -Be -3
            $prop.DisplayValue | Should -Be -3
            $prop.ToString() | Should -Be int64=-3

            $prop = $actual.SourceEventArgs.Info.Properties[15]
            $prop.Name | Should -Be pointer
            $prop.Value | Should -BeOfType ([int64])
            $prop.Value | Should -Be -4
            $prop.DisplayValue | Should -Be 0xFFFFFFFFFFFFFFFC
            $prop.ToString() | Should -Be pointer=0xFFFFFFFFFFFFFFFC

            $prop = $actual.SourceEventArgs.Info.Properties[16]
            $prop.Name | Should -Be signedByte
            $prop.Value | Should -BeOfType ([sbyte])
            $prop.Value | Should -Be -5
            $prop.DisplayValue | Should -Be -5
            $prop.ToString() | Should -Be signedByte=-5

            $prop = $actual.SourceEventArgs.Info.Properties[17]
            $prop.Name | Should -Be single
            $prop.Value | Should -BeOfType ([float])
            $prop.Value | Should -Be '102.01'
            $prop.DisplayValue | Should -Be '102.010002'
            $prop.ToString() | Should -Be single=102.010002

            $prop = $actual.SourceEventArgs.Info.Properties[18]
            $prop.Name | Should -Be uint16
            $prop.Value | Should -BeOfType ([UInt16])
            $prop.Value | Should -Be 32768
            $prop.DisplayValue | Should -Be 32768
            $prop.ToString() | Should -Be uint16=32768

            $prop = $actual.SourceEventArgs.Info.Properties[19]
            $prop.Name | Should -Be uint32
            $prop.Value | Should -BeOfType ([UInt32])
            $prop.Value | Should -Be 2147483648
            $prop.DisplayValue | Should -Be 2147483648
            $prop.ToString() | Should -Be uint32=2147483648

            $prop = $actual.SourceEventArgs.Info.Properties[20]
            $prop.Name | Should -Be uint64
            $prop.Value | Should -BeOfType ([UInt64])
            $prop.Value | Should -Be 9223372036854775808
            $prop.DisplayValue | Should -Be 9223372036854775808
            $prop.ToString() | Should -Be uint64=9223372036854775808
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
            Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier foo -Forward -Action { 'foo' }
        } | Should -Throw "The action is not supported when you are forwarding events."
    }

    It "Fails to open ETW Trace Session that does not exist" {
        $expected = "Failed to open session 'Invalid', ensure it exists and the current user has permissions to open the session*"
        $err = {
            Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier foo -SessionName Invalid
        } | Should -Throw -PassThru

        [string]$err | Should -BeLike $expected
    }
}
