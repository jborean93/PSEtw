using PSEtw.Shared.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace PSEtw.Shared;

internal static class EtwApi
{
    public static SafeEtwTraceSession CreateTraceSession(string name)
    {
        if (name.Length > 1024)
        {
            throw new ArgumentException(
                "Trace session name must not be more than 1024 characters",
                nameof(name));
        }

        int propsLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();
        int bufferSize = propsLength + Encoding.Unicode.GetByteCount(name) + 2;
        long handle = 0;
        nint buffer = Marshal.AllocHGlobal(bufferSize);

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
                Marshal.FreeHGlobal(buffer);
                throw new Win32Exception();
            }

            return new(handle, buffer);
        }
    }

    public static SafeEtwTraceSession OpenTraceSession(string name)
    {
        if (name.Length > 1024)
        {
            throw new ArgumentException(
                "Trace session name must not be more than 1024 characters",
                nameof(name));
        }

        int propsLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();
        int bufferSize = propsLength + 4096;
        long handle = 0;
        nint buffer = Marshal.AllocHGlobal(bufferSize);

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
                Marshal.FreeHGlobal(buffer);
                throw new Win32Exception();
            }
            handle = props->Wnode.HistoricalContext;

            return new(handle, buffer);
        }
    }

    public static void EnableTrace(
        SafeEtwTraceSession session,
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
                session.DangerousGetTraceHandle(),
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
}
