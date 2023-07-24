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

[Cmdlet(VerbsLifecycle.Register, "EtwEvent", DefaultParameterSetName = "Single")]
public sealed class RegisterEtwEventCommand : PSCmdlet
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
    public int KeywordsAny { get; set; }

    [Parameter(
        ParameterSetName = "Single"
    )]
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

public sealed class ProviderStringOrGuid
{
    private Guid? _providerGuid;
    private string? _providerString;

    public ProviderStringOrGuid(Guid value)
    {
        _providerGuid = value;
    }

    public ProviderStringOrGuid(string value)
    {
        _providerString = value;
    }

    internal Guid GetProviderGuid()
    {
        if (_providerGuid != null)
        {
            return (Guid)_providerGuid;
        }

        Dictionary<string, Guid> cache = PSETWGlobals.InstalledProviders;
        string providerName = _providerString ?? "";
        if (cache.TryGetValue(providerName, out var providerGuid))
        {
            return providerGuid;
        }

        throw new ArgumentException($"Unknown ETW provider '{providerName}'");
    }

    internal static Dictionary<string, Guid> GetInstalledProviders()
    {
        Dictionary<string, Guid> finalRes = new();

        int bufferSize = 0;
        nint buffer = IntPtr.Zero;
        try
        {
            while (true)
            {
                int res = Tdh.TdhEnumerateProviders(buffer, ref bufferSize);
                if (res == 0x0000007A)  // ERROR_INSUFFICIENT_BUFFER
                {
                    if (buffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer);
                        buffer = IntPtr.Zero;
                    }
                    buffer = Marshal.AllocHGlobal(bufferSize);
                    continue;
                }
                else if (res != 0)
                {
                    throw new Win32Exception(res);
                }

                unsafe
                {
                    Tdh.PROVIDER_ENUMERATION_INFO* enumInfo = (Tdh.PROVIDER_ENUMERATION_INFO*)buffer;
                    nint providerPtr = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.PROVIDER_ENUMERATION_INFO>());
                    Span<Tdh.TRACE_PROVIDER_INFO> providers = new((void*)providerPtr, enumInfo->NumberOfProviders);
                    foreach (Tdh.TRACE_PROVIDER_INFO info in providers)
                    {
                        nint stringPtr = IntPtr.Add(buffer, info.ProviderNameoffset);
                        string providerName = Marshal.PtrToStringUni(stringPtr) ?? "";
                        finalRes[providerName] = info.ProviderGuid;
                    }
                }

                break;
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return finalRes;
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

internal sealed class ProviderCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        string[] providerNames = PSETWGlobals.InstalledProviders.Keys.ToArray();
        foreach (KeyValuePair<string, Guid> provider in PSETWGlobals.InstalledProviders)
        {
            string name = provider.Key;
            if (
                name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) ||
                new WildcardPattern($"{name}*", WildcardOptions.IgnoreCase).IsMatch(wordToComplete)
            )
            {
                yield return new(name, name, CompletionResultType.Text, $"Provider Guid: {provider.Value}");
            }
        }
    }

    private static (Guid, string)[] GetInstalledProviders()
    {
        List<(Guid, string)> finalRes = new();

        int bufferSize = 0;
        nint buffer = IntPtr.Zero;
        try
        {
            while (true)
            {
                int res = Tdh.TdhEnumerateProviders(buffer, ref bufferSize);
                if (res == 0x0000007A)  // ERROR_INSUFFICIENT_BUFFER
                {
                    if (buffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer);
                        buffer = IntPtr.Zero;
                    }
                    buffer = Marshal.AllocHGlobal(bufferSize);
                    continue;
                }
                else if (res != 0)
                {
                    throw new Win32Exception(res);
                }

                unsafe
                {
                    Tdh.PROVIDER_ENUMERATION_INFO* enumInfo = (Tdh.PROVIDER_ENUMERATION_INFO*)buffer;
                    nint providerPtr = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.PROVIDER_ENUMERATION_INFO>());
                    Span<Tdh.TRACE_PROVIDER_INFO> providers = new((void*)providerPtr, enumInfo->NumberOfProviders);
                    foreach (Tdh.TRACE_PROVIDER_INFO info in providers)
                    {
                        nint stringPtr = IntPtr.Add(buffer, info.ProviderNameoffset);
                        string providerName = Marshal.PtrToStringUni(stringPtr) ?? "";
                        finalRes.Add((info.ProviderGuid, providerName));
                    }
                }

                break;
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return finalRes.ToArray();
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

internal sealed class SessionNameCompletor : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        string[] sessionNames = GetEtwTraceSessionNames();
        WildcardPattern pattern = new(wordToComplete, WildcardOptions.IgnoreCase);
        foreach (string name in sessionNames)
        {
            if (name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) || pattern.IsMatch(name))
            {
                yield return new(name);
            }
        }
    }

    private static string[] GetEtwTraceSessionNames()
    {
        int traceCount = 64;
        int propLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();

        List<string> sessionNames = new();
        unsafe
        {
            while (true)
            {
                int bufferLength = (propLength + 4096) * traceCount;
                nint buffer = Marshal.AllocHGlobal(bufferLength);
                try
                {
                    new Span<byte>((void*)buffer, bufferLength).Fill(0);

                    nint[] propArray = new nint[traceCount];
                    nint propBuffer = buffer;
                    for (int i = 0; i < traceCount; i++)
                    {
                        Advapi32.EVENT_TRACE_PROPERTIES_V2* prop = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)propBuffer;
                        prop->Wnode.BufferSize = propLength + 4096;
                        prop->LoggerNameOffset = propLength;
                        prop->LogFileNameOffset = propLength + 2048;
                        prop->V2Control = 2;
                        propArray[i] = propBuffer;
                        propBuffer = IntPtr.Add(propBuffer, propLength + 4096);
                    }

                    int res;
                    fixed (nint* propArrayPtr = propArray)
                    {
                        res = Advapi32.QueryAllTracesW(propArrayPtr, traceCount, out traceCount);
                    }
                    if (res == 0x0000007A)  // ERROR_INSUFFICIENT_BUFFER
                    {
                        continue;
                    }
                    else if (res != 0)
                    {
                        throw new Win32Exception(res);
                    }

                    propBuffer = buffer;
                    for (int i = 0; i < traceCount; i++)
                    {
                        Advapi32.EVENT_TRACE_PROPERTIES_V2* prop = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)propBuffer;
                        nint stringPtr = IntPtr.Add(propBuffer, prop->LoggerNameOffset);
                        sessionNames.Add(Marshal.PtrToStringUni(stringPtr) ?? "");
                        propBuffer = IntPtr.Add(propBuffer, propLength + 4096);
                    }

                    break;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        return sessionNames.ToArray();
    }
}
