Add-Type -Path $PSScriptRoot\tests\PSEtwProvider\bin\Release\netstandard2.0\publish\PSEtwProvider.dll

$l = [Microsoft.TraceLoggingDynamic.EventProvider]::new("PSEtw-TraceLogger", [Microsoft.TraceLoggingDynamic.EventProviderOptions]::new())
# $l = [PSEtwProvider.PSEtwManifest]::new()
try {
    $eb = [Microsoft.TraceLoggingDynamic.EventBuilder]::new()
    $eb.Reset("MyEventName", [Microsoft.TraceLoggingDynamic.EventLevel]::Info, 1, 0)

    $eb.AddUnicodeString("RootEntry 1", "foo" , [Microsoft.TraceLoggingDynamic.EventOutType]::Default, 0)

    $null = $eb.AddStruct("MyStruct", 2)
    $eb.AddUnicodeString("field 1", "value 1", [Microsoft.TraceLoggingDynamic.EventOutType]::Default, 0)
    $eb.AddUnicodeString("field 2", "value 2", [Microsoft.TraceLoggingDynamic.EventOutType]::Default, 0)

    $eb.AddUnicodeString("RootEntry 2", "bar" , [Microsoft.TraceLoggingDynamic.EventOutType]::Default, 0)

    $null = $l.Write($eb)

    # $l.StringTest(
    #     "string 1",
    #     "Caf$([char]0xE9)s",
    #     "string 3 with unicode $([Char]::ConvertFromUtf32(0x1F3B5))"
    # )
}
finally {
    $l.Dispose()
}
