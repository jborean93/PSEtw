using PSEtw.Shared;
using PSEtw.Shared.Native;
using System;
using System.Diagnostics;
using System.Management.Automation;

namespace PSEtw.Commands;

[Cmdlet(VerbsLifecycle.Register, "PSEtwEvent", DefaultParameterSetName = "Single")]
public sealed class RegisterPSEtwEventCommand : PSCmdlet
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
    [ArgumentCompleter(typeof(SessionNameCompletor))]
    public string? SessionName { get; set; }

    [Parameter]
    public string SourceIdentifier { get; set; } = Guid.NewGuid().ToString();

    [Parameter]
    public SwitchParameter SupportEvent { get; set; }

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

        EtwTrace trace = session.OpenTrace();

        PSEventSubscriber eventSub = Events.SubscribeEvent(
            trace,
            "EventReceived",
            SourceIdentifier,
            MessageData,
            Action,
            SupportEvent,
            Forward,
            MaxTriggerCount);

        trace.Start();

        WriteObject(trace);
        if (Action != null)
        {
            WriteObject(eventSub);
        }
    }
}
