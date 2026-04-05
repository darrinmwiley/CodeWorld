# Directory Purpose
In-world IDE runtime: tabbed editor, file explorer, console rendering, pane/window composition, and toolbar behaviors.

## What Lives Here
- Console document state/input/render pipeline (`ConsoleStateManager`, `ConsoleInputManager`, `ConsoleRenderer`).
- File/session integration (`ConsoleLevelFileSet`, `ConsoleFileSessionManager`, `FileHeirarchyComponent`).
- Window/pane orchestration (`WindowController`, `MultiPaneWindowController`, `TabbedConsoleWindowController`).
- UXML/USS layouts for tabs, panes, toolbar, and option bar.

## Key Files
- `ConsoleLevelFileSet.cs`: level-provided file catalog model.
- `TabbedConsoleWindowController.cs`: runtime tab lifecycle.
- `ConsoleWindowController.cs`: bridges UI container to renderer/state manager.

## How It Connects
- Consumes level file sets from `Assets/_Levels/002 - Primitives and Variable Declarations/Files`.
- Uses shared editing/input logic in `Assets/_Scripts`.
- Execution actions route to `Assets/Scripts/CodeExecutor.cs`.

## Runtime Role (LV1/LV2/Server)
- Core player coding interface, primarily exercised by LV2.

## Dependencies
- UI Toolkit runtime + shared gameplay/editor services.

## Notable Flows
- File set injected -> explorer/tree builds -> tabs open docs -> execute action sends active code to server pipeline.

## Maintenance Notes
- Consider this folder the canonical source for IDE UX behavior.
