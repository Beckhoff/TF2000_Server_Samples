# About the "StaticSymbols" sample

The "StaticSymbols" sample server extension shows how to generate and use
static symbols using the `ExportSymbolAttribute` class.

**First steps:**

- [Working with server extensions](../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../resources/InteractingWithServerExtensions.md)

Static symbols are symbols that can be created at compile time of a server extension.
In contrast, dynamic symbols are created and removed during the runtime of a server extension.
For general information about creating static symbols in server extensions, see the documentation
of the `TcHmiSrv.Core.Tools.StaticSymbols` namespace in the
[Beckhoff Information System](https://infosys.beckhoff.com/index.php?content=../content/1031/te2000_tc3_hmi_engineering/3864419211.html).

The static symbols in this sample server extension represent machines of type
`Furnace`, `Press`, and `Saw` created from the corresponding types in .NET.

After the sample server extension has been compiled and the symbols and
definitions have been added, they will be displayed as mapped symbols in the
TwinCAT HMI Configuration window after the sample server extension is loaded.
You can view the machines' schema in this window and create or delete mappings
for the machines themselves or their sub-symbols.
You can then read from or write to the machines using the mapped symbols. For
example, setting the property `IsWorking` of any machine to `true` simulates
a work step.
