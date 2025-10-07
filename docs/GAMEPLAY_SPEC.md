# Gameplay Spec (MVP)

## Core Loop
Sail → Hunt/Engage → Loot → Repair/Upgrade → Repeat

## Player Controls (MVP)
- Throttle (sails): W/S or Gamepad RT/LT
- Rudder (steer): A/D or Gamepad Left Stick X
- Fire Port Cannons: Q / LB
- Fire Starboard Cannons: E / RB
- Spyglass: Right Mouse
- Pause/Menu: Esc

See exact bindings in docs/INPUT_MAP.md

## Ships (MVP)
- Player: Brig (medium handling)
- Enemies: Sloop (fast), Gunboat (glass cannon)

## Combat (MVP)
- Broadside cannons, arc-limited
- Hull health, chance to set fire (later)
- Simple AI: pursue, position for broadside, retreat at low HP

## Progression (MVP)
- Gold from loot
- Upgrades: hull plating, reload speed, sail quality (data via ScriptableObjects)

## Metrics (Tuning Targets)
- Top speed (beam reach, 12–14 knots equivalent)
- Turn radius ~3–5 ship lengths at half-sail
- Cannon range ~150–200m, time-to-kill single sloop 20–40s