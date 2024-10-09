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
    <!-- basically, this is needed to run valheim through the configuration -->
    <PropertyGroup Label="ValheimPaths">
        <!-- use GamePath property if the game is not located in the steam root folder -->
        <GamePath>%Steam_Library_Path%\steamapps\common\Valheim</GamePath>
        <ValheimServerPath>$(GamePath) dedicated server</ValheimServerPath>
        <R2ModManPath>%APPDATA%\r2modmanPlus-local</R2ModManPath>
        <R2ModManProfileName>profile-name</R2ModManProfileName>
        <R2ModManProfile>Valheim\profiles\$(R2ModManProfileName)</R2ModManProfile>
        <PluginDeployTarget>BepInEx\plugins\zolantris-ValheimRAFT</PluginDeployTarget>
        <PluginDeployPath>$(R2ModManPath)\$(R2ModManProfile)\$(PluginDeployTarget)</PluginDeployPath>
    </PropertyGroup>
    <PropertyGroup Label="LocalPaths">
        <BepInExPath>$(R2ModManPath)\$(R2ModManProfile)\BepInEx\core</BepInExPath>
        <ManagedDataPath>$(GamePath)\valheim_Data\Managed</ManagedDataPath>
    </PropertyGroup>
```

Alternatively copy pasting the Valheim\valheim_Data\Managed and other require
dependencies into "libs" will make this step unnecessary.

> Note: if you still see a lot of errors after importing all the libraries, this
> may be due to the fact that your IDE does not support the latest version of
> C#.
> Support depends on the version of the NET
> SDK [(see)](https://dotnet.microsoft.com/en-us/download).

## Publicizing

### Publicizing Manually

Publicizing manually is not needed. There is a nuget package that handles this
for us. We can drop the valheim reference into the libs folder or map to the
game directory and the publicizer will automatically update the lib.
If nuget or your IDE Rider/Visual Studio do not install the provided nuget
package then you may need to run `nuget restore`

### Manual Copy Paste publicizing

Publicizer for assembly_valheim steps.

1. Alternatively to publicize, copy all the dependencies required into a
   separate folder.
2. Install https://github.com/CabbageCrow/AssemblyPublicizer and run the GUI
   tool on that folder.
3. Rename the __publicized assemblies back to their original names.
4. Move those assemblies to the libs folder under ValheimRaft.
5. Click compile. It should work.

## Installing nuget packages

### With Rider

use Rider and click nuget (should be bottom left panel, one of the items), and
search for the package

### with nuget.exe (not recommended)

1. Install nuget.exe
2. For installing sentry as an example run.
    ```powershell
    ..\..\programs\nuget.exe install Sentry -Version 3.41.3 -OutputDirectory Packages`
    ```

The example above is if the path for nuget exists 2 directories above this repo
and within programs folder.

## Custom Logging for powershell

Add this to your powershell profile. This will allow your to highlight only logs
from ValheimVehicles and ValheimRAFT mods as well as
errors. [(about profiles)](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_profiles)

These commands are for powershell users. If you are running things on linux
there are simpler (and similar) commands for syntax highlighting. I advise
googling them.

```powershell
function Get-LogColor {
    Param([Parameter(Position=0)]
    [String]$LogEntry)

    process {
        if ($LogEntry.Contains("ValheimRAFT") -or $LogEntry.Contains("ValheimVehicles")){
          if ($LogEntry.Contains("Debug")) {Return "Green"}
          elseif ($LogEntry.Contains("Warn")) {Return "Yellow"}
          elseif ($LogEntry.Contains("Error") -or $LogEntry.Contains("NullReferenceException")) {Return "Red"}
          else {Return "White"}
        }
        if ($LogEntry.Contains("NullReferenceException")) {Return "Red"}
        else {Return "White"}
    }
}
```

When launching the game just run the following command to output the latest
logs.

```powershell
gc -wait -tail 10 C:\Users\__username__\AppData\LocalLow\IronGate\Valheim\Player.log | ForEach {Write-Host -ForegroundColor (Get-LogColor $_) $_}
```

## Dev Builds

Please reference [ValheimDebugging](./ValheimDebugging.md)