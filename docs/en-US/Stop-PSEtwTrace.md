---
external help file: PSEtw.dll-Help.xml
Module Name: PSEtw
online version: https://www.github.com/jborean93/PSEtw/blob/main/docs/en-US/Stop-PSEtwTrace.md
schema: 2.0.0
---

# Stop-PSEtwTrace

## SYNOPSIS
Stops an active PSEtw trace session.

## SYNTAX

```
Stop-PSEtwTrace [-InputObject] <EtwEventArgs> [<CommonParameters>]
```

## DESCRIPTION
Stops a PSEtw trace session started by [Trace-PSEtwEvent](./Trace-PSEtwEvent.md).
The trace session associated with the event provided to this cmdlet is the one that will be stopped.

## EXAMPLES

### Example 1 - Stop a trace after receiving an event
```powershell
PS C:\> Trace-PSEtwEvent -Provider MyProvider | ForEach-Object {
    $_

    if ($_.Header.Descriptor.Id -eq 10) {
        $_ | Stop-PSEtwTrace
    }
}
```

Will capture events for the provider `MyProvider` until an event with the `Id` of `10` is received.
The trace is stopped by piping the event into `Stop-PSEtwTrace`.

## PARAMETERS

### -InputObject
The event object that was created by the trace session that should be stopped.

```yaml
Type: EtwEventArgs
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### PSEtw.Shared.EtwEventArgs
The trace event can be provided as pipeline input.

## OUTPUTS

### None
This cmdlet does not output anything.

## NOTES

## RELATED LINKS
