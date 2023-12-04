using PSEtw.Shared.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSEtw.Shared;

internal static class ProviderHelper
{
    public static (Guid, string)[] GetProviders()
    {
        List<(Guid, string)> finalRes = new();

        int bufferSize = 0;
        nint buffer = IntPtr.Zero;
        try
        {
            while (true)
            {
                int res = Tdh.TdhEnumerateProviders(buffer, ref bufferSize);
                if (res == 0x0000007A)  // ERROR_INSUFFICIENT_BUFFER
                {
                    if (buffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer);
                        buffer = IntPtr.Zero;
                    }
                    buffer = Marshal.AllocHGlobal(bufferSize);
                    continue;
                }
                else if (res != 0)
                {
                    throw new Win32Exception(res);
                }

                unsafe
                {
                    Tdh.PROVIDER_ENUMERATION_INFO* enumInfo = (Tdh.PROVIDER_ENUMERATION_INFO*)buffer;
                    nint providerPtr = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.PROVIDER_ENUMERATION_INFO>());
                    Span<Tdh.TRACE_PROVIDER_INFO> providers = new((void*)providerPtr, enumInfo->NumberOfProviders);
                    foreach (Tdh.TRACE_PROVIDER_INFO info in providers)
                    {
                        nint stringPtr = IntPtr.Add(buffer, info.ProviderNameoffset);
                        string providerName = Marshal.PtrToStringUni(stringPtr) ?? "";
                        finalRes.Add((info.ProviderGuid, providerName));
                    }
                }

                break;
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return finalRes.ToArray();
    }

    public static string[] QueryAllTraces()
    {
        int traceCount = 64;
        int propLength = Marshal.SizeOf<Advapi32.EVENT_TRACE_PROPERTIES_V2>();

        List<string> sessionNames = new();
        while (true)
        {
            int bufferLength = (propLength + 4096) * traceCount;
            nint buffer = Marshal.AllocHGlobal(bufferLength);
            try
            {
                unsafe
                {
                    new Span<byte>((void*)buffer, bufferLength).Fill(0);

                    nint[] propArray = new nint[traceCount];
                    nint propBuffer = buffer;
                    for (int i = 0; i < traceCount; i++)
                    {
                        Advapi32.EVENT_TRACE_PROPERTIES_V2* prop = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)propBuffer;
                        prop->Wnode.BufferSize = propLength + 4096;
                        prop->LoggerNameOffset = propLength;
                        prop->LogFileNameOffset = propLength + 2048;
                        prop->V2Control = 2;
                        propArray[i] = propBuffer;
                        propBuffer = IntPtr.Add(propBuffer, propLength + 4096);
                    }

                    int res;
                    fixed (nint* propArrayPtr = propArray)
                    {
                        res = Advapi32.QueryAllTracesW(propArrayPtr, traceCount, out traceCount);
                    }
                    if (res == 0x0000007A)  // ERROR_INSUFFICIENT_BUFFER
                    {
                        continue;
                    }
                    else if (res != 0)
                    {
                        throw new Win32Exception(res);
                    }

                    propBuffer = buffer;
                    for (int i = 0; i < traceCount; i++)
                    {
                        Advapi32.EVENT_TRACE_PROPERTIES_V2* prop = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)propBuffer;
                        nint stringPtr = IntPtr.Add(propBuffer, prop->LoggerNameOffset);
                        sessionNames.Add(Marshal.PtrToStringUni(stringPtr) ?? "");
                        propBuffer = IntPtr.Add(propBuffer, propLength + 4096);
                    }
                }

                break;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return sessionNames.ToArray();
    }

    public static FieldInfo[] GetProviderFieldInfo(Guid provider, EventFieldType fieldType)
    {
        List<FieldInfo> finalRes = new();

        nint buffer = IntPtr.Zero;
        int bufferSize = 0;
        try
        {
            int res = Tdh.TdhEnumerateProviderFieldInformation(
                ref provider,
                fieldType,
                buffer,
                ref bufferSize);
            if (res == 0x0000007A) // ERROR_INSUFFICIENT_BUFFER
            {
                buffer = Marshal.AllocHGlobal(bufferSize);
                res = Tdh.TdhEnumerateProviderFieldInformation(
                    ref provider,
                    fieldType,
                    buffer,
                    ref bufferSize);
            }

            if (res != 0)
            {
                throw new Win32Exception(res);
            }

            unsafe
            {
                Tdh.PROVIDER_FIELD_INFOARRAY* infoArray = (Tdh.PROVIDER_FIELD_INFOARRAY*)buffer;
                nint arrayPtr = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.PROVIDER_FIELD_INFOARRAY>());
                Span<Tdh.PROVIDER_FIELD_INFO> fields = new((void*)arrayPtr, infoArray->NumberOfElements);
                foreach (Tdh.PROVIDER_FIELD_INFO info in fields)
                {
                    string name = ReadPtrString(buffer, info.NameOffset) ?? "";
                    string description = ReadPtrString(buffer, info.DescriptionOffset) ?? "";
                    finalRes.Add(new(name, description, info.Value));
                }
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return finalRes.ToArray();
    }

    private static string? ReadPtrString(nint buffer, int offset)
    {
        if (offset == 0)
        {
            return null;
        }

        return Marshal.PtrToStringUni(IntPtr.Add(buffer, offset));
    }
}

internal sealed class FieldInfo
{
    public string Name { get; }
    public string Description { get; }
    public long Value { get; }

    public FieldInfo(string name, string description, long value)
    {
        Name = name;
        Description = description;
        Value = value;
    }
}

internal sealed class SafeEtwTraceSession : SafeHandle
{
    private long _sessionHandle = 0;

    public SafeEtwTraceSession(long handle, nint buffer) : base(buffer, true)
    {
        _sessionHandle = handle;
    }

    public override bool IsInvalid => _sessionHandle != 0 || handle != IntPtr.Zero;

    internal long DangerousGetTraceHandle() => _sessionHandle;

    protected override bool ReleaseHandle()
    {
        int res = 0;
        if (handle != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(handle);
            handle = IntPtr.Zero;
        }
        return res == 0;
    }
}
