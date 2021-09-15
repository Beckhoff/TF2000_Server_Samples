//-----------------------------------------------------------------------
// <copyright file="ErrorHandling.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;

namespace ErrorHandling
{
    public class ErrorHandling : IServerExtension, IErrorProvider
    {
        private readonly RequestListener _requestListener = new RequestListener();

        public ErrorValue Init()
        {
            // add event handlers
            _requestListener.OnRequest += OnRequest;

            return ErrorValue.HMI_SUCCESS;
        }

        public string ErrorString(uint errorCode)
        {
            string name = Enum.GetName(typeof(ExtensionSpecificError), errorCode);
            return string.IsNullOrEmpty(name) ? "UNKNOWN" : name;
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
                        case "FailingFunction":
                            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FUNCTION_FAILED);
                            command.ResultString = "This function always fails.";
                            break;
                        default:
                            throw new Exception("Handler is missing.");
                    }
                }
                catch (Exception ex)
                {
                    // ignore exceptions and continue processing the other commands in the group
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.INTERNAL_ERROR);
                    command.ResultString = "An exception was thrown while the command was processed by the extension: '" + ex.Message + "'.";
                }
            }
        }
    }
}
