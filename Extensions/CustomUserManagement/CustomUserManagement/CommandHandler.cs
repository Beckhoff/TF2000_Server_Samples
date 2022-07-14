using System;
using System.Diagnostics;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;

namespace CustomUserManagement
{
    internal static class CommandHandler
    {
        private const bool ENABLED_BY_DEFAULT = true;

        public static void AddUser(Context context, Command command)
        {
            Debug.Assert(context.Domain == TcHmiApplication.Context.Domain);  // make sure that nobody passes a server context

            string username = command.WriteValue[StringConstants.USERNAME];
            string plain_password = command.WriteValue[StringConstants.PASSWORD];

            // if you want to check whether the user exists already, you can fetch the config value.
            // here, we simply ignore existing accounts and overwrite them entirely.
            //
            // Value existingUserValue = TcHmiApplication.AsyncHost.GetConfigValue(context, userPath);

            command.ExtensionResult = Convert.ToUInt32(User.CreateUser(
                out User user,
                username,
                plain_password,
                command.WriteValue.TryGetValue(StringConstants.ENABLED, out var venabled) ? (bool)venabled : ENABLED_BY_DEFAULT
            ));
            if (command.ExtensionResult != 0)
            {
                return;
            }

            Context internalContext = context;
            internalContext.Session.EndpointInfo = EndpointInfo.Internal;

            var path = TcHmiApplication.JoinPath(StringConstants.CFG_USERS, username);
            ErrorValue serverError = TcHmiApplication.AsyncHost.SetConfigValue(internalContext, path, user.ConfigValue);
            if (serverError == ErrorValue.HMI_SUCCESS)
            {
                Context serverContext = context;
                serverContext.Domain = StringConstants.SERVER_DOMAIN;

                path = TcHmiApplication.JoinPath(StringConstants.USERGROUPUSERS, TcHmiApplication.Context.Domain, username);
                if (TcHmiApplication.AsyncHost.GetConfigValue(serverContext, path).Type == TcHmiSrv.Core.ValueType.Null)
                {
                    // new user, add to the default group
                    var groups = new Value
                    {
                        StringConstants.DEFAULT_GROUP
                    };
                    var usergroupUser = new Value
                    {
                        { StringConstants.USERGROUPUSERS_GROUPS, groups }
                    };

                    serverError = TcHmiApplication.AsyncHost.SetConfigValue(serverContext, path, usergroupUser);
                }
            }

            if (serverError != ErrorValue.HMI_SUCCESS)
            {
                // unable to set group access
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.CANNOT_SET_GROUP_ACCESS);
                return;
            }

            TcHmiAsyncLogger.Send(Severity.Verbose, StringConstants.MSG_ADDUSER_SUCCESS, username);
        }

        public static void EnableDisableUser(Context context, Command command)
        {
            Debug.Assert(context.Domain == TcHmiApplication.Context.Domain);  // make sure that nobody passes a server context

            string username = command.WriteValue;
            string path = TcHmiApplication.JoinPath(StringConstants.CFG_USERS, username);

            if (TcHmiApplication.AsyncHost.GetConfigValue(context, path).Type != TcHmiSrv.Core.ValueType.Null)
            {
                ErrorValue err = TcHmiApplication.AsyncHost.SetConfigValue(context, TcHmiApplication.JoinPath(path, StringConstants.CFG_USER_ENABLED), string.Equals(command.Mapping, StringConstants.ENABLE_USER_COMMAND));
                if (err != ErrorValue.HMI_SUCCESS)
                {
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILED);
                }
            }
            else
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.USER_NOT_FOUND);
            }
        }

        public static void RemoveUser(Context context, Command command)
        {
            Debug.Assert(context.Domain == TcHmiApplication.Context.Domain);  // make sure that nobody passes a server context

            string username = command.WriteValue;
            string userPath = TcHmiApplication.JoinPath(StringConstants.CFG_USERS, username);

            Value user = TcHmiApplication.AsyncHost.GetConfigValue(context, userPath);
            if (user.Type == TcHmiSrv.Core.ValueType.Null)
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.USER_NOT_FOUND);
            }

            ErrorValue error = TcHmiApplication.AsyncHost.DeleteConfigValue(context, userPath);
            if (error != ErrorValue.HMI_SUCCESS)
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILED);
            }
            else
            {
                Context serverContext = context.ShallowCopy();  // perform a shallow copy so that we can change the domain of the 'serverContext' without affecting 'context'
                serverContext.Domain = StringConstants.SERVER_DOMAIN;

                TcHmiApplication.AsyncHost.DeleteConfigValue(serverContext, TcHmiApplication.JoinPath(StringConstants.USERGROUPUSERS, TcHmiApplication.Context.Domain, username));
            }
        }

        public static void ChangePassword(Context context, Command command)
        {
            Debug.Assert(context.Domain == TcHmiApplication.Context.Domain);  // make sure that nobody passes a server context

            string oldPassword = command.WriteValue[StringConstants.OLD_PASSWORD];
            string newPassword = command.WriteValue[StringConstants.NEW_PASSWORD];


            var userPath = TcHmiApplication.JoinPath(StringConstants.CFG_USERS, User.UsernameFromSession(context.Session.User));
            Value userConfigValue = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, userPath);
            if (userConfigValue.Type == TcHmiSrv.Core.ValueType.Null)
            {
                command.ExtensionResult = Convert.ToUInt32(ErrorValue.HMI_E_AUTH_USER_NOT_FOUND);
                return;
            }

            // check the current credentials
            command.ExtensionResult = Convert.ToUInt32((new User(userConfigValue)).CheckCredentials(User.UsernameFromSession(context.Session.User), oldPassword));
            if (command.ExtensionResult != 0)
            {
                return;
            }

            Value oldUserValue = TcHmiApplication.AsyncHost.GetConfigValue(context, userPath);
            command.ExtensionResult = Convert.ToUInt32(User.CreateUser(out User updatedUser, User.UsernameFromSession(context.Session.User), newPassword, oldUserValue.TryGetValue(StringConstants.CFG_USER_ENABLED, out var enabled) ? (bool)enabled : ENABLED_BY_DEFAULT));
            if (command.ExtensionResult != 0)
            {
                return;
            }

            Context adminContext = TcHmiApplication.Context;
            adminContext.Domain = context.Domain;
            adminContext.Session.EndpointInfo = EndpointInfo.Internal;

            command.ExtensionResult = Convert.ToUInt32(TcHmiApplication.AsyncHost.SetConfigValue(adminContext, userPath, updatedUser.ConfigValue));
        }

        public static void RenameUser(Context context, Command command)
        {
            string oldName = command.WriteValue[StringConstants.OLD_USERNAME];
            string newName = command.WriteValue[StringConstants.NEW_USERNAME];

            string oldPath = TcHmiApplication.JoinPath(StringConstants.CFG_USERS, oldName);
            string newPath = TcHmiApplication.JoinPath(StringConstants.CFG_USERS, newName);

            Value current = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, oldPath);
            Value target = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, newPath);
            if (current.Type == TcHmiSrv.Core.ValueType.Null)
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.USER_NOT_FOUND);
            }
            else if (target.Type != TcHmiSrv.Core.ValueType.Null)
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.USER_ALREADY_EXISTS);
            }
            else
            {
                Context adminContext = TcHmiApplication.Context.DeepCopy();

                // rename entry in usermanagement extension
                if (TcHmiApplication.AsyncHost.RenameConfigValue(adminContext, oldPath, newPath) != ErrorValue.HMI_SUCCESS)
                {
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILED);
                }
                else
                {
                    adminContext.Domain = StringConstants.SERVER_DOMAIN;

                    // rename entry in USERGROUPUSERS
                    string basePath = TcHmiApplication.JoinPath("USERGROUPUSERS", TcHmiApplication.Context.Domain, "");
                    if (TcHmiApplication.AsyncHost.RenameConfigValue(adminContext, basePath + oldName, basePath + newName) != ErrorValue.HMI_SUCCESS)
                    {
                        command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILED);
                    }
                }
            }
        }
    }
}
