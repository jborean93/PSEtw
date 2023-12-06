using PSEtw.Shared.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace PSEtw.Shared;

public sealed class EtwTrace : IDisposable
{
    private Advapi32.EVENT_TRACE_LOGFILEW _logFile = new();
    private long _handle = 0;
    private Thread? _processThread;
    private Advapi32.PEVENT_RECORD_CALLBACK _delegate;
    private EtwTraceSession _session;

    public event EventHandler<EtwEventArgs>? EventReceived;

    internal EtwTrace(nint loggerName, EtwTraceSession session)
    {
        _delegate = new(EventRecordCallback);
        _session = session;

        _logFile.LoggerName = loggerName;
        _logFile.ProcessTraceMode = ProcessTraceMode.PROCESS_TRACE_MODE_EVENT_RECORD | ProcessTraceMode.PROCESS_TRACE_MODE_REAL_TIME;
        _logFile.EventRecordCallback = Marshal.GetFunctionPointerForDelegate(_delegate);
    }

    internal void Start()
    {
        _handle = Advapi32.OpenTraceW(ref _logFile);
        long invalidHandle = Environment.Is64BitProcess ? -1 : 0xFFFFFFFF;
        if (_handle == invalidHandle)
        {
            _handle = 0;
            throw new Win32Exception();
        }

        _processThread = new Thread(() => ProcessTrace(_handle));
        _processThread.Start();
    }

    internal void EventRecordCallback(ref Advapi32.EVENT_RECORD record)
    {
        try
        {
            EtwEventArgs eventArgs = new(record);
            EventReceived?.Invoke(this, eventArgs);
        }
        catch (Win32Exception)
        { }
    }

    private static void ProcessTrace(long handle)
    {
        EtwApi.ProcessTrace(stackalloc[] { handle });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal void Dispose(bool disposing)
    {
        if (_handle != 0)
        {
            Advapi32.CloseTrace(_handle);
            _handle = 0;
        }
        if (disposing)
        {
            if (_processThread != null)
            {
                _processThread.Join();
            }
            _session.DisableAllTraces(inDispose: true);
        }
    }
    ~EtwTrace() => Dispose(false);
}
