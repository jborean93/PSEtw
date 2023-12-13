---
external help file: PSEtw.dll-Help.xml
Module Name: PSEtw
online version: https://www.github.com/jborean93/PSEtw/blob/main/docs/en-US/New-PSEtwSession.md
schema: 2.0.0
---

# New-PSEtwSession

## SYNOPSIS
Creates a new ETW Trace Session.

## SYNTAX

### Name (Default)
```
New-PSEtwSession [-SystemLogger] [-SessionName] <TraceSessionOrString[]> [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Default
```
New-PSEtwSession [-Default] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Creates a new ETW Trace Session that can be used by other cmdlets in this module to perform real time tracing.
This is a cut down cmdlet of [Start-EtwTraceSession](https://learn.microsoft.com/en-us/powershell/module/eventtracingmanagement/start-etwtracesession?view=windowsserver2022-ps) provided by this module to create a new session that is usable with [Register-PSEtwEvent](./Register-PSEtwEvent.md) or [Trace-PSEtwEvent](./Trace-PSEtwEvent.md).

A session is global to the host and will persist even after the current process has ended.
Use [Remove-PSEtwSession](./Remove-PSEtwSession.md) or [Stop-EtwTraceSession](https://learn.microsoft.com/en-us/powershell/module/eventtracingmanagement/stop-etwtracesession?view=windowsserver2022-ps) to remove a trace session on the host.

## EXAMPLES

### Example 1 - Create and remove Trace Session
```powershell
PS C:\> $session = New-PSEtwSession -Name MySession
PS C:\> $session | Remove-PSEtwSession
```

Creates a new ETW Trace Session called `MySession` then removes it.

### Example 2 - Creates the default PSEtw Session
```powershell
PS C:\> New-PSEtwSession -Default
```

Creates the default ETW Trace Session used by this module when no explicit session was specified.
This requires admin access to perform as it is created as a System logger.

### Example 3 - Create system logger session
```powershell
PS C:\> New-PSEtwSession -Name MySession -SystemLogger
```

Creates a new ETW Trace Session called `MySession` with the flag `EVENT_TRACE_SYSTEM_LOGGER_MODE` set.
A system logger session can trace other processes but requires admin access to create.

## PARAMETERS

### -Default
Creates the default ETW Trace Session used by this module called `PSEtw`.
Always implies `-SystemLogger` is set to ensure the trace session can trace other processes.
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
The name of the session to be created.
A session name cannot exceed 1024 characters, is case insensitive, and must be unique.

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

### -SystemLogger
Will create the trace session with the `EVENT_TRACE_SYSTEM_LOGGER_MODE` flag.
This flag allows the trace session to trace other processes and not just the current process.
This requires the current user to be a member of the local Administrators group.

```yaml
Type: SwitchParameter
Parameter Sets: Name
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
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
The names of the session to create.

## OUTPUTS

### PSEtw.Shared.EtwTraceSession
An `EtwTraceSession` object that can be used with other cmdlets in this module. Disposing this object will dispose the handle but the session will still persist unless removed with Remove-PSEtwSession (./Remove-PSEtwSession.md).

## NOTES

## RELATED LINKS

[StartTraceW](https://learn.microsoft.com/en-us/windows/win32/api/evntrace/nf-evntrace-starttracew)
