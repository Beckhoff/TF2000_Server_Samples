//-----------------------------------------------------------------------
// <copyright file="InterExtensionCommunication.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;

namespace InterExtensionCommunication
{
    public class InterExtensionCommunication : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();

        public ErrorValue Init()
        {
            // add event handlers
            _requestListener.OnRequest += OnRequest;

            return ErrorValue.HMI_SUCCESS;
        }

        public void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            var adminContext = TcHmiApplication.Context;

            // handle all commands one by one
            foreach (Command command in e.Commands)
            {
                try
                {
                    switch (command.Mapping)
                    {
                        case "ListLocalRoutes":
                            {
                                // invoke the function symbol of another extension

                                var cmd = new Command("ADS.ListRoutes");
                                ErrorValue result = TcHmiApplication.AsyncHost.Execute(ref adminContext, ref cmd);

                                if (result != ErrorValue.HMI_SUCCESS || cmd.Result != ErrorValue.HMI_SUCCESS)
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILURE);
                                }
                                else
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
                                    command.ReadValue = FilterLocalRoutes(cmd.ReadValue);
                                }
                            }
                            break;
                        case "CheckClientLicenseAndListLocalRoutes":
                            {
                                // invoke multiple function symbols of one other extension simultaneously

                                var checkLicenseCmd = new Command("ADS.CheckLicense")
                                {
                                    WriteValue = "37E5160D-987B-4640-888D-0F97727B53E2" // the UUID of the "TC3 HMI Clients" license
                                };
                                var listLocalRoutesCmd = new Command("ADS.ListRoutes");

                                var group = new CommandGroup("ADS")
                                {
                                    checkLicenseCmd,
                                    listLocalRoutesCmd
                                };
                                ErrorValue result = TcHmiApplication.AsyncHost.Execute(ref adminContext, ref group);
                                if (result != ErrorValue.HMI_SUCCESS ||
                                    group.Result != Convert.ToUInt32(ErrorValue.HMI_SUCCESS) ||
                                    group[0].Result != ErrorValue.HMI_SUCCESS ||
                                    group[1].Result != ErrorValue.HMI_SUCCESS)
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILURE);
                                }
                                else
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
                                    command.ReadValue = new Value();
                                    bool firstIsCheckLicense = (group[0].Name == "ADS.CheckLicense");
                                    command.ReadValue.Add("clientLicense", firstIsCheckLicense ? group[0].ReadValue : group[1].ReadValue);
                                    command.ReadValue.Add("localRoutes", FilterLocalRoutes(
                                        firstIsCheckLicense ? group[1].ReadValue : group[0].ReadValue
                                    ));
                                }
                            }
                            break;
                        case "DoubleAdsTimeout":
                            {
                                // read from and write into the configuration of another extension

                                command.ReadValue = new Value();

                                var readCmd = new Command(TcHmiApplication.JoinPath("ADS.Config", "TIMEOUT"));
                                ErrorValue readResult = TcHmiApplication.AsyncHost.Execute(ref adminContext, ref readCmd);
                                if (readResult != ErrorValue.HMI_SUCCESS || readCmd.Result != ErrorValue.HMI_SUCCESS)
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILURE);
                                }
                                else
                                {
                                    var timeout = (TimeSpan)readCmd.ReadValue;
                                    var writeCmd = new Command(readCmd.Name)
                                    {
                                        WriteValue = new TimeSpan(timeout.Ticks * 2)
                                    };
                                    ErrorValue writeResult = TcHmiApplication.AsyncHost.Execute(ref adminContext, ref writeCmd);
                                    if (writeResult != ErrorValue.HMI_SUCCESS || writeCmd.Result != ErrorValue.HMI_SUCCESS)
                                    {
                                        command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILURE);
                                    }
                                    else
                                    {
                                        command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
                                    }
                                }
                            }
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

        private static Value FilterLocalRoutes(Value allRoutes)
        {
            var arr = new Value
            {
                Type = TcHmiSrv.Core.ValueType.Vector
            };
            if (allRoutes.Type == TcHmiSrv.Core.ValueType.Vector)
            {
                foreach (Value route in allRoutes)
                {
                    if (route.Type == TcHmiSrv.Core.ValueType.Struct ||
                        route.Type == TcHmiSrv.Core.ValueType.Map)
                    {
                        var name = route["name"];
                        if (name == "local" || name == "local_remote")
                        {
                            arr.Add(route);
                        }
                    }
                }
            }
            return arr;
        }
    }
}
