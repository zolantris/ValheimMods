# Getting Started

This guide aims to get you setup in the repo and able to build the code.

## Setup the repo

1. Clone / download this project
2. IDE: Download [Rider](https://www.jetbrains.com/rider/) or Visual studio. Use
   Rider if you do not want to waste tons of time resizing things or trying to
   switch between folder paths and solution paths. Visual
   Studio is not intuitive or user friendly.
3. (optional) Unity: Download Unity Hub
    - Unity is only required if you are changing scripting related to the
      prefabs
4. Running things
    1. Open the whole project with Rider. Typically you can double click on
       ValheimRAFT.sln to open it
    2. Unity -> Open the ValheimRaftUnity folder and install the Unity 2020
       version
    3.
    Install [AssetBundle Browser](https://github.com/Unity-Technologies/AssetBundles-Browser)
    from github master url
    or release url. Since it's not updating master is okay.
        - copy
          paste https://github.com/Unity-Technologies/AssetBundles-Browser.git
          into the input under this interface
          in Unity. Window > Package Manager > (click plus arrow > click add
          package from giturl

## Compiling

1. Compiling with the current branch libs folder
    - You will not need to do anything. These libs are pre-built off the latest
      supported valheim, jotunn, bepinex.
    - Skip this step If -
        - you need to fix an issue after valheim updates, you will need to
          update, re-publicize, download newer bepinex
          and harmony references etc.
2. Manual Compiling
    1. Do not reference the libs folder
    2. In the valheim.targets folder change the paths of PropertyGroup with "
       LocalPaths" label. Make the paths match your install folders for
       BepInExPlugins, BepInExPath to get to Harmony and BepInEx.dll and the
       game folder
        - alternatively just copy paste and override all the libs folders from
          the GameDirectory and download the new Jotunn and other required
          dependencies.
3. Create a `.props` file at the top level of the project

Add the following data.

```xml
    <PropertyGroup Label="LocalPaths">
        <BepInExPath><USER_HOME_DIR>\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\valheim_raft_debugging\BepInEx\core</BepInExPath>
        <ManagedDataPath>C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed</ManagedDataPath>
    </PropertyGroup>
```

Alternatively copy pasting the Valheim\valheim_Data\Managed and other require
dependencies into "libs" will make this step unnecessary.

## Publicizing

### Publicizing Manually

Publicizing manually is not needed. There is a nuget package that handles this
for us. We can drop the valheim reference into the libs folder or map to the
game directory and the publicizer will automatically update the lib.
If nuget or your IDE Rider/Visual Studio do not install the provided nuget
package then you may need to run `nuget restore`

### Manual Copy Paste publicizing

Publicizer for assembly_valheim steps.

1. Alternatively To publicize, copy all the dependencies required into a
   separate folder.
2. Install https://github.com/CabbageCrow/AssemblyPublicizer and run the GUI
   tool on that folder.
3. Rename the __publicized assemblies back to their original names.
4. Move those assemblies to the libs folder under ValheimRaft.
5. Click compile. It should work.