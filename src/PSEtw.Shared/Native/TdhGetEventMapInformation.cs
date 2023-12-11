using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal partial class Tdh
{
    [DllImport("Tdh.dll")]
    public static extern int TdhGetEventMapInformation(
        ref Advapi32.EVENT_RECORD pEvent,
        nint pMapName,
        nint pBuffer,
        ref int pBufferSize);
}
