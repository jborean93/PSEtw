. (Join-Path $PSScriptRoot common.ps1)

Describe "Register-PSEtwEvent" -Skip:(-not $IsAdmin) {
    BeforeAll {
        Install-TestEtwProvider

        $providerGuid = (New-PSEtwEventInfo -Provider PSEtw-Manifest).Provider
    }

    AfterAll {
        Uninstall-TestEtwProvider
        if (Test-PSEtwSession -Default) {
            Remove-PSEtwSession -Default
        }
    }

    It "Receives simple event" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId
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
            $info.Provider | Should -Be PSEtw-Manifest
            $info.Level | Should -Be Information
            $info.Channel | Should -BeNullOrEmpty
            $info.Keywords | Should -BeNullOrEmpty
            $info.Task | Should -Be BasicEvent
            $info.OpCode | Should -BeNullOrEmpty
            $info.RawEventMessage | Should -BeNullOrEmpty
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
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId -Action {
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
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId -Level Error
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
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId -KeywordsAny Bar
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
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId -KeywordsAll Foo, Bar
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
        $eventInfo = New-PSEtwEventInfo -Provider PSEtw-Manifest -Level Error

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
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId
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

    It "Parses string types" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.StringTest(
                    "string 1",
                    "Caf$([char]0xE9)s",
                    "string 3 with unicode $([Char]::ConvertFromUtf32(0x1F3B5))"
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

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 12
            $actual.SourceEventArgs.Info.RawEventMessage | Should -Be "Event Message '%1', '%2', '%3'"
            $actual.SourceEventArgs.Info.EventMessage | Should -Be "Event Message 'string 1', 'Caf$([char]0xE9)s', 'string 3 with unicode $([Char]::ConvertFromUtf32(0x1F3B5))'"
            $actual.SourceEventArgs.Info.Properties.Count | Should -Be 3

            $prop = $actual.SourceEventArgs.Info.Properties[0]
            $prop.Name | Should -Be arg1
            $prop.Value | Should -BeOfType ([string])
            $prop.Value | Should -Be "string 1"
            $prop.DisplayValue | Should -Be "string 1"
            $prop.ToString() | Should -Be "arg1=string 1"

            $prop = $actual.SourceEventArgs.Info.Properties[1]
            $prop.Name | Should -Be arg2
            $prop.Value | Should -BeOfType ([string])
            $prop.Value | Should -Be "Caf$([char]0xE9)s"
            $prop.DisplayValue | Should -Be "Caf$([char]0xE9)s"
            $prop.ToString() | Should -Be "arg2=Caf$([char]0xE9)s"

            $prop = $actual.SourceEventArgs.Info.Properties[2]
            $prop.Name | Should -Be arg3
            $prop.Value | Should -BeOfType ([string])
            $prop.Value | Should -Be "string 3 with unicode $([Char]::ConvertFromUtf32(0x1F3B5))"
            $prop.DisplayValue | Should -Be "string 3 with unicode $([Char]::ConvertFromUtf32(0x1F3B5))"
            $prop.ToString() | Should -Be "arg3=string 3 with unicode $([Char]::ConvertFromUtf32(0x1F3B5))"
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Parses struct types" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-TraceLogger -SourceIdentifier $sourceId
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -TraceLogger -ScriptBlock {
                $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
                $eb.Reset("MyEventName", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

                $eb.AddUnicodeString("RootEntry 1", "foo" , [Microsoft.TraceLoggingDynamic.EventOutType]::Default, 0)

                $null = $eb.AddStruct("MyStruct", 2)
                $eb.AddUnicodeString("field 1", "value 1", [Microsoft.TraceLoggingDynamic.EventOutType]::Default, 0)
                $eb.AddUnicodeString("field 2", "value 2", [Microsoft.TraceLoggingDynamic.EventOutType]::Default, 0)

                $eb.AddUnicodeString("RootEntry 2", "bar" , [Microsoft.TraceLoggingDynamic.EventOutType]::Default, 0)

                $null = $logger.Write($eb)
            }

            $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
            $actual | Should -Not -BeNullOrEmpty
            $actual | Remove-Event

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 0
            $actual.SourceEventArgs.Info.Properties.Count | Should -Be 3

            $prop = $actual.SourceEventArgs.Info.Properties[0]
            $prop.Name | Should -Be 'RootEntry 1'
            $prop.Value | Should -Be foo
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be foo
            $prop.ToString() | Should -Be 'RootEntry 1=foo'

            $prop = $actual.SourceEventArgs.Info.Properties[1]
            $prop.Name | Should -Be MyStruct
            $prop.Value.Count | Should -Be 2
            $prop.DisplayValue | Should -Be 'field 1=value 1, field 2=value 2'
            $prop.ToString() | Should -Be "MyStruct=field 1=value 1, field 2=value 2"

            $prop.Value[0] | Should -BeOfType ([PSEtw.Shared.EventPropertyInfo])
            $prop.Value[0].Name | Should -Be 'field 1'
            $prop.Value[0].Value | Should -Be 'value 1'
            $prop.Value[0].Value | Should -BeOfType ([string])
            $prop.Value[0].DisplayValue | Should -Be 'value 1'
            $prop.Value[0].ToString() | Should -Be 'field 1=value 1'

            $prop.Value[1] | Should -BeOfType ([PSEtw.Shared.EventPropertyInfo])
            $prop.Value[1].Name | Should -Be 'field 2'
            $prop.Value[1].Value | Should -Be 'value 2'
            $prop.Value[1].Value | Should -BeOfType ([string])
            $prop.Value[1].DisplayValue | Should -Be 'value 2'
            $prop.Value[1].ToString() | Should -Be 'field 2=value 2'

            $prop = $actual.SourceEventArgs.Info.Properties[2]
            $prop.Name | Should -Be 'RootEntry 2'
            $prop.Value | Should -Be bar
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be bar
            $prop.ToString() | Should -Be 'RootEntry 2=bar'
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Uses a custom ETW session" {
        $session = New-PSEtwSession -Name PSEtw-Temp
        try {
            $sourceId = [Guid]::NewGuid()
            $registerOutput = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId -SessionName $session.Name
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

    It "Uses custom tag" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-TraceLogger -SourceIdentifier $sourceId
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -TraceLogger -ScriptBlock {
                $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
                $eb.Reset("MyEventName", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

                $eb.AddUnicodeString(
                    "UnicodeString",
                    "value",
                    [Microsoft.TraceLoggingDynamic.EventOutType]::Default,
                    10)
                $null = $logger.Write($eb)
            }

            $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
            $actual | Should -Not -BeNullOrEmpty
            $actual | Remove-Event

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 0
            $actual.SourceEventArgs.Info.Properties[0].Name | Should -Be UnicodeString
            $actual.SourceEventArgs.Info.Properties[0].Value | Should -Be value
            $actual.SourceEventArgs.Info.Properties[0].Tags | Should -Be 10
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Decodes custom types from trace logger" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-TraceLogger -SourceIdentifier $sourceId
        try {
            $registerOutput | Should -BeNullOrEmpty

            $etwAssembly = [System.IO.Path]::Combine(
                $PSScriptRoot,
                'PSEtwProvider',
                'bin',
                'Release',
                'netstandard2.0',
                'publish',
                'PSEtwProvider.dll')
            Add-Type -LiteralPath $etwAssembly
            $logger = [Microsoft.TraceLoggingDynamic.EventProvider]::new(
                "PSEtw-TraceLogger",
                [Microsoft.TraceLoggingDynamic.EventProviderOptions]::new())

            $defaultOut = [Microsoft.TraceLoggingDynamic.EventOutType]::Default
            $ansi = [System.Text.Encoding]::GetEncoding(
                [System.Globalization.CultureInfo]::CurrentCulture.TextInfo.ANSICodePage)
            $sid = [System.Security.Principal.SecurityIdentifier]'S-1-5-19'
            $sidBytes = [byte[]]::new($sid.BinaryLength)
            $sid.GetBinaryForm($sidBytes, 0)
            $ipPort = [UInt16]("0x$([System.Net.IPAddress]::HostToNetworkOrder([int16]1234).ToString("X"))")
            $ipv4 = [System.Net.IPAddress]::Parse("127.0.0.1")
            $ipv6 = [System.Net.IPAddress]::Parse("80a7:f53b:5501:7baf:4d77:66e1:a9f0:fbd1")

            $saIPv4 = [System.Net.IPEndPoint]::new($ipv4, 1234).Serialize()
            $saIPv4Bytes = [byte[]]::new($saIPv4.Size)
            for ($i = 0; $i -lt $saIPv4Bytes.Length; $i++) {
                $saIPv4Bytes[$i] = $saIPv4[$i]
            }
            $saIPv6 = [System.Net.IPEndPoint]::new($ipv6, 1234).Serialize()
            $saIPv6Bytes = [byte[]]::new($saIPv6.Size)
            for ($i = 0; $i -lt $saIPv6Bytes.Length; $i++) {
                $saIPv6Bytes[$i] = $saIPv6[$i]
            }

            $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
            $eb.Reset("MyEventName", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

            $eb.AddUnicodeString("AddUnicodeString", "unicode default $([Char]::ConvertFromUtf32(0x1F3B5))", $defaultOut, 0)
            $eb.AddUnicodeStringArray("AddUnicodeStringArray1", @("string 1"), $defaultOut, 0)
            $eb.AddUnicodeStringArray("AddUnicodeStringArray2", @("string 2", "string 3"), $defaultOut, 0)
            $eb.AddAnsiString(
                'AddAnsiString',
                $ansi.GetBytes("Caf$([char]0xE9)s"),
                $defaultOut,
                0)
            $eb.AddAnsiString(
                'AddAnsiStringUtf8',
                [System.Text.Encoding]::UTF8.GetBytes("Caf$([char]0xE9)s"),
                [Microsoft.TraceLoggingDynamic.EventOutType]::Utf8,
                0)
            $eb.AddUInt8(
                "AddUInt8AsString",
                "0xE9",
                [Microsoft.TraceLoggingDynamic.EventOutType]::String,
                0)
            $eb.AddUInt16(
                "AddUInt16AsString",
                "0x210E",
                [Microsoft.TraceLoggingDynamic.EventOutType]::String,
                0)
            $eb.AddIntPtr("AddIntPtr", [IntPtr]-1, $defaultOut, 0)
            $eb.AddIntPtr(
                "AddIntPtrAsPointer",
                [IntPtr]-1,
                [Microsoft.TraceLoggingDynamic.EventOutType]::CodePointer,
                0)
            $eb.AddUIntPtr("AddUIntPtr", [UIntPtr]1, $defaultOut, 0)
            $eb.AddUIntPtr(
                "AddUIntPtrAsPointer",
                [UIntPtr]1,
                [Microsoft.TraceLoggingDynamic.EventOutType]::CodePointer,
                0)
            $eb.AddSystemTime("AddSystemTime", @(1999, 12, 0, 12, 23, 59, 59, 999), $defaultOut, 0)
            $eb.AddSid("AddSid", $sidBytes, $defaultOut, 0)
            $eb.AddSid(
                "AddSidAsBytes",
                $sidBytes,
                [Microsoft.TraceLoggingDynamic.EventOutType]::Hex,
                0)
            $eb.AddHexInt32("AddHexInt32", -1, $defaultOut, 0)
            $eb.AddHexInt32(
                "AddHexInt32AsWin32Error",
                1,
                [Microsoft.TraceLoggingDynamic.EventOutType]::Win32Error,
                0)
            $eb.AddHexInt32(
                "AddHexInt32AsNtStatus",
                1,
                [Microsoft.TraceLoggingDynamic.EventOutType]::NtStatus,
                0)
            $eb.AddHexInt64("AddHexInt64", -1, $defaultOut, 0)
            $eb.AddHexInt64(
                "AddHexInt64AsWin32Error",
                1,
                [Microsoft.TraceLoggingDynamic.EventOutType]::Win32Error,
                0)
            $eb.AddHexInt64(
                "AddHexInt64AsNtStatus",
                1,
                [Microsoft.TraceLoggingDynamic.EventOutType]::NtStatus,
                0)
            $eb.AddCountedString(
                "AddCountedString",
                "string $([char]0) value $([Char]::ConvertFromUtf32(0x1F3B5))$([char]0)",
                $defaultOut, 0)
            $eb.AddCountedAnsiString(
                'AddCountedAnsiString',
                $ansi.GetBytes("Caf$([char]0xE9)s$([char]0)Test$([char]0)"),
                $defaultOut,
                0)
            $eb.AddCountedAnsiString(
                'AddCountedAnsiStringUtf8',
                [System.Text.Encoding]::UTF8.GetBytes("Caf$([char]0xE9)s"),
                [Microsoft.TraceLoggingDynamic.EventOutType]::Utf8,
                0)
            $eb.AddCountedBinary("AddCountedBinary", [byte[]]@(0, 1, 2, 3), $defaultOut, 0)
            $eb.AddUInt32(
                "AddUInt32AsPid",
                $pid,
                [Microsoft.TraceLoggingDynamic.EventOutType]::Pid,
                0)
            $eb.AddUInt32(
                "AddUInt32AsTid",
                1234,
                [Microsoft.TraceLoggingDynamic.EventOutType]::Tid,
                0)
            $eb.AddUInt16(
                "AddUInt16AsPort",
                $ipPort,
                [Microsoft.TraceLoggingDynamic.EventOutType]::Port,
                0)
            $eb.AddBinary(
                "AddBinaryAsIPv4",
                $ipv4.GetAddressBytes(),
                [Microsoft.TraceLoggingDynamic.EventOutType]::IPv4,
                0)
            $eb.AddBinary(
                "AddBinaryAsIPv6",
                $ipv6.GetAddressBytes(),
                [Microsoft.TraceLoggingDynamic.EventOutType]::IPv6,
                0)
            $eb.AddCountedBinary(
                "AddCountedBinaryAsSocketAddressIPv4",
                $saIPv4Bytes,
                [Microsoft.TraceLoggingDynamic.EventOutType]::SocketAddress,
                0)
            $eb.AddCountedBinary(
                "AddCountedBinaryAsSocketAddressIPv6",
                $saIPv6Bytes,
                [Microsoft.TraceLoggingDynamic.EventOutType]::SocketAddress,
                0)
            $eb.AddUnicodeString(
                "AddUnicodeStringAsXml",
                "<root><test>value</test></root>",
                [Microsoft.TraceLoggingDynamic.EventOutType]::Xml,
                0)
            $eb.AddCountedBinary(
                "AddCountedBinaryAsUnicodeXml",
                [System.Text.Encoding]::Unicode.GetBytes(
                    "<?xml version=""1.0"" encoding=""UTF-16LE""?>`n<root><test>value</test></root>"),
                [Microsoft.TraceLoggingDynamic.EventOutType]::Xml,
                0)
            $eb.AddCountedBinary(
                "AddCountedBinaryAsUtf8Xml",
                [System.Text.Encoding]::UTF8.GetBytes(
                    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<root><test>value</test></root>"),
                [Microsoft.TraceLoggingDynamic.EventOutType]::Xml,
                0)
            $eb.AddUnicodeString(
                "AddUnicodeStringAsJson",
                '{"foo": "bar"}',
                [Microsoft.TraceLoggingDynamic.EventOutType]::Json,
                0)
            $eb.AddHexInt32(
                "AddHexInt32AsHresult",
                0x00090320,
                [Microsoft.TraceLoggingDynamic.EventOutType]::Hresult,
                0)
            $eb.AddFileTime(
                "AddFileTimeAsCultureInsensitiveDateTime",
                116444736000000000,
                [Microsoft.TraceLoggingDynamic.EventOutType]::CultureInsensitiveDateTime,
                0)
            $eb.AddFileTime(
                "AddFileTimeAsDateTimeUtc",
                116444736000000000,
                [Microsoft.TraceLoggingDynamic.EventOutType]::DateTimeUtc,
                0)

            $null = $logger.Write($eb)

            $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
            $actual | Should -Not -BeNullOrEmpty
            $actual | Remove-Event

            $actual.SourceEventArgs.Info.Properties.Count | Should -Be 42

            $prop = $actual.SourceEventArgs.Info.Properties[0]
            $prop.Name | Should -Be AddUnicodeString
            $prop.Value | Should -Be "unicode default $([Char]::ConvertFromUtf32(0x1F3B5))"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be "unicode default $([Char]::ConvertFromUtf32(0x1F3B5))"
            $prop.ToString() | Should -Be "AddUnicodeString=unicode default $([Char]::ConvertFromUtf32(0x1F3B5))"

            $prop = $actual.SourceEventArgs.Info.Properties[1]
            $prop.Name | Should -Be AddUnicodeStringArray1.Count
            $prop.Value | Should -Be 1
            $prop.Value | Should -BeOfType ([ushort])
            $prop.DisplayValue | Should -Be 1
            $prop.ToString() | Should -Be "AddUnicodeStringArray1.Count=1"

            $prop = $actual.SourceEventArgs.Info.Properties[2]
            $prop.Name | Should -Be AddUnicodeStringArray1
            $prop.Value | Should -Be "string 1"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be 'string 1'
            $prop.ToString() | Should -Be "AddUnicodeStringArray1=string 1"

            $prop = $actual.SourceEventArgs.Info.Properties[3]
            $prop.Name | Should -Be AddUnicodeStringArray2.Count
            $prop.Value | Should -Be 2
            $prop.Value | Should -BeOfType ([ushort])
            $prop.DisplayValue | Should -Be 2
            $prop.ToString() | Should -Be "AddUnicodeStringArray2.Count=2"

            $prop = $actual.SourceEventArgs.Info.Properties[4]
            $prop.Name | Should -Be AddUnicodeStringArray2
            ($prop.Value -join "|") | Should -Be 'string 2|string 3'
            $prop.Value | ForEach-Object { $_ | Should -BeOfType ([string]) }
            $prop.DisplayValue | Should -Be 'string 2, string 3'
            $prop.ToString() | Should -Be "AddUnicodeStringArray2=string 2, string 3"

            $prop = $actual.SourceEventArgs.Info.Properties[5]
            $prop.Name | Should -Be AddAnsiString
            $prop.Value | Should -Be "Caf$([char]0xE9)s"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be "Caf$([char]0xE9)s"
            $prop.ToString() | Should -Be "AddAnsiString=Caf$([char]0xE9)s"

            $prop = $actual.SourceEventArgs.Info.Properties[6]
            $prop.Name | Should -Be AddAnsiStringUtf8
            $prop.Value | Should -Be "Caf$([char]0xE9)s"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be "Caf$([char]0xE9)s"
            $prop.ToString() | Should -Be "AddAnsiStringUtf8=Caf$([char]0xE9)s"

            $prop = $actual.SourceEventArgs.Info.Properties[7]
            $prop.Name | Should -Be AddUInt8AsString
            $prop.Value | Should -Be "$([char]0xE9)"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be "$([char]0xE9)"
            $prop.ToString() | Should -Be "AddUInt8AsString=$([char]0xE9)"

            $prop = $actual.SourceEventArgs.Info.Properties[8]
            $prop.Name | Should -Be AddUInt16AsString
            $prop.Value | Should -Be "$([char]0x210E)"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be "$([char]0x210E)"
            $prop.ToString() | Should -Be "AddUInt16AsString=$([char]0x210E)"

            $prop = $actual.SourceEventArgs.Info.Properties[9]
            $prop.Name | Should -Be AddIntPtr
            $prop.Value | Should -Be ([Int64]-1)
            $prop.Value | Should -BeOfType ([Int64])
            $prop.DisplayValue | Should -Be "-1"
            $prop.ToString() | Should -Be "AddIntPtr=-1"

            $prop = $actual.SourceEventArgs.Info.Properties[10]
            $prop.Name | Should -Be AddIntPtrAsPointer
            $prop.Value | Should -Be ([Int64]-1)
            $prop.Value | Should -BeOfType ([Int64])
            $prop.DisplayValue | Should -Be "-1"
            $prop.ToString() | Should -Be "AddIntPtrAsPointer=-1"

            $prop = $actual.SourceEventArgs.Info.Properties[11]
            $prop.Name | Should -Be AddUIntPtr
            $prop.Value | Should -Be ([UInt64]1)
            $prop.Value | Should -BeOfType ([UInt64])
            $prop.DisplayValue | Should -Be "1"
            $prop.ToString() | Should -Be "AddUIntPtr=1"

            $prop = $actual.SourceEventArgs.Info.Properties[12]
            $prop.Name | Should -Be AddUIntPtrAsPointer
            $prop.Value | Should -Be ([Int64]1)
            $prop.Value | Should -BeOfType ([Int64])
            $prop.DisplayValue | Should -Be "0x1"
            $prop.ToString() | Should -Be "AddUIntPtrAsPointer=0x1"

            $prop = $actual.SourceEventArgs.Info.Properties[13]
            $prop.Name | Should -Be AddSystemTime
            $prop.Value | Should -Be ([DateTime]::new(1999, 12, 12, 23, 59, 59, 999, [DateTimeKind]::Utc))
            $prop.Value | Should -BeOfType ([DateTime])
            $prop.DisplayValue | Should -Be "1999$([char]0x200E)-$([char]0x200E)12$([char]0x200E)-$([char]0x200E)12T23:59:59.999Z"
            $prop.ToString() | Should -Be "AddSystemTime=1999$([char]0x200E)-$([char]0x200E)12$([char]0x200E)-$([char]0x200E)12T23:59:59.999Z"

            $prop = $actual.SourceEventArgs.Info.Properties[14]
            $prop.Name | Should -Be AddSid
            $prop.Value | Should -Be "S-1-5-19"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be "S-1-5-19"
            $prop.ToString() | Should -Be "AddSid=S-1-5-19"

            $prop = $actual.SourceEventArgs.Info.Properties[15]
            $prop.Name | Should -Be AddSidAsBytes
            [Convert]::ToBase64String($prop.Value) | Should -Be "AQEAAAAAAAUTAAAA"
            $prop.Value | ForEach-Object { $_ | Should -BeOfType ([byte]) }
            $prop.DisplayValue | Should -Be "S-1-5-19"
            $prop.ToString() | Should -Be "AddSidAsBytes=S-1-5-19"

            $prop = $actual.SourceEventArgs.Info.Properties[16]
            $prop.Name | Should -Be AddHexInt32
            $prop.Value | Should -Be -1
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "0xFFFFFFFF"
            $prop.ToString() | Should -Be "AddHexInt32=0xFFFFFFFF"

            $prop = $actual.SourceEventArgs.Info.Properties[17]
            $prop.Name | Should -Be AddHexInt32AsWin32Error
            $prop.Value | Should -Be 1
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "Incorrect function."
            $prop.ToString() | Should -Be "AddHexInt32AsWin32Error=Incorrect function."

            $prop = $actual.SourceEventArgs.Info.Properties[18]
            $prop.Name | Should -Be AddHexInt32AsNtStatus
            $prop.Value | Should -Be 1
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "STATUS_WAIT_1"
            $prop.ToString() | Should -Be "AddHexInt32AsNtStatus=STATUS_WAIT_1"

            $prop = $actual.SourceEventArgs.Info.Properties[19]
            $prop.Name | Should -Be AddHexInt64
            $prop.Value | Should -Be "-1"
            $prop.Value | Should -BeOfType ([int64])
            $prop.DisplayValue | Should -Be "0xFFFFFFFFFFFFFFFF"
            $prop.ToString() | Should -Be "AddHexInt64=0xFFFFFFFFFFFFFFFF"

            $prop = $actual.SourceEventArgs.Info.Properties[20]
            $prop.Name | Should -Be AddHexInt64AsWin32Error
            $prop.Value | Should -Be 1
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "0x1"
            $prop.ToString() | Should -Be "AddHexInt64AsWin32Error=0x1"

            $prop = $actual.SourceEventArgs.Info.Properties[21]
            $prop.Name | Should -Be AddHexInt64AsNtStatus
            $prop.Value | Should -Be 1
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "0x1"
            $prop.ToString() | Should -Be "AddHexInt64AsNtStatus=0x1"

            $prop = $actual.SourceEventArgs.Info.Properties[22]
            $prop.Name | Should -Be AddCountedString
            $prop.Value | Should -Be "string $([char]0) value $([Char]::ConvertFromUtf32(0x1F3B5))$([char]0)"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be "string $([char]0) value $([Char]::ConvertFromUtf32(0x1F3B5))$([char]0)"
            $prop.ToString() | Should -Be "AddCountedString=string $([char]0) value $([Char]::ConvertFromUtf32(0x1F3B5))$([char]0)"

            $prop = $actual.SourceEventArgs.Info.Properties[23]
            $prop.Name | Should -Be AddCountedAnsiString
            $prop.Value | Should -Be "Caf$([char]0xE9)s$([char]0)Test"
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be "Caf$([char]0xE9)s$([char]0)Test"
            $prop.ToString() | Should -Be "AddCountedAnsiString=Caf$([char]0xE9)s$([char]0)Test"

            $prop = $actual.SourceEventArgs.Info.Properties[24]
            $prop.Name | Should -Be AddCountedAnsiStringUtf8
            $prop.Value | Should -Be "Caf$([char]0xE9)s"
            $prop.Value | Should -BeOfType ([string])
            # This is buggy, I don't know why
            # $prop.DisplayValue | Should -Be "Caf$([char]0xE9)s"
            # $prop.ToString() | Should -Be "AddCountedAnsiStringUtf8=Caf$([char]0xE9)s"

            $prop = $actual.SourceEventArgs.Info.Properties[25]
            $prop.Name | Should -Be AddCountedBinary
            [Convert]::ToBase64String($prop.Value) | Should -Be "AAECAw=="
            $prop.Value | ForEach-Object { $_ | Should -BeOfType ([byte]) }
            $prop.DisplayValue | Should -Be "0x00010203"
            $prop.ToString() | Should -Be "AddCountedBinary=0x00010203"

            $prop = $actual.SourceEventArgs.Info.Properties[26]
            $prop.Name | Should -Be AddUInt32AsPid
            $prop.Value | Should -Be $pid
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "$pid"
            $prop.ToString() | Should -Be "AddUInt32AsPid=$pid"

            $prop = $actual.SourceEventArgs.Info.Properties[27]
            $prop.Name | Should -Be AddUInt32AsTid
            $prop.Value | Should -Be 1234
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "1234"
            $prop.ToString() | Should -Be "AddUInt32AsTid=1234"

            $prop = $actual.SourceEventArgs.Info.Properties[28]
            $prop.Name | Should -Be AddUInt16AsPort
            $prop.Value | Should -Be 1234
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "1234"
            $prop.ToString() | Should -Be "AddUInt16AsPort=1234"

            $prop = $actual.SourceEventArgs.Info.Properties[29]
            $prop.Name | Should -Be AddBinaryAsIPv4.Length
            $prop.Value | Should -Be 4
            $prop.Value | Should -BeOfType ([uint16])
            $prop.DisplayValue | Should -Be 4
            $prop.ToString() | Should -Be "AddBinaryAsIPv4.Length=4"

            $prop = $actual.SourceEventArgs.Info.Properties[30]
            $prop.Name | Should -Be AddBinaryAsIPv4
            $prop.Value | Should -Be $ipv4
            $prop.Value | Should -BeOfType ([System.Net.IPAddress])
            $prop.DisplayValue | Should -Be "0x7F000001"
            $prop.ToString() | Should -Be "AddBinaryAsIPv4=0x7F000001"

            $prop = $actual.SourceEventArgs.Info.Properties[31]
            $prop.Name | Should -Be AddBinaryAsIPv6.Length
            $prop.Value | Should -Be 16
            $prop.Value | Should -BeOfType ([uint16])
            $prop.DisplayValue | Should -Be 16
            $prop.ToString() | Should -Be "AddBinaryAsIPv6.Length=16"

            $prop = $actual.SourceEventArgs.Info.Properties[32]
            $prop.Name | Should -Be AddBinaryAsIPv6
            $prop.Value | Should -Be $ipv6
            $prop.Value | Should -BeOfType ([System.Net.IPAddress])
            $prop.DisplayValue | Should -Be $ipv6.ToString()
            $prop.ToString() | Should -Be "AddBinaryAsIPv6=$ipv6"

            $prop = $actual.SourceEventArgs.Info.Properties[33]
            $prop.Name | Should -Be AddCountedBinaryAsSocketAddressIPv4
            $prop.Value | Should -Be $saIpv4
            $prop.Value | Should -BeOfType ([System.Net.SocketAddress])
            $prop.DisplayValue | Should -Be "$($ipv4.IPAddressToString):1234"
            $prop.ToString() | Should -Be "AddCountedBinaryAsSocketAddressIPv4=$($ipv4.IPAddressToString):1234"

            $prop = $actual.SourceEventArgs.Info.Properties[34]
            $prop.Name | Should -Be AddCountedBinaryAsSocketAddressIPv6
            $prop.Value | Should -Be $saIpv6
            $prop.Value | Should -BeOfType ([System.Net.SocketAddress])
            $prop.DisplayValue | Should -Be "[$($ipv6.IPAddressToString)]:1234"
            $prop.ToString() | Should -Be "AddCountedBinaryAsSocketAddressIPv6=[$($ipv6.IPAddressToString)]:1234"

            $prop = $actual.SourceEventArgs.Info.Properties[35]
            $prop.Name | Should -Be AddUnicodeStringAsXml
            $prop.Value.root.test | Should -Be value
            $prop.Value | Should -BeOfType ([xml])
            $prop.DisplayValue | Should -Be "<root><test>value</test></root>"
            $prop.ToString() | Should -Be "AddUnicodeStringAsXml=<root><test>value</test></root>"

            $prop = $actual.SourceEventArgs.Info.Properties[36]
            $prop.Name | Should -Be AddCountedBinaryAsUnicodeXml
            $prop.Value.root.test | Should -Be value
            $prop.Value | Should -BeOfType ([xml])
            # Windows will format this has a really long hex string
            # $prop.DisplayValue | Should -Be ""
            # $prop.ToString() | Should -Be "AddCountedBinaryAsUnicodeXml=\"

            $prop = $actual.SourceEventArgs.Info.Properties[37]
            $prop.Name | Should -Be AddCountedBinaryAsUtf8Xml
            $prop.Value.root.test | Should -Be value
            $prop.Value | Should -BeOfType ([xml])
            # Windows will format this has a really long hex string
            # $prop.DisplayValue | Should -Be ""
            # $prop.ToString() | Should -Be "AddCountedBinaryAsUtf8Xml="

            $prop = $actual.SourceEventArgs.Info.Properties[38]
            $prop.Name | Should -Be AddUnicodeStringAsJson
            $prop.Value | Should -Be '{"foo": "bar"}'
            $prop.Value | Should -BeOfType ([string])
            $prop.DisplayValue | Should -Be '{"foo": "bar"}'
            $prop.ToString() | Should -Be 'AddUnicodeStringAsJson={"foo": "bar"}'

            $prop = $actual.SourceEventArgs.Info.Properties[39]
            $prop.Name | Should -Be AddHexInt32AsHresult
            $prop.Value | Should -Be 0x90320
            $prop.Value | Should -BeOfType ([int])
            $prop.DisplayValue | Should -Be "0x90320"
            $prop.ToString() | Should -Be "AddHexInt32AsHresult=0x90320"

            $prop = $actual.SourceEventArgs.Info.Properties[40]
            $prop.Name | Should -Be AddFileTimeAsCultureInsensitiveDateTime
            $prop.Value | Should -Be ([DateTime]::new(1970, 1, 1, 0, 0, 0, 0, [DateTimeKind]::Unspecified))
            $prop.Value | Should -BeOfType ([DateTime])
            $prop.DisplayValue | Should -Be "1970-01-01T00:00:00.000000000Z"
            $prop.ToString() | Should -Be "AddFileTimeAsCultureInsensitiveDateTime=1970-01-01T00:00:00.000000000Z"

            $prop = $actual.SourceEventArgs.Info.Properties[41]
            $prop.Name | Should -Be AddFileTimeAsDateTimeUtc
            $prop.Value | Should -Be ([DateTime]::new(1970, 1, 1, 0, 0, 0, 0, [DateTimeKind]::Utc))
            $prop.Value | Should -BeOfType ([DateTime])
            $prop.DisplayValue | Should -Be "$([char]0x200E)1970$([char]0x200E)-$([char]0x200E)01$([char]0x200E)-$([char]0x200E)01T00:00:00.000000000Z"
            $prop.ToString() | Should -Be "AddFileTimeAsDateTimeUtc=$([char]0x200E)1970$([char]0x200E)-$([char]0x200E)01$([char]0x200E)-$([char]0x200E)01T00:00:00.000000000Z"
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Does not crash when parsing an invalid value" {
        $sourceId = [Guid]::NewGuid()
        $registerOutput = Register-PSEtwEvent -Provider PSEtw-TraceLogger -SourceIdentifier $sourceId
        try {
            $registerOutput | Should -BeNullOrEmpty

            Invoke-WithTestEtwProvider -TraceLogger -ScriptBlock {
                $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
                $eb.Reset("MyFailedEvent", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

                # We expect this to be lost
                $eb.AddUnicodeString(
                    "AddUnicodeStringAsXml",
                    "invalid xml",
                    [Microsoft.TraceLoggingDynamic.EventOutType]::Xml,
                    0)

                $null = $logger.Write($eb)

                # This should still be reported
                $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
                $eb.Reset("MyGoodEvent", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

                $eb.AddUnicodeString(
                    "UnicodeString",
                    "value",
                    [Microsoft.TraceLoggingDynamic.EventOutType]::Default,
                    10)
                $null = $logger.Write($eb)
            }

            $actual = Wait-Event -SourceIdentifier $sourceId -Timeout 5
            $actual | Should -Not -BeNullOrEmpty
            $actual | Remove-Event

            $actual.SourceEventArgs.Header.Descriptor.Id | Should -Be 0
            $actual.SourceEventArgs.Info.EventName | Should -Be MyGoodEvent
            $actual.SourceEventArgs.Info.Properties[0].Name | Should -Be UnicodeString
            $actual.SourceEventArgs.Info.Properties[0].Value | Should -Be value
            $actual.SourceEventArgs.Info.Properties[0].Tags | Should -Be 10
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Errors on invalid trace input" {
        $sourceId = [Guid]::NewGuid()
        $actual = Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier $sourceId -Level Foo -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        [string]$err | Should -Be "Unknown provider level 'Foo'"
    }

    It "Fails with Forward and Action set" {
        {
            Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier foo -Forward -Action { 'foo' }
        } | Should -Throw "The action is not supported when you are forwarding events."
    }

    It "Fails to open ETW Trace Session that does not exist" {
        $expected = "Failed to open session 'Invalid', ensure it exists and the current user has permissions to open the session*"
        $err = {
            Register-PSEtwEvent -Provider PSEtw-Manifest -SourceIdentifier foo -SessionName Invalid
        } | Should -Throw -PassThru

        [string]$err | Should -BeLike $expected
    }
}
