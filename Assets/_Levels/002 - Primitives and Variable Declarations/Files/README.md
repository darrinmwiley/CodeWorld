# Directory Purpose
LV2 starter/reference files used by the in-world IDE and coding exercises.

## What Lives Here
- Example `.txt` lesson files (`HelloWorld`, `PrimitiveDeclarations`, `DigitalLogic`, etc.).
- Java sample/source files (`PrinterController.java`, `PrinterController.java.txt`).
- `level 2 fileset.asset` defining which files appear in the IDE.

## Key Files
- `level 2 fileset.asset`: ScriptableObject consumed by `ConsoleLevelFileSet`.

## How It Connects
- Loaded by `Assets/_UI/IDE/ConsoleFileSessionManager` and file explorer UI.
- Drives the editable tabs/content surfaced during LV2.

## Runtime Role (LV1/LV2/Server)
- LV2 content payload for player-facing coding tasks.

## Dependencies
- `ConsoleLevelFileSet` and tab/file components in `Assets/_UI/IDE`.

## Notable Flows
- Level boot -> file set loads -> files appear in tabbed console -> player edits/runs code.

## Maintenance Notes
- Keep pedagogical file names stable unless level UI references are updated too.
