//-----------------------------------------------------------------------
// <copyright file="DynamicSymbols.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using DynamicSymbols.Machines;
using System;
using System.Collections.Generic;
using System.IO;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.DynamicSymbols;
using TcHmiSrv.Core.Tools.Management;
using ValueType = TcHmiSrv.Core.ValueType;

// Declare the default type of the server extension
[assembly: TcHmiSrv.Core.Tools.TypeAttribute.ServerExtensionType(typeof(DynamicSymbols.DynamicSymbols))]

namespace DynamicSymbols
{
    // Represents the default type of the TwinCAT HMI server extension.
    public class DynamicSymbols : IServerExtension
    {
        private readonly RequestListener requestListener = new RequestListener();
        private readonly ShutdownListener shutdownListener = new ShutdownListener();

        private DynamicSymbolsProvider provider = null;
        private string machineHall = null;

        // Initializes the TwinCAT HMI server extension.
        public ErrorValue Init()
        {
            try
            {
                // Add event handlers
                this.requestListener.OnRequest += this.OnRequest;
                this.shutdownListener.OnShutdown += this.OnShutdown;

                this.machineHall = Path.Combine(TcHmiApplication.Path, "MachineHall");

                // Check if the machines have already been configured
                if (Directory.Exists(this.machineHall))
                {
                    var machines = new Dictionary<string, Symbol>();

                    // Load configured machines
                    foreach (var file in Directory.EnumerateFiles(this.machineHall, "*.txt"))
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
                                    var furnace = new Furnace
                                    {
                                        MaxTemperature = int.Parse(contents["MaxTemperature"])
                                    };
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
                                throw new TcHmiException(string.Concat("Unknown machine type: ", type), ErrorValue.HMI_E_EXTENSION);
                        }

                        machine.HasError = bool.Parse(contents["HasError"]);
                        machine.IsWorking = bool.Parse(contents["IsWorking"]);
                        machines.Add(name, new MachineSymbol(machine));
                    }

                    // Create a new 'DynamicSymbolsProvider' from the existing machine configurations
                    this.provider = new DynamicSymbolsProvider(machines);

                    // Remove machine configurations (updated machine configurations will be saved when shutting down the server extension)
                    Directory.Delete(this.machineHall, true);
                }
                else
                    // Create a new empty 'DynamicSymbolsProvider'
                    this.provider = new DynamicSymbolsProvider();

                TcHmiAsyncLogger.Send(Severity.Info, "MESSAGE_INIT");
                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INIT", ex.ToString());
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        // Called when a client requests a symbol from the domain of the TwinCAT HMI server extension.
        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            ErrorValue ret = ErrorValue.HMI_SUCCESS;
            Context context = e.Context;
            CommandGroup commands = e.Commands;

            try
            {
                string mapping = string.Empty;

                // Handle commands 'ListSymbols', 'GetDefinitions', 'GetSchema', read and write operations to the dynamic symbols by the 'DynamicSymbolsProvider'
                // Don't forget to add symbols 'ListSymbols', 'GetDefinitions', 'GetSchema' to your *.Config.json!
                foreach (Command command in this.provider.HandleCommands(commands))
                {
                    mapping = command.Mapping;

                    try
                    {
                        // Use the mapping to check which command is requested
                        switch (mapping)
                        {
                            case "AddMachine":
                                {
                                    var writeValue = command.WriteValue;

                                    if (writeValue is null)
                                        throw new TcHmiException("Write value cannot be null.", ErrorValue.HMI_E_INVALID_PARAMETER);

                                    if (!writeValue.IsMapOrStruct)
                                        throw new TcHmiException(string.Concat("Unexpected type for write value: ", writeValue.Type), ErrorValue.HMI_E_TYPE_MISMATCH);

                                    // Add a new machine to the dynamic symbols provider
                                    this.provider.Add(writeValue["name"], new MachineSymbol(CreateMachine(writeValue["type"])));
                                    break;
                                }

                            case "RemoveMachine":
                                {
                                    var writeValue = command.WriteValue;

                                    if (writeValue is null)
                                        throw new TcHmiException("Write value cannot be null.", ErrorValue.HMI_E_INVALID_PARAMETER);

                                    var type = writeValue.Type;

                                    if (type != ValueType.String)
                                        throw new TcHmiException(string.Concat("Unexpected type for write value: ", writeValue.Type), ErrorValue.HMI_E_TYPE_MISMATCH);

                                    command.ReadValue = this.provider.Remove(writeValue);
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
                        command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.FAILURE);
                        command.ResultString = TcHmiAsyncLogger.Localize(context, "ERROR_CALL_COMMAND", new string[] { mapping, ex.ToString() });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TcHmiException(ex.ToString(), (ret == ErrorValue.HMI_SUCCESS) ? ErrorValue.HMI_E_EXTENSION : ret);
            }
        }

        private void OnShutdown(object sender, TcHmiSrv.Core.Listeners.ShutdownListenerEventArgs.OnShutdownEventArgs e)
        {
            // Save updated machine configurations
            foreach (var symbol in this.provider)
            {
                if (!Directory.Exists(this.machineHall))
                    Directory.CreateDirectory(this.machineHall);

                var machineSymbol = (MachineSymbol)symbol.Value;
                var machine = machineSymbol.Machine;
                var machineType = machine.GetType();
                var contents = new List<string>
                {
                    string.Concat("Type:", machineType.Name),
                    string.Concat("IsWorking:", machine.IsWorking),
                    string.Concat("HasError:", machine.HasError),
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

                File.WriteAllLines(Path.Combine(this.machineHall, string.Concat(symbol.Key, ".txt")), contents);
            };
        }

        private static Machine CreateMachine(string type)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));

            switch (type.ToLower())
            {
                case "furnace":
                    return new Furnace();

                case "press":
                    return new Press();

                case "saw":
                    return new Saw();

                default:
                    throw new ArgumentException(string.Concat("Unknown machine type: ", type), nameof(type));
            }
        }
    }
}
