using PSEtw.Shared;
using PSEtw.Shared.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSEtw;

internal sealed class KeywordCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        ProviderFieldInfo[] providerKeywords = CompletorHelper.GetProviderFields(
            fakeBoundParameters,
            EventFieldType.EventKeywordInformation);

        WildcardPattern pattern = new($"{wordToComplete}*", WildcardOptions.IgnoreCase);
        foreach (ProviderFieldInfo kwd in providerKeywords)
        {
            string name = kwd.Name;
            string description = string.Format("{0} 0x{1:X8}",
                string.IsNullOrWhiteSpace(kwd.Description) ? "Unknown description" : kwd.Description.TrimEnd(),
                kwd.Value
            );

            if (
                name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) ||
                pattern.IsMatch(name) || pattern.IsMatch(description)
            )
            {
                yield return CompletorHelper.GenerateResult(name, description);
            }
        }

        yield return new("*", "*", CompletionResultType.Text, "All keywords 0xFFFFFFFFFFFFFFFF");
    }
}

internal sealed class LevelCompletor : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        ProviderFieldInfo[] providerLevels = CompletorHelper.GetProviderFields(
            fakeBoundParameters,
            EventFieldType.EventLevelInformation);

        WildcardPattern pattern = new($"{wordToComplete}*", WildcardOptions.IgnoreCase);
        for (int i = 0; i < LevelStringOrInt.ReservedLevels.Length; i++)
        {
            string name = LevelStringOrInt.ReservedLevels[i];
            string description = string.Format("{0} 0x{1:X2}", name, i);
            if (
                name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) ||
                pattern.IsMatch(name)
            )
            {
                yield return new(name, name, CompletionResultType.Text, description);
            }
        }

        foreach (ProviderFieldInfo lvl in providerLevels)
        {
            if (lvl.Value < 6)
            {
                continue;
            }

            string name = lvl.Name;
            string description = string.Format("{0} 0x{1:X2}",
                string.IsNullOrWhiteSpace(lvl.Description) ? "Unknown level" : lvl.Description.TrimEnd(),
                lvl.Value
            );

            if (
                name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) ||
                pattern.IsMatch(name) || pattern.IsMatch(description)
            )
            {
                yield return CompletorHelper.GenerateResult(name, description);
            }
        }

        yield return new("*", "*", CompletionResultType.Text, "All levels 0xFF");
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

        foreach ((Guid providerId, string name) in ProviderHelper.GetProviders())
        {
            string value = providerId.ToString();
            if (
                name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) ||
                pattern.IsMatch(name) || pattern.IsMatch(value)
            )
            {
                yield return CompletorHelper.GenerateResult(name, $"Provider Guid: {value}");
            }
        }
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
        WildcardPattern pattern = new($"{wordToComplete}*", WildcardOptions.IgnoreCase);
        foreach (string name in ProviderHelper.QueryAllTraces())
        {
            if (name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) || pattern.IsMatch(name))
            {
                yield return CompletorHelper.GenerateResult(name, $"ETW Session {name}");
            }
        }
    }
}

internal sealed class CompletorHelper
{
    public static ProviderFieldInfo[] GetProviderFields(IDictionary fakeBoundParameters, EventFieldType fieldType)
    {
        if (fakeBoundParameters.Contains("Provider"))
        {
            ProviderStringOrGuid provider = new(fakeBoundParameters["Provider"]?.ToString() ?? "");
            Guid providerGuid = provider.GetProviderGuid();
            return ProviderHelper.GetProviderFieldInfo(providerGuid, fieldType);
        }
        else
        {
            return Array.Empty<ProviderFieldInfo>();
        }
    }

    public static CompletionResult GenerateResult(string value, string toolTip)
    {
        string completionText = value;
        if (Regex.Match(
            completionText,
            @"(^[<>@#])|([$\s'\u2018\u2019\u201a\u201b\u201c\u201d\u201e""`,;(){}|&])"
        ).Success)
        {
            completionText = $"'{CodeGeneration.EscapeSingleQuotedStringContent(completionText)}'";
        }

        return new(
            completionText,
            value,
            CompletionResultType.ParameterValue,
            toolTip
        );
    }
}
