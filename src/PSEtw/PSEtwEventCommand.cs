using PSEtw.Shared;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;

namespace PSEtw;

public abstract class PSEtwEventBase : PSCmdlet, IDisposable
{
    internal const string DEFAULT_PARAM_SET = "Single";

    private EtwTraceSession? _session;
    private bool _freeSession = false;

    [Parameter]
    [ArgumentCompleter(typeof(SessionNameCompletor))]
    [Alias("Name")]
    public string? SessionName { get; set; }

    [Parameter(
        Mandatory = true,
        ParameterSetName = "Single"
    )]
    [ArgumentCompleter(typeof(ProviderCompleter))]
    public ProviderStringOrGuid? Provider { get; set; }

    [Parameter(
        ParameterSetName = "Single"
    )]
    [ArgumentCompleter(typeof(KeywordCompleter))]
    public KeywordsStringOrLong[] KeywordsAny { get; set; } = Array.Empty<KeywordsStringOrLong>();

    [Parameter(
        ParameterSetName = "Single"
    )]
    [ArgumentCompleter(typeof(KeywordCompleter))]
    public KeywordsStringOrLong[] KeywordsAll { get; set; } = Array.Empty<KeywordsStringOrLong>();

    [Parameter(
        ParameterSetName = "Single"
    )]
    [ArgumentCompleter(typeof(LevelCompletor))]
    public LevelStringOrInt? Level { get; set; }

    [Parameter(
        Mandatory = true,
        ParameterSetName = "Multi",
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
    )]
    public EtwEventInfo[] TraceInfo { get; set; } = Array.Empty<EtwEventInfo>();

    protected override void BeginProcessing()
    {
        if (string.IsNullOrEmpty(SessionName))
        {
            try
            {
                _session = PSEtwGlobals.DefaultETWSession;
            }
            catch (Win32Exception e) when (e.NativeErrorCode == 0x00000005) // ERROR_ACCESS_DENIED
            {
                string msg =
                    "Failed to open or create default ETW Trace Session for module. Either:\n" +
                    "    * Rerun command as an Administrator\n" +
                    "    * Create a custom trace session with 'New-PSEtwTraceSession' and use it with the " +
                    $"'-{nameof(SessionName)}' parameter\n" +
                    "    * Create new default session as an Administrator with " +
                    "'New-PSEtwTraceSession -UseDefault -SystemLogger' and add the current user to the " +
                    "'Performance Log Users' group before trying again";
                ErrorRecord err = new(
                    e,
                    "FailedToCreateOrOpenDefaultSession",
                    ErrorCategory.PermissionDenied,
                    null)
                {
                    ErrorDetails = new(msg)
                };

                ThrowTerminatingError(err);
                return;
            }
        }
        else
        {
            _freeSession = true;
            try
            {
                _session = EtwTraceSession.Open(SessionName!);
            }
            catch (Win32Exception e)
            {
                string msg =
                    $"Failed to open session '{SessionName}', ensure it exists and the current user has permissions " +
                    $"to open the session: {e.Message} (0x{e.NativeErrorCode:X8})";
                ErrorRecord err = new(
                    e,
                    "FailedToOpenSession",
                    ErrorCategory.NotSpecified,
                    SessionName)
                {
                    ErrorDetails = new(msg)
                };

                ThrowTerminatingError(err);
                return;
            }
        }
    }

    protected override void ProcessRecord()
    {
        Debug.Assert(_session != null);

        EtwEventInfo[] traces;
        if (ParameterSetName == DEFAULT_PARAM_SET)
        {
            Debug.Assert(Provider != null);

            EtwEventInfo? info = NewPSEtwTraceInfoCommand.CreateEventInfo(
                this,
                Provider!,
                KeywordsAll,
                KeywordsAny,
                Level);
            if (info == null)
            {
                return;
            }

            traces = new[] { info };
        }
        else
        {
            traces = TraceInfo;
        }

        foreach (EtwEventInfo trace in traces)
        {
            try
            {
                _session!.EnableTrace(
                    trace.Provider,
                    trace.Level,
                    trace.KeywordsAny,
                    trace.KeywordsAll);
            }
            catch (Win32Exception e)
            {
                string msg =
                    $"Failed to enable trace provider {trace.Provider}: {e.Message} (0x{e.NativeErrorCode:X8})";
                ErrorRecord err = new(
                    e,
                    "FailedToEnableTrace",
                    ErrorCategory.NotSpecified,
                    SessionName)
                {
                    ErrorDetails = new(msg)
                };

                WriteError(err);
            }
        }
    }

    protected override void EndProcessing()
    {
        Debug.Assert(_session != null);
        StartTrace(_session!.OpenTrace());
    }

    protected abstract void StartTrace(EtwTrace trace);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _freeSession)
        {
            _session?.Dispose();
        }
    }
}

[Cmdlet(VerbsLifecycle.Register, "PSEtwEvent", DefaultParameterSetName = DEFAULT_PARAM_SET)]
[OutputType(typeof(PSEventJob))]
public sealed class RegisterPSEtwEventCommand : PSEtwEventBase
{
    [Parameter]
    public ScriptBlock? Action { get; set; }

    [Parameter]
    public SwitchParameter Forward { get; set; }

    [Parameter]
    public int MaxTriggerCount { get; set; }

    [Parameter]
    public PSObject? MessageData { get; set; }

    [Parameter]
    public string SourceIdentifier { get; set; } = Guid.NewGuid().ToString();

    [Parameter]
    public SwitchParameter SupportEvent { get; set; }

    protected override void BeginProcessing()
    {
        if (Forward && (Action != null))
        {
            string msg = "The action is not supported when you are forwarding events.";
            ErrorRecord err = new(
                new ArgumentException(msg),
                "ActionAndForwardUsedTogether",
                ErrorCategory.InvalidArgument,
                null);
            ThrowTerminatingError(err);
            return;
        }

        base.BeginProcessing();
    }

    protected override void StartTrace(EtwTrace trace)
    {
        PSEventSubscriber eventSub = Events.SubscribeEvent(
            trace,
            nameof(EtwTrace.EventReceived),
            SourceIdentifier,
            MessageData,
            Action,
            SupportEvent,
            Forward,
            MaxTriggerCount);
        eventSub.Unsubscribed += static (s, e) =>
        {
            (s as EtwTrace)?.Dispose();
        };

        trace.Start();

        if ((Action != null) && (!SupportEvent))
        {
            WriteObject(eventSub.Action);
        }
    }
}

[Cmdlet(VerbsLifecycle.Stop, "PSEtwTrace")]
public sealed class StopPSEtwTraceCommand : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true
    )]
    public EtwEventArgs? InputObject { get; set; }

    protected override void EndProcessing()
    {
        Debug.Assert(InputObject != null);
        try
        {
            InputObject!.CancelToken?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            WriteVerbose("Trace has already been stopped");
        }
    }
}

[Cmdlet(VerbsDiagnostic.Trace, "PSEtwEvent", DefaultParameterSetName = DEFAULT_PARAM_SET)]
[OutputType(typeof(EtwEventArgs))]
public sealed class TracePSEtwEventCommand : PSEtwEventBase
{
    private sealed class ExceptionOrEvent
    {
        public Exception? Exception { get; }
        public EtwEventArgs? Args { get; }

        public ExceptionOrEvent(Exception exception)
        {
            Exception = exception;
        }

        public ExceptionOrEvent(EtwEventArgs args)
        {
            Args = args;
        }
    }
    private BlockingCollection<ExceptionOrEvent> _events = new();

    protected override void StartTrace(EtwTrace trace)
    {
        using (trace)
        using (CancellationTokenSource cancelSource = new())
        {
            trace.EventReceived += EventReceived;
            trace.UnhandledException += UnhandledExceptionReceived;
            trace.Start();

            foreach (ExceptionOrEvent obj in _events.GetConsumingEnumerable())
            {
                if (obj.Args != null)
                {
                    obj.Args.CancelToken = cancelSource;
                    WriteObject(obj.Args);
                }
                else
                {
                    Debug.Assert(obj.Exception != null);
                    Exception exp = obj.Exception!;

                    string msg = $"Unhandled exception in trace callback: {exp.Message}";
                    ErrorRecord err = new(
                        exp,
                        "TraceCallbackException",
                        ErrorCategory.NotEnabled,
                        null
                    )
                    {
                        ErrorDetails = new(msg)
                    };
                    WriteError(err);
                }

                if (cancelSource.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private void EventReceived(object? sender, EtwEventArgs args)
        => AddToCollection(new(args));

    private void UnhandledExceptionReceived(object? sender, UnhandledExceptionEventArgs args)
        => AddToCollection(new((Exception)args.ExceptionObject));

    private void AddToCollection(ExceptionOrEvent obj)
    {
        if (!_events.IsAddingCompleted)
        {
            _events.Add(obj);
        }
    }

    protected override void StopProcessing()
    {
        _events?.CompleteAdding();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _events?.Dispose();
        }

        base.Dispose(disposing);
    }
}
