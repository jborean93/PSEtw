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
    public virtual string[] SessionName { get; set; } = Array.Empty<string>();

    [Parameter(
        ParameterSetName = "Default"
    )]
    public SwitchParameter Default { get; set; }

    protected override void ProcessRecord()
    {
        string[] names;
        if (Default)
        {
            names = new[] { PSEtwGlobals.DEFAULT_SESSION_NAME };
        }
        else
        {
            names = SessionName;
        }

        foreach (string name in names)
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

    protected abstract void ProcessName(string name);
}

[Cmdlet(VerbsCommon.New, "PSEtwSession", SupportsShouldProcess = true, DefaultParameterSetName = DEFAULT_PARAM_SET)]
[OutputType(typeof(EtwTraceSession))]
public sealed class NewPSEtwCommand : PSEtwCommandBase
{
    [Parameter(
        ParameterSetName = DEFAULT_PARAM_SET
    )]
    public SwitchParameter SystemLogger { get; set; }

    protected override void ProcessName(string name)
    {
        if (ShouldProcess(name, "create"))
        {
            bool isSystemLogger = SystemLogger;
            if (string.Equals(name, PSEtwGlobals.DEFAULT_SESSION_NAME, StringComparison.OrdinalIgnoreCase))
            {
                isSystemLogger = true;
            }

            WriteObject(EtwTraceSession.Create(name, isSystemLogger: isSystemLogger));
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
    public override string[] SessionName { get; set; } = Array.Empty<string>();

    protected override void ProcessName(string name)
    {
        if (ShouldProcess(name, "remove"))
        {
            if (string.Equals(name, PSEtwGlobals.DEFAULT_SESSION_NAME, StringComparison.OrdinalIgnoreCase))
            {
                PSEtwGlobals.RemoveDefaultSession();
            }

            EtwApi.RemoveTraceSession(name);
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
    protected override void ProcessName(string name)
    {
        WriteObject(_allSessions.Contains(name));
    }
}
