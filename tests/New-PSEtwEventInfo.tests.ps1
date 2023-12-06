. (Join-Path $PSScriptRoot common.ps1)

Describe "New-PSEtwEventInfo" -Skip:(-not $IsAdmin) {
    BeforeAll {
        Install-TestEtwProvider
    }

    AfterAll {
        Uninstall-TestEtwProvider
    }

    Context "Value Validation" {
        BeforeAll {
            $providerGuid = (New-PSEtwEventInfo -Provider PSEtw-Event).Provider
        }

        It "Accepts Provider as string" {
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event
            $actual.Provider | Should -Be $providerGuid
        }

        It "Accepts Provider as guid" {
            $actual = New-PSEtwEventInfo -Provider $providerGuid
            $actual.Provider | Should -Be $providerGuid
        }

        It "Accepts Provider as guid from string" {
            $actual = New-PSEtwEventInfo -Provider "$providerGuid"
            $actual.Provider | Should -Be $providerGuid
        }

        It "Fails with invalid provider" {
            $actual = New-PSEtwEventInfo -Provider 'Invalid Provider' -ErrorAction SilentlyContinue -ErrorVariable err
            $actual | Should -BeNullOrEmpty
            $err.Count | Should -Be 1
            [string]$err | Should -Be "Unknown ETW provider 'Invalid Provider'"
            $err[0].Exception | Should -BeOfType ([ArgumentException])
        }

        It "Accepts <Param> as string" -TestCases @(
            @{ Param = "KeywordsAll" }
            @{ Param = "KeywordsAny" }
        ) {
            param ($Param)

            $splat = @{
                "$Param" = "Foo"
            }
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event @splat
            $actual.$Param | Should -Be 1
        }

        It "Accepts <Param> as string numeric value" -TestCases @(
            @{ Param = "KeywordsAll" }
            @{ Param = "KeywordsAny" }
        ) {
            param ($Param)

            $splat = @{
                "$Param" = "0xFFFFFFFF"
            }
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event @splat
            $actual.$Param | Should -Be ([Int64]"0xFFFFFFFF")
        }

        It "Accepts <Param> as wildcard string" -TestCases @(
            @{ Param = "KeywordsAll" }
            @{ Param = "KeywordsAny" }
        ) {
            param ($Param)

            $splat = @{
                "$Param" = "*"
            }
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event @splat
            $actual.$Param | Should -Be 0xFFFFFFFFFFFFFFFF
        }

        It "Accepts <Param> as known int" -TestCases @(
            @{ Param = "KeywordsAll" }
            @{ Param = "KeywordsAny" }
        ) {
            param ($Param)

            $splat = @{
                "$Param" = 1
            }
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event @splat
            $actual.$Param | Should -Be 1
        }

        It "Accepts <Param> as any int" -TestCases @(
            @{ Param = "KeywordsAll" }
            @{ Param = "KeywordsAny" }
        ) {
            $splat = @{
                "$Param" = 526
            }
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event @splat
            $actual.$Param | Should -Be 526
        }

        It "Accepts level <Level> as string" -TestCases @(
            @{ Level = 'LogAlways' ; Expected = 0 }
            @{ Level = 'Critical' ; Expected = 1 }
            @{ Level = 'Error' ; Expected = 2 }
            @{ Level = 'Warning' ; Expected = 3 }
            @{ Level = 'Info' ; Expected = 4 }
            @{ Level = 'Verbose' ; Expected = 5 }
            @{ Level = '*'; Expected = 0xFF }
        ) {
            param ($Level, $Expected)

            $actual = New-PSEtwEventInfo -Provider PSEtw-Event -Level $Level
            $actual.Level | Should -Be $Expected
        }

        It "Accepts custom provider level string value" {
            $actual = New-PSEtwEventInfo -Provider PowerShellCore -Level Debug
            $actual.Level | Should -Be 0x14
        }

        It "Accepts known level as int" {
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event -Level 3
            $actual.Level | Should -Be 3
        }

        It "Accepts known level as int string" {
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event -Level "0x3"
            $actual.Level | Should -Be 3
        }

        It "Accepts any level as int" {
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event -Level 60
            $actual.Level | Should -Be 60
        }

        It "Fails with level greater than 0xFF" {
            $actual = New-PSEtwEventInfo -Provider PSEtw-Event -Level 256 -ErrorAction SilentlyContinue -ErrorVariable err
            $actual | Should -BeNullOrEmpty
            $err.Count | Should -Be 1
            [string]$err | Should -Be "Provider level 256 must be less than or equal to 255"
            $err[0].Exception | Should -BeOfType ([ArgumentException])
        }
    }

    Context "Completion and Parameter Helpers" {
        It "Completes available providers" {
            $actual = Complete 'New-PSEtwEventInfo -Provider '
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
            $actual = Complete 'New-PSEtwEventInfo -Provider PSEtw-'
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

            $actual = Complete "New-PSEtwEventInfo -$Param "
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

            $actual = Complete "New-PSEtwEventInfo -Provider PSEtw-Event -$Param "
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
            $actual = Complete 'New-PSEtwEventInfo -Level '
            $actual.Count | Should -Be 7
            $found = $actual | ForEach-Object {
                if ($_.ListItemText -eq 'LogAlways') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "LogAlways 0x00"
                }
                elseif ($_.ListItemText -eq 'Critical') {
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
            $found.Count | Should -Be 7
        }

        It "Completes available levels with specific provider" {
            $actual = Complete 'New-PSEtwEventInfo -Provider PSEtw-Event -Level E'
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

        It "Completes custom level value" {
            $actual = Complete 'New-PSEtwEventInfo -Provider PowerShellCore -Level D'
            $actual.Count | Should -Be 2

            $found = $actual | ForEach-Object {
                if ($_.ListItemText -eq 'Debug') {
                    $_.CompletionText | Should -Be $_.ListItemText
                    $_.ToolTip | Should -Be "Debug level defined by PowerShell (which is above Informational defined by system) 0x14"
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
