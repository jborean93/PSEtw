using PSEtw.Shared.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace PSEtw.Shared;

internal static class EtwApi
{
    public static SafeEtwTraceSession CreateTraceSession(
        string name,
        bool isSystemLogger = false)
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
            if (isSystemLogger)
            {
                props->LogFileMode |= EventTraceMode.EVENT_TRACE_SYSTEM_LOGGER_MODE;
            }
            props->LoggerNameOffset = propsLength;
            props->V2Control = 2;

            int res = Advapi32.StartTraceW(
                out handle,
                name,
                buffer);

            if (res != 0)
            {
                Marshal.FreeHGlobal(buffer);
                throw new Win32Exception(res);
            }

            return new(handle, buffer);
        }
    }

    public static SafeEtwTraceSession OpenTraceSession(string name)
        => ControlTraceByName(name, EventTraceControl.EVENT_TRACE_CONTROL_QUERY);

    public static void RemoveTraceSession(string name)
    {
        ControlTraceByName(name, EventTraceControl.EVENT_TRACE_CONTROL_STOP).Dispose();
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
                &providerId,
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

    public static void ProcessTrace(Span<long> handles)
    {
        unsafe
        {
            fixed (long* handlesPtr = handles)
            {
                Advapi32.ProcessTrace(
                    (nint)handlesPtr,
                    handles.Length,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
        }
    }

    private static SafeEtwTraceSession ControlTraceByName(
            string name,
            EventTraceControl controlCode)
    {
        if (name.Length > 1024)
        {
            throw new ArgumentException(
                "Trace session name must not be more than 1024 characters",
                nameof(name));
        }

        int propsLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();
        int bufferSize = propsLength + 4096;
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
                    controlCode);
            }

            if (res != 0)
            {
                Marshal.FreeHGlobal(buffer);
                throw new Win32Exception(res);
            }

            return new(props->Wnode.HistoricalContext, buffer);
        }
    }
}
