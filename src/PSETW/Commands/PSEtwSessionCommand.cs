using PSEtw.Shared;
using System;
using System.Management.Automation;

namespace PSEtw.Commands;

[Cmdlet(VerbsCommon.New, "PSEtwSession")]
[OutputType(typeof(EtwTraceSession))]
public sealed class NewPSEtwCommand : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
    )]
    [Alias("Name")]
    public string[] SessionName { get; set; } = Array.Empty<string>();
    protected override void ProcessRecord()
    {
        foreach (string name in SessionName)
        {
            WriteObject(EtwTraceSession.Create(name));
        }
    }
}

[Cmdlet(VerbsCommon.Remove, "PSEtwSession")]
public sealed class RemovePSEtwCommand : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
    )]
    [Alias("Name")]
    public string[] SessionName { get; set; } = Array.Empty<string>();

    protected override void ProcessRecord()
    {
        foreach (string name in SessionName)
        {
            WriteObject(EtwTraceSession.Create(name));
        }
    }
}
