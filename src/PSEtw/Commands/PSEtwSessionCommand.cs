using PSEtw.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;

namespace PSEtw.Commands;

public abstract class PSEtwCommandBase : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
    )]
    [Alias("Name")]
    public virtual string[] SessionName { get; set; } = Array.Empty<string>();

    protected override void ProcessRecord()
    {
        foreach (string name in SessionName)
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

[Cmdlet(VerbsCommon.New, "PSEtwSession", SupportsShouldProcess = true)]
[OutputType(typeof(EtwTraceSession))]
public sealed class NewPSEtwCommand : PSEtwCommandBase
{
    protected override void ProcessName(string name)
    {
        if (ShouldProcess(name, "create"))
        {
            WriteObject(EtwTraceSession.Create(name));
        }
    }
}

[Cmdlet(VerbsCommon.Remove, "PSEtwSession", SupportsShouldProcess = true)]
public sealed class RemovePSEtwCommand : PSEtwCommandBase
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
    )]
    [Alias("Name")]
    [ArgumentCompleter(typeof(SessionNameCompletor))]
    public override string[] SessionName { get; set; } = Array.Empty<string>();

    protected override void ProcessName(string name)
    {
        if (ShouldProcess(name, "create"))
        {
            EtwApi.RemoveTraceSession(name);
        }
    }
}

[Cmdlet(VerbsDiagnostic.Test, "PSEtwSession")]
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
