# Directory Purpose
Core backend service and execution runtime for CodeWorld Java integration.

## What Lives Here
- `CodeWorldServer`: gRPC server bootstrap.
- `CodeWorldServiceImpl`: duplex streaming service implementation.
- `DynamicExecutionEngine`: compile/run dynamic Java code.
- `ExecutionContext`: per-execution thread context and pending object-query futures.
- `GameUtils`: Java API helper (`findObjectInLevel`).
- `Printer` + `PrinterProxy`: player-facing API and Unity-directed proxy implementation.
- `CompilationException`: compile diagnostics wrapper.

## Key Files
- `CodeWorldServiceImpl.java`
- `DynamicExecutionEngine.java`
- `GameUtils.java`
- `PrinterProxy.java`

## How It Connects
- Consumes generated proto classes in `com.codeworld.generated`.
- Exchanges stream events with Unity `Assets/Scripts/CodeExecutor.cs`.
- Uses `FindObjectQuery`/`FindObjectResult` roundtrip to resolve Unity object pointers.

## Runtime Role (LV1/LV2/Server)
- Server-side brain for LV2 code execution and command/event forwarding.

## Dependencies
- gRPC StreamObserver APIs.
- JDK compiler APIs for dynamic compilation.

## Notable Flows
- Receive Java source -> compile/reflective run -> emit print/error/completion events.
- Emit object query -> wait on `ExecutionContext` future -> construct proxy bound to Unity object ID.

## Maintenance Notes
- Any new player API (e.g., new interactable object type) requires coordinated updates in proto, Java proxies/helpers, and Unity dispatch handling.
