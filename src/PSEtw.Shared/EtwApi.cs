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

    public static void RemoveTraceSession(SafeEtwTraceSession session)
    {
        unsafe
        {
            int res = Advapi32.ControlTraceW(
                session.DangerousGetTraceHandle(),
                null,
                session.DangerousGetHandle(),
                EventTraceControl.EVENT_TRACE_CONTROL_STOP);

            if (res != 0)
            {
                throw new Win32Exception(res);
            }
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

    public static SafeEtwTrace OpenTrace(Advapi32.EVENT_TRACE_LOGFILEW logFile)
    {
        long handle = Advapi32.OpenTraceW(ref logFile);
        long invalidHandle = Environment.Is64BitProcess ? -1 : 0xFFFFFFFF;
        if (handle == invalidHandle)
        {
            handle = 0;
            throw new Win32Exception();
        }

        return new(handle);
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

    public static Guid[] GetTraceGuids()
    {
        int guidSize = Marshal.SizeOf<Guid>();
        unsafe
        {
            int bufferSize = 64;
            while (true)
            {
                Span<Guid> buffer = new Span<Guid>(new Guid[bufferSize]);
                fixed (Guid* bufferPtr = buffer)
                {
                    int res = Advapi32.EnumerateTraceGuidsEx(
                        (int)TRACE_INFO_CLASS.TraceGuidQueryList,
                        IntPtr.Zero,
                        0,
                        (nint)bufferPtr,
                        bufferSize * guidSize,
                        out int returnLength);

                    if (res == 0)
                    {
                        return buffer.Slice(0, returnLength / guidSize).ToArray();
                    }
                    else if (res == 122)
                    {
                        bufferSize = returnLength / guidSize;
                    }
                    else
                    {
                        throw new Win32Exception(res);
                    }
                }
            }
        }
    }
}


internal sealed class SafeEtwTraceSession : SafeHandle
{
    private long _sessionHandle = 0;

    public SafeEtwTraceSession(long handle, nint buffer) : base(buffer, true)
    {
        _sessionHandle = handle;
    }

    public override bool IsInvalid => _sessionHandle == 0 || handle == IntPtr.Zero;

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

internal sealed class SafeEtwTrace : SafeHandle
{
    private long _traceHandle = 0;

    public SafeEtwTrace(long handle) : base(IntPtr.Zero, true)
    {
        _traceHandle = handle;
    }

    public override bool IsInvalid => _traceHandle == 0;

    internal long DangerousGetTraceHandle() => _traceHandle;

    protected override bool ReleaseHandle()
    {
        int res = 0;
        if (_traceHandle != 0)
        {
            res = Advapi32.CloseTrace(_traceHandle);
        }

        return res == 0;
    }
}
