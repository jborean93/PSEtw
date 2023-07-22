using System;
using System.Runtime.InteropServices;

namespace PSETW.Native;

internal partial class Advapi32
{
    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode)]
    public static extern int ControlTraceW(
        long TraceHandle,
        [MarshalAs(UnmanagedType.LPWStr)] string InstanceName,
        nint Properties,
        EventTraceControl ControlCode);
}

public enum EventTraceControl
{
    EVENT_TRACE_CONTROL_QUERY = 0,
    EVENT_TRACE_CONTROL_STOP = 1,
    EVENT_TRACE_CONTROL_UPDATE = 2,
    EVENT_TRACE_CONTROL_FLUSH = 3,
    EVENT_TRACE_CONTROL_INCREMENT_FILE = 4,
    EVENT_TRACE_CONTROL_CONVERT_TO_REALTIME = 5,
}
