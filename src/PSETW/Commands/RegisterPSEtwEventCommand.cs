using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using PSETW.Native;

namespace PSETW.Commands;

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
    public string? SourceIdentifier { get; set; }

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
    public int KeywordsAny { get; set; }

    [Parameter(
        ParameterSetName = "Single"
    )]
    [ArgumentCompleter(typeof(KeywordCompleter))]
    public int KeywordsAll { get; set; }

    [Parameter(
        ParameterSetName = "Single"
    )]
    public TraceIntOrString Level { get; set; }

    protected override void ProcessRecord()
    {
        Debug.Assert(Provider != null);
        Guid providerGuid = Provider!.GetProviderGuid();

        /*
        LOG_ALWAYS (0) 	Event bypasses level-based event filtering. Events should not use this level.
        CRITICAL (1) 	Critical error
        ERROR (2) 	Error
        WARNING (3) 	Warning
        INFO (4) 	Informational
        VERBOSE (5) 	Verbose
        */
        string a = "";
    }
}

public sealed class TraceIntOrString
{
    public static string[] KnownTraceNames = new[]
    {
        "Critical",
        "Error",
        "Warning",
        "Information",
        "Verbose",
    };

    private int? _traceInt;
    private string? _traceString;

    public TraceIntOrString(int value)
    {
        _traceInt = value;
    }

    public TraceIntOrString(string value)
    {
        _traceString = value;
    }

    internal int GetTraceLevel(Guid provider)
    {
        return 0;
    }
}

internal sealed class LevelCompletor : IArgumentCompleter
{
    private static HashSet<string> _constantNames = new()
    {
        "Critical",
        "Error",
        "Warning",
        "Information",
        "Verbose",
    };

    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        yield return new("ab");
    }
}
