# Directory Purpose
Level 1 (Computational Thinking) content: intro narrative flow, face/character interaction, and turtle puzzle setup.

## What Lives Here
- `Intro Scene.unity`
- Level-local scripts (`NarrativeManager`, `FacePowerClickController`, `QuadFaceAnimator`)
- `TurtleGame/` subfolder with LV1 puzzle mechanics and art assets.

## Key Files
- `NarrativeManager.cs`: controls introduction/victory dialogue sequencing.
- `FacePowerClickController.cs`: bridges face power click input to level events.
- `QuadFaceAnimator.cs`: visual state machine for the face character.

## How It Connects
- Uses shared interaction/input systems from `Assets/_Scripts`.
- Uses shared UI and console systems where needed (`Assets/_UI`, `Assets/_UI/IDE`).
- LV1 progression and puzzle outputs can feed player understanding for LV2 coding workflows.

## Runtime Role (LV1/LV2/Server)
- Primary runtime container for LV1 scene behavior and sequencing.

## Dependencies
- Unity scene objects/prefabs/materials.
- Shared gameplay/controller components in root `Assets` and `_Scripts`.

## Notable Flows
- Face power-on triggers intro dialogue sequence.
- Turtle puzzle interactions run inside `TurtleGame/` components.

## Maintenance Notes
- Keep LV1-specific logic here; move reusable utilities to shared folders.
