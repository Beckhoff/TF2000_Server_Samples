# About the "PythonRandomValue" sample

The "PythonRandomValue" sample extension provides random numbers between zero
and a configurable maximum.

The initial maximum is 1000. New random numbers are returned by the
`PythonRandomValue.RandomValue` symbol and are always less than or equal to
`PythonRandomValue.Config::maxRandom`.

The extension configuration includes a single integer value
(`PythonRandomValue.Config::maxRandom`) that can be read directly from the extension
configuration.

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
                "symbol": "PythonRandomValue.RandomValue",
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
                "symbol": "PythonRandomValue.RandomValue",
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
                "symbol": "PythonRandomValue.Config::maxRandom",
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
                "symbol": "PythonRandomValue.Config::maxRandom",
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
                "symbol": "PythonRandomValue.Config::maxRandom",
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
                "symbol": "PythonRandomValue.Config::maxRandom",
                "readValue": 2048
            }
        ]
    }
    ```
