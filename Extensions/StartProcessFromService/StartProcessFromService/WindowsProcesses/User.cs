using System;
using System.ComponentModel;
using System.Diagnostics;

namespace WindowsProcesses
{
    /// <summary>
    /// Represents a logged-on user.
    /// </summary>
    public sealed class User : IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IntPtr _token;

        /// <summary>
        /// Gets an <see cref="IntPtr"/> that represents the primary access token of the logged-on user.
        /// </summary>
        public IntPtr Token
        {
            get
            {
                return _token;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class with the specified session identifier.
        /// </summary>
        /// <param name="sessionId">The session identifier of the logged-on user from which to obtain the primary access token.</param>
        public User(uint sessionId)
        {
            if (!NativeMethods.WTSQueryUserToken(sessionId, out _token))
                throw new Win32Exception();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class with the session identifier of the console session. The console session is the session that is currently attached to the physical console.
        /// </summary>
        public User() : this(UserSession.GetActiveConsoleSessionId())
        {
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
                    if (!NativeMethods.CloseHandle(_token))
                        throw new Win32Exception();

                    GC.SuppressFinalize(this);
                    _isDisposed = true;
                }
            }
        }

        ~User()
        {
            Dispose();
        }

        #endregion
    }
}
