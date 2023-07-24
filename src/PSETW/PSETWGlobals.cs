using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PSETW.Native;

namespace PSETW;

internal static class PSETWGlobals
{
    private static TraceSession? _defaultETWSession = null;
    private static Dictionary<string, Guid>? _installedProviders = null;

    public static TraceSession DefaultETWSession
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

                _defaultETWSession = TraceSession.Create(name);
            }

            return _defaultETWSession;
        }
    }

    public static Dictionary<string, Guid> InstalledProviders
    {
        get
        {
            if (_installedProviders == null)
            {
                _installedProviders = Commands.ProviderStringOrGuid.GetInstalledProviders();
            }

            return _installedProviders;
        }
    }
}

internal sealed class SafeETWTraceSession : SafeHandle
{
    private long _sessionHandle = 0;
    private bool _stopSession;

    public SafeETWTraceSession(long handle, nint buffer, bool stopSession) : base(buffer, true)
    {
        _sessionHandle = handle;
        _stopSession = stopSession;
    }

    public override bool IsInvalid => _sessionHandle != 0 || handle != IntPtr.Zero;

    internal long DangerousGetTraceHandle() => _sessionHandle;

    protected override bool ReleaseHandle()
    {
        int res = 0;
        if (_sessionHandle != 0 && _stopSession)
        {
            unsafe
            {
                res = Advapi32.ControlTraceW(
                    _sessionHandle,
                    null,
                    handle,
                    EventTraceControl.EVENT_TRACE_CONTROL_STOP);
            }
            _sessionHandle = 0;
        }
        if (handle != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(handle);
            handle = IntPtr.Zero;
        }
        return res == 0;
    }
}
