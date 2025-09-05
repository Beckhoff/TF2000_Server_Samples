# About the "NodeJSRandomValue" sample

The "NodeJSRandomValue" sample extension provides random numbers between zero
and a configurable maximum.

The initial maximum is 1000. New random numbers are returned by the
`NodeJSRandomValue.RandomValue` symbol and are always smaller or equal to
`NodeJSRandomValue.Config::maxRandom`.

The extension configuration includes a single integer value
(`NodeJSRandomValue.Config::maxRandom`) that can be read directly from the
extension configuration.

**First steps:**

- [Working with server extensions](../../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Generate a new random number

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "NodeJSRandomValue.RandomValue",
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
                "symbol": "NodeJSRandomValue.RandomValue",
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
                "symbol": "NodeJSRandomValue.Config::maxRandom",
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
                "symbol": "NodeJSRandomValue.Config::maxRandom",
                "readValue": 1000
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
                "symbol": "NodeJSRandomValue.Config::maxRandom",
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
                "symbol": "NodeJSRandomValue.Config::maxRandom",
                "readValue": 2048
            }
        ]
    }
    ```
