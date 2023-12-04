using System;
using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal partial class Advapi32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_TRACE_LOGFILEW
    {
        public nint LogFileName;
        public nint LoggerName;
        public long CurrentTime;
        public int BuffersRead;
        public ProcessTraceMode ProcessTraceMode;
        public EVENT_TRACE CurrentEvent;
        public TRACE_LOGFILE_HEADER LogfileHeader;
        public nint BufferCallback;
        public int BufferSize;
        public int Filled;
        public int EventsLost;
        public nint EventRecordCallback;
        public int IsKernelTrace;
        public nint Context;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_TRACE
    {
        public EVENT_TRACE_HEADER Header;
        public int InstanceId;
        public int ParentInstanceId;
        public Guid ParentGuid;
        public nint MofData;
        public int MofLength;
        public ETW_BUFFER_CONTEXT ClientContext;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_TRACE_HEADER
    {
        public short Size;
        public short FieldTypeFlags;
        public int Version;
        public int ThreadId;
        public int ProcessId;
        public long TimeStamp;
        public Guid GuidPtr;
        public long ProcessorTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ETW_BUFFER_CONTEXT
    {
        public byte ProcessorNumber;
        public byte Alignment;
        public short LoggerId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TRACE_LOGFILE_HEADER
    {
        public int BufferSize;
        public int Version;
        public int ProviderVersion;
        public int NumberOfProcessors;
        public long EndTime;
        public int TimerResolution;
        public int MaximumFileSize;
        public int LogFileMode;
        public int BuffersWritten;
        public Guid LogInstanceGuid;
        public nint LoggerName;
        public nint LogFileName;
        public TIME_ZONE_INFORMATION TimeZone;
        public long BootTime;
        public long PerfFreq;
        public long StartTime;
        public int ReservedFlags;
        public int BuffersLost;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TIME_ZONE_INFORMATION
    {
        public int Bias;
        public unsafe fixed char StandardName[32];
        public SYSTEMTIME StandardDate;
        public int StandardBias;
        public unsafe fixed char DaylightName[32];
        public SYSTEMTIME DaylightDate;
        public int DaylightBias;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEMTIME
    {
        public short wYear;
        public short wMonth;
        public short wDayOfWeek;
        public short wDay;
        public short wHour;
        public short wMinute;
        public short wSecond;
        public short wMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_RECORD
    {
        public EVENT_HEADER EventHeader;
        public ETW_BUFFER_CONTEXT BufferContext;
        public short ExtendedDataCount;
        public short UserDataLength;
        public nint ExtendedData;
        public nint UserData;
        public nint UserContext;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_HEADER
    {
        public short Size;
        public short HeaderType;
        public HeaderFlags Flags;
        public HeaderProperty EventProperty;
        public int ThreadId;
        public int ProcessId;
        public long TimeStamp;
        public Guid ProviderId;
        public EVENT_DESCRIPTOR EventDescriptor;
        public long ProcessorTime;
        public Guid ActivityId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_DESCRIPTOR
    {
        public short Id;
        public byte Version;
        public byte Channel;
        public byte Level;
        public byte Opcode;
        public short Task;
        public long Keyword;
    }

    public delegate void PEVENT_RECORD_CALLBACK(ref EVENT_RECORD EventRecord);

    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public unsafe static extern long OpenTraceW(
        ref EVENT_TRACE_LOGFILEW Logfile);
}

public enum ProcessTraceMode
{
    PROCESS_TRACE_MODE_REAL_TIME = 0x00000100,
    PROCESS_TRACE_MODE_RAW_TIMESTAMP = 0x00001000,
    PROCESS_TRACE_MODE_EVENT_RECORD = 0x10000000,
}

[Flags]
public enum HeaderFlags : short
{
    EVENT_HEADER_FLAG_NONE = 0x0000,
    EVENT_HEADER_FLAG_EXTENDED_INFO = 0x0001,
    EVENT_HEADER_FLAG_PRIVATE_SESSION = 0x0002,
    EVENT_HEADER_FLAG_STRING_ONLY = 0x0004,
    EVENT_HEADER_FLAG_TRACE_MESSAGE = 0x0008,
    EVENT_HEADER_FLAG_NO_CPUTIME = 0x0010,
    EVENT_HEADER_FLAG_32_BIT_HEADER = 0x0020,
    EVENT_HEADER_FLAG_64_BIT_HEADER = 0x0040,
    EVENT_HEADER_FLAG_DECODE_GUID = 0x0080,
    EVENT_HEADER_FLAG_CLASSIC_HEADER = 0x0100,
    EVENT_HEADER_FLAG_PROCESSOR_INDEX = 0x0200,
}

[Flags]
public enum HeaderProperty : short
{
    EVENT_HEADER_PROPERTY_NONE = 0x0000,
    EVENT_HEADER_PROPERTY_XML = 0x0001,
    EVENT_HEADER_PROPERTY_FORWARDED_XML = 0x0002,
    EVENT_HEADER_PROPERTY_LEGACY_EVENTLOG = 0x0004,
    EVENT_HEADER_PROPERTY_RELOGGABLE = 0x0008,
}
