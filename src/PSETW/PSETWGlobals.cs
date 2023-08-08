using System;
using System.Diagnostics;

namespace PSETW;

internal static class PSETWGlobals
{
    private static TraceSession? _defaultETWSession = null;

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
}
