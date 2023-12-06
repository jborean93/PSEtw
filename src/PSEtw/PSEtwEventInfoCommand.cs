using PSEtw.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;

namespace PSEtw;

[Cmdlet(VerbsCommon.New, "PSEtwEventInfo")]
[OutputType(typeof(EtwEventInfo))]
public sealed class NewPSEtwTraceInfoCommand : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true
    )]
    [ArgumentCompleter(typeof(ProviderCompleter))]
    public ProviderStringOrGuid? Provider { get; set; }

    [Parameter(
        ValueFromPipelineByPropertyName = true
    )]
    [ArgumentCompleter(typeof(KeywordCompleter))]
    public KeywordsStringOrLong[] KeywordsAny { get; set; } = Array.Empty<KeywordsStringOrLong>();

    [Parameter(
        ValueFromPipelineByPropertyName = true
    )]
    [ArgumentCompleter(typeof(KeywordCompleter))]
    public KeywordsStringOrLong[] KeywordsAll { get; set; } = Array.Empty<KeywordsStringOrLong>();

    [Parameter(
        ValueFromPipelineByPropertyName = true
    )]
    [ArgumentCompleter(typeof(LevelCompletor))]
    public LevelStringOrInt? Level { get; set; }

    protected override void ProcessRecord()
    {
        Debug.Assert(Provider != null);

        EtwEventInfo? info = CreateEventInfo(this, Provider!, KeywordsAll, KeywordsAny, Level);
        if (info != null)
        {
            WriteObject(info);
        }
    }

    internal static EtwEventInfo? CreateEventInfo(
        PSCmdlet cmdlet,
        ProviderStringOrGuid provider,
        KeywordsStringOrLong[] keywordsAll,
        KeywordsStringOrLong[] keywordsAny,
        LevelStringOrInt? level)
    {
        try
        {
            return EtwEventInfo.Create(provider, keywordsAll, keywordsAny, level);
        }
        catch (Win32Exception e)
        {
            ErrorRecord err = new(
                e,
                "Win32Exception",
                ErrorCategory.NotSpecified,
                null);
            cmdlet.WriteError(err);
        }
        catch (ArgumentException e)
        {
            ErrorRecord err = new(
                e,
                "InvalidEventInfoValue",
                ErrorCategory.InvalidArgument,
                null);
            cmdlet.WriteError(err);
        }

        return null;
    }
}
