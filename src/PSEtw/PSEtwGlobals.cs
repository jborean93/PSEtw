using PSEtw.Shared;

namespace PSEtw;

internal static class PSEtwGlobals
{
    internal static string DEFAULT_SESSION_NAME = typeof(PSEtwGlobals).Assembly.GetName().Name!;

    private static EtwTraceSession? _defaultETWSession = null;

    public static EtwTraceSession DefaultETWSession
    {
        get
        {
            if (_defaultETWSession == null)
            {
                _defaultETWSession = EtwTraceSession.OpenOrCreate(DEFAULT_SESSION_NAME, isSystemLogger: true);
            }

            return _defaultETWSession;
        }
    }

    public static void RemoveDefaultSession()
    {
        _defaultETWSession?.Dispose();
        _defaultETWSession = null;
    }
}
