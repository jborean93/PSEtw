Add-Type -Path $PSScriptRoot\tests\PSEtwProvider\bin\Release\netstandard2.0\publish\PSEtwProvider.dll
$l = [PSEtwProvider.PSEtwEvent]::new()
try {
    # $l.LevelLogAlways(20)
    $l.TypeTest(
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
    $l.BasicEvent(10)
}
finally {
    $l.Dispose()
}
