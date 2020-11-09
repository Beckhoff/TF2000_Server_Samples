# About the "NetworkTime" sample

The "NetworkTime" sample extension is a server extension that can query an NTP server.
It uses the "RequestListener" interface to provide a single function symbol called "Now" that
fetches the current timestamp from the NTP server and returns it.
The NTP server URL is stored in the extension configuration at path `NetworkTime.Config::ntpServer`.

**First steps:**

- [Working with server extensions](../../README/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../README/InteractingWithServerExtensions.md)

## Example requests

1. Get the current time from the configured NTP server.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "NetworkTime.Now",
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
                "symbol": "NetworkTime.Now",
                "readValue": "2020-12-28T06:41:54.587Z"
            }
        ]
    }
    ```

1. Get the configured NTP server URL.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "NetworkTime.Config::ntpServer",
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
                "symbol": "NetworkTime.Config::ntpServer",
                "readValue": "ntp.beckhoff-cloud.com"
            }
        ]
    }
    ```

1. Change the configured NTP server URL.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "NetworkTime.Config::ntpServer",
                "writeValue": "time.windows.com",
                "commandOptions": [ "SendErrorMessage", "SendWriteValue" ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "NetworkTime.Config::ntpServer",
                "readValue": "time.windows.com"
            }
        ]
    }
    ```
