# Test Plan (MVP)

## Functional Smoke
- Sailing: throttle changes speed, rudder turns
- Wind: changing wind dir changes best angle of sail
- Combat: Q/E fire respective broadsides, projectiles hit and damage
- Death: ship sinks at 0 HP

## Balance Checks
- Time-to-kill: 20–40s vs sloop
- Turning: figure-8 within 60–90s at half-sail

## Performance
- 10 enemy ships in scene ≥ 60 FPS on target GPU (tune later)
- GC spikes < 1ms during combat

## Automation (lightweight)
- PlayMode test: Damageable reduces health and fires OnDeath
- EditMode test: Polar curve returns higher speed at ~60° than 0°/180°