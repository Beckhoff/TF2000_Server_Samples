# About the "MinimalAuthentication" sample

The "MinimalAuthentication" extension is the simplest possible authentication extension.
It uses the "AuthListener" interface to handle authentication of a single hard-coded user account:

```txt
Username: "admin"
Password: "123"
```

Also, it uses the "RequestListener" interface to implement the "ListUsers" function symbol that
all authentication extensions must implement.

Authentication extensions only handle authentication, whereas the HMI server handles account settings, permissions, auto-logout, and group memberships. This sample also showcases how extensions can register user accounts in the HMI server's configuration ("TcHmiSrv.Config").

**First steps:**

- [Working with server extensions](../../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Login request for the user account provided by the "MinimalAuthentication" extension.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "Login",
                "writeValue": {
                    "userName": "MinimalAuthentication::admin",
                    "password": "123"
                },
                "commandOptions": [ "SendErrorMessage", "SendWriteValue" ]
            }
        ]
    }
    ```

1. List the names of all usernames provided by the "MinimalAuthentication" extension.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "MinimalAuthentication.ListUsers",
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
                "symbol": "MinimalAuthentication.ListUsers",
                "readValue": [ "admin" ]
            }
        ]
    }
    ```

1. Show the account settings of the "MinimalAuthentication" accounts registered in the HMI server's configuration.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "TcHmiSrv.Config::USERGROUPUSERS::MinimalAuthentication",
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
                "symbol": "TcHmiSrv.Config::USERGROUPUSERS::MinimalAuthentication",
                "readValue": {
                    "admin": {
                        "USERGROUPUSERS_AUTO_LOGOFF": "P30D",
                        "USERGROUPUSERS_GROUPS": [
                            "__SystemAdministrators"
                        ],
                        "USERGROUPUSERS_LOCALE": "client"
                    }
                }
            }
        ]
    }
    ```

1. Get the name of the current default authentication extension.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "TcHmiSrv.Config::DEFAULTAUTHEXTENSION",
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
                "symbol": "TcHmiSrv.Config::DEFAULTAUTHEXTENSION",
                "readValue": "TcHmiUserManagement",
            }
        ]
    }
    ```

1. Make "MinimalAuthentication" the default authentication extension.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "TcHmiSrv.Config::DEFAULTAUTHEXTENSION",
                "writeValue": "MinimalAuthentication",
                "commandOptions": [ "SendErrorMessage", "SendWriteValue" ]
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
                "symbol": "TcHmiSrv.Config::DEFAULTAUTHEXTENSION",
                "readValue": "MinimalAuthentication"
            }
        ]
    }
    ```
