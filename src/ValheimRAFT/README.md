# ValheimRAFT

<img src="./Thunderstore/icon.png" alt="ValheimRAFT Community Made Boat Hjalmere">

This mod aims to continue support for Water based features of the original
ValheimRAFT mod and incorporate more vehicle items and mechanics within the mod
and further extends it's capabilities.

## Build Status

[![ValheimRAFT Build](https://github.com/zolantris/ValheimRaft/actions/workflows/build-release.yml/badge.svg)](https://github.com/zolantris/ValheimRaft/actions/workflows/build-release.yml)

## Background

This repo is a [ValheimRaft](https://www.nexusmods.com/valheim/mods/1136) fork
that works
with latest Valheim. The original mod
owner [Sarcen](https://www.nexusmods.com/valheim/users/3061574) stopped
maintaining the mod, but gave permission to maintain/improve and open source
this mod as of
12/25/2023.

## Contents

<!-- TOC -->

* [ValheimRAFT](#valheimraft)
    * [Build Status](#build-status)
    * [Contents](#contents)
    * [Client/Server/SinglePlayer Support](#clientserversingleplayer-support)
    * [Features](#features)
    * [Community](#community)
    * [Config](#config)
        * [Meshes](#meshes)
    * [Issues](#issues)
    * [Mod Support](#mod-support)
    * [Contributing](#contributing)
    * [Support Open Source](#support-open-source)
    * [Getting Started](#getting-started)

<!-- TOC -->

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

## Features

- Build a RAFT on the water using Valheim's building prefabs.
- Adds a climbable ladder
- Add piers that reach the ocean floor (stone and log)
- Adds anchors
- Adds custom sails (requires meshes to be working)
- Adds ropes (requires meshes to be working)
- Sails now will each contribute to the total shipShip.Speed. Mesh sails do an
  area
  calculation while tier1-3 sails are
  preset values.

## Community

Join us on discord to get the latest information, upcoming features, and
discover other mods.

[Discord Link](https://discord.gg/jeUcpCvB3z)

## Prefabs

| Name                           | Icon                                                                                                                                                                                                     | Description                                                                                                                                                                                                                                                                                                                   |
|--------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Hull (Wood)                    | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_center_wood.png" width="50"/>            | V2 Raft Hull. See more info on [Hull Mechanics](hull_mechanics)                                                                                                                                                                                                                                                               |
| Hull (Iron)                    | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_center_iron.png" width="50"/>            | V2 Raft Hull Iron, See more info on [Hull Mechanics](hull_mechanics)                                                                                                                                                                                                                                                          |
| Hull Slab (Wood)               | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_slab_2x2_wood.png" width="50"/>          | V2 Raft Hull Slab 2x2 and 4x4 variants. See more info on [Hull Mechanics](hull_mechanics)                                                                                                                                                                                                                                     |
| Hull Slab (Iron)               | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_slab_2x2_iron.png" width="50"/>          | V2 Raft Hull Slab Iron 2x2 and 4x4 variants. See more info on [Hull Mechanics](hull_mechanics).                                                                                                                                                                                                                               |
| Hull Wall (Wood)               | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_wall_2x2_wood.png" width="50"/>          | V2 Raft Hull Wall 2x2 and 4x4 variants. See more info on [Hull Mechanics](hull_mechanics).                                                                                                                                                                                                                                    |
| Hull Wall (Iron)               | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/hull_wall_2x2_iron.png" width="50"/>          | V2 Raft Hull Wall Iron 2x2 and 4x4 variants. See more info on [Hull Mechanics](hull_mechanics).                                                                                                                                                                                                                               |
| Rudder Basic                   | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/rudder_basic.png" width="50"/>                         | Basic Raft Rudder, turns with wheel. First rudder on ship will also move ship's wake effects to it's location. Future updates may add functionality                                                                                                                                                                           |
| Rudder Advanced (Iron)         | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/rudder_advanced_single_iron.png" width="50"/> | Advanced Rudder, a larger rudder that turns with the ship. First rudder on ship will also move ship's wake effects to it's location. No other functionality.                                                                                                                                                                  |
| Rudder Advanced (Wood)         | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/rudder_advanced_single_wood.png" width="50"/> | Advanced Rudder, a larger rudder that turns with the ship. First rudder on ship will also move ship's wake effects to it's location. No other functionality.                                                                                                                                                                  |
| Rudder Advanced Twin (Wood)    | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/rudder_advanced_double_wood.png" width="50"/> | Advanced Rudder, a larger rudder that turns with the ship. First rudder on ship will also move ship's wake effects to it's location. No other functionality.                                                                                                                                                                  |
| Rudder Advanced Twin (Iron)    | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons/rudder_advanced_double_iron.png" width="50"/> | Advanced Rudder, a larger rudder that turns with the ship. First rudder on ship will also move ship's wake effects to it's location. No other functionality.                                                                                                                                                                  |
| Steering Wheel                 | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/steering_wheel.png" width="50"/>                       | Steer the vehicle. Steering will be fast on slow speed, medium on half speed, and slow on fast speed. Previously all steering was slow. The direction it faces will determine the direction the boat sails.                                                                                                                   |
| Raft Sail                      | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/raftmast.png" width="50"/>                             | Tier 1 Raft Sail. Offers modest sailing power.                                                                                                                                                                                                                                                                                |
| Karve Sail                     | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/karvemast.png" width="50"/>                            | Tier 2 Karve Sail offers mid-level sailing power.                                                                                                                                                                                                                                                                             |
| Viking Sail                    | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/vikingmast.png" width="50"/>                           | Tier 3 Viking Sail offers high-level sailing power.                                                                                                                                                                                                                                                                           |
| Custom Sail (3) or (4) corners | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/customsail.png" width="50"/>                           | Tier 1 Custom Sail, Offers a customizable sail of 3 points or 4 point sails. Each sail cubic foot is counted for sailing force. The sails are a balanced at tier1 sailing.                                                                                                                                                    |
| Rope Anchor                    | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/rope_anchor.png" width="50"/>                          | Connect an infinite number of ropes from this anchor point to the target prefab's center position. To disconnect grab a rope from the anchor point and re-attach to the already attached prefab. It should disconnect the rope. Ropes will not have any affect on physics. Norm do they anchor the ship if connected to land. |
| Rope Ladder                    | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/rope_ladder.png" width="50"/>                          | Connect a rope ladder to a boat or any building to allow for climbing from the ground up to the building. Will automatically extend to seafloor or ground every couple seconds if the terrain changes.                                                                                                                        |
| Rope Ladder                    | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/rope_ladder.png" width="50"/>                          | Connect a rope ladder to a boat or any building to allow for climbing from the ground up to the building. Will automatically extend to seafloor or ground every couple seconds if the terrain changes                                                                                                                         |

### Custom Sail Assets

:warning: The Raft Mod must be installed correctly in order to resolve Custom
Sail materials. :warning:

- Folder must be placed within `<ValheimGameFolder>/BepInEx/plugins/`
- The folder must be named `ValheimRAFT` or `zolantris-ValheimRAFT` and contain
  a `Assets` folder

Noting that in future updates this may not be required, however loading custom
textures will always be supported. This allows players to drag and drop their
favorite logos or custom sail textures and see them in game.

#### Current logos

| Name        | Image                                                                                                                                                                                         |
|-------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Stag Logo   | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/stag.png" width="50"/>                      |
| Pirate Logo | <img height="50" src="https://raw.githubusercontent.com/zolantris/ValheimMods/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons/pirate_skull_uncompressed.png" width="50"/> |

### Hull Mechanics

These mechanics apply **only** to **V2 WaterVehicles**. The older V1 rafts will
never support this feature.

Every piece of hull added to a ship will adjust the ship's floatation.
Floatation is determined by the key `HullFloatationColliderLocation`.

| Value   | Floatation Calculation Description                                                                                                                                                                                                                                                                                    |
|---------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Average | Average is the total number of hull pieces height averaged together. <br/><br>Example: 4 pieces are used with 0 height and 1 piece at height 5. The average would be (sum of height)/(number of pieces) or 5/5=1 height. If a hull is 4 pieces high the mid point (top of second piece) would be the floatation point |
| Center  | The center point between the max height hull piece on a raft and the lowest height hull piece placed on a raft. No quantity will be factored in to determine the center point.                                                                                                                                        |
| Top     | The ship will float at the top most hull piece. Likely only useful if using hull pieces to make underwater vehicles                                                                                                                                                                                                   |
| Bottom  | The lowest most hull pieces will determine the floatation point. Placing other prefabs that are not hull pieces can be done, but any hull piece below another one will make the ship float higher.                                                                                                                    |

[//]: # ([image_base_urls]&#40;https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/Icons&#41;)

[//]: # ([image_base_urls_generated]&#40;https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT/ValheimRAFT.Unity/Assets/ValheimVehicles/GeneratedIcons&#41;)

## Config

Please reference the config output document

## Issues

If you have a bug, please create an issue under the
repo's [issues section](https://github.com/zolantris/ValheimMods/issues).

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

| Name      | Contributions                                                                                        |
|-----------|------------------------------------------------------------------------------------------------------|
| Sarcen    | For creating the initial mod and supporting it until v1.4.8!                                         |
| Zolantris | For supporting and expanding the Raft Mod since v1.5.0.                                              |
| RacerX    | For contributing a stag icon for sails - Looks awesome! And for making some other unreleased assets. |

## Support Open Source

<a href='https://ko-fi.com/zolantris' target='_blank'><img height='35' style='border:0px;height:46px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com'></a>

## Logging Metrics

These packages may contain logging. Logging can be opted out by any user by
unchecking `enableMetrics`. If you choose to opt-out, please be aware, this will
make it more difficult to troubleshoot issues.

- Logging does not do anything without a SentryUnityPlugin package that has not
  been made available to the community (yet). In future updates Sentry logging
  will be recommended to beta testing players so benchmarks can be collected
  related to Vehicle performance.

### What information will be collected
- 

- Paths related to Valheim Game directory
- Paths related to ValheimRAFT / ValheimVehicles plugin directory such as
  symlinked directories from vortex mod installer or directories created by
  r2modman or thunderstore.
- Health related to the ValheimRAFT mod. It will track performance, slowdowns,
  and bottlenecks making it significantly easier to debug.

This logging has yet to be implemented.