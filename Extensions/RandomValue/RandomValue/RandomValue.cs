//-----------------------------------------------------------------------
// <copyright file="RandomValue.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;
using ValueType = TcHmiSrv.Core.ValueType;

namespace RandomValue
{
    // represents the default type of the TwinCAT HMI server extension
    public class RandomValue : IServerExtension
    {
        private readonly RequestListener requestListener = new RequestListener();

        private readonly Random rand = new Random();

        private Data data;

        // initializes the TwinCAT HMI server extension
        public ErrorValue Init()
        {
            try
            {
                // initialize data from configuration
                // this can not be done before Init, because the properties of TcHmiApplication are initialized just before 
                data = new Data(TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "maxRandom"));

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
                        case "RandomValue":
                            NextRandomValue(command);
                            break;
                        case "MaxRandom":
                            MaxRandom(command);
                            break;
                        case "MaxRandomFromConfig":
                            MaxRandomFromConfig(command);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // ignore exceptions and continue processing the other commands in the group
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.INTERNAL_ERROR);
                    command.ResultString = TcHmiAsyncLogger.Localize(e.Context, "errorCallCommand", new string[] { command.Mapping, ex.ToString() });
                }
            }
        }

        // generates a random value and writes it to the read value of the specified command
        private void NextRandomValue(Command command)
        {
            command.ReadValue = this.rand.Next(this.data.MaxRandom) + 1;
            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
        }

        // gets or sets the maximum random value
        private void MaxRandom(Command command)
        {
            if (command.WriteValue != null && command.WriteValue.Type == ValueType.Int32)
            {
                this.data.MaxRandom = command.WriteValue;
            }

            command.ReadValue = this.data.MaxRandom;
            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
        }

        // gets the maximum random value from the configuration of the TwinCAT HMI server extension
        private void MaxRandomFromConfig(Command command)
        {
            command.ReadValue = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "maxRandom");
            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
        }
    }
}
