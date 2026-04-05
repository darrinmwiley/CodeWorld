# Directory Purpose
Dialogue/terminal presentation assets used for narrative-style text interactions.

## What Lives Here
- Dialog terminal prefab/UXML/USS.
- `TerminalDialog.cs` runtime controller.

## Key Files
- `TerminalDialog.cs`: controls line display/terminal behavior for dialogue moments.

## How It Connects
- Used by level narrative managers (LV1/LV2) when dialogue is surfaced through terminal UI.

## Runtime Role (LV1/LV2/Server)
- Unity-side narrative UI component (no direct Java server calls).

## Dependencies
- UI Toolkit panel settings and style sheets.

## Notable Flows
- Narrative event -> terminal prefab activated -> lines rendered to player.

## Maintenance Notes
- Keep narrative rendering behavior here; keep level trigger logic in level folders.
