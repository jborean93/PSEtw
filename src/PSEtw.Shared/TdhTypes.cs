using PSEtw.Shared.Native;
using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;

namespace PSEtw.Shared;

internal class TdhTypeReader
{
    internal static readonly Encoding ANSIEncoding = Encoding.GetEncoding(
        CultureInfo.CurrentCulture.TextInfo.ANSICodePage);

    protected short? StoreInteger { get; set; } = null;

    protected virtual ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length)
        => data.Slice(0, length);

    public static object Transform(
        string? propertyName,
        int pointerSize,
        ReadOnlySpan<byte> data,
        short length,
        TdhInType inType,
        TdhOutType outType,
        out short? storeInteger)
    {
        (TdhTypeReader reader, TdhOutType defaultOutType) = GetReader(
            propertyName, pointerSize, inType);
        if (outType == TdhOutType.TDH_OUTTYPE_NULL)
        {
            outType = defaultOutType;
        }

        ValueTransformer transformer = GetTransformer(propertyName, outType);
        object? value = transformer(reader, data, length);
        if (value == null)
        {
            string msg =
                $"No valid transformations of {reader.GetType().Name} to " +
                $"{transformer.GetType().Name} for '{propertyName}'";
            throw new NotImplementedException(msg);
        }

        storeInteger = reader.StoreInteger;

        return value;
    }

    protected static string SpanToString(ReadOnlySpan<byte> data, Encoding encoding)
    {
#if NET6_0_OR_GREATER
        return encoding.GetString(data);
#else
        unsafe
        {
            fixed (byte* bytePtr = data)
            {
                return encoding.GetString(bytePtr, data.Length);
            }
        }
#endif
    }

    private static (TdhTypeReader, TdhOutType) GetReader(
        string? propertyName,
        int pointerSize,
        TdhInType inType
    ) => inType switch
    {
        TdhInType.TDH_INTYPE_UNICODESTRING => (
            new TdhNullTerminatedStringReader(Encoding.Unicode),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_ANSISTRING => (
            new TdhNullTerminatedStringReader(ANSIEncoding),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_INT8 => (
            new TdhFixedLengthReader(1),
            TdhOutType.TDH_OUTTYPE_BYTE),

        TdhInType.TDH_INTYPE_UINT8 => (
            new TdhFixedLengthReader(1),
            TdhOutType.TDH_OUTTYPE_UNSIGNEDBYTE),

        TdhInType.TDH_INTYPE_INT16 => (
            new TdhFixedLengthReader(2),
            TdhOutType.TDH_OUTTYPE_SHORT),

        TdhInType.TDH_INTYPE_UINT16 => (
            new TdhFixedLengthReader(2),
            TdhOutType.TDH_OUTTYPE_UNSIGNEDSHORT),

        TdhInType.TDH_INTYPE_INT32 => (
            new TdhFixedLengthReader(4),
            TdhOutType.TDH_OUTTYPE_INT),

        TdhInType.TDH_INTYPE_UINT32 => (
            new TdhFixedLengthReader(4),
            TdhOutType.TDH_OUTTYPE_UNSIGNEDINT),

        TdhInType.TDH_INTYPE_INT64 => (
            new TdhFixedLengthReader(8),
            TdhOutType.TDH_OUTTYPE_LONG),

        TdhInType.TDH_INTYPE_UINT64 => (
            new TdhFixedLengthReader(8),
            TdhOutType.TDH_OUTTYPE_UNSIGNEDLONG),

        TdhInType.TDH_INTYPE_FLOAT => (
            new TdhFixedLengthReader(4),
            TdhOutType.TDH_OUTTYPE_FLOAT),

        TdhInType.TDH_INTYPE_DOUBLE => (
            new TdhFixedLengthReader(8),
            TdhOutType.TDH_OUTTYPE_DOUBLE),

        TdhInType.TDH_INTYPE_BOOLEAN => (
            new TdhFixedLengthReader(4),
            TdhOutType.TDH_OUTTYPE_BOOLEAN),

        TdhInType.TDH_INTYPE_BINARY => (
            new TdhTypeReader(),
            TdhOutType.TDH_OUTTYPE_HEXBINARY),

        TdhInType.TDH_INTYPE_GUID => (
            new TdhFixedLengthReader(16),
            TdhOutType.TDH_OUTTYPE_GUID),

        TdhInType.TDH_INTYPE_POINTER => (
            new TdhFixedLengthReader(pointerSize),
            TdhOutType.TDH_OUTTYPE_HEXINT64),

        TdhInType.TDH_INTYPE_FILETIME => (
            new TdhFixedLengthReader(8),
            TdhOutType.TDH_OUTTYPE_DATETIME),

        TdhInType.TDH_INTYPE_SYSTEMTIME => (
            new TdhFixedLengthReader(16),
            TdhOutType.TDH_OUTTYPE_DATETIME),

        TdhInType.TDH_INTYPE_SID => (
            new TdhSecurityIdentifierReader(),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_HEXINT32 => (
            new TdhFixedLengthReader(4),
            TdhOutType.TDH_OUTTYPE_HEXINT32),

        TdhInType.TDH_INTYPE_HEXINT64 => (
            new TdhFixedLengthReader(8),
            TdhOutType.TDH_OUTTYPE_HEXINT64),

        TdhInType.TDH_INTYPE_MANIFEST_COUNTEDSTRING => (
            new TdhCountedLengthReader(Encoding.Unicode, true),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_MANIFEST_COUNTEDANSISTRING => (
            new TdhCountedLengthReader(ANSIEncoding, true),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_MANIFEST_COUNTEDBINARY => (
            new TdhCountedLengthReader(Encoding.Unicode, true),
            TdhOutType.TDH_OUTTYPE_HEXBINARY),

        TdhInType.TDH_INTYPE_COUNTEDSTRING => (
            new TdhCountedLengthReader(Encoding.Unicode, true),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_COUNTEDANSISTRING => (
            new TdhCountedLengthReader(ANSIEncoding, true),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_REVERSEDCOUNTEDSTRING => (
            new TdhCountedLengthReader(Encoding.Unicode, false),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_REVERSEDCOUNTEDANSISTRING => (
            new TdhCountedLengthReader(ANSIEncoding, false),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_NONNULLTERMINATEDSTRING => (
            new TdhNullTerminatedStringReader(Encoding.Unicode),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_NONNULLTERMINATEDANSISTRING => (
            new TdhNullTerminatedStringReader(ANSIEncoding),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_UNICODECHAR => (
            new TdhFixedLengthReader(2),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_ANSICHAR => (
            new TdhFixedLengthReader(1),
            TdhOutType.TDH_OUTTYPE_STRING),

        TdhInType.TDH_INTYPE_SIZET => (
            new TdhFixedLengthReader(pointerSize),
            TdhOutType.TDH_OUTTYPE_HEXINT64),

        TdhInType.TDH_INTYPE_HEXDUMP => (
            new TdhCountedLengthReader(Encoding.Unicode, true, lengthIsShort: false),
            TdhOutType.TDH_OUTTYPE_HEXBINARY),

        TdhInType.TDH_INTYPE_WBEMSID => (
            new TdhSecurityIdentifierReader(),
            TdhOutType.TDH_OUTTYPE_STRING),

        _ => throw new NotImplementedException($"Property '{propertyName}' has unknown input type, cannot unpack"),
    };

    private static ValueTransformer GetTransformer(
        string? propertyName,
        TdhOutType outType
    ) => outType switch
    {
        TdhOutType.TDH_OUTTYPE_STRING => ValueTransformerString,
        TdhOutType.TDH_OUTTYPE_DATETIME => ValueTransformerDateTimeUnspecified,
        TdhOutType.TDH_OUTTYPE_BYTE => ValueTransformerSignedByte,
        TdhOutType.TDH_OUTTYPE_UNSIGNEDBYTE => ValueTransformerByte,
        TdhOutType.TDH_OUTTYPE_SHORT => ValueTransformerShort,
        TdhOutType.TDH_OUTTYPE_UNSIGNEDSHORT => ValueTransformerUnsignedShort,
        TdhOutType.TDH_OUTTYPE_INT => ValueTransformerInt,
        TdhOutType.TDH_OUTTYPE_UNSIGNEDINT => ValueTransformerUnsignedInt,
        TdhOutType.TDH_OUTTYPE_LONG => ValueTransformerLong,
        TdhOutType.TDH_OUTTYPE_UNSIGNEDLONG => ValueTransformerUnsignedLong,
        TdhOutType.TDH_OUTTYPE_FLOAT => ValueTransformerFloat,
        TdhOutType.TDH_OUTTYPE_DOUBLE => ValueTransformerDouble,
        TdhOutType.TDH_OUTTYPE_BOOLEAN => ValueTransformerBoolean,
        TdhOutType.TDH_OUTTYPE_GUID => ValueTransformerGuid,
        TdhOutType.TDH_OUTTYPE_HEXBINARY => ValueTransformerByteArray,
        TdhOutType.TDH_OUTTYPE_HEXINT8 => ValueTransformerByte,
        TdhOutType.TDH_OUTTYPE_HEXINT16 => ValueTransformerShort,
        TdhOutType.TDH_OUTTYPE_HEXINT32 => ValueTransformerInt,
        TdhOutType.TDH_OUTTYPE_HEXINT64 => ValueTransformerLong,
        TdhOutType.TDH_OUTTYPE_PID => ValueTransformerInt,
        TdhOutType.TDH_OUTTYPE_TID => ValueTransformerInt,
        TdhOutType.TDH_OUTTYPE_PORT => ValueTransformerPort,
        TdhOutType.TDH_OUTTYPE_IPV4 => ValueTransformerIPAddress,
        TdhOutType.TDH_OUTTYPE_IPV6 => ValueTransformerIPAddress,
        // TdhOutType.TDH_OUTTYPE_SOCKETADDRESS => ValueTransformer,
        // TdhOutType.TDH_OUTTYPE_CIMDATETIME => ValueTransformer,
        // TdhOutType.TDH_OUTTYPE_ETWTIME => ValueTransformer,
        // TdhOutType.TDH_OUTTYPE_XML => ValueTransformer,
        TdhOutType.TDH_OUTTYPE_ERRORCODE => ValueTransformerInt,
        TdhOutType.TDH_OUTTYPE_WIN32ERROR => ValueTransformerInt,
        TdhOutType.TDH_OUTTYPE_NTSTATUS => ValueTransformerInt,
        TdhOutType.TDH_OUTTYPE_HRESULT => ValueTransformerInt,
        TdhOutType.TDH_OUTTYPE_CULTURE_INSENSITIVE_DATETIME => ValueTransformerDateTimeUnspecified,
        TdhOutType.TDH_OUTTYPE_JSON => ValueTransformerString,
        TdhOutType.TDH_OUTTYPE_UTF8 => ValueTransformerUtf8String,
        TdhOutType.TDH_OUTTYPE_PKCS7_WITH_TYPE_INFO => ValueTransformerByteArray,
        TdhOutType.TDH_OUTTYPE_CODE_POINTER => ValueTransformerLong,
        TdhOutType.TDH_OUTTYPE_DATETIME_UTC => ValueTransformerDateTimeUtc,
        // TdhOutType.TDH_OUTTYPE_REDUCEDSTRING => ValueTransformer,
        // TdhOutType.TDH_OUTTYPE_NOPRINT => ValueTransformer,
        _ => throw new NotImplementedException($"Property '{propertyName}' has unknown output type, cannot unpack"),
    };

    protected delegate object? ValueTransformer(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length);

    private static ReadOnlySpan<byte> GetBytes(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => reader.GetBytes(data, length);

    private static string? ValueTransformerString(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length)
    {
        if (reader is ITdhStringReader stringReader)
        {
            return stringReader.GetString(data, length, null);
        }

        return null;
    }

    private static string? ValueTransformerUtf8String(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length)
    {
        if (reader is ITdhStringReader stringReader)
        {
            return stringReader.GetString(data, length, Encoding.UTF8);
        }

        return null;
    }

    private static object? ValueTransformerDateTime(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        DateTimeKind kind)
    {
        ReadOnlySpan<byte> raw = GetBytes(reader, data, length);
        if (raw.Length == 8)
        {
            long ft = BinaryPrimitives.ReadInt64LittleEndian(raw);
            return DateTime.SpecifyKind(DateTime.FromFileTime(ft), kind);
        }
        else if (raw.Length == Marshal.SizeOf<SYSTEMTIME>())
        {
            unsafe
            {
                ReadOnlySpan<SYSTEMTIME> st = MemoryMarshal.Cast<byte, SYSTEMTIME>(raw);

                return new DateTime(
                    st[0].wYear,
                    st[0].wMonth,
                    st[0].wDay,
                    st[0].wHour,
                    st[0].wMinute,
                    st[0].wSecond,
                    st[0].wMilliseconds,
                    kind);
            }

        }

        return null;
    }

    private static object? ValueTransformerDateTimeUnspecified(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => ValueTransformerDateTime(reader, data, length, DateTimeKind.Unspecified);

    private static object? ValueTransformerDateTimeUtc(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => ValueTransformerDateTime(reader, data, length, DateTimeKind.Utc);

    private static object? ValueTransformerByte(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => GetBytes(reader, data, length)[0];

    private static object? ValueTransformerSignedByte(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => unchecked((sbyte)GetBytes(reader, data, length)[0]);

    private static object? ValueTransformerShort(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => BinaryPrimitives.ReadInt16LittleEndian(GetBytes(reader, data, length));

    private static object? ValueTransformerUnsignedShort(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => BinaryPrimitives.ReadUInt16LittleEndian(GetBytes(reader, data, length));

    private static object? ValueTransformerInt(
         TdhTypeReader reader,
         ReadOnlySpan<byte> data,
         short length
    ) => BinaryPrimitives.ReadInt32LittleEndian(GetBytes(reader, data, length));

    private static object? ValueTransformerUnsignedInt(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => BinaryPrimitives.ReadUInt32LittleEndian(GetBytes(reader, data, length));

    private static object? ValueTransformerLong(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => BinaryPrimitives.ReadInt64LittleEndian(GetBytes(reader, data, length));

    private static object? ValueTransformerUnsignedLong(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => BinaryPrimitives.ReadUInt64LittleEndian(GetBytes(reader, data, length));

    private static object? ValueTransformerFloat(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length)
    {
        ReadOnlySpan<byte> raw = GetBytes(reader, data, length);
#if NET6_0_OR_GREATER
        return BinaryPrimitives.ReadSingleLittleEndian(raw);
#else
        return BitConverter.ToSingle(raw.ToArray(), 0);
#endif
    }

    private static object? ValueTransformerDouble(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length)
    {
        ReadOnlySpan<byte> raw = GetBytes(reader, data, length);
#if NET6_0_OR_GREATER
        return BinaryPrimitives.ReadDoubleLittleEndian(raw);
#else
        return BitConverter.ToDouble(raw.ToArray(), 0);
#endif
    }

    private static object? ValueTransformerBoolean(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length)
    {
        foreach (byte b in GetBytes(reader, data, length))
        {
            if (b != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static object? ValueTransformerGuid(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length)
    {
        ReadOnlySpan<byte> raw = GetBytes(reader, data, length);

#if NET6_0_OR_GREATER
        return new Guid(raw);
#else
        return new Guid(raw.ToArray());
#endif
    }

    private static object? ValueTransformerByteArray(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => GetBytes(reader, data, length).ToArray();

    private static object? ValueTransformerPort(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length
    ) => (int)BinaryPrimitives.ReadUInt16BigEndian(GetBytes(reader, data, length));

    private static object? ValueTransformerIPAddress(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length)
    {
        ReadOnlySpan<byte> raw = GetBytes(reader, data, length);

#if NET6_0_OR_GREATER
        return new IPAddress(raw);
#else
        return new IPAddress(raw.ToArray());
#endif
    }
}

internal interface ITdhStringReader
{
    public string GetString(ReadOnlySpan<byte> data, int length, Encoding? encoding);
}

internal class TdhNullTerminatedStringReader : TdhTypeReader, ITdhStringReader
{
    private Encoding _encoding;

    public TdhNullTerminatedStringReader(Encoding encoding)
    {
        _encoding = encoding;
    }

    protected override ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length)
        => GetBytes(data, length, _encoding);

    private ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length, Encoding encoding)
    {
        // int charSize = encoding.GetByteCount(new[] { (char)0 });
        Span<byte> nullTerminator = encoding.GetBytes(new[] { (char)0 });

        if (length > 0)
        {
            return data.Slice(0, length * nullTerminator.Length);
        }

        int nullIdx = data.IndexOf(nullTerminator);
        if (nullIdx == -1)
        {
            return data;
        }
        else
        {
            // We don't want to include the null terminator in the final bytes.
            return data.Slice(0, nullIdx - 1);
        }
    }

    public string GetString(ReadOnlySpan<byte> data, int length, Encoding? encoding)
    {
        encoding ??= _encoding;
        ReadOnlySpan<byte> raw = GetBytes(data, length, encoding);

        return SpanToString(raw, encoding);
    }
}

internal class TdhFixedLengthReader : TdhTypeReader, ITdhStringReader
{
    private int _size;

    public Encoding StringEncoding => _size == 1 ? ANSIEncoding : Encoding.Unicode;

    internal TdhFixedLengthReader(int size)
    {
        _size = size;
    }

    protected override ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length)
    {
        ReadOnlySpan<byte> actualData = data.Slice(0, _size);
        switch (_size)
        {
            case 1:
                StoreInteger = actualData[0];
                break;
            case 2:
                StoreInteger = BinaryPrimitives.ReadInt16LittleEndian(actualData);
                break;
            case 4:
                StoreInteger = (short)(BinaryPrimitives.ReadInt32LittleEndian(actualData) & 0xFFFF);
                break;
            case 8:
                StoreInteger = (short)(BinaryPrimitives.ReadInt64LittleEndian(actualData) & 0xFFFF);
                break;
        }

        return actualData;
    }

    public string GetString(ReadOnlySpan<byte> data, int length, Encoding? encoding)
    {
        ReadOnlySpan<byte> raw = GetBytes(data, length);
        if (encoding == null)
        {
            if (raw.Length == 1)
            {
                encoding = ANSIEncoding;
            }
            else if (raw.Length == 2)
            {
                encoding = Encoding.Unicode;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot get string of fixed length input data of size {raw.Length}");
            }
        }

        return SpanToString(raw, encoding);
    }
}

internal class TdhCountedLengthReader : TdhTypeReader, ITdhStringReader
{
    private Encoding _encoding;
    private bool _littleEndian;
    private bool _lengthIsShort;

    internal TdhCountedLengthReader(Encoding encoding, bool littleEndian, bool lengthIsShort = true)
    {
        _encoding = encoding;
        _littleEndian = littleEndian;
        _lengthIsShort = lengthIsShort;
    }

    protected override ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length)
    {
        int size = 2;
        if (_lengthIsShort)
        {
            size = 4;
            length = BinaryPrimitives.ReadInt32BigEndian(data);
        }
        else if (_littleEndian)
        {
            length = BinaryPrimitives.ReadUInt16LittleEndian(data);
        }
        else
        {
            length = BinaryPrimitives.ReadUInt16BigEndian(data);
        }

        return data.Slice(size, length);
    }

    public string GetString(ReadOnlySpan<byte> data, int length, Encoding? encoding)
        => SpanToString(GetBytes(data, length), encoding ?? _encoding);
}

internal class TdhSecurityIdentifierReader : TdhTypeReader, ITdhStringReader
{
    protected override ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length)
    {
        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                length = Advapi32.GetLengthSid((nint)dataPtr);
            }
        }

        return data.Slice(0, length);
    }

#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public string GetString(ReadOnlySpan<byte> data, int length, Encoding? encoding)
    {
        ReadOnlySpan<byte> sidBytes = GetBytes(data, 0);
        unsafe
        {
            fixed (byte* sidPtr = sidBytes)
            {
                return new SecurityIdentifier((nint)sidPtr).ToString();
            }
        }
    }
}
