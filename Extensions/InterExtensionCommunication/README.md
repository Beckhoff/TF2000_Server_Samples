# About the "InterExtensionCommunication" sample

The "InterExtensionCommunication" sample extension is a server extension that showcases communication between
extensions. For this, it implements multiple function symbols using the "RequestListener" interface.

**First steps:**

- [Working with server extensions](../../README/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../README/InteractingWithServerExtensions.md)

## Example requests

1. The "ListLocalRoutes" function symbol calls "ADS.ListRoutes" and only returns the local ADS routes.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "InterExtensionCommunication.ListLocalRoutes",
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
                "symbol": "InterExtensionCommunication.ListLocalRoutes",
                "readValue": [
                    {
                        "label": "local (127.0.0.1.1.1)",
                        "name": "local",
                        "value": "127.0.0.1.1.1"
                    },
                    {
                        "label": "local_remote (172.17.62.213.1.1)",
                        "name": "local_remote",
                        "value": "172.17.62.213.1.1"
                    }
                ]
            }
        ]
    }
    ```

1. The "CheckClientLicenseAndListLocalRoutes" function symbol calls "ADS.ListRouts" and "ADS.CheckLicense" simultaneously and combines the results.

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "InterExtensionCommunication.CheckClientLicenseAndListLocalRoutes",
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
                "symbol": "InterExtensionCommunication.CheckClientLicenseAndListLocalRoutes",
                "readValue": {
                    "clientLicense": {
                        "count": 168,
                        "expireTimeUTC": "2021-01-01T00:00:00Z",
                        "result": 596
                    },
                    "localRoutes": [
                        {
                            "label": "local (127.0.0.1.1.1)",
                            "name": "local",
                            "value": "127.0.0.1.1.1"
                        },
                        {
                            "label": "local_remote (172.17.62.213.1.1)",
                            "name": "local_remote",
                            "value": "172.17.62.213.1.1"
                        }
                    ]
                }
            }
        ]
    }
    ```

1. The "DoubleAdsTimeout" function symbols reads from and writes to the ADS extension's configuration ("ADS.Config").

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "InterExtensionCommunication.DoubleAdsTimeout",
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```
