# Changelog

All notable changes to ValheimRAFT are documented here.
For full commit history see
the [GitHub Releases](https://github.com/zolantris/ValheimMods/releases) page.

---

## [4.2.0] — main branch

### Added

- Vehicle Name field — set a custom name per-vehicle, saved to ZDO and shown on
  the minimap pin as `V:<name>`.
- Custom minimap pin icons for all vehicle types (water, land, air) sourced from
  the asset bundle.
- `AddTextInputRow` UI helper — reusable TMP input field that scales to its
  container, with `onChanged` and `onEndEdit` callbacks.
- Documentation overhaul — new tutorials for Basics, Cannons, Power System, and
  a Changelog.

### Fixed

- Map pin sync stability improvements and NRE guards on server reconnect.
- Vehicle config panel input text rendering white-on-white (was inheriting label
  colour instead of input text colour).
- Server sync and GUI inconsistencies across clients.

---

## [4.1.3] — 2025-12-04

### Fixed

- Archive step in the release build pipeline.

---

## [4.1.2] — 2025-12-03

### Fixed

- Optimised `HumanoidOnAttackTrigger` patch to better support external mods that
  hook the same method.
- Versioning chores and dependency bump to Jotunn 2.27.

---

## [4.1.1] — 2025-10-20

### Fixed

- Sail normals rendering incorrectly on some mesh variants.
- Flight stability issues when sails were attached to a flying vehicle.

---

## [4.1.0-beta.1] — 2025-10-14

### Added

- Vehicle chunk boundary limits — constrain the physics bounds of a vehicle to a
  defined area using Boundary Chunk Addition prefabs.

---

## [4.0.4] / [4.0.3] — 2025-10-12

### Fixed

- Removed the 4×4 Dirt Planter prefab which was accidentally included in the
  release build.

---

## [4.0.2] — 2025-10-12

### Added

- Vehicle boundary safety — prevents hull collision bounds from expanding
  dangerously when pieces are placed at extreme offsets.

---

## [4.0.1] — 2025-10-03

### Fixed

- Unity 6 compatibility for Water Vehicle ships and Air Vehicles.

---

## [4.0.0] — 2025-08-25

### Added

- Full Unity 6 compatibility pass across all vehicle types.
- Power System: Eitr Power Generator, Eitr Power Storage (battery), Power
  Pylons (BETA), and Drain/Charge Activator Plates.
- Swivels — rotate or move any placed prefab using Mechanism Toggle triggers.
  Requires power by default.
- Mechanism Toggle (mechanism switch) — configure swivels, open the vehicle
  debug panel, and trigger docking sequences.

### Changed

- Swivels now require a connected power network and charge to activate (
  configurable).

---

## [3.7.2] — 2025-08-24

### Added

- Prefab naming snapshots — prefab names are now stable across saves.
- Various quality-of-life improvements to piece placement and snapping.

---

## [3.7.1-beta] — 2025-08-16

### Added

- New hull pieces and updated existing hull geometry.

---

## [3.6.6] — 2025-07-31

### Fixed

- Steering wheel not detecting the player when standing at certain positions.

---

## [3.6.5] — 2025-07-30

### Fixed

- Null reference exception on equip patch affecting some item interactions.

---

## [3.6.4] — 2025-07-26

### Added

- Cannonballs (solid bronze, explosive blackmetal) added to the items section.

### Fixed

- Multiple stability fixes across the 3.6.x cannon release series.

---

## [3.6.3-beta] / [3.6.1-beta.2] / [3.6.0-beta.1] — 2025-07-21

### Added

- Fixed Cannons — mount to any vehicle or base. Controlled from the Steering
  Wheel on a vehicle or from the Cannon Control Center off-vehicle.
- Cannon Control Center (telescope) — fire-control station for fixed cannons.
  Manages all cannons within its discovery radius (default 15 units). Supports
  four directional groups (forward / port / starboard / aft).
- Handheld Cannon — mobile cannon with auto-aim mechanics. Fully multiplayer
  compatible.
- Cannonball ammunition — solid (piercing) and explosive (AoE blackmetal)
  variants.

---

## [3.5.4] — 2025-07-03

### Fixed

- Compile error in swivel stability changes.

---

## [3.5.3] — 2025-06-24

### Fixed

- Missing prefabs added back to the build.
- Scale of corner floor hull pieces corrected.

---

## [3.5.2] — 2025-06-23

### Added

- Improved vehicle docking — child vehicles now align anchor-to-anchor with lerp
  smoothing.
- Mast ZDO syncing across clients.

### Fixed

- Sail mesh and rotation sync issues.

---

## [3.4.3] — 2025-05-30

### Fixed

- Eitr Drain Conduit multiplayer sync issues.
- Swivel multiplayer sync reliability.
- Power network logic sync across clients.

---

## [3.4.0-beta.1] — 2025-05-10

### Added

- Swivel GUI improvements — configure rotation axes, speed, and power
  requirements from a dedicated panel.
- Swivel rotators with visible connection lines.

---

## [3.3.0] — 2025-04-29

### Added

- Custom masts with crow's nest — tier 1–3 mast variants with a climbable crow's
  nest platform.
- Vehicle Save & Spawn system — save a vehicle blueprint to disk and spawn it
  later anywhere.
  See [Vehicle Storage Tutorial](./docs/tutorials/Tutorial_VehicleStorage.md).

---

## [3.2.1] — 2025-04-28

### Added

- Russian and Chinese Simplified translations.

---

## [3.2.0] — 2025-04-28

### Added

- Vehicle Hammer — dedicated build tool replacing the vanilla hammer for all
  ValheimRAFT pieces. All vehicle prefabs now live under its tabbed build menu.
- Custom floatation height per vehicle — set a fixed height or use the
  Floatation Prefab for per-vehicle control.
- Toggle switch persistence — mechanism switch state is now saved to ZDO.

### Changed

- Removed V1 raft support. Use `vehicle recover` to migrate old V1 rafts.
- `Custom` floatation mode is now locked to local vehicles only to prevent sync
  bugs on shared ships.

---

## [3.1.0] — 2025-04-04

### Added

- Collision object parenting optimisations — large ships with many pieces load
  faster.

---

## [3.0.8] / [3.0.7] / [3.0.5] / [3.0.4] — 2025-04-01

### Fixed

- NRE in collection rebuild bounds on ship load.
- Convex hull optimisation — large ships compute collision bounds significantly
  faster.
- Valheim game update regressions.
- Land vehicle stability and tread rotation logic.

---

## [3.0.0-beta] — 2025-02-01

### Added

- Land Vehicle (tank) — wheeled/tread ground vehicle. Drive it overland or load
  it onto a water vehicle for transport.
- Tank tread rotation logic — treads animate based on vehicle velocity and
  turning.
- Boundary alignment tools for large vehicle builds.

---

## [2.5.3] — 2025-01-20

### Added

- Anchor HUD — shows anchor state on screen when near your vehicle.

### Fixed

- Stability tweaks for anchor physics and multiplayer sync.

---

## [2.5.0-beta.1] — 2025-01-12

### Added

- Anchors — physically anchor the ship when the anchor touches the seabed or
  terrain. Synced across clients. Requires swamp-tier materials.
- Window Porthole walls and floors (2×2, 4×4, 6×4, 8×4 iron variants) — see
  through hull sections with half the health of solid iron pieces.
- New hull geometry: corner floors (2×2, 2×4, 2×8 left/right wood and iron).

---

## [2.4.3] — 2024-11-30

### Fixed

- Player control responsiveness tweaks while helming a vehicle.
- Ship lean cosmetics towards wind direction.
- Boat ownership transfer stability.

---

## [2.4.2] — 2024-11-24

### Fixed

- Water damping minimum and maximum values clamped correctly — ships no longer
  oscillate uncontrollably in rough weather.

---

## [2.4.1] — 2024-11-11

### Fixed

- Auto-ascend/descend logic for flying ships.
- Target height offset when flying causing incorrect vertical positioning.

---

## [2.4.0] — 2024-11-10

### Added

- Water ballast mass system — hull pieces affect buoyancy dynamically.
- Flight mode — vehicles can be made to fly using ballast controls.
- Vehicle map pins — vehicles now show on the minimap.
- Translocations (teleport vehicle to player position, admin only).
- Rope Ladder — climbable ladder that extends to the ground or seafloor
  automatically.

---

## [2.4.0-beta.1] — 2024-11-09

### Added

- Water mesh masking (water opacity bucket) — hide water inside or around a
  sealed hull to create submarine-like interiors.
- Dynamic location system for world-space vehicle placement.

---

## [2.2.4] / [2.2.3] / [2.2.2] — 2024-08-25
 
### Fixed

- Ship floatation logic regressions.
- Flying vehicle height and ballast stability.
- Sail asset and ladder tweaks.
- Hull piece sizing corrections.

---

## [2.2.0] — 2024-07-06

### Added

- Fully sealable hull — new rib, corner, prow, and seal pieces allow building a
  watertight ship.
- Yggdrassil iron reinforced prefab variants for hull walls and floors.

### Fixed

- Rudder turning speed optimisations.
- Sail rotation, physics, and anchor behaviour tweaks.
- Many hull wall and floor collision bug fixes.

---

## [2.1.1] / [2.1.0] — 2024-05-22

### Fixed

- Sail 3-point mesh not rendering correctly.
- `raftcreative` rotation fix — no longer rotates the vehicle unexpectedly.
- Multiplayer RPC stability improvements.

---

## [2.0.3] / [2.0.2] / [2.0.1] / [2.0.0] — 2024-05-17

### Added

- V2 WaterVehicle — completely rewritten vehicle system with proper physics, ZDO
  syncing, and hull floatation.
- `vehicle recover` command — migrates V1 raft pieces to the new V2 system.
- Boarding Ramp — extendable ramp (1–50+ units) for boarding ships from the
  water or shore.
- Dirt Floor (1×1, 2×2) — grow vanilla Valheim crops on your ship.

### Changed

- Plugin GUID changed from the original ValheimRaft GUID to
  `zolantris.ValheimRAFT`. Existing V1 ships must be recovered with
  `vehicle recover`.

### Fixed

- ZDO logging spam on rope anchors removed.
- Multiplayer ship sync regressions from 2.0.x patch series.

---

## [1.6.14] — 2024-01-21

### Fixed

- Local server (LAN host) ship rendering on connecting clients.

---

## [1.6.13] — 2024-01-20

### Fixed

- Rendering regression introduced in 1.6.12 for locally hosted servers.

---

## [1.6.12] — 2024-01-07

### Fixed

- Nexus/Vortex folder structure install path.

---

## [1.6.11] — 2023-12-30

### Added

- Vulkan graphics API support (Windows and Linux).

### Fixed

- Collider placement moved below the lowest deck to stop clutter items flipping
  the boat.
- Config options added for collider sensitivity.

---

## [1.6.9] / [1.6.10] — 2023-12-30

### Fixed

- Anchor now correctly stops a flying raft.
- Sail component NRE guards to prevent log spam.

---

## [1.6.7] / [1.6.8] — 2023-12-29

### Added

- Boat size-based sailing calculations — larger ships are proportionally faster.
- Admin-only build mode config option.

### Fixed

- Thunderstore folder structure for manual installs.

---

## [1.6.5] / [1.6.6] — 2023-12-27

### Fixed

- Sail mesh generation errors.
- Rope anchor duplicate WearNTear NRE spam.

---

## [1.6.4] — 2023-12-26

### Added

- Sail area calculations — sail speed is now based on the cubic volume of each
  sail.

---

## [1.6.3] — 2023-12-24

### Fixed

- Stable dedicated server and client raft rendering.

---

## [1.6.2] — 2023-12-21
 
### Fixed

- Server stability for zone-based raft updates.
- Plugin patch compatibility fixes for mods hooking ValheimRaft classes.

