namespace WindowsProcesses.NativeTypes
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/windows/win32/api/wtsapi32/ne-wtsapi32-wts_connectstate_class
    /// </summary>
    internal enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive = 0,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }
}
