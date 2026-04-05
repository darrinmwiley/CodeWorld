# Directory Purpose
LV1 turtle puzzle implementation and supporting assets.

## What Lives Here
- Turtle command scripts (`TurtleCommander`, `TurtleCommand`, `TurtleButton`, `CommandDisplay`, `GraphingUtility`)
- Puzzle control textures/materials/icons.

## Key Files
- `TurtleCommander.cs`: orchestrates command list execution and turtle movement flow.
- `TurtleCommand.cs`: command representation used by puzzle runtime.
- `TurtleButton.cs`: UI/button input for selecting/issuing turtle commands.
- `CommandDisplay.cs`: visual command list feedback.

## How It Connects
- Embedded by LV1 scene in parent directory.
- Interacts with LV1 narrative manager for intro/win pacing.

## Runtime Role (LV1/LV2/Server)
- LV1-only puzzle subsystem (no direct Java server dependency).

## Dependencies
- Unity transforms, UI/input events, and level scene references.

## Notable Flows
- Player issues directional commands -> command queue/display updates -> turtle executes path.

## Maintenance Notes
- Keep puzzle-specific logic here; avoid mixing with generic editor/RPC systems.
