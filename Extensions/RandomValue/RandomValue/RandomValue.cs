//-----------------------------------------------------------------------
// <copyright file="RandomValue.cs" company="Beckhoff Automation GmbH & Co. KG">
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

namespace RandomValue
{
    // represents the default type of the TwinCAT HMI server extension
    // ReSharper disable once UnusedType.Global
    public class RandomValue : IServerExtension
    {
        private readonly Random _rand = new Random();
        private readonly RequestListener _requestListener = new RequestListener();

        private Data _data;

        // initializes the TwinCAT HMI server extension
        public ErrorValue Init()
        {
            try
            {
                // initialize data from configuration
                // this can not be done before Init, because the properties of TcHmiApplication are initialized just before 
                _data = new Data(TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "maxRandom"));

                // add event handlers
                _requestListener.OnRequest += OnRequest;

                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                _ = TcHmiAsyncLogger.Send(Severity.Error, "errorInit", ex.ToString());
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        // called when a client requests a symbol from the domain of the TwinCAT HMI server extension
        private void OnRequest(object sender, OnRequestEventArgs e)
        {
            // handle all commands one by one
            foreach (var command in e.Commands)
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
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.InternalError);
                    command.ResultString =
                        TcHmiAsyncLogger.Localize(e.Context, "errorCallCommand", command.Mapping, ex.ToString());
                }
            }
        }

        // generates a random value and writes it to the read value of the specified command
        private void NextRandomValue(Command command)
        {
            command.ReadValue = _rand.Next(_data.MaxRandom) + 1;
            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
        }

        // gets or sets the maximum random value
        private void MaxRandom(Command command)
        {
            if (command.WriteValue != null && command.WriteValue.Type == ValueType.Int32)
            {
                _data.MaxRandom = command.WriteValue;
            }

            command.ReadValue = _data.MaxRandom;
            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
        }

        // gets the maximum random value from the configuration of the TwinCAT HMI server extension
        private void MaxRandomFromConfig(Command command)
        {
            command.ReadValue = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "maxRandom");
            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
        }
    }
}
