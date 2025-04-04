# Server Extension Samples

> Welcome to the TwinCAT HMI version 1.14 sample repository!
> If you're still using version 1.12, you can find the relevant samples
> in the [1.12 branch](https://github.com/Beckhoff/TF2000_Server_Samples/tree/1.12).
> Make sure to check it out if you haven't upgraded yet.
> Your feedback and contributions are highly appreciated. Happy coding!

This repository contains server extensions that showcase many features of the
server extension APIs for different programming languages, as well as various
aspects of the interaction between extensions and the server.

Here is a list of all sample extensions:

- *ComplexConfig* ([.NET](Extensions/dotnet/ComplexConfig/README.md))
- *ConfigListening* ([.NET](Extensions/dotnet/ConfigListening/README.md))
- *CustomConfig* ([.NET](Extensions/dotnet/CustomConfig/README.md))
- *CustomUserManagement* ([.NET](Extensions/dotnet/CustomUserManagement/README.md))
- *Diagnostics* ([.NET](Extensions/dotnet/Diagnostics/README.md))
- *DynamicSymbols* ([.NET](Extensions/dotnet/DynamicSymbols/README.md))
- *EditPermissions* ([.NET](Extensions/dotnet/EditPermissions/README.md))
- *ErrorHandling* ([.NET](Extensions/dotnet/ErrorHandling/README.md))
- *EventListening* ([.NET](Extensions/dotnet/EventListening/README.md))
- *EventSystem* ([.NET](Extensions/dotnet/EventSystem/README.md))
- *InterExtensionCommunication* ([.NET](Extensions/dotnet/InterExtensionCommunication/README.md))
- *LetsEncrypt* ([.NET](Extensions/dotnet/LetsEncrypt/README.md))
- *MinimalAuthentication* ([.NET](Extensions/dotnet/MinimalAuthentication/README.md))
- *NetworkTime* ([.NET](Extensions/dotnet/NetworkTime/README.md))
- *ProtectedSymbol* ([.NET](Extensions/dotnet/ProtectedSymbol/README.md))
- *RandomValue* ([.NET](Extensions/dotnet/RandomValue/README.md) and [Python](Extensions/python/RandomValue/README.md))
- *StartProcessFromService* ([.NET](Extensions/dotnet/StartProcessFromService/README.md))
- *StaticSymbols* ([.NET](Extensions/dotnet/StaticSymbols/README.md))
- *WeatherData* ([Python](Extensions/python/WeatherData/README.md))

For more TwinCAT HMI samples check out the related repositories:

- [Client Samples](https://github.com/Beckhoff/TE2000_Client_Samples)

## Documentation for the .NET extension API

The documentation for the API can be found in the
[Beckhoff Information System](https://infosys.beckhoff.com/index.php?content=../content/1031/te2000_tc3_hmi_engineering/3864419211.html).

## Getting started

Our suggestion is to start with the *NetworkTime*
([.NET](Extensions/dotnet/NetworkTime/README.md)) or the
*RandomValue* ([.NET](Extensions/dotnet/RandomValue/README.md) or
[Python](Extensions/python/RandomValue/README.md)) samples. Both are relatively
short but contain many of the most commonly used features:

- Registering listeners
- Handling symbol requests
- Storing settings in the extension configuration

Every extension can define its own set of error codes. The *ErrorHandling*
([.NET](Extensions/dotnet/ErrorHandling/README.md)) showcases how this should
be implemented.

The HMI server generates a configuration page for every server extension. If
you want to display additional status information on your extension's
configuration page, have a look at the *Diagnostics*
([.NET](Extensions/dotnet/Diagnostics/README.md)) sample.

## Advanced samples

### **Authentication**

- *MinimalAuthentication* ([.NET](Extensions/dotnet/MinimalAuthentication/README.md)):
If you want to extend the authentication system of the HMI server, this sample
extension is the best starting point.
- *CustomUserManagement* ([.NET](Extensions/dotnet/CustomUserManagement/README.md)):
A more realistic implementation of a user management extension that supports
adding, removing, renaming, as well as enabling and disabling users.
- *EditPermissions* ([.NET](Extensions/dotnet/EditPermissions/README.md)): An
extension that edits symbol permissions and user groups at runtime.

### **Event system**

- *EventSystem* ([.NET](Extensions/dotnet/EventSystem/README.md)): Use this
sample as a starting point if you want to write an extension that sends
messages or raises alarms.
- *EventListening* ([.NET](Extensions/dotnet/EventListening/README.md)): If
your server extension is going to listen for messages and alarms from other
extensions or the HMI server, take a look at this sample.

### **Miscellaneous**

- *InterExtensionCommunication* ([.NET](Extensions/dotnet/InterExtensionCommunication/README.md)):
This sample will give you an understanding of how multiple extensions can
interact with each other, and with the HMI server.
- *ConfigListening* ([.NET](Extensions/dotnet/ConfigListening/README.md)): If
your server extension wants to listen for changes to its configuration, take a
look at this sample.
- *ComplexConfig* ([.NET](Extensions/dotnet/ComplexConfig/README.md)): A server
extension with a complex configuration schema that showcases how an extension
can read and edit its own extension configuration.
- *CustomConfig* ([.NET](Extensions/dotnet/CustomConfig/README.md)): The HMI
server generates a configuration page for every extension. This sample
showcases how this default page can be replaced with a custom HTML page.
- *StaticSymbols* ([.NET](Extensions/dotnet/StaticSymbols/README.md)): This
sample demonstrates how to automatically generate symbols based on .NET types
at compile time.
- *DynamicSymbols* ([.NET](Extensions/dotnet/DynamicSymbols/README.md)): All
other samples provide a fixed list of symbols that clients can use to interact
with the extension. This sample demonstrates how an extension can provide a
dynamic list of symbols that changes at runtime.
- *StartProcessFromService* ([.NET](Extensions/dotnet/StartProcessFromService/README.md)):
Starts a process from a server extension running as a service.
- *LetsEncrypt* ([.NET](Extensions/dotnet/LetsEncrypt/README.md)): This server
extension generates an SSL certificate with
[Let's Encrypt](https://letsencrypt.org/).
- *ProtectedSymbol* ([.NET](Extensions/dotnet/ProtectedSymbol/README.md)):
Protect and encrypt symbols with the windows
[DPAPI](https://learn.microsoft.com/de-de/dotnet/standard/security/how-to-use-data-protection).
- *WeatherData* ([.NET](Extensions/dotnet/WeatherData/README.md)): Retrieve
current weather data for a specific location using a REST API to query an
online weather service.

## Code Snippets

Descriptions of small blocks of reusable code that showcase concepts or
facilitate the development of a server extensions.

- Debugging the `Init` method of a server extension in
[.NET](Snippets/dotnet/DebuggingInit.md)
- Inter-extension subscriptions in
[.NET](Snippets/dotnet/InterExtensionSubscription.md)

## Requirements

The following components must be installed to build the .NET samples:

- [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) or higher

- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)

  Please also make sure that the required workloads for .NET development are
  installed.

- [TE1000 TwinCAT 3 Engineering](https://www.beckhoff.com/en-en/products/automation/twincat/texxxx-twincat-3-engineering/te1000.html)
version 3.1.4024.0 or higher

- [TE2000 TwinCAT 3 HMI Engineering](https://www.beckhoff.com/en-en/products/automation/twincat/texxxx-twincat-3-engineering/te2000.html)
version 1.14 or higher

- [TF2200 TwinCAT 3 HMI Extension SDK](https://www.beckhoff.com/en-en/products/automation/twincat/tfxxxx-twincat-3-functions/tf2xxx-tc3-hmi/tf2200.html)
([standard](https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_licensing/3510308491.html) or
[trial](https://infosys.beckhoff.com/content/1033/tc3_licensing/3510308491.html?id=3407725140381911891) license)
