//-----------------------------------------------------------------------
// <copyright file="EventSystem.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.AlarmProviderListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;

namespace EventSystem
{

    public class EventSystem : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly EventLocalizationListener _eventLocalizationListener = new EventLocalizationListener();
        private readonly AlarmProviderListener _alarmProviderListener = new AlarmProviderListener();
        private int _dynamicallyLocalizedMessagesCount = 0;
        private int _nextAlarmId = 1;
        private readonly Dictionary<int, Event> _activeAlarmsById = new Dictionary<int, Event>();
        private readonly Dictionary<int, Event> _inactiveAlarmsById = new Dictionary<int, Event>();
        private readonly object _alarmsLock = new object();

        public ErrorValue Init()
        {
            Context adminContext = TcHmiApplication.Context;

            // add event handlers
            _requestListener.OnRequest += OnRequest;
            _eventLocalizationListener.OnRequest += OnRequest;
            _alarmProviderListener.OnConfirm += OnConfirm;
            _alarmProviderListener.OnInitialListEvents += OnInitialListEvents;

            // you can send messages to the extension host. they will be forwarded to the
            // default logger extension and stored in a database. they will then appear in
            // the result of the 'ListEvents' function symbol. additionally, all clients
            // that opened an event subscription via the 'SubscribeEvents' function symbol
            // will automatically be notified about the new message.
            TcHmiApplication.AsyncHost.Send(adminContext, new Message()
            {
                Severity = Severity.Info,
                Domain = TcHmiApplication.Context.Domain,
                Name = "FIRST_MESSAGE",
                TimeRaised = DateTime.Now,
            });

            // create our first alarm. sending it here is not necessary,
            // because 'OnInitialListEvents' will be called immediately after 'Init'.
            RaiseAlarm(send: false);

            return ErrorValue.HMI_SUCCESS;
        }

        public void RaiseAlarm(bool send = true)
        {
            int alarmId = 0;
            lock (_alarmsLock)
            {
                alarmId = _nextAlarmId++;
            }

            Context adminContext = TcHmiApplication.Context;

            // an event usually has an alarm or a message as payload. here, we raise an
            // alarm that can then be confirmed with the 'ConfirmAlarm' function symbol.
            var now = DateTime.Now;
            var e = new Event
            {
                Name = "ALARM_TO_CONFIRM",
                Domain = TcHmiApplication.Context.Domain,
                TimeReceived = now,
                Payload = new Alarm
                {
                    Name = "ALARM_TO_CONFIRM",
                    Domain = TcHmiApplication.Context.Domain,
                    Severity = Severity.Info,
                    Id = alarmId,
                    TimeRaised = now,
                    TimeCleared = now,
                    TimeConfirmed = UnixTime.UnixEpoch,
                    ConfirmationState = AlarmConfirmationState.Wait,
                },
            };

            lock (_alarmsLock)
            {
                _activeAlarmsById.Add(alarmId, e);
            }

            if (send)
            {
                // sending alarm with 'EventLifetime.Temporary' so that they are automatically
                // removed after restart or re-initialization of the extension.
                TcHmiApplication.AsyncHost.Send(adminContext, ref e, EventLifetime.Temporary);
            }
        }

        private void OnConfirm(object sender, TcHmiSrv.Core.Listeners.AlarmProviderListenerEventArgs.OnConfirmEventArgs e)
        {
            var alarm = e.Alarm;
            lock (_alarmsLock)
            {
                if (!_activeAlarmsById.ContainsKey(alarm.Id))
                {
                    throw new TcHmiException("this alarm id does not correspond to an active alarm", ErrorValue.HMI_E_INVALID_PARAMETER);
                }

                alarm.TimeConfirmed = DateTime.Now;
                alarm.ConfirmationState = AlarmConfirmationState.Confirmed;

                // move alarm to the other dictionary
                _inactiveAlarmsById.Add(alarm.Id, _activeAlarmsById[alarm.Id]);
                _activeAlarmsById.Remove(alarm.Id);
            }

            // notify clients that opened a subscription using the 'SubscribeEvents' function symbol
            // and tell the default logger extension to change the alarm in the database.
            TcHmiApplication.AsyncHost.NotifyListener(TcHmiApplication.Context, AlarmChangeType.Dispose, alarm);
        }

        private void OnInitialListEvents(object sender, OnInitialListEventsEventArgs e)
        {
            // this handler is called after initialization of the extension,
            // after initialization of another extension that implements the
            // EventListener interface, and if a refresh is triggered by this
            // extension using 'TcHmiApplication.AsyncHost.ResetTemporaryEvents'.

            // add all existing events created with 'EventLifetime.Temporary':
            lock (_alarmsLock)
            {
                foreach (var kv in _activeAlarmsById)
                {
                    e.Events.Add(kv.Value);
                }
                foreach (var kv in _inactiveAlarmsById)
                {
                    e.Events.Add(kv.Value);
                }
            }
        }

        public void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            Context adminContext = TcHmiApplication.Context;

            // handle all commands one by one
            foreach (Command command in e.Commands)
            {
                try
                {
                    switch (command.Mapping)
                    {
                        case "SendAnotherMessage":
                            TcHmiApplication.AsyncHost.Send(adminContext, new Message()
                            {
                                Severity = Severity.Info,
                                Domain = TcHmiApplication.Context.Domain,
                                Name = "ANOTHER_MESSAGE",
                                TimeRaised = DateTime.Now,
                            });
                            break;
                        case "SendDynamicallyLocalizedMessage":
                            {
                                var msg = new Message()
                                {
                                    Severity = Severity.Info,
                                    Domain = TcHmiApplication.Context.Domain,
                                    Name = "DYNAMICALLY_LOCALIZED_MESSAGE",
                                    TimeRaised = DateTime.Now,
                                };
                                msg.Parameters.Add("index", ++_dynamicallyLocalizedMessagesCount);
                                TcHmiApplication.AsyncHost.Send(adminContext, msg);
                                break;
                            }
                        case "RaiseAdditionalAlarm":
                            RaiseAlarm();
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

        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.EventLocalizationListenerEventArgs.OnRequestEventArgs e)
        {
            switch (e.Localizable.Name)
            {
                case "DYNAMICALLY_LOCALIZED_MESSAGE":
                    {
                        switch (e.Locale)
                        {
                            case "en":
                                e.Localized = "Dynamically localized message no. " + ((int)e.Localizable.Parameters["index"]).ToString();
                                return;
                        }
                        break;
                    }
            }
            throw new TcHmiException("no localization found for this localizable object", ErrorValue.HMI_E_INVALID_PARAMETER);
        }
    }
}
