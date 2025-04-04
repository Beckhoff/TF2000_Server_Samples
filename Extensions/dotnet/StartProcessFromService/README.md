# About the "StartProcessFromService" sample

The "StartProcessFromService" sample server extension is a server extension
that starts a process as a specified user, even when the current process runs
as a service under the SYSTEM account. This is useful when server extensions
loaded into a TwinCAT HMI server running as a service need to start a user
process. To start a process from a service this sample uses the
[CreateProcessAsUserA function](https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessasusera).

**First steps:**

- [Working with server extensions](../../README/WorkingWithServerExtensions.md)

## Example requests

Start Notepad in a new window from a server extension loaded into a TwinCAT HMI
server running as a service:

```json
{
    "requestType": "ReadWrite",
    "commands": [
        {
            "symbol": "StartProcessFromService.StartProcess",
            "commandOptions": [ "SendErrorMessage" ],
            "writeValue": {
                "applicationName": "C:\\Windows\\System32\\notepad.exe",
                "showWindow": true
            }
        }
    ]
}
```
