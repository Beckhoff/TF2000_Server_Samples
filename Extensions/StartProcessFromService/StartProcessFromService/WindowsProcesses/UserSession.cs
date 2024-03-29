﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsProcesses.NativeTypes;

// ReSharper disable once CheckNamespace
namespace WindowsProcesses
{
    /// <summary>
    ///     Contains information about a client session on a Remote Desktop Session Host (RD Session Host) server.
    /// </summary>
    public sealed class UserSession
    {
        private UserSession(WTS_SESSION_INFO sessionInfo)
        {
            Id = sessionInfo.SessionID;
            WinStation = sessionInfo.pWinStationName;
            IsActive = sessionInfo.State == WTS_CONNECTSTATE_CLASS.WTSActive;
        }

        /// <summary>
        ///     Gets the session identifier.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public uint Id { get; }

        /// <summary>
        ///     Gets the name of the WinStation. Windows associates the name of the WinStation with the session, for example,
        ///     "services", "console", or "RDP-Tcp#0".
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string WinStation { get; }

        /// <summary>
        ///     Gets a value that indicates whether the session is active.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsActive { get; }

        /// <summary>
        ///     Retrieves an <see cref="IReadOnlyCollection{T}" /> of <see cref="UserSession" />s on a Remote Desktop Session Host
        ///     (RD Session Host) server.
        /// </summary>
        /// <returns>
        ///     An <see cref="IReadOnlyCollection{T}" /> of <see cref="UserSession" />s on a Remote Desktop Session Host (RD
        ///     Session Host) server.
        /// </returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static IReadOnlyCollection<UserSession> EnumerateSessions()
        {
            using var sessionEnumerator = new UserSessionEnumerator();
            return sessionEnumerator.GetSessions();
        }

        /// <summary>
        ///     Retrieves the session identifier of the console session. The console session is the session that is currently
        ///     attached to the physical console.
        /// </summary>
        /// <returns>The session identifier of the console session.</returns>
        public static uint GetActiveConsoleSessionId()
        {
            foreach (var sessionInfo in EnumerateSessions())
            {
                if (sessionInfo.IsActive)
                {
                    return sessionInfo.Id;
                }
            }

            return NativeMethods.WTSGetActiveConsoleSessionId();
        }

        private sealed class UserSessionEnumerator : IDisposable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private static readonly int s_sessionInfoSize = Marshal.SizeOf<WTS_SESSION_INFO>();

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly int _sessionCount;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly IntPtr _sessionInfos;

            public UserSessionEnumerator()
            {
                if (!NativeMethods.WTSEnumerateSessions(IntPtr.Zero, 0, 1, out _sessionInfos, out _sessionCount))
                {
                    throw new Win32Exception();
                }
            }

            public IReadOnlyCollection<UserSession> GetSessions()
            {
                var sessionInfos = new List<UserSession>(_sessionCount);
                var currentInfo = _sessionInfos;

                for (var i = 0; i < _sessionCount; i++)
                {
                    var sessionInfo = Marshal.PtrToStructure<WTS_SESSION_INFO>(currentInfo);
                    sessionInfos.Add(new UserSession(sessionInfo));

                    currentInfo += s_sessionInfoSize;
                }

                return sessionInfos.AsReadOnly();
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
                        NativeMethods.WTSFreeMemory(_sessionInfos);

                        GC.SuppressFinalize(this);
                        _isDisposed = true;
                    }
                }
            }

            ~UserSessionEnumerator()
            {
                Dispose();
            }

            #endregion
        }
    }
}
