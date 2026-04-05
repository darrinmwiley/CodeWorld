# Directory Purpose
Shared Unity gameplay/runtime scripts used across levels and systems.

## What Lives Here
- Core interaction/input abstractions (`BaseClickable`, listeners, focus helpers).
- Console editing state/transactions (`ConsoleState`, `InsertTransaction`, `DeleteTransaction`, etc.).
- Puzzle/gameplay controllers (line logic gates, light triggers, conveyor/laser utilities).
- Syntax highlighting support (`SyntaxHighlighting/`) and ANTLR lexer/parser assets (`Antlr/`).

## Key Files
- `OutputConsoleController.cs`, `Printer.cs`: output/printing surface used by code execution UX.
- `ConsoleState.cs` + transaction classes: text buffer edit model.
- `FocusManager.cs`: focus routing for interactive windows/components.

## How It Connects
- Consumed by level-local scripts in `Assets/_Levels/*`.
- Powers editor components in `Assets/_UI/IDE`.
- Supports `Assets/Scripts/CodeExecutor.cs` runtime interactions.

## Runtime Role (LV1/LV2/Server)
- Shared in-Unity systems layer for LV1/LV2 gameplay and code UI behavior.

## Dependencies
- Unity MonoBehaviour/UI Toolkit APIs.
- Optional ANTLR-generated parser/lexer files for syntax processing.

## Notable Flows
- User input -> transaction/state updates -> renderer/UI refresh.
- Focus and click routing unify interactions across in-world panels.

## Maintenance Notes
- Prefer adding reusable behavior here rather than duplicating in level folders.
