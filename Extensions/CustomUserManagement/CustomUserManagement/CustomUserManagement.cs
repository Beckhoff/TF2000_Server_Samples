//-----------------------------------------------------------------------
// <copyright file="CustomUserManagement.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.AuthListenerEventArgs;
using TcHmiSrv.Core.Listeners.ConfigListenerEventArgs;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.Settings;
using ValueType = TcHmiSrv.Core.ValueType;

namespace CustomUserManagement
{
    // ReSharper disable once UnusedType.Global
    public class CustomUserManagement : IServerExtension
    {
        private readonly AuthListener _authListener = new AuthListener();
        private readonly ConfigListener _configListener = new ConfigListener();
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly ReaderWriterLockSlim _rwl = new ReaderWriterLockSlim();

        public ErrorValue Init()
        {
            try
            {
                var serverContext = TcHmiApplication.Context;
                serverContext.Domain = StringConstants.ServerDomain;

                // add event handlers
                _authListener.OnLogin += OnLogin;
                _requestListener.OnRequest += OnRequest;
                _configListener.OnDelete += OnDelete;

                // tell the config listener that we're interested in everything
                TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _configListener,
                    ConfigListenerSettings.Default);

                // make sure that the USERGROUPUSERS entry for this extension exists in TcHmiSrv.Config
                if (TcHmiApplication.AsyncHost.GetConfigValue(serverContext,
                            TcHmiApplication.JoinPath(StringConstants.UserGroupUsers, TcHmiApplication.Context.Domain))
                        .Type == ValueType.Null)
                {
                    var map = new Value { Type = ValueType.Map };
                    var tmp = new Value { { TcHmiApplication.Context.Domain, map } };
                    _ = TcHmiApplication.AsyncHost.SetConfigValue(serverContext, StringConstants.UserGroupUsers, tmp);
                }

                _ = TcHmiAsyncLogger.Send(Severity.Info, StringConstants.MsgInit);
                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                _ = TcHmiAsyncLogger.Send(Severity.Error, StringConstants.MsgErrorInit, ex.Message);
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        private void OnLogin(object sender, OnLoginEventArgs e)
        {
            SafeReadAction(() =>
            {
                string username = e.Value[StringConstants.Username];
                string plainPassword = e.Value[StringConstants.Password];

                var userPath = TcHmiApplication.JoinPath(StringConstants.CfgUsers, username);
                var userConfigValue = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, userPath);

                if (userConfigValue.Type == ValueType.Null)
                {
                    throw new TcHmiException(ErrorValue.HMI_E_AUTH_USER_NOT_FOUND);
                }

                var result = new User(userConfigValue).CheckCredentials(plainPassword);

                if (result != ErrorValue.HMI_SUCCESS)
                {
                    throw new TcHmiException(result);
                }

                if (username != StringConstants.AdminUsername)
                {
                    var command = new Command(StringConstants.GetCurrentUser);

                    // example of inter-extension communication.
                    // make a request to the server domain
                    var context = e.Context;
                    context.Domain = StringConstants.ServerDomain;
                    _ = TcHmiApplication.AsyncHost.Execute(ref context, ref command);

                    var currentUser = command.ReadValue;

                    if (currentUser != null && currentUser.TryGetValue(StringConstants.ClientIp, out var clientIp) &&
                        clientIp == "127.0.0.1")
                    {
                        // handle special rights for local access of the user
                        // e.g. add a special group (that must exist in the Server)
                    }
                }
            });
        }

        private Value ListUsers(Context context, bool disabledOnly)
        {
            Debug.Assert(context.Domain ==
                         TcHmiApplication.Context.Domain); // make sure that nobody passes a server context

            var names = new Value
            {
                Type = ValueType
                    .Vector // we need to set the type explicitly. otherwise it wouldn't be an array if there are no users to list
            };

            var users = TcHmiApplication.AsyncHost.GetConfigValue(context, StringConstants.CfgUsers);

            if (users.Type == ValueType.Null)
            {
                return null; // probably not sufficient access rights
            }

            foreach (KeyValuePair<string, Value> user in users)
            {
                if (disabledOnly && user.Value.TryGetValue(StringConstants.CfgUserEnabled, out var enabled) && enabled)
                {
                    // skip enabled users
                    continue;
                }

                names.Add(user.Key);
            }

            return names;
        }

        private void OnRequest(object sender, OnRequestEventArgs e)
        {
            // handle all commands one by one
            foreach (var command in e.Commands)
            {
                try
                {
                    switch (command.Mapping)
                    {
                        case StringConstants.ListUsersCommand:
                            SafeReadAction(() =>
                            {
                                var tmp = ListUsers(e.Context, false);

                                if (tmp == null)
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failed);
                                    command.ReadValue =
                                        new Value(); // when executed as a subscription, the old read-value might still be there
                                }
                                else
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
                                    command.ReadValue = tmp;
                                }
                            });
                            break;
                        case StringConstants.ListDisabledUsersCommand:
                            SafeReadAction(() =>
                            {
                                var tmp = ListUsers(e.Context, true);

                                if (tmp == null)
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failed);
                                    command.ReadValue =
                                        new Value(); // when executed as a subscription, the old read-value might still be there
                                }
                                else
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
                                    command.ReadValue = tmp;
                                }
                            });
                            break;
                        case StringConstants.RemoveUserCommand:
                            SafeWriteAction(() => CommandHandler.RemoveUser(e.Context, command));
                            break;
                        case StringConstants.DisableUserCommand:
                        case StringConstants.EnableUserCommand:
                            SafeWriteAction(() => CommandHandler.EnableDisableUser(e.Context, command));
                            break;
                        case StringConstants.ChangePasswordCommand:
                            SafeWriteAction(() => CommandHandler.ChangePassword(e.Context, command));
                            break;
                        case StringConstants.AddUserCommand:
                            SafeWriteAction(() => CommandHandler.AddUser(e.Context, command));
                            break;
                        case StringConstants.RenameUserCommand:
                            SafeWriteAction(() => CommandHandler.RenameUser(command));
                            break;
                    }
                }
                catch
                {
                    // ignore exceptions and continue processing the other commands in the group
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.InternalError);
                }
            }
        }

        private void OnDelete(object sender, OnDeleteEventArgs e)
        {
            var parts = TcHmiApplication.SplitPath(e.Path, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1 && parts[0] == StringConstants.CfgUsers)
            {
                var username = parts[1];

                // remove user from user group configuration
                var serverContext = e.Context;
                serverContext.Domain = StringConstants.ServerDomain;

                var result = TcHmiApplication.AsyncHost.DeleteConfigValue(serverContext,
                    TcHmiApplication.JoinPath(StringConstants.UserGroupUsers, TcHmiApplication.Context.Domain,
                        username));

                if (result != ErrorValue.HMI_SUCCESS)
                {
                    throw new TcHmiException(result);
                }
            }
        }

        private void SafeReadAction(Action action)
        {
            _rwl.EnterReadLock();

            try
            {
                action();
            }
            finally
            {
                _rwl.ExitReadLock();
            }
        }

        private void SafeWriteAction(Action action)
        {
            _rwl.EnterWriteLock();

            try
            {
                action();
            }
            finally
            {
                _rwl.ExitWriteLock();
            }
        }
    }
}
