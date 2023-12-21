# ValheimRAFT

<img src="./Thunderstore/icon.png" alt="ValheimRAFT Community Made Boat Hjalmere">

A [ValheimRaft](https://www.nexusmods.com/valheim/mods/1136) fork that works with latest Valheim. The original mod
owner [Sarcenzzz](https://www.nexusmods.com/valheim/users/3061574) abandoned the
mod, this fork aims to keep the mod functional with future goals of expanding functionality.

## Build Status

[![ValheimRAFT Build](https://github.com/zolantris/ValheimRaft/actions/workflows/build-release.yml/badge.svg)](https://github.com/zolantris/ValheimRaft/actions/workflows/build-release.yml)

## Client/Server/SinglePlayer Support

Server support may or may not work. In **>=1.6.2** ships and loading were mainly fixed by adding some threading
optimization. There is also a config called `ServerRaftUpdateZoneInterval` this allows users to tweak the update
frequency of checks.

The following has been tested:

- SinglePlayer
    - boats will load
    - FPS issues with giant ships
- Client only with a server IE (connecting to server without mod),
    - creating a boat and logging on and off the server does not destroy the boat.
    - sailing and teleporting does not destroy the boat.
    - The client will still be able to create items and the server will keep those raft items. (unless you have mods
      that garbage collect unused ZDOs)
    - restarting/stopping/starting server keeps the boat.
    - Removing the ValheimRAFT mod from the client will make the server glitch out the boat items.
    - joining back (after adding the mod again) will render the items correctly.
    - Loading a ship into view will possibly
- Server & Client
    - Loading a 4500 build plan ship. Traveling around a couple sectors. The ship remained as built.
    - Moving away from the ship until it unloaded. Moving back immediately and attempting to land on the ship while it
      renders. It works and still loads. Looks a bit funny as it spawns.
        - **Warning** to anyone that builds property next to a large ship, when it renders it's possible a wave could
          briefly flip it and the top part of the ship rolls and hits property.

## Features

- Build a RAFT on the water using Valheim's building prefabs.
- Adds a climbable ladder
- Add piers that reach the ocean floor (stone and log)
- Adds anchors
- Adds custom sails (requires meshes to be working)
- Adds ropes (requires meshes to be working)

## Community

Join us on discord to get the latest information, upcoming features, and discover other mods.

[Discord Link](https://discord.gg/jeUcpCvB3z)

## Config

### InitialRaftFloorHeight

| ConfigName                   | Description                                                                                                                  |
|------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| InitialRaftFloorHeight       | Lets you set the height of the floors so they do not clip. Recommended to stay between 0.4 and 0.6.                          |
| ServerRaftUpdateZoneInterval | Set interval in seconds for the server update to trigger and re-render pieces if in view                                     |
| pluginFolderName             | Allows you to specify the plugin folder if it's been renamed. See the meshes section below for more details                  |
| raftHealth                   | Set the raft health for wear and tear. I set it to `500` default, but it was originaly `10000` which is not balanced         |
| fixPlanBuildPositionIssues   | Turn on/off the patches for PlanBuild, only needed if the user has PlanBuild and it adds support for ValheimRaft coordinates |

### Meshes

In Mod versions at or lower than `1.4.9` there were problems with the folder being renamed. In `>=1.5.0` there is a
configuration
manager option to change the path to resolve the ValheimRAFT folder.

If you want meshes (IE sails and ropes ) to render automatically, your mod must either be named `ValheimRAFT`
or `zolantris-ValheimRAFT`.

Otherwise make sure to edit the `pluginFolderName` key and add the folder name for ValheimRaft located
within the BepInEx\Plugins path. Afterwards relaunch the game. There should be no mesh issues.

## Issues

If you have a bug, please create an issue under the
repo's [issues section](https://github.com/zolantris/ValheimRaft/issues). Issues templates will be created soon, but
general format is

1. Title of Issue
2. Description of the problem
3. Possible solutions
4. Your mod list. (really this is important)
5. You have replicated this bug in isolation with only this mod. (yes or no)

## Contributing

1. Please fork the codebase and make a pull request via your fork branch.
2. Changes need to be feature based IE keep the changes minimal and single focused. Larger changes are welcome, but they
   have higher chance of breaking other things and are harder to maintain.

## Mod Support

In 1.6.0+ this mod supports [PlanBuild](https://www.nexusmods.com/valheim/mods/1125) a popular blueprinting mod.

Support is added via a patch that makes PlanBuild use the Raft `localPosition` instead of world position which each item
could be moving/swaying and updating as the PlanBuild iterated through the items. This easily breaks the Plan save.

- Hopefully this logic can be incorporated within PlanBuild directly. There is a config toggle to turn this patch off to
  future proof things.
- Alternative approaches for this could be doing a detection if the game object is attached to a parent object and
  calculating coordinates of the items **after** all object positions have been fetched.

## Getting Started

1. Clone / download this project
2. IDE: Download Rider or Visual studio. Use Rider if you do not want to waste tons of time resizing things. Visual
   Studio is not intuitive or user friendly.
3. Unity: Download Unity Hub
    - Unity is only required if you are changing scripting related to the prefabs
4. Running things
    1. Open the whole project with Rider. Typically you can double click on ValheimRAFT.sln to open it
    2. Unity -> Open the ValheimRaftUnity folder and install the Unity 2020 version
    3. Install [AssetBundle Browser](https://github.com/Unity-Technologies/AssetBundles-Browser) from github master url
       or release url. Since it's not updating master is okay.
        - copy paste https://github.com/Unity-Technologies/AssetBundles-Browser.git into the input under this interface
          in Unity. Window > Package Manager > (click plus arrow > click add package from giturl
5. Compiling with the current branch libs folder
    - You will not need to do anything. These libs are pre-built off the latest supported valheim, jotunn, bepinex.
    - Skip this step If -
        - you need to fix an issue after valheim updates, you will need to update, re-publicize, download newer bepinex
          and harmony references etc.
6. Manual Compiling
    1. delete the current libs folder (if the libs are outdated) and create a new libs folder and follow the manual
       compiling steps below.
    2. Publicizer for assembly_valheim:
        1. You will need to publicize all the required require dependencies. These dependencies can be found in
           DRIVE_PATH_TO_STEAM...Steam\steamapps\common\Valheim\valheim_Data\Managed
        2. Run .\programs\nuget.exe source add -Name nuget.org -Source https://api.nuget.org/v3/index.json
        3. Run .\programs\nuget.exe install Krafs.Publicizer -OutputDirectory packages
        4. This will install the automatic publicizer. It will publicize only the assembly_valheim file under libs.
        5. Alternatively To publicize, copy all the dependencies required into a separate folder.
           Install https://github.com/CabbageCrow/AssemblyPublicizer and run the GUI tool on that folder.
    3. Rename the __publicized assemblies back to their original names.
    4. Move those assemblies to the libs folder under ValheimRaft.
    5. Click compile. It should work.
