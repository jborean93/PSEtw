using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using PSETW.Native;

namespace PSETW;

internal sealed class TraceSession : IDisposable
{
    private SafeETWTraceSession _session;

    public string Name { get; }

    private TraceSession(SafeETWTraceSession session, string name)
    {
        _session = session;
        Name = name;
    }

    public static TraceSession Create(string name)
    {
        if (name.Length > 1024)
        {
            throw new ArgumentException("Trace session name must not be more than 1024 characters", nameof(name));
        }

        int propsLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();
        int bufferSize = propsLength + Encoding.Unicode.GetByteCount(name) + 2;
        long handle = 0;
        nint buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            unsafe
            {
                new Span<byte>((void*)buffer, propsLength).Fill(0);
                Advapi32.EVENT_TRACE_PROPERTIES_V2* props = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)buffer;
                props->Wnode.BufferSize = bufferSize;
                props->Wnode.ClientContext = 1;  // Query Performance Counter (QPC).
                props->Wnode.Flags = WNodeFlag.WNODE_FLAG_TRACED_GUID | WNodeFlag.WNODE_FLAG_VERSIONED_PROPERTIES;
                props->LogFileMode = EventTraceMode.EVENT_TRACE_REAL_TIME_MODE;
                props->LoggerNameOffset = propsLength;
                props->V2Control = 2;

                int res = Advapi32.StartTraceW(
                    out handle,
                    name,
                    buffer);

                if (res != 0)
                {
                    throw new Win32Exception();
                }

                SafeETWTraceSession sessionHandle = new(handle, buffer, true);
                return new(sessionHandle, name);
            }
        }
        catch
        {
            Marshal.FreeHGlobal(buffer);
            throw;
        }
    }

    public static TraceSession Open(string name)
    {
        if (name.Length > 1024)
        {
            throw new ArgumentException("Trace session name must not be more than 1024 characters", nameof(name));
        }

        int propsLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();
        int bufferSize = propsLength + 4096;
        long handle = 0;
        nint buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            unsafe
            {
                new Span<byte>((void*)buffer, propsLength).Fill(0);
                Advapi32.EVENT_TRACE_PROPERTIES_V2* props = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)buffer;
                props->Wnode.BufferSize = bufferSize;
                props->LoggerNameOffset = propsLength;
                props->LogFileNameOffset = propsLength + 2048;
                props->V2Control = 2;

                int res;
                fixed (char* namePtr = name)
                {
                    res = Advapi32.ControlTraceW(
                        0,
                        namePtr,
                        buffer,
                        EventTraceControl.EVENT_TRACE_CONTROL_QUERY);
                }

                if (res != 0)
                {
                    throw new Win32Exception();
                }
                handle = props->Wnode.HistoricalContext;

                SafeETWTraceSession sessionHandle = new(handle, buffer, false);
                return new(sessionHandle, name);
            }
        }
        catch
        {
            Marshal.FreeHGlobal(buffer);
            throw;
        }
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
                _session.DangerousGetTraceHandle(),
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

    public Trace OpenTrace()
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
