# Directory Purpose
Java gRPC backend that compiles/runs player Java code and streams execution events to Unity.

## What Lives Here
- Gradle build/app configuration.
- Java source under `src/main/java/com/codeworld/server`.
- Wrapper scripts (`gradlew`, `gradlew.bat`).

## Key Files
- `build.gradle`: gRPC/protobuf plugins + dependencies; proto source points to `../proto`.
- `settings.gradle`, `gradlew*`: project/run tooling.

## How It Connects
- Reads shared proto contract from root `proto/`.
- Hosts `CodeWorldServiceImpl` consumed by Unity gRPC clients.

## Runtime Role (LV1/LV2/Server)
- Server-side execution engine and object-lookup broker for LV2 Java interactions.

## Dependencies
- Java toolchain, gRPC Java libs, protobuf Gradle plugin.

## Notable Flows
- `./gradlew run` -> starts server on `50051` -> Unity `CodeExecutor` connects and opens duplex streams.

## Maintenance Notes
- Keep Java package names and proto options aligned with generated code layout.
