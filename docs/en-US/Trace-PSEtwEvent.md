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
 [<CommonParameters>]
```

### Multi
```
Trace-PSEtwEvent [-SessionName <String>] -TraceInfo <EtwEventInfo[]> [<CommonParameters>]
```

## DESCRIPTION
Starts an ETW trace which outputs trace events as they are received as object outputs.
This like [Register-PSEtwEvent](./Register-PSEtwEvent.md) except that the events are outputted from the cmdlet rather than requiring `Get-Event` or `Wait-Event`.
A trace will continue to run indefinitely until either the pipeline has been stopped with `ctrl+c` or an event outputted by this cmdlet is piped into [Stop-PSEtwTrace](./Stop-PSEtwTrace.md).

The parameters for this cmdlet define what ETW providers to register to, the keywords, and levels for that provider to subscribe to.
It is possible to use [New-PSEtwEventInfo](./New-PSEtwEventInfo.md) to define multiple filters when creating an ETW trace.

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

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
This cmdlet outputs each event as an output object.
It can be used with other cmdlets like `ForEach-Object` to process the event in realtime.
The event can be passed into [Stop-PSEtwTrace](./Stop-PSEtwTrace.md) to stop the current trace.

## NOTES

## RELATED LINKS
