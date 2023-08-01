using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSETW.Commands;

internal sealed class KeywordCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {

        yield return new("abc");
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
        WildcardPattern pattern = new($"{wordToComplete}*", WildcardOptions.IgnoreCase);

        foreach (EventProvider provider in PSETWGlobals.InstalledProviders)
        {
            string name = provider.Name;
            string value = provider.Id.ToString();
            if (
                name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) ||
                pattern.IsMatch(name) || pattern.IsMatch(value)
            )
            {
                yield return new(name, name, CompletionResultType.Text, $"Provider Guid: {value}");
            }
        }
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

        EventProvider[] cache = PSETWGlobals.InstalledProviders;
        string providerName = _providerString ?? "";
        foreach (EventProvider provider in cache)
        {
            if (provider.Name == providerName)
            {
                return provider.Id;
            }
        }

        throw new ArgumentException($"Unknown ETW provider '{providerName}'");
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
        string[] sessionNames = PSETWGlobals.TraceSessionNames;
        WildcardPattern pattern = new($"{wordToComplete}*", WildcardOptions.IgnoreCase);
        foreach (string name in sessionNames)
        {
            if (name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) || pattern.IsMatch(name))
            {
                yield return new(name);
            }
        }
    }
}
