. (Join-Path $PSScriptRoot common.ps1)

Describe "Stop-PSEtwTrace" -Skip:(-not $IsAdmin) {
    BeforeAll {
        Install-TestEtwProvider
    }

    AfterAll {
        Uninstall-TestEtwProvider
        if (Test-PSEtwSession -Default) {
            Remove-PSEtwSession -Default
        }
    }

    It "Stops an active trace" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Manifest | ForEach-Object {
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

        $actual.Header.Descriptor.Id | Should -Be 1
        $actual.Info.Properties[0].Value | Should -Be 10
    }

    It "Ignores events with no cancel token" {
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

            $ea | Stop-PSEtwTrace
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
        }
    }

    It "Ignores event with a trace already cancelled" {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $iss.ImportPSModulesFromPath($global:ModuleManifest)
        $ps = [PowerShell]::Create($iss)
        [void]$ps.AddScript({
                Trace-PSEtwEvent -Provider PSEtw-Manifest | ForEach-Object {
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

        $actual | Stop-PSEtwTrace
    }
}
