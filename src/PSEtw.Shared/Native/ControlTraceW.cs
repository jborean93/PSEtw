using System;
using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal partial class Advapi32
{
    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode)]
    public unsafe static extern int ControlTraceW(
        long TraceHandle,
        char* InstanceName,
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
