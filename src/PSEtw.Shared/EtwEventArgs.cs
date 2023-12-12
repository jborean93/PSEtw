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

public sealed class EtwEventArgs : EventArgs
{
    public EventHeader Header { get; }
    public EventInfo? Info { get; }

    // Used by Trace-PSEtwEvent and Stop-PSEtwTrace
    internal CancellationTokenSource? CancelToken { get; set; }

    private EtwEventArgs(EventHeader header, EventInfo? info)
    {
        Header = header;
        Info = info;
    }

    internal static EtwEventArgs Create(ref Advapi32.EVENT_RECORD record)
    {
        EventHeader header = new(ref record.EventHeader);

        if (
            record.EventHeader.Flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_TRACE_MESSAGE) ||
            record.EventHeader.Flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_CLASSIC_HEADER) ||
            record.EventHeader.Flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_STRING_ONLY)
        )
        {
            // FIXME: Find a WPP event to test with
            throw new NotImplementedException(
                $"Support for event with flags {record.EventHeader.Flags} has not been implemented");
        }

        int bufferSize = 0;
        int res = Tdh.TdhGetEventInformation(
            ref record,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            ref bufferSize);

        if (res == Win32Error.ERROR_NOT_FOUND)
        {
            // No known schema for this event so we can only return out header.
            return new(header, null);
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
                Span<Tdh.TRACE_EVENT_INFO> eventInfo = new((void*)buffer, 1);
                EventInfo info = new(
                    ref record,
                    ref eventInfo[0],
                    buffer,
                    GetPointerSize(record.EventHeader.Flags),
                    record.UserData,
                    record.UserDataLength);

                return new(header, info);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
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

    internal static string? ReadPtrString(nint buffer, int offset, int length = 0)
    {
        if (offset == 0)
        {
            return null;
        }

        nint ptr = IntPtr.Add(buffer, offset);

        string? value = length > 0 ? Marshal.PtrToStringUni(ptr, length) : Marshal.PtrToStringUni(ptr);

        // Some event entries end with a space so we strip that.
        return value?.TrimEnd(' ');
    }
}

public sealed class EventHeader
{
    public int ThreadId { get; }
    public int ProcessId { get; }
    public DateTime TimeStamp { get; }
    public Guid ProviderId;
    public EventDescriptor Descriptor { get; }
    public Guid ActivityId { get; }

    internal EventHeader(ref Advapi32.EVENT_HEADER header)
    {
        ThreadId = header.ThreadId;
        ProcessId = header.ProcessId;
        TimeStamp = DateTime.FromFileTimeUtc(header.TimeStamp);
        ProviderId = header.ProviderId;
        Descriptor = new(ref header.EventDescriptor);
        ActivityId = header.ActivityId;
    }
}

public sealed class EventDescriptor
{
    public short Id { get; }
    public byte Version { get; }
    public byte Channel { get; }
    public byte Level { get; }
    public byte Opcode { get; }
    public short Task { get; }
    public long Keyword { get; }

    internal EventDescriptor(ref Advapi32.EVENT_DESCRIPTOR descriptor)
    {
        Id = descriptor.Id;
        Version = descriptor.Version;
        Channel = descriptor.Channel;
        Level = descriptor.Level;
        Opcode = descriptor.Opcode;
        Task = descriptor.Task;
        Keyword = descriptor.Keyword;
    }
}

public sealed class EventInfo
{
    public Guid EventGuid { get; }
    public string? Provider { get; }
    public string? Level { get; }
    public string? Channel { get; }
    public string[] Keywords { get; }
    public string? Task { get; }
    public string? OpCode { get; }
    public string? RawEventMessage { get; }
    public string? EventMessage { get; }
    public string? ProviderMessage { get; }
    public string? EventName { get; }
    public string? RelatedActivityIdName { get; }
    public EventPropertyInfo[] Properties { get; }

    internal EventInfo(
        ref Advapi32.EVENT_RECORD record,
        ref Tdh.TRACE_EVENT_INFO info,
        nint buffer,
        int pointerSize,
        nint userData,
        short userDataLength)
    {
        EventGuid = info.EventGuid;
        Provider = EtwEventArgs.ReadPtrString(buffer, info.ProviderNameOffset);
        Level = EtwEventArgs.ReadPtrString(buffer, info.LevelNameOffset);
        Channel = EtwEventArgs.ReadPtrString(buffer, info.ChannelNameOffset);
        Keywords = ReadPtrStringList(buffer, info.KeywordsNameOffset);
        Task = EtwEventArgs.ReadPtrString(buffer, info.TaskNameOffset);
        OpCode = EtwEventArgs.ReadPtrString(buffer, info.OpcodeNameOffset);
        RawEventMessage = EtwEventArgs.ReadPtrString(buffer, info.EventMessageOffset);
        ProviderMessage = EtwEventArgs.ReadPtrString(buffer, info.ProviderMessageOffset);
        EventName = EtwEventArgs.ReadPtrString(buffer, info.EventNameOffset);
        RelatedActivityIdName = EtwEventArgs.ReadPtrString(buffer, info.RelatedActivityIDNameOffset);
        Properties = ReadProperties(
            ref record,
            ref info,
            buffer,
            info.TopLevelPropertyCount,
            info.PropertyCount,
            pointerSize,
            userData,
            userDataLength);

        if (info.EventMessageOffset > 0)
        {
            string[] replacements = Properties.Select(v => v.DisplayValue).ToArray();
            EventMessage = FormatMessage(IntPtr.Add(buffer, info.EventMessageOffset), replacements);
        }
        else
        {
            EventMessage = RawEventMessage;
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
        int topLevelCount,
        int count,
        int pointerSize,
        nint userData,
        short userDataLength)
    {
        if (count == 0)
        {
            return Array.Empty<EventPropertyInfo>();
        }

        List<EventPropertyInfo> results = new();
        nint propertiesOffset = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.TRACE_EVENT_INFO>());
        unsafe
        {
            Span<Tdh.EVENT_PROPERTY_INFO> properties = new((void*)propertiesOffset, count);

            // We need to keep track of how much data is consumed from the
            // buffer as properties are read.
            Span<byte> userDataBuffer = new((byte*)userData, userDataLength);

            // Properties can refer back to previous ones to retrieve integer
            // values needed for things like the data/array count. The count
            // and length values are always 16 bits in length.
            Span<short> integerValues = stackalloc short[count];

            for (int i = 0; i < topLevelCount; i++)
            {
                EventPropertyInfo prop = EventPropertyInfo.Create(
                    ref record,
                    ref info,
                    properties,
                    buffer,
                    i,
                    integerValues,
                    pointerSize,
                    userDataBuffer,
                    out int consumed);
                userDataBuffer = userDataBuffer.Slice(consumed);
                results.Add(prop);
            }
        }

        return results.ToArray();
    }

    private static string[] ReadPtrStringList(nint buffer, int offset)
    {
        if (offset == 0)
        {
            return Array.Empty<string>();
        }

        buffer = IntPtr.Add(buffer, offset);
        List<string> values = new();
        while (true)
        {
            string? value = Marshal.PtrToStringUni(buffer);
            if (string.IsNullOrEmpty(value))
            {
                break;
            }
            else
            {
                buffer = IntPtr.Add(buffer, (value.Length + 1) * 2);
                values.Add(value.TrimEnd(' '));
            }
        }

        return values.ToArray();
    }
}

public sealed class EventPropertyInfo
{
    public string? Name { get; }
    public object Value { get; }
    public string DisplayValue { get; }
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
        string? name = EtwEventArgs.ReadPtrString(buffer, info.NameOffset);

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
                    displayValues.Add(displayValue ?? outValue.ToString() ?? "");
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
