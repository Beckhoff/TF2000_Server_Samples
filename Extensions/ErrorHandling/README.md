# About the "ErrorHandling" sample

The "ErrorHandling" sample extension showcases various error handling features.
Every extension can define its own set of error codes and set additional information about errors in the `Command` instance.
The error details provided by the extension are returned in the `error` field of the command response like this:

- The value of the `domain` field specifies which domain returned the error. This is important because every extension can specify its own error codes.
- The value of the `code` field corresponds to the value of `Command.ExtensionResult`.
- The value of the `message` field corresponds to the value returned by the extension's `string ErrorString(uint)` method that is called by the HMI server if the extension sets `Command.ExtensionResult` to a non-zero value.

**First steps:**

- [Working with server extensions](../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Call the 'FailingFunction' function symbol of the sample extension.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ErrorHandling.FailingFunction",
                "commandOptions": [
                    "SendWriteValue",
                    "SendErrorMessage"
                ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ErrorHandling.FailingFunction",
                "error": {
                    "domain": "ErrorHandling",
                    "code": 2,
                    "message": "FUNCTION_FAILED",
                    "reason": "This function symbol always fails."
                }
            }
        ]
    }
    ```

1. Call the 'NotImplementedFunction' function symbol of the sample extension.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ErrorHandling.NotImplementedFunction",
                "commandOptions": [
                    "SendWriteValue",
                    "SendErrorMessage"
                ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ErrorHandling.NotImplementedFunction",
                "error": {
                    "domain": "ErrorHandling",
                    "code": 1,
                    "message": "INTERNAL_ERROR",
                    "reason": "An exception was thrown while the command was processed by the extension: 'Handler is missing.'."
                }
            }
        ]
    }
    ```

1. Call a function that doesn't exist. The HMI server should catch this problem and return an error.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ErrorHandling.UnknownFunction",
                "commandOptions": [
                    "SendWriteValue",
                    "SendErrorMessage"
                ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ErrorHandling.UnknownFunction",
                "error": {
                    "domain": "TcHmiSrv",
                    "code": 513,
                    "message": "SYMBOL_NOT_MAPPED",
                    "reason": "Symbol not mapped"
                }
            }
        ]
    }
    ```
