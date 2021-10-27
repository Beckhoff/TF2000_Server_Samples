# About the "Diagnostics" sample

By convention, server extensions provide status information by providing a
function symbol called `Diagnostics`. This convention is used by the HMI
server's configuration page. If your extension has a `Diagnostics` symbol,
the state information will be shown on the configuration page.

**First steps:**

- [Working with server extensions](../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../resources/InteractingWithServerExtensions.md)

## Example requests

1. Get the current state information of the extension

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "Diagnostics.Diagnostics",
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
                "symbol": "Diagnostics.Diagnostics",
                "readValue": {
                    "cpuUsage": 7.650740623474121,
                    "sinceStartup": "PT42M33.5623363S"
                }
            }
        ]
    }
    ```

## Examples of state information returned by other extensions

1. **Example response of `ADS.Diagnostics`:**

    ```json
    {
        "symbol": "ADS.Diagnostics",
        "readValue": {
            "runtimes": {
                "PLC1": {
                    "adsApplicationName": "Port_851",
                    "adsProjectCompiledAt": "2021-07-28T12:32:30Z",
                    "adsProjectName": "One",
                    "adsState": "Run",
                    "adsVersion": "3.1.4024",
                    "connectionState": "Good",
                    "deviceName": "TwinCAT System"
                }
            }
        }
    }
    ```

1. **Example response the server's `Diagnostics` symbol:**

    ```json
    {
        "symbol": "Diagnostics",
        "readValue": {
            "ACCEPTEDSOCKETS": 146,
            "ACTIVESESSIONS": 4,
            "ACTIVESOCKETS": 3,
            "ARCHITECTURE": "win-x64",
            "DISCOVERY_RUNNING": true,
            "DOTNETCLASSICVERSIONS": ["v2.0", "v3.5", "v4.0", "v4.8"],
            "DOTNETCOREVERSIONS": ["2.1.28", "3.1.15", "3.1.16", "5.0.6", "5.0.7"],
            "DOTNETVERSIONS": ["v2.0.50727", "v4.0.30319"],
            "LICENSE": {
                "CLIENTS": 101,
                "EXTENSIONS": true,
                "SERVERS": 1,
                "STATE": "OK",
                "TARGETS": 101,
                "USEDCLIENTS": 1,
                "USEDTARGETS": 1
            },
            "MEMORYUSAGE": 58.19140625,
            "REMOTESERVERS": {},
            "SERVERTIME": "2021-07-29T08:43:06Z",
            "TRAFFICIN": 375021,
            "TRAFFICOUT": 1340373,
            "UPTIME": "PT3M57S"
        }
    }
    ```
