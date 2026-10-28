# Tests for the "CSharpRandomValue" sample

These [pytest](https://docs.pytest.org) tests show that a .NET server
extension is tested with the TwinCAT HMI Server Fixture
(`beckhoff-tchmi-tests-serverfixture`) the same way as a Python extension; see
the [WeatherData tests](../../../python/WeatherData/Tests/README.md). Only the
extension path differs: a .NET extension is registered from its build output.

Each test session:

1. starts a temporary TwinCAT HMI server with a random port and a throwaway
   administrator password (see [conftest.py](conftest.py)),
2. registers the built extension from `../RandomValue/bin/Release`,
3. waits until the extension is initialized,
4. runs the tests in [test_randomvalue.py](test_randomvalue.py) against it and
5. shuts the server down and removes its temporary files.

The tests check that `RandomValue` stays below the configured
`Config::maxRandom`, that changing `maxRandom` limits the values accordingly
and that the extension rejects a negative `maxRandom`. Unlike the
[unit tests](../RandomValueUnitTests), which call the extension class with a
mocked server, these tests run the extension in a real server.

## Requirements

- Windows with Python 3.14 or later in `PATH`
- The .NET SDK to build the extension
- The TwinCAT HMI Server and a TwinCAT license for it
- The Beckhoff Python packages, installed via TcPkg:

  ```bat
  tcpkg install Python.Beckhoff.tchmi.client Python.Beckhoff.tchmi.tests.serverfixture
  ```

## Running the tests

Run the following commands in this folder:

```bat
dotnet build ..\RandomValue\CSharpRandomValue.csproj -c Release
python -m venv pythonenv
pythonenv\Scripts\pip install --require-hashes -r requirements.txt
pythonenv\Scripts\python -m pytest
```

The framework log and the server output are written to `logs/`.

The following environment variables are optional:

| Variable | Purpose |
|---|---|
| `TCHMI_SERVER_BINARY` | Path of the `TcHmiSrv.exe` to test with. Default: the installed server |
| `TCHMI_EXTENSION_PATH` | Build output of the extension. Default: `../RandomValue/bin/Release` |
| `TCHMI_EXTENSION_INIT_TIMEOUT` | Seconds to wait for the extension to initialize. Default: 60 |
| `HMI_PASSWORD` | Administrator password of the test server. Default: a random one |
