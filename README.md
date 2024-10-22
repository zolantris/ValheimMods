# ValheimMods

A collection of Valheim mods maintained by zolantris. Many of these mods were
previously maintained by other authors.

Some mods may be maintained only, other mods may be actively worked on

## Mods

Each mod has the link to the folder and mentions activity.

The following statuses will be used to label repos.

- LTS: Long-term support. Features will be released to fix bugs and for valheim
  compatibility only.
- Active: Active development. Features will be released on a weekly cadence.
- Deprecated: No longer supported, features will not be released to fix bugs.

### Supported Mods

| Mod Name                                                   | Status     | Description                                                                                                   | 
|------------------------------------------------------------|------------|---------------------------------------------------------------------------------------------------------------|
| [ValheimRAFT][ValheimRAFT_Dir]                             | **Active** | Allows Valheim Build system on the water, similar to the Raft Game                                            |
| [YggdrasilTerrain][YggdrassilTerrain_Dir]                             | **Active** | Allows for walking, building, and colliding with the Yggdrasil Branch. Adds teleport commands and collision config to futureproof it.                                           |
| [BuildingDamageModExtended][BuildingDamageModExtended_Dir] | **LTS**    | Allows setting building damage multipliers based on entity types and also additionally allows for damage caps |

## Contributing

Go [here](docs/CONTRIBUTING.md) for more information.

## Support Open Source

<a href='https://ko-fi.com/zolantris' target='_blank'><img height='35' style='border:0px;height:46px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com'></a>

## Step Debugging ValheimMods

1. Download doorstop >=4.x.x
2. Place doorstop files into your BepInEx folder under r2modman profile/<name>
   or directly in valheim game directory if you are directly managing valheim (
   NOT RECOMMENDED)
3. Change the doorstop config to these values
    ```ini
    [General]
    enabled=true
    target_assembly=BepInEx\core\BepInEx.Preloader.dll
    redirect_output_log=false
    ignore_disable_switch=false
    [UnityMono]
    dll_search_path_override=
    debug_enabled=true
    debug_address=127.0.0.1:10000
    debug_suspend=false
    [Il2Cpp]
    coreclr_path=
    corlib_dir=
    ```
   _Noting that doorstop_config.ini must be changed within the valheim game
   folder
   instead of the r2modman/thunderstore profile folder._
4. Copy and overwrite the doorstop_4.x.x_libs/BepInEx.Preloader.dll into
   BepInEx/core and replace the current file.
5. For Rider IDE under BepInEx/config/BepInEx.cfg enable dump assemblies. (set
   to true)
6. In Rider connect the debugger via attach to process. Create a custom process
   call it `Valheim` add the IP which is the debug_address e.g. `127.0.0.1` and
   the port `10000`
7. add a breakpoint in your mod to pause things.
8. alternative wait for a game error to throw and it will pause and decompile
   that assembly!

## Want Valheim To Run Faster?

Add this to `valheim_Data/boot.config folder. This should allow script heavy
mods to run on more threads without bottlenecking the game UI so fps will be
much higher.

The gc-max-time-slice takes a integer that should be the number of (seconds?) before garbage collecting on the machine. This essentially freeze up your machine to do thing without worry until you get near max memory and then it will garbage collect anyways.

Example below for a machine with 20 seconds before force attempting to garbage collect even when low on memory. 20 seems to be a nice fps boost of 30-60fps. 

```
gc-max-time-slice=20
```

[ValheimRAFT_Dir]: src/ValheimRAFT
[YggdrassilTerrain_Dir]: src/YggdrasilTerrain

[BuildingDamageModExtended_Dir]: src/ValheimRAFT
