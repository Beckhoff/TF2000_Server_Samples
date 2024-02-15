//-----------------------------------------------------------------------
// <copyright file="CustomConfig.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;

namespace CustomConfig
{
    // Represents the default type of the TwinCAT HMI server extension.
    public class CustomConfig : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();

        // Called after the TwinCAT HMI server loaded the server extension.
        public ErrorValue Init()
        {
            _requestListener.OnRequest += OnRequest;
            return ErrorValue.HMI_SUCCESS;
        }

        // Called when a client requests a symbol from the domain of the TwinCAT HMI server extension.
        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            try
            {
                e.Commands.Result = CustomConfigErrorValue.CustomConfigSuccess;

                foreach (var command in e.Commands)
                {
                    try
                    {
                        // Use the mapping to check which command is requested
                        switch (command.Mapping)
                        {
                            // case "YOUR_MAPPING":
                            //     Handle command
                            //     break;

                            case "GetRandom":
                                int max = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "max");
                                int min = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "min");
                                var r = new Random();
                                command.ReadValue = r.Next(min, max);
                                break;

                            default:
                                command.ExtensionResult = CustomConfigErrorValue.CustomConfigFail;
                                command.ResultString = "Unknown command '" + command.Mapping + "' not handled.";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        command.ExtensionResult = CustomConfigErrorValue.CustomConfigFail;
                        command.ResultString = "Calling command '" + command.Mapping + "' failed! Additional information: " + ex.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TcHmiException(ex.ToString(), ErrorValue.HMI_E_EXTENSION);
            }
        }
    }
}
