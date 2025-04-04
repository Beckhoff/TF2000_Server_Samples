// ReSharper disable InconsistentNaming

using System;
using System.Runtime.InteropServices;
using WindowsProcesses.NativeTypes;

// ReSharper disable once CheckNamespace
namespace WindowsProcesses
{
    internal static class NativeMethods
    {
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/userenv/nf-userenv-createenvironmentblock
        /// </summary>
        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessa
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine,
            IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessasusera
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine,
            IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/userenv/nf-userenv-destroyenvironmentblock
        /// </summary>
        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/wtsapi32/nf-wtsapi32-wtsenumeratesessionsa
        /// </summary>
        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version,
            out IntPtr ppSessionInfo, out int pCount);

        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/wtsapi32/nf-wtsapi32-wtsfreememory
        /// </summary>
        [DllImport("wtsapi32.dll", SetLastError = false)]
        public static extern void WTSFreeMemory(IntPtr memory);

        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-wtsgetactiveconsolesessionid
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WTSGetActiveConsoleSessionId();

        /// <summary>
        ///     https://docs.microsoft.com/en-us/windows/win32/api/wtsapi32/nf-wtsapi32-wtsqueryusertoken
        /// </summary>
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);
#pragma warning restore IDE1006 // Naming Styles
    }
}
