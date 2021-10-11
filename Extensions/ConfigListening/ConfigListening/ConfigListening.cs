//-----------------------------------------------------------------------
// <copyright file="ConfigListening.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.ConfigListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;
using TcHmiSrv.Core.Tools.Settings;

namespace ConfigListening
{
    public class ConfigListening : IServerExtension
    {
        private readonly ConfigListener _configListener = new ConfigListener();
        private const int _configHintId = 1;

        public ErrorValue Init()
        {
            // add event handlers
            _configListener.OnChange += OnChange;
            _configListener.BeforeChange += BeforeChange;
            _configListener.BeforeRename += BeforeRename;
            _configListener.OnDelete += OnDelete;

            // set up the config listener:
            //   1. we need to specify which handlers we're using
            //   2. we need to specify which parts of the configuration we're interested in. "*" enables the handlers for the whole configuration.
            // config listeners can affect performance and should be limited to the variables that are actually used in the handlers.
            var settings = new ConfigListenerSettings();
            var filterPalindromes = new ConfigListenerSettingsFilter(
                ConfigChangeType.OnChange |
                ConfigChangeType.BeforeChange |
                ConfigChangeType.OnDelete,
                new string[] { "palindromes[*" }
            );
            var filterAnagrams = new ConfigListenerSettingsFilter(
                ConfigChangeType.BeforeChange |
                ConfigChangeType.BeforeRename,
                new string[] { "anagrams::*" }
            );
            settings.Filters.Add(filterPalindromes);
            settings.Filters.Add(filterAnagrams);
            TcHmiApplication.AsyncHost.RegisterListener(TcHmiApplication.Context, _configListener, settings);

            return ErrorValue.HMI_SUCCESS;
        }

        private void OnChange(object sender, OnChangeEventArgs e)
        {
            // called after a configuration change is applied. here, we can react to the change.
            // examples:
            //   - if a third palindrome was added, this handler is called for the paths "", "palindromes", and "palindromes[2]"
            //   - if an anagram called "tea" was added, this handler is called for the paths "", "anagrams", and "anagrams::tea"

            if (e.Path.StartsWith("palindromes"))
            {
                UpdateConfigurationHint();
            }
        }

        private void BeforeChange(object sender, BeforeChangeEventArgs e)
        {
            // called before a configuration change is applied. here, we can reject changes.
            // examples:
            //   - if a third palindrome is added, this handler is called for the paths "", "palindromes", and "palindromes[2]"
            //   - if an anagram called "tea" is added, this handler is called for the paths "", "anagrams", and "anagrams::tea"

            if (e.Path.StartsWith("palindromes["))
            {
                // reject invalid palindromes
                if (PalindromeValidator.Validate(e.Value) == PalindromeType.None)
                {
                    throw new TcHmiException("Change attempt rejected. '" + e.Value + "' is not palindrome.", ErrorValue.HMI_E_INVALID_FIELD);
                }
            }
            else
            {
                var start = "anagrams::";
                if (e.Path.StartsWith(start))
                {
                    var text = e.Path.Substring(start.Length);
                    if (!AnagramValidator.Validate(text, e.Value))
                    {
                        throw new TcHmiException("Change attempt rejected. '" + e.Value + "' is not an anagram of '" + text + "'.", ErrorValue.HMI_E_INVALID_FIELD);
                    }
                }
            }
        }

        private void BeforeRename(object sender, BeforeRenameEventArgs e)
        {
            // the HMI server's "Rename" symbol can be used to change the name of properties in the extension configuration.
            // this handler is called before a property is renamed. here, we can reject rename attempts.
            // to react to successful rename attempts, you can use the 'OnRename' handler.
            //
            // most extensions don't need to implement the 'BeforeRename' and 'OnRename' handlers,
            // because a rename attempt also triggers the 'BeforeChange' and 'OnChange' handlers.
            // in this sample, we use the 'BeforeRename' handler to return a more accurate error message.
            //
            // example:
            //   - if an anagram called "tea" is renamed to "eat", the following 'Before*' handlers are called:
            //       1. BeforeRename (NewPath="anagrams::eat", OldPath="anagrams::tea")
            //       2. BeforeChange (Path="anagrams::tea")
            //       3. BeforeChange (Path="anagrams")
            //       4. BeforeChange (Path="")

            var start = "anagrams::";
            if (e.NewPath.StartsWith(start))
            {
                var anagram = (string)TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, e.OldPath);
                var newText = e.NewPath.Substring(start.Length);
                if (!AnagramValidator.Validate(newText, anagram))
                {
                    throw new TcHmiException("Rename attempt rejected. '" + newText + "' is not an anagram of '" + anagram + "'.", ErrorValue.HMI_E_INVALID_FIELD);
                }
            }
        }

        private void OnDelete(object sender, OnDeleteEventArgs e)
        {
            // called after data was deleted from the configration.
            // to prevent deletion, you can use the 'BeforeDelete' handler.
            // examples:
            //   - if the first palindrome was deleted, this handler is called for "", "palindromes", and "palindromes[0]"
            //   - if an anagram called "tea" was deleted, this handler is called for "", "anagrams", and "anagrams::tea"

            if (e.Path.StartsWith("palindromes"))
            {
                UpdateConfigurationHint();
            }
        }

        private void UpdateConfigurationHint()
        {
            // this extension raises a configuration hint if at least one of the configured palindromes
            // consists of more than one word. the hint is, for example, displayed on the extension's configuration page.

            bool allAreCharacterUnitPalindromes = true;
            foreach (Value s in TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "palindromes"))
            {
                if (PalindromeValidator.Validate(s) != PalindromeType.CharacterUnit)
                {
                    allAreCharacterUnitPalindromes = false;
                    break;
                }
            }

            var serverContext = TcHmiApplication.Context;
            serverContext.Domain = "TcHmiSrv";

            var ev = new Event(serverContext, "CONFIGURATION_HINT");
            ev.TimeReceived = DateTime.UtcNow;
            var payload = new Alarm(TcHmiApplication.Context, "strictPalindromeValidationFailed");
            payload.Severity = Severity.Warning;
            payload.Id = _configHintId;
            payload.ConfirmationState = allAreCharacterUnitPalindromes ? AlarmConfirmationState.Reset : AlarmConfirmationState.Wait;
            ev.Payload = payload;

            TcHmiApplication.AsyncHost.Send(serverContext, ref ev);
        }
    }
}
