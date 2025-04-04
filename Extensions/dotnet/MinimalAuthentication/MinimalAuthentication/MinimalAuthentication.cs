//-----------------------------------------------------------------------
// <copyright file="MinimalAuthentication.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.AuthListenerEventArgs;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;

namespace MinimalAuthentication
{
    // ReSharper disable once UnusedType.Global
    public class MinimalAuthentication : IServerExtension
    {
        private readonly AuthListener _authListener = new AuthListener();
        private readonly RequestListener _requestListener = new RequestListener();

        public ErrorValue Init()
        {
            var serverContext = TcHmiApplication.Context;
            serverContext.Domain = "TcHmiSrv";

            // add event handlers
            _authListener.OnLogin += OnLogin;
            _requestListener.OnRequest += OnRequest;

            // decide on a default auto logoff duration
            var autoLogoff = TcHmiApplication.AsyncHost.GetConfigValue(serverContext, "AUTO_LOGOFF");

            // here, you can configure group memberships, localization settings and auto logoff duration for the default users.
            // keep in mind that since we are in the 'Init' method, these settings are re-set (potentially overwriting changes)
            // every time this extension is initialized. in your authentication extension you should instead check if the entry
            // in the server configuration already exists.

            var adminGroups = new Value { "__SystemAdministrators" };

            var admin = new Value
            {
                { "USERGROUPUSERS_LOCALE", "client" },
                { "USERGROUPUSERS_GROUPS", adminGroups },
                { "USERGROUPUSERS_AUTO_LOGOFF", autoLogoff }
            };

            var users = new Value { { "admin", admin } };

            return TcHmiApplication.AsyncHost.ReplaceConfigValue(
                serverContext,
                TcHmiApplication.JoinPath("USERGROUPUSERS", TcHmiApplication.Context.Domain),
                users
            );
        }

        private void OnLogin(object sender, OnLoginEventArgs e)
        {
            string username = e.Value["userName"];
            string plainPassword = e.Value["password"];

            if (username != "admin")
            {
                throw new TcHmiException("unknown user", ErrorValue.HMI_E_AUTH_USER_NOT_FOUND);
            }

            if (plainPassword != "123")
            {
                throw new TcHmiException("invalid password", ErrorValue.HMI_E_AUTH_FAILED);
            }
        }

        private Value ListUsers()
        {
            var names = new Value { "admin" };
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
                        case "ListUsers":
                        {
                            var tmp = ListUsers();
                            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
                            command.ReadValue = tmp;
                            break;
                        }
                    }
                }
                catch
                {
                    // ignore exceptions and continue processing the other commands in the group
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.InternalError);
                }
            }
        }
    }
}
