# ValheimRAFT — Basics

A quick-start guide to building and sailing your first vehicle.

---

## Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [The Vehicle Hammer](#the-vehicle-hammer)
- [Building Your First Water Vehicle](#building-your-first-water-vehicle)
- [Steering and Sailing](#steering-and-sailing)
- [Sails](#sails)
- [Anchors](#anchors)
- [Hull and Floatation](#hull-and-floatation)
- [Creative Mode](#creative-mode)
- [Config](#config)
- [Vehicle Hauling](#vehicle-hauling)
- [Common Problems](#common-problems)
- [Further Reading](#further-reading)

---

## Requirements

- Valheim (latest version)
- [BepInEx 5.x](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)
- ValheimRAFT (this mod)

> **Recommended:** install everything
> through [r2modman](https://thunderstore.io/package/ebkr/r2modman/) to avoid
> manual path mistakes.

---

## Installation

1. Install r2modman and create a Valheim profile.
2. Search for **ValheimRAFT** and install it — r2modman will pull in Jotunn and
   BepInEx automatically.
3. Launch Valheim through r2modman (use the **Start modded** button).

### Manual installation (not recommended)

- Place the `zolantris-ValheimRAFT` folder inside
  `<ValheimGame>/BepInEx/plugins/`.
- The folder **must** be named `ValheimRAFT` or `zolantris-ValheimRAFT` and
  contain an `Assets` subfolder — this is required for custom sail textures to
  load.

---

## The Vehicle Hammer

The **Vehicle Hammer** is the primary build tool for all ValheimRAFT prefabs.

- Craft it from the workbench once you have the mod installed.
- Equip it like a normal hammer — right-click opens the build menu.
- The menu is split into tabs: **Hull**, **Sails**, **Rigging**, **Mechanisms**,
  **Utility**, and more.
- All vehicle pieces must be placed while the Vehicle Hammer is equipped.

> You cannot use the vanilla hammer to place ValheimRAFT pieces on a vehicle.

---

## Building Your First Water Vehicle

### 1. Place the Water Vehicle in the water

Find open water. Open the build menu and place a **Water Vehicle** prefab. This
is the invisible physics anchor every other piece attaches to — place it on the
water surface.

[Image: Water Vehicle prefab placement](https://raw.githubusercontent.com/zolantris/ValheimMods/main/docs/images/tutorial_basics/water_vehicle_placement.png)

### 2. Add a Floor

Switch to the **Hull** tab and place hull floor pieces on top of the vehicle
core. Wood hull pieces are available early; iron hull pieces become available
after smelting iron

### 3. Add a Steering Wheel

Place a **Steering Wheel** on the deck. The direction the wheel faces becomes
the forward direction of the vehicle. You must have a wheel to sail.

### 4. Add at Least One Sail

Place any sail mast on the deck. Without a sail the vehicle will not move under
wind power. See [Sails](#sails) for tier details.

### 5. Board and Sail

Walk up to the steering wheel and press **[Use]** (`E` by default) to take the
helm. The vehicle is now under your control.

## Propulsion Options

- **Wind power** — place sails to harness the wind. The more and larger the
  sails, the faster you go.
- **Engines** — Currently the user can modify valheimraft.cfg file to set values
  for the mid and full speed so that the vehicle is much faster and goes up
  wind. This is not realistic, but it is fun. Balanced Engine pieces might be
  introduced in a future update.

Example config without rudder speed
[RudderSpeed](https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/docs/assets/engine_speed_config_default.png)

```yaml
## Set the Slow speed of rudder, this will not apply sail speed.
# Setting type: Single
# Default value: 5
# Acceptable value range: From 2 to 20
Rudder Slow Speed = 5

  ## Set the Half speed of rudder, this will apply additively with sails
  # Setting type: Single
  # Default value: 0
  # Acceptable value range: From 0 to 100
Rudder Half Speed = 0

  ## Set the Full speed of rudder, this will apply additively with sails
  # Setting type: Single
  # Default value: 0
  # Acceptable value range: From 0 to 100
Rudder Full Speed = 0
```

Example config with rudder speed
[RudderSpeed](https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/docs/assets/engine_speed_config_overrides.png)

```yaml
## Set the Slow speed of rudder, this will not apply sail speed.
# Setting type: Single
# Default value: 5
# Acceptable value range: From 2 to 20
Rudder Slow Speed = 5

  ## Set the Half speed of rudder, this will apply additively with sails
  # Setting type: Single
  # Default value: 0
  # Acceptable value range: From 0 to 100
Rudder Half Speed = 30

  ## Set the Full speed of rudder, this will apply additively with sails
  # Setting type: Single
  # Default value: 0
  # Acceptable value range: From 0 to 100
Rudder Full Speed = 15
```

## Map and Minimap

All Vehicles can be viewed on the map. There are three types of vehicles, each
with their own minimap icon:

| Vehicle Type  |                                                                           Icon                                                                            | Pin Label                 | Description                                               |
|---------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------:|---------------------------|-----------------------------------------------------------|
| Water Vehicle | <img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/vehicle_water.png" width="40"/> | `V:<name>` or `V:Unnamed` | Ships and water-based rafts sailing on the ocean surface. |
| Air Vehicle   |  <img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/vehicle_air.png" width="40"/>  | `V:<name>` or `V:Unnamed` | Flying vehicles operating above terrain and water.        |
| Land Vehicle  | <img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/vehicle_land.png" width="40"/>  | `V:<name>` or `V:Unnamed` | Wheeled/tread vehicles travelling overland.               |

Vehicles that have been given a name via the vehicle config panel show as
`V:<name>` on the pin. Unnamed vehicles show as `V:Unnamed`.

Example image of a vehicle pin on the minimap:
<img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/docs/assets/vehicle_map_pins_all.png" width="400"/>

For visibility distance, there is configuration on distance of visible vehicles
and whether they show
on the minimap. See the config reference for details.

---

## Per Vehicle Config Panel

All vehicles have a config panel that can be accessed by interacting with the
steering wheel while holding **Shift**. This panel allows you to set a custom
name
for the vehicle, toggle map visibility, and adjust other settings on a
per-vehicle.

1. Board the vehicle.
2. Place a **Mechanism Switch** found in the Power tab of the Vehicle Hammer.
3. Interact with the switch while holding **Shift + E** to open the configure
   panel. Ensure the mode is set to **CommandsHud** — this will open the debug
   panel which contains commands and shortcuts for building and configuring your
   vehicle.
4. Click **Config** in the debug panel.
5. In the Config panel you can set a custom name for the vehicle and adjust the
   floatation height.

<table style="width:100%; border-collapse:collapse;">
  <thead>
    <tr>
      <th style="padding:8px; border-bottom:2px solid #444;">Step</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Image</th>
      <th style="padding:8px; border-bottom:2px solid #444;">Description</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td style="padding:8px;"><b>1. Place the Mechanism Switch</b></td>
      <td style="padding:8px;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/docs/assets/1_build_mechanical_switch.png" width="220"/></td>
      <td style="padding:8px;">Open the Vehicle Hammer build menu, go to the <b>Power</b> tab, and place a <b>Mechanism Switch</b> somewhere accessible on your vehicle.</td>
    </tr>
    <tr>
      <td style="padding:8px;"><b>2. Open the Configure Panel</b></td>
      <td style="padding:8px;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/docs/assets/2_shift_e_open_configure.png" width="220"/></td>
      <td style="padding:8px;">Hold <b>Shift + E</b> on the Mechanism Switch to open the configure panel. Set the mode to <b>CommandsHud</b> if it is not already.</td>
    </tr>
    <tr>
      <td style="padding:8px;"><b>3. Open Config from the Debug Menu</b></td>
      <td style="padding:8px;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/docs/assets/3_on_debug_menu_open_config_by_pressing_button.png" width="220"/></td>
      <td style="padding:8px;">Press <b>E</b> on the switch to open the debug panel. Click the <b>Config</b> button to open the per-vehicle config panel.</td>
    </tr>
    <tr>
      <td style="padding:8px;"><b>4. Vehicle Config Menu</b></td>
      <td style="padding:8px;"><img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/docs/assets/4_vehicle_config_menu.png" width="220"/></td>
      <td style="padding:8px;">Set a custom <b>Vehicle Name</b> (shown on the minimap as <code>V:&lt;name&gt;</code>), adjust the <b>Custom Floatation Height</b>, and configure other per-vehicle settings.</td>
    </tr>
  </tbody>
</table>

## Steering and Sailing

| Input            | Action                      |
|------------------|-----------------------------|
| `W`              | Increase speed / raise sail |
| `S`              | Decrease speed / lower sail |
| `A` / `D`        | Turn left / right           |
| `[Use]` on wheel | Board / leave helm          |

**Speed tiers:** stopped → slow → half → full. Each tier raises the sail higher.

Steering responsiveness scales inversely with speed — the wheel turns faster at
slow speed and slower at full speed.

The direction the wheel faces is the ship's forward direction. Rotate the wheel
during placement to change the ship's orientation.

---

## Sails

Each sail contributes independently to total speed. Multiple sails stack.

| Sail                        | Tier | Notes                                         |
|-----------------------------|------|-----------------------------------------------|
| Raft Sail                   | 1    | Starter sail, modest power                    |
| Karve Sail                  | 2    | Mid-tier power                                |
| Viking Sail                 | 3    | High power                                    |
| Drakkal Sail                | 4    | Highest power                                 |
| Custom Sail (3- or 4-point) | 1    | Power scales with sail area — larger = faster |

### Custom Sail textures

Drop a `.png` image into the `Assets` folder inside your ValheimRAFT plugin
directory. It will appear as an option when placing a Custom Sail. The `stag`
and `pirate skull` logos ship with the mod by default.

---

## Anchors

Without an anchor the vehicle will drift when you leave the helm.

- Place an **Anchor** prefab on the deck.
- Interact with it to drop or raise the anchor.
- The anchor physically touches the seabed or terrain to hold position — it
  syncs across all clients.
- **Flight mode** always allows an immediate stop regardless of whether an
  anchor is placed.

> Anchor materials require swamp biome resources.

---

## Hull and Floatation

Hull pieces (slabs, walls, ribs, prows, corners) do two things:

1. Form the visible shape of the ship.
2. Adjust the vehicle's **floatation height** based on the global config
   `HullFloatationColliderLocation`.

| Config Value           | Behaviour                                                                                                                  |
|------------------------|----------------------------------------------------------------------------------------------------------------------------|
| `Fixed`                | Floatation locked at 0. Most stable. **Default.**                                                                          |
| `Average`              | Average height of all pieces on the vehicle.                                                                               |
| `AverageVehiclePieces` | Average of hull pieces only (ignores masts etc).                                                                           |
| `Center`               | Midpoint between highest and lowest hull piece.                                                                            |
| `Custom`               | Per-vehicle override — place the Floatation Prefab on the desired surface. ⚠️ Can launch the player if moved while aboard. |

> Hull mechanics only apply to **V2 WaterVehicles**. Older V1 rafts are not
> affected.

### Sealing a hull

To seal a ship against water entry combine:

- **Hull Walls / Slabs** — sides and floor
- **Hull Rib Sides** — curved walls (0–90°)
- **Hull Rib Corners** — seal corners where ribs meet
- **Ship Prow / Prow Sleek / Prow Cutter** — seal the front/back

---

## Creative Mode

Creative mode stops the vehicle in place and lets you build freely without
resource costs.

```
vehicle creative
```

Run the command again to exit. While in creative mode the vehicle is held
stationary so pieces can be snapped accurately.

> Some third-party mods (e.g. Gizmo) override piece rotation in ways that
> conflict with a moving vehicle. Always enter creative mode before using
> rotation
> mods on a ship.

---

## ValheimRAFT Config

ValheimRAFT has extensive config editable at runtime
via [BepInEx Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/).

The config file is located in
`<ValheimGame>/BepInEx/config/zolantris-ValheimRAFT.cfg` and is auto-generated
if missing. Missing keys will also be added automatically when the mod updates
with new config options.

Key settings to know as a new player:

| Setting                          | What it does                                                                                 |
|----------------------------------|----------------------------------------------------------------------------------------------|
| `HullFloatationColliderLocation` | Controls how hull pieces affect floatation. See [Hull and Floatation](#hull-and-floatation). |
| `ServerRaftUpdateZoneInterval`   | How often the server checks vehicle zones. Increase on low-spec servers.                     |
| `fixPlanBuildPositionIssues`     | Enables the PlanBuild coordinate fix patch. Disable if PlanBuild is not installed.           |

The full config reference is in
the [auto-generated config doc](https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT/docs/ValheimRAFT_AutoDoc.md).

---

## Vehicle Hauling

Hauling lets you physically drag a vehicle by hand using the **Rope Anchor**
prefab. This is useful for repositioning a vehicle on land, pulling it off a
beach, or moving it short distances without needing to board and pilot it.

---

### How It Works

Approach a **Rope Anchor** placed on any vehicle and hold **Shift + [Use]** (
`Shift + E` by default) to grab the haul rope. While holding it the vehicle will
follow the direction you walk. Releasing the input drops the rope.

Hauling consumes **stamina** continuously while you are moving and pulling. If
your stamina hits zero you take damage, so keep an eye on it.

> **Tip:** if you need to pause hauling, stop moving near the vehicle. Stamina
> regenerates normally when you are stationary and the vehicle is not being
> dragged. You will not take damage while resting.

---

### Requirements

- A **Rope Anchor** prefab placed on the vehicle you want to haul.
- Enough stamina to complete the haul — bring food.

---

### Step-by-Step

1. Place a **Rope Anchor** on the vehicle using the Vehicle Hammer.
2. Walk up to the Rope Anchor.
3. Hold **Shift + E** to grab the haul rope and begin dragging.
4. Walk in the direction you want the vehicle to move — it will follow you.
5. If your stamina gets low, **stop moving** and let it regenerate before
   continuing. The vehicle will hold its position while you rest nearby.
6. Release **Shift + E** (or stop holding it) to drop the rope.

---

### Stamina Management

| Situation                   | Stamina effect               |
|-----------------------------|------------------------------|
| Moving while hauling        | Stamina drains continuously  |
| Standing still near vehicle | Stamina regenerates normally |
| Stamina reaches 0           | Player takes damage          |

Eat high-stamina foods before a long haul. Barley, cloudberries, and serpent
stew are good choices in the mid-to-late game.

---

### Tips

- The Rope Anchor can be placed anywhere on the vehicle — put it at the front or
  the side depending on which direction you want to pull from.
- Multiple Rope Anchors can be placed on a vehicle. Only the one you interact
  with is active at a time.
- For long overland moves consider using **Creative Mode** (`vehicle creative`)
  to freeze the vehicle in place while you plan your route, then release it and
  haul in short bursts.

---

## Common Problems

### The vehicle looks like a box / cube

1. Check the mod is installed correctly — the `Assets` folder must exist inside
   the plugin directory.
2. Upgrading from v1.x? Run `vehicle recover` near the old raft.
3. Disable all other mods except ValheimRAFT, Jotunn, and BepInEx, then test
   again.

### The vehicle sank underground

1.

Download [Unity Explorer](https://thunderstore.io/c/valheim/p/sinai-dev/UnityExplorer/).

2. Search the object list for `ValheimVehicles_WaterVehicleShip`.
3. In the Inspector, change only the **Y** value of LocalPosition to `+20` or
   more.  
   ⚠️ Never change X or Z — the vehicle will teleport to the other side of the
   world.

### The vehicle turns into a box when I sail near it (area loads)

- On a dedicated server this was largely fixed in `>=2.0.0`.
- Single player rendering issues should be reported as a bug with your
  `Player.log` attached.

### Hull is making the ship sink

- Switch `HullFloatationColliderLocation` to `Fixed` in config — this locks
  floatation at 0 regardless of hull layout.
- Remove and re-add one piece to force the floatation to recalculate.

### My bed spawn doesn't update when the ship moves

This is a known issue. Rebuild the bed and set your spawn again after each
voyage.

---

## Further Reading

- [Vehicle Commands](./Tutorial_VehicleCommandsInterface.md) — full command
  reference
- [Vehicle Storage / Save & Spawn](./Tutorial_VehicleStorage.md) — save and
  reload ship blueprints
- [Power System Guide](./Tutorial_PowerSystem.md) — engines, batteries, pylons,
  and swivels
- [Vehicle Dock demo (video)](https://youtu.be/iJSqmvJZlzo) — loading a land
  vehicle onto a water vehicle via the docking system

