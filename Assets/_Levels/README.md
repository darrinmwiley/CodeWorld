# Directory Purpose
Level-specific content for current playable curriculum slices (LV1 and LV2).

## What Lives Here
- `001 - Computational Thinking/` (LV1)
- `002 - Primitives and Variable Declarations/` (LV2)
- A UI design test scene folder used for experimentation.

## Key Files
- Level `.unity` scenes plus per-level narrative and interaction scripts.

## How It Connects
- Level folders hold scene-local scripts, dialogue, and assets.
- Levels rely on shared systems in `Assets/_UI/IDE`, `Assets/_Scripts`, `Assets/Scripts`, and `Assets/Rpc`.

## Runtime Role (LV1/LV2/Server)
- Primary entry point for level-specific gameplay behavior and sequencing.

## Dependencies
- Unity scene loading + GameObjects/prefabs.
- Shared gameplay/UI/editor systems and RPC bridge.

## Notable Flows
- LV narrative scripts trigger dialogue and progression events.
- LV2 uses level file assets that populate the in-world IDE/editor experience.

## Maintenance Notes
- Keep shared logic out of this folder when possible; place reusable systems in shared directories.


