using System;
using System.Diagnostics.Tracing;

namespace PSEtwProvider;

public enum IntEnum
{
    Value1,
    Value2,
    Value3,
    Value4,
    Value5
}

[Flags]
public enum IntFlags
{
    Value1 = 0,
    Value2 = 1,
    Value3 = 2,
    Value4 = 4,
    Value5 = 8
}

[EventSource(Name = "PSEtw-Event")]
public sealed class PSEtwEvent : EventSource
{
    public class Keywords
    {
        public const EventKeywords Foo = (EventKeywords)0x0001;
        public const EventKeywords Bar = (EventKeywords)0x0002;
    }

    [Event(1)]
    public void BasicEvent(int myId)
        => WriteEvent(1, myId);

    [Event(2, Level = EventLevel.LogAlways)]
    public void LevelLogAlways(int myId)
        => WriteEvent(2, myId);

    [Event(3, Level = EventLevel.Critical)]
    public void LevelCritical(int myId)
        => WriteEvent(3, myId);

    [Event(4, Level = EventLevel.Error)]
    public void LevelError(int myId)
        => WriteEvent(4, myId);

    [Event(5, Level = EventLevel.Warning)]
    public void LevelWarning(int myId)
        => WriteEvent(5, myId);

    [Event(6, Level = EventLevel.Informational)]
    public void LevelInformational(int myId)
        => WriteEvent(6, myId);

    [Event(7, Level = EventLevel.Verbose)]
    public void LevelVerbose(int myId)
        => WriteEvent(7, myId);

    [Event(8, Keywords = Keywords.Foo)]
    public void KeywordCustomFoo(int myId)
        => WriteEvent(8, myId);

    [Event(9, Keywords = Keywords.Bar)]
    public void KeywordCustomBar(int myId)
        => WriteEvent(9, myId);

    [Event(10, Keywords = Keywords.Foo | Keywords.Bar)]
    public void KeywordCustomFooBar(int myId)
        => WriteEvent(10, myId);

    [Event(11, Level = EventLevel.Informational)]
    public void TypeTest(
        bool boolValue,
        byte byteValue,
        byte[] byteArray,
        char charValue,
        DateTime dateTimeUtc,
        DateTime dateTimeLocal,
        DateTime dateTimeUnspecified,
        double doubleValue,
        IntEnum enumValue,
        IntFlags enumFlags,
        Guid guid,
        short int16,
        int int32,
        long int64,
        nint pointer,
        sbyte signedByte,
        float single,
        ushort uint16,
        uint uint32,
        ulong uint64
    ) => WriteEvent(
        11,
        new object[] {
            boolValue,
            byteValue,
            byteArray,
            charValue,
            dateTimeUtc,
            dateTimeLocal,
            dateTimeUnspecified,
            doubleValue,
            enumValue,
            enumFlags,
            guid,
            int16,
            int32,
            int64,
            pointer,
            signedByte,
            single,
            uint16,
            uint32,
            uint64
        });
}
