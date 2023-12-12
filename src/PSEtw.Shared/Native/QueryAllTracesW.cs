using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal partial class Advapi32
{
    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode)]
    public unsafe static extern int QueryAllTracesW(
        nint* PropertyArray,
        int PropertyArrayCount,
        out int LoggerCount);
}
