using System;
using System.Runtime.InteropServices;

namespace PSETW.Native;

internal partial class Tdh
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PROVIDER_ENUMERATION_INFO
    {
        public int NumberOfProviders;
        public int Reserved;
        // public TRACE_PROVIDER_INFO TraceProviderInfoArray[ANYSIZE_ARRAY];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TRACE_PROVIDER_INFO
    {
        public Guid ProviderGuid;
        public int SchemaSource;
        public int ProviderNameoffset;
    }

    [DllImport("Tdh.dll")]
    public static extern int TdhEnumerateProviders(
        nint pBuffer,
        ref int pBufferSize);
}
