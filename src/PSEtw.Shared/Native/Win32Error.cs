using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal static class Win32Error
{
    public const int ERROR_SUCCESS = 0x00000000;
    public const int ERROR_INSUFFICIENT_BUFFER = 0x0000007A;
    public const int ERROR_MORE_DATA = 0x000000EA;
    public const int ERROR_NOT_FOUND = 0x00000490;

    public static void ThrowIfError(int err)
    {
        if (err != ERROR_SUCCESS)
        {
            throw new Win32Exception(err);
        }
    }
}
