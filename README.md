# ValheimRAFT

A [ValheimRaft](https://www.nexusmods.com/valheim/mods/1136) fork that works with latest Valheim. The original mod
owner [Sarcenzzz](https://www.nexusmods.com/valheim/users/3061574) abandoned the
mod, this fork aims to keep the mod functional with future goals of expanding functionality.

## Build Status

[![ValheimRAFT Build](https://github.com/zolantris/ValheimRaft/actions/workflows/build-release.yml/badge.svg)](https://github.com/zolantris/ValheimRaft/actions/workflows/build-release.yml)

# Single Player Support

Everything works well.

Small issues

- There may be some object destruction issues related to creating/spawning boats on top of each
  other, (don't do it)

## Server Support

Important! **Please UNINSTALL ValheimRAFT from your server**. At this time the **server** and the **client** do not work
well
together.

The following as been tested:

- Client only with a server IE (connecting to server without mod),
    - creating a boat and logging on and off the server does not destroy the boat.
    - sailing and teleporting does not destroy the boat.
    - The client will still be able to create items and the server will keep those raft items
    - restarting/stopping/starting server keeps the boat.
    - Removing the ValheimRAFT mod from the client will make the server glitch out the boat items.
    - joining back (after adding the mod again) will render the items correctly.
- Server & Client (BROKEN)
    - If you use both, the server will garbage collect the client's IDs because they are considered invalid.
    - If you use both the server will break the client mod items due to there being a breakage.

The server issues are not being tabled, but they won't be fixed for a bit.

- There needs to be a bunch of logging added to areas that do the object destruction and validation.
- Once that's done we can look at the broken areas and compare them with the client IDs to see why things are not
  working.

## Features

- Build a RAFT on the water using Valheim's building prefabs.
- Adds a climbable ladder
- Add piers that reach the ocean floor (stone and log)
- Adds anchors
- Adds custom sails (requires meshes to be working)
- Adds ropes (requires meshes to be working)

## Community

Join us on discord to get the latest information, upcoming features, and discover other mods.

[Discord Link](https://discord.gg/Kcw4d97ez3)

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

## Meshes

In Mod versions lower than `1.5.0` there were problems with the folder being renamed. In `>=1.5.0` there is a
configuration
manager option to change the path to resolve the ValheimRAFT folder.

If you want meshes (IE sails and ropes ) to render automatically, your mod must either be named `ValheimRAFT`
or `zolantris-ValheimRAFT`.

Otherwise make sure to edit the `pluginFolderName` key and add the folder name for ValheimRaft located
within the BepInEx\Plugins path. Afterwards relaunch the game. There should be no mesh issues.

### Getting Started

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
