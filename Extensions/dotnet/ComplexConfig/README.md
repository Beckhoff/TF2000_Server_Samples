# About the "ComplexConfig" sample

Every server extension defines a set of configuration data
in its `*.Schema.json` file.
The "ComplexConfig" sample extension is a server extension
with relatively complex configuration data.
Additionally, the configuration data can be viewed/edited via
the server configuration page.
If you want to learn about JSON schema and how to use
(recursive) definitions, this is a good starting point.

The extension uses the "RequestListener" interface to provide
functions to interact with the configuration data.
The implementations of the `ComplexConfig.MarkAsDone` and
`ComplexConfig.CreateReport` function symbols
showcase the usage of the `GetConfigValue`, `ReplaceConfigValue`
and `RenameConfigValue` functions that are used by almost
all server extensions.

**First steps:**

- [Working with server extensions](../../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Read all data from the "Garden" project.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ComplexConfig.Config::projects::Garden",
                "commandOptions": [ "SendErrorMessage" ]
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
                "symbol": "ComplexConfig.Config::projects::Garden",
                "readValue": {
                    "done": [],
                    "reports": [],
                    "todo": [
                        {
                            "name": "Mow the lawn"
                        },
                        {
                            "name": "Plant trees",
                            "notes": [
                                {
                                    "children": [
                                        {
                                            "text": "Oak"
                                        },
                                        {
                                            "text": "Maple"
                                        }
                                    ],
                                    "text": "Species"
                                }
                            ]
                        }
                    ]
                }
            }
        ]
    }
    ```

1. Move a `todo` list item to the `done` list using the
   `ComplexConfig.MarkAsDone` function symbol.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ComplexConfig.MarkAsDone",
                "writeValue": {
                    "project": "Garden",
                    "index": 1
                },
                "commandOptions": [ "SendErrorMessage" ]
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
                "symbol": "ComplexConfig.MarkAsDone"
            }
        ]
    }
    ```

1. Add a summary of all the items from the `done` list to the `reports` array.
   Then clear the `done` array. We do this by calling the
   `ComplexConfig.CreateReport` function symbol.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ComplexConfig.CreateReport",
                "writeValue": {
                    "project": "Garden"
                },
                "commandOptions": [ "SendErrorMessage" ]
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
                "symbol": "ComplexConfig.CreateReport"
            }
        ]
    }
    ```

1. Read all data from the "Garden" project after calling
   `ComplexConfig.MarkAsDone` and `ComplexConfig.CreateReport`.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ComplexConfig.Config::projects::Garden",
                "commandOptions": [ "SendErrorMessage" ]
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
                "symbol": "ComplexConfig.Config::projects::Garden",
                "readValue": {
                    "done": [],
                    "reports": [
                        "# Done in the week of Monday, February 20, 2023\r\n1. Plant trees\r\n  - Species\r\n    - Oak\r\n    - Maple\r\n"
                    ],
                    "todo": [
                        {
                            "name": "Mow the lawn"
                        }
                    ]
                }
            }
        ]
    }
    ```
