# Technical Design

## Architecture
- Managers: WindSystem, TimeOfDay (later), GameEvents
- Systems: Sailing (ShipController + Buoyancy + SailDrive), Combat (CannonBattery + Projectile), Damage (Damageable), AI (ShipAI placeholder)
- Data: ScriptableObjects (ShipStats, WeaponStats)
- Scenes: Sandbox (tests), World (main), Additive sub-scenes for islands/POIs

## Water Abstraction
interface IWaterProvider {
  bool SampleHeightAndNormal(Vector3 pos, out float height, out Vector3 normal);
}
- SimpleWaterProvider: dev-only sine/Gerstner
- Integrations:
  - Crest: use SampleHeight functions
  - Boat Attack: adapt to provided API

## Physics
- Rigidbody boats, center of mass tuned low
- Buoyancy via multiple float points (AddForceAtPosition)
- Sail drive from polar curve approximations (relative wind angle)
- Rudder applies yaw torque with speed-scaled effectiveness

## Messaging
- Lean on C# events / UnityEvents for simple decoupling (OnSank, OnFired, OnHit)

## Performance
- LOD for distant ships
- URP batching, GPU instancing for cannonballs
- Fixed Timestep 0.02–0.033s; evaluate stability in storms

## Code Style
- PascalCase types/properties, camelCase fields, _prefix for private serializables
- One responsibility per component