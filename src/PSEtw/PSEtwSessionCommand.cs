using PSEtw.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;

namespace PSEtw;

public abstract class PSEtwCommandBase : PSCmdlet
{
    internal const string DEFAULT_PARAM_SET = "Name";

    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = DEFAULT_PARAM_SET
    )]
    [Alias("Name")]
    public virtual TraceSessionOrString[] SessionName { get; set; } = Array.Empty<TraceSessionOrString>();

    [Parameter(
        ParameterSetName = "Default"
    )]
    public SwitchParameter Default { get; set; }

    protected override void ProcessRecord()
    {
        TraceSessionOrString[] names;
        if (Default)
        {
            names = new[] { new TraceSessionOrString(PSEtwGlobals.DEFAULT_SESSION_NAME) };
        }
        else
        {
            names = SessionName;
        }

        foreach (TraceSessionOrString name in names)
        {
            try
            {
                ProcessName(name);
            }
            catch (ArgumentException e)
            {
                ErrorRecord err = new(
                    e,
                    "NameTooLong",
                    ErrorCategory.InvalidArgument,
                    name);
                WriteError(err);
            }
            catch (Win32Exception e)
            {
                ErrorRecord err = new(
                    e,
                    "NativeError",
                    ErrorCategory.NotSpecified,
                    name);
                WriteError(err);
            }
        }
    }

    protected abstract void ProcessName(TraceSessionOrString name);
}

[Cmdlet(VerbsCommon.New, "PSEtwSession", SupportsShouldProcess = true, DefaultParameterSetName = DEFAULT_PARAM_SET)]
[OutputType(typeof(EtwTraceSession))]
public sealed class NewPSEtwCommand : PSEtwCommandBase
{
    [Parameter(
        ParameterSetName = DEFAULT_PARAM_SET
    )]
    public SwitchParameter SystemLogger { get; set; }

    protected override void ProcessName(TraceSessionOrString name)
    {
        if (ShouldProcess(name.Name, "create"))
        {
            bool isSystemLogger = SystemLogger;
            if (string.Equals(name.Name, PSEtwGlobals.DEFAULT_SESSION_NAME, StringComparison.OrdinalIgnoreCase))
            {
                isSystemLogger = true;
            }

            WriteObject(EtwTraceSession.Create(name.Name, isSystemLogger: isSystemLogger));
        }
    }
}

[Cmdlet(VerbsCommon.Remove, "PSEtwSession", SupportsShouldProcess = true, DefaultParameterSetName = DEFAULT_PARAM_SET)]
public sealed class RemovePSEtwCommand : PSEtwCommandBase
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = DEFAULT_PARAM_SET
    )]
    [Alias("Name")]
    [ArgumentCompleter(typeof(SessionNameCompletor))]
    public override TraceSessionOrString[] SessionName { get; set; } = Array.Empty<TraceSessionOrString>();

    protected override void ProcessName(TraceSessionOrString name)
    {
        if (ShouldProcess(name.Name, "remove"))
        {
            if (string.Equals(name.Name, PSEtwGlobals.DEFAULT_SESSION_NAME, StringComparison.OrdinalIgnoreCase))
            {
                PSEtwGlobals.RemoveDefaultSession();
            }

            if (name.SessionValue is EtwTraceSession session)
            {
                EtwApi.RemoveTraceSession(session._session);
                session.Dispose();
            }
            else
            {
                EtwApi.RemoveTraceSession(name.Name);
            }
        }
    }
}

[Cmdlet(VerbsDiagnostic.Test, "PSEtwSession", DefaultParameterSetName = DEFAULT_PARAM_SET)]
[OutputType(typeof(bool))]
public sealed class TestPSEtwSessionCommand : PSEtwCommandBase
{
    private HashSet<string> _allSessions = new();

    protected override void BeginProcessing()
    {
        _allSessions = ProviderHelper.QueryAllTraces().ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
    protected override void ProcessName(TraceSessionOrString name)
    {
        WriteObject(_allSessions.Contains(name.Name));
    }
}
