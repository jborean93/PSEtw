---
external help file: PSEtw.dll-Help.xml
Module Name: PSEtw
online version: https://www.github.com/jborean93/PSEtw/blob/main/docs/en-US/Register-PSEtwEvent.md
schema: 2.0.0
---

# Register-PSEtwEvent

## SYNOPSIS
Subscribes to the events generated by an ETW Trace Session.

## SYNTAX

### Single (Default)
```
Register-PSEtwEvent [-Action <ScriptBlock>] [-Forward] [-MaxTriggerCount <Int32>] [-MessageData <PSObject>]
 [-SourceIdentifier <String>] [-SupportEvent] [-SessionName <String>] -Provider <ProviderStringOrGuid>
 [-KeywordsAny <KeywordsStringOrLong[]>] [-KeywordsAll <KeywordsStringOrLong[]>] [-Level <LevelStringOrInt>]
 [-IncludeRawData] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### Multi
```
Register-PSEtwEvent [-Action <ScriptBlock>] [-Forward] [-MaxTriggerCount <Int32>] [-MessageData <PSObject>]
 [-SourceIdentifier <String>] [-SupportEvent] [-SessionName <String>] -TraceInfo <EtwEventInfo[]>
 [-IncludeRawData] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
This cmdlet can be used to subscribe ETW traces as PowerShell events similar to how [Register-ObjectEvent](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/register-objectevent?view=powershell-7.4) works for .NET object events.
When a subscribed trace event is raised, it is added to the event queue in your session.
Events in the queue can be retrieved by [Get-Event](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/get-event?view=powershell-7.4) and/or [Wait-Event](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/wait-event?view=powershell-7.4).

The parameters for this cmdlet define what ETW providers to register to, the keywords, and levels for that provider to subscribe to.
It is possible to use [New-PSEtwEventInfo](./New-PSEtwEventInfo.md) to define multiple filters when creating an ETW trace.

When an ETW event is registered, an event subscriber is added to your session.
To get the event subscribers in the session, use the [Get-EventSubscriber](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/get-eventsubscriber?view=powershell-7.4) cmdlet.
To cancel/stop the subscription, use the [Unregister-Event](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/unregister-event?view=powershell-7.4) cmdlet.
It is important that any registered subscribers are unregistered when no longer needed so that the trace session is no longer using any system resources.

The [Trace-PSEtwEvent](./Trace-PSEtwEvent.md) cmdlet can be used to retrieve ETW events in realtime without having to go through PowerShell's eventing system.
See [about_PSEtwEventArgs](./about_PSEtwEventArgs.md) for more information on the structure of the event data that is set in `SourceEventArgs`.

## EXAMPLES

### Example 1 - Register PowerShellCore pipeline events
```powershell
PS C:\> $sourceId = [Guid]::NewGuid()
PS C:\> Register-PSEtwEvent -Provider PowerShellCore -KeywordsAll Pipeline -SourceIdentifier $sourceId
PS C:\> $event = Wait-Event -SourceIdentifier $sourceId
PS C:\> $event.SourceEventArgs | ConvertTo-Json
PS C:\> $event | Remove-Event
PS C:\> Unregister-Event -SourceIdentifier $sourceId
```

Registers an event subscriber for the `PowerShellCore` provider with the keyword `Pipeline`.
It then waits for the first event and prints the event data under `SourceEventArgs` as a Json string.
This data contains the `Header` being the event descriptor info and the `Info` property being the event data.
Once done the event is removed from the queue with `Remove-Event` and then the event subscriber is unregistered.

### Example 2 - Use an Action to process each event
```powershell
PS C:\> $etwEvent = Register-PSEtwEvent -Provider PowerShellCore -KeywordsAll Pipeline -Action {
    $EventArgs | ConvertTo-Json -WarningAction SilentlyContinue | Out-Host
}
PS C:\> ... # The pipeline must be free for the events to be processed
PS C:\> Unregister-Event -SourceIdentifier $etwEvent.Name
```

Register an event and runs the action for each received event.
This action will output the event data as a Json string to the host when received.
As PowerShell events only run when the pipeline is free, the action will only run when nothing else is running or a cmdlet like `Start-Sleep`, `Wait-Event` has freed the pipeline.
The event subscription still needs to be unregistered to ensure the ETW trace session is no longer processing these events.

### Example 3 - Register ETW event on remote PSSession
```powershell
PS C:\> $session = New-PSSession -ComputerName remote-host
PS C:\> $eventParams = @{
    InputObject = $session.Runspace.Events.ReceivedEvents
    EventName = 'PSEventReceived'
    SourceIdentifier = [Guid]::NewGuid()
}
PS C:\> Register-ObjectEvent @eventParams
PS C:\> $remoteSourceId = Invoke-Command -Session $session -ScriptBlock {
    Import-Module -Name PSEtw

    $sourceId = [Guid]::NewGuid()
    Register-PSEtwEvent -Provider PowerShellCore -KeywordsAll Runspace -Forward -SourceIdentifier $sourceId
    $sourceId
}
PS C:\> while ($true) {
    $e = Wait-Event -SourceIdentifier $eventParams.SourceIdentifier
    $e | Remove-Event

    $e.SourceEventArgs.SourceEventArgs.SerializedRemoteEventArgs
}
PS C:\> Invoke-Command -Session $session -ScriptBlock {
    Unregister-Event -SourceIdentifier $args[0]
} -ArgumentList $remoteSourceId
PS C:\> Unregister-Event -SourceIdentifier $eventParams.SourceIdentifier
PS C:\> $session | Remove-PSSession
```

Creates a PSSession to the host `remote-host` and starts a trace for the `PowerShellCore` provider with the keyword `Runspace`.
These events will be forwarded to the local machine as they are received.
Each event is then picked up to the `PSEventReceived` event and the event details under the `SerializedRemoteEventArgs`.
Once the event is no longer needed the remote event subscriber is unregistered, the local event subscriber is also registered, and the PSSession closed.

It is important to note that the remote data values will be deserialized and any deep objects lost as part of the deserialization process.
To get a richer object more complex code is needed.
The target host must have the `PSEtw` module installed for this to work, it is not needed on the local computer.

## PARAMETERS

### -Action
Specifies the commands to handle the ETW trace.
The commands in the Action run when an event is raised, instead of sending the event to the event queue.
Enclose the commands in braces `{ }` to create a script block.

The value of the Action parameter can include the following automatic variables:

+ `$Event` - The full event details, includes `SourceEventArgs`, `Sender`, and other PowerShell event data

+ `$EventSubscriber` - The PowerShell event subscriber for the current event

+ `$Sender` - The event sender (same as `$Event.Sender`), this currently has no use

+ `$EventArgs` - The event data (same as `$Event.SourceEventArgs`), see [about_PSEtwEventArgs](./about_PSEtwEventArgs.md) to see more information on this object

+ `$Args` - The `$Sender` and `$EventArgs` value that can be provided through params as positional arguments

As `$Args` are supplied positionally, the script block can be run with a param block that accepts two arguments.
The first being the `$Sender` and the second being `$EventArgs`.
Typically the `$EventArgs` is the variable of most interest to a user as it contains the ETW trace information.

These variables provide information about the event to the Action script block.
For more information, see [about_Automatic_Variables](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_automatic_variables?view=powershell-7.4).

When you specify an action, `Register-PSEtwEvent` returns an event job object that represents that action.
You can use the Job cmdlets to manage the event job.

```yaml
Type: ScriptBlock
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Forward
Indicates that the cmdlet sends events for this subscription to a remote session.
Use this parameter when you are registering for events on a remote computer or in a remote session.

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

### -MaxTriggerCount
Specifies the maximum number of times an event can be triggered.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MessageData
Specifies any additional data to be associated with this event subscription.
The value of this parameter appears in the MessageData property of all events associated with this subscription.

```yaml
Type: PSObject
Parameter Sets: (All)
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
If set to a registered provider, other parameters tab completion can retrieve values specific to that provider for example `Register-PSEtwEvent -Provider PowerShellCore -KeywordsAny <ctrl+space>`.

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
A trace session can be created with [New-PSEtwSession](./New-PSEtwSession.md).
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

### -SourceIdentifier
Specifies a name that you select for the subscription.
The name that you select must be unique in the current session.
The default value is the GUID that PowerShell assigns.

The value of this parameter appears in the value of the SourceIdentifier property of the subscriber object and all event objects associated with this subscription.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SupportEvent
Indicates that the cmdlet hides the event subscription.
Use this parameter when the current subscription is part of a more complex event registration mechanism and should not be discovered independently.

To view or cancel a subscription that was created with the SupportEvent parameter, use the Force parameter of the `Get-EventSubscriber` and `Unregister-Event` cmdlets.

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

### None
By default, this cmdlet returns no output.

### PSEventJob
When you use the Action parameter, this cmdlet returns a PSEventJob object.

## NOTES

Events, event subscriptions, and the event queue exist only in the current session. If you close the current session, the event queue is discarded and the event subscription is canceled.
The underlying ETW trace session exists beyond the current session, failing to call `Unregister-Event` for the event subscription will mean the ETW trace session will continue to run.

## RELATED LINKS
