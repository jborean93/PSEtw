. (Join-Path $PSScriptRoot common.ps1)

Describe "Trace-PSEtwEvent" -Skip:(-not $IsAdmin) {
    BeforeAll {
        Install-TestEtwProvider
    }

    AfterAll {
        Uninstall-TestEtwProvider
    }

    Context "Completion and Parameter Helpers" {
        It "Completes available providers" {
            $actual = Complete 'Trace-PSEtwEvent -Provider '
            $actual.Count | Should -BeGreaterThan 0
            $found = $actual | ForEach-Object {
                if (-not $_.ListItemText.StartsWith('PSEtw-')) {
                    return
                }

                $_.CompletionText | Should -Be 'PSEtw-Event'
                $_.ListItemText | Should -Be $_.CompletionText

                $_
            }
            $found.Count | Should -Be 1
        }

        It "Completes available providers with partial match" {
            $actual = Complete 'Trace-PSEtwEvent -Provider PSEtw-'
            $actual.Count | Should -BeGreaterThan 0
            $found = $actual | ForEach-Object {
                if (-not $_.ListItemText.StartsWith('PSEtw-')) {
                    throw "Found extra provider $($_.ListItemText)"
                }

                $_.CompletionText | Should -Be 'PSEtw-Event'
                $_.ListItemText | Should -Be $_.CompletionText

                $_
            }
            $found.Count | Should -Be 1
        }

        It "Completes available <Param> with no provider" -TestCases @(
            @{ Param = 'KeywordsAny' }
            @{ Param = 'KeywordsAll' }
        ) {
            param ($Param)

            $actual = Complete "Trace-PSEtwEvent -$Param "
            $actual.Count | Should -Be 1
            $actual.CompletionText | Should -Be '*'
            $actual.ListItemText | Should -Be '*'
            $actual.ToolTip | Should -Be 'All keywords 0xFFFFFFFFFFFFFFFF'
        }

        It "Completes available <Param> with specific provider" -TestCases @(
            @{ Param = 'KeywordsAny' }
            @{ Param = 'KeywordsAll' }
        ) {
            param ($Param)

            $actual = Complete "Trace-PSEtwEvent -Provider PSEtw-Event -$Param "
            $actual.Count | Should -BeGreaterOrEqual 3
            $found = $actual | ForEach-Object {
                if ($_.ListItemText -eq 'Foo') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Foo 0x00000001"
                }
                elseif ($_.ListItemText -eq 'Bar') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Bar 0x00000002"
                }
                elseif ($_.ListItemText -eq '*') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be 'All keywords 0xFFFFFFFFFFFFFFFF'
                }
                else {
                    return
                }

                $_
            }

            $found.Count | Should -Be 3
        }

        It "Completes available levels with no provider" {
            $actual = Complete 'Trace-PSEtwEvent -Level '
            $actual.Count | Should -Be 6
            $found = $actual | ForEach-Object {
                if ($_.ListItemText -eq 'Critical') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Critical 0x01"
                }
                elseif ($_.ListItemText -eq 'Error') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Error 0x02"
                }
                elseif ($_.ListItemText -eq 'Warning') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Warning 0x03"
                }
                elseif ($_.ListItemText -eq 'Info') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Info 0x04"
                }
                elseif ($_.ListItemText -eq 'Verbose') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Verbose 0x05"
                }
                elseif ($_.ListItemText -eq '*') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "All levels 0xFF"
                }
                else {
                    return
                }

                $_
            }
            $found.Count | Should -Be 6
        }

        It "Completes available levels with specific provider" {
            $actual = Complete 'Trace-PSEtwEvent -Provider PSEtw-Event -Level E'
            $actual.Count | Should -Be 2
            $found = $actual | ForEach-Object {
                if ($_.ListItemText -eq 'Error') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Error 0x02"
                }
                elseif ($_.ListItemText -eq '*') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "All levels 0xFF"
                }
                else {
                    return
                }

                $_
            }
            $found.Count | Should -Be 2
        }
    }
}
