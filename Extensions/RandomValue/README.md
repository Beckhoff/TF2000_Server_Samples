# About the "RandomValue" samples

The "RandomValue" sample extensions for C# and Python are server extensions
that provide random numbers between zero and a configurable maximum.

The initial maximum is 1000. New random numbers are returned by the
`RandomValue.RandomValue` symbol and are always smaller or equal to
`RandomValue.Config::maxRandom`.

The extension configuration includes a single integer value
(`RandomValue.Config::maxRandom`) that can be read directly from the extension
configuration.

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
