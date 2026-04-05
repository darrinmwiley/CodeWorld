# Directory Purpose
Printer prefab assets used by level scenes and RPC print demonstrations.

## What Lives Here
- `Printer.prefab` and metadata.

## How It Connects
- Prefab pairs with `PrinterComponent` (`Assets/Scripts`) and `VariablePrinter` logic (`Assets` root scripts).

## Runtime Role (LV1/LV2/Server)
- In-scene target object for print job visualization.

## Dependencies
- `CodeWorldObject`/`PrinterComponent` object ID + API registration.

## Notable Flows
- Java `PrinterProxy.print(...)` -> Unity `CodeExecutor` dispatch -> `PrinterComponent` -> visual output.

## Maintenance Notes
- Ensure prefab has required components attached when duplicated.
