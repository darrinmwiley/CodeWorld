# Directory Purpose
Shared UI Toolkit prefabs, UXML/USS assets, and runtime controllers for in-world interfaces.

## What Lives Here
- `IDE/` multi-pane code editor window system.
- `DialogTerminal/` narrative/dialog terminal UI.
- `DraggableWindow/` reusable draggable/resizable window assets.
- `Printer2Pane/` printer-focused two-pane UI variant.
- Theme assets (`UITheme`, `UIThemeController`).

## Key Files
- `UITheme.cs` / `UIThemeController.cs`: centralized UI theming.

## How It Connects
- LV1/LV2 scenes instantiate these UI assets.
- `IDE/` components interact with `Assets/_Scripts` state/renderer logic and `Assets/Scripts/CodeExecutor`.

## Runtime Role (LV1/LV2/Server)
- Primary presentation layer for code editing, terminal output, and window interactions.

## Dependencies
- Unity UI Toolkit (`UIDocument`, UXML/USS, VisualElement).

## Notable Flows
- Scene starts -> UI documents initialize -> controllers bind runtime systems and file sets.

## Maintenance Notes
- Keep generic UI systems here; level-specific UI logic should stay in level folders.
