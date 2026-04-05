# Directory Purpose
Core Unity runtime bridge between in-game IDE execution and Java gRPC backend.

## What Lives Here
- `CodeExecutor.cs`: sends Java code and processes execution events.
- `CodeWorldObject.cs`: base network-addressable Unity object contract (`ObjectId` + API names).
- `PrinterComponent.cs`: maps print jobs to `VariablePrinter` visual behavior.
- `ScreenController.cs`: scene control helper.

## Key Files
- `CodeExecutor.cs`
- `CodeWorldObject.cs`
- `PrinterComponent.cs`

## How It Connects
- Uses generated gRPC client types from `Assets/Rpc/Generated`.
- Resolves game objects by API name/object ID for RPC dispatch.
- Integrates with IDE UI (`Assets/_UI/IDE`) to execute active tab code.

## Runtime Role (LV1/LV2/Server)
- Main Unity-side execution client for LV2 Java coding loop.

## Dependencies
- `proto/game.proto` contract via generated C# classes.
- Java service implementation in `java-server`.

## Notable Flows
- Execute button -> send `ExecuteRequest.JavaCode` -> receive responses (`Print*`, errors, completion, object queries).
- `FindObjectQuery` -> scan `CodeWorldObject` instances -> send `FindObjectResult` back to server.

## Maintenance Notes
- Keep dispatch method names and payload semantics aligned with proto and Java proxy implementations.
