# PSEtw Event Args
## about_PSEtwEventArgs

# SHORT DESCRIPTION
Each event object returned by this module contains some properties that describe the event.
This document will go through each of these properties and describe what they represent.

# LONG DESCRIPTION
The `EtwEventArgs` object is designed to present a user friendly object that contains the event data returned by the ETW trace session.
Internally it uses the following Win32 structures:

+ [EVENT_RECORD](https://learn.microsoft.com/en-us/windows/win32/api/evntcons/ns-evntcons-event_record)

+ [EVENT_HEADER](https://learn.microsoft.com/en-us/windows/win32/api/evntcons/ns-evntcons-event_header)

+ [EVENT_DESCRIPTOR](https://learn.microsoft.com/en-us/windows/win32/api/evntprov/ns-evntprov-event_descriptor)

+ [TRACE_EVENT_INFO](https://learn.microsoft.com/en-us/windows/win32/api/tdh/ns-tdh-trace_event_info)

+ [EVENT_PROPERTY_INFO](https://learn.microsoft.com/en-us/windows/win32/api/tdh/ns-tdh-event_property_info)

See the documentation of these structures to find out more information on the properties that `EtwEventArgs` exposed.

The `EtwEventArgs` object contains the following properties:

|Name|Type|Description|
|-|-|-|
|ProviderId|Guid|The provider guid|
|ProviderName|string?|The name of the provider|
|ProcessId|int|The process that emitted the event|
|ThreadId|int|The thread that emitted the event|
|TimeStamp|DateTime|When the event was emitted|
|ActivityId|Guid|Identifier that can relate multiple events to each other|
|Id|short|A unique identifier for manifest based events|
|Version|byte|The version of the manifest definition for the `Id`|
|Channel|byte|Designed to enable special event processing|
|ChannelName|string?|The name of the channel|
|Level|byte|The event's severity or importance|
|LevelName|string?|The name of the level|
|OpCode|byte|Marks the event with special semantics according to the provider|
|OpCodeName|string?|The name of the OpCode|
|Task|int16|Identifiers the event with a provider specific value|
|TaskName|string?|The name of the task|
|Keyword|int64|The keywords for the event|
|KeywordNames|string[]|The names of each keyword for the event|
|Tags|int|Custom tag for the event that is provider specific|
|EventData|byte[]|Only populated when `-IncludeRawData` was set, this is the event user data as a byte array|
|Properties|EventPropertyInfo[]|The properties of the event, see below for more information|
|EventMessage|string?|The event message, if one is present, will contain the error message if there was a failure parsing the event|

_Note: `?` after the type means the value can be `null` in certain conditions._

The `*Name` properties will only be set if the event contains the required event data and could be parsed.
The numeric values for each of those properties will always be set for every event though.

The properties of the event are contained in the `Properties` property which is an array of `EventPropertyInfo` objects.
The `EventPropertyInfo` object contains the following properties:

|Name|Type|Description|
|-|-|-|
|Name|string?|The name of the property, can be `null` if no name is provided|
|Value|object|The property value, the type of this depends on the property itself|
|DisplayValue|string|The string formatted value as provided by Windows|
|Tags|int|Additional semantic data of the property, the meaning of the value is dependent on the event provider|

The known types that `Value` can be set to are:

+ Various numeric types `byte`, `sbyte`, `int16`, `uint16`, `int32`, `uint32`, `int64`, `uint64`, `float`, `double`, `decimal`

+ `byte[]`

+ `string`

+ `DateTime`

+ `bool`

+ `Guid`

+ `System.Net.IPAddress`

+ `System.Net.SocketAddress`

+ `System.Xml.XmlDocument`

The value can also be an array of these types or an array of `EventPropertyInfo` objects if the value was a structure itself.
