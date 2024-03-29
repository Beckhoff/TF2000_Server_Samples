﻿//-----------------------------------------------------------------------
// <copyright file="InterExtensionCommunication.cs" company="Beckhoff Automation GmbH & Co. KG">
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

namespace InterExtensionCommunication
{
    // ReSharper disable once UnusedType.Global
    public class InterExtensionCommunication : IServerExtension
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
            var adminContext = TcHmiApplication.Context;

            // handle all commands one by one
            foreach (var command in e.Commands)
            {
                try
                {
                    switch (command.Mapping)
                    {
                        case "ListLocalRoutes":
                        {
                            // invoke the function symbol of another extension

                            var cmd = new Command("ADS.ListRoutes");
                            var result = TcHmiApplication.AsyncHost.Execute(ref adminContext, ref cmd);

                            if (result != ErrorValue.HMI_SUCCESS || cmd.Result != ErrorValue.HMI_SUCCESS)
                            {
                                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failure);
                            }
                            else
                            {
                                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
                                command.ReadValue = FilterLocalRoutes(cmd.ReadValue);
                            }

                            break;
                        }
                        case "CheckClientLicenseAndListLocalRoutes":
                        {
                            // invoke multiple function symbols of one other extension simultaneously

                            var checkLicenseCmd = new Command("ADS.CheckLicense")
                            {
                                WriteValue =
                                    "37E5160D-987B-4640-888D-0F97727B53E2" // the UUID of the "TC3 HMI Clients" license
                            };
                            var listLocalRoutesCmd = new Command("ADS.ListRoutes");

                            var group = new CommandGroup("ADS") { checkLicenseCmd, listLocalRoutesCmd };
                            var result = TcHmiApplication.AsyncHost.Execute(ref adminContext, ref group);

                            if (result != ErrorValue.HMI_SUCCESS ||
                                group.Result != Convert.ToUInt32(ErrorValue.HMI_SUCCESS) ||
                                group[0].Result != ErrorValue.HMI_SUCCESS ||
                                group[1].Result != ErrorValue.HMI_SUCCESS)
                            {
                                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failure);
                            }
                            else
                            {
                                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
                                command.ReadValue = new Value();
                                var firstIsCheckLicense = group[0].Name == "ADS.CheckLicense";
                                command.ReadValue.Add("clientLicense",
                                    firstIsCheckLicense ? group[0].ReadValue : group[1].ReadValue);
                                command.ReadValue.Add("localRoutes", FilterLocalRoutes(
                                    firstIsCheckLicense ? group[1].ReadValue : group[0].ReadValue
                                ));
                            }

                            break;
                        }
                        case "DoubleAdsTimeout":
                        {
                            // read from and write into the configuration of another extension

                            command.ReadValue = new Value();

                            var readCmd = new Command(TcHmiApplication.JoinPath("ADS.Config", "TIMEOUT"));
                            var readResult = TcHmiApplication.AsyncHost.Execute(ref adminContext, ref readCmd);

                            if (readResult != ErrorValue.HMI_SUCCESS || readCmd.Result != ErrorValue.HMI_SUCCESS)
                            {
                                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failure);
                            }
                            else
                            {
                                var timeout = (TimeSpan)readCmd.ReadValue;
                                var writeCmd = new Command(readCmd.Name)
                                {
                                    WriteValue = new TimeSpan(timeout.Ticks * 2)
                                };
                                var writeResult =
                                    TcHmiApplication.AsyncHost.Execute(ref adminContext, ref writeCmd);

                                if (writeResult != ErrorValue.HMI_SUCCESS ||
                                    writeCmd.Result != ErrorValue.HMI_SUCCESS)
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failure);
                                }
                                else
                                {
                                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
                                }
                            }

                            break;
                        }
                        case "AddSymbolForAdsTimeout":
                        {
                            // extensions can create mapped symbols by calling the "AddSymbol" function symbol.
                            // the following command adds a new symbol called
                            // "InterExtensionCommunication.AdsTimeout" that allows all members of the
                            // "__SystemUsers" group to read and write the ADS timeout in the
                            // ADS extension configuration.
                            // to create a mapped symbol for a PLC variable, you just have to change the
                            // "MAPPING" parameter to something like "PLC1::MAIN::nCounter".

                            var addSymbolCommand = new Command("AddSymbol")
                            {
                                WriteValue = new Value
                                {
                                    { "NAME", TcHmiApplication.Context.Domain + ".AdsTimeout" },
                                    { "DOMAIN", "ADS" },
                                    { "MAPPING", "Config::TIMEOUT" },
                                    { "AUTOMAP", true },
                                    { "ACCESS", 3 },
                                    { "USERGROUPS", new Value { "__SystemUsers" } }
                                }
                            };
                            var addSymbolResult =
                                TcHmiApplication.AsyncHost.Execute(ref adminContext, ref addSymbolCommand);
                            if (addSymbolResult != ErrorValue.HMI_SUCCESS ||
                                addSymbolCommand.Result != ErrorValue.HMI_SUCCESS)
                            {
                                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failure);
                            }
                            else
                            {
                                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
                            }

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

        private static Value FilterLocalRoutes(Value allRoutes)
        {
            var arr = new Value { Type = ValueType.Vector };

            if (allRoutes.Type == ValueType.Vector)
            {
                foreach (Value route in allRoutes)
                {
                    if (route.Type == ValueType.Struct ||
                        route.Type == ValueType.Map)
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
