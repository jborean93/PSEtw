using PSEtw.Shared;
using PSEtw.Shared.Native;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;

namespace PSEtw.Commands;

[Cmdlet(VerbsDiagnostic.Trace, "PSEtwEvent", DefaultParameterSetName = "Single")]
public sealed class TracePSEtwEventCommand : PSCmdlet, IDisposable
{
    private BlockingCollection<ETWEventArgs> _events = new();

    [Parameter]
    [ArgumentCompleter(typeof(SessionNameCompletor))]
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
    public LevelStringOrInt[] Level { get; set; } = Array.Empty<LevelStringOrInt>();

    protected override void ProcessRecord()
    {
        Debug.Assert(Provider != null);
        Guid providerGuid = Provider!.GetProviderGuid();

        FieldInfo[] keywords = ProviderHelper.GetProviderFieldInfo(providerGuid,
            EventFieldType.EventKeywordInformation);
        long keywordsAny = 0;
        foreach (KeywordsStringOrLong kwd in KeywordsAny)
        {
            keywordsAny |= kwd.GetKeywordLong(keywords);
        }
        long keywordsAll = 0;
        foreach (KeywordsStringOrLong kwd in KeywordsAll)
        {
            keywordsAll |= kwd.GetKeywordLong(keywords);
        }

        FieldInfo[] levels = ProviderHelper.GetProviderFieldInfo(providerGuid,
            EventFieldType.EventLevelInformation);
        int level = 0;
        foreach (LevelStringOrInt lvl in Level)
        {
            level |= lvl.GetLevelInt(levels);
        }

        EtwTraceSession session;
        if (string.IsNullOrEmpty(SessionName))
        {
            session = PSETWGlobals.DefaultETWSession;
        }
        else
        {
            session = EtwTraceSession.Open(SessionName!);
        }

        session.EnableTrace(
            providerGuid,
            (int)EventControlCode.EVENT_CONTROL_CODE_ENABLE_PROVIDER,
            (byte)level,
            keywordsAny,
            keywordsAll);

        using EtwTrace trace = session.OpenTrace();
        trace.EventReceived += EventReceived;
        trace.Start();

        foreach (ETWEventArgs args in _events.GetConsumingEnumerable())
        {
            WriteObject(args.Message);
        }
    }

    private void EventReceived(object? sender, ETWEventArgs args)
        => _events.Add(args);

    protected override void StopProcessing()
    {
        _events?.CompleteAdding();
    }

    public void Dispose()
    {
        _events?.Dispose();
    }
}
