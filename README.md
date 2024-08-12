# Archipelago.MultiClient.Net.Analyzers

Source analyzers, fixes, and code generation for the [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net)
library.

## Analyzers and Diagnostics

### MULTICLIENT001 - DataStorageElement assigned outside of DataStorageHelper

This warning is intended to prevent bugs which may be caused by misuse of `DataStorageHelper`. The Archipelago
network protocol allows batching several data storage operations in an atomic fashion. In order to support this,
`DataStorageHelper` only sends a packet to the server when a `DataStorageElement`, containing the batched operations,
is re-assigned back to the `DataStorageHelper`. This means that storing a `DataStorageElement` into a variable can
cause undesirable side effects on the element, as well as prevent it from being sent to the server at all. Instead,
it is recommended to use a compound assignment operator to apply edits directly to the `DataStorageHelper`.

**Incorrect Code:**

```cs
DataStorageElement elem = session.DataStorage[Scope.Slot, "MyData"];
elem.Initialize(0);
elem += 2;
```

**Fixed Code:**

```cs
session.DataStorage[Scope.Slot, "MyData"].Initialize(0);
session.DataStorage[Scope.Slot, "MyData"] += 2;
```