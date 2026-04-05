# Directory Purpose
Reusable draggable/resizable window UI assets and manipulators.

## What Lives Here
- Base 1-pane/2-pane prefabs.
- Shared window UXML/USS templates and icons.
- `UiResizableManipulator.cs` for resize interactions.

## Key Files
- `UiResizableManipulator.cs`: low-level drag/resize input handling for panel borders/handles.

## How It Connects
- Composed by higher-level IDE/window controllers in `Assets/_UI/IDE`.

## Runtime Role (LV1/LV2/Server)
- Shared UX infrastructure for in-world windows.

## Dependencies
- UI Toolkit pointer/manipulator event model.

## Notable Flows
- Pointer drag events update panel geometry for runtime window resizing.

## Maintenance Notes
- Prefer keeping generic window visuals/behaviors here instead of duplicating in IDE folder.
