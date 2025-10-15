//-----------------------------------------------------------------------
// <copyright file="CSharpRandomValue.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.ConfigListenerEventArgs;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.Settings;
using ValueType = TcHmiSrv.Core.ValueType;

namespace CSharpRandomValue
{
    // represents the default type of the TwinCAT HMI server extension
    // ReSharper disable once UnusedType.Global
    public class CSharpRandomValue : IServerExtension
    {
        private const string CfgMaxRandom = "maxRandom";
        private readonly Random _rand = new Random();
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly ConfigListener _configListener = new ConfigListener();

        // initializes the TwinCAT HMI server extension
        public ErrorValue Init()
        {
            try
            {
                RegisterListeners();
                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                _ = Send(Severity.Error, "errorInit", ex.ToString());
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        protected virtual void RegisterListeners()
        {
            // add event handlers
            _requestListener.OnRequest += OnRequest;
            _configListener.BeforeChange += BeforeChange;

            var filter = new ConfigListenerSettingsFilter(ConfigChangeType.BeforeChange, new string[] { CfgMaxRandom });
            var settings = new ConfigListenerSettings();
            settings.Filters.Add(filter);
            TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _configListener, settings);
        }

        protected virtual Value GetConfigValue(string path)
        {
            return TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, path);
        }

        protected virtual string Localize(Context context, string name, params string[] parameters)
        {
            return TcHmiAsyncLogger.Localize(context, name, parameters);
        }

        protected virtual ErrorValue Send(Severity severity, string name, params string[] parameters)
        {
            return TcHmiAsyncLogger.Send(severity, name, parameters);
        }

        public void OnRequest(Context context, CommandGroup commands)
        {
            // handle all commands one by one
            foreach (var command in commands)
            {
                try
                {
                    // use the mapping to check which command is requested
                    switch (command.Mapping)
                    {
                        case "RandomValue":
                            NextRandomValue(command);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // ignore exceptions and continue processing the other commands in the group
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.InternalError);
                    command.ResultString = Localize(context, "errorCallCommand", command.Mapping, ex.ToString());
                }
            }
        }

        public void BeforeChange(Context _1, string path, Value value, Value _2)
        {
            if (string.Equals(path, CfgMaxRandom))
            {
                if (value is null || value.Type != ValueType.Int32 || value.GetInt32() < 0)
                {
                    throw new TcHmiException("Max random value must not be less than zero.",
                        ErrorValue.HMI_E_INVALID_PARAMETER);
                }
            }
        }

        // called when a client requests a symbol from the domain of the TwinCAT HMI server extension
        private void OnRequest(object sender, OnRequestEventArgs e)
        {
            OnRequest(e.Context, e.Commands);
        }

        // called before a configuration value is changed
        private void BeforeChange(object sender, BeforeChangeEventArgs e)
        {
            BeforeChange(e.Context, e.Path, e.Value, e.Reference);
        }

        // generates a random value and writes it to the read value of the specified command
        private void NextRandomValue(Command command)
        {
            command.ReadValue = _rand.Next(GetConfigValue(CfgMaxRandom));
            command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.Success);
        }
    }
}
