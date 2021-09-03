using System.Runtime.InteropServices;

namespace WindowsProcesses.NativeTypes
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/windows/win32/api/wtsapi32/ns-wtsapi32-wts_session_infoa
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WTS_SESSION_INFO
    {
        public uint SessionID;
        public string pWinStationName;
        public WTS_CONNECTSTATE_CLASS State;
    }
}
