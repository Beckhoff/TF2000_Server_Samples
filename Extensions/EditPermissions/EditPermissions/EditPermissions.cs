//-----------------------------------------------------------------------
// <copyright file="EditPermissions.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;
using ValueType = TcHmiSrv.Core.ValueType;

namespace EditPermissions
{
    // ReSharper disable once UnusedType.Global
    public class EditPermissions : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();

        public ErrorValue Init()
        {
            // add event handlers
            _requestListener.OnRequest += OnRequest;

            return ErrorValue.HMI_SUCCESS;
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
                        case "ConfigureOperator":
                            command.ExtensionResult = (uint)ConfigureOperator();
                            break;
                        case "ToggleOperatorAccess":
                            command.ExtensionResult = (uint)ToggleOperatorAccess(command.WriteValue);
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

        private static ExtensionSpecificError ConfigureOperatorsGroup()
        {
            // this function adds a new user group called "operators" that,
            // by default, has full access to all symbols of all domains/extensions.
            // the "SYMBOLS" object is used to overwrite the default "SYMBOLACCESS" and
            // disable access to some specific symbols.

            var files = new Value { Type = ValueType.Map };
            var symbols = new Value { { "ADS.CheckLicense", 0 }, { "TcHmiSrv.Config", 0 } };
            var result = TcHmiApplication.AsyncHost.ReplaceConfigValue(
                ServerContextWithAdminPermissions(),
                TcHmiApplication.JoinPath("USERGROUPS", "operators"),
                new Value
                {
                    { "ENABLED", true },
                    { "FILEACCESS", 3 },
                    { "FILES", files },
                    { "SYMBOLACCESS", 3 },
                    { "SYMBOLS", symbols }
                }
            );
            return result == ErrorValue.HMI_SUCCESS
                ? ExtensionSpecificError.Success
                : ExtensionSpecificError.ConfigurationChangeFailed;
        }

        private static ExtensionSpecificError ConfigureOperatorUser()
        {
            // this function adds a new user called "operator" that is
            // a member of the "operators" user group.

            var groups = new Value { Type = ValueType.Vector };
            groups.Add("operators");

            var result = TcHmiApplication.AsyncHost.ReplaceConfigValue(
                ServerContextWithAdminPermissions(),
                TcHmiApplication.JoinPath("USERGROUPUSERS", "TcHmiUserManagement", "operator"),
                new Value
                {
                    { "USERGROUPUSERS_AUTO_LOGOFF", "PT0S" },
                    { "USERGROUPUSERS_GROUPS", groups },
                    { "USERGROUPUSERS_LOCALE", "project" }
                }
            );
            return result == ErrorValue.HMI_SUCCESS
                ? ExtensionSpecificError.Success
                : ExtensionSpecificError.ConfigurationChangeFailed;
        }

        private ExtensionSpecificError ConfigureOperator()
        {
            var result = ConfigureOperatorsGroup();
            return result == ExtensionSpecificError.Success
                ? ConfigureOperatorUser()
                : result;
        }

        private static ExtensionSpecificError ToggleOperatorAccess(string symbolName)
        {
            // this function checks what the default "SYMBOLACCESS" of the "operators" user group is
            // and toggles access to a specific symbol by changing the "SYMBOLS" object.

            var defaultSymbolAccess = (Access)(int)TcHmiApplication.AsyncHost.GetConfigValue(
                ServerContextWithAdminPermissions(),
                TcHmiApplication.JoinPath("USERGROUPS", "operators", "SYMBOLACCESS")
            );
            var symbols = TcHmiApplication.AsyncHost.GetConfigValue(
                ServerContextWithAdminPermissions(),
                TcHmiApplication.JoinPath("USERGROUPS", "operators", "SYMBOLS")
            );
            if (symbols.ContainsKey(symbolName))
            {
                _ = symbols.Remove(symbolName);
            }
            else
            {
                symbols.Add(symbolName, ToggleAccessLevel(defaultSymbolAccess));
            }

            var result = TcHmiApplication.AsyncHost.ReplaceConfigValue(
                ServerContextWithAdminPermissions(),
                TcHmiApplication.JoinPath("USERGROUPS", "operators", "SYMBOLS"),
                symbols
            );
            return result == ErrorValue.HMI_SUCCESS
                ? ExtensionSpecificError.Success
                : ExtensionSpecificError.ConfigurationChangeFailed;
        }

        private static Access ToggleAccessLevel(Access access)
        {
            if (access == Access.None)
            {
                return Access.ReadWrite;
            }

            if (access == Access.ReadWrite)
            {
                return Access.None;
            }

            return access;
        }

        private static Context ServerContextWithAdminPermissions()
        {
            var serverContext = TcHmiApplication.Context;
            serverContext.Domain = "TcHmiSrv";
            return serverContext;
        }
    }
}
