namespace WindowsProcesses.NativeTypes
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
    /// </summary>
    internal enum SHOW_WINDOW : short
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL,
        SW_SHOWMINIMIZED,
        SW_SHOWMAXIMIZED,
        SW_SHOWNOACTIVATE,
        SW_SHOW,
        SW_MINIMIZE,
        SW_SHOWMINNOACTIVE,
        SW_SHOWNA,
        SW_RESTORE,
        SW_SHOWDEFAULT,
        SW_FORCEMINIMIZE,
    }
}
