using PSEtw.Shared.Native;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace PSEtw.Shared;

internal abstract class TdhTypeReader
{
    public short? StoreInteger { get; protected set; } = null;

    internal abstract TdhTypeTransformer GetDefaultTransformer();
    internal abstract ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length);

    public static TdhTypeReader Create(
        string? propertyName,
        HeaderFlags headerFlags,
        TdhInType inType
    ) => inType switch
    {
        TdhInType.TDH_INTYPE_UNICODESTRING => new TdhStringReader(Encoding.Unicode),
        TdhInType.TDH_INTYPE_ANSISTRING => new TdhStringReader(TdhStringReader.ANSIEncoding),
        TdhInType.TDH_INTYPE_INT8 => new TdhIntegerReader<TdhByteTransformer>(1),
        TdhInType.TDH_INTYPE_UINT8 => new TdhIntegerReader<TdhUnsignedByteTransformer>(1),
        TdhInType.TDH_INTYPE_INT16 => new TdhIntegerReader<TdhShortTransformer>(2),
        TdhInType.TDH_INTYPE_UINT16 => new TdhIntegerReader<TdhUnsignedShortTransformer>(2),
        TdhInType.TDH_INTYPE_INT32 => new TdhIntegerReader<TdhIntTransformer>(4),
        TdhInType.TDH_INTYPE_UINT32 => new TdhIntegerReader<TdhUnsignedIntTransformer>(4),
        TdhInType.TDH_INTYPE_INT64 => new TdhIntegerReader<TdhLongTransformer>(8),
        TdhInType.TDH_INTYPE_UINT64 => new TdhIntegerReader<TdhUnsignedLongTransformer>(8),
        // TdhInType.TDH_INTYPE_FLOAT => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_DOUBLE => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_BOOLEAN => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_BINARY => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_GUID => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_POINTER => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_FILETIME => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_SYSTEMTIME => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_SID => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_HEXINT32 => new TdhIntegerReader<TdhByteTransformer>(4),
        // TdhInType.TDH_INTYPE_HEXINT64 => new TdhIntegerReader<TdhByteTransformer>(8),
        // TdhInType.TDH_INTYPE_MANIFEST_COUNTEDSTRING => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_MANIFEST_COUNTEDANSISTRING => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_MANIFEST_COUNTEDBINARY => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_COUNTEDSTRING => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_COUNTEDANSISTRING => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_REVERSEDCOUNTEDSTRING => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_REVERSEDCOUNTEDANSISTRING => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_NONNULLTERMINATEDSTRING => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_NONNULLTERMINATEDANSISTRING => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_UNICODECHAR => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_ANSICHAR => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_SIZET => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_HEXDUMP => new TdhIntType(propertyName),
        // TdhInType.TDH_INTYPE_WBEMSID => new TdhIntType(propertyName),
        _ => throw new NotImplementedException($"Property '{propertyName}' has unknown input type, cannot unpack"),
    };

    // private static object GetDataOutValue(
    //     string name,
    //     HeaderFlags headerFlags,
    //     ReadOnlySpan<byte> data,
    //     short length,
    //     TdhInType inType,
    //     TdhOutType outType,
    //     Span<short> integerValue,
    //     int index,
    //     out int consumed)
    // {
    //     consumed = 0;
    //     TdhOutType defaultOutType;
    //     object? value = null;
    //     switch (inType)
    //     {
    //         case TdhInType.TDH_INTYPE_INT8:
    //         case TdhInType.TDH_INTYPE_UINT8:
    //         case TdhInType.TDH_INTYPE_ANSICHAR:
    //             defaultOutType = inType switch
    //             {
    //                 TdhInType.TDH_INTYPE_INT8 => TdhOutType.TDH_OUTTYPE_BYTE,
    //                 TdhInType.TDH_INTYPE_UINT8 => TdhOutType.TDH_OUTTYPE_UNSIGNEDBYTE,
    //                 TdhInType.TDH_INTYPE_ANSICHAR => TdhOutType.TDH_OUTTYPE_STRING,
    //             };
    //             consumed = 1;
    //             break;

    //         case TdhInType.TDH_INTYPE_INT16:
    //         case TdhInType.TDH_INTYPE_UINT16:
    //         case TdhInType.TDH_INTYPE_UNICODECHAR:
    //             consumed = 2;
    //             break;

    //         case TdhInType.TDH_INTYPE_INT32:
    //         case TdhInType.TDH_INTYPE_UINT32:
    //         case TdhInType.TDH_INTYPE_FLOAT:
    //         case TdhInType.TDH_INTYPE_BOOLEAN:
    //         case TdhInType.TDH_INTYPE_HEXINT32:
    //             consumed = 4;
    //             break;

    //         case TdhInType.TDH_INTYPE_INT64:
    //         case TdhInType.TDH_INTYPE_UINT64:
    //         case TdhInType.TDH_INTYPE_DOUBLE:
    //         case TdhInType.TDH_INTYPE_FILETIME:
    //         case TdhInType.TDH_INTYPE_HEXINT64:
    //             consumed = 8;
    //             break;

    //         case TdhInType.TDH_INTYPE_GUID:
    //         case TdhInType.TDH_INTYPE_SYSTEMTIME:
    //             consumed = 16;
    //             break;

    //         case TdhInType.TDH_INTYPE_BINARY:
    //             consumed = length;
    //             break;

    //         case TdhInType.TDH_INTYPE_POINTER:
    //         case TdhInType.TDH_INTYPE_SIZET:
    //             consumed = headerFlags switch
    //             {
    //                 HeaderFlags.EVENT_HEADER_FLAG_32_BIT_HEADER => 4,
    //                 HeaderFlags.EVENT_HEADER_FLAG_64_BIT_HEADER => 8,
    //                 _ => IntPtr.Size,
    //             };
    //             break;

    //         case TdhInType.TDH_INTYPE_MANIFEST_COUNTEDSTRING:
    //         case TdhInType.TDH_INTYPE_MANIFEST_COUNTEDANSISTRING:
    //         case TdhInType.TDH_INTYPE_MANIFEST_COUNTEDBINARY:
    //         case TdhInType.TDH_INTYPE_COUNTEDSTRING:
    //         case TdhInType.TDH_INTYPE_COUNTEDANSISTRING:
    //             consumed = BinaryPrimitives.ReadInt16LittleEndian(data);
    //             break;

    //         case TdhInType.TDH_INTYPE_REVERSEDCOUNTEDSTRING:
    //         case TdhInType.TDH_INTYPE_REVERSEDCOUNTEDANSISTRING:
    //             consumed = BinaryPrimitives.ReadInt16BigEndian(data);
    //             break;

    //         case TdhInType.TDH_INTYPE_HEXDUMP:
    //             consumed = BinaryPrimitives.ReadInt32LittleEndian(data);
    //             break;

    //         case TdhInType.TDH_INTYPE_UNICODESTRING:
    //         case TdhInType.TDH_INTYPE_NONNULLTERMINATEDSTRING:
    //             string uniValue = "";
    //             consumed = uniValue.Length * 2;  // Also add NULL char
    //             value = uniValue;
    //             break;


    //         case TdhInType.TDH_INTYPE_ANSISTRING:
    //         case TdhInType.TDH_INTYPE_NONNULLTERMINATEDANSISTRING:
    //             string ansiValue = "";
    //             consumed = ansiValue.Length;  // Also add NULL char
    //             value = ansiValue;
    //             break;

    //         case TdhInType.TDH_INTYPE_SID:
    //         case TdhInType.TDH_INTYPE_WBEMSID:
    //             SecurityIdentifier sid;
    //             unsafe
    //             {
    //                 fixed (byte* bytePtr = data)
    //                 {
    //                     sid = new((nint)bytePtr);
    //                 }
    //             }
    //             consumed = sid.BinaryLength;
    //             value = sid;
    //             break;

    //         default:
    //             throw new NotImplementedException(
    //                 $"Event Property '{name}' has TDH_INTYPE {inType} that is not implemented");
    //     }

    //     // If the in type actually parsed the bytes into an object we can
    //     // return that here.
    //     if (value != null)
    //     {
    //         return value;
    //     }

    //     return 0;
    // }
}

internal interface ITdhStringReader
{
    public string GetString(ReadOnlySpan<byte> data, int length, out int consumed);
}

internal class TdhStringReader : TdhTypeReader, ITdhStringReader
{
    internal static readonly Encoding ANSIEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);

    private Encoding _encoding;
    private int _charSize;

    public TdhStringReader(Encoding encoding)
    {
        _charSize = encoding == Encoding.Unicode ? 2 : 1;
        _encoding = encoding;
    }

    internal override TdhTypeTransformer GetDefaultTransformer() => new TdhStringTransformer();

    internal override ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length)
    {
        if (length > 0)
        {
            return data.Slice(0, length * _charSize);
        }

        Span<byte> nullTerminator = stackalloc byte[_charSize];
        for (int i = 0; i < _charSize; i++)
        {
            nullTerminator[i] = 0;
        }

        int nullIdx = data.IndexOfAny(nullTerminator);
        if (nullIdx == -1)
        {
            return data;
        }
        else
        {
            return data.Slice(0, nullIdx + _charSize);
        }
    }

    public string GetString(ReadOnlySpan<byte> data, int length, out int consumed)
    {
        ReadOnlySpan<byte> actualData = GetBytes(data, length);
        consumed = actualData.Length;

        // If no length was specified the data will include the null terminator
        // which we don't want in the final string.
        // FIXME: Check that end is the null char first.
        if (length == 0 && actualData.Length < data.Length)
        {
            actualData = actualData.Slice(0, actualData.Length - _charSize);
        }

        return SpanToString(actualData, _encoding);
    }

    internal static string SpanToString(ReadOnlySpan<byte> data, Encoding encoding)
    {
#if NET6_0_OR_GREATER
        return encoding.GetString(data);
#else
        // .NET Framework does not have the Span overload.
        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                return encoding.GetString(dataPtr, data.Length);
            }
        }
#endif
    }
}

internal class TdhIntegerReader<T> : TdhTypeReader, ITdhStringReader
    where T : TdhTypeTransformer, new()
{
    private int _size;

    internal TdhIntegerReader(int size)
    {
        _size = size;
    }

    internal override TdhTypeTransformer GetDefaultTransformer() => new T();

    internal override ReadOnlySpan<byte> GetBytes(ReadOnlySpan<byte> data, int length)
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

    public string GetString(ReadOnlySpan<byte> data, int length, out int consumed)
    {
        ReadOnlySpan<byte> actualData = GetBytes(data, length);
        consumed = actualData.Length;

        Encoding encoding;
        if (actualData.Length == 1)
        {
            encoding = TdhStringReader.ANSIEncoding;
        }
        else if (actualData.Length == 2)
        {
            encoding = Encoding.Unicode;
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot convert an integer value of size {actualData.Length} to string");
        }

        return TdhStringReader.SpanToString(actualData, encoding);
    }
}

internal abstract class TdhTypeTransformer
{
    public static object Transform(
        string propertyName,
        ReadOnlySpan<byte> data,
        short length,
        TdhTypeReader typeReader,
        TdhOutType outType,
        out int consumed,
        out short? storeInteger)
    {
        TdhTypeTransformer transformer;
        if (outType == TdhOutType.TDH_OUTTYPE_NULL)
        {
            transformer = typeReader.GetDefaultTransformer();
        }
        else
        {
            transformer = GetTransformer(propertyName, outType);
        }

        object value = transformer.GetValue(typeReader, data, length, out consumed);
        storeInteger = typeReader.StoreInteger;

        return value;
    }

    private static TdhTypeTransformer GetTransformer(
        string propertyName,
        TdhOutType outType
    ) => outType switch
    {
        TdhOutType.TDH_OUTTYPE_STRING => new TdhStringTransformer(),
        // TdhOutType.TDH_OUTTYPE_DATETIME => new TdhByteTransformer(),
        TdhOutType.TDH_OUTTYPE_BYTE => new TdhByteTransformer(),
        TdhOutType.TDH_OUTTYPE_UNSIGNEDBYTE => new TdhUnsignedByteTransformer(),
        TdhOutType.TDH_OUTTYPE_SHORT => new TdhShortTransformer(),
        TdhOutType.TDH_OUTTYPE_UNSIGNEDSHORT => new TdhUnsignedShortTransformer(),
        TdhOutType.TDH_OUTTYPE_INT => new TdhIntTransformer(),
        TdhOutType.TDH_OUTTYPE_UNSIGNEDINT => new TdhUnsignedIntTransformer(),
        TdhOutType.TDH_OUTTYPE_LONG => new TdhLongTransformer(),
        TdhOutType.TDH_OUTTYPE_UNSIGNEDLONG => new TdhUnsignedLongTransformer(),
        // TdhOutType.TDH_OUTTYPE_FLOAT => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_DOUBLE => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_BOOLEAN => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_GUID => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_HEXBINARY => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_HEXINT8 => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_HEXINT16 => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_HEXINT32 => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_HEXINT64 => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_PID => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_TID => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_PORT => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_IPV4 => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_IPV6 => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_SOCKETADDRESS => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_CIMDATETIME => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_ETWTIME => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_XML => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_ERRORCODE => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_WIN32ERROR => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_NTSTATUS => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_HRESULT => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_CULTURE_INSENSITIVE_DATETIME => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_JSON => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_UTF8 => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_PKCS7_WITH_TYPE_INFO => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_CODE_POINTER => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_DATETIME_UTC => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_REDUCEDSTRING => new TdhByteTransformer(),
        // TdhOutType.TDH_OUTTYPE_NOPRINT => new TdhByteTransformer(),
        _ => throw new NotImplementedException($"Property '{propertyName}' has unknown output type, cannot unpack"),
    };

    protected ReadOnlySpan<byte> GetBytes(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        ReadOnlySpan<byte> raw = reader.GetBytes(data, length);
        consumed = raw.Length;

        return raw;
    }

    protected abstract object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed);
}

internal class TdhStringTransformer : TdhTypeTransformer
{
    protected override object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        if (reader is ITdhStringReader stringReader)
        {
            return stringReader.GetString(data, length, out consumed);
        }

        throw new InvalidOperationException($"Cannot create a string with the in type {reader.GetType().Name}");
    }
}

internal class TdhByteTransformer : TdhTypeTransformer
{
    protected override object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        return GetBytes(reader, data, length, out consumed)[0];
    }
}

internal class TdhUnsignedByteTransformer : TdhTypeTransformer
{
    protected override object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        return unchecked((sbyte)GetBytes(reader, data, length, out consumed)[0]);
    }
}

internal class TdhShortTransformer : TdhTypeTransformer
{
    protected override object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(
            GetBytes(reader, data, length, out consumed));
    }
}

internal class TdhUnsignedShortTransformer : TdhTypeTransformer
{
    protected override object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(
            GetBytes(reader, data, length, out consumed));
    }
}

internal class TdhIntTransformer : TdhTypeTransformer
{
    protected override object GetValue(
         TdhTypeReader reader,
         ReadOnlySpan<byte> data,
         short length,
         out int consumed)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(
            GetBytes(reader, data, length, out consumed));
    }
}

internal class TdhUnsignedIntTransformer : TdhTypeTransformer
{
    protected override object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(
            GetBytes(reader, data, length, out consumed));
    }
}

internal class TdhLongTransformer : TdhTypeTransformer
{
    protected override object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(
            GetBytes(reader, data, length, out consumed));
    }
}

internal class TdhUnsignedLongTransformer : TdhTypeTransformer
{
    protected override object GetValue(
        TdhTypeReader reader,
        ReadOnlySpan<byte> data,
        short length,
        out int consumed)
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(
            GetBytes(reader, data, length, out consumed));
    }
}
