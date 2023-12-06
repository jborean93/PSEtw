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
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### Multi
```
Trace-PSEtwEvent [-SessionName <String>] -TraceInfo <EtwEventInfo[]> [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -KeywordsAll
{{ Fill KeywordsAll Description }}

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
{{ Fill KeywordsAny Description }}

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
{{ Fill Level Description }}

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
{{ Fill Provider Description }}

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
{{ Fill SessionName Description }}

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
{{ Fill TraceInfo Description }}

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

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
