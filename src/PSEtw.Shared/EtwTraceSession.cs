using PSEtw.Shared.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSEtw.Shared;

public sealed class EtwTraceSession : IDisposable
{
    private SafeEtwTraceSession _session;
    private List<(Guid, byte, long, long)> _enabledTraces = new();

    public string Name { get; }

    public bool IsSystemLogger
    {
        get
        {
            unsafe
            {
                nint buffer = _session.DangerousGetHandle();
                Advapi32.EVENT_TRACE_PROPERTIES_V2* props = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)buffer;
                return (props->LogFileMode & EventTraceMode.EVENT_TRACE_SYSTEM_LOGGER_MODE) != 0;
            }
        }
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
    {
        nint sessionNamePtr = IntPtr.Zero;
        unsafe
        {
            nint buffer = _session.DangerousGetHandle();
            Advapi32.EVENT_TRACE_PROPERTIES_V2* props = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)buffer;
            sessionNamePtr = IntPtr.Add(buffer, props->LoggerNameOffset);
        }

        return new(sessionNamePtr, this);
    }

    public void Dispose()
    {
        _session?.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal sealed class SafeEtwTraceSession : SafeHandle
{
    private long _sessionHandle = 0;

    public SafeEtwTraceSession(long handle, nint buffer) : base(buffer, true)
    {
        _sessionHandle = handle;
    }

    public override bool IsInvalid => _sessionHandle != 0 || handle != IntPtr.Zero;

    internal long DangerousGetTraceHandle() => _sessionHandle;

    protected override bool ReleaseHandle()
    {
        int res = 0;
        if (handle != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(handle);
            handle = IntPtr.Zero;
        }
        return res == 0;
    }
}
