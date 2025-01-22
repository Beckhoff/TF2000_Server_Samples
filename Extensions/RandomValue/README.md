# About the "RandomValue" sample

The "RandomValue" sample extension is a server extension that provides random
numbers between zero and a configurable maximum.

The current maximum can be read/set using the `RandomValue.MaxRandom` symbol.
The initial maximum is 1000. New random numbers are returned by the
`RandomValue.RandomValue` symbol and are always smaller or equal to
`RandomValue.MaxRandom`.

The extension configuration includes a single integer value
(`RandomValue.Config::maxRandom`) that can be read directly from the extension
configuration or using the `RandomValue.MaxRandomFromConfig` symbol.
`RandomValue.MaxRandom` and `RandomValue.Config::maxRandom` are never synced
automatically. The value in the extension configuration is only used as a
persistent storage.

**First steps:**

- [Working with server extensions](../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Generate a new random number

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "RandomValue.RandomValue",
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
                "symbol": "RandomValue.RandomValue",
                "readValue": 32
            }
        ]
    }
    ```

1. Get the currently configured maximum using the `RandomValue.MaxRandom`
symbol

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "RandomValue.MaxRandom",
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
                "symbol": "RandomValue.MaxRandom",
                "readValue": 1000
            }
        ]
    }
    ```

1. Change the current maximum using the `RandomValue.MaxRandom` symbol

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "RandomValue.MaxRandom",
                "writeValue": 1234,
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
                "symbol": "RandomValue.MaxRandom",
                "readValue": 1000
            }
        ]
    }
    ```

1. Get the value that is currently stored in the extension configuration using
the `RandomValue.MaxRandomFromConfig` symbol

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "RandomValue.MaxRandomFromConfig",
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
                "symbol": "RandomValue.MaxRandomFromConfig",
                "readValue": 10
            }
        ]
    }
    ```

1. Read the value that is currently stored in the extension configuration
directly

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "RandomValue.Config::maxRandom",
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
                "symbol": "RandomValue.Config::maxRandom",
                "readValue": 10
            }
        ]
    }
    ```

1. Change the value that is stored in the extension configuration

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "RandomValue.Config::maxRandom",
                "writeValue": 2048,
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
                "symbol": "RandomValue.Config::maxRandom",
                "readValue": 2048
            }
        ]
    }
    ```
