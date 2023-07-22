using System;
using System.Runtime.InteropServices;

namespace PSETW.Native;

internal partial class Tdh
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TRACE_EVENT_INFO
    {
        public Guid ProviderGuid;
        public Guid EventGuid;
        public Advapi32.EVENT_DESCRIPTOR EventDescriptor;
        public DecodingSource DecodingSource;
        public int ProviderNameOffset;
        public int LevelNameOffset;
        public int ChannelNameOffset;
        public int KeywordsNameOffset;
        public int TaskNameOffset;
        public int OpcodeNameOffset;
        public int EventMessageOffset;
        public int ProviderMessageOffset;
        public int BinaryXMLOffset;
        public int BinaryXMLSize;
        public int EventNameOffset;
        public int RelatedActivityIDNameOffset;
        public int PropertyCount;
        public int TopLevelPropertyCount;
        public int Flags;
        // public EVENT_PROPERTY_INFO[] EventPropertyInfoArray[ANYSIZE_ARRAY];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_PROPERTY_INFO
    {
        public EventPropertyFlags Flags;
        public int NameOffset;
        public short InType;
        public short OutType;
        public int MapNameOffset;
        public short Count;
        public short Length;
        public int Tags;
    }

    [DllImport("Tdh.dll")]
    public static extern int TdhGetEventInformation(
        ref Advapi32.EVENT_RECORD Event,
        int TdhContextCount,
        nint TdhContext,
        nint Buffer,
        ref int BufferSize);
}

public enum DecodingSource {
  DecodingSourceXMLFile,
  DecodingSourceWbem,
  DecodingSourceWPP,
  DecodingSourceTlg,
  DecodingSourceMax
}

[Flags]
public enum EventPropertyFlags
{
    None = 0x0,
    PropertyStruct = 0x1,
    PropertyParamLength = 0x2,
    PropertyParamCount = 0x4,
    PropertyWBEMXmlFragment = 0x8,
    PropertyParamFixedLength = 0x10,
    PropertyParamFixedCount = 0x20,
    PropertyHasTags = 0x40,
    PropertyHasCustomSchema = 0x80,
}

/*
InType provides basic information about the raw encoding of the data in the
field of an ETW event. An event field's InType tells the event decoder how to
determine the size of the field. In the case that a field's OutType is
NULL/unspecified/unrecognized, the InType also provides a default OutType for
the data (an OutType refines how the data should be interpreted). For example,
InType = INT32 indicates that the field's data is 4 bytes in length. If the
field's OutType is NULL/unspecified/unrecognized, the InType of INT32 also
provides the default OutType, TDH_OUTTYPE_INT, indicating that the field's data
should be interpreted as a Win32 INT value.

Note that there are multiple ways for the size of a field to be determined.

- Some InTypes have a fixed size. For example, InType UINT16 is always 2 bytes.
  For these fields, the length property of the EVENT_PROPERTY_INFO structure
  can be ignored by decoders.
- Some InTypes support deriving the size from the data content. For example,
  the size of a COUNTEDSTRING field is determined by reading the first 2 bytes
  of the data, which contain the size of the remaining string. For these
  fields, the length property of the EVENT_PROPERTY_INFO structure must be
  ignored.
- Some InTypes use the Flags and length properties of the EVENT_PROPERTY_INFO
  structure associated with the field. Details on how to do this are provided
  for each type.

For ETW InType values, the corresponding default OutType and the list of
applicable OutTypes can be found in winmeta.xml. For legacy WBEM InType values
(i.e. values not defined in winmeta.xml), the details for each InType are
included below.
*/
public enum InType
{
    TDH_INTYPE_NULL, /* Invalid InType value. */
    TDH_INTYPE_UNICODESTRING, /*
        Field size depends on the Flags and length fields of the corresponding
        EVENT_PROPERTY_INFO structure (epi) as follows:
        - If ((epi.Flags & PropertyParamLength) != 0), the
          epi.lengthPropertyIndex field contains the index of the property that
          contains the number of WCHARs in the string.
        - Else if ((epi.Flags & PropertyLength) != 0 || epi.length != 0), the
          epi.length field contains number of WCHARs in the string.
        - Else the string is nul-terminated (terminated by (WCHAR)0).
        Note that some event providers do not correctly nul-terminate the last
        string field in the event. While this is technically invalid, event
        decoders may silently tolerate such behavior instead of rejecting the
        event as invalid. */
    TDH_INTYPE_ANSISTRING, /*
        Field size depends on the Flags and length fields of the corresponding
        EVENT_PROPERTY_INFO structure (epi) as follows:
        - If ((epi.Flags & PropertyParamLength) != 0), the
          epi.lengthPropertyIndex field contains the index of the property that
          contains the number of BYTEs in the string.
        - Else if ((epi.Flags & PropertyLength) != 0 || epi.length != 0), the
          epi.length field contains number of BYTEs in the string.
        - Else the string is nul-terminated (terminated by (CHAR)0).
        Note that some event providers do not correctly nul-terminate the last
        string field in the event. While this is technically invalid, event
        decoders may silently tolerate such behavior instead of rejecting the
        event as invalid. */
    TDH_INTYPE_INT8,    /* Field size is 1 byte. */
    TDH_INTYPE_UINT8,   /* Field size is 1 byte. */
    TDH_INTYPE_INT16,   /* Field size is 2 bytes. */
    TDH_INTYPE_UINT16,  /* Field size is 2 bytes. */
    TDH_INTYPE_INT32,   /* Field size is 4 bytes. */
    TDH_INTYPE_UINT32,  /* Field size is 4 bytes. */
    TDH_INTYPE_INT64,   /* Field size is 8 bytes. */
    TDH_INTYPE_UINT64,  /* Field size is 8 bytes. */
    TDH_INTYPE_FLOAT,   /* Field size is 4 bytes. */
    TDH_INTYPE_DOUBLE,  /* Field size is 8 bytes. */
    TDH_INTYPE_BOOLEAN, /* Field size is 4 bytes. */
    TDH_INTYPE_BINARY, /*
        Field size depends on the OutType, Flags, and length fields of the
        corresponding EVENT_PROPERTY_INFO structure (epi) as follows:
        - If ((epi.Flags & PropertyParamLength) != 0), the
          epi.lengthPropertyIndex field contains the index of the property that
          contains the number of BYTEs in the field.
        - Else if ((epi.Flags & PropertyLength) != 0 || epi.length != 0), the
          epi.length field contains number of BYTEs in the field.
        - Else if (epi.OutType == IPV6), the field size is 16 bytes.
        - Else the field is incorrectly encoded. */
    TDH_INTYPE_GUID, /* Field size is 16 bytes. */
    TDH_INTYPE_POINTER, /*
        Field size depends on the eventRecord.EventHeader.Flags value. If the
        EVENT_HEADER_FLAG_32_BIT_HEADER flag is set, the field size is 4 bytes.
        If the EVENT_HEADER_FLAG_64_BIT_HEADER flag is set, the field size is 8
        bytes. Default OutType is HEXINT64. Other usable OutTypes include
        CODE_POINTER, LONG, UNSIGNEDLONG.
        */
    TDH_INTYPE_FILETIME,   /* Field size is 8 bytes. */
    TDH_INTYPE_SYSTEMTIME, /* Field size is 16 bytes. */
    TDH_INTYPE_SID, /*
        Field size is determined by reading the first few bytes of the field
        value to determine the number of relative IDs. */
    TDH_INTYPE_HEXINT32, /* Field size is 4 bytes. */
    TDH_INTYPE_HEXINT64, /* Field size is 8 bytes. */
    TDH_INTYPE_MANIFEST_COUNTEDSTRING, /*
        Supported in Windows 2018 Fall Update or later. This is the same as
        TDH_INTYPE_COUNTEDSTRING, but can be used in manifests.
        Field contains a little-endian 16-bit bytecount followed by a WCHAR
        (16-bit character) string. Default OutType is STRING. Other usable
        OutTypes include XML, JSON. Field size is determined by reading the
        first two bytes of the payload, which are then interpreted as a
        little-endian 16-bit integer which gives the number of additional bytes
        (not characters) in the field. */
    TDH_INTYPE_MANIFEST_COUNTEDANSISTRING, /*
        Supported in Windows 2018 Fall Update or later. This is the same as
        TDH_INTYPE_COUNTEDANSISTRING, but can be used in manifests.
        Field contains a little-endian 16-bit bytecount followed by a CHAR
        (8-bit character) string. Default OutType is STRING. Other usable
        OutTypes include XML, JSON, UTF8. Field size is determined by reading
        the first two bytes of the payload, which are then interpreted as a
        little-endian 16-bit integer which gives the number of additional bytes
        (not characters) in the field. */
    TDH_INTYPE_RESERVED24,
    TDH_INTYPE_MANIFEST_COUNTEDBINARY, /*
        Supported in Windows 2018 Fall Update or later.
        Field contains a little-endian 16-bit bytecount followed by binary
        data. Default OutType is HEXBINARY. Other usable
        OutTypes include IPV6, SOCKETADDRESS, PKCS7_WITH_TYPE_INFO. Field size
        is determined by reading the first two bytes of the payload, which are
        then interpreted as a little-endian 16-bit integer which gives the
        number of additional bytes in the field. */

    // End of winmeta intypes.
    // Start of TDH intypes for WBEM. These types cannot be used in manifests.

    TDH_INTYPE_COUNTEDSTRING = 300, /*
        Field contains a little-endian 16-bit bytecount followed by a WCHAR
        (16-bit character) string. Default OutType is STRING. Other usable
        OutTypes include XML, JSON. Field size is determined by reading the
        first two bytes of the payload, which are then interpreted as a
        little-endian 16-bit integer which gives the number of additional bytes
        (not characters) in the field. */
    TDH_INTYPE_COUNTEDANSISTRING, /*
        Field contains a little-endian 16-bit bytecount followed by a CHAR
        (8-bit character) string. Default OutType is STRING. Other usable
        OutTypes include XML, JSON, UTF8. Field size is determined by reading
        the first two bytes of the payload, which are then interpreted as a
        little-endian 16-bit integer which gives the number of additional bytes
        (not characters) in the field. */
    TDH_INTYPE_REVERSEDCOUNTEDSTRING, /*
        Deprecated. Prefer TDH_INTYPE_COUNTEDSTRING.
        Field contains a big-endian 16-bit bytecount followed by a WCHAR
        (16-bit little-endian character) string. Default OutType is STRING.
        Other usable OutTypes include XML, JSON. Field size is determined by
        reading the first two bytes of the payload, which are then interpreted
        as a big-endian 16-bit integer which gives the number of additional
        bytes (not characters) in the field. */
    TDH_INTYPE_REVERSEDCOUNTEDANSISTRING, /*
        Deprecated. Prefer TDH_INTYPE_COUNTEDANSISTRING.
        Field contains a big-endian 16-bit bytecount followed by a CHAR (8-bit
        character) string. Default OutType is STRING. Other usable OutTypes
        include XML, JSON, UTF8. Field size is determined by reading the first
        two bytes of the payload, which are then interpreted as a big-endian
        16-bit integer which gives the number of additional bytes in the
        field. */
    TDH_INTYPE_NONNULLTERMINATEDSTRING, /*
        Deprecated. Prefer TDH_INTYPE_COUNTEDSTRING.
        Field contains a WCHAR (16-bit character) string. Default OutType is
        STRING. Other usable OutTypes include XML, JSON. Field size is the
        remaining bytes of data in the event. */
    TDH_INTYPE_NONNULLTERMINATEDANSISTRING, /*
        Deprecated. Prefer TDH_INTYPE_COUNTEDANSISTRING.
        Field contains a CHAR (8-bit character) string. Default OutType is
        STRING. Other usable OutTypes include XML, JSON, UTF8. Field size is
        the remaining bytes of data in the event. */
    TDH_INTYPE_UNICODECHAR, /*
        Deprecated. Prefer TDH_INTYPE_UINT16 with TDH_OUTTYPE_STRING.
        Field contains a WCHAR (16-bit character) value. Default OutType is
        STRING. Field size is 2 bytes. */
    TDH_INTYPE_ANSICHAR, /*
        Deprecated. Prefer TDH_INTYPE_UINT8 with TDH_OUTTYPE_STRING.
        Field contains a CHAR (8-bit character) value. Default OutType is
        STRING. Field size is 1 byte. */
    TDH_INTYPE_SIZET, /*
        Deprecated. Prefer TDH_INTYPE_POINTER with TDH_OUTTYPE_UNSIGNEDLONG.
        Field contains a SIZE_T (UINT_PTR) value. Default OutType is HEXINT64.
        Field size depends on the eventRecord.EventHeader.Flags value. If the
        EVENT_HEADER_FLAG_32_BIT_HEADER flag is set, the field size is 4 bytes.
        If the EVENT_HEADER_FLAG_64_BIT_HEADER flag is set, the field size is
        8 bytes. */
    TDH_INTYPE_HEXDUMP, /*
        Deprecated. Prefer TDH_INTYPE_BINARY.
        Field contains binary data. Default OutType is HEXBINARY. Field size is
        determined by reading the first four bytes of the payload, which are
        then interpreted as a little-endian UINT32 which gives the number of
        additional bytes in the field. */
    TDH_INTYPE_WBEMSID /*
        Deprecated. Prefer TDH_INTYPE_SID.
        Field contains an SE_TOKEN_USER (security identifier) value. Default
        OutType is STRING (i.e. the SID will be converted to a string during
        decoding using ConvertSidToStringSid or equivalent). Field size is
        determined by reading the first few bytes of the field value to
        determine the number of relative IDs. Because the SE_TOKEN_USER
        structure includes pointers, decoding this structure requires accurate
        knowledge of the event provider's pointer size (i.e. from
        eventRecord.EventHeader.Flags). */
}


/*
OutType describes how to interpret a field's data. If a field's OutType is
not specified in the manifest, it defaults to TDH_OUTTYPE_NULL. If the field's
OutType is NULL, decoding should use the default OutType associated with the
field's InType.

Not all combinations of InType and OutType are valid, and event decoding tools
will only recognize a small set of InType+OutType combinations. If an
InType+OutType combination is not recognized by a decoder, the decoder should
use the default OutType associated with the field's InType (i.e. the decoder
should behave as if the OutType were NULL).
*/
enum _TDH_OUT_TYPE {
    TDH_OUTTYPE_NULL, /*
        Default OutType value. If a field's OutType is set to this value, the
        decoder should determine the default OutType corresponding to the
        field's InType and use that OutType when decoding the field. */
    TDH_OUTTYPE_STRING, /*
        Implied by the STRING, CHAR, and SID InType values. Applicable to the
        INT8, UINT8, UINT16 InType values. Specifies that the field should be
        decoded as text. Decoding depends on the InType. For INT8, UINT8, and
        ANSISTRING InTypes, the data is decoded using the ANSI code page of the
        event provider. For UINT16 and UNICODESTRING InTypes, the data is
        decoded as UTF-16LE. For SID InTypes, the data is decoded using
        ConvertSidToStringSid or equivalent. */
    TDH_OUTTYPE_DATETIME, /*
        Implied by the FILETIME and SYSTEMTIME InType values. Data is decoded
        as a date/time. FILETIME is decoded as a 64-bit integer representing
        the number of 100-nanosecond intervals since January 1, 1601.
        SYSTEMTIME is decoded as the Win32 SYSTEMTIME structure. In both cases,
        the time zone must be determined using other methods. (FILETIME is
        usually but not always UTC.) */
    TDH_OUTTYPE_BYTE, /*
        Implied by the INT8 InType value. Data is decoded as a signed integer. */
    TDH_OUTTYPE_UNSIGNEDBYTE, /*
        Implied by the UINT8 InType value. Data is decoded as an unsigned
        integer. */
    TDH_OUTTYPE_SHORT, /*
        Implied by the INT16 InType value. Data is decoded as a signed
        little-endian integer. */
    TDH_OUTTYPE_UNSIGNEDSHORT, /*
        Implied by the UINT16 InType value. Data is decoded as an unsigned
        little-endian integer. */
    TDH_OUTTYPE_INT, /*
        Implied by the INT32 InType value. Data is decoded as a signed
        little-endian integer. */
    TDH_OUTTYPE_UNSIGNEDINT, /*
        Implied by the UINT32 InType value. Data is decoded as an unsigned
        little-endian integer. */
    TDH_OUTTYPE_LONG, /*
        Implied by the INT64 InType value. Applicable to the INT32 InType value
        (i.e. to distinguish between the C data types "long int" and "int").
        Data is decoded as a signed little-endian integer. */
    TDH_OUTTYPE_UNSIGNEDLONG, /*
        Implied by the UINT64 InType value. Applicable to the UINT32 InType
        value (i.e. to distinguish between the C data types "long int" and
        "int"). Data is decoded as an unsigned little-endian integer. */
    TDH_OUTTYPE_FLOAT, /*
        Implied by the FLOAT InType value. Data is decoded as a
        single-precision floating-point number. */
    TDH_OUTTYPE_DOUBLE, /*
        Implied by the DOUBLE InType value. Data is decoded as a
        double-precision floating-point number. */
    TDH_OUTTYPE_BOOLEAN, /*
        Implied by the BOOL InType value. Applicable to the UINT8 InType value.
        Data is decoded as a Boolean (false if zero, true if non-zero). */
    TDH_OUTTYPE_GUID, /*
        Implied by the GUID InType value. Data is decoded as a GUID. */
    TDH_OUTTYPE_HEXBINARY, /*
        Not commonly used. Implied by the BINARY and HEXDUMP InType values. */
    TDH_OUTTYPE_HEXINT8, /*
        Specifies that the field should be formatted as a hexadecimal integer.
        Applicable to the UINT8 InType value. */
    TDH_OUTTYPE_HEXINT16, /*
        Specifies that the field should be formatted as a hexadecimal integer.
        Applicable to the UINT16 InType value. */
    TDH_OUTTYPE_HEXINT32, /*
        Not commonly used. Implied by the HEXINT32 InType value. Applicable to
        the UINT32 InType value. */
    TDH_OUTTYPE_HEXINT64, /*
        Not commonly used. Implied by the HEXINT64 InType value. Applicable to
        the UINT64 InType value. */
    TDH_OUTTYPE_PID, /*
        Specifies that the field is a process identifier. Applicable to the
        UINT32 InType value. */
    TDH_OUTTYPE_TID, /*
        Specifies that the field is a thread identifier. Applicable to the
        UINT32 InType value. */
    TDH_OUTTYPE_PORT, /*
        Specifies that the field is an Internet Protocol port number, specified
        in network byte order (big-endian). Applicable to the UINT16 InType
        value. */
    TDH_OUTTYPE_IPV4, /*
        Specifies that the field is an Internet Protocol V4 address. Applicable
        to the UINT32 InType value. */
    TDH_OUTTYPE_IPV6, /*
        Specifies that the field is an Internet Protocol V6 address. Applicable
        to the BINARY InType value. If the length of a field is unspecified in
        the EVENT_PROPERTY_INFO but the field's InType is BINARY and its
        OutType is IPV6, the field's length should be assumed to be 16 bytes. */
    TDH_OUTTYPE_SOCKETADDRESS, /*
        Specifies that the field is a SOCKADDR structure. Applicable to the
        BINARY InType value. Note that different address types have different
        sizes. */
    TDH_OUTTYPE_CIMDATETIME, /*
        Not commonly used. */
    TDH_OUTTYPE_ETWTIME, /*
        Not commonly used. Applicable to the UINT32 InType value. */
    TDH_OUTTYPE_XML, /*
        Specifies that the field should be treated as XML text. Applicable to
        the *STRING InType values. When this OutType is used, decoders should
        use standard XML decoding rules (i.e. assume a Unicode encoding unless
        the document specifies a different encoding in its encoding
        attribute). */
    TDH_OUTTYPE_ERRORCODE, /*
        Not commonly used. Specifies that the field is an error code of
        some type. Applicable to the UINT32 InType value. */
    TDH_OUTTYPE_WIN32ERROR, /*
        Specifies that the field is a Win32 error code. Applicable to the
        UINT32 and HEXINT32 InType values. */
    TDH_OUTTYPE_NTSTATUS, /*
        Specifies that the field is an NTSTATUS code. Applicable to the UINT32
        and HEXINT32 InType values. */
    TDH_OUTTYPE_HRESULT, /*
        Specifies that the field is an HRESULT error code. Applicable to the
        INT32 InType value. */
    TDH_OUTTYPE_CULTURE_INSENSITIVE_DATETIME, /*
        Specifies that a date/time value should be formatted in a
        locale-invariant format. Applicable to the FILETIME and SYSTEMTIME
        InType values. */
    TDH_OUTTYPE_JSON, /*
        Specifies that the field should be treated as JSON text. Applicable to
        the *STRING InType values. When this OutType is used with the ANSI
        string InType values, decoders should decode the data as UTF-8. */
    TDH_OUTTYPE_UTF8, /*
        Specifies that the field should be treated as UTF-8 text. Applicable to
        the *ANSISTRING InType values. */
    TDH_OUTTYPE_PKCS7_WITH_TYPE_INFO, /*
        Specifies that the field should be treated as a PKCS#7 message (e.g.
        encrypted and/or signed). Applicable to the BINARY InType value. One
        or more bytes of TraceLogging-compatible type information (providing
        the type of the inner content) may optionally be appended immediately
        after the PKCS#7 message. For example, the byte 0x01
        (TlgInUNICODESTRING = 0x01) might be appended to indicate that the
        inner content is to be interpreted as InType = UNICODESTRING; the bytes
        0x82 0x22 (TlgInANSISTRING + TlgInChain = 0x82, TlgOutJSON = 0x22)
        might be appended to indicate that the inner content is to be
        interpreted as InType = ANSISTRING, OutType = JSON. */
    TDH_OUTTYPE_CODE_POINTER, /*
        Specifies that the field should be treated as an address that can
        potentially be decoded into a symbol name. Applicable to InTypes
        UInt32, UInt64, HexInt32, HexInt64, and Pointer. */
    TDH_OUTTYPE_DATETIME_UTC, /*
        Usable with the FILETIME and SYSTEMTIME InType values. Data is decoded
        as a date/time. FILETIME is decoded as a 64-bit integer representing
        the number of 100-nanosecond intervals since January 1, 1601.
        SYSTEMTIME is decoded as the Win32 SYSTEMTIME structure. In both cases,
        the time zone is assumed to be UTC.) */

    // End of winmeta outtypes.
    // Start of TDH outtypes for WBEM.

    TDH_OUTTYPE_REDUCEDSTRING = 300, /*
        Not commonly used. */
    TDH_OUTTYPE_NOPRINT /*
        Not commonly used. Specifies that the field should not be shown in the
        output of the decoding tool. This might be applied to a Count or a
        Length field. Applicable to all InType values. Most decoders ignore
        this value. */
}
