using PSEtw.Shared.Native;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
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

        if (record.EventHeader.Flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_TRACE_MESSAGE))
        {
            // FIXME: Find a WPP event to test with
            throw new Win32Exception(0x000000A0);  // ERROR_BAD_ARGUMENTS
        }
        else if (record.EventHeader.Flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_CLASSIC_HEADER))
        {
            // FIXME: Find a WPP event to test with
            throw new Win32Exception(0x000000A0);  // ERROR_BAD_ARGUMENTS
        }
        else if (record.EventHeader.Flags.HasFlag(HeaderFlags.EVENT_HEADER_FLAG_STRING_ONLY))
        {
            string? userData = ReadPtrString(
                record.UserData,
                0,
                length: record.UserDataLength / 2);
            throw new Win32Exception(0x000000A0);  // ERROR_BAD_ARGUMENTS
        }

        int bufferSize = 0;
        int res = Tdh.TdhGetEventInformation(
            ref record,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            ref bufferSize);

        if (res == 0x00000490) // ERROR_NOT_FOUND
        {
            // No known schema for this event so we can only return out header.
            return new(header, null);
        }
        else if (res != 0 && res != 122) // ERROR_INSUFFICIENT_BUFFER
        {
            // Some other error we should be reporting back if possible.
            throw new Win32Exception(res);
        }

        nint buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            res = Tdh.TdhGetEventInformation(ref record, 0, IntPtr.Zero, buffer, ref bufferSize);
            if (res != 0)
            {
                throw new Win32Exception(res);
            }

            unsafe
            {
                Span<Tdh.TRACE_EVENT_INFO> eventInfo = new((void*)buffer, 1);
                EventInfo info = new(
                    ref eventInfo[0],
                    buffer,
                    record.EventHeader.Flags,
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

    internal static string? ReadPtrString(nint buffer, int offset, int length = 0, bool noTrim = false)
    {
        if (offset == 0)
        {
            return null;
        }

        nint ptr = IntPtr.Add(buffer, offset);

        string? value = length > 0 ? Marshal.PtrToStringUni(ptr, length) : Marshal.PtrToStringUni(ptr);

        // Some event entries end with a space so we strip that.
        return noTrim ? value : value?.TrimEnd(' ');
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
    public string? EventMessage { get; }
    public string? ProviderMessage { get; }
    public string? EventName { get; }
    public string? RelatedActivityIdName { get; }
    public EventPropertyInfo[] Properties { get; }


    internal EventInfo(
        ref Tdh.TRACE_EVENT_INFO info,
        nint buffer,
        HeaderFlags headerFlags,
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
        EventMessage = EtwEventArgs.ReadPtrString(buffer, info.EventMessageOffset);
        ProviderMessage = EtwEventArgs.ReadPtrString(buffer, info.ProviderMessageOffset);
        EventName = EtwEventArgs.ReadPtrString(buffer, info.EventNameOffset);
        RelatedActivityIdName = EtwEventArgs.ReadPtrString(buffer, info.RelatedActivityIDNameOffset);
        Properties = ReadProperties(
            buffer,
            info.TopLevelPropertyCount,
            info.PropertyCount,
            headerFlags,
            userData,
            userDataLength);
    }

    private static EventPropertyInfo[] ReadProperties(
        nint buffer,
        int count,
        int totalCount,
        HeaderFlags headerFlags,
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
            Span<short> integerValues = stackalloc short[totalCount];

            for (int i = 0; i < count; i++)
            {
                EventPropertyInfo prop = EventPropertyInfo.Create(
                    properties,
                    buffer,
                    i,
                    integerValues,
                    headerFlags,
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
                buffer = IntPtr.Add(buffer, value.Length + 2);
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

    internal EventPropertyInfo(string? name, object value)
    {
        Name = name;
        Value = value;
    }

    internal static EventPropertyInfo Create(
        Span<Tdh.EVENT_PROPERTY_INFO> properties,
        nint buffer,
        int index,
        Span<short> integerValues,
        HeaderFlags headerFlags,
        ReadOnlySpan<byte> userData,
        out int consumed)
    {
        ref Tdh.EVENT_PROPERTY_INFO info = ref properties[0];
        string? name = EtwEventArgs.ReadPtrString(buffer, info.NameOffset);

        // We store scalar integer values in case they are needed for
        // subsequent properties that reference back to them.
        if (
            !info.Flags.HasFlag(EventPropertyFlags.PropertyStruct | EventPropertyFlags.PropertyParamCount) &&
            info.Count == 1
        )
        {
            switch ((TdhInType)info.InType)
            {
                case TdhInType.TDH_INTYPE_INT8:
                case TdhInType.TDH_INTYPE_UINT8:
                    integerValues[index] = userData[0];
                    break;

                case TdhInType.TDH_INTYPE_INT16:
                case TdhInType.TDH_INTYPE_UINT16:
                    integerValues[index] = BinaryPrimitives.ReadInt16LittleEndian(userData);
                    break;

                case TdhInType.TDH_INTYPE_INT32:
                case TdhInType.TDH_INTYPE_UINT32:
                case TdhInType.TDH_INTYPE_HEXINT32:
                    integerValues[index] = (short)(BinaryPrimitives.ReadInt32LittleEndian(userData) & 0xFFFF);
                    break;

                case TdhInType.TDH_INTYPE_INT64:
                case TdhInType.TDH_INTYPE_UINT64:
                case TdhInType.TDH_INTYPE_HEXINT64:
                    integerValues[index] = (short)(BinaryPrimitives.ReadInt64LittleEndian(userData) & 0xFFFF);
                    break;
            }
        }

        TdhInType inType = (TdhInType)info.InType;
        TdhOutType outType = (TdhOutType)info.OutType;
        short propLength = GetPropertyLength(
            inType,
            outType,
            info.Flags, info.Length, integerValues);
        short arrayCount = GetArrayCount(info.Flags, info.Count, integerValues);
        if (arrayCount == 0)
        {
            throw new NotImplementedException($"Property '{name}' has an array count of 0");
        }

        // PropertyParamFixedCount is used to signify if an array of 1 value is
        // actually an array.
        bool isArray = arrayCount != 1 ||
            info.Flags.HasFlag(EventPropertyFlags.PropertyParamCount | EventPropertyFlags.PropertyParamFixedCount);

        if (info.MapNameOffset != 0)
        {
            throw new NotImplementedException($"Property '{name}' has an associated map which is not implemented");
        }

        List<object> values = new();
        consumed = 0;
        for (int i = 0; i < arrayCount; i++)
        {
            if (info.Flags.HasFlag(EventPropertyFlags.PropertyStruct))
            {
                throw new NotImplementedException($"Property '{name}' is a struct which is not implemented");
            }
            else if (info.Flags.HasFlag(EventPropertyFlags.PropertyHasCustomSchema))
            {
                throw new NotImplementedException($"Property '{name}' is a custom schema which is not implemented");
            }
            else
            {
                object outValue = GetDataOutValue(
                    name ?? "<no name>",
                    headerFlags,
                    userData,
                    propLength,
                    inType,
                    outType,
                    integerValues,
                    index,
                    out int valueConsumed);
                userData = userData.Slice(valueConsumed);
                consumed += valueConsumed;
                values.Add(outValue);
            }
        }

        // int tags = 0;
        // if (info.Flags.HasFlag(EventPropertyFlags.PropertyHasTags))
        // {
        //     // Tags are only a 28-bit value, the leading byte is reserved
        //     tags = info.Tags & 0x0FFFFFFF;
        // }
        return new(name, isArray ? values.ToArray() : values[0]);
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
            // Special case for incorrectly-defined IPV6 addresses.
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

    private static object GetDataOutValue(
        string name,
        HeaderFlags headerFlags,
        ReadOnlySpan<byte> data,
        short length,
        TdhInType inType,
        TdhOutType outType,
        Span<short> integerValue,
        int index,
        out int consumed)
    {
        consumed = 0;
        object? value = null;
        switch (inType)
        {
            case TdhInType.TDH_INTYPE_INT8:
            case TdhInType.TDH_INTYPE_UINT8:
            case TdhInType.TDH_INTYPE_ANSICHAR:
                consumed = 1;
                break;

            case TdhInType.TDH_INTYPE_INT16:
            case TdhInType.TDH_INTYPE_UINT16:
            case TdhInType.TDH_INTYPE_UNICODECHAR:
                consumed = 2;
                break;

            case TdhInType.TDH_INTYPE_INT32:
            case TdhInType.TDH_INTYPE_UINT32:
            case TdhInType.TDH_INTYPE_FLOAT:
            case TdhInType.TDH_INTYPE_BOOLEAN:
            case TdhInType.TDH_INTYPE_HEXINT32:
                consumed = 4;
                break;

            case TdhInType.TDH_INTYPE_INT64:
            case TdhInType.TDH_INTYPE_UINT64:
            case TdhInType.TDH_INTYPE_DOUBLE:
            case TdhInType.TDH_INTYPE_FILETIME:
            case TdhInType.TDH_INTYPE_HEXINT64:
                consumed = 8;
                break;

            case TdhInType.TDH_INTYPE_GUID:
            case TdhInType.TDH_INTYPE_SYSTEMTIME:
                consumed = 16;
                break;

            case TdhInType.TDH_INTYPE_BINARY:
                consumed = length;
                break;

            case TdhInType.TDH_INTYPE_POINTER:
            case TdhInType.TDH_INTYPE_SIZET:
                consumed = headerFlags switch
                {
                    HeaderFlags.EVENT_HEADER_FLAG_32_BIT_HEADER => 4,
                    HeaderFlags.EVENT_HEADER_FLAG_64_BIT_HEADER => 8,
                    _ => IntPtr.Size,
                };
                break;

            case TdhInType.TDH_INTYPE_MANIFEST_COUNTEDSTRING:
            case TdhInType.TDH_INTYPE_MANIFEST_COUNTEDANSISTRING:
            case TdhInType.TDH_INTYPE_MANIFEST_COUNTEDBINARY:
            case TdhInType.TDH_INTYPE_COUNTEDSTRING:
            case TdhInType.TDH_INTYPE_COUNTEDANSISTRING:
                consumed = BinaryPrimitives.ReadInt16LittleEndian(data);
                break;

            case TdhInType.TDH_INTYPE_REVERSEDCOUNTEDSTRING:
            case TdhInType.TDH_INTYPE_REVERSEDCOUNTEDANSISTRING:
                consumed = BinaryPrimitives.ReadInt16BigEndian(data);
                break;

            case TdhInType.TDH_INTYPE_HEXDUMP:
                consumed = BinaryPrimitives.ReadInt32LittleEndian(data);
                break;

            case TdhInType.TDH_INTYPE_UNICODESTRING:
            case TdhInType.TDH_INTYPE_NONNULLTERMINATEDSTRING:
                string uniValue = "";
                consumed = uniValue.Length * 2;  // Also add NULL char
                value = uniValue;
                break;


            case TdhInType.TDH_INTYPE_ANSISTRING:
            case TdhInType.TDH_INTYPE_NONNULLTERMINATEDANSISTRING:
                string ansiValue = "";
                consumed = ansiValue.Length;  // Also add NULL char
                value = ansiValue;
                break;

            case TdhInType.TDH_INTYPE_SID:
            case TdhInType.TDH_INTYPE_WBEMSID:
                SecurityIdentifier sid;
                unsafe
                {
                    fixed (byte* bytePtr = data)
                    {
                        sid = new((nint)bytePtr);
                    }
                }
                consumed = sid.BinaryLength;
                value = sid;
                break;

            default:
                throw new NotImplementedException(
                    $"Event Property '{name}' has TDH_INTYPE {inType} that is not implemented");
        }

        if (value != null)
        {
            return value;
        }

        return 0;
    }
}
