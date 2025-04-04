# About the "EventSystem" sample

The "EventSystem" extension is an extension that showcases various features of the event system.
It uses the "RequestListener" interface to provide the function symbols "SendAnotherMessage,"
"SendDynamicallyLocalizedMessage", and "RaiseAdditionalAlarm".
The "EventLocalizationListener" interface is used to localize the messages with the name "DYNAMICALLY_LOCALIZED_MESSAGE,"
that are created/sent when the "SendDynamicallyLocalizedMessage" function symbol is called, dynamically
at runtime (as opposed to static localizations defined in the "EventSystem.Language.en.json" file).
It uses the "AlarmProviderListener" to handle confirmations of the provided alarms, as well as to
make use of temporary events, not just persistent events.

**First steps:**

- [Working with server extensions](../../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Check whether the "FIRST_MESSAGE" was successfully sent during initialization
of the sample extension.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ListEvents",
                "filter": [
                    {
                        "comparator": "==",
                        "path": "domain",
                        "value": "EventSystem"
                    },
                    {
                        "logic": "AND"
                    },
                    {
                        "comparator": "==",
                        "path": "name",
                        "value": "FIRST_MESSAGE"
                    }
                ],
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ListEvents",
                "readValue": [
                    {
                        "domain": "EventSystem",
                        "localizedString": "First message. Sent upon initialization of this sample extension.",
                        "name": "FIRST_MESSAGE",
                        "payload": {
                            "name": "FIRST_MESSAGE",
                            "domain": "EventSystem",
                            "params": {},
                            "severity": 1,
                            "timeRaised": "2020-12-28T09:40:03.8830387Z"
                        },
                        "payloadType": 0,
                        "timeReceived": "2020-12-28T09:40:03.8950405Z"
                    }
                ]
            }
        ]
    }
    ```

1. List all events (alarms and messages) of the sample extension

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ListEvents",
                "filter": [
                    {
                        "comparator": "==",
                        "path": "domain",
                        "value": "EventSystem"
                    }
                ],
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ListEvents",
                "readValue": [
                    {
                        "domain": "EventSystem",
                        "localizedString": "An alarm. If not already confirmed, it can be confirmed by calling the 'ConfirmAlarm' function symbol.",
                        "name": "ALARM_TO_CONFIRM",
                        "payload": {
                            "name": "ALARM_TO_CONFIRM",
                            "domain": "EventSystem",
                            "params": {},
                            "severity": 1,
                            "id": 3,
                            "timeRaised": "2020-12-28T10:32:23.2066268Z",
                            "timeCleared": "2020-12-28T10:32:23.2066268Z",
                            "timeConfirmed": null,
                            "alarmState": 2,
                            "confirmationState": 2
                        },
                        "payloadType": 1,
                        "timeReceived": "2020-12-28T10:32:23.2066268Z"
                    },
                    {
                        "domain": "EventSystem",
                        "localizedString": "First message. Sent upon initialization of this sample extension.",
                        "name": "FIRST_MESSAGE",
                        "payload": {
                            "name": "FIRST_MESSAGE",
                            "domain": "EventSystem",
                            "params": {},
                            "severity": 1,
                            "timeRaised": "2020-12-28T09:40:03.8830387Z"
                        },
                        "payloadType": 0,
                        "timeReceived": "2020-12-28T09:40:03.8950405Z"
                    }
                ]
            }
        ]
    }
    ```

1. Send a message with name "ANOTHER_MESSAGE".

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "EventSystem.SendAnotherMessage",
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

1. Send an additional alarm with a new alarm ID.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "EventSystem.RaiseAdditionalAlarm",
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

1. Send a message with name "DYNAMICALLY_LOCALIZED_MESSAGE".

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "EventSystem.SendDynamicallyLocalizedMessage",
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

1. Raise a new alarm with a new unique alarm id and name "ALARM_TO_CONFIRM".

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "EventSystem.SendDynamicallyLocalizedMessage",
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

1. Confirm a specific alarm instance.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ConfirmAlarm",
                "writeValue": {
                    "name": "ALARM_TO_CONFIRM",
                    "domain": "EventSystem",
                    "params": {},
                    "severity": 1,
                    "id": 3,
                    "timeRaised": "2020-12-28T10:32:23.2066268Z",
                    "timeCleared": "2020-12-28T10:32:23.2066268Z",
                    "timeConfirmed": null,
                    "alarmState": 2,
                    "confirmationState": 2
                },
                "commandOptions": [ "SendErrorMessage", "SendWriteValue" ]
            }
        ]
    }
    ```
