using PSEtw.Shared;
using System;
using System.Diagnostics;

namespace PSEtw;

internal static class PSETWGlobals
{
    private static EtwTraceSession? _defaultETWSession = null;

    public static EtwTraceSession DefaultETWSession
    {
        get
        {
            if (_defaultETWSession == null)
            {
                string name = typeof(PSETWGlobals).Assembly.GetName().Name!;

                _defaultETWSession = EtwTraceSession.Create(name);
            }

            return _defaultETWSession;
        }
    }
}
