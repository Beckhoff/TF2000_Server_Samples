# About the "ProtectedSymbol" sample

The "ProtectedSymbol" sample extension is a server extension that can encrypt and decrypt a symbol using the "Data Protection API" (DPAPI). The DPAPI is a Windows API, that encrypts a value using the account information of the current windows user. The extension uses a "ConfigListener" to encrypt the value written to the config. An "ExportListener" is used to encrypt and decrypt the symbols when importing or exporting the config. The encrypted values can be read by using the "GetProtectedSymbol" function symbol.

**First steps:**

- [Working with server extensions](../../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Get the encrypted value from the Config. This returns the encrypted data encoded in Base64.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ProtectedSymbol.Config::protectedSymbol"
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ProtectedSymbol.Config::protectedSymbol",
                "readValue": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA8c1PaEuhXkCpHF+qpjL2wwAAAAACAAAAAAADZgAAwAAAABAAAADiYYHxmmGtgYcOjFJIye1bAAAAAASAAACgAAAAEAAAAD4p7UaGEtX53kl4GiXVZ4UQAAAAd8c0HLEqV2iwP8xfUUehJBQAAACcF3uxtiZ4lDgdFFP3zx6Ry6y1cQ=="
            }
        ]
    }
    ```

2. Get the plaintext value from the Config. The `writeValue` is the symbol path relative to `ProtectedSymbol.Config`.  

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ProtectedSymbol.GetProtectedSymbol",
                "writeValue": "protectedSymbol"
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ProtectedSymbol.GetProtectedSymbol",
                "readValue": "Hello World!"
            }
        ]
    }
    ```

3. Write a new value to the Config. The value will be encrypted and send as the `readValue`.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ProtectedSymbol.Config::protectedSymbol",
                "writeValue": "foo"
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ProtectedSymbol.Config::protectedSymbol",
                "readValue": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA8c1PaEuhXkCpHF+qpjL2wwAAAAACAAAAAAADZgAAwAAAABAAAAA3a9/RemG4t5RELENUAMBfAAAAAASAAACgAAAAEAAAAIQDkjmP+wMiq5djrFq0lOoIAAAAjjcCVpqRdMMUAAAA+A3QYXBZnzckW8MQoMs4rca5u74="
            }
        ]
    }
    ```
