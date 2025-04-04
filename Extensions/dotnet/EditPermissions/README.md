# About the "EditPermissions" sample

The "EditPermissions" sample extension showcases how an extension can
configure user groups, assign users to user groups, and edit the symbol
access permissions for specific user groups.

You should be careful with granting access rights. Granting write access to
things like the file system, authentication extensions or server configuration
might put your system at risk.

**First steps:**

- [Working with server extensions](../../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Call the `EditPermissions.ConfigureOperator` function symbol that creates
   the `operators` user group and the `operator` user. It also configures an initial
   set of permissions for the new user group.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "EditPermissions.ConfigureOperator",
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

1. Verify the configuration of the new user and user group.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "TcHmiSrv.Config::USERGROUPS::operators",
                "commandOptions": [ "SendErrorMessage" ]
            },
            {
                "symbol": "TcHmiSrv.Config::USERGROUPUSERS::TcHmiUserManagement::operator",
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
                "symbol": "TcHmiSrv.Config::USERGROUPS::operators",
                "readValue": {
                    "ENABLED": true,
                    "FILEACCESS": 3,
                    "FILES": {},
                    "SYMBOLACCESS": 3,
                    "SYMBOLS": {
                        "ADS.CheckLicense": 0,
                        "TcHmiSrv.Config": 0
                    }
                }
            },
            {
                "symbol": "TcHmiSrv.Config::USERGROUPUSERS::TcHmiUserManagement::operator",
                "readValue": {
                    "USERGROUPUSERS_AUTO_LOGOFF": "PT0S",
                    "USERGROUPUSERS_GROUPS": [
                        "operators"
                    ],
                    "USERGROUPUSERS_LOCALE": "project"
                }
            }
        ]
    }
    ```

1. Toggle the access permission of the `ADS.CheckLicense` symbol
   for the `operators` user group between `ReadWrite` and `None`.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "EditPermissions.ToggleOperatorAccess",
                "writeValue": "ADS.CheckLicense",
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```
