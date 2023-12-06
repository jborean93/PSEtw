using PSEtw.Shared.Native;
using System;
using System.Linq;
using System.Management.Automation;

namespace PSEtw.Shared;

public sealed class EtwEventInfo
{
    public Guid Provider { get; }

    public long KeywordsAny { get; }

    public long KeywordsAll { get; }

    public byte Level { get; }

    internal EtwEventInfo(Guid provider, long keywordsAny, long keywordsAll, byte level)
    {
        Provider = provider;
        KeywordsAny = keywordsAny;
        KeywordsAll = keywordsAll;
        Level = level;
    }

    internal static EtwEventInfo Create(
        ProviderStringOrGuid provider,
        KeywordsStringOrLong[] keywordsAll,
        KeywordsStringOrLong[] keywordsAny,
        LevelStringOrInt? level)
    {
        Guid providerGuid = provider.GetProviderGuid();
        ProviderFieldInfo[] keywords = ProviderHelper.GetProviderFieldInfo(providerGuid,
            EventFieldType.EventKeywordInformation);

        long rawKeywordsAll = 0;
        foreach (KeywordsStringOrLong kwd in keywordsAll)
        {
            rawKeywordsAll |= kwd.GetKeywordLong(keywords);
        }

        long rawKeywordsAny = 0;
        foreach (KeywordsStringOrLong kwd in keywordsAny)
        {
            rawKeywordsAny |= kwd.GetKeywordLong(keywords);
        }

        ProviderFieldInfo[] levels = ProviderHelper.GetProviderFieldInfo(providerGuid,
            EventFieldType.EventLevelInformation);
        byte rawLevel = level?.GetLevelInt(levels) ?? 4; // Info

        return new(
            providerGuid,
            rawKeywordsAny,
            rawKeywordsAll,
            rawLevel);
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
        if (LanguagePrimitives.TryConvertTo(value, out long longValue))
        {
            _keywordLong = longValue;
        }
        else
        {
            _keywordString = value;
        }
    }

    internal long GetKeywordLong(ProviderFieldInfo[] validKeywords)
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

        foreach (ProviderFieldInfo kwd in validKeywords)
        {
            if (kwd.Name.Equals(keywordName, StringComparison.OrdinalIgnoreCase))
            {
                return kwd.Value;
            }
        }

        throw new ArgumentException($"Unknown provider keyword '{keywordName}'");
    }
}

public sealed class LevelStringOrInt
{
    private int? _levelInt;
    private string? _levelString;

    internal static string[] ReservedLevels = new[]
    {
        "LogAlways",
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
        if (LanguagePrimitives.TryConvertTo(value, out byte byteValue))
        {
            _levelInt = byteValue;
        }
        else
        {
            _levelString = value;
        }
    }

    internal byte GetLevelInt(ProviderFieldInfo[] validLevels)
    {
        if (_levelInt != null)
        {
            if (_levelInt > Byte.MaxValue)
            {
                throw new ArgumentException(
                    $"Provider level {_levelInt} must be less than or equal to {Byte.MaxValue}");
            }

            return (byte)_levelInt;
        }

        string levelName = _levelString ?? "";
        if (levelName == "*")
        {
            return 0xFF;
        }
        else if (ReservedLevels.Contains(levelName))
        {
            return (byte)Array.IndexOf(ReservedLevels, levelName);
        }

        foreach (ProviderFieldInfo lvl in validLevels)
        {
            if (lvl.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
            {
                return (byte)lvl.Value;
            }
        }

        throw new ArgumentException($"Unknown provider level '{levelName}'");
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
        if (LanguagePrimitives.TryConvertTo(value, out Guid guidValue))
        {
            _providerGuid = guidValue;
        }
        else
        {
            _providerString = value;
        }
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
