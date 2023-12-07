using PSEtw.Shared.Native;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PSEtw.Shared;

public sealed class EtwTrace : IDisposable
{
    private static readonly Guid EVENT_TRACE_GUID = new("68fdd900-4a3e-11d1-84f4-0000f80464e3");

    private Thread? _processThread;
    private EtwTraceSession _session;
    private SafeEtwTrace? _trace;

    private Advapi32.PEVENT_RECORD_CALLBACK _delegate;
    private nint _delegatePtr;

    public event EventHandler<EtwEventArgs>? EventReceived;
    public event UnhandledExceptionEventHandler? UnhandledException;

    internal EtwTrace(EtwTraceSession session)
    {
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
            /*
            While https://learn.microsoft.com/en-us/windows/win32/etw/retrieving-event-metadata
            ignores records with this provider and a 0 Opcode I am seeing 3
            events for this provider at the start of every trace. I cannot find
            any documentation around this but am going to ignore them for now.
            This will probably bite me in the future but lack of documentation
            is killing me here.
            */
            if (record.EventHeader.ProviderId == EVENT_TRACE_GUID)
            {
                return;
            }

            EtwEventArgs eventArgs = EtwEventArgs.Create(ref record);
            EventReceived?.Invoke(this, eventArgs);
        }
        catch (Exception e)
        {
            UnhandledException?.Invoke(this, new(e, false));
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
