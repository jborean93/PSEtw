using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using PSETW.Native;

namespace PSETW;

public sealed class TraceSession : IDisposable
{
    private SafeETWTraceSession _session;

    public string Name { get; }

    private TraceSession(string name)
    {
        Name = name;
        int propsLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();
        int bufferSize = propsLength + Encoding.Unicode.GetByteCount(name) + 2;
        _traceProperties = Marshal.AllocHGlobal(bufferSize);

        int res;
        unsafe
        {
            new Span<byte>((void*)_traceProperties, propsLength).Fill(0);
            Advapi32.EVENT_TRACE_PROPERTIES_V2* props = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)_traceProperties;
            props->Wnode.BufferSize = bufferSize;
            props->Wnode.ClientContext = 1;  // Query Performance Counter (QPC).
            props->Wnode.Flags = WNodeFlag.WNODE_FLAG_TRACED_GUID | WNodeFlag.WNODE_FLAG_VERSIONED_PROPERTIES;
            props->LogFileMode = EventTraceMode.EVENT_TRACE_REAL_TIME_MODE;
            props->LoggerNameOffset = propsLength;
            props->V2Control = 2;
            _sessionName = IntPtr.Add(_traceProperties, propsLength);

            res = Advapi32.StartTraceW(
                out _handle,
                Name,
                (nint)props);
        }

        if (res != 0)
        {
            throw new Win32Exception(res);
        }
    }

    public static TraceSession Create(string name)
    {
        if (name.Length > 1024) {
            throw new ArgumentException("Trace session name must not be more than 1024 characters", nameof(name));
        }

        return new(name);
    }

    public void EnableTrace(
        Guid providerId,
        int controlCode,
        byte level,
        long matchAnyKeyword,
        long matchAllKeyword = 0)
    {
        int res;
        unsafe
        {
            res = Advapi32.EnableTraceEx2(
                _handle,
                ref providerId,
                controlCode,
                level,
                matchAnyKeyword,
                matchAllKeyword,
                -1,
                null);
        }

        if (res != 0)
        {
            throw new Win32Exception(res);
        }
    }

    public Trace OpenTrace() => new(_sessionName);

    public void Dispose()
    {
        _session?.Dispose();
        GC.SuppressFinalize(this);
    }
}
