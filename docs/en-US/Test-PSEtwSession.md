---
external help file: PSEtw.dll-Help.xml
Module Name: PSEtw
online version: https://www.github.com/jborean93/PSEtw/blob/main/docs/en-US/Test-PSEtwSession.md
schema: 2.0.0
---

# Test-PSEtwSession

## SYNOPSIS
Tests if an ETW Trace Session exists or not.

## SYNTAX

### Name (Default)
```
Test-PSEtwSession [-SessionName] <TraceSessionOrString[]> [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

### Default
```
Test-PSEtwSession [-Default] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Tests whether the ETW Trace Session specified exists or not on the current host.

## EXAMPLES

### Example 1 - Checks if a trace session exists before removing it
```powershell
PS C:\> if (Test-PSEtwSession -SessionName MySession) {
    Remove-PSEtwSession -SessionName MySession
}
```

Checks if the session specified exists before removing it.

## PARAMETERS

### -Default
Tests whether the default Trace Session used by PSEtw exists or not.
The default Trace Session is used if no `-SessionName` is specified with [Register-PSEtwEvent](./Register-PSEtwEvent.md) or [Trace-PSEtwEvent](./Trace-PSEtwEvent.md).

```yaml
Type: SwitchParameter
Parameter Sets: Default
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

### -SessionName
The name of the ETW Trace Session to check.

```yaml
Type: TraceSessionOrString[]
Parameter Sets: Name
Aliases: Name

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
The name(s) of the ETW Trace Session to check.

## OUTPUTS

### System.Boolean
A boolean value representing whether the ETW Trace Session exists or not.

## NOTES

## RELATED LINKS
