# About the "CustomUserManagement" sample

The "CustomUserManagement" extension is a basic authentication extension that mimics the
behavior of the "TcHmiUserManagement" extension.
It uses the "RequestListener" interface to implement all required and optional function symbols
that are used for user management.
They "ConfigListener" interface is used to to unregister user accounts from the HMI server's
configuration (`TcHmiSrv.Config`) after they are deleted from the "CustomUserManagement" extension.

**First steps:**

- [Working with server extensions](../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../resources/InteractingWithServerExtensions.md)

## Remarks

- The "TcHmiUserManagement" extension is required, i.e., cannot be disabled. It is responsible for the special users "__SystemGuest" and "__SystemAdministrator." Custom authentication extensions do not need to handle these cases.
- The `EnableUser`, `DisableUser`, `ListDisabledUsers` triplet is optional. The HMI engineering checks whether these function symbols exist and adjusts its behavior/UI accordingly.
