# Server Extension Samples

This repository contains server extensions that showcase many features of the
server extension API, as well as various aspects of the extension-server
interaction.

Here is a list of all sample extensions:

- [NetworkTime](Extensions/NetworkTime/README.md)
- [RandomValue](Extensions/RandomValue/README.md)
- [Diagnostics](Extensions/Diagnostics/README.md)
- [InterExtensionCommunication](Extensions/InterExtensionCommunication/README.md)
- [MinimalAuthentication](Extensions/MinimalAuthentication/README.md)
- [CustomUserManagement](Extensions/CustomUserManagement/README.md)
- [EventSystem](Extensions/EventSystem/README.md)
- [EventListening](Extensions/EventListening/README.md)
- [ConfigListening](Extensions/ConfigListening/README.md)
- [DynamicSymbols](Extensions/DynamicSymbols/README.md)
- [ErrorHandling](Extensions/ErrorHandling/README.md)

## Documentation for the .NET extension API

The documentation for the API can be found in the
[Beckhoff Information System](https://infosys.beckhoff.com/index.php?content=../content/1031/te2000_tc3_hmi_engineering/10591698827.html&id=7157243092038441902).

If the TE2000-HMI-Engineering is installed on your system, the documentation is
also available at this path:

```txt
C:\TwinCAT\Functions\TE2000-HMI-Engineering\Infrastructure\TcHmiServer\docs\TcHmiSrvExtNet.Core.Documentation.chm
```

## Getting started

Our suggestion is to start with the
[NetworkTime](Extensions/NetworkTime/README.md) or the
[RandomValue](Extensions/RandomValue/README.md) sample.
Both are relatively short but contain many of the most commonly used features:
Registering listeners, handling symbol requests, storing settings in the
extension configuration.

Every extension can define its own set of error codes. The
[ErrorHandling](Extensions/ErrorHandling/README.md) showcases how this should be
implemented.

The HMI server generates a configuration page for every server extension. If
you want to display additional status information on your extension's
configuration page, have a look at the
[Diagnostics](Extensions/Diagnostics/README.md) sample.

## Advanced samples

### **Authentication**

- [MinimalAuthentication](Extensions/MinimalAuthentication/README.md): If you
want to extend the authentication system of the HMI server, this sample
extension is the best starting point.
- [CustomUserManagement](Extensions/CustomUserManagement/README.md): A more
realistic implementation of a user management extension that supports adding,
removing, renaming, as well as enabling and disabling users.

### **Event system**

- [EventSystem](Extensions/EventSystem/README.md): Use this sample as a
starting point if you want to write an extension that sends messages or raises
alarms.
- [EventListening](Extensions/EventListening/README.md): If your server
extension is going to listen for messages and alarms from other extensions or
the HMI server, take a look at this sample.

### **Miscellaneous**

- [InterExtensionCommunication](Extensions/InterExtensionCommunication/README.md):
This sample will give you an understanding of how multiple extensions can
interact with each other, and with the HMI server.
- [ConfigListening](Extensions/ConfigListening/README.md): If your server
extension wants to listen for changes to its configuration, take a look at this
sample.
- [DynamicSymbols](Extensions/DynamicSymbols/README.md): All other samples
provide a fixed list of symbols that clients can use to interact with the
extension. This sample demonstrates how an extension can provide a dynamic list
of symbols that changes at runtime.

## Code Snippets

Descriptions of small blocks of reusable code that showcase concepts or
facilitate the development of a server extensions.

- [Debugging the `Init` method of a server extension](Snippets/DebuggingInit.md)

## Requirements

The following components must be installed to build the samples:

- [.NET Core SDK 3.1](https://dotnet.microsoft.com/download/dotnet) or higher

- [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/?utm_medium=microsoft&utm_source=docs.microsoft.com&utm_campaign=button+cta&utm_content=download+vs2019)
version 16.4 or higher (see
[Install .NET on Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net50#install-with-visual-studio)
for details)

  Please also make sure that the required workloads for .NET development are
  installed.

- [TE1000 TwinCAT 3 Engineering](https://www.beckhoff.com/en-en/products/automation/twincat/te1xxx-twincat-3-engineering/te1000.html)
version 3.1.4024.0 or higher

- [TE2000 TwinCAT 3 HMI Engineering](https://www.beckhoff.com/en-en/products/automation/twincat/te1xxx-twincat-3-engineering/te2000.html)
version 1.12.746.0 or higher

- [TF2200 TwinCAT 3 HMI Extension SDK](https://www.beckhoff.com/en-en/products/automation/twincat/tfxxxx-twincat-3-functions/tf2xxx-tc3-hmi/tf2200.html)
([standard](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_licensing/3510308491.html) or
[trial](https://infosys.beckhoff.com/content/1033/tc3_licensing/3510308491.html?id=3407725140381911891) license)
