//-----------------------------------------------------------------------
// <copyright file="EventListening.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.EventListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;

namespace EventListening
{
    internal class EventDomainInfo
    {
        public string ExtensionSessionId;
        public HashSet<int> IdsOfPersistentAlarms { get; } = new HashSet<int>();
        public HashSet<int> IdsOfTemporaryAlarms { get; } = new HashSet<int>();
        public Dictionary<string, int> PersistentEventsPerText { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> TemporaryEventsPerText { get; } = new Dictionary<string, int>();
    }

    public class EventListening : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();
        private readonly EventListener _eventListener = new EventListener();

        private readonly object _lock = new object();
        private readonly Dictionary<string, EventDomainInfo> _eventDomainInfo = new Dictionary<string, EventDomainInfo>();

        public ErrorValue Init()
        {
            // the event system of the HMI server distinguishes between persistent and temporary events:
            //   - events sent with 'EventLifetime.Persistent' are not affected by calls of the 'OnReceive' handler.
            //     the 'OnRemoveEvent' handler is never called for persistent events.
            //   - events sent with 'EventLifetime.Temporary' are valid until the 'OnReceive' handler is called for
            //     the event domain or 'OnRemoveEvent' is called for the event.
            // example use case for temporary events:
            //   - the TcHmiEventLogger extension uses temporary events for the events that are imported from the TwinCAT EventLogger.
            //     it forwards information about new or changed events to the HMI server that will then inform all 'EventListener's.
            //   - every time the connection to a target system is (re)established, recent existing events are imported and the
            //     'OnReceive' handler of all 'EventListener's is called to invalidate all old temporary events and to provide a new
            //     list of existing temporary events.
            //   - every time a new alarm or message is received or an existing alarm is changed in the TwinCAT EventLogger,
            //     the 'OnChange' handler of all 'EventListener's is called.
            //   - to limit the number of imported events from the TwinCAT EventLogger, the TcHmiEventLogger extension removes
            //     temporary events that are no longer relevant. the 'OnRemoveEvent' handler of all 'EventListener's are called in this case.

            // add event handlers
            _requestListener.OnRequest += OnRequest;
            _eventListener.OnChange += OnChange;
            _eventListener.OnRemoveEvent += OnRemoveEvent;
            _eventListener.OnReceive += OnReceive;

            return ErrorValue.HMI_SUCCESS;
        }

        private void OnChange(object sender, OnChangeEventArgs e)
        {
            // the 'OnChange' handler is called whenever a new message or alarm is created, or an existing alarm is changed.
            // new persistent events are always received via the 'OnChange' handler. for temporary events it is not reliable
            // in and of itself, because an initial list of temporary events can be provided via 'OnReceive'.
            // if you care about temporary events, you should probably also implement the 'OnReceive' and 'OnRemoveEvent' handlers.
            // we can detect temporary events by checking whether the 'ExtensionSessionId' is provided. the 'ExtensionSessionId' is
            // specific to the event domain and is only set for temporary events. it changes with every 'OnReceive' call.

            lock (_lock)
            {
                if (e.Event.Payload.Type != TcHmiSrv.Core.ValueType.Message && e.Event.Payload.Type != TcHmiSrv.Core.ValueType.Alarm)
                {
                    // ignore events that have a generic value as payload. those events are rare.
                    // we care only about messages and alarms.
                    return;
                }

                bool isPersistent = string.IsNullOrEmpty(e.ExtensionSessionId);
                if (!_eventDomainInfo.ContainsKey(e.Event.Domain))
                {
                    _eventDomainInfo[e.Event.Domain] = new EventDomainInfo();
                }
                var eventDomainInfo = _eventDomainInfo[e.Event.Domain];
                if (!isPersistent && eventDomainInfo.ExtensionSessionId != e.ExtensionSessionId)
                {
                    // this temporary event can be ignored because the 'ExtensionSessionId' was already changed by an 'OnReceive' call.
                    // this temporary event belongs to the old 'ExtensionSessionId' and is no longer relevant.
                    return;
                }

                var idsOfAlarms = isPersistent ? eventDomainInfo.IdsOfPersistentAlarms : eventDomainInfo.IdsOfTemporaryAlarms;
                var eventsPerText = isPersistent ? eventDomainInfo.PersistentEventsPerText : eventDomainInfo.TemporaryEventsPerText;

                if (e.Event.Payload.Type == TcHmiSrv.Core.ValueType.Alarm)
                {
                    Alarm alarm = e.Event.Payload.GetAlarm();
                    if (idsOfAlarms.Contains(alarm.Id))
                    {
                        return;  // we've already seen this alarm and don't care about changes.
                    }
                    idsOfAlarms.Add(alarm.Id);
                }

                string text = LocalizedEventName(e.Event, "en");
                if (!eventsPerText.ContainsKey(text))
                {
                    eventsPerText.Add(text, 1);
                }
                else
                {
                    eventsPerText[text] = eventsPerText[text] + 1;
                }
            }
        }

        private void OnReceive(object sender, OnReceiveEventArgs e)
        {
            // when 'OnReceive' is called, the 'ExtensionSessionId' of the event domain changes and all previously received temporary events
            // are invalidated. a new list of temporary events is provided as a parameter. Persistent events aren't affected by 'OnReceive' calls.

            lock (_lock)
            {
                if (!_eventDomainInfo.ContainsKey(e.Context.Domain))
                {
                    _eventDomainInfo[e.Context.Domain] = new EventDomainInfo();
                }
                var eventDomainInfo = _eventDomainInfo[e.Context.Domain];

                // invalidate/forget existing temporary events
                eventDomainInfo.IdsOfTemporaryAlarms.Clear();
                eventDomainInfo.TemporaryEventsPerText.Clear();

                // update the 'ExtensionSessionId'
                eventDomainInfo.ExtensionSessionId = e.ExtensionSessionId;

                // process the new list of temporary events
                foreach (Value v in e.Events)
                {
                    Event ev = v.GetEvent();

                    if (ev.Payload.Type == TcHmiSrv.Core.ValueType.Alarm)
                    {
                        Alarm alarm = ev.Payload.GetAlarm();
                        eventDomainInfo.IdsOfTemporaryAlarms.Add(alarm.Id);
                    }

                    string text = LocalizedEventName(ev, "en");
                    if (!eventDomainInfo.TemporaryEventsPerText.ContainsKey(text))
                    {
                        eventDomainInfo.TemporaryEventsPerText.Add(text, 1);
                    }
                    else
                    {
                        eventDomainInfo.TemporaryEventsPerText[text] = eventDomainInfo.TemporaryEventsPerText[text] + 1;
                    }
                }
            }
        }

        private void OnRemoveEvent(object sender, OnRemoveEventEventArgs e)
        {
            // the 'OnRemoveEvent' handler is called whenever a specific temporary event should be removed/invalidated.

            lock (_lock)
            {
                if (!_eventDomainInfo.ContainsKey(e.Event.Domain))
                {
                    return;  // nothing to remove
                }

                var eventDomainInfo = _eventDomainInfo[e.Event.Domain];
                if (eventDomainInfo.ExtensionSessionId != e.ExtensionSessionId)
                {
                    // the removal of this temporary event can be ignored because the 'ExtensionSessionId' was already
                    // changed by an 'OnReceive' call. this temporary event belongs to the old 'ExtensionSessionId'
                    // and is no longer relevant.
                    return;
                }

                if (e.Event.Payload.Type == TcHmiSrv.Core.ValueType.Alarm)
                {
                    Alarm alarm = e.Event.Payload.GetAlarm();
                    eventDomainInfo.IdsOfTemporaryAlarms.Remove(alarm.Id);
                }

                string text = LocalizedEventName(e.Event, "en");
                if (!eventDomainInfo.TemporaryEventsPerText.ContainsKey(text))
                {
                    // nothing to remove
                }
                else
                {
                    eventDomainInfo.TemporaryEventsPerText[text] = eventDomainInfo.TemporaryEventsPerText[text] - 1;
                    if (eventDomainInfo.TemporaryEventsPerText[text] == 0)
                    {
                        eventDomainInfo.TemporaryEventsPerText.Remove(text);
                    }
                }
            }
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
                        case "EventStatistics":
                            lock (_lock)
                            {
                                command.ReadValue = new Value
                                {
                                    Type = TcHmiSrv.Core.ValueType.Map
                                };
                                foreach (KeyValuePair<string, EventDomainInfo> kv in _eventDomainInfo)
                                {
                                    var eventDomainInfo = (command.ReadValue[kv.Key] = new Value());
                                    eventDomainInfo.Type = TcHmiSrv.Core.ValueType.Map;
                                    eventDomainInfo["ExtensionSessionId"] = kv.Value.ExtensionSessionId;
                                    var persistentEventsPerText = (eventDomainInfo["PersistentEventsPerText"] = new Value());
                                    persistentEventsPerText.Type = TcHmiSrv.Core.ValueType.Map;
                                    foreach (var textAndCount in kv.Value.PersistentEventsPerText)
                                    {
                                        persistentEventsPerText[textAndCount.Key] = textAndCount.Value;
                                    }
                                    var temporaryEventsPerText = (eventDomainInfo["TemporaryEventsPerText"] = new Value());
                                    temporaryEventsPerText.Type = TcHmiSrv.Core.ValueType.Map;
                                    foreach (var textAndCount in kv.Value.TemporaryEventsPerText)
                                    {
                                        temporaryEventsPerText[textAndCount.Key] = textAndCount.Value;
                                    }
                                }
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

        private static string LocalizedEventName(Event ev, string locale)
        {
            Context adminContext = TcHmiApplication.Context;
            adminContext.Session.Locale = locale;

            // IMPORTANT: extensions may add or change parameters during localization. For example the TcHmiEventLogger
            //            adds the "eventClassName" parameter. we don't use the parameters in this example.

            switch (ev.Payload.Type)
            {
                case TcHmiSrv.Core.ValueType.Alarm:
                    {
                        Alarm alarmCopy = ev.Payload.GetAlarm().DeepCopy();
                        return TcHmiApplication.AsyncHost.Localize(adminContext, alarmCopy);
                    }
                case TcHmiSrv.Core.ValueType.Message:
                    {
                        Message messageCopy = ev.Payload.GetMessage().DeepCopy();
                        return TcHmiApplication.AsyncHost.Localize(adminContext, messageCopy);
                    }
                default:
                    return null;
            }
        }
    }
}
