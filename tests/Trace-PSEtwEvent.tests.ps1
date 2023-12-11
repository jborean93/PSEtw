. (Join-Path $PSScriptRoot common.ps1)

Describe "Trace-PSEtwEvent" -Skip:(-not $IsAdmin) {
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
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Event | ForEach-Object {
                    if ($_.Header.Descriptor.Id -gt 0) {
                        $_
                        $_ | Stop-PSEtwTrace
                    }
                }
            })
        $task = $ps.BeginInvoke()

        Invoke-WithTestEtwProvider -ScriptBlock {
            $logger.BasicEvent(10)
        }

        $actual = $ps.EndInvoke($task)
        foreach ($err in $ps.Streams.Error) {
            Write-Error -ErrorRecord $err
        }

        $actual.Count | Should -Be 1
        $actual | Should -BeOfType ([PSEtw.Shared.EtwEventArgs])

        $header = $actual.Header
        $header | Should -BeOfType ([PSEtw.Shared.EventHeader])
        $header.ThreadId | Should -BeOfType ([int])
        $header.ProcessId | Should -BeOfType ([int])
        $header.TimeStamp | Should -BeOfType ([DateTime])
        $header.TimeStamp.Kind | Should -Be Utc
        $header.ProviderId | Should -Be $providerGuid
        $header.ActivityId | Should -BeOfType ([guid])

        $descriptor = $actual.Header.Descriptor
        $descriptor | Should -BeOfType ([PSEtw.Shared.EventDescriptor])

        $descriptor.Id | Should -Be 1
        $descriptor.Version | Should -Be 0
        $descriptor.Channel | Should -Be 0
        $descriptor.Level | Should -Be 4
        $descriptor.Opcode | Should -Be 0
        $descriptor.Task | Should -Be -3
        $descriptor.Keyword | Should -Be 0x0000F00000000000

        $info = $actual.Info
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

    It "Filters by level" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Event -Level Error | ForEach-Object {
                    if ($_.Header.Descriptor.Id -gt 0) {
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

        $actual.Header.Descriptor.Id | Should -Be 4
        $actual.Header.Descriptor.Level | Should -Be 2
        $actual.Info.Level | Should -Be Error
        $actual.Info.Properties[0].Name | Should -Be myId
        $actual.Info.Properties[0].Value | Should -Be 20
    }

    It "Filters by keywords any" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Event -KeywordsAny Bar | ForEach-Object {
                    if ($_.Header.Descriptor.Id -gt 0) {
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

        $actual.Header.Descriptor.Id | Should -Be 9
        $actual.Header.Descriptor.Keyword | Should -Be 0x0000F00000000002
        $actual.Info.Keywords.Count | Should -Be 1
        $actual.Info.Keywords[0] | Should -Be Bar
        $actual.Info.Properties[0].Name | Should -Be myId
        $actual.Info.Properties[0].Value | Should -Be 20
    }

    It "Filters by keywords all" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Event -KeywordsAll Foo, Bar | ForEach-Object {
                    if ($_.Header.Descriptor.Id -gt 0) {
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

        $actual.Header.Descriptor.Id | Should -Be 10
        $actual.Header.Descriptor.Keyword | Should -Be 0x0000F00000000003
        $actual.Info.Keywords.Count | Should -Be 2
        $actual.Info.Keywords[0] | Should -Be Foo
        $actual.Info.Keywords[1] | Should -Be Bar
        $actual.Info.Properties[0].Name | Should -Be myId
        $actual.Info.Properties[0].Value | Should -Be 20
    }

    It "Filters using pipeline input" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                $eventInfo = New-PSEtwEventInfo -Provider PSEtw-Event -Level Error
                $eventInfo | Trace-PSEtwEvent | ForEach-Object {
                    if ($_.Header.Descriptor.Id -gt 0) {
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

        $actual.Header.Descriptor.Id | Should -Be 4
        $actual.Header.Descriptor.Level | Should -Be 2
        $actual.Info.Level | Should -Be Error
        $actual.Info.Properties[0].Name | Should -Be myId
        $actual.Info.Properties[0].Value | Should -Be 20
    }

    It "Uses a custom ETW session" {
        $session = New-PSEtwSession -Name PSEtw-Temp
        try {
            $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
            $iss.ImportPSModulesFromPath($global:ModuleManifest)
            $ps = [PowerShell]::Create($iss)
            [void]$ps.AddScript({
                    Trace-PSEtwEvent -Provider PSEtw-Event -SessionName $args[0] | ForEach-Object {
                        if ($_.Header.Descriptor.Id -gt 0) {
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

            $actual.Header.Descriptor.Id | Should -Be 1
            $actual.Info.Properties[0].Name | Should -Be myId
            $actual.Info.Properties[0].Value | Should -Be 10
        }
        finally {
            $session | Remove-PSEtwSession
        }
    }

    It "Fails to open ETW Trace Session that does not exist" {
        $expected = "Failed to open session 'Invalid', ensure it exists and the current user has permissions to open the session*"
        $err = {
            Trace-PSEtwEvent -Provider PSEtw-Event -SessionName Invalid
        } | Should -Throw -PassThru

        [string]$err | Should -BeLike $expected
    }
}
