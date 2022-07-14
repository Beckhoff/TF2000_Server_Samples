using System;
using System.ComponentModel;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace WindowsProcesses
{
    /// <summary>
    ///     Represents the environment of a specified <see cref="User" />.
    /// </summary>
    public sealed class UserEnvironment : IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IntPtr _token;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserEnvironment" /> class with the specified <see cref="User" /> and a
        ///     value that indicates whether to inherit from the current process' environment.
        /// </summary>
        /// <param name="user">
        ///     A <see cref="User" /> that represents the logged-on user from which to retrieve the environment
        ///     variables.
        /// </param>
        /// <param name="inherit">
        ///     A value that indicates whether to inherit from the current process' environment. If this value is
        ///     true, the process inherits the current process' environment. Otherwise, the process does not inherit the current
        ///     process' environment. The default value is false.
        /// </param>
        public UserEnvironment(User user, bool inherit = false)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!NativeMethods.CreateEnvironmentBlock(out _token, user.Token, inherit))
            {
                throw new Win32Exception();
            }
        }

        /// <summary>
        ///     Gets an <see cref="IntPtr" /> that represents the environment of the specified <see cref="User" />.
        /// </summary>
        public IntPtr Token
        {
            get
            {
                return _token;
            }
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
                    if (!NativeMethods.DestroyEnvironmentBlock(_token))
                    {
                        throw new Win32Exception();
                    }

                    GC.SuppressFinalize(this);
                    _isDisposed = true;
                }
            }
        }

        ~UserEnvironment()
        {
            Dispose();
        }

        #endregion
    }
}
