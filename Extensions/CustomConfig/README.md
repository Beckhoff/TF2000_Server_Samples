# About the "CustomConfig" sample

The `CustomConfig`-Extension contains a subfolder `ConfigResources` with a CSS-,
JavaScript- and HTML-file. To show their content on the server configuration page, they must be
copied to the output directory (Visual Studio: Right click &#8594; Properties &#8594; Copy
to Output Directory).

If you open an HMI project with this extension, there will be a `Custom config`
tab on the server configuration page which includes a button and a textarea. If you click on the
button, `CustomConfig.GetRandom` will be executed which returns a random integer
between `min` and `max` of the extension configuration. The default configuration
will be still available with the `General` tab.

The default configuration looks like this:

````json
{
    "max": 0,
    "min": 0
}
````

## Example: Download file

1. Virtual directories

    To enable downloading a file of the server system, all file systems, which should be served, must
    be added as virtual directory. All virtual directories can be found on the configuration page under
    `TcHmiSrv > Virtual directories`.

    ````json
    {
        "VIRTUALDIRECTORIES": {
            "/VirtualDirectory": "Path/Foldername"
        }
    }
    ````

1. Download button

    ````html
    <a href="http://127.0.0.1:1010/VirtualDirectory/Path/Filename" download>Download link</a>
    ````

    Limitation: The download button is not available inside of the live-view of the engineering.

## Example requests

1. Read the current configuration of the sample extension:

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "CustomConfig.Config",
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
                "symbol": "Custom.Config",
                "readValue": {
                    "min": 2,
                    "max": 4
                }
            }
        ]
    }
    ```

1. Get a random integer:

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "CustomConfig.GetRandom",
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
                "symbol": "Custom.GetRandom",
                "readValue": 3
            }
        ]
    }
    ```