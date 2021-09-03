using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsProcesses.NativeTypes;

namespace WindowsProcesses
{
    /// <summary>
    /// Represents a new process that runs in the security context of a specified <see cref="User"/>.
    /// </summary>
    public sealed class UserProcess : IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCESS_INFORMATION _info;

        /// <summary>
        /// Gets a <see cref="System.Diagnostics.Process"/> that represents the created process.
        /// </summary>
        public Process Process
        {
            get
            {
                return Process.GetProcessById(Convert.ToInt32(_info.dwProcessId));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProcess"/> class with the specified arguments.
        /// </summary>
        /// <param name="user">A <see cref="User"/> that represents the logged-on user in whose security context the new process runs. If this parameter is null, the new process runs in the security context of the calling process. The default value is null.</param>
        /// <param name="userEnvironment">The <see cref="UserEnvironment"/> of the new process. If this parameter is null, the new process uses the environment of the calling process. The default value is null.</param>
        /// <param name="applicationName">The name of the module to be executed. If this parameter is null, the module name must be the first white space–delimited token in <paramref name="commandLine"/>. The default value is null.</param>
        /// <param name="commandLine">The command line to be executed. If this parameter is null, the constructor uses <paramref name="applicationName"/> as the command line. The default value is null.</param>
        /// <param name="currentDirectory">The full path to the current directory for the process. The <see cref="string"/> can also specify a UNC path. If this parameter is null, the new process will have the same current drive and directory as the calling process. The default value is null.</param>
        /// <param name="showWindow">true to show a window for the created process; otherwise, false to run the process without a window. The default value is false.</param>
        public UserProcess(User user = null, UserEnvironment userEnvironment = null, string applicationName = null, string commandLine = null, string currentDirectory = null, bool showWindow = false)
        {
            var creationFlags = (uint)((showWindow ? PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE : PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW) | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT);
            var environment = (userEnvironment is null) ?
                IntPtr.Zero :
                userEnvironment.Token;
            var startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.lpDesktop = "winsta0\\default";
            startupInfo.dwFlags = (uint)START_FLAGS.STARTF_USESHOWWINDOW;
            startupInfo.wShowWindow = (short)(showWindow ? SHOW_WINDOW.SW_SHOW : SHOW_WINDOW.SW_HIDE);

            var succeeded = (user is null) ?
                NativeMethods.CreateProcess(applicationName, commandLine, IntPtr.Zero, IntPtr.Zero, false, creationFlags, environment, currentDirectory, ref startupInfo, out _info) :
                NativeMethods.CreateProcessAsUser(user.Token, applicationName, commandLine, IntPtr.Zero, IntPtr.Zero, false, creationFlags, environment, currentDirectory, ref startupInfo, out _info);

            if (!succeeded)
                throw new Win32Exception();
        }

        /// <summary>
        /// Creates a new process that runs in the security context of a specified <see cref="User"/>.
        /// </summary>
        /// <param name="user">A <see cref="User"/> that represents the logged-on user in whose security context the new process runs. If this parameter is null, the new process runs in the security context of the calling process. The default value is null.</param>
        /// <param name="userEnvironment">The <see cref="UserEnvironment"/> of the new process. If this parameter is null, the new process uses the environment of the calling process. The default value is null.</param>
        /// <param name="applicationName">The name of the module to be executed. If this parameter is null, the module name must be the first white space–delimited token in <paramref name="commandLine"/>. The default value is null.</param>
        /// <param name="commandLine">The command line to be executed. If this parameter is null, the constructor uses <paramref name="applicationName"/> as the command line. The default value is null.</param>
        /// <param name="currentDirectory">The full path to the current directory for the process. The <see cref="string"/> can also specify a UNC path. If this parameter is null, the new process will have the same current drive and directory as the calling process. The default value is null.</param>
        /// <param name="showWindow">true to show a window for the created process; otherwise, false to run the process without a window. The default value is false.</param>
        /// <returns>A <see cref="System.Diagnostics.Process"/> that represents the created process.</returns>
        public static Process Create(User user = null, UserEnvironment userEnvironment = null, string applicationName = null, string commandLine = null, string currentDirectory = null, bool showWindow = false)
        {
            using var userProcess = new UserProcess(user, userEnvironment, applicationName, commandLine, currentDirectory, showWindow);
            return userProcess.Process;
        }

        /// <summary>
        /// Creates a new process. If the current process is running in user interactive mode, the new process runs in the security context of the calling process. If the current process is not running in user interactive mode, for example, when running as a service, the new process runs in the security context of the user whose console session is currently attached to the physical console, if any.
        /// </summary>
        /// <param name="applicationName">The name of the module to be executed. If this parameter is null, the module name must be the first white space–delimited token in <paramref name="commandLine"/>. The default value is null.</param>
        /// <param name="commandLine">The command line to be executed. If this parameter is null, the constructor uses <paramref name="applicationName"/> as the command line. The default value is null.</param>
        /// <param name="currentDirectory">The full path to the current directory for the process. The <see cref="string"/> can also specify a UNC path. If this parameter is null, the new process will have the same current drive and directory as the calling process. The default value is null.</param>
        /// <param name="showWindow">true to show a window for the created process; otherwise, false to run the process without a window. The default value is false.</param>
        /// <returns>A <see cref="System.Diagnostics.Process"/> that represents the created process.</returns>
        public static Process Create(string applicationName = null, string commandLine = null, string currentDirectory = null, bool showWindow = false)
        {
            // Check if running in user interactive mode to create the new process in the security context of the calling process.
            // Services normally do not run in user interactive mode, but in the context of the LocalSystem (SYSTEM) account.
            if (Environment.UserInteractive)
                return Create(null, null, applicationName, commandLine, currentDirectory, showWindow);

            // The following statements will fail if not running in the context of the LocalSystem account
            using var user = new User();
            using var userEnvironment = new UserEnvironment(user);
            return Create(user, userEnvironment, applicationName, commandLine, currentDirectory, showWindow);
        }

        #region IDisposable Support

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object _isDisposedLock = new object();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _isDisposed;

        public void Dispose()
        {
            lock (_isDisposedLock)
            {
                if (!_isDisposed)
                {
                    if (!(NativeMethods.CloseHandle(_info.hProcess) && NativeMethods.CloseHandle(_info.hThread)))
                        throw new Win32Exception();

                    GC.SuppressFinalize(this);
                    _isDisposed = true;
                }
            }
        }

        ~UserProcess()
        {
            Dispose();
        }

        #endregion
    }
}
