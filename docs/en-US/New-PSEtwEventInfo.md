---
external help file: PSEtw.dll-Help.xml
Module Name: PSEtw
online version: https://www.github.com/jborean93/PSEtw/blob/main/docs/en-US/New-PSEtwEventInfo.md
schema: 2.0.0
---

# New-PSEtwEventInfo

## SYNOPSIS
Create a trace info object used for filtering traces with an ETW event.

## SYNTAX

```
New-PSEtwEventInfo -Provider <ProviderStringOrGuid> [-KeywordsAny <KeywordsStringOrLong[]>]
 [-KeywordsAll <KeywordsStringOrLong[]>] [-Level <LevelStringOrInt>] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet is used to create a trace info object that can be used to filter ETW trace events started with [Register-PSEtwEvent](./Register-PSEtwEvent.md) or [Trace-PSEtwEvent](./Trace-PSEtwEvent.md).
It describes the provider to trace as well as any keywords or levels to filter by in the trace itself.

Multiple trace info objects can be provided to a trace event loop allowing the caller to capture traces using multiple criteria values.

This cmdlet supports tab completion to autocomplete and list the available values for each parameter.
The default value for Windows PSReadLine to list all parameters and a description is to use `ctrl + space` after the parameter name like `New-PSEtwEventInfo -Provider <ctrl + space>`.
Each parameter matches with a simple wildcard pattern to filter the available options further.
The `-KeywordsAll`, `-KeywordsAny`, and `-Level` parameters can display provider specific values if `-Provider` is already set in the call when tab completing those values.

## EXAMPLES

### Example 1
```powershell
PS C:\> $eventParams = @{
    Provider = 'PowerShellCore'
    KeywordsAny = 'Runspace', 'Pipeline'
    Level = 'Verbose'
}
PS C:\> $info = New-PSEtwEventInfo @eventParams
PS C:\> $info | Trace-PSEtwEvent
```

Creates a event info that can be used to filter events for the `PowerShellCore` provider, the keywords `Runspace`, `Pipeline`, and all events lower than the `Verbose` level.

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
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Provider
The provider name or guid to retrieve events for.
This parameter supports tab completion to retrieve all available providers that have been registered on the system.
Trace Logger providers can be specified by name but as they are not registered on the system by name they cannot be validated when creating the filter.
If set to a registered provider, other parameters tab completion can retrieve values specific to that provider for example `New-PSEtwEventInfo -Provider PowerShellCore -KeywordsAny <ctrl+space>`.

```yaml
Type: ProviderStringOrGuid
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### PSEtw.Shared.ProviderStringOrGuid
The provider string or guid can be passed as input using the property name `Provider`.

### PSEtw.Shared.KeywordsStringOrLong[]
The keywords all/any string or numeric flag value can be passed as input using the property name `KeywordsAll` or `KeywordsAny`.

### PSEtw.Shared.LevelStringOrInt[]
The level name or numeric flag value can be passed as input using the property name `Level`.

## OUTPUTS

### PSEtw.Shared.EtwTraceInfo
This cmdlet outputs an `EtwTraceInfo` that contains trace details to use when starting a trace. It can be provided using the `-TraceInfo` cmdlet or piped into the [Register-PSEtwEvent](./Register-PSEtwEvent.md) or [Trace-PSEtwEvent](./Trace-PSEtwEvent.md) cmdlets.

## NOTES

## RELATED LINKS

[EnableTraceEx2](https://learn.microsoft.com/en-us/windows/win32/api/evntrace/nf-evntrace-enabletraceex2)
