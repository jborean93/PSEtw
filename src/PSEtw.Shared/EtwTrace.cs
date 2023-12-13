using PSEtw.Shared.Native;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PSEtw.Shared;

public sealed class EtwTrace : IDisposable
{
    private Thread? _processThread;
    private EtwTraceSession _session;
    private SafeEtwTrace? _trace;
    private bool _includeRawData;

    private Advapi32.PEVENT_RECORD_CALLBACK _delegate;
    private nint _delegatePtr;

    public event EventHandler<EtwEventArgs>? EventReceived;

    internal EtwTrace(EtwTraceSession session, bool includeRawData)
    {
        _includeRawData = includeRawData;
        _session = session;

        _delegate = new(EventRecordCallback);
        _delegatePtr = Marshal.GetFunctionPointerForDelegate(_delegate);
    }

    internal void Start()
    {
        Advapi32.EVENT_TRACE_LOGFILEW logFile = new()
        {
            LoggerName = _session.SessionNamePtr,
            ProcessTraceMode = ProcessTraceMode.PROCESS_TRACE_MODE_EVENT_RECORD |
                ProcessTraceMode.PROCESS_TRACE_MODE_REAL_TIME,
            EventRecordCallback = _delegatePtr,
        };

        _trace = EtwApi.OpenTrace(logFile);
        _processThread = new Thread(() =>
        {
            EtwApi.ProcessTrace(stackalloc[] { _trace.DangerousGetTraceHandle() });
        });
        _processThread.Start();
    }

    internal void EventRecordCallback(ref Advapi32.EVENT_RECORD record)
    {
        try
        {
            EtwEventArgs eventArgs = new(ref record, _includeRawData);
            EventReceived?.Invoke(this, eventArgs);
        }
        catch
        {
            return;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal void Dispose(bool disposing)
    {
        _trace?.Dispose();

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
