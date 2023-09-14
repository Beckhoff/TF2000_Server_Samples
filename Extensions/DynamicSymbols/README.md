# About the "DynamicSymbols" sample

The "DynamicSymbols" sample server extension shows how to create and manage dynamic symbols using the `DynamicSymbolsProvider` class.

**First steps:**

- [Working with server extensions](../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../resources/InteractingWithServerExtensions.md)

Dynamic symbols are symbols that can be created and removed during the runtime of a server extension.
In contrast, static symbols are specified in the `Config.json` of a server extension at compile time.
For general information about providing dynamic symbols in server extensions, see the documentation
of the `TcHmiSrv.Core.Tools.DynamicSymbols` namespace in the
[Beckhoff Information System](https://infosys.beckhoff.com/index.php?content=../content/1031/te2000_tc3_hmi_engineering/3864419211.html).

The dynamic symbols in this sample server extension represent machines of type `Furnace`, `Press`, and `Saw`.

```json
"AddMachine": {
  "readValue": {
    "function": true
  },
  "writeValue": {
    "type": "object",
    "properties": {
      "name": {
        "type": "string"
      },
      "type": {
        "type": "string"
      }
    },
    "additionalProperties": false,
    "required": [
      "name",
      "type"
    ]
  }
}
```

`name` specifies the name of the dynamic symbol that represents the new machine.

`type` specified the type of the new machine. (`Furnace`, `Press` or `Saw`)

After a machine has been added, it will be displayed in the TwinCAT HMI Configuration window.
You can view the machine's schema in this window and create mappings for the machine itself or its sub-symbols.
You can then read from or write to the machine using the mapped symbols.
For example, setting the property `IsWorking` of any machine to `true` simulates a work step.

When the "DynamicSymbols" server extension is shut down, the added machines don't get lost. Instead, they
are saved to a file that is loaded at the next start of the server extension to restore the added machines.
The storage type doesn't have to be a file but can be any database, even the server
extension configuration itself.

Finally, to remove a machine, you can call the following function symbol:

```json
"RemoveMachine": {
  "readValue": {
    "function": true,
    "type": "boolean"
  },
  "writeValue": {
    "type": "string"
  }
}
```

`writeValue` specifies the name of the machine resp. dynamic symbol to remove.

`readValue` indicates whether the machine resp. dynamic symbol was successfully removed.
