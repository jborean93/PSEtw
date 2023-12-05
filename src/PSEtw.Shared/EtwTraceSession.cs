using PSEtw.Shared.Native;
using System;

namespace PSEtw.Shared;

public sealed class EtwTraceSession : IDisposable
{
    private SafeEtwTraceSession _session;

    public string Name { get; }

    private EtwTraceSession(SafeEtwTraceSession session, string name)
    {
        _session = session;
        Name = name;
    }

    internal static EtwTraceSession Create(string name)
    {
        SafeEtwTraceSession sessionHandle = EtwApi.CreateTraceSession(name);
        return new(sessionHandle, name);
    }

    internal static EtwTraceSession Open(string name)
    {
        SafeEtwTraceSession sessionHandle = EtwApi.OpenTraceSession(name);
        return new(sessionHandle, name);
    }

    internal void EnableTrace(
        Guid providerId,
        int controlCode,
        byte level,
        long matchAnyKeyword,
        long matchAllKeyword = 0)
    {
        EtwApi.EnableTrace(_session, providerId, controlCode, level, matchAnyKeyword, matchAllKeyword);
    }

    internal EtwTrace OpenTrace()
    {
        nint sessionNamePtr = IntPtr.Zero;
        unsafe
        {
            nint buffer = _session.DangerousGetHandle();
            Advapi32.EVENT_TRACE_PROPERTIES_V2* props = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)buffer;
            sessionNamePtr = IntPtr.Add(buffer, props->LoggerNameOffset);
        }

        return new(sessionNamePtr);
    }

    public void Dispose()
    {
        _session?.Dispose();
        GC.SuppressFinalize(this);
    }
}
