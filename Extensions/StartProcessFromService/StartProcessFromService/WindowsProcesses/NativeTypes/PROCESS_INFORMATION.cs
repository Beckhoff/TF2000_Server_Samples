// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace WindowsProcesses.NativeTypes
{
    /// <summary>
    ///     https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-process_information
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    internal struct PROCESS_INFORMATION
    {
#pragma warning disable IDE1006 // Naming Styles
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
#pragma warning restore IDE1006 // Naming Styles
    }
}
