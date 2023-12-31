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

        int bufferSize = 1024;
        nint buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            while (true)
            {
                int res = Tdh.TdhEnumerateProviders(buffer, ref bufferSize);
                if (res == Win32Error.ERROR_INSUFFICIENT_BUFFER)
                {
                    buffer = Marshal.ReAllocHGlobal(buffer, (nint)bufferSize);
                    continue;
                }
                Win32Error.ThrowIfError(res);

                unsafe
                {
                    Tdh.PROVIDER_ENUMERATION_INFO* enumInfo = (Tdh.PROVIDER_ENUMERATION_INFO*)buffer;
                    nint providerPtr = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.PROVIDER_ENUMERATION_INFO>());
                    Span<Tdh.TRACE_PROVIDER_INFO> providers = new((void*)providerPtr, enumInfo->NumberOfProviders);
                    foreach (Tdh.TRACE_PROVIDER_INFO info in providers)
                    {
                        nint stringPtr = IntPtr.Add(buffer, info.ProviderNameoffset);
                        string providerName = Marshal.PtrToStringUni(stringPtr) ?? string.Empty;
                        finalRes.Add((info.ProviderGuid, providerName));
                    }
                }

                break;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
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

                    if (res == Win32Error.ERROR_INSUFFICIENT_BUFFER)
                    {
                        continue;
                    }
                    Win32Error.ThrowIfError(res);

                    propBuffer = buffer;
                    for (int i = 0; i < traceCount; i++)
                    {
                        Advapi32.EVENT_TRACE_PROPERTIES_V2* prop = (Advapi32.EVENT_TRACE_PROPERTIES_V2*)propBuffer;
                        nint stringPtr = IntPtr.Add(propBuffer, prop->LoggerNameOffset);
                        sessionNames.Add(Marshal.PtrToStringUni(stringPtr) ?? string.Empty);
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

    public static ProviderFieldInfo[] GetProviderFieldInfo(Guid provider, EventFieldType fieldType)
    {
        List<ProviderFieldInfo> finalRes = new();

        nint buffer = IntPtr.Zero;
        int bufferSize = 0;
        try
        {
            int res = Tdh.TdhEnumerateProviderFieldInformation(
                ref provider,
                fieldType,
                buffer,
                ref bufferSize);
            if (res == Win32Error.ERROR_INSUFFICIENT_BUFFER)
            {
                buffer = Marshal.AllocHGlobal(bufferSize);
                res = Tdh.TdhEnumerateProviderFieldInformation(
                    ref provider,
                    fieldType,
                    buffer,
                    ref bufferSize);
            }
            else if (res == Win32Error.ERROR_NOT_FOUND)
            {
                // TraceLoggers won't have any fields defined, just present
                // there are none.
                return Array.Empty<ProviderFieldInfo>();
            }
            Win32Error.ThrowIfError(res);

            unsafe
            {
                Tdh.PROVIDER_FIELD_INFOARRAY* infoArray = (Tdh.PROVIDER_FIELD_INFOARRAY*)buffer;
                nint arrayPtr = IntPtr.Add(buffer, Marshal.SizeOf<Tdh.PROVIDER_FIELD_INFOARRAY>());
                Span<Tdh.PROVIDER_FIELD_INFO> fields = new((void*)arrayPtr, infoArray->NumberOfElements);
                foreach (Tdh.PROVIDER_FIELD_INFO info in fields)
                {
                    string name = ReadPtrString(buffer, info.NameOffset) ?? string.Empty;
                    string description = ReadPtrString(buffer, info.DescriptionOffset) ?? string.Empty;
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

internal sealed class ProviderFieldInfo
{
    public string Name { get; }
    public string Description { get; }
    public long Value { get; }

    public ProviderFieldInfo(string name, string description, long value)
    {
        Name = name;
        Description = description;
        Value = value;
    }
}
