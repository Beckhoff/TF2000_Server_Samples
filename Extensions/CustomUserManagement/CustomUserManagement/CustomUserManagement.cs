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
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.Settings;
using ValueType = TcHmiSrv.Core.ValueType;

namespace CustomUserManagement
{
    public class CustomUserManagement : IServerExtension
    {
        private readonly ReaderWriterLockSlim _rwl = new ReaderWriterLockSlim();

        private readonly AuthListener _authListener = new AuthListener();
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly ConfigListener _configListener = new ConfigListener();

        public ErrorValue Init()
        {
            try
            {
                Context serverContext = TcHmiApplication.Context;
                serverContext.Domain = StringConstants.SERVER_DOMAIN;

                // add event handlers
                _authListener.OnLogin += OnLogin;
                _requestListener.OnRequest += OnRequest;
                _configListener.OnDelete += OnDelete;

                // tell the config listener that we're interested in everything
                TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _configListener, ConfigListenerSettings.Default);

                // make sure that the USERGROUPUSERS entry for this extension exists in TcHmiSrv.Config
                if (TcHmiApplication.AsyncHost.GetConfigValue(serverContext, TcHmiApplication.JoinPath(StringConstants.USERGROUPUSERS, TcHmiApplication.Context.Domain)).Type == TcHmiSrv.Core.ValueType.Null)
                {
                    var map = new Value
                    {
                        Type = ValueType.Map
                    };
                    var tmp = new Value
                    {
                        { TcHmiApplication.Context.Domain, map }
                    };
                    TcHmiApplication.AsyncHost.SetConfigValue(serverContext, StringConstants.USERGROUPUSERS, tmp);
                }

                TcHmiAsyncLogger.Send(Severity.Info, StringConstants.MSG_INIT);
                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                TcHmiAsyncLogger.Send(Severity.Error, StringConstants.MSG_ERROR_INIT, ex.Message);
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        private void OnLogin(object sender, OnLoginEventArgs e)
        {
            SafeReadAction(() =>
            {
                string username = e.Value[StringConstants.USERNAME];
                string plain_password = e.Value[StringConstants.PASSWORD];

                var userPath = TcHmiApplication.JoinPath(StringConstants.CFG_USERS, username);
                Value userConfigValue = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, userPath);
                if (userConfigValue.Type == ValueType.Null)
                {
                    throw new TcHmiException(ErrorValue.HMI_E_AUTH_USER_NOT_FOUND);
                }

                ErrorValue result = (new User(userConfigValue)).CheckCredentials(username, plain_password);
                if (result != ErrorValue.HMI_SUCCESS)
                {
                    throw new TcHmiException(result);
                }

                if (username != StringConstants.ADMIN_USERNAME)
                {
                    Command command = new Command(StringConstants.GET_CURRENT_USER);

                    // example of inter-extension communication.
                    // make a request to the server domain
                    Context context = e.Context;
                    context.Domain = StringConstants.SERVER_DOMAIN;
                    TcHmiApplication.AsyncHost.Execute(ref context, ref command);

                    Value currentUser = command.ReadValue;
                    if ((currentUser != null) && currentUser.TryGetValue(StringConstants.CLIENT_IP, out var clientIp))
                    {
                        if (clientIp == "127.0.0.1")
                        {
                            // handle special rights for local access of the user
                            // e.g. add a special group (that must exist in the Server)
                        }
                    }
                }
            });
        }

        private Value ListUsers(Context context, bool disabledOnly)
        {
            Debug.Assert(context.Domain == TcHmiApplication.Context.Domain);  // make sure that nobody passes a server context

            Value names = new Value
            {
                Type = ValueType.Vector  // we need to set the type explicitly. otherwise it wouldn't be an array if there are no users to list
            };

            Value users = TcHmiApplication.AsyncHost.GetConfigValue(context, StringConstants.CFG_USERS);
            if (users.Type == ValueType.Null)
            {
                return null;  // probably not sufficient access rights
            }

            foreach (KeyValuePair<string, Value> user in users)
            {
                if (disabledOnly)
                {
                    user.Value.TryGetValue(StringConstants.CFG_USER_ENABLED, out Value enabled);
                    if ((bool)enabled)
                    {
                        // skip enabled users
                        continue;
                    }
                }
                names.Add(user.Key);
            }
            return names;
        }

        public void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            // handle all commands one by one
            foreach (Command command in e.Commands)
            {
                try
                {
                    switch (command.Mapping)
                    {
                        case StringConstants.LIST_USERS_COMMAND:
                            SafeReadAction(() =>
                            {
                                var tmp = ListUsers(e.Context, false);
                                if (tmp == null)
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILED);
                                    command.ReadValue = new Value();  // when executed as a subscription, the old read-value might still be there
                                }
                                else
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
                                    command.ReadValue = tmp;
                                }
                            });
                            break;
                        case StringConstants.LIST_DISABLED_USERS_COMMAND:
                            SafeReadAction(() =>
                            {
                                var tmp = ListUsers(e.Context, true);
                                if (tmp == null)
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILED);
                                    command.ReadValue = new Value();  // when executed as a subscription, the old read-value might still be there
                                }
                                else
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
                                    command.ReadValue = tmp;
                                }
                            });
                            break;
                        case StringConstants.REMOVE_USER_COMMAND:
                            SafeWriteAction(() => CommandHandler.RemoveUser(e.Context, command));
                            break;
                        case StringConstants.DISABLE_USER_COMMAND:
                        case StringConstants.ENABLE_USER_COMMAND:
                            SafeWriteAction(() => CommandHandler.EnableDisableUser(e.Context, command));
                            break;
                        case StringConstants.CHANGE_PASSWORD_COMMAND:
                            SafeWriteAction(() => CommandHandler.ChangePassword(e.Context, command));
                            break;
                        case StringConstants.ADD_USER_COMMAND:
                            SafeWriteAction(() => CommandHandler.AddUser(e.Context, command));
                            break;
                        case StringConstants.RENAME_USER_COMMAND:
                            SafeWriteAction(() => CommandHandler.RenameUser(e.Context, command));
                            break;
                    }
                }
                catch
                {
                    // ignore exceptions and continue processing the other commands in the group
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.INTERNAL_ERROR);
                }
            }
        }

        private void OnDelete(object sender, OnDeleteEventArgs e)
        {
            string[] parts = TcHmiApplication.SplitPath(e.Path, StringSplitOptions.RemoveEmptyEntries);

            if ((parts.Length > 1) && (parts[0] == StringConstants.CFG_USERS))
            {
                string username = parts[1];

                // remove user from usergroup configuration
                Context serverContext = e.Context;
                serverContext.Domain = StringConstants.SERVER_DOMAIN;

                ErrorValue result = TcHmiApplication.AsyncHost.DeleteConfigValue(serverContext, TcHmiApplication.JoinPath(StringConstants.USERGROUPUSERS, TcHmiApplication.Context.Domain, username));
                if (result != ErrorValue.HMI_SUCCESS)
                {
                    throw new TcHmiException(result);
                }
            }
        }

        public void SafeReadAction(Action action)
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
        public void SafeWriteAction(Action action)
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
