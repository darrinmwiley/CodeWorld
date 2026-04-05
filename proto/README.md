# Directory Purpose
Single source-of-truth protocol contract between Unity and Java server.

## What Lives Here
- `game.proto` defining service methods and message schema.

## Key Files
- `game.proto`:
  - Service: `CodeWorldService`
  - Streams: `StreamObjectData`, `ExecuteCodeStream`
  - Execution events/jobs: print jobs, object lookup query/result, compile/runtime error, completion.

## How It Connects
- Java server Gradle build generates Java classes from this proto.
- Unity regenerates C# classes into `Assets/Rpc/Generated` from this proto.

## Runtime Role (LV1/LV2/Server)
- Cross-runtime API contract for LV2 coding/execution loop.

## Dependencies
- Protobuf compiler and language-specific gRPC plugins.

## Notable Flows
- Contract update -> regenerate both Java + C# artifacts -> rebuild/test both sides.

## Maintenance Notes
- This file is authoritative; never diverge generated code manually.
