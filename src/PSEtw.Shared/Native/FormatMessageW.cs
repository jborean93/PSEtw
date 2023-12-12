using System;
using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal partial class Kernel32
{
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int FormatMessageW(
        int dwFlags,
        nint lpSource,
        int dwMessageId,
        int dwLanguageId,
        ref nint lpBuffer,
        int nSize,
        string[] Arguments);
}

[Flags]
public enum FormatMessageFlags
{
    FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
    FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
    FORMAT_MESSAGE_FROM_STRING = 0x00000400,
    FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
    FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
    FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
    FORMAT_MESSAGE_MAX_WIDTH_MASK = 0x000000FF,

}
