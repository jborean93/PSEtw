<#
.rdata:00000001801FEA08 LOG_WSMAN_AN_AUTHORIZATION_DONE _EVENT_DESCRIPTOR <601h, 0, 12h, 4, 0, 8, 200000000000000Ch>
Level 4
Op 0
Task 8
Keyword 0x200000000000000C

#>

$ErrorActionPreference = 'Stop'

# $providerId = [Guid]::new('a7975c8f-ac13-49f1-87da-5a984a4ab417')
# $controlCode = 1
# $level = 5
# $keyword = 0x200000000000000C

# $providerId = [Guid]::new('f90714a8-5509-434a-bf6d-b1624c8a19a2')
# $controlCode = 1
# $level = 0x4
# $keyword = 0x8  # Transport

# Get-EtwTraceSession -Name P* | Stop-EtwTraceSession

# $eventRegister = $proc = $trace = $null
# $session = [PSEtw.Shared.EtwTraceSession]::Create("PSEtw")
# try {
#     $session.EnableTrace($providerId, $controlCode, $level, $keyword, 0)
#     $trace = $session.OpenTrace()

#     $eventRegister = Register-ObjectEvent -InputObject $trace -EventName EventReceived -SourceIdentifier PSETWId

#     $trace.Start()
#     $proc = Start-Process pwsh -PassThru
#     while ($true) {
#         $e = Wait-Event -SourceIdentifier PSETWId
#         $e | Remove-Event

#         Write-Host $e.SourceEventArgs.Message
#         if ($e.SourceEventArgs.Message -eq 'PowerShell console is ready for user input') {
#             break
#         }
#     }
# }
# finally {
#     if ($proc) {
#         $proc | Stop-Process -ErrorAction SilentlyContinue
#     }
#     if ($eventRegister) {
#         Unregister-Event -SourceIdentifier PSETWId
#         $eventRegister = $null
#     }
#     if ($trace) {
#         $trace.Dispose()
#     }
#     $session.Dispose()
# }

<#
# Starts a realtime trace for a single provider until ctrl+c is sent
Trace-EtwEvent -SessionName PSEtw -Provider $guid -KeywordsAny 0x1 -Keywords All 0x1 -Level 1

# Providers can be passed through input and are generated from
# another cmdlet
Trace-EtwEvent -SessionName PSEtw -Provider $providerArray

# Same as Register-ObjectEvent to support integration in pwsh's eventing setup
Register-EtwEvent -SessionName PSEtw @SameAsTraceEtwEvent @SameAsRegisterObjectEvent
#>

. $PSScriptRoot/tests/common.ps1

$schema = New-YamlSchema -EmitTransformer {
    param($Value, $Schema)

    if ($Value -is [Enum]) {
        [Ordered]@{
            Raw = '0x{0:X8}' -f ([int]$Value)
            Value = $Value.ToString()
        }
    }
    else {
        $Schema.EmitTransformer($Value)
    }
}

Install-TestEtwProvider

$eventHandler = $sourceId = $null
try {
    $providerGuid = (New-PSEtwEventInfo -Provider PSEtw-Event).Provider
    Write-Host "Provider Guid - $providerGuid"

    Trace-PSEtwEvent -Provider PSEtw-Event -ErrorAction Continue | ForEach-Object {
        $_ | ConvertTo-Yaml -Schema $schema -Depth 5 | Out-Host
        Write-Host ""

        if ($_.Header.Descriptor.Id -eq 1) {
            $_ | Stop-PSEtwTrace
        }
    }

    # $sourceId = [Guid]::NewGuid().Guid
    # Register-PSEtwEvent -Provider PSEtw-Event -SourceIdentifier $sourceId

    # Invoke-WithTestEtwProvider -ScriptBlock {
    #     $logger.LevelLogAlways(20)

    #     $logger.BasicEvent(10)
    # }

    # $run = $true
    # while ($run) {
    #     $found = $false
    #     Wait-Event -SourceIdentifier $sourceId -Timeout 5 | ForEach-Object {
    #         $found = $true
    #         $_.SourceEventArgs.Header | ConvertTo-Yaml -Schema $schema | Out-Host
    #         Write-Host ""
    #         $_ | Remove-Event

    #         if ($_.SourceEventArgs.Header.Descriptor.Id -eq 1) {
    #             $run = $false
    #         }
    #     }

    #     if (-not $found) {
    #         throw "Didn't find expected events"
    #     }
    # }
}
finally {
    if ($sourceId) {
        Unregister-Event -SourceIdentifier $sourceId
    }

    Uninstall-TestEtwProvider
    if (Test-PSEtwSession -Default) {
        Remove-PSEtwSession -Default
    }
}
