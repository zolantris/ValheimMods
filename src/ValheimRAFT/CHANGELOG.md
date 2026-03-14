## Changelog

For full release notes see
the [GitHub Releases](https://github.com/zolantris/ValheimMods/releases) page.

### [4.2.0]

#### Added

- Vehicle Name field — set a custom name per-vehicle saved to ZDO, shown in the
  vehicle config panel and on the minimap pin as `V:<name>`.
- `AddTextInputRow` UI helper — reusable TMP text input that scales to its
  container, with `onChanged` and `onEndEdit` callbacks.
- Custom minimap pin icon for all vehicles types (land, water, air).

#### Fixed

- Input text colour in the vehicle config panel was rendering white-on-white due
  to inheriting `LabelColor` instead of `InputTextColor`.

---

### [4.x.x]

#### Added

- Power System: engines, batteries, pylons, conduit plates, and swivels (
  see [Power System Guide](./docs/tutorials/Tutorial_PowerSystem.md)).
- Pylons for extending power networks across distance (BETA).
- Eitr Power Generator and Eitr Power Storage prefabs.
- Drain/Charge Activator Plates.
- Mechanism Toggle (mechanism switch) for controlling swivels and opening the
  vehicle debug panel.

#### Changed

- Swivels now require power from a connected power network by default.

---

### [3.x.x]

#### Added

- Swivels (`v3.4.0+`) — rotate or move any prefab via mechanism toggles.
- Cannons (fixed, `v3.6.x+`) — placeable on any vehicle or base, controlled from
  the wheel or Cannon Control Center.
- Cannon Control Center — telescope-style controller for fixed cannon groups.
- Vehicle Storage / Save & Spawn system (`v3.3.0+`).

#### Changed

- Vehicle commands interface overhauled (
  see [Vehicle Commands Tutorial](./docs/tutorials/Tutorial_VehicleCommandsInterface.md)).

---

### [2.x.x]

#### Added

- V2 WaterVehicle hull system — hull pieces affect floatation dynamically.
- Underwater vehicle travel (`>=2.4.0`).
- Anchors (`v2.5.0+`) — physically anchor the ship when the anchor touches land.
- Window Porthole walls and floors (`v2.5.0+`).
- PlanBuild mod support (`>=1.6.0`).
- Vulkan graphics support (`>=1.6.11`).

#### Fixed

- Ship loading and threading optimisation for dedicated servers (`>=1.6.2`).

---
