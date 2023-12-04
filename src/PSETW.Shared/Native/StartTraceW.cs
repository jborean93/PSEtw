using System;
using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal partial class Advapi32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WNODE_HEADER
    {
        public int BufferSize;
        public int ProviderId;
        public long HistoricalContext;
        public long Timestamp;
        public Guid Guid;
        public int ClientContext;
        public WNodeFlag Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_TRACE_PROPERTIES_V2
    {
        public WNODE_HEADER Wnode;
        public int BufferSize;
        public int MinimumBuffers;
        public int MaximumBuffers;
        public int MaximumFileSize;
        public EventTraceMode LogFileMode;
        public int FlushTimer;
        public int EnableFlags;
        public int AgeLimit;
        public int NumberOfBuffers;
        public int FreeBuffers;
        public int EventsLost;
        public int BuffersWritten;
        public int LogBuffersLost;
        public int ReadTimeBuffersLost;
        public nint LoggerThreadId;
        public int LogFileNameOffset;
        public int LoggerNameOffset;
        public int V2Control;
        public int FilterDescCount;
        public nint FilterDesc;
        public long V2Options;
    }

    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode)]
    public static extern int StartTraceW(
        out long TraceHandle,
        [MarshalAs(UnmanagedType.LPWStr)] string InstanceName,
        nint Properties);
}

[Flags]
public enum EventTraceMode
{
    EVENT_TRACE_FILE_MODE_NONE = 0x00000000,
    EVENT_TRACE_FILE_MODE_SEQUENTIAL = 0x00000001,
    EVENT_TRACE_FILE_MODE_CIRCULAR = 0x00000002,
    EVENT_TRACE_FILE_MODE_APPEND = 0x00000004,
    EVENT_TRACE_FILE_MODE_NEWFILE = 0x00000008,
    EVENT_TRACE_FILE_MODE_PREALLOCATE = 0x00000020,
    EVENT_TRACE_NONSTOPPABLE_MODE = 0x00000040,
    EVENT_TRACE_SECURE_MODE = 0x00000080,
    EVENT_TRACE_REAL_TIME_MODE = 0x00000100,
    EVENT_TRACE_DELAY_OPEN_FILE_MODE = 0x00000200,
    EVENT_TRACE_BUFFERING_MODE = 0x00000400,
    EVENT_TRACE_PRIVATE_LOGGER_MODE = 0x00000800,
    EVENT_TRACE_ADD_HEADER_MODE = 0x00001000,
    EVENT_TRACE_USE_KBYTES_FOR_SIZE = 0x00002000,
    EVENT_TRACE_USE_GLOBAL_SEQUENCE = 0x00004000,
    EVENT_TRACE_USE_LOCAL_SEQUENCE = 0x00008000,
    EVENT_TRACE_RELOG_MODE = 0x00010000,
    EVENT_TRACE_PRIVATE_IN_PROC = 0x00020000,
    EVENT_TRACE_MODE_RESERVED = 0x00100000,
    EVENT_TRACE_STOP_ON_HYBRID_SHUTDOWN = 0x00400000,
    EVENT_TRACE_PERSIST_ON_HYBRID_SHUTDOWN = 0x00800000,
    EVENT_TRACE_USE_PAGED_MEMORY = 0x01000000,
    EVENT_TRACE_SYSTEM_LOGGER_MODE = 0x02000000,
    EVENT_TRACE_INDEPENDENT_SESSION_MODE = 0x08000000,
    EVENT_TRACE_NO_PER_PROCESSOR_BUFFERING = 0x10000000,
    EVENT_TRACE_ADDTO_TRIAGE_DUMP = unchecked((int)0x80000000),
}

[Flags]
public enum WNodeFlag
{
    WNODE_FLAG_ALL_DATA = 0x00000001,
    WNODE_FLAG_SINGLE_INSTANCE = 0x00000002,
    WNODE_FLAG_SINGLE_ITEM = 0x00000004,
    WNODE_FLAG_EVENT_ITEM = 0x00000008,
    WNODE_FLAG_FIXED_INSTANCE_SIZE = 0x00000010,
    WNODE_FLAG_TOO_SMALL = 0x00000020,
    WNODE_FLAG_INSTANCES_SAME = 0x00000040,
    WNODE_FLAG_STATIC_INSTANCE_NAMES = 0x00000080,
    WNODE_FLAG_INTERNAL = 0x00000100,
    WNODE_FLAG_USE_TIMESTAMP = 0x00000200,
    WNODE_FLAG_PERSIST_EVENT = 0x00000400,
    WNODE_FLAG_EVENT_REFERENCE = 0x00002000,
    WNODE_FLAG_ANSI_INSTANCENAMES = 0x00004000,
    WNODE_FLAG_METHOD_ITEM = 0x00008000,
    WNODE_FLAG_PDO_INSTANCE_NAMES = 0x00010000,
    WNODE_FLAG_TRACED_GUID = 0x00020000,
    WNODE_FLAG_LOG_WNODE = 0x00040000,
    WNODE_FLAG_USE_GUID_PTR = 0x00080000,
    WNODE_FLAG_USE_MOF_PTR = 0x00100000,
    WNODE_FLAG_NO_HEADER = 0x00200000,
    WNODE_FLAG_SEND_DATA_BLOCK = 0x00400000,
    WNODE_FLAG_VERSIONED_PROPERTIES = 0x00800000,
    WNODE_FLAG_SEVERITY_MASK = unchecked((int)0xff000000),
}
