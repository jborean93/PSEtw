using System;
using System.Runtime.InteropServices;

namespace PSETW.Native;

internal partial class Advapi32
{
    [DllImport("Advapi32.dll", SetLastError = true)]
    public static extern int CloseTrace(
        long TraceHandle);
}
