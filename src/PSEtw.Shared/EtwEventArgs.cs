using PSEtw.Share;
using PSEtw.Shared.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace PSEtw.Shared;

// A lot of this logic is derived from the following.
// https://learn.microsoft.com/en-us/windows/win32/etw/using-tdhformatproperty-to-consume-event-data
// https://learn.microsoft.com/en-us/windows/win32/etw/retrieving-event-metadata

/// <summary>
/// Event args representing the raw ETW event record.
/// </summary>
public sealed class EtwEventArgs : EventArgs
{
    /// <summary>
    /// The Guid that uniquely identifies the provider that logged the event.
    /// </summary>
    public Guid ProviderId { get; }

    /// <summary>
    /// The name of the provider that logged the event.
    /// This may be null if the event did not provider this information.
    /// </summary>
    public string? ProviderName { get; }

    /// <summary>
    /// Identifies the process that generated the event.
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    /// Identifies the thread that generated the event.
    /// </summary>
    public int ThreadId { get; }

    /// <summary>
    /// The time that the event occurred as a UTC DateTime.
    /// </summary>
    public DateTime TimeStamp { get; }

    /// <summary>
    /// Identifier that related two events.
    /// </summary>
    public Guid ActivityId { get; }

    /// <summary>
    /// Identifier for manifest-based events.
    /// For TraceLogging events this is typically set to 0 and can be ignored.
    /// </summary>
    public short Id { get; }

    /// <summary>
    /// Indicates a revision of the definition of an event with a particular id.
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// Number used to enable special event processing.
    /// Values less than 16 are typically reserved by Microsoft, values above
    /// can be given user-defined semantics.
    /// </summary>
    public byte Channel { get; }

    /// <summary>
    /// The name of the channel if provided by the event.
    /// </summary>
    public string? ChannelName { get; }

    /// <summary>
    /// Describes an event's severity or importance.
    /// </summary>
    public byte Level { get; }

    /// <summary>
    /// The name of the level if provided by the event.
    /// </summary>
    public string? LevelName { get; }

    /// <summary>
    /// Mark events with special semantics. Values from 10 through 239 can be
    /// given user-defined semantics.
    /// </summary>
    public byte OpCode { get; }

    /// <summary>
    /// The name of the OpCode if provided by the event.
    /// </summary>
    public string? OpCodeName { get; }

    /// <summary>
    /// Identifies the event with a provider specific value.
    /// </summary>
    public short Task { get; }

    /// <summary>
    /// The name of the task if provided by the event.
    /// </summary>
    public string? TaskName { get; }

    /// <summary>
    /// A bitmask used to indicate an event's membership in a set of event
    /// categories. The top 16 bits of a keyword (0xFFFF000000000000) are
    /// defined by Microsoft. The bottom 48 bits of a keyword
    /// (0x0000FFFFFFFFFFFF) are defined by the event provider. Events with a
    /// Keyword of 0 will typically bypass keyword-based filtering.
    /// </summary>
    public long Keyword { get; }

    /// <summary>
    /// The names of each keyword set if provided by the event.
    /// </summary>
    public string[] KeywordNames { get; } = Array.Empty<string>();

    /// <summary>
    /// Provides additional semantic data with an event. The semantics of any
    /// values in this property are defined by the event provider.
    /// </summary>
    public int Tags { get; set; }

    /// <summary>
    /// The raw event data as a byte array. This can be used for manual
    /// decoding of data or debugging in the case of a failure when unpacking
    /// an event info object.
    /// </summary>
    public byte[] EventData { get; }

    /// <summary>
    /// The event properties with the name, value, and display value set.
    /// </summary>
    public EventPropertyInfo[] Properties { get; } = Array.Empty<EventPropertyInfo>();

    /// <summary>
    /// A description of the event based on the event data provided. If there
    /// was a failure when attempting to unpack the data this will contain the
    /// error message for debugging purposes.
    /// </summary>
    public string? EventMessage { get; }

    /// <summary>
    /// Used by Trace-PSEtwEvent and Stop-PSEtwTrace.
    /// </summary>
    internal CancellationTokenSource? CancelToken { get; set; }

    internal EtwEventArgs(ref Advapi32.EVENT_RECORD record, bool includeEventData)
    {
        Advapi32.EVENT_HEADER header = record.EventHeader;
        Advapi32.EVENT_DESCRIPTOR descriptor = header.EventDescriptor;
        ReadOnlySpan<byte> userData;
        unsafe
        {
            userData = new((byte*)record.UserData, record.UserDataLength);
        }
        EventData = includeEventData ? userData.ToArray() : Array.Empty<byte>();

        ProviderId = header.ProviderId;
        ProcessId = header.ProcessId;
        ThreadId = header.ThreadId;
        TimeStamp = DateTime.FromFileTimeUtc(header.TimeStamp);
        ActivityId = header.ActivityId;

        Id = descriptor.Id;
        Version = descriptor.Version;
        Channel = descriptor.Channel;
        Level = descriptor.Level;
        OpCode = descriptor.Opcode;
        Task = descriptor.Task;
        Keyword = descriptor.Keyword;

        if (record.EventHeader.Flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_STRING_ONLY))
        {
            EventMessage = UnmanagedHelpers.SpanToString(userData);
            return;
        }

        try
        {
            int bufferSize = 0;
            int res = Tdh.TdhGetEventInformation(
                ref record,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                ref bufferSize);

            if (res == Win32Error.ERROR_NOT_FOUND)
            {
                // No known schema for this event so we can't do anything else.
                return;
            }
            else if (res != Win32Error.ERROR_SUCCESS && res != Win32Error.ERROR_INSUFFICIENT_BUFFER)
            {
                // Some other error we should be reporting back if possible.
                throw new Win32Exception(res);
            }

            nint buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                res = Tdh.TdhGetEventInformation(ref record, 0, IntPtr.Zero, buffer, ref bufferSize);
                Win32Error.ThrowIfError(res);

                unsafe
                {
                    Span<Tdh.TRACE_EVENT_INFO> eventInfoData = new((void*)buffer, 1);
                    ref Tdh.TRACE_EVENT_INFO eventInfo = ref eventInfoData[0];

                    ProviderName = UnmanagedHelpers.ReadPtrStringUni(buffer, eventInfo.ProviderNameOffset);
                    LevelName = UnmanagedHelpers.ReadPtrStringUni(buffer, eventInfo.LevelNameOffset);
                    ChannelName = UnmanagedHelpers.ReadPtrStringUni(buffer, eventInfo.ChannelNameOffset);
                    KeywordNames = UnmanagedHelpers.ReadPtrStringListUni(buffer, eventInfo.KeywordsNameOffset);
                    TaskName = UnmanagedHelpers.ReadPtrStringUni(buffer, eventInfo.TaskNameOffset);
                    OpCodeName = UnmanagedHelpers.ReadPtrStringUni(buffer, eventInfo.OpcodeNameOffset);
                    EventMessage = UnmanagedHelpers.ReadPtrStringUni(buffer, eventInfo.EventMessageOffset);
                    Tags = eventInfo.Tags & 0x0FFFFFFF;
                    Properties = ReadProperties(
                        ref record,
                        ref eventInfo,
                        buffer,
                        userData);

                    if (!string.IsNullOrWhiteSpace(EventMessage) && Properties.Length > 0)
                    {
                        string[] replacements = Properties.Select(v => v.DisplayValue).ToArray();
                        EventMessage = FormatMessage(IntPtr.Add(buffer, eventInfo.EventMessageOffset), replacements);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Win32Exception e)
        {
            EventMessage = $"Failed to unpack EventData: {e.Message} (0x{e.NativeErrorCode:X8})\n{e}";
        }
        catch (Exception e)
        {
            EventMessage = $"Failed to unpack EventData due to unhandled exception: {e.Message}\n{e}";
        }
    }

    private static string FormatMessage(nint source, string[] replacements)
    {
        int flags = (int)(
            FormatMessageFlags.FORMAT_MESSAGE_ALLOCATE_BUFFER |
            FormatMessageFlags.FORMAT_MESSAGE_FROM_STRING |
            FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY
        );

        nint buffer = IntPtr.Zero;
        try
        {
            int count = Kernel32.FormatMessageW(
                flags,
                source,
                0,
                0,
                ref buffer,
                0,
                replacements);

            if (count == 0)
            {
                throw new Win32Exception();
            }

            // FormatMessage seems to add a space, we shouldn't care about
            // any trailing spaces to we trim it.
            return Marshal.PtrToStringUni(buffer, count).TrimEnd();
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    internal static EventPropertyInfo[] ReadProperties(
        ref Advapi32.EVENT_RECORD record,
        ref Tdh.TRACE_EVENT_INFO info,
        nint buffer,
        ReadOnlySpan<byte> userData)
    {
        if (info.PropertyCount == 0)
        {
            return Array.Empty<EventPropertyInfo>();
        }

        List<EventPropertyInfo> results = new();
        nint propertiesOffset = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.TRACE_EVENT_INFO>());
        unsafe
        {
            Span<Tdh.EVENT_PROPERTY_INFO> properties = new((void*)propertiesOffset, info.PropertyCount);

            // Properties can refer back to previous ones to retrieve integer
            // values needed for things like the data/array count. The count
            // and length values are always 16 bits in length.
            Span<short> integerValues = stackalloc short[info.PropertyCount];
            int pointerSize = GetPointerSize(record.EventHeader.Flags);

            for (int i = 0; i < info.TopLevelPropertyCount; i++)
            {
                EventPropertyInfo prop = EventPropertyInfo.Create(
                    ref record,
                    ref info,
                    properties,
                    buffer,
                    i,
                    integerValues,
                    pointerSize,
                    userData,
                    out int consumed);

                // We need to keep track of how much data is consumed from the
                // buffer as properties are read.
                userData = userData.Slice(consumed);
                results.Add(prop);
            }
        }

        return results.ToArray();
    }

    private static int GetPointerSize(HeaderFlags flags)
    {
        if (flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_32_BIT_HEADER))
        {
            return 4;
        }
        else if (flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_64_BIT_HEADER))
        {
            return 8;
        }
        else
        {
            return IntPtr.Size;
        }
    }
}

/// <summary>
/// Event property values.
/// </summary>
public sealed class EventPropertyInfo
{
    /// <summary>
    /// The name of the property, can be null if unset.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The value of the property as parsed by this library.
    /// The type is dependent on the property metadata provided.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// The string representation of the property as provided by Windows.
    /// </summary>
    public string DisplayValue { get; }

    /// <summary>
    /// Provides additional semantic data with a property. The semantics of any
    /// values in this property are defined by the event provider.
    /// </summary>
    public int Tags { get; }

    internal EventPropertyInfo(string? name, object value, string displayValue, int tags)
    {
        Name = name;
        Value = value;
        DisplayValue = displayValue;
        Tags = tags;
    }

    public override string ToString()
    {
        return $"{Name ?? "<noname>"}={DisplayValue}";
    }

    internal static EventPropertyInfo Create(
        ref Advapi32.EVENT_RECORD record,
        ref Tdh.TRACE_EVENT_INFO eventInfo,
        Span<Tdh.EVENT_PROPERTY_INFO> properties,
        nint buffer,
        int index,
        Span<short> integerValues,
        int pointerSize,
        ReadOnlySpan<byte> userData,
        out int consumed)
    {
        ref Tdh.EVENT_PROPERTY_INFO info = ref properties[index];
        string? name = UnmanagedHelpers.ReadPtrStringUni(buffer, info.NameOffset);

        TdhInType inType = (TdhInType)info.InType;
        TdhOutType outType = (TdhOutType)info.OutType;
        short propLength = GetPropertyLength(
            inType,
            outType,
            info.Flags, info.Length, integerValues);
        short arrayCount = GetArrayCount(info.Flags, info.Count, integerValues);
        if (arrayCount == 0)
        {
            throw new InvalidOperationException($"Property '{name}' has an array count of 0");
        }

        // PropertyParamFixedCount is used to signify if an array of 1 value is
        // actually an array.
        bool isArray = arrayCount != 1 ||
            info.Flags.HasFlag(EventPropertyFlags.PropertyParamCount | EventPropertyFlags.PropertyParamFixedCount);

        List<object> values = new();
        List<string> displayValues = new();
        consumed = 0;
        IntPtr mapNameBuffer = IntPtr.Zero;
        try
        {
            mapNameBuffer = GetEventMapInformation(ref record, info.MapNameOffset, buffer);

            for (int i = 0; i < arrayCount; i++)
            {
                if (info.Flags.HasFlag(EventPropertyFlags.PropertyHasCustomSchema))
                {
                    throw new NotImplementedException($"Property '{name}' is a custom schema which is not implemented");
                }
                else if (info.Flags.HasFlag(EventPropertyFlags.PropertyStruct))
                {
                    List<EventPropertyInfo> structProps = new();

                    for (int j = info.InType; j < info.InType + info.OutType; j++)
                    {
                        EventPropertyInfo prop = Create(
                            ref record,
                            ref eventInfo,
                            properties,
                            buffer,
                            j,
                            integerValues,
                            pointerSize,
                            userData,
                            out int structConsumed);
                        structProps.Add(prop);

                        userData = userData.Slice(structConsumed);
                        consumed += structConsumed;
                    }

                    displayValues.Add(string.Join(", ", structProps));
                    values.Add(structProps);
                }
                else
                {
                    object outValue = TdhTypeReader.Transform(
                        name,
                        pointerSize,
                        userData,
                        propLength,
                        inType,
                        outType,
                        out short? storeInteger);

                    string? displayValue = FormatProperty(
                        ref eventInfo,
                        info.Flags,
                        mapNameBuffer,
                        pointerSize,
                        propLength,
                        inType,
                        outType,
                        userData,
                        out short dataConsumed);

                    if (storeInteger != null && !isArray)
                    {
                        integerValues[index] = (short)storeInteger;
                    }

                    userData = userData.Slice(dataConsumed);
                    consumed += dataConsumed;
                    displayValues.Add(displayValue ?? outValue.ToString() ?? string.Empty);
                    values.Add(outValue);
                }
            }
        }
        finally
        {
            if (mapNameBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(mapNameBuffer);
            }
        }

        int tags = 0;
        if (info.Flags.HasFlag(EventPropertyFlags.PropertyHasTags))
        {
            // Tags are only a 28-bit value, the leading byte is reserved
            tags = info.Tags & 0x0FFFFFFF;
        }

        return new(
            name,
            isArray ? values.ToArray() : values[0],
            string.Join(", ", displayValues),
            tags
        );
    }

    private static nint GetEventMapInformation(
        ref Advapi32.EVENT_RECORD record,
        int mapNameOffset,
        nint buffer)
    {
        if (mapNameOffset == 0)
        {
            return IntPtr.Zero;
        }

        nint mapNamePtr = IntPtr.Add(buffer, mapNameOffset);
        int mapNameSize = 0;
        int mapNameRes = Tdh.TdhGetEventMapInformation(
            ref record,
            mapNamePtr,
            IntPtr.Zero,
            ref mapNameSize);

        if (mapNameRes != Win32Error.ERROR_SUCCESS && mapNameRes != Win32Error.ERROR_INSUFFICIENT_BUFFER)
        {
            throw new Win32Exception(mapNameRes);
        }

        nint mapNameBuffer = Marshal.AllocHGlobal(mapNameSize);
        mapNameRes = Tdh.TdhGetEventMapInformation(
            ref record,
            mapNamePtr,
            mapNameBuffer,
            ref mapNameSize);

        if (mapNameRes != Win32Error.ERROR_SUCCESS)
        {
            Marshal.FreeHGlobal(mapNameBuffer);
            throw new Win32Exception(mapNameRes);
        }

        return mapNameBuffer;
    }

    private static short GetPropertyLength(
        TdhInType inType,
        TdhOutType outType,
        EventPropertyFlags flags,
        short value,
        ReadOnlySpan<short> propertyValues)
    {
        if (
            outType == TdhOutType.TDH_OUTTYPE_IPV6 &&
            inType == TdhInType.TDH_INTYPE_BINARY &&
            value == 0 &&
            !flags.HasFlag(EventPropertyFlags.PropertyParamLength | EventPropertyFlags.PropertyParamFixedLength)
        )
        {
            // Special case for IPV6 addresses. The tdh.h header mentions this
            // under the TDH_OUTTYPE_IPV6 entry.
            return 16;
        }
        else if (flags.HasFlag(EventPropertyFlags.PropertyParamLength))
        {
            return propertyValues[value];
        }
        else
        {
            return value;
        }
    }

    private static short GetArrayCount(
        EventPropertyFlags flags,
        short value,
        ReadOnlySpan<short> propertyValues
    ) => flags.HasFlag(EventPropertyFlags.PropertyParamCount) ? propertyValues[value] : value;

    private static string? FormatProperty(
        ref Tdh.TRACE_EVENT_INFO info,
        EventPropertyFlags flags,
        nint mapInfo,
        int pointerSize,
        short propertyLength,
        TdhInType inType,
        TdhOutType outType,
        ReadOnlySpan<byte> userData,
        out short consumed)
    {
        unsafe
        {
            fixed (byte* userDataPtr = userData)
            {
                int bufferSize = 1024;
                nint buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    while (true)
                    {
                        int res = Tdh.TdhFormatProperty(
                            ref info,
                            mapInfo,
                            pointerSize,
                            (short)inType,
                            (short)outType,
                            propertyLength,
                            (short)userData.Length,
                            (nint)userDataPtr,
                            ref bufferSize,
                            buffer,
                            out consumed);

                        if (res == Win32Error.ERROR_INSUFFICIENT_BUFFER)
                        {
                            buffer = Marshal.ReAllocHGlobal(buffer, (nint)bufferSize);
                            continue;
                        }
                        else if (res == Win32Error.ERROR_SUCCESS)
                        {
                            // The buffer is a null terminated WCHAR so we
                            // divide the bytes by 2 and remove the null char.
                            int stringLength = (bufferSize / 2) - 1;
                            return Marshal.PtrToStringUni(buffer, stringLength);
                        }

                        // On an error the caller uses ToString() on our
                        // managed object of the property.
                        return null;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
    }
}
