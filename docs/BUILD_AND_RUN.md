# Build & Run

## Requirements
- Unity 2023 LTS or Unity 6-compatible editor with URP
- Packages:
  - Input System
  - (Optional) Crest or Boat Attack for water

## URP Setup
- Enable Depth and Opaque Texture
- Set HDR on main camera
- Set MSAA 2x/4x (tune later)

## Steps
1) Open Scenes/Sandbox/Sea_Test.unity
2) Ensure Player prefab in scene with Rigidbody + scripts
3) Press Play
4) For build: File → Build Settings → PC → Build

## Troubleshooting
- Boat sinking? Raise Buoyancy displacement or lower Rigidbody mass.
- No input? Ensure Input System installed or enable Old Input in Project Settings.