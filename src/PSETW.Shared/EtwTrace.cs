using PSEtw.Shared.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSEtw.Shared;

public sealed class ETWEventArgs : EventArgs
{
    public ETWEventArgs(string message)
    {
        Message = message;
    }

    public string Message { get; set; }
}

public sealed class EtwTrace : IDisposable
{
    private Advapi32.EVENT_TRACE_LOGFILEW _logFile = new();
    private long _handle = 0;
    private Thread? _processThread;
    private Advapi32.PEVENT_RECORD_CALLBACK _delegate;

    public event EventHandler<ETWEventArgs>? EventReceived;

    internal EtwTrace(nint loggerName)
    {
        _delegate = new(EventRecordCallback);

        _logFile.LoggerName = loggerName;
        _logFile.ProcessTraceMode = ProcessTraceMode.PROCESS_TRACE_MODE_EVENT_RECORD | ProcessTraceMode.PROCESS_TRACE_MODE_REAL_TIME;
        _logFile.EventRecordCallback = Marshal.GetFunctionPointerForDelegate<Advapi32.PEVENT_RECORD_CALLBACK>(_delegate);
    }

    public void Start()
    {
        _handle = Advapi32.OpenTraceW(ref _logFile);
        long invalidHandle = Environment.Is64BitProcess ? -1 : 0xFFFFFFFF;
        if (_handle == invalidHandle)
        {
            _handle = 0;
            throw new Win32Exception();
        }

        _processThread = new Thread(() => ProcessTrace(_handle));
        _processThread.Start();
    }

    internal void EventRecordCallback(ref Advapi32.EVENT_RECORD record)
    {
        // https://learn.microsoft.com/en-us/windows/win32/etw/using-tdhformatproperty-to-consume-event-data
        Guid activityId = record.EventHeader.ActivityId;
        Guid providerId = record.EventHeader.ProviderId;
        int processId = record.EventHeader.ProcessId;
        int threadId = record.EventHeader.ThreadId;
        DateTime timestamp = DateTime.FromFileTime(record.EventHeader.TimeStamp);
        // record.EventHeader.EventDescriptor

        int bufferSize = 0;
        int res = Tdh.TdhGetEventInformation(ref record, 0, IntPtr.Zero, IntPtr.Zero, ref bufferSize);
        if (res != 0 && res != 122) // ERROR_INSUFFICIENT_BUFFER
        {
            return;
        }

        nint buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            res = Tdh.TdhGetEventInformation(ref record, 0, IntPtr.Zero, buffer, ref bufferSize);
            if (res != 0)
            {
                return;
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

                ETWEventArgs e = new(eventMessage ?? "Unknown event message");
                EventReceived?.Invoke(this, e);
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

    private static void ProcessTrace(long handle)
    {
        Span<long> handles = stackalloc[] { handle };

        unsafe
        {
            fixed (long* handlesPtr = handles)
            {
                Advapi32.ProcessTrace(
                    (nint)handlesPtr,
                    1,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
        }
    }

    public void Dispose() => Dispose(true);

    internal void Dispose(bool disposing)
    {
        if (_handle != 0)
        {
            Advapi32.CloseTrace(_handle);
            _handle = 0;
        }
        if (disposing)
        {
            if (_processThread != null)
            {
                _processThread.Join();
            }
        }
        GC.SuppressFinalize(this);
    }
    ~EtwTrace() => Dispose(false);
}
