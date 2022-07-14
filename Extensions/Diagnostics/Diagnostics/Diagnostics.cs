//-----------------------------------------------------------------------
// <copyright file="Diagnostics.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using TcHmiSrv.Core;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;

namespace Diagnostics
{
    // ReSharper disable once UnusedType.Global
    public class Diagnostics : IServerExtension
    {
        private readonly PerformanceCounter _cpuUsage =
            new PerformanceCounter("Processor", "% Processor Time", "_Total");

        private readonly RequestListener _requestListener = new RequestListener();
        private DateTime _startup;

        public ErrorValue Init()
        {
            _startup = DateTime.Now;

            // add event handlers
            _requestListener.OnRequest += OnRequest;

            return ErrorValue.HMI_SUCCESS;
        }

        private Value CollectDiagnosticsData()
        {
            return new Value { { "cpuUsage", _cpuUsage.NextValue() }, { "sinceStartup", DateTime.Now - _startup } };
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
                        case "Diagnostics":
                        {
                            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
                            command.ReadValue = CollectDiagnosticsData();
                        }
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
    }
}
