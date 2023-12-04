using PSEtw.Shared;
using PSEtw.Shared.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace PSEtw.Commands;

internal sealed class KeywordCompleter : IArgumentCompleter
{
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        FieldInfo[] providerKeywords = Array.Empty<FieldInfo>();
        if (fakeBoundParameters.Contains("Provider"))
        {
            ProviderStringOrGuid provider = new(fakeBoundParameters["Provider"]?.ToString() ?? "");
            // catch ArgumentException
            Guid providerGuid = provider.GetProviderGuid();
            // catch Win32Exception
            providerKeywords = ProviderHelper.GetProviderFieldInfo(providerGuid, EventFieldType.EventKeywordInformation);
        }

        WildcardPattern pattern = new($"{wordToComplete}*", WildcardOptions.IgnoreCase);
        foreach (FieldInfo kwd in providerKeywords)
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
                yield return CompletionHelper.GenerateResult(name, description);
            }
        }

        yield return new("*", "*", CompletionResultType.Text, "All keywords 0xFFFFFFFFFFFFFFFF");
    }
}

public sealed class KeywordsStringOrLong
{
    private long? _keywordLong;
    private string? _keywordString;

    public KeywordsStringOrLong(int value)
    {
        _keywordLong = value;
    }

    public KeywordsStringOrLong(long value)
    {
        _keywordLong = value;
    }

    public KeywordsStringOrLong(string value)
    {
        if (Int64.TryParse(value, out var longValue))
        {
            _keywordLong = longValue;
        }
        else
        {
            _keywordString = value;
        }
    }

    internal long GetKeywordLong(FieldInfo[] validKeywords)
    {
        if (_keywordLong != null)
        {
            return (long)_keywordLong;
        }

        string keywordName = _keywordString ?? "";
        if (keywordName == "*")
        {
            return -1;
        }

        foreach (FieldInfo kwd in validKeywords)
        {
            if (kwd.Name.Equals(keywordName, StringComparison.OrdinalIgnoreCase))
            {
                return kwd.Value;
            }
        }

        throw new ArgumentException($"Unknown provider keyword '{keywordName}'");
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
        FieldInfo[] providerLevels = Array.Empty<FieldInfo>();
        if (fakeBoundParameters.Contains("Provider"))
        {
            ProviderStringOrGuid provider = new(fakeBoundParameters["Provider"]?.ToString() ?? "");
            // catch ArgumentException
            Guid providerGuid = provider.GetProviderGuid();
            // catch Win32Exception
            providerLevels = ProviderHelper.GetProviderFieldInfo(providerGuid, EventFieldType.EventLevelInformation);
        }

        WildcardPattern pattern = new($"{wordToComplete}*", WildcardOptions.IgnoreCase);

        for (int i = 0; i < LevelStringOrInt.ReservedLevels.Length; i++)
        {
            string name = LevelStringOrInt.ReservedLevels[i];
            string description = string.Format("{0} 0x{1:X2}", name, i + 1);
            if (
                name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) ||
                pattern.IsMatch(name)
            )
            {
                yield return new(name, name, CompletionResultType.Text, description);
            }
        }

        foreach (FieldInfo lvl in providerLevels)
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
                yield return CompletionHelper.GenerateResult(name, description);
            }
        }

        yield return new("*", "*", CompletionResultType.Text, "All levels 0xFF");
    }
}

public sealed class LevelStringOrInt
{
    private int? _levelInt;
    private string? _levelString;

    internal static string[] ReservedLevels = new[]
    {
        "Critical",
        "Error",
        "Warning",
        "Info",
        "Verbose"
    };

    public LevelStringOrInt(int value)
    {
        _levelInt = value;
    }

    public LevelStringOrInt(string value)
    {
        _levelString = value;
    }

    internal int GetLevelInt(FieldInfo[] validLevels)
    {
        if (_levelInt != null)
        {
            return (int)_levelInt;
        }

        string levelName = _levelString ?? "";
        if (levelName == "*")
        {
            return 0xFF;
        }
        else if (ReservedLevels.Contains(levelName))
        {
            return Array.IndexOf(ReservedLevels, levelName) + 1;
        }

        foreach (FieldInfo lvl in validLevels)
        {
            if (lvl.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
            {
                return (int)lvl.Value;
            }
        }

        throw new ArgumentException($"Unknown provider level '{levelName}'");
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
                yield return CompletionHelper.GenerateResult(name, $"Provider Guid: {value}");
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

        string providerName = _providerString ?? "";
        foreach ((Guid providerId, string name) in ProviderHelper.GetProviders())
        {
            if (name.Equals(providerName, StringComparison.OrdinalIgnoreCase))
            {
                return providerId;
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
        WildcardPattern pattern = new($"{wordToComplete}*", WildcardOptions.IgnoreCase);
        foreach (string name in ProviderHelper.QueryAllTraces())
        {
            if (name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase) || pattern.IsMatch(name))
            {
                yield return CompletionHelper.GenerateResult(name, $"ETW Session {name}");
            }
        }
    }
}

internal sealed class CompletionHelper
{
    public static CompletionResult GenerateResult(string value, string toolTip)
    {
        string completionText = value;
        if (Regex.Match(completionText, @"(^[<>@#])|([$\s'\u2018\u2019\u201a\u201b\u201c\u201d\u201e""`,;(){}|&])").Success)
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
