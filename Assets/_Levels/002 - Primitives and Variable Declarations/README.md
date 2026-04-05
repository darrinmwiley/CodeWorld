# Directory Purpose
Level 2 (Primitives and Variable Declarations) scene content, narrative flow, and level-local UI interaction scripts.

## What Lives Here
- `002 - Primitives and Variable Declarations.unity` (LV2 scene)
- Level-local scripts (`Level2NarrativeManager`, `UiDraggableManipulator`)
- Materials/models used by LV2 interactions.
- `Files/` subfolder containing starter/source files for the in-world IDE.

## Key Files
- `Level2NarrativeManager.cs`: LV2 dialogue/progression sequencing.
- `UiDraggableManipulator.cs`: level-scoped drag interaction behavior.

## How It Connects
- Uses shared editor/window runtime in `Assets/_UI/IDE`.
- Uses shared code execution/RPC pipeline in `Assets/Scripts`, `Assets/Rpc`, `proto`, and `java-server`.
- Uses shared input/gameplay helpers from `Assets/_Scripts`.

## Runtime Role (LV1/LV2/Server)
- Primary LV2 scene package where Java coding interactions are presented to the player.

## Dependencies
- Level file set assets from `Files/`.
- Shared console/editor rendering and state systems.

## Notable Flows
- LV2 loads starter files into the UI IDE and drives narrative prompts while code is authored/executed.

## Maintenance Notes
- Keep level-specific scripts here; move reusable editor/gameplay behavior to shared folders.
