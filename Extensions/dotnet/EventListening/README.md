# About the "EventListening" sample

The "EventListening" extension is an extension that showcases various features of the event system.
It uses the "EventListener" interface to listen for messages and alarms from other domains.
It localizes the events and computes some statistics about the events, and makes them available
via the "EventStatistics" symbol.

**First steps:**

- [Working with server extensions](../../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Read the current event statistics

   **Request:**

   ```json
   {
      "requestType": "ReadWrite",
      "commands": [
         {
            "symbol": "EventListening.EventStatistics"
         }
      ]
   }
   ```

   **Response:**

   ```json
   {
      "commands": [
         {
            "symbol": "EventListening.EventStatistics",
            "readValue": {
               "TcHmiSrv": {
                  "ExtensionSessionId": "",
                  "PersistentEventsPerText": {
                     "Domain 'EventListening' initialized":1,
                     "Script execution has been cancelled: second call": 10,
                     "Domain 'TcHmiEventLogger' initialized": 2
                  },
                  "TemporaryEventsPerText": {}
               },
               "TcHmiEventLogger": {
                  "ExtensionSessionId": "011C5B67-18E3-47C3-91B4-2CEFFDF88ED8",
                  "PersistentEventsPerText": {},
                  "TemporaryEventsPerText": {
                     "This is a sample of a verbose event (13030) (Hello, World!)": 1,
                     "This is a sample of a verbose event": 111
                  }
               }
            }
         }
      ]
   }
   ```
