//-----------------------------------------------------------------------
// <copyright file="StaticSymbols.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using StaticSymbols.Machines;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Json.Newtonsoft.Converters;
using TcHmiSrv.Core.Tools.Json.Newtonsoft;
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.StaticSymbols;
using TcHmiSrv.Core.Tools.TypeAttribute;
using TcHmiSrv.Core.Tools.Resolving.Handlers;

// Declare the default type of the server extension
[assembly: ServerExtensionType(typeof(StaticSymbols.StaticSymbols))]

// Export static symbols
[assembly: ExportSymbol("Furnace", ReadValue = typeof(Furnace), WriteValue = typeof(Furnace), AddSymbol = true)]
[assembly: ExportSymbol("Press", ReadValue = typeof(Press), WriteValue = typeof(Press), AddSymbol = true)]
[assembly: ExportSymbol("Saw", ReadValue = typeof(Saw), WriteValue = typeof(Saw), AddSymbol = true)]

namespace StaticSymbols
{
    // Represents the default type of the TwinCAT HMI server extension.
    public class StaticSymbols : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();

        private readonly IReadOnlyDictionary<string, Machine> _machines = new Dictionary<string, Machine>()
        {
            { "Furnace", new Furnace() },
            { "Press", new Press() },
            { "Saw", new Saw() },
        };

        // Initializes the TwinCAT HMI server extension.
        public ErrorValue Init()
        {
            try
            {
                // Add event handlers
                _requestListener.OnRequest += OnRequest;

                _ = TcHmiAsyncLogger.Send(Severity.Info, "MESSAGE_INIT");
                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                _ = TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INIT", ex.ToString());
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        // Called when a client requests a symbol from the domain of the TwinCAT HMI server extension.
        private void OnRequest(object sender, OnRequestEventArgs e)
        {
            var ret = ErrorValue.HMI_SUCCESS;
            var context = e.Context;
            var commands = e.Commands;

            try
            {
                foreach (var command in commands)
                {
                    var mapping = command.Mapping;

                    try
                    {
                        // Use the mapping to check which command is requested
                        if (_machines.TryGetValue(mapping, out var machine))
                        {
                            var elements = ResolveHandler.CreateQueue(command.Path);
                            var writeValue = command.WriteValue;

                            command.ReadValue = writeValue is null
                                ? ReadValue(machine, elements)
                                : WriteValue(machine, elements, writeValue);

                            // Subsymbols are handled by the above methods
                            command.SubsymbolHandled = true;
                        }
                        else
                        {
                            ret = ErrorValue.HMI_E_EXTENSION;
                        }

                        // if (ret != ErrorValue.HMI_SUCCESS)
                        //   Do something on error
                    }
                    catch (Exception ex)
                    {
                        command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Failure);
                        command.ResultString =
                            TcHmiAsyncLogger.Localize(context, "ERROR_CALL_COMMAND", mapping, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TcHmiException(ex.ToString(),
                    ret == ErrorValue.HMI_SUCCESS ? ErrorValue.HMI_E_EXTENSION : ret);
            }
        }

        // Read a value from the specified machine
        private static Value ReadValue(Machine machine, Queue<string> elements)
        {
            if (machine is null)
            {
                throw new ArgumentNullException(nameof(machine));
            }

            if (elements is null)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            // Convert the entire machine to JSON because no sub-element is requested
            if (elements.Count == 0)
            {
                return TcHmiJsonSerializer.Deserialize(ValueJsonConverter.DefaultConverter, JsonConvert.SerializeObject(machine));
            }

            // Get the name if the requested sub-element
            var element = elements.Dequeue();

            // A sub-element of a sub-element cannot be requested because machines contain only top-level properties
            if (elements.Count > 0)
            {
                throw new ArgumentException("Too many elements.", nameof(elements));
            }

            return element switch
            {
                "Description" => machine.Description,
                "State" => machine.State.ToString(),
                "IsWorking" => machine.IsWorking,
                "HasError" => machine.IsWorking,
                "CurrentTemperature" => ((Furnace)machine).CurrentTemperature,
                "MaxTemperature" => ((Furnace)machine).MaxTemperature,
                "CurrentPressure" => ((Press)machine).CurrentPressure,
                "MaxPressure" => ((Press)machine).MaxPressure,
                "CompressionTime" => ((Press)machine).CompressionTime,
                "CurrentRotationsPerMinute" => ((Saw)machine).CurrentRotationsPerMinute,
                "MaxRotationsPerMinute" => ((Saw)machine).MaxRotationsPerMinute,
                "NumberOfPieces" => ((Saw)machine).NumberOfPieces,
                _ => throw new ArgumentException(string.Concat("Unknown element: ", element), nameof(elements))
            };
        }

        // Write a value to the current machine
        private static Value WriteValue(Machine machine, Queue<string> elements, Value value)
        {
            if (machine is null)
            {
                throw new ArgumentNullException(nameof(machine));
            }

            if (elements is null)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            if (elements.Count == 0)
            {
                throw new ArgumentException("Missing elements because the entire machine cannot be overwritten.", nameof(elements));
            }

            var element = elements.Dequeue();

            if (elements.Count > 0)
            {
                throw new ArgumentException("Too many elements.", nameof(elements));
            }

            switch (element)
            {
                case "IsWorking":
                    machine.IsWorking = value;
                    return machine.IsWorking;

                case "HasError":
                    machine.HasError = value;
                    return machine.IsWorking;

                case "MaxTemperature":
                    {
                        var furnace = (Furnace)machine;
                        furnace.MaxTemperature = value;
                        return furnace.MaxTemperature;
                    }

                case "MaxPressure":
                    {
                        var press = (Press)machine;
                        press.MaxPressure = value;
                        return press.MaxPressure;
                    }

                case "CompressionTime":
                    {
                        var press = (Press)machine;
                        press.CompressionTime = value;
                        return press.CompressionTime;
                    }

                case "MaxRotationsPerMinute":
                    {
                        var saw = (Saw)machine;
                        saw.MaxRotationsPerMinute = value;
                        return saw.MaxRotationsPerMinute;
                    }

                case "NumberOfPieces":
                    {
                        var saw = (Saw)machine;
                        saw.NumberOfPieces = value;
                        return saw.NumberOfPieces;
                    }

                default:
                    throw new ArgumentException(string.Concat("Element is read-only or unknown: ", element), nameof(elements));
            }
        }
    }
}
