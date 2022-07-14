//-----------------------------------------------------------------------
// <copyright file="DynamicSymbols.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using DynamicSymbols.Machines;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Listeners.ShutdownListenerEventArgs;
using TcHmiSrv.Core.Tools.DynamicSymbols;
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.TypeAttribute;
using ValueType = TcHmiSrv.Core.ValueType;

// Declare the default type of the server extension
[assembly: ServerExtensionType(typeof(DynamicSymbols.DynamicSymbols))]

namespace DynamicSymbols
{
    // Represents the default type of the TwinCAT HMI server extension.
    public class DynamicSymbols : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly ShutdownListener _shutdownListener = new ShutdownListener();
        private string _machineHall;

        private DynamicSymbolsProvider _provider;

        // Initializes the TwinCAT HMI server extension.
        public ErrorValue Init()
        {
            try
            {
                // Add event handlers
                _requestListener.OnRequest += OnRequest;
                _shutdownListener.OnShutdown += OnShutdown;

                _machineHall = Path.Combine(TcHmiApplication.Path, "MachineHall");

                // Check if the machines have already been configured
                if (Directory.Exists(_machineHall))
                {
                    var machines = new Dictionary<string, Symbol>();

                    // Load configured machines
                    foreach (var file in Directory.EnumerateFiles(_machineHall, "*.txt"))
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        var lines = File.ReadAllLines(file);
                        var contents = new Dictionary<string, string>(lines.Length);

                        foreach (var line in lines)
                        {
                            var elements = line.Split(':');
                            contents.Add(elements[0], elements[1]);
                        }

                        var type = contents["Type"];
                        Machine machine;

                        switch (type)
                        {
                            case "Furnace":
                            {
                                var furnace = new Furnace { MaxTemperature = int.Parse(contents["MaxTemperature"]) };
                                machine = furnace;
                                break;
                            }

                            case "Press":
                            {
                                var press = new Press
                                {
                                    MaxPressure = double.Parse(contents["MaxPressure"]),
                                    CompressionTime = TimeSpan.Parse(contents["CompressionTime"])
                                };
                                machine = press;
                                break;
                            }

                            case "Saw":
                            {
                                var saw = new Saw
                                {
                                    MaxRotationsPerMinute = uint.Parse(contents["MaxRotationsPerMinute"]),
                                    NumberOfPieces = uint.Parse(contents["NumberOfPieces"])
                                };
                                machine = saw;
                                break;
                            }

                            default:
                                throw new TcHmiException(string.Concat("Unknown machine type: ", type),
                                    ErrorValue.HMI_E_EXTENSION);
                        }

                        machine.HasError = bool.Parse(contents["HasError"]);
                        machine.IsWorking = bool.Parse(contents["IsWorking"]);
                        machines.Add(name, new MachineSymbol(machine));
                    }

                    // Create a new 'DynamicSymbolsProvider' from the existing machine configurations
                    _provider = new DynamicSymbolsProvider(machines);

                    // Remove machine configurations (updated machine configurations will be saved when shutting down the server extension)
                    Directory.Delete(_machineHall, true);
                }
                else
                    // Create a new empty 'DynamicSymbolsProvider'
                {
                    _provider = new DynamicSymbolsProvider();
                }

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
                // Handle commands 'ListSymbols', 'GetDefinitions', 'GetSchema', read and write operations to the dynamic symbols by the 'DynamicSymbolsProvider'
                // Don't forget to add symbols 'ListSymbols', 'GetDefinitions', 'GetSchema' to your *.Config.json!
                foreach (var command in _provider.HandleCommands(commands))
                {
                    var mapping = command.Mapping;

                    try
                    {
                        // Use the mapping to check which command is requested
                        switch (mapping)
                        {
                            case "AddMachine":
                            {
                                var writeValue = command.WriteValue;

                                if (writeValue is null)
                                {
                                    throw new TcHmiException("Write value cannot be null.",
                                        ErrorValue.HMI_E_INVALID_PARAMETER);
                                }

                                if (!writeValue.IsMapOrStruct)
                                {
                                    throw new TcHmiException(
                                        string.Concat("Unexpected type for write value: ", writeValue.Type),
                                        ErrorValue.HMI_E_TYPE_MISMATCH);
                                }

                                // Add a new machine to the dynamic symbols provider
                                _provider.Add(writeValue["name"],
                                    new MachineSymbol(CreateMachine(writeValue["type"])));
                                break;
                            }

                            case "RemoveMachine":
                            {
                                var writeValue = command.WriteValue;

                                if (writeValue is null)
                                {
                                    throw new TcHmiException("Write value cannot be null.",
                                        ErrorValue.HMI_E_INVALID_PARAMETER);
                                }

                                var type = writeValue.Type;

                                if (type != ValueType.String)
                                {
                                    throw new TcHmiException(
                                        string.Concat("Unexpected type for write value: ", writeValue.Type),
                                        ErrorValue.HMI_E_TYPE_MISMATCH);
                                }

                                command.ReadValue = _provider.Remove(writeValue);
                                break;
                            }

                            default:
                                ret = ErrorValue.HMI_E_EXTENSION;
                                break;
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

        private void OnShutdown(object sender, OnShutdownEventArgs e)
        {
            // Save updated machine configurations
            foreach (var symbol in _provider)
            {
                if (!Directory.Exists(_machineHall))
                {
                    _ = Directory.CreateDirectory(_machineHall);
                }

                var machineSymbol = (MachineSymbol)symbol.Value;
                var machine = machineSymbol.Machine;
                var machineType = machine.GetType();
                var contents = new List<string>
                {
                    string.Concat("Type:", machineType.Name),
                    string.Concat("IsWorking:", machine.IsWorking),
                    string.Concat("HasError:", machine.HasError)
                };

                if (machine is Furnace furnace)
                {
                    contents.Add(string.Concat("MaxTemperature:", furnace.MaxTemperature));
                }
                else if (machine is Press press)
                {
                    contents.Add(string.Concat("MaxPressure:", press.MaxPressure));
                    contents.Add(string.Concat("CompressionTime:", press.CompressionTime));
                }
                else if (machine is Saw saw)
                {
                    contents.Add(string.Concat("MaxRotationsPerMinute:", saw.MaxRotationsPerMinute));
                    contents.Add(string.Concat("NumberOfPieces:", saw.NumberOfPieces));
                }

                File.WriteAllLines(Path.Combine(_machineHall, string.Concat(symbol.Key, ".txt")), contents);
            }
        }

        private static Machine CreateMachine(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.ToLower() switch
            {
                "furnace" => new Furnace(),
                "press" => new Press(),
                "saw" => new Saw(),
                _ => throw new ArgumentException(string.Concat("Unknown machine type: ", type), nameof(type))
            };
        }
    }
}
