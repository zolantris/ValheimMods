# ValheimMods

A collection of Valheim mods maintained by zolantris. Many of these mods were previously maintained by other authors.

Some mods may be maintained only, other mods may be actively worked on

## Mods

Each mod has the link to the folder and mentions activity. 

The following statuses will be used to label repos.

- LTS: Long-term support. Features will be released to fix bugs and for valheim compatibility only.
- Active: Active development. Features will be released on a weekly cadence. 
- Deprecated: No longer supported, features will not be released to fix bugs.

### Supported Mods

| Mod Name                                                   | Status     | Description                                                                                                   | 
|------------------------------------------------------------|------------|---------------------------------------------------------------------------------------------------------------|
| [ValheimRAFT][ValheimRAFT_Dir]                             | **Active** | Allows Valheim Build system on the water, similar to the Raft Game                                            |
| [BuildingDamageModExtended][BuildingDamageModExtended_Dir] | **LTS**    | Allows setting building damage multipliers based on entity types and also additionally allows for damage caps |


## Contributing

Go [here](docs/CONTRIBUTING.md) for more information.

## Support Open Source

<a href='https://ko-fi.com/zolantris' target='_blank'><img height='35' style='border:0px;height:46px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com'></a>

## Want Valheim To Run Faster?

Add this to `valheim_Data/boot.config folder. This should allow script heavy mods to run on more threads without bottlenecking the game UI so fps will be much higher.

The gc-max-time-slice takes a integer that should be the number of threads available on your machine. Example below for a machine with 20 threads 
```
gc-max-time-slice=20
```

[ValheimRAFT_Dir]: src/ValheimRAFT
[BuildingDamageModExtended_Dir]: src/ValheimRAFT