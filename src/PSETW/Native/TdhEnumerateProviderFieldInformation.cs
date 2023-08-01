using System;
using System.Runtime.InteropServices;

namespace PSETW.Native;

internal partial class Tdh
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PROVIDER_FIELD_INFOARRAY
    {
        public int NumberOfElements;
        public EventFieldType FieldType;
        // public PROVIDER_FIELD_INFO FieldInfoArray[ANYSIZE_ARRAY];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROVIDER_FIELD_INFO
    {
        public int NameOffset;
        public int DescriptionOffset;
        public long Value;
    }

    [DllImport("Tdh.dll")]
    public static extern int TdhEnumerateProviderFieldInformation(
        ref Guid pGuid,
        EventFieldType EventFieldType,
        nint pBuffer,
        ref int pBufferSize);
}

public enum EventFieldType
{
    EventKeywordInformation = 0,
    EventLevelInformation,
    EventChannelInformation,
    EventTaskInformation,
    EventOpcodeInformation,
    EventInformationMax
}
