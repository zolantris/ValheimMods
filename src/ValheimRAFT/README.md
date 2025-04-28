# ValheimRAFT

<img src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/Thunderstore/icon.png" alt="ValheimRAFT Community Made Boat Hjalmere">

This mod aims to continue support for Water based features of the original
ValheimRAFT mod and incorporate more vehicle types, items, and mechanics within
the mod
and further extends its capabilities.

## V3.0.0 Changes

### Land Vehicles can now be built.

They will be absolutely fun to use. But are unbalanced in energy and power and
really should require rams to be enabled (but you can toggle them off).

- vehicles have a bit of trouble going up mountains. But you can turn and slowly
  move up the mountain.
- Losing momentum can cause the vehicle to go the opposite way. Use the break or
  keep energy going on inclines
- There is no balance for energy / propulsion. This will be coming in another
  update.
- Land Vehicles can wreck forests and mine rocks. Their damage is configurable
  through `Ram Vehicles` sections. Highly recommended to keep damage high for
  vehicles to allow them to move.
    - Later we can consider adding balance to damage breaking parts of the
      vehicles and costing energy to demolish areas.

### Adds hauling/towing - vikings can now lift their rafts and move them

- Costs stamina and then health until it snaps.
- Configurable to only cost stamina then snap.
- Stamina cost is configurable.
- Dragging vehicles will trigger ram damage. You can turn this off.

### Breaking Changes

- Many ValheimRAFT prefabs have been moved. Break the misplaced prefabs and
  replace them correctly.

### Other fixes

- **Carts can be added on vehicles**
- **Stability of vehicles drastically improved**. See "CenterOfMass" percentage
  allowing you to move the center up to 50% lower than the vehicle's lowest
  point. Or at 50% exactly at it's lowest Y point.
- Bounds and rendering are exact. No issues with bound ever again (unless a mod
  adds a massive collider).
- Improved ram collisions. There is now a filter for repeated hits to ensure
  that velocity is over a threshold other to not do a hit.
- Improved logic to protect player when teleporting. There is a collision check
  when the ram attempt to hit a player so it does not hit the player if they are
  currently teleporting.
- MapSync should have 0 Null reference errors. But this logic will continue to
  be improved/optimize.


------

This mod has both a beta and a non-beta on Thunderstore. Please make sure you
are using non-beta if you want a stable experience. If you want to test
previews, swap to the beta variant.

:warning: Warning. If you are reading this readme on the
[ValheimMods/ValheimRAFT](https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT)
repo :warning:

- There likely will
  be features listed that do not yet exist within the game.
- Always read the readme
  from the latest official releases instead
    - [thunderstore page](https://thunderstore.io/c/valheim/p/zolantris/ValheimRAFT/)
      Quickest way is to look at the last readme post on thunderstore.
    - [Releases](https://github.com/zolantris/ValheimMods/releases)
      Alternatively look at the release source code. TBD this may be made
      easier.

------

[//]: # (## Build Status)

[//]: # ()

[//]: # ([![ValheimRAFT Build]&#40;https://github.com/zolantris/ValheimRaft/actions/workflows/build-release.yml/badge.svg&#41;]&#40;https://github.com/zolantris/ValheimRaft/actions/workflows/build-release.yml&#41;)

## Background

This repo is a [ValheimRaft](https://www.nexusmods.com/valheim/mods/1136) fork
that works
with latest Valheim.

The original mod
owner [Sarcen](https://www.nexusmods.com/valheim/users/3061574) stopped
maintaining the mod August 2023. They gave permission to maintain and open
source the mod on 12/25/2023.

## Contents

<!-- TOC -->

* [ValheimRAFT](#valheimraft)
    * [Background](#background)
    * [Contents](#contents)
    * [Features](#features)
    * [Community](#community)
    * [Prefabs](#prefabs)
        * [Installation Guide](#installation-guide)
            * [Current logos](#current-logos)
        * [Hull Mechanics](#hull-mechanics)
    * [Client/Server/SinglePlayer Support](#clientserversingleplayer-support)
    * [Config](#config)
    * [Issues](#issues)
    * [Mod Support](#mod-support)
    * [FAQs](#faqs)
        * [Help My Raft is A Box](#help-my-raft-is-a-box)
        * [Help My Raft Turns into a Box when I load the area](#help-my-raft-turns-into-a-box-when-i-load-the-area)
        * [Help My Raft went underground](#help-my-raft-went-underground)
        * [Help My Raft Is Sinking When I Build Hulls](#help-my-raft-is-sinking-when-i-build-hulls)
        * [My Bed does not update](#my-bed-does-not-update)
    * [Graphics](#graphics)
    * [Contributing](#contributing)
    * [Maintainers](#maintainers)
    * [Attribution](#attribution)
    * [Support Open Source](#support-open-source)
    * [Logging Metrics](#logging-metrics)
        * [What information will be collected](#what-information-will-be-collected)

<!-- TOC -->

## Features

- Build a RAFT on the water using Valheim's building prefabs.
- Adds a climbable ladder
- Add piers that reach the ocean floor (stone and log)
- Adds anchors
- Adds custom sails (requires meshes to be working)
    - Players can add their own patterns to the meshes folder FYI, you can have
      more than the stack or pirate logo, just add what you want.
- Adds ropes (requires meshes to be working)
- Sails now will each contribute to the total shipShip.Speed. Custom Sails do an
  area
  calculation while Tier 1-3 sails are
  preset values.
- Vehicles can be made to Fly.
- Vehicles can go underwater [>=2.4.0]
- There are tools to hide water. It looks pretty cool, but unrealistic for now

## Community

Join us on discord to get the latest information, upcoming features, and
discontain other mods.

[Discord Link](https://discord.gg/jeUcpCvB3z)

## Prefabs

| Name                                                                  | Icon                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
|-----------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Hull (Wood)                                                           | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_center_wood.png" width="200"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | V2 Raft Hull. See more info on [Hull Mechanics][hull-mechanics-link]                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| Hull (Iron)                                                           | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_center_iron.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | V2 Raft Hull Iron, See more info on [Hull Mechanics][hull-mechanics-link]                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| Hull Slab (Wood)                                                      | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_slab_wood_2x2.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | V2 Raft Hull Slab 2x2 and 4x4 variants. See more info on [Hull Mechanics][hull-mechanics-link]                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| Hull Slab (Iron)                                                      | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_slab_iron_2x2.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | V2 Raft Hull Slab Iron 2x2 and 4x4 variants. See more info on [Hull Mechanics][hull-mechanics-link].                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| Hull Wall (Wood)                                                      | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_wall_wood_2x2.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | V2 Raft Hull Wall 2x2 and 4x4 variants. See more info on [Hull Mechanics][hull-mechanics-link].                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| Hull Rib Side (Wood) (Iron)                                           | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_rib_wood.png" width="100"/>  <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_rib_iron.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | V2 Raft Hull Rib, a curved hull wall going for 0-90 degrees a combo of 3 hull walls. See more info on [Hull Mechanics][hull-mechanics-link].                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| Ship Prow (Wood)(Iron)                                                | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_rib_prow_wood_2x2.png" width="100"/>  <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_rib_prow_iron_2x2.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      | Allows for creating a front/back of the ship. Pairing this with a Rib side and a rib corner will seal the ship completely.                                                                                                                                                                                                                                                                                                                                                                                                                                                             |   
| Ship Hull-Rib Corner (Wood, Iron)                                     | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_rib_corner_universal_wood.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_rib_corner_universal_iron.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Allows for sealing a hull for a ship. Useful when adding hull sides and prow pieces to seal a ship from all directions.                                                                                                                                                                                                                                                                                                                                                                                                                                                                |  
| Ship Hull Corner floor (Wood left & right, Iron left & right)         | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_corner_floor_left_wood.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_corner_floor_right_wood.png" width="100"/>  <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_corner_floor_left_iron.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_corner_floor_right_iron.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                      | Allows for sealing a hull for a ship. Useful when adding hull sides and prow pieces to seal a ship from all directions.                                                                                                                                                                                                                                                                                                                                                                                                                                                                |  
| Hull Rib (Iron)                                                       | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_rib_iron.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             | V2 Raft Hull Rib, a curved hull wall going for 0-90 degrees a combo of 3 hull walls. See more info on [Hull Mechanics][hull-mechanics-link].                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| Rudder Basic                                                          | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/rudder_basic.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Basic Raft Rudder, turns with wheel. First rudder on ship will also move ship's wake effects to it's location. Future updates may add functionality                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| Rudder Advanced (Iron)                                                | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/rudder_advanced_single_iron.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Advanced Rudder, a larger rudder that turns with the ship. First rudder on ship will also move ship's wake effects to it's location. No other functionality.                                                                                                                                                                                                                                                                                                                                                                                                                           |
| Rudder Advanced (Wood)                                                | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/rudder_advanced_single_wood.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Advanced Rudder, a larger rudder that turns with the ship. First rudder on ship will also move ship's wake effects to it's location. No other functionality.                                                                                                                                                                                                                                                                                                                                                                                                                           |
| Rudder Advanced Twin (Wood)                                           | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/rudder_advanced_double_wood.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Advanced Rudder, a larger rudder that turns with the ship. First rudder on ship will also move ship's wake effects to it's location. No other functionality.                                                                                                                                                                                                                                                                                                                                                                                                                           |
| Rudder Advanced Twin (Iron)                                           | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/rudder_advanced_double_iron.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Advanced Rudder, a larger rudder that turns with the ship. First rudder on ship will also move ship's wake effects to it's location. No other functionality.                                                                                                                                                                                                                                                                                                                                                                                                                           |
| Steering Wheel                                                        | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/steering_wheel.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     | Steer the vehicle. Steering will be fast on slow speed, medium on half speed, and slow on fast speed. Previously all steering was slow. The direction it faces will determine the direction the boat sails.                                                                                                                                                                                                                                                                                                                                                                            |
| Raft Sail                                                             | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/raftmast.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           | Tier 1 Raft Sail. Offers modest sailing power.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| Karve Sail                                                            | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/karvemast.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Tier 2 Karve Sail offers mid-level sailing power.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| Viking Sail                                                           | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/vikingmast.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Tier 3 Viking Sail offers high-level sailing power.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| Drakkal Sail                                                          | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/vikingmast.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Tier 4 Drakkal Sail offers high sailing sailing power and looks great.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| Custom Sail (3) or (4) corners                                        | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/customsail.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Tier 1 Custom Sail, Offers a customizable sail of 3 points or 4 point sails. Each sail cubic foot is counted for sailing force. The sails are a balanced at tier1 sailing.                                                                                                                                                                                                                                                                                                                                                                                                             |
| Rope Anchor                                                           | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/rope_anchor.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | Connect an infinite number of ropes from this anchor point to the target prefab's center position. To disconnect grab a rope from the anchor point and re-attach to the already attached prefab. It should disconnect the rope. Ropes will not have any affect on physics. They *do not* anchor the ship.                                                                                                                                                                                                                                                                              |
| Rope Ladder                                                           | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/rope_ladder.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | Connect a rope ladder to a boat or any building to allow for climbing from the ground up to the building. Will automatically extend to seafloor or ground every couple seconds if the terrain changes.                                                                                                                                                                                                                                                                                                                                                                                 |
| Boarding Ramp (normal, Wide)                                          | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/boarding_ramp.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      | Boarding ramp can extend out from 1 - 50+ units. Each unit adds an additional expanded plank. There are no resource costs to increasing the extension bridge size. Boarding ramps collide with the sea so they will effectively hover over the sea. Ramps require network connection in multiplayer to properly work if the player is not the owner.                                                                                                                                                                                                                                   |
| Dirt Floor (1x1, 2x2)                                                 | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/dirtfloor_icon.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     | Allows for growing vanilla Valheim crops. __Currently does not support mods that utilize heightmaps to determine placement__. Each dirt section is not a heightmap.                                                                                                                                                                                                                                                                                                                                                                                                                    |
| Ram Stake (Wood,Iron) (1x2 2x4)                                       | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_stake_tier1_1x2.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_stake_tier1_2x4.png" width="100"/>  <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_stake_tier3_1x2.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_stake_tier3_2x4.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                        | Allows for ramming ships, destroying rocks, and hitting monsters/characters. Rams impact within an area. A ram stake is better for piercing damage but will not do any slash or hurt trees.                                                                                                                                                                                                                                                                                                                                                                                            |
| Ram Blade (Iron-Only) (left, right, top, bottom)                      | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_blade_top.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_blade_left.png" width="100"/>  <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_blade_top.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_blade_right.png" width="100"/>  <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_blade_top.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/ram_blade_bottom.png" width="100"/> | Has the largest area of effect hit, will crush rocks and by design is meant to be used as armor to bash and shred enemy ships. This ram is inspired (and part of) the nautilus project                                                                                                                                                                                                                                                                                                                                                                                                 |
| Watermask creator - hides water                                       | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/water_opacity_bucket.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               | Hides water, can be used to see through water from inside and outside. Allows for placing custom cubic shapes and other ships. Ships are generated via a point system each point creates a Bounds and each additional point increases the bounds. Final output is a volume which then is applied to the shape (Cube) or other ones if picked. Cube is only stable one working right now. Place 8 points. To generate the custom water mask. Can be deleted when in edit mode. `vehicle colliderEditMode` to toggle the edit mode. Edit mode has blue boxes around all watermesh hiders |
| Window Porthole Walls and floor (2x2, 4x4, 8x4) (4x4 floor) (v2.5.0+) | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_wall_window_porthole_iron_4x4.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_wall_window_porthole_iron_8x4.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_floor_window_porthole_iron_4x4.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     | Protects the user against external threats and allows clear visibility of outside. Has half the health of the Iron / Yggdrassil reinforced wood prefabs.                                                                                                                                                                                                                                                                                                                                                                                                                               |
| Anchors (v2.5.0+)                                                     | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/anchor_full_wood.png" width="100"/> <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/anchor.png" width="100"/>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      | Allows for physically anchoring the ship when the anchor touches the land. This will sync across clients. Users will need to get to the swamp and find materials to make it there. **Without an anchor you will not be able to immediately stop the ship.** <br/>_<br />This excludes flight. Flight can always immediately anchor the ship_.                                                                                                                                                                                                                                          |

## Experimental Prefabs

These prefabs are not fully ready or may be broken into smaller pieces. Please
follow the description when using these prefabs and please be mindful that they
are only available early so people can test them out and/or have fun with them.
They are not meant to be balanced.

| Name     | Icon                                                                                                                                                                                           | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
|----------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Nautilus | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/nautilus.png" width="100"/> | Nautilus is inspired by Jules Verne's [20,000 Leagues Under The Sea](https://en.wikipedia.org/wiki/Twenty_Thousand_Leagues_Under_the_Seas). Built with original assets by RacerX and given free to Zolantris on discord. This Prefab has all the meshes from the boat included, but the hatch is pulled back to players can enter the boat. It's not meant for serious play, and the recipe needs to be more balanced + the ship needs to be fully rigged for propulsion (propellers do not move, nor do the fins) |

### Installation Guide

:warning: The Raft Mod must be installed correctly in order to resolve Custom
Sail materials. :warning:

- Folder must be placed within `<ValheimGameFolder>/BepInEx/plugins/`
- The folder must be named `ValheimRAFT` or `zolantris-ValheimRAFT` and contain
  a `Assets` folder

Noting that in future updates this may not be required, however loading custom
textures will always be supported. This allows players to drag and drop their
favorite logos or custom sail textures and see them in game.

#### Current logos

| Name        | Image                                                                                                                                                                                                  |
|-------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Stag Logo   | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/stag.png" width="100"/>                      |
| Pirate Logo | <img style="object-fit: scale-down;" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/pirate_skull_uncompressed.png" width="100"/> |

### Hull Mechanics

These mechanics apply **only** to **V2 WaterVehicles**. The older V1 rafts will
never support this feature.

Every piece of hull added to a ship will adjust the ship's floatation.
Floatation is determined by the key `HullFloatationColliderLocation`.

| Value                | Floatation Calculation Description                                                                                                                                                                                                                                                                                                                                                                                                                                        |
|----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Average              | Average is the total number of vehicle pieces height averaged together. <br/><br>Example: 4 pieces are used with 0 height and 1 piece at height 5. The average would be (sum of height)/(number of pieces) or 5/5=1 height. If a hull is 4 pieces high the mid point (top of second piece) would be the floatation point                                                                                                                                                  |
| AverageVehiclePieces | The average of hull pieces only on the vehicle. This makes it ignore things like masts making hull calcs a bit more legitimate.                                                                                                                                                                                                                                                                                                                                           |
| Center               | The center point between the max height hull piece on a raft and the lowest height hull piece placed on a raft. No quantity will be factored in to determine the center point.                                                                                                                                                                                                                                                                                            |
| Fixed                | Fixed a 0. This will never move making it the most stable way to build boats. This is the default config. Other values will mutate the floatation as more pieces are added or removed.                                                                                                                                                                                                                                                                                    |
| Custom               | Direct way to control each vehicle. This will only be allowed on local vehicles.<br><br> Place the FloatationPrefab on a surface of the boat. The surface's height will become the new float position. Placing another FloatationPrefab will unset and revert the vehicle back to the global default EG Fixed,Center,Average,AverageOfHull.<br><br> **This can launch the player beware of moving the custom float marker while on the vehicle without fall-protection**. |

[//]: # ([image_base_urls]&#40;https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons&#41;)

[//]: # ([image_base_urls_generated]&#40;https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons&#41;)

## Client/Server/SinglePlayer Support

Server support may or may not work. In **>=1.6.2** ships and loading were mainly
fixed by adding some threading
optimization. There is also a config called `ServerRaftUpdateZoneInterval` this
allows users to tweak the update
frequency of checks.

The following has been tested:

- SinglePlayer
    - boats will load
    - FPS issues with giant ships
- Client only with a server IE (connecting to server without mod),
    - creating a boat and logging on and off the server does not destroy the
      boat.
    - sailing and teleporting does not destroy the boat.
    - The client will still be able to create items and the server will keep
      those raft items. (unless you have mods
      that garbage collect unused ZDOs)
    - restarting/stopping/starting server keeps the boat.
    - Removing the ValheimRAFT mod from the client will make the server glitch
      out the boat items.
    - joining back (after adding the mod again) will render the items correctly.
    - Loading a ship into view will possibly
- Server & Client
    - Loading a 4500 build plan ship. Traveling around a couple sectors. The
      ship remained as built.
    - Moving away from the ship until it unloaded. Moving back immediately and
      attempting to land on the ship while it
      renders. It works and still loads. Looks a bit funny as it spawns.
        - **Warning** to anyone that builds property next to a large ship, when
          it renders it's possible a wave could
          briefly flip it and the top part of the ship rolls and hits property.

## Config

ValheimRAFT has **extensive** config. _If you do not like the current balance,
you are capable of editing this config and setting thing the way you like._

Please feel free to share the config if you feel it is more balanced than
default. Maintainers will need to test it before accepting it as
the default though.

Please download the
mod [Official BepInEx Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/)
to be able to **edit ValheimRAFT config while playing**. Most of the config can
be set
while playing.

Excludes

- prefabs
- patches
- zone related content or first render content

There is auto-documentation for this config at this link. Please go here to read
up on all the
config. [config auto-generated document](https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT/docs/ValheimRAFT_AutoDoc.md)

## Commands

ValheimRAFT has many commands. Please read below to see what is possible. All
Commands start with `vehicle` and then have a argument. IE `vehicle debug` will
open some debug menus.

I highly recommend
installing [Server DevCommands](https://thunderstore.io/c/valheim/p/JereKuusela/Server_devcommands/)
by Jere if you want to get code completion for these scripts. (I believe it's
this mod that does this. Also allows flight and other hacks on servers and
automating it if you want to debug a build.)

| name                  | description                                                                                                                                                                                                                                                                                                                                                                                                                                            |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `creative`            | Must be fired on or near a ship, will place the ship in edit mode above the water. Height can be configured in the config menu. Can and will hit the player so if the players are not on the ship and not the one running the command beware of this.                                                                                                                                                                                                  |
| `debug`               | opens the debug menu. Has some more ways to debug the vehicle.                                                                                                                                                                                                                                                                                                                                                                                         |
| `report-info`         | Formats a report of the current vehicle with some metrics added to it. Helpful for when submitting a bug. Also, could be helpful for users if they want to know why their raft is broken.                                                                                                                                                                                                                                                              |
| `recover`             | Will recover any vanilla pieces on a raft. Pieces that are non-vanilla can be lost due to them not registering before the mod crashes and then the game deletes them as invalid content. Raft has a way to prevent this, but if raft bugs out it's going to not protect users.                                                                                                                                                                         |
| `move`                | Will teleport the raft and protect the players on the raft from being smashed out of the world...all players on the raft. If you are in the water outside the raft in the landing zone, you will die or get hit into space. Takes the arguements of `X Y Z` in decimal format. IE `0.0 0 -50` all work. And would take you zero (x) zero (y) and move you backwards by 50. Rembemer raft is relative so this will be relative coordinates to the raft. |
| `move-up`             | Same as teleport but will allow moving the raft in the area upwards.                                                                                                                                                                                                                                                                                                                                                                                   |
| `rotate`              | Will will attempt to force rotate the vehicle.                                                                                                                                                                                                                                                                                                                                                                                                         |
| `toggleOceanSway`     | Toggles ocean sway of vehicles. No more lurches only up and down. For those that are sea sick...or want to build but be moving.                                                                                                                                                                                                                                                                                                                        |
| `upgradeShipToV2`     | Upgrades an older ValheimRAFT world ship to the V2 ship. This will fix a broken ship. V1 ships are deprecated and will be removed soon. Do not use them. They probably do not work for ashlands anymore either.                                                                                                                                                                                                                                        |
| `downgradeToShipToV1` | Downgrades to an older v1 ship. Not supported. Do not do this. Support will be removed soon IE `3.x.x`.                                                                                                                                                                                                                                                                                                                                                |
| `colliderEditMode`    | A new command added in 2.4.0 watermesh overhaul. This will allow editing the invisible / non-colliding (with build hammer) water mesh colliders. Running it again will hide the generated squares around the water meshes.                                                                                                                                                                                                                             |

## Issues

If you have a bug, please create an issue under the
repo's [issues section](https://github.com/zolantris/ValheimMods/issues).

- `Player.log` is required to properly debug many issues. Otherwise, you will
  not be providing enough information for maintainers to help. Please copy paste
  the log or
  /drag drop it into the github issue.
    - Player.log can be found under the User directory's AppData path for
      valheim (Same location as worldsaves). This path should work if copy
      pasting
      into Windows explorer  `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\`
    - Full path
      example `C:\Users\bob_smith\AppData\LocalLow\IronGate\Valheim\Player.log`
        - bob_smith - is the example username.
        - Use the `%USERPROFILE%` to skip having to add your username or drive
          path before
          it. [(more detailed)](https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid#requirements)
- Please respect the form. Adding the relevant information makes it easier to
  triage the problem.

Click [here](https://github.com/zolantris/ValheimRaft/issues/new?assignees=&labels=bug&projects=&template=raft-bug-report.yml)
to create an issue now.

## Mod Support

In 1.6.0+ this mod
supports [PlanBuild](https://www.nexusmods.com/valheim/mods/1125) a popular
blueprinting mod.

Support is added via a patch that makes PlanBuild use the Raft `localPosition`
instead of world position which each item
could be moving/swaying and updating as the PlanBuild iterated through the
items. This easily breaks the Plan save.

- Hopefully this logic can be incorporated within PlanBuild directly. There is a
  config toggle to turn this patch off to
  future-proof things.
- Alternative approaches for this could be doing a detection if the game object
  is attached to a parent object and
  calculating coordinates of the items **after** all object positions have been
  fetched.

Noting that Planbuild does not register well with harmony so searching for it is
not possible (from what I saw). There
is a path resolution and a config flag to enable coordinate fixes. Disable the
config value `fixPlanBuildPositionIssues`
and it should no longer cause errors for not having PlanBuild installed.

## FAQs

Please read these if you are experiencing trouble.

### Help My Raft is A Box

1. Did you install the ValheimRAFT mod correctly?
    - No? Install it correctly following this guide, otherwise the commands may
      not work or sails will not be generated.
2. Did you upgrade from v1.x.x to >=v2.0.0?
    - Run `vehicle recover` this command works in `>=2.0.2`
    - v2.0.0 had to make a plugin GUID change which
      likely
      broke the prefab registry of the v1 raft. This GUID change was in order to
      reset older mods from pointing to the updated ValheimRAFT and breaking
      features.
3. Are you manually installing?
    - A: Yes? Don't do it.
        - Install r2modman and have it manage your mods. This prevent a bunch of
          installation problems with raft and other mods especially as valheim
          updates.
        - https://thunderstore.io/package/ebkr/r2modman/
    - No: Good Job, debugging the problem should be as simple as unchecking your
      mods, automatically updating things, and editing your configs.
4. Did you install a mod that is crashing the bepinex plugins?
    - Try to remove mods that are older. Many mods are not meant to endure
      updates from valheim and need a fix to make their code stable again.
      Disable all older mods and only include ValheimRAFT, Jotunn, and BepInEx.
5. Did you install a mod that overrides ValheimRAFT code?
    - Mods that override `Player.PlacePiece` will cause problems with piece
      placement.
        - Example Gizmos overrides the rotations and angles of pieces so it is
          not compatible for a boat in motion. Use `vehicle creative`
          or `raftcreative` to make the ship stop moving and place pieces.
    - Mods that override `Player.GetControlledShip` will cause the boat to be
      unable to interact or update hud or be controlled.
    - __Mods that adjust or change `Ship` Values from Valheim will no longer
      affect the V2 WaterVehicle__. This means the Vehicle is much more
      resilient
      and more compatible with mods that add new ships but also means it has
      it's own logic so the ship will not benefit OR break from those mods.

### Help My Raft Turns into a Box when I load the area

1. Are you on a dedicated server?
    - No? Switch to a dedicated server. The raft mod may not be stable when
      hosting from a LAN IP. This likely was fixed in >=2.0.0. But will continue
      to be an issue if you are using a v1 raft.
    - No, But I'm on single player...
        - Submit a bug report. Single player should render correctly. If you
          only have ValheimRAFT + Jotunn and absolutely trust your mod list.

### Help My Raft went underground

- For now there is no command to save your raft.

1. Download [Unity Explorer][unity-explorer-link]

2. Under Object list search for `WaterVehicleShip` it should show up
   as `ValheimVehicles_WaterVehicleShip`

- `ValheimVehicles` is a prefix for
  anything related to V2 RAFT/Vehicles mod.
- If you see multiple ships, make sure you click the green box to Enable/disable
  them to confirm you are changing the correct ship.

3. Change the height of the vehicle by clicking on the Object and
   within `inspector` - another pannel, click on local position and change the Y
   coordinate to +20 height or more.
    - **Make sure you never change the x or z positions**
      otherwise the boat might be removed from rendering and be sent to the
      other
      side of the world.
    - Example `Position 1600.084 29.7749 1685.39`
      Set to `1600.084 50 1685.39`

### Help My Raft Is Sinking When I Build Hulls

1. Please read
   the [guide on Vehicle Hulls][hull-mechanics-link]?
   Each Hull affects the floatation
   of the vehicle.
2. Select the correct config and add or remove a piece to see the ship update
   based on the new config value.

### My Bed does not update

**This is a known issue do the following work-around.**

- Every time the raft is moved and then stopped. Rebuild your bed and then set
  your spawn again.

This may be resolved in future updates.

## Graphics

This project supports both **Vulkan** and **Direct3D11** as of >=1.6.11.
Previously it only supported Direct3D11.

## Contributing

1. Please fork the codebase and make a pull request via your fork branch.
2. Changes need to be feature based IE keep the changes minimal and single
   focused. Larger changes are welcome, but they
   have higher chance of breaking other things and are harder to maintain.
3. If you want to directly get involved. Reach out on discord.
4. Please read
   the [Contributing.md](https://github.com/zolantris/ValheimMods/blob/main/docs/CONTRIBUTING.md)
   document for more information

## Maintainers

| Name      | Date              | Active     |
|-----------|-------------------|------------|
| Zolantris | 12-2023 - present | **Active** |
| Sarcen    | 04-2021 - 08-2023 | Inactive   |

## Attribution

| Name        | Contributions                                                                                              |
|-------------|------------------------------------------------------------------------------------------------------------|
| Sarcen      | For creating the initial mod and supporting it until v1.4.8!                                               |
| Zolantris   | For supporting and expanding the Raft Mod since v1.5.0.                                                    |
| RacerX      | For contributing a stag icon for sails - Looks awesome! And the nautilus assets, (experimental)            |
| Cl0udstr1fe | For adding the new hull meshes for Rib Side, Prow, and Corners. Completes the ship look with these meshes! |

## Support Open Source

<a href='https://ko-fi.com/zolantris' target='_blank'><img height='35' style='border:0px;height:46px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com'></a>

## Videos and Promos

- [2.4.0 ValheimRAFT Water overhaul nautilus](https://www.youtube.com/watch?v=qlB_sWbFfEA)
- [2.4.0 ValheimRAFT Water overhaul demo](https://studio.youtube.com/video/yoPz0H5kZcc/edit)

## Logging Metrics

**NOT APPLICABLE YET**.

These packages may contain logging. Logging can be opted out by any user by
unchecking `enableMetrics`. If you choose to opt-out, please be aware, this will
make it more difficult to troubleshoot issues.

- Logging does not do anything without a SentryUnityPlugin package that has not
  been made available to the community (yet). In future updates Sentry logging
  will be recommended to beta testing players so benchmarks can be collected
  related to Vehicle performance.

### What information will be collected

- Paths related to Valheim Game directory
- Paths related to ValheimRAFT / ValheimVehicles plugin directory such as
  symlinked directories from vortex mod installer or directories created by
  r2modman or thunderstore.
- Health related to the ValheimRAFT mod. It will track performance, slowdowns,
  and bottlenecks making it significantly easier to debug.

This logging has yet to be implemented.

[//]: # (Links and shared resources)

[unity-explorer-link]: https://thunderstore.io/c/valheim/p/sinai-dev/UnityExplorer/

[hull-mechanics-link]: https://github.com/zolantris/ValheimMods/blob/main/src/ValheimRAFT/README.md#hull-mechanics
