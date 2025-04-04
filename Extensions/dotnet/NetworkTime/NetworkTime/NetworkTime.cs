//-----------------------------------------------------------------------
// <copyright file="NetworkTime.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Sockets;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;

namespace NetworkTime
{
    // ReSharper disable once UnusedType.Global
    public class NetworkTime : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();

        public ErrorValue Init()
        {
            // add event handlers
            _requestListener.OnRequest += OnRequest;

            return ErrorValue.HMI_SUCCESS;
        }

        private static void Now(Command command)
        {
            // read configured time server from the extension's configuration.
            // we don't use the client context because the extension configuration
            // is usually only accessible to administrators.
            string ntpServer = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "ntpServer");

            var now = GetNetworkTime(ntpServer);

            if (now.HasValue)
            {
                command.ReadValue = new Value(now.Value);
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
            }
            else
            {
                command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.NtpServerNotAvailable);
            }
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
                        case "Now":
                            Now(command);
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

        private static DateTime? GetNetworkTime(string ntpServer)
        {
            var message = new byte[48]; // see RFC2030 for more information
            message[0] = 0x1B; // LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (client mode)

            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(new IPEndPoint(Dns.GetHostEntry(ntpServer).AddressList[0], 123));
                socket.ReceiveTimeout = 5000;
                _ = socket.Send(message);
                _ = socket.Receive(message);
            }
            catch
            {
                return null; // NTP server not available
            }

            const byte TransitTimestampOffset = 40;
            var intPart = BitConverter.ToUInt32(message, TransitTimestampOffset);
            var fractionPart = BitConverter.ToUInt32(message, TransitTimestampOffset + 4);

            var milliseconds = ((ulong)SwapEndianness(intPart) * 1000) +
                               ((ulong)SwapEndianness(fractionPart) * 1000 / 0x100000000L);
            return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)milliseconds)
                .ToLocalTime();
        }

        private static uint SwapEndianness(uint x)
        {
            return ((x & 0x000000ff) << 24) +
                   ((x & 0x0000ff00) << 8) +
                   ((x & 0x00ff0000) >> 8) +
                   ((x & 0xff000000) >> 24);
        }
    }
}
