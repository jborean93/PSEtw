---
external help file: PSEtw.dll-Help.xml
Module Name: PSEtw
online version: https://www.github.com/jborean93/PSEtw/blob/main/docs/en-US/Remove-PSEtwSession.md
schema: 2.0.0
---

# Remove-PSEtwSession

## SYNOPSIS
Removes an existing ETW Trace Session.

## SYNTAX

### Name (Default)
```
Remove-PSEtwSession [-SessionName] <String[]> [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Default
```
Remove-PSEtwSession [-Default] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Removes an existing ETW Trace Session on the current host.
This is a cut down cmdlet of [Stop-EtwTraceSession](https://learn.microsoft.com/en-us/powershell/module/eventtracingmanagement/stop-etwtracesession?view=windowsserver2022-ps) which can also be used to remove an ETW Trace Session.

## EXAMPLES

### Example 1 - Create and remove Trace Session
```powershell
PS C:\> $session = New-PSEtwSession -Name MySession
PS C:\> $session | Remove-PSEtwSession
```

Creates a new ETW Trace Session called `MySession` then removes it.

## PARAMETERS

### -Default
Removes the default Trace Session used by `PSEtw`.
The default Trace Session is used if no `-SessionName` is specified with [Registser-PSEtwEvent](./Register-PSEtwEvent.md) or [Trace-PSEtwEvent](./Trace-PSEtwEvent.md).

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
The ETW Trace Session name to remove.

```yaml
Type: String[]
Parameter Sets: Name
Aliases: Name

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
The session names to remove.

## OUTPUTS

### None
This cmdlet does not output any objects.

## NOTES

## RELATED LINKS

[ControlTraceW](https://learn.microsoft.com/en-us/windows/win32/api/evntrace/nf-evntrace-controltracew)
