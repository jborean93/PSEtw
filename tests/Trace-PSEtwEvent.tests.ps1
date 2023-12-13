. (Join-Path $PSScriptRoot common.ps1)

Describe "Trace-PSEtwEvent" -Skip:(-not $IsAdmin) {
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
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-TraceLogger | ForEach-Object {
                    if ($_.ProviderId -eq '68fdd900-4a3e-11d1-84f4-0000f80464e3') { return }

                    $_
                    $_ | Stop-PSEtwTrace
                }
            })
        $task = $ps.BeginInvoke()

        Invoke-WithTestEtwProvider -TraceLogger -ScriptBlock {
            $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
            $eb.Reset("MyEvent", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

            $eb.AddUnicodeString(
                "String Property",
                "value",
                [Microsoft.TraceLoggingDynamic.EventOutType]::Default,
                20)
            $null = $logger.Write($eb)
        }

        $actual = $ps.EndInvoke($task)
        foreach ($err in $ps.Streams.Error) {
            Write-Error -ErrorRecord $err
        }

        $actual.Count | Should -Be 1
        $actual | Should -BeOfType ([PSEtw.Shared.EtwEventArgs])

        $actual.ProviderId | Should -BeOfType ([guid])
        $actual.ProviderId | Should -Be 1054782c-5461-554b-15c9-8d36ab0c3097
        $actual.ProviderName | Should -Be PSEtw-TraceLogger
        $actual.ThreadId | Should -BeOfType ([int])
        $actual.ProcessId | Should -BeOfType ([int])
        $actual.TimeStamp | Should -BeOfType ([DateTime])
        $actual.TimeStamp.Kind | Should -Be Utc
        $actual.ActivityId | Should -BeOfType ([guid])
        $actual.Id | Should -Be 0
        $actual.Version | Should -Be 0
        $actual.Channel | Should -Be 11
        $actual.ChannelName | Should -BeNullOrEmpty
        $actual.Level | Should -Be 4
        $actual.LevelName | Should -BeNullOrEmpty
        $actual.OpCode | Should -Be 0
        $actual.OpCodeName | Should -BeNullOrEmpty
        $actual.Task | Should -Be 0
        $actual.TaskName | Should -Be MyEvent
        $actual.Keyword | Should -Be 1
        $actual.KeywordNames.Count | Should -Be 0
        $actual.Tags | Should -Be 0
        $actual.EventData | Should -BeNullOrEmpty
        $actual.Properties.Count | Should -Be 1
        $actual.Properties[0].Name | Should -Be 'String Property'
        $actual.Properties[0].Value | Should -BeOfType ([string])
        $actual.Properties[0].Value | Should -Be value
        $actual.Properties[0].DisplayValue | Should -Be value
        $actual.Properties[0].Tags | Should -Be 20
        $actual.Properties[0].ToString() | Should -Be 'String Property=value'
        $actual.EventMessage | Should -BeNullOrEmpty
    }

    It "Filters by level" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Manifest -Level Error | ForEach-Object {
                    if ($_.Id -gt 0) {
                        $_
                        $_ | Stop-PSEtwTrace
                    }
                }
            })
        $task = $ps.BeginInvoke()

        Invoke-WithTestEtwProvider -ScriptBlock {
            $logger.LevelWarning(10)
            $logger.LevelError(20)
        }

        $actual = $ps.EndInvoke($task)
        foreach ($err in $ps.Streams.Error) {
            Write-Error -ErrorRecord $err
        }

        $actual.Count | Should -Be 1
        $actual | Should -BeOfType ([PSEtw.Shared.EtwEventArgs])

        $actual.Id | Should -Be 4
        $actual.Level | Should -Be 2
        $actual.LevelName | Should -Be Error
        $actual.Properties[0].Name | Should -Be myId
        $actual.Properties[0].Value | Should -Be 20
    }

    It "Filters by keywords any" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Manifest -KeywordsAny Bar | ForEach-Object {
                    if ($_.Id -gt 0) {
                        $_
                        $_ | Stop-PSEtwTrace
                    }
                }
            })
        $task = $ps.BeginInvoke()

        Invoke-WithTestEtwProvider -ScriptBlock {
            $logger.KeywordCustomFoo(10)
            $logger.KeywordCustomBar(20)
        }

        $actual = $ps.EndInvoke($task)
        foreach ($err in $ps.Streams.Error) {
            Write-Error -ErrorRecord $err
        }

        $actual.Count | Should -Be 1
        $actual | Should -BeOfType ([PSEtw.Shared.EtwEventArgs])

        $actual.Id | Should -Be 9
        $actual.Keyword | Should -Be 0x0000F00000000002
        $actual.KeywordNames.Count | Should -Be 1
        @($actual.KeywordNames)[0] | Should -Be Bar
        $actual.Properties[0].Name | Should -Be myId
        $actual.Properties[0].Value | Should -Be 20
    }

    It "Filters by keywords all" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Manifest -KeywordsAll Foo, Bar | ForEach-Object {
                    if ($_.Id -gt 0) {
                        $_
                        $_ | Stop-PSEtwTrace
                    }
                }
            })
        $task = $ps.BeginInvoke()

        Invoke-WithTestEtwProvider -ScriptBlock {
            $logger.KeywordCustomFoo(10)
            $logger.KeywordCustomFooBar(20)
        }

        $actual = $ps.EndInvoke($task)
        foreach ($err in $ps.Streams.Error) {
            Write-Error -ErrorRecord $err
        }

        $actual.Count | Should -Be 1
        $actual | Should -BeOfType ([PSEtw.Shared.EtwEventArgs])

        $actual.Id | Should -Be 10
        $actual.Keyword | Should -Be 0x0000F00000000003
        $actual.KeywordNames.Count | Should -Be 2
        $actual.KeywordNames[0] | Should -Be Foo
        $actual.KeywordNames[1] | Should -Be Bar
        $actual.Properties[0].Name | Should -Be myId
        $actual.Properties[0].Value | Should -Be 20
    }

    It "Filters using pipeline input" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                $eventInfo = New-PSEtwEventInfo -Provider PSEtw-Manifest -Level Error
                $eventInfo | Trace-PSEtwEvent | ForEach-Object {
                    if ($_.Id -gt 0) {
                        $_
                        $_ | Stop-PSEtwTrace
                    }
                }
            })
        $task = $ps.BeginInvoke()

        Invoke-WithTestEtwProvider -ScriptBlock {
            $logger.LevelWarning(10)
            $logger.LevelError(20)
        }

        $actual = $ps.EndInvoke($task)
        foreach ($err in $ps.Streams.Error) {
            Write-Error -ErrorRecord $err
        }

        $actual.Count | Should -Be 1
        $actual | Should -BeOfType ([PSEtw.Shared.EtwEventArgs])

        $actual.Id | Should -Be 4
        $actual.Level | Should -Be 2
        $actual.LevelName | Should -Be Error
        $actual.Properties[0].Name | Should -Be myId
        $actual.Properties[0].Value | Should -Be 20
    }

    It "Uses a custom ETW session" {
        $session = New-PSEtwSession -Name PSEtw-Temp
        try {
            $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
            $iss.ImportPSModulesFromPath($global:ModuleManifest)
            $ps = [PowerShell]::Create($iss)
            [void]$ps.AddScript({
                    Trace-PSEtwEvent -Provider PSEtw-Manifest -SessionName $args[0] | ForEach-Object {
                        if ($_.Id -gt 0) {
                            $_
                            $_ | Stop-PSEtwTrace
                        }
                    }
                }).AddArgument($session.Name)
            $task = $ps.BeginInvoke()

            Invoke-WithTestEtwProvider -ScriptBlock {
                $logger.BasicEvent(10)
            }

            $actual = $ps.EndInvoke($task)
            foreach ($err in $ps.Streams.Error) {
                Write-Error -ErrorRecord $err
            }

            $actual.Id | Should -Be 1
            $actual.Properties[0].Name | Should -Be myId
            $actual.Properties[0].Value | Should -Be 10
        }
        finally {
            $session | Remove-PSEtwSession
        }
    }

    It "Records error in event when unpacking invalid property data" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-TraceLogger | ForEach-Object {
                    if ($_.ProviderId -eq '68fdd900-4a3e-11d1-84f4-0000f80464e3') { return }

                    $_
                    if ($_.TaskName -eq 'MyGoodEvent') {
                        $_ | Stop-PSEtwTrace
                    }
                }
            })
        $task = $ps.BeginInvoke()

        Invoke-WithTestEtwProvider -TraceLogger -ScriptBlock {
            # We expect this to be an error as it's not valid XML
            $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
            $eb.Reset("MyFailedEvent1", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

            $eb.AddUnicodeString(
                "AddUnicodeStringAsXml",
                "invalid xml",
                [Microsoft.TraceLoggingDynamic.EventOutType]::Xml,
                0)
            $null = $logger.Write($eb)

            # This should also be an error as it's an invalid input/output combination
            $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
            $eb.Reset("MyFailedEvent2", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

            $eb.AddBinary(
                "AddBinaryAsString",
                [byte[]]@(0, 1, 2, 3),
                [Microsoft.TraceLoggingDynamic.EventOutType]::String,
                0)
            $null = $logger.Write($eb)

            $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
            $eb.Reset("MyFailedEvent3", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)
            $eb.AddBinary(
                "AddBinaryAsUtf8String",
                [byte[]]@(0, 1, 2, 3),
                [Microsoft.TraceLoggingDynamic.EventOutType]::Utf8,
                0)
            $null = $logger.Write($eb)

            $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
            $eb.Reset("MyFailedEvent4", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)
            $eb.AddBinary(
                "AddBinaryAsDateTime",
                [byte[]]@(0, 1, 2, 3),
                [Microsoft.TraceLoggingDynamic.EventOutType]::DateTimeUtc,
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

        $actual = $ps.EndInvoke($task)
        $actual.Count | Should -Be 5

        $actual[0].TaskName | Should -Be MyFailedEvent1
        $actual[0].EventMessage | Should -BeLike "Failed to unpack EventData due to unhandled exception: Data at the root level is invalid. Line 1, position 1.*"
        $actual[0].Properties.Count | Should -Be 0

        $actual[1].TaskName | Should -Be MyFailedEvent2
        $actual[1].EventMessage | Should -BeLike "Failed to unpack EventData due to unhandled exception: No valid transformations of TdhTypeReader to TDH_OUTTYPE_STRING for 'AddBinaryAsString'*"
        $actual[1].Properties.Count | Should -Be 0

        $actual[2].TaskName | Should -Be MyFailedEvent3
        $actual[2].EventMessage | Should -BeLike "Failed to unpack EventData due to unhandled exception: No valid transformations of TdhTypeReader to TDH_OUTTYPE_UTF8 for 'AddBinaryAsUtf8String'*"
        $actual[2].Properties.Count | Should -Be 0

        $actual[3].TaskName | Should -Be MyFailedEvent4
        $actual[3].EventMessage | Should -BeLike "Failed to unpack EventData due to unhandled exception: No valid transformations of TdhTypeReader to TDH_OUTTYPE_DATETIME_UTC for 'AddBinaryAsDateTime'*"
        $actual[3].Properties.Count | Should -Be 0

        $actual[4].TaskName | Should -Be MyGoodEvent
        $actual[4].EventMessage | Should -BeNullOrEmpty
        $actual[4].Properties.Count | Should -Be 1
        $actual[4].Properties[0].Name | Should -Be UnicodeString
        $actual[4].Properties[0].Value | Should -Be value
    }

    It "Store the event byte data" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-TraceLogger -IncludeRawData | ForEach-Object {
                    if ($_.ProviderId -eq '68fdd900-4a3e-11d1-84f4-0000f80464e3') { return }

                    $_
                    $_ | Stop-PSEtwTrace
                }
            })
        $task = $ps.BeginInvoke()

        Invoke-WithTestEtwProvider -TraceLogger -ScriptBlock {
            $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
            $eb.Reset("MyEvent", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

            $eb.AddUnicodeString(
                "UnicodeString",
                "value",
                [Microsoft.TraceLoggingDynamic.EventOutType]::Default,
                10)
            $null = $logger.Write($eb)
        }

        $actual = $ps.EndInvoke($task)
        $actual.Count | Should -Be 1

        $actual[0].TaskName | Should -Be MyEvent
        # value as Unicode encoded bytes
        [Convert]::ToBase64String($actual[0].EventData) | Should -Be dgBhAGwAdQBlAAAA
        $actual[0].EventMessage | Should -BeNullOrEmpty
        $actual[0].Properties.Count | Should -Be 1
        $actual[0].Properties[0].Name | Should -Be UnicodeString
        $actual[0].Properties[0].Value | Should -Be value
    }

    It "Errors on invalid trace input" {
        $actual = Trace-PSEtwEvent -Provider PSEtw-Manifest -Level Foo -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        [string]$err | Should -Be "Unknown provider level 'Foo'"
    }

    It "Fails to open ETW Trace Session that does not exist" {
        $expected = "Failed to open session 'Invalid', ensure it exists and the current user has permissions to open the session*"
        $err = {
            Trace-PSEtwEvent -Provider PSEtw-Manifest -SessionName Invalid
        } | Should -Throw -PassThru

        [string]$err | Should -BeLike $expected
    }
}
