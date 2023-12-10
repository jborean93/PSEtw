using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal static partial class Advapi32
{
    [DllImport("Advapi32.dll")]
    public static extern int GetLengthSid(
        nint pSid);
}
