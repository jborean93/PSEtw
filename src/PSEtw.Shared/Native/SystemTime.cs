using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct SYSTEMTIME
{
    public short wYear;
    public short wMonth;
    public short wDayOfWeek;
    public short wDay;
    public short wHour;
    public short wMinute;
    public short wSecond;
    public short wMilliseconds;
}
