using System;
using System.Diagnostics;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using ValueType = TcHmiSrv.Core.ValueType;

namespace CustomUserManagement
{
    internal static class CommandHandler
    {
        public static void AddUser(Context context, Command command)
        {
            Debug.Assert(context.Domain ==
                         TcHmiApplication.Context.Domain); // make sure that nobody passes a server context

            string username = command.WriteValue[StringConstants.Username];
            string plainPassword = command.WriteValue[StringConstants.Password];

            // if you want to check whether the user exists already, you can fetch the config value.
            // here, we simply ignore existing accounts and overwrite them entirely.
            //
            // Value existingUserValue = TcHmiApplication.AsyncHost.GetConfigValue(context, userPath);

            command.ExtensionResult = Convert.ToUInt32(User.CreateUser(
                out var user,
                username,
                plainPassword,
                !command.WriteValue.TryGetValue(StringConstants.Enabled, out var enabled) || enabled
            ));

            if (command.ExtensionResult != 0)
            {
                return;
            }

            var internalContext = context;
            internalContext.Session.EndpointInfo = EndpointInfo.Internal;

            var path = TcHmiApplication.JoinPath(StringConstants.CfgUsers, username);
            var serverError = TcHmiApplication.AsyncHost.SetConfigValue(internalContext, path, user.ConfigValue);

            if (serverError == ErrorValue.HMI_SUCCESS)
            {
                var serverContext = context;
                serverContext.Domain = StringConstants.ServerDomain;

                path = TcHmiApplication.JoinPath(StringConstants.UserGroupUsers, TcHmiApplication.Context.Domain,
                    username);

                if (TcHmiApplication.AsyncHost.GetConfigValue(serverContext, path).Type == ValueType.Null)
                {
                    // new user, add to the default group
                    var groups = new Value { StringConstants.DefaultGroup };
                    var usergroupUser = new Value { { StringConstants.UserGroupUsersGroups, groups } };

                    serverError = TcHmiApplication.AsyncHost.SetConfigValue(serverContext, path, usergroupUser);
                }
            }

            if (serverError != ErrorValue.HMI_SUCCESS)
            {
                // unable to set group access
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.CannotSetGroupAccess);
                return;
            }

            _ = TcHmiAsyncLogger.Send(Severity.Verbose, StringConstants.MsgAdduserSuccess, username);
        }

        public static void EnableDisableUser(Context context, Command command)
        {
            Debug.Assert(context.Domain ==
                         TcHmiApplication.Context.Domain); // make sure that nobody passes a server context

            string username = command.WriteValue;
            var path = TcHmiApplication.JoinPath(StringConstants.CfgUsers, username);

            if (TcHmiApplication.AsyncHost.GetConfigValue(context, path).Type != ValueType.Null)
            {
                var err = TcHmiApplication.AsyncHost.SetConfigValue(context,
                    TcHmiApplication.JoinPath(path, StringConstants.CfgUserEnabled),
                    string.Equals(command.Mapping, StringConstants.EnableUserCommand));

                if (err != ErrorValue.HMI_SUCCESS)
                {
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failed);
                }
            }
            else
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.UserNotFound);
            }
        }

        public static void RemoveUser(Context context, Command command)
        {
            Debug.Assert(context.Domain ==
                         TcHmiApplication.Context.Domain); // make sure that nobody passes a server context

            string username = command.WriteValue;
            var userPath = TcHmiApplication.JoinPath(StringConstants.CfgUsers, username);

            var user = TcHmiApplication.AsyncHost.GetConfigValue(context, userPath);

            if (user.Type == ValueType.Null)
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.UserNotFound);
            }

            var error = TcHmiApplication.AsyncHost.DeleteConfigValue(context, userPath);

            if (error != ErrorValue.HMI_SUCCESS)
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failed);
            }
            else
            {
                var serverContext =
                    context.ShallowCopy(); // perform a shallow copy so that we can change the domain of the 'serverContext' without affecting 'context'
                serverContext.Domain = StringConstants.ServerDomain;

                _ = TcHmiApplication.AsyncHost.DeleteConfigValue(serverContext,
                    TcHmiApplication.JoinPath(StringConstants.UserGroupUsers, TcHmiApplication.Context.Domain,
                        username));
            }
        }

        public static void ChangePassword(Context context, Command command)
        {
            Debug.Assert(context.Domain ==
                         TcHmiApplication.Context.Domain); // make sure that nobody passes a server context

            string oldPassword = command.WriteValue[StringConstants.OldPassword];
            string newPassword = command.WriteValue[StringConstants.NewPassword];

            var userPath = TcHmiApplication.JoinPath(StringConstants.CfgUsers,
                User.UsernameFromSession(context.Session.User));
            var userConfigValue = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, userPath);

            if (userConfigValue.Type == ValueType.Null)
            {
                command.ExtensionResult = Convert.ToUInt32(ErrorValue.HMI_E_AUTH_USER_NOT_FOUND);
                return;
            }

            // check the current credentials
            command.ExtensionResult = Convert.ToUInt32(new User(userConfigValue).CheckCredentials(oldPassword));

            if (command.ExtensionResult != 0)
            {
                return;
            }

            var oldUserValue = TcHmiApplication.AsyncHost.GetConfigValue(context, userPath);
            command.ExtensionResult = Convert.ToUInt32(User.CreateUser(out var updatedUser,
                User.UsernameFromSession(context.Session.User), newPassword,
                !oldUserValue.TryGetValue(StringConstants.CfgUserEnabled, out var enabled) || enabled));

            if (command.ExtensionResult != 0)
            {
                return;
            }

            var adminContext = TcHmiApplication.Context;
            adminContext.Domain = context.Domain;
            adminContext.Session.EndpointInfo = EndpointInfo.Internal;

            command.ExtensionResult =
                Convert.ToUInt32(
                    TcHmiApplication.AsyncHost.SetConfigValue(adminContext, userPath, updatedUser.ConfigValue));
        }

        public static void RenameUser(Command command)
        {
            string oldName = command.WriteValue[StringConstants.OldUsername];
            string newName = command.WriteValue[StringConstants.NewUsername];

            var oldPath = TcHmiApplication.JoinPath(StringConstants.CfgUsers, oldName);
            var newPath = TcHmiApplication.JoinPath(StringConstants.CfgUsers, newName);

            var current = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, oldPath);
            var target = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, newPath);

            if (current.Type == ValueType.Null)
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.UserNotFound);
            }
            else if (target.Type != ValueType.Null)
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.UserAlreadyExists);
            }
            else
            {
                var adminContext = TcHmiApplication.Context;

                // rename entry in usermanagement extension
                if (TcHmiApplication.AsyncHost.RenameConfigValue(adminContext, oldPath, newPath) !=
                    ErrorValue.HMI_SUCCESS)
                {
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failed);
                }
                else
                {
                    adminContext.Domain = StringConstants.ServerDomain;

                    // rename entry in USERGROUPUSERS
                    var basePath = TcHmiApplication.JoinPath("USERGROUPUSERS", TcHmiApplication.Context.Domain, "");

                    if (TcHmiApplication.AsyncHost.RenameConfigValue(adminContext, basePath + oldName,
                            basePath + newName) != ErrorValue.HMI_SUCCESS)
                    {
                        command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failed);
                    }
                }
            }
        }
    }
}
