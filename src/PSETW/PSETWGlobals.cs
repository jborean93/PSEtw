using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PSETW.Native;

namespace PSETW;

internal static class PSETWGlobals
{
    private static SafeETWTraceSession? _defaultETWSession = null;

    public static SafeETWTraceSession DefaultETWSession
    {
        get
        {
            if (_defaultETWSession == null)
            {
#if NET472
                int procId = Process.GetCurrentProcess().Id;
#else
                int procId = Environment.ProcessId;
#endif
                string name = $"PSETW-P{procId}";

                int propsLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();
                int bufferSize = propsLength + Encoding.Unicode.GetByteCount(name) + 2;

                long handle = 0;
                nint buffer = Marshal.AllocHGlobal(bufferSize);

                int res;
                unsafe
                {
                    new Span<byte>((void*)buffer, propsLength).Fill(0);
                    Advapi32.EVENT_TRACE_PROPERTIES_V2* props = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)_traceProperties;
                    props->Wnode.BufferSize = bufferSize;
                    props->Wnode.ClientContext = 1;  // Query Performance Counter (QPC).
                    props->Wnode.Flags = WNodeFlag.WNODE_FLAG_TRACED_GUID | WNodeFlag.WNODE_FLAG_VERSIONED_PROPERTIES;
                    props->LogFileMode = EventTraceMode.EVENT_TRACE_REAL_TIME_MODE;
                    props->LoggerNameOffset = propsLength;
                    props->V2Control = 2;

                    res = Advapi32.StartTraceW(
                        out handle,
                        name,
                        buffer);
                }

                if (res != 0)
                {
                    Marshal.FreeHGlobal(buffer);
                    throw new Win32Exception(res);
                }

                _defaultETWSession = new(handle, buffer);
            }

            return _defaultETWSession;
        }
    }

}

internal sealed class SafeETWTraceSession : SafeHandle
{
    private long _sessionHandle = 0;

    public SafeETWTraceSession(long handle, nint buffer) : base(buffer, true)
    {
        _sessionHandle = handle;
    }

    public override bool IsInvalid => _sessionHandle != 0;

    internal long DangerousGetTraceHandle() => _sessionHandle;

    protected override bool ReleaseHandle()
    {
        int res = Advapi32.ControlTraceW(
            _sessionHandle,
            null,
            handle,
            EventTraceControl.EVENT_TRACE_CONTROL_STOP);
        if (handle != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(handle);
        }
        _handle = 0;
        return res == 0;
    }
}
