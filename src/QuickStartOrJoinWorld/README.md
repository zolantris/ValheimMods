# QuickStartOrJoinWorld

A BepInEx mod for Valheim that enables rapid world startup and server joining without UI interaction. Perfect for both developers and players who want to skip menus and jump straight into the action.

## Overview

QuickStartOrJoinWorld is a **debug-only** mod that automatically loads a Valheim world or joins a server on startup. Once configured, simply launch the game and it will:

1. Automatically load your configured world
2. Select your configured character
3. Load the world completely
4. Place your character in the game

No manual UI interaction required—from launch to fully loaded in-world.

## Features

- **Auto-Load Worlds**: Automatically load a saved world on startup
- **Auto-Join Servers**: Connect to a remote server without UI prompts
- **Player Selection**: Automatically select a configured player profile
- **Backend Configuration**: Choose between Steamworks and other online backends
- **Server Hosting**: Automatically host a world as open or public server
- **Password Protection**: Support for server passwords
- **Hands-Free Startup**: Launch and walk away—everything loads automatically

## Configuration

All settings are in the `QuickStartOrJoinWorld.cfg` config file under the `[QuickStartWorld]` section:

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `QuickStartEnabled` | bool | false | Enable/disable quick start functionality |
| `QuickStartWorldName` | string | "" | Name of the world to load |
| `QuickStartWorldPlayerName` | string | "" | Character/player name to use |
| `QuickStartWorldPassword` | string | "" | Password for the hosted world |
| `ServerOnlineBackendType` | enum | Steamworks | Online backend (Steamworks, etc.) |
| `IsJoinServer` | bool | false | Join remote server instead of hosting |
| `JoinServerUrl` | string | "" | IP or URL of remote server |
| `JoinServerPort` | int | 2456 | Port of remote server |
| `IsOpenServer` | bool | false | Allow other players to connect to hosted world |
| `IsPublicServer` | bool | false | List hosted world publicly |

## Usage Example

To automatically load a world on startup:

```ini
[QuickStartWorld]
QuickStartEnabled=true
QuickStartWorldName=MyWorld
QuickStartWorldPlayerName=MyCharacter
ServerOnlineBackendType=Steamworks
```

Launch the game—your world and character load automatically.

To automatically join a remote server:

```ini
[QuickStartWorld]
QuickStartEnabled=true
IsJoinServer=true
JoinServerUrl=192.168.1.100
JoinServerPort=2456
QuickStartWorldPlayerName=MyCharacter
ServerOnlineBackendType=Steamworks
```

Launch the game—you're connected and in the world.

## Use Cases

**For Players:**
- Skip menu navigation every time you launch
- Quick load into your favorite world
- Automatically join your favorite multiplayer server
- Streamlined gaming experience

**For Developers & Modders:**
- Rapid iteration during development cycles
- Skip menu navigation during testing
- Automated testing with consistent game state
- CI/CD integration for game testing pipelines
- Load specific configurations for mod testing

## How It Works

When enabled and properly configured, the mod patches the `FejdStartup.Start` method to intercept game initialization. Before the main menu renders, it:

1. Resolves your configured world and player profile
2. Sets the online backend (Steamworks, etc.)
3. Either hosts the world or connects to the configured server
4. Loads the main scene directly
5. Your character spawns and the world is fully loaded

No menus. No clicking. Just gameplay.

## Debug-Only

This mod is **debug-only** and only activates in Debug builds. It will not run in Release builds.

## Dependencies

- BepInEx
- HarmonyLib

## License

GNU General Public License v3.0

