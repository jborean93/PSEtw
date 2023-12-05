. (Join-Path $PSScriptRoot common.ps1)

Describe "New-PSEtwSession" {
    It "Creates ETW Session" {
        $name = "PSEtw-Test-$([Guid]::NewGuid())"

        $actual = New-PSEtwSession -SessionName $name
        try {
            $actual.Name | Should -Be $name
            $actual | Should -BeOfType ([PSEtw.Shared.EtwTraceSession])
            Test-PSEtwSession -Name $name | Should -BeTrue
        }
        finally {
            $actual | Remove-PSEtwSession
        }
    }

    It "Creates ETW Session with -WhatIf" {
        $name = "PSEtw-Test-$([Guid]::NewGuid())"

        $actual = New-PSEtwSession -SessionName $name -WhatIf
        $actual | Should -BeNullOrEmpty
        Test-PSEtwSession -Name $name | Should -BeFalse
    }

    It "Fails to create session that already exists" {
        $name = "PSEtw-Test-$([Guid]::NewGuid())"

        $session = New-PSEtwSession -SessionName $name
        try {
            $actual = New-PSEtwSession -Name $name -ErrorAction SilentlyContinue -ErrorVariable err
            $actual | Should -BeNullOrEmpty
            $err.Count | Should -Be 1
            $err[0].CategoryInfo.Category | Should -Be NotSpecified
            [string]$err | Should -Be "Cannot create a file when that file already exists."
        }
        finally {
            $session | Remove-PSEtwSession
        }
    }

    It "Fails when session name is too long" {
        $actual = New-PSEtwSession -Name ('a' * 1025) -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        $err[0].CategoryInfo.Category | Should -Be InvalidArgument
        [string]$err | Should -BeLike "Trace session name must not be more than 1024 characters*"
    }
}

Describe "Remove-PSEtwSession" {
    It "Remove ETW Session" {
        $name = "PSEtw-Test-$([Guid]::NewGuid())"

        $actual = New-PSEtwSession -SessionName $name
        try {
            Test-PSEtwSession -Name $name | Should -BeTrue
        }
        finally {
            $actual | Remove-PSEtwSession
        }

        Test-PSEtwSession -Name $name | Should -BeFalse
    }

    It "Removes ETW Session with -WhatIf" {
        $name = "PSEtw-Test-$([Guid]::NewGuid())"

        $actual = New-PSEtwSession -SessionName $name
        try {
            Test-PSEtwSession -Name $name | Should -BeTrue

            Remove-PSEtwSession -Name $name -WhatIf

            Test-PSEtwSession -Name $name | Should -BeTrue
        }
        finally {
            $actual | Remove-PSEtwSession
        }

        Test-PSEtwSession -Name $name | Should -BeFalse
    }

    It "Completes with existing ETW session names" {
        $sessions = @(
            New-PSEtwSession -SessionName "PSEtw-Test-$([Guid]::NewGuid())"
            New-PSEtwSession -SessionName "PSEtw Test $([Guid]::NewGuid())"
        )

        try {
            $actual = Complete 'Remove-PSEtwSession -SessionName '
            $actual.Count | Should -BeGreaterOrEqual 2
            $found = $actual | ForEach-Object -Process {
                if (-not $_.ListItemText.StartsWith('PSEtw')) {
                    return
                }

                if ($_.ListItemText -eq $sessions[0].Name) {
                    $_.CompletionText | Should -Be $sessions[0].Name
                }
                else {
                    $_.CompletionText | Should -Be "'$($sessions[1].Name)'"

                }

                $_
            }
            $found.Count | Should -Be 2
        }
        finally {
            $sessions | Remove-PSEtwSession
        }
    }

    It "Completes with partial ETW session name match" {
        $sessions = @(
            New-PSEtwSession -SessionName "PSEtw-Test-$([Guid]::NewGuid())"
            New-PSEtwSession -SessionName "PSEtw Test $([Guid]::NewGuid())"
        )

        try {
            $actual = Complete 'Remove-PSEtwSession -SessionName PSEtw?'
            $actual.Count | Should -Be 2
            $actual | ForEach-Object -Process {
                if ($_.ListItemText -eq $sessions[0].Name) {
                    $_.CompletionText | Should -Be $sessions[0].Name
                }
                else {
                    $_.CompletionText | Should -Be "'$($sessions[1].Name)'"

                }
            }
        }
        finally {
            $sessions | Remove-PSEtwSession
        }
    }

    It "Fails to remove session that doesn't exist" {
        $name = "PSEtw-Test-$([Guid]::NewGuid())"

        $actual = Remove-PSEtwSession -Name $name -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        $err[0].CategoryInfo.Category | Should -Be NotSpecified
        [string]$err | Should -Be "The instance name passed was not recognized as valid by a WMI data provider."
    }

    It "Fails when session name is too long" {
        $actual = Remove-PSEtwSession -Name ('a' * 1025) -ErrorAction SilentlyContinue -ErrorVariable err
        $actual | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        $err[0].CategoryInfo.Category | Should -Be InvalidArgument
        [string]$err | Should -BeLike "Trace session name must not be more than 1024 characters*"
    }
}

Describe "Test-PSEtwSession" {
    It "Checks session exists" {
        $name = "PSEtw-Test-$([Guid]::NewGuid())"

        Test-PSEtwSession -SessionName $name | Should -BeFalse
        Test-PSEtwSession -Name $name | Should -BeFalse
        $name | Test-PSEtwSession | Should -BeFalse

        $session = New-PSEtwSession -Name $name
        try {
            Test-PSEtwSession -SessionName $name | Should -BeTrue
            Test-PSEtwSession -Name $name | Should -BeTrue
            $name | Test-PSEtwSession | Should -BeTrue
        }
        finally {
            $session | Remove-PSEtwSession
        }

        Test-PSEtwSession -SessionName $name | Should -BeFalse
        Test-PSEtwSession -Name $name | Should -BeFalse
        $name | Test-PSEtwSession | Should -BeFalse
    }
}
