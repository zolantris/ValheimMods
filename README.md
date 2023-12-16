# ValheimRAFT

A [ValheimRaft](https://www.nexusmods.com/valheim/mods/1136) fork that works with latest Valheim. The original mod
owner [Sarcenzzz](https://www.nexusmods.com/valheim/users/3061574) abandoned the
mod, this fork aims to keep the mod functional with future goals of expanding functionality.

## Meshes

In Mod versions lower than `1.5.1` there were problems with the folder being renamed. In >=1.5.1 there is a
configuration
manager option to change the path to resolve the ValheimRAFT folder.

If you want meshes (IE sails and ropes ) to render, your mod must either be named `ValheimRaft`
or `zolantris-ValheimRaft`.

Otherwise make sure to edit the "pluginFolderName" key and add the folder name for ValheimRaft located
within the BepInEx\Plugins path. Afterwards relaunch the game. There should be no mesh issues.

## Community

Join us on discord to get the latest information, upcoming features, and discover other mods.

[Discord Link](https://discord.gg/XqA7j2qgRZ)

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
    2. You will need to publicize all the required require dependencies. These dependencies can be found in
       DRIVE_PATH_TO_STEAM...Steam\steamapps\common\Valheim\valheim_Data\Managed
    3. To publicize, copy all the dependencies required into a separate folder.
       Install https://github.com/CabbageCrow/AssemblyPublicizer and run the GUI tool on that folder.
    4. Rename the __publicized assemblies back to their original names.
    5. Move those assemblies to the libs folder under ValheimRaft.
    6. Click compile. It should work.