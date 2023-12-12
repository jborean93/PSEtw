using System.Runtime.InteropServices;

namespace PSEtw.Shared.Native;

internal static partial class Advapi32
{
    [DllImport("Advapi32.dll")]
    public static extern int EnumerateTraceGuidsEx(
        int TraceQueryInfoClass,
        nint InBuffer,
        int InBufferSize,
        nint OutBuffer,
        int OutBufferSize,
        out int ReturnLength);
}

internal enum TRACE_INFO_CLASS
{
    TraceGuidQueryList = 0,
    TraceGuidQueryInfo = 1,
    TraceGuidQueryProcess = 2,
    TraceStackTracingInfo = 3,
    TraceSystemTraceEnableFlagsInfo = 4,
    TraceSampledProfileIntervalInfo = 5,
    TraceProfileSourceConfigInfo = 6,
    TraceProfileSourceListInfo = 7,
    TracePmcEventListInfo = 8,
    TracePmcCounterListInfo = 9,
    TraceSetDisallowList = 10,
    TraceVersionInfo = 11,
    TraceGroupQueryList = 12,
    TraceGroupQueryInfo = 13,
    TraceDisallowListQuery = 14,
    TraceInfoReserved15,
    TracePeriodicCaptureStateListInfo = 16,
    TracePeriodicCaptureStateInfo = 17,
    TraceProviderBinaryTracking = 18,
    TraceMaxLoggersQuery = 19,
    TraceLbrConfigurationInfo = 20,
    TraceLbrEventListInfo = 21,
    TraceMaxPmcCounterQuery = 22,
    TraceStreamCount = 23,
    TraceStackCachingInfo = 24,
    TracePmcCounterOwners = 25,
    TraceUnifiedStackCachingInfo = 26,
    TracePmcSessionInformation = 27,
    MaxTraceSetInfoClass = 28
}
