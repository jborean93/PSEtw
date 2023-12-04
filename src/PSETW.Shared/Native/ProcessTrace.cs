using System;
using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal partial class Advapi32
{
    [DllImport("Advapi32.dll", SetLastError = true)]
    public static extern int ProcessTrace(
        nint HandleArray,
        int HandleCount,
        nint StartTime,
        nint EndTime);
}
