//-----------------------------------------------------------------------
// <copyright file="ProtectedSymbol.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using Integrative.Encryption;
using TcHmiSrv.Core;
using TcHmiSrv.Core.Extensions;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;

namespace ProtectedSymbol
{
    // Represents the default type of the TwinCAT HMI server extension.
    public class ProtectedSymbol : IServerExtension
    {
        private readonly RequestListener requestListener = new RequestListener();
        private readonly ExportListener exportListener = new ExportListener();
        private readonly ConfigListener configListener = new ConfigListener();

        // A random entropy to pass to the the protection function. This is used so only this application can unprotect the data.
        private static readonly byte[] s_entropy = {
            40, 149, 96, 43, 138, 32, 77, 69, 9, 174, 237, 132, 96, 180, 54, 96
        };

        // A list of symbols that should be protected
        private static readonly string[] protectedSymbols = { "protectedSymbol", "nestedSymbol::protectedChild" };

        // Called after the TwinCAT HMI server loaded the server extension. Here, the listeners are registered.
        public ErrorValue Init()
        {
            this.configListener.BeforeChange += this.BeforeChange;
            this.exportListener.BeforeExport += this.BeforeExport;
            this.exportListener.BeforeImport += this.BeforeImport;
            this.requestListener.OnRequest += this.OnRequest;
            return ErrorValue.HMI_SUCCESS;
        }

        // Protect a string using the Windows Data Protection API
        private void ProtectString(Value value)
        {
            string s = value.GetString();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(s); 

            // Encrypt the byte array using the credentials of the current windows user
            buffer = CrossProtect.Protect(buffer, s_entropy, DataProtectionScope.CurrentUser);
            s = Convert.ToBase64String(buffer);

            // Write the encrypted string back to the value
            value.SetValue(s);
        }

        // Unprotect a string using the Windows Data Protection API
        private void UnprotectString(Value value)
        {
            string s = value.GetString();
            byte[] buffer = Convert.FromBase64String(s);

            // decrypt the byte array using the credentials of the current windows user
            buffer = CrossProtect.Unprotect(buffer, s_entropy, DataProtectionScope.CurrentUser);
            s = System.Text.Encoding.UTF8.GetString(buffer);

            // Write the decrypted string back to the value
            value.SetValue(s);
        }

        // Called when a client requests a symbol from the domain of the TwinCAT HMI server extension.
        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            try
            {
                e.Commands.Result = ProtectedSymbolErrorValue.ProtectedSymbolSuccess;

                foreach (Command command in e.Commands)
                {
                    try
                    {
                        // Use the mapping to check which command is requested
                        switch (command.Mapping)
                        {
                            case "GetProtectedSymbol":
                                GetProtectedSymbol(e.Context, command);
                                break;

                            default:
                                command.ExtensionResult = ProtectedSymbolErrorValue.ProtectedSymbolFail;
                                command.ResultString = "Unknown command '" + command.Mapping + "' not handled.";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        command.ExtensionResult = ProtectedSymbolErrorValue.ProtectedSymbolFail;
                        command.ResultString = "Calling command '" + command.Mapping + "' failed! Additional information: " + ex.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TcHmiException(ex.ToString(), ErrorValue.HMI_E_EXTENSION);
            }
        }

        // Called before the config is exported
        private void BeforeExport(object sender, TcHmiSrv.Core.Listeners.ExportListenerEventArgs.BeforeExportEventArgs e)
        {
            foreach (string protectedSymbol in protectedSymbols)
            {
                // Split the symbol path into its parts
                Queue<string> path = new Queue<string>(protectedSymbol.Split("::"));

                // If the protected symbol is about to be exported, decrypt it first
                if (e.Value.TryResolveBy(path, out Value subValue))
                {
                    UnprotectString(subValue);
                }
            }
        }

        private void BeforeImport(object sender, TcHmiSrv.Core.Listeners.ExportListenerEventArgs.BeforeImportEventArgs e)
        {
            foreach (string protectedSymbol in protectedSymbols)
            {
                // Split the symbol path into its parts
                Queue<string> path = new Queue<string>(protectedSymbol.Split("::"));

                // If the protected symbol is about to be imported, encrypt it first
                if (e.Value.TryResolveBy(path, out Value subValue))
                {
                    ProtectString(subValue);
                }
            }
        }

        private void BeforeChange(object sender, TcHmiSrv.Core.Listeners.ConfigListenerEventArgs.BeforeChangeEventArgs e)
        {
            // When a new value gets written to a protected symbol, encrypt it first
            if (protectedSymbols.Contains(e.Path))
            {
                ProtectString(e.Value);
            }
        }

        private void GetProtectedSymbol(Context ctx, Command cmd)
        {
            // Check if the symbol is protected
            if (protectedSymbols.Contains(cmd.WriteValue.ToString()))
            {
                // Decrypt the symbol value and return it
                Value val = TcHmiApplication.AsyncHost.GetConfigValue(ctx, cmd.WriteValue);
                UnprotectString(val);
                cmd.ReadValue = val;
            }
            else
            {
                cmd.ExtensionResult = (uint)ErrorValue.HMI_E_INVALID_SYMBOL;
            }
        }
    }
}
