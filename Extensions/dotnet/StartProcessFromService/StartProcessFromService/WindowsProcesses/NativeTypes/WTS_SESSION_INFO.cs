// ReSharper disable FieldCanBeMadeReadOnly.Global

using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace WindowsProcesses.NativeTypes
{
    /// <summary>
    ///     https://docs.microsoft.com/en-us/windows/win32/api/wtsapi32/ns-wtsapi32-wts_session_infoa
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    internal struct WTS_SESSION_INFO
    {
#pragma warning disable IDE1006 // Naming Styles
        public uint SessionID;
        public string pWinStationName;
        public WTS_CONNECTSTATE_CLASS State;
#pragma warning restore IDE1006 // Naming Styles
    }
}
