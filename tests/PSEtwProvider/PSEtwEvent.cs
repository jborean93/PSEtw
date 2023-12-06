using System;
using System.Diagnostics.Tracing;

namespace PSEtwProvider;

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
}
