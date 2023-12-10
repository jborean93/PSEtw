Add-Type -Path $PSScriptRoot\tests\PSEtwProvider\bin\Release\netstandard2.0\publish\PSEtwProvider.dll
$l = [PSEtwProvider.PSEtwEvent]::new()
try {
    # $l.LevelLogAlways(20)
    $l.BasicEvent(10)
}
finally {
    $l.Dispose()
}
