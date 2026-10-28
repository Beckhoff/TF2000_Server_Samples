# Tests for the "WeatherData" sample

These [pytest](https://docs.pytest.org) tests show how to test a server
extension with the TwinCAT HMI Server Fixture
(`beckhoff-tchmi-tests-serverfixture`). Use them as a starting point for the
tests of your own extension.

Each test session:

1. starts a temporary TwinCAT HMI server with a random port and a throwaway
   administrator password (see [conftest.py](conftest.py)),
2. registers the [WeatherData](../WeatherData) extension directly from its
   source folder. The server creates the extension's virtual environment and
   installs its [requirements.txt](../WeatherData/requirements.txt), so the
   first start can take a few minutes,
3. waits until the extension is initialized,
4. runs the tests in [test_weatherdata.py](test_weatherdata.py) against it and
5. shuts the server down and removes its temporary files.

The tests check that the extension loads, that its symbols return values of
the documented types and that it fills them with data from the
[Open-Meteo](https://open-meteo.com/) weather service. The last test is
skipped when the weather service is not reachable from the test machine.

## Requirements

- Windows with Python 3.14 or later in `PATH`
- The TwinCAT HMI Server and a TwinCAT license for it
- The Beckhoff Python packages, installed via TcPkg:

  ```bat
  tcpkg install Python.Beckhoff.tchmi.client Python.Beckhoff.tchmi.extensionapi Python.Beckhoff.tchmi.tests.serverfixture
  ```

## Running the tests

Run the following commands in this folder:

```bat
python -m venv pythonenv
pythonenv\Scripts\pip install --require-hashes -r requirements.txt
pythonenv\Scripts\python -m pytest
```

The framework log and the server output are written to `logs/`.

The following environment variables are optional:

| Variable | Purpose |
|---|---|
| `TCHMI_SERVER_BINARY` | Path of the `TcHmiSrv.exe` to test with. Default: the installed server |
| `TCHMI_EXTENSION_PATH` | Extension folder to test. Default: [../WeatherData](../WeatherData) |
| `TCHMI_EXTENSION_INIT_TIMEOUT` | Seconds to wait for the extension to initialize. Default: 300 |
| `HMI_PASSWORD` | Administrator password of the test server. Default: a random one |

## Testing your own extension

1. Copy this folder next to your extension.
2. Change `EXTENSION_NAME` in [conftest.py](conftest.py) to the name of your
   extension and make `EXTENSION_PATH` point to the folder of the built
   extension. The fixture works with Python, .NET and C++ extensions.
3. Replace the tests in [test_weatherdata.py](test_weatherdata.py) with tests
   for your symbols. `hmi_server.read_symbol()` and
   `hmi_server.write_symbol()` cover simple symbol access; for anything else,
   send a `Command` through `active_session(hmi_server).request()`.

To list the full API of the server fixture, run:

```bat
pythonenv\Scripts\python -m pydoc beckhoff.tchmi.tests.serverfixture.HmiServerFixture
```
