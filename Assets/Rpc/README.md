# Directory Purpose
Unity gRPC integration layer and generated protocol artifacts.

## What Lives Here
- `GrpcBridge.cs`: duplex stream sample/heartbeat + color update demo.
- `Generated/`: protobuf + gRPC C# classes generated from `proto/game.proto`.
- Helper subfolders for language/codegen artifacts.

## Key Files
- `GrpcBridge.cs`
- `Generated/Game.cs`
- `Generated/GameGrpc.cs`

## How It Connects
- `Assets/Scripts/CodeExecutor.cs` and other runtime code depend on generated message/service types.
- Regenerated whenever `proto/game.proto` changes.

## Runtime Role (LV1/LV2/Server)
- Contract binding layer between Unity runtime and Java server.

## Dependencies
- Protobuf compiler + gRPC C# plugin workflow (`rebuild-proto.ps1` / manual protoc command).

## Notable Flows
- Proto edit -> regenerate `Generated/*` -> Unity runtime compiles against updated contract.

## Maintenance Notes
- Do not hand-edit generated files in `Generated/`; regenerate instead.
