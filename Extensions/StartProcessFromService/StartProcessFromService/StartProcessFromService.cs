//-----------------------------------------------------------------------
// <copyright file="StartProcessFromService.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;
using WindowsProcesses;

namespace StartProcessFromService
{
    // represents the default type of the TwinCAT HMI server extension
    public class StartProcessFromService : IServerExtension
    {
        private readonly RequestListener requestListener = new RequestListener();

        // initializes the TwinCAT HMI server extension
        public ErrorValue Init()
        {
            try
            {
                // add event handlers
                this.requestListener.OnRequest += this.OnRequest;

                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                TcHmiAsyncLogger.Send(Severity.Error, "errorInit", ex.ToString());
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        // called when a client requests a symbol from the domain of the TwinCAT HMI server extension
        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            // handle all commands one by one
            foreach (Command command in e.Commands)
            {
                try
                {
                    // use the mapping to check which command is requested
                    switch (command.Mapping)
                    {
                        case "StartProcess":
                            StartProcess(command);
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

        private static Value RetrieveOptionalValue(Value writeValue, string key)
        {
            return writeValue.TryGetValue(key, out var value) ? value : null;
        }

        private static void StartProcess(Command command)
        {
            var writeValue = command.WriteValue;

            if ((writeValue != null) && writeValue.IsMapOrStruct)
            {
                string applicationName = RetrieveOptionalValue(writeValue, "applicationName");
                string commandLine = RetrieveOptionalValue(writeValue, "commandLine");
                string workingDirectory = RetrieveOptionalValue(writeValue, "workingDirectory");
                bool showWindow = RetrieveOptionalValue(writeValue, "showWindow") ?? false;

                // Use this method to create a process.
                //
                // If the calling process is running as a service in the context of the LocalSystem (SYSTEM) account (this is the case when the TwinCAT HMI server that has loaded the server extension
                // is running as a service), the created process runs in the security context of the logged-on user instead of the LocalSystem account.
                //
                // If the calling process is running in user interactive mode (this is the case when the server extension is loaded by a TwinCAT HMI engineering server), the created process runs in
                // the security context of the calling process instead.
                var process = UserProcess.Create(applicationName, commandLine, workingDirectory, showWindow);
                command.ReadValue = process.Id;
            }
        }
    }
}
