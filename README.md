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

This analyzer also offers a corresponding fix action "Make DataStorage access inline" on variable declarations, which
will remove the offending declaration and inline it at all usage sites.

**Incorrect Code:**

```cs
// MULTICLIENT001
DataStorageElement elem = session.DataStorage[Scope.Slot, "MyData"];
elem.Initialize(0);
// This will never make it to the server
elem += 2;
```

**Fixed Code:**

```cs
session.DataStorage[Scope.Slot, "MyData"].Initialize(0);
// This will make it to the server
session.DataStorage[Scope.Slot, "MyData"] += 2;
```