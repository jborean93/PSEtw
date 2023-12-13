---
external help file: PSEtw.dll-Help.xml
Module Name: PSEtw
online version: https://www.github.com/jborean93/PSEtw/blob/main/docs/en-US/Trace-PSEtwEvent.md
schema: 2.0.0
---

# Trace-PSEtwEvent

## SYNOPSIS
Starts an ETW Trace for the provider specified.

## SYNTAX

### Single (Default)
```
Trace-PSEtwEvent [-SessionName <String>] -Provider <ProviderStringOrGuid>
 [-KeywordsAny <KeywordsStringOrLong[]>] [-KeywordsAll <KeywordsStringOrLong[]>] [-Level <LevelStringOrInt>]
 [-IncludeRawData] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### Multi
```
Trace-PSEtwEvent [-SessionName <String>] -TraceInfo <EtwEventInfo[]> [-IncludeRawData]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Starts an ETW trace which outputs trace events as they are received as object outputs.
This like [Register-PSEtwEvent](./Register-PSEtwEvent.md) except that the events are outputted from the cmdlet rather than requiring `Get-Event` or `Wait-Event`.
A trace will continue to run indefinitely until either the pipeline has been stopped with `ctrl+c` or an event outputted by this cmdlet is piped into [Stop-PSEtwTrace](./Stop-PSEtwTrace.md).

The parameters for this cmdlet define what ETW providers to register to, the keywords, and levels for that provider to subscribe to.
It is possible to use [New-PSEtwEventInfo](./New-PSEtwEventInfo.md) to define multiple filters when creating an ETW trace.
See [about_PSEtwEventArgs](./about_PSEtwEventArgs.md) for more information on the structure of the event data that is set in `SourceEventArgs`.

## EXAMPLES

### Example 1 - Trace PowerShellCore Runspace events
```powershell
PS C:\> Trace-PSEtwEvent -Provider PowerShellCore -KeywordsAll Runspace
```

Starts an interactive trace that outputs the events for `PowerShellCore` with the keyword `Runspace`.
This will continue to run until the caller stops the pipeline with `ctrl+c`.

### Example 2 - Trace WSMan Authentication events and output as JSON
```powershell
PS C:\> Trace-PSEtwEvent -Provider Microsoft-Windows-WinRM -KeywordsAll Keyword.Server, Keyword.Security |
    ForEach-Object { $_ | ConvertTo-Json -Depth 3 }
```

Captures authentication trace events for the WinRM listener and outputs the event data as a Json string.
The `Keyword.Server` and `Keyword.Security` keywords are used when filtering the events.
Note that the `Keyword.` prefix is how the `WinRM` provider has registered its keyword names, not all providers follow this standard.
Use tab completion with the `-KeywordsAll` or `-KeywordsAny` parameters with a registered provider set to view the available keywords.

### Example 3 - Trace events and stop the trace when an event is received
```powershell
PS C:\> Trace-PSEtwEvent -Provider PowerShellCore -KeywordsAll Runspace | ForEach-Object {
    $_
    if ($_.Id -eq -24574) {
        $_ | Stop-PSEtwTrace
    }
}
```

Captures all `Runspace` traces for `PowerShellCore` and will stop the trace when the event with the ID `-24574` is received.
This event is the `PowerShell Console Startup` event.

## PARAMETERS

### -IncludeRawData
Stores the raw event data as part of the `EventData` property of the returned event arg object.
If not set this property will be an empty array.
This is useful when needing to debug the event data if the parser failed to extract the event information.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -KeywordsAll
Restrict the events for the specified provider to only the ones that match all the keywords specified here.
This filter does not apply to events that do not have a keyword associated with it.

The keyword can either be specified as a 64-bit integer value which are combined together or as a string representing the keyword.
The keyword strings are dependent on the provider that was specified and what keywords it defines through its manifest.
Trace Logging providers that aren't registered on the system cannot be filtered by name, the integer value must be specified for these providers.
This parameter supports tab completion to retrieve the keywords for a registered provider if one is set by `-Provider`.
The value `*` represents the numeric value `0xFFFFFFFFFFFFFFFF` which is all keywords set.

```yaml
Type: KeywordsStringOrLong[]
Parameter Sets: Single
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -KeywordsAny
Restrict the events for the specified provider to only the ones that match any of the keywords specified here.
This filter does not apply to events that do not have a keyword associated with it.

The keyword can either be specified as a 64-bit integer value which are combined together or as a string representing the keyword.
The keyword strings are dependent on the provider that was specified and what keywords it defines through its manifest.
Trace Logging providers that aren't registered on the system cannot be filtered by name, the integer value must be specified for these providers.
This parameter supports tab completion to retrieve the keywords for a registered provider if one is set by `-Provider`.
The value `*` represents the numeric value `0xFFFFFFFFFFFFFFFF` which is all keywords set.

```yaml
Type: KeywordsStringOrLong[]
Parameter Sets: Single
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Level
Restricts the events to only ones with a level that is equal to or less than the level specified.
Builtin levels are:

+ `0` - `LogAlways` - only events with `LogAlways` will be emitted
+ `1` - `Critical`
+ `2` - `Error`
+ `3` - `Warning`
+ `4` - `Info`
+ `5` - `Verbose`
+ `0xFF` - `*`

Some providers may implement custom levels which can be specified by the numeric value or by name.
Use tab completion with `-Provider` set to see the known levels for the provider in use.
The level `*` or `0xFF` is set then all levels will be captured.
If no level is set then the default is `4 (Info)`.

```yaml
Type: LevelStringOrInt
Parameter Sets: Single
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
New common parameter introduced in PowerShell 7.4.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Provider
The provider name or guid to retrieve events for.
This parameter supports tab completion to retrieve all available providers that have been registered on the system.
Trace Logger providers can be specified by name but as they are not registered on the system by name they cannot be validated when creating the filter.
If set to a registered provider, other parameters tab completion can retrieve values specific to that provider for example `Trace-PSEtwEvent -Provider PowerShellCore -KeywordsAny <ctrl+space>`.

```yaml
Type: ProviderStringOrGuid
Parameter Sets: Single
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SessionName
Register the events on the ETW trace session specified.
A trace session can be created with [New-PStwSession](./New-PSEtwSession.md).
When running the process as an administrator, a default ETW Trace Session called `PSEtw` will be created and used.
Non-administrative sessions will attempt to open this session which may work depending on the permissions applied to the trace session and what groups the user is a member of.

```yaml
Type: String
Parameter Sets: (All)
Aliases: Name

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TraceInfo
Specifies the trace info objects to filter by.
These objects can be created by [New-PSEtwEventInfo](./New-PSEtwEventInfo.md).
Each trace info object specifies what traces to register with.

```yaml
Type: EtwEventInfo[]
Parameter Sets: Multi
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### PSEtw.Shared.EtwEventInfo
This cmdlet accepts an `EtwEventInfo` object through the pipeline as part of the `-TraceInfo` parameter.

## OUTPUTS

### PSEtw.Shared.EtwEventArgs
This cmdlet outputs each event as an output object. It can be used with other cmdlets like `ForEach-Object` to process the event in realtime. The event can be passed into `Stop-PSEtwTrace` to stop the current trace. See `about_PSEtwEventArgs` for more information on the structure of the event data that is set in `SourceEventArgs`.

## NOTES

## RELATED LINKS
