using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PSEtw.Share;

internal static class UnmanagedHelpers
{
    /// <summary>
    /// Converts a byte span to a string using the encoding specified.
    /// </summary>
    /// <param name="data">The span to decode.</param>
    /// <param name="encoding">Custom encoding to use, defaults to Unicode.</param>
    /// <returns>The decoded string.</returns>
    public static string SpanToString(ReadOnlySpan<byte> data, Encoding? encoding = null)
    {
        encoding ??= Encoding.Unicode;

#if NET6_0_OR_GREATER
        return encoding.GetString(data);
#else
        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                return encoding.GetString(dataPtr, data.Length);
            }
        }
#endif
    }

    /// <summary>
    /// Reads a null terminated Unicode string from the pointer provided.
    /// </summary>
    /// <param name="buffer">The buffer to read from</param>
    /// <param name="offset">The buffer offset to start reading from</param>
    /// <param name="length">The length of the string in characters, 0 will read to the null terminator</param>
    /// <returns>The string read</returns>
    public static string? ReadPtrStringUni(nint buffer, int offset, int length = 0)
    {
        if (offset == 0)
        {
            return null;
        }

        nint ptr = IntPtr.Add(buffer, offset);

        string? value = length > 0 ? Marshal.PtrToStringUni(ptr, length) : Marshal.PtrToStringUni(ptr);

        // Some event entries end with a space so we strip that.
        return value?.TrimEnd(' ');
    }

    /// <summary>
    /// Returns an array of strings from the pointer that points to the null
    /// terminated Unicode strings. The end of the array is marked by an empty
    /// string value.
    /// </summary>
    /// <param name="buffer">The pointer to the unmanaged string array.</param>
    /// <param name="offset">The offset from the pointer to read from.</param>
    /// <returns>The string array.</returns>
    public static string[] ReadPtrStringListUni(nint buffer, int offset)
    {
        if (offset == 0)
        {
            return Array.Empty<string>();
        }

        buffer = IntPtr.Add(buffer, offset);
        List<string> values = new();
        while (true)
        {
            string? value = Marshal.PtrToStringUni(buffer);
            if (string.IsNullOrEmpty(value))
            {
                break;
            }
            else
            {
                buffer = IntPtr.Add(buffer, (value!.Length + 1) * 2);
                values.Add(value.TrimEnd(' '));
            }
        }

        return values.ToArray();
    }
}
