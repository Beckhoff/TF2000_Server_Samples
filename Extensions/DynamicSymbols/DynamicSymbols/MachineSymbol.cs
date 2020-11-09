//-----------------------------------------------------------------------
// <copyright file="MachineSymbol.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using DynamicSymbols.Machines;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TcHmiSrv.Core;
using TcHmiSrv.Core.Tools.DynamicSymbols;
using TcHmiSrv.Core.Tools.Json.Newtonsoft;
using TcHmiSrv.Core.Tools.Json.Newtonsoft.Converters;

namespace DynamicSymbols
{
    // 'Symbol' is the base class for dynamic symbols
    // Inherit from 'SymbolWithValue' or 'SymbolWithDirectValue' instead to automatically read and write sub-elements of the value of the current symbol
    internal class MachineSymbol : Symbol
    {
        public static TcHmiJSchemaGenerator CustomGenerator { get; } = CreateGenerator();

        public Machine Machine { get; }

        // Initialize the base 'Symbol' with the schema of the current machine type
        // Take a look at the 'Symbol' constructor overloads for additional configuration options
        public MachineSymbol(Machine machine) : base((machine is null) ? throw new ArgumentNullException(nameof(machine)) : CustomGenerator.Generate(machine.GetType()))
        {
            this.Machine = machine;
        }

        private static TcHmiJSchemaGenerator CreateGenerator()
        {
            // The default JSON schema generator does not contain a 'JSchemaGenerationProvider' for enumerations by default
            // You can implement a custom 'JSchemaGenerationProvider' for enumerations, create a 'JSchemaGenerationProvider' for a specific type by calling 'TcHmiSchemaGenerator.CreateEnumGenerationProvider' or use the 'TcHmiJSchemaGenerator.DefaultEnumGenerationProvider' for all enum types
            var generator = TcHmiJSchemaGenerator.DefaultGenerator;
            generator.GenerationProviders.Add(TcHmiJSchemaGenerator.DefaultEnumGenerationProvider);
            return generator;
        }

        // Read a value from the current machine
        protected override Value Read(Queue<string> elements)
        {
            // Convert the entire machine to JSON because no sub-element is requested
            if (elements.Count == 0)
                return TcHmiJsonSerializer.Deserialize(ValueJsonConverter.DefaultConverter, JsonConvert.SerializeObject(Machine));

            // Get the name if the requested sub-element
            var element = elements.Dequeue();

            // A sub-element of a sub-element cannot be requested because machines contain only top-level properties
            if (elements.Count > 0)
                throw new ArgumentException("Too many elements.", nameof(elements));

            switch (element)
            {
                case "Description":
                    return this.Machine.Description;

                case "State":
                    return this.Machine.State.ToString();

                case "IsWorking":
                    return this.Machine.IsWorking;

                case "HasError":
                    return this.Machine.IsWorking;

                case "CurrentTemperature":
                    return ((Furnace)this.Machine).CurrentTemperature;

                case "MaxTemperature":
                    return ((Furnace)this.Machine).MaxTemperature;

                case "CurrentPressure":
                    return ((Press)this.Machine).CurrentPressure;

                case "MaxPressure":
                    return ((Press)this.Machine).MaxPressure;

                case "CompressionTime":
                    return ((Press)this.Machine).CompressionTime;

                case "CurrentRotationsPerMinute":
                    return ((Saw)this.Machine).CurrentRotationsPerMinute;

                case "MaxRotationsPerMinute":
                    return ((Saw)this.Machine).MaxRotationsPerMinute;

                case "NumberOfPieces":
                    return ((Saw)this.Machine).NumberOfPieces;

                default:
                    throw new ArgumentException(string.Concat("Unknown element: ", element), nameof(elements));
            }
        }

        // Write a value to the current machine
        protected override Value Write(Queue<string> elements, Value value)
        {
            if (elements.Count == 0)
                throw new ArgumentException("Missing elements because the entire machine cannot be overwritten.", nameof(elements));

            var element = elements.Dequeue();

            if (elements.Count > 0)
                throw new ArgumentException("Too many elements.", nameof(elements));

            switch (element)
            {
                case "IsWorking":
                    this.Machine.IsWorking = value;
                    return this.Machine.IsWorking;

                case "HasError":
                    this.Machine.HasError = value;
                    return this.Machine.IsWorking;

                case "MaxTemperature":
                    {
                        var furnace = (Furnace)this.Machine;
                        furnace.MaxTemperature = value;
                        return furnace.MaxTemperature;
                    }

                case "MaxPressure":
                    {
                        var press = (Press)this.Machine;
                        press.MaxPressure = value;
                        return press.MaxPressure;
                    }

                case "CompressionTime":
                    {
                        var press = (Press)this.Machine;
                        press.CompressionTime = value;
                        return press.CompressionTime;
                    }

                case "MaxRotationsPerMinute":
                    {
                        var saw = (Saw)this.Machine;
                        saw.MaxRotationsPerMinute = value;
                        return saw.MaxRotationsPerMinute;
                    }

                case "NumberOfPieces":
                    {
                        var saw = (Saw)this.Machine;
                        saw.NumberOfPieces = value;
                        return saw.NumberOfPieces;
                    }

                default:
                    throw new ArgumentException(string.Concat("Element is read-only or unknown: ", element), nameof(elements));
            }
        }
    }
}
