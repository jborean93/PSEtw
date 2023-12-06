using PSEtw.Shared.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSEtw.Shared;

public sealed class EtwEventArgs : EventArgs
{
    public EventHeader Header { get; }
    public string Message { get; }

    internal EtwEventArgs(Advapi32.EVENT_RECORD record)
    {
        // https://learn.microsoft.com/en-us/windows/win32/etw/using-tdhformatproperty-to-consume-event-data
        // https://learn.microsoft.com/en-us/windows/win32/etw/retrieving-event-metadata
        Header = new(record.EventHeader);

        int bufferSize = 0;
        int res = Tdh.TdhGetEventInformation(ref record, 0, IntPtr.Zero, IntPtr.Zero, ref bufferSize);
        if (res != 0 && res != 122) // ERROR_INSUFFICIENT_BUFFER
        {
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
                string? providerName = ReadPtrString(buffer, eventInfo[0].ProviderNameOffset);
                string? levelName = ReadPtrString(buffer, eventInfo[0].LevelNameOffset);
                string? channelName = ReadPtrString(buffer, eventInfo[0].ChannelNameOffset);
                string[] keywordsName = ReadPtrStringList(buffer, eventInfo[0].KeywordsNameOffset);
                string? taskName = ReadPtrString(buffer, eventInfo[0].TaskNameOffset);
                string? opcodeName = ReadPtrString(buffer, eventInfo[0].OpcodeNameOffset);
                string? eventMessage = ReadPtrString(buffer, eventInfo[0].EventMessageOffset);
                string? providerMessage = ReadPtrString(buffer, eventInfo[0].ProviderMessageOffset);
                string? eventName = ReadPtrString(buffer, eventInfo[0].EventNameOffset);
                string? relatedActivityIdName = ReadPtrString(buffer, eventInfo[0].RelatedActivityIDNameOffset);
                ReadProperties(buffer, eventInfo[0].PropertyCount);

                Message = eventMessage ?? "Unknown event message";
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private unsafe static void ReadProperties(nint buffer, int count)
    {
        if (count == 0)
        {
            return;
        }

        nint propertiesOffset = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.TRACE_EVENT_INFO>());
        Span<Tdh.EVENT_PROPERTY_INFO> properties = new((void*)propertiesOffset, count);
        foreach (Tdh.EVENT_PROPERTY_INFO prop in properties)
        {
            string propName = ReadPtrString(buffer, prop.NameOffset) ?? "";

            if (prop.Flags.HasFlag(EventPropertyFlags.PropertyStruct))
            {
                short structStartIndex = prop.InType;
                short numOfStructMembers = prop.OutType;
            }
            else if (prop.Flags.HasFlag(EventPropertyFlags.PropertyHasCustomSchema))
            {
                short inType = prop.InType;
                short outType = prop.OutType;
                int customSchemaOffset = prop.MapNameOffset;
            }
            else
            {
                short inType = prop.InType;
                short outType = prop.OutType;
                int mapNameOffset = prop.MapNameOffset;
            }

            int tags = 0;
            if (prop.Flags.HasFlag(EventPropertyFlags.PropertyHasTags))
            {
                // Tags are only a 28-bit value, the leading byte is reserved
                tags = prop.Tags & 0x0FFFFFFF;
            }
        }
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

    private static string? ReadPtrString(nint buffer, int offset)
    {
        if (offset == 0)
        {
            return null;
        }
        // These chars end with a space so strip that.
        return Marshal.PtrToStringUni(IntPtr.Add(buffer, offset))?.TrimEnd(' ');
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

    internal EventHeader(Advapi32.EVENT_HEADER header)
    {
        ThreadId = header.ThreadId;
        ProcessId = header.ProcessId;
        TimeStamp = DateTime.FromFileTimeUtc(header.TimeStamp);
        ProviderId = header.ProviderId;
        Descriptor = new(header.EventDescriptor);
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

    internal EventDescriptor(Advapi32.EVENT_DESCRIPTOR descriptor)
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
