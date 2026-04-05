# Directory Purpose
Auto-generated C# protobuf/gRPC classes from `proto/game.proto`.

## What Lives Here
- `Game.cs`: protobuf message types.
- `GameGrpc.cs`: gRPC service client/server stubs.

## How It Connects
- Used by Unity runtime (`Assets/Scripts/CodeExecutor.cs`, `Assets/Rpc/GrpcBridge.cs`).
- Must stay synchronized with Java generated classes from the same proto.

## Runtime Role (LV1/LV2/Server)
- Serialization and RPC client contract for Unity side.

## Dependencies
- Generated from root `proto/game.proto`.

## Notable Flows
- Any proto schema change requires regeneration before runtime testing.

## Maintenance Notes
- Generated code: treat as build artifact, not source-of-truth.
