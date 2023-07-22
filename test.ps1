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

$providerId = [Guid]::new('f90714a8-5509-434a-bf6d-b1624c8a19a2')
$controlCode = 1
$level = 0x4
$keyword = 0x8  # Transport

Get-EtwTraceSession -Name P* | Stop-EtwTraceSession

$eventRegister = $proc = $trace = $null
$session = [PSETW.TraceSession]::Create("PSETW")
try {
    $session.EnableTrace($providerId, $controlCode, $level, $keyword, 0)
    $trace = $session.OpenTrace()

    $eventRegister = Register-ObjectEvent -InputObject $trace -EventName EventReceived -SourceIdentifier PSETWId

    $trace.Start()
    $proc = Start-Process pwsh -PassThru
    while ($true) {
        $e = Wait-Event -SourceIdentifier PSETWId
        $e | Remove-Event

        Write-Host $e.SourceEventArgs.Message
        if ($e.SourceEventArgs.Message -eq 'PowerShell console is ready for user input') {
            break
        }
    }
}
finally {
    if ($proc) {
        $proc | Stop-Process -ErrorAction SilentlyContinue
    }
    if ($eventRegister) {
        Unregister-Event -SourceIdentifier PSETWId
        $eventRegister = $null
    }
    if ($trace) {
        $trace.Dispose()
    }
    $session.Dispose()
}
