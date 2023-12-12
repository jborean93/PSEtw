using PSEtw.Shared.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PSEtw.Shared;

public sealed class EtwTraceSession : IDisposable
{
    internal SafeEtwTraceSession _session;
    private List<(Guid, byte, long, long)> _enabledTraces = new();

    public string Name { get; }

    public bool IsSystemLogger
    {
        get
        {
            unsafe
            {
                Advapi32.EVENT_TRACE_PROPERTIES_V2* props = TraceProperties;
                return (props->LogFileMode & EventTraceMode.EVENT_TRACE_SYSTEM_LOGGER_MODE) != 0;
            }
        }
    }

    internal nint SessionNamePtr
    {
        get
        {
            unsafe
            {
                int offset = TraceProperties->LoggerNameOffset;
                return IntPtr.Add(_session.DangerousGetHandle(), offset);
            }
        }
    }

    private unsafe Advapi32.EVENT_TRACE_PROPERTIES_V2* TraceProperties
    {
        get => (Advapi32.EVENT_TRACE_PROPERTIES_V2*)_session.DangerousGetHandle();
    }

    private EtwTraceSession(SafeEtwTraceSession session, string name)
    {
        _session = session;
        Name = name;
    }

    internal static EtwTraceSession Create(string name, bool isSystemLogger = false)
    {
        SafeEtwTraceSession sessionHandle = EtwApi.CreateTraceSession(
            name,
            isSystemLogger: isSystemLogger);
        return new(sessionHandle, name);
    }

    internal static EtwTraceSession Open(string name)
    {
        SafeEtwTraceSession sessionHandle = EtwApi.OpenTraceSession(name);
        return new(sessionHandle, name);
    }

    internal static EtwTraceSession OpenOrCreate(string name, bool isSystemLogger = false)
    {
        try
        {
            return Open(name);
        }
        catch (Win32Exception e) when (e.NativeErrorCode == 0x00001069) // ERROR_WMI_INSTANCE_NOT_FOUND
        {
            return Create(name, isSystemLogger: isSystemLogger);
        }
    }

    internal void EnableTrace(
        Guid providerId,
        byte level,
        long matchAnyKeyword,
        long matchAllKeyword)
    {
        EtwApi.EnableTrace(
            _session,
            providerId,
            (int)EventControlCode.EVENT_CONTROL_CODE_ENABLE_PROVIDER,
            level, matchAnyKeyword,
            matchAllKeyword);
        _enabledTraces.Add((providerId, level, matchAnyKeyword, matchAllKeyword));
    }

    internal void DisableAllTraces(bool inDispose = false)
    {
        foreach ((Guid provider, byte level, long matchAnyKeyword, long matchAllKeyword) in _enabledTraces)
        {
            try
            {
                EtwApi.EnableTrace(
                    _session,
                    provider,
                    (int)EventControlCode.EVENT_CONTROL_CODE_DISABLE_PROVIDER,
                    level, matchAnyKeyword,
                    matchAllKeyword);
            }
            catch (Win32Exception) when (inDispose)
            { }  // If in Dispose() we don't want to throw an exception
        }
        _enabledTraces = new();
    }

    internal EtwTrace OpenTrace()
        => new(this);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal void Dispose(bool disposing)
    {
        _session?.Dispose();
    }
    ~EtwTraceSession() => Dispose(false);
}

public sealed class TraceSessionOrString
{
    internal EtwTraceSession? SessionValue { get; }
    internal string? StringValue { get; }

    public string Name => StringValue ?? SessionValue!.Name;

    public TraceSessionOrString(EtwTraceSession value)
    {
        SessionValue = value;
    }

    public TraceSessionOrString(string value)
    {
        StringValue = value;
    }
}
