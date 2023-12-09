using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal static partial class Tdh
{
    [DllImport("Tdh.dll")]
    public static extern int TdhFormatProperty(
        ref TRACE_EVENT_INFO EventInfo,
        nint MapInfo,
        int PointerSize,
        short PropertyInType,
        short PropertyOutType,
        short PropertyLength,
        short UserDataLength,
        nint UserData,
        ref int BufferSize,
        nint Buffer,
        out short UserDataConsumed);
}
