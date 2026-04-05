# Directory Purpose
Top-level Unity scenes used for gameplay, UI testing, and RPC experiments.

## What Lives Here
- Main/experimental scenes (`Code Kingdom`, `UI Test Scene`, `Text Editor Test Scene`, etc.).
- `RPC Scene/` baked lighting/profile assets for RPC demo scene.

## Key Files
- `Code Kingdom.unity` (likely principal gameplay scene hub).
- `RPC Scene` assets for network demo setup.

## How It Connects
- Level folders under `Assets/_Levels` hold many level-specific scenes/scripts; this folder contains broader/testing scenes.

## Runtime Role (LV1/LV2/Server)
- Scene entry points for testing and integration of UI/runtime systems.

## Dependencies
- Shared systems in `_UI`, `_Scripts`, `Scripts`, `Rpc`, and level content.

## Notable Flows
- Scene selection determines which subset of LV systems are active during play mode.

## Maintenance Notes
- Keep test scenes named clearly to avoid confusion with curriculum progression scenes.
