# Project Crimson Wake

Single-player naval pirate game built in Unity (URP). Focused on wind-driven sailing, tactical broadsides, and exploration.

## Quickstart
1) Clone and open in Unity (2023 LTS or newer / Unity 6 compatible).
2) Open Scene: `Scenes/Sandbox/Sea_Test.unity`
3) Press Play (W/S throttle sails, A/D rudder, Q fire port, E fire starboard)

## Tech
- Unity + URP
- Optional water: Boat Attack, Crest, or custom (abstracted via IWaterProvider)
- Input: Unity Input System (fallback to old Input for prototype)

## Branching
- main: stable
- dev: integration
- feature/*: per feature
- hotfix/*: critical fixes

## Folders
- Assets/_Core: scripts, data, prefabs
- Assets/_Content: art/audio
- Assets/_ThirdParty: external assets
- Assets/_Sandbox: test scenes
- docs/: design and engineering docs

## Build Targets
- PC (Windows) MVP; other targets later.

## Contacts
- Issues: GitHub Issues
- Roadmap: docs/TECH_DESIGN.md and docs/GAMEPLAY_SPEC.md