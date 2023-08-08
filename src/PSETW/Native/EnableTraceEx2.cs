using System;
using System.Runtime.InteropServices;

namespace PSETW.Native;

internal partial class Advapi32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ENABLE_TRACE_PARAMETERS
    {
        public const int ENABLE_TRACE_PARAMETERS_VERSIONS_2 = 2;

        public int Version;
        public int EnableProperty;
        public int ControlFlags;
        public Guid SourceId;
        public nint EnableFilterDesc;
        public int FilterDescCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_FILTER_DESCRIPTION
    {
        public long Ptr;
        public int Size;
        public int Type;
    }

    public const int EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0;

    public const int EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;

    public const int EVENT_CONTROL_CODE_CAPTURE_STATE = 2;

    [DllImport("Advapi32.dll")]
    public unsafe static extern int EnableTraceEx2(
        long TraceHandle,
        ref Guid ProviderId,
        int ControlCode,
        byte Level,
        long MatchAnyKeyword,
        long MatchAllKeyword,
        int Timeout,
        ENABLE_TRACE_PARAMETERS* EnableParameters);
}

public enum EventControlCode
{
    EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0,
    EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1,
    EVENT_CONTROL_CODE_CAPTURE_STATE = 2,
}

public enum TraceLevel : byte
{
    TRACE_LEVEL_LOG_ALWAYS = 0,
    TRACE_LEVEL_CRITICAL = 1,
    TRACE_LEVEL_ERROR = 2,
    TRACE_LEVEL_WARNING = 3,
    TRACE_LEVEL_INFORMATION = 4,
    TRACE_LEVEL_VERBOSE = 5,
}
