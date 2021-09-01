//-----------------------------------------------------------------------
// <copyright file="Diagnostics.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Extensions;
using TcHmiSrv.Core.Tools.Management;

namespace Diagnostics
{
    public class Diagnostics : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly PerformanceCounter _cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private DateTime _startup;

        public ErrorValue Init()
        {
            _startup = DateTime.Now;

            // add event handlers
            _requestListener.OnRequest += OnRequest;

            return ErrorValue.HMI_SUCCESS;
        }

        private Value CollectDiagnosticsData(Command command)
        {
            var diagObject = new Value
            {
                { "cpuUsage", Math.Truncate(_cpuUsage.NextValue()) },
                { "sinceStartup", DateTime.Now - _startup }
            };

            // ResolveBy allows subsymbols of objects to be accessed directly
            return diagObject.ResolveBy(command.Path);
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
                        case "Diagnostics":
                            {
                            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.SUCCESS);
                            command.ReadValue = CollectDiagnosticsData(command);
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
    }
}
