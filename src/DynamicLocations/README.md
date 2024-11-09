# Dynamic Locations

A mod tool that Supports teleporting players to spawnpoints or logout points on
moving objects.

This tool is meant to be used with other mods. Please review the API. Currently
there is only support for login integrations but eventually spawn will be added.

## Possible Future plans

1. Spawning nearby other players (mobile spawn)
2. Dynamic Spawning logic based on distance / combat
3. GUI menu for choosing a spawn.

## API

Other mods can integrate with this mod.

This api allows subscribing to login controller api and then firing off a custom
action.

Example with
[ValheimVehicles Integration](https://github.com/zolantris/ValheimMods/tree/main/src/ValheimVehicles/ValheimVehicles.ModSupport/DynamicLocationsLoginIntegration.cs)

### Steps

1. Create an integration by using `DynamicLoginIntegration` class and extending
   it's virtual methods.
2. Make sure to depend upon DynamicLocations. It also requires ZDOWatcher as a
   dependency (all within ValheimRAFT published mod for now.)
3. Call `LoginApiController.AddLoginApiIntegration`.
4. Your integration should be available. You can valid it has been integrated
   with `dynamic-locations list-integrations` command. See
   below [command classes](#2-listallkeyscommand)

## API docs for DynamicLogin integration

Here's the refined documentation for the public methods and properties of the
`DynamicLoginIntegration` class, excluding any protected and internal members:

---

# DynamicLoginIntegration Class Documentation

## Overview

The `DynamicLoginIntegration` class enables seamless integration of login
functionality within the Dynamic Locations mod framework, allowing other mods to
connect their login systems with minimal configuration.

## Properties

- **LoginPrefabHashCode**:
    - Gets the hash code of the prefab associated with the login integration.

- **ShouldFreezePlayer**:
    - Indicates whether the player should be frozen during the login process.

- **RunBeforePlugins**:
    - A list of plugins that should execute before this integration.

- **RunAfterPlugins**:
    - A list of plugins that should execute after this integration.

- **Priority**:
    - Returns the priority of the integration; defaults to 999 if the configured
      priority is less than or equal to zero.

- **MovementTimeoutMs**:
    - Returns the timeout duration (in milliseconds) for the player's movement
      to a specified ZDO (Zonally Distributed Object).

- **IsComplete**:
    - Indicates whether the login movement process has been completed.

- **Config**:
    - Contains the configuration settings for the integration.

## Constructors

### `DynamicLoginIntegration(IntegrationConfig config)`

Initializes a new instance of the `DynamicLoginIntegration` class with the
specified configuration.

- **Parameters**:
    - `config`: An `IntegrationConfig` object containing the configuration
      settings for this integration.

## Methods

###

`static IntegrationConfig CreateConfig(BaseUnityPlugin plugin, string zdoTargetPrefabName)`

Creates a new `IntegrationConfig` instance using the specified plugin and target
prefab name.

- **Parameters**:
    - `plugin`: The `BaseUnityPlugin` used to extract the plugin's GUID,
      version, and name for debugging.
    - `zdoTargetPrefabName`: The name of the prefab target for the ZDO.

- **Returns**:
    - A newly created `IntegrationConfig` object.

###

`IEnumerator API_OnLoginMoveToZDO(ZDO zdo, Vector3? offset, PlayerSpawnController playerSpawnController)`

Invoked by the APIController when the player should move to a specified ZDO upon
login. This method should not be overridden.

- **Parameters**:
    - `zdo`: The ZDO to which the player should be moved.
    - `offset`: An optional offset vector to adjust the player's position.
    - `playerSpawnController`: The `PlayerSpawnController` instance responsible
      for managing player movements.

- **Returns**:
    - An enumerator that allows coroutine execution to handle the movement
      process.

###

`virtual IEnumerator OnLoginMoveToZDO(ZDO zdo, Vector3? offset, PlayerSpawnController playerSpawnController)`

Can be overridden by mods to implement custom logic for moving the player to a
ZDO upon login.

- **Parameters**:
    - `zdo`: The ZDO to which the player should be moved.
    - `offset`: An optional offset vector to adjust the player's position.
    - `playerSpawnController`: The `PlayerSpawnController` instance responsible
      for managing player movements.

- **Returns**:
    - An enumerator for coroutine execution that may handle custom player
      movement logic.

### `public virtual bool OnLoginMatchZdoPrefab(ZDO zdo)`

Checks if the provided ZDO matches the prefab hash code configured for the login
integration. This method may be overridden by mods that require different
matching criteria.

- **Parameters**:
    - `zdo`: The ZDO to check for a match.

- **Returns**:
    - `true` if the ZDO matches the prefab hash code; otherwise, `false`.

---

This documentation focuses solely on the relevant public aspects of the
`DynamicLoginIntegration` class. If you need further modifications or additional
sections, feel free to ask!

---

## Overview (ChatGPT 4 generated)

The `DynamicLocationsConfig` class is responsible for managing the configuration
settings of the Dynamic Locations mod for BepInEx. This documentation outlines
the available configuration entries, their descriptions, and default values.

## Configuration Entries

### Main Configuration Section

1. **DEBUG_ShouldNotRemoveTargetKey**
    - **Type**: `bool`
    - **Default**: `true`
    - **Description**: Prevents the removal of data on the player during
      debugging. This is intended for troubleshooting spawn point issues. Should
      not be enabled in production builds.

2. **LocationControlsTimeoutInMs**
    - **Type**: `int`
    - **Default**: `20000`
    - **Description**: Sets the delay for exiting the spawn logic to prevent
      degraded performance. Acceptable range: 1000 to 40000 milliseconds.

3. **HasCustomSpawnDelay**
    - **Type**: `bool`
    - **Default**: `false`
    - **Description**: Enables a custom spawn delay to speed up the game.

4. **CustomSpawnDelay**
    - **Type**: `float`
    - **Default**: `1f`
    - **Description**: Sets a custom spawn delay to expedite the respawn and
      login process. Values above 10 seconds are not supported.

5. **DisabledLoginApiIntegrations**
    - **Type**: `string`
    - **Default**: `""`
    - **Description**: A comma-separated list of disabled plugins by GUID or
      name. This will force-disable any matching plugins (e.g.,
      `"zolantris.ValheimRAFT"`).

6. **EnableDynamicSpawnPoint**
    - **Type**: `bool`
    - **Default**: `true`
    - **Description**: Allows users to respawn in a new area of the map if a
      vehicle has moved.

7. **EnableDynamicLogoutPoint**
    - **Type**: `bool`
    - **Default**: `true`
    - **Description**: Enables users to log in to a new area of the map if a
      vehicle has moved.

8. **RespawnHeightOffset**
    - **Type**: `int`
    - **Default**: `0`
    - **Description**: Sets the respawn height for beds. Useful to ensure
      players spawn above the bed instead of within it. Acceptable range: -5 to
        10.

9. **debug**
    - **Type**: `bool`
    - **Default**: `false`
    - **Description**: Enables additional logging and debug visuals around spawn
      and logout points for troubleshooting.

### Debug Configuration Section

1. **DebugDisableFreezePlayerTeleportMechanics**
    - **Type**: `bool`
    - **Default**: `false`
    - **Description**: Disables the freezing of players during teleportation.
      Only disable if you understand the implications.

2. **DebugDisableDistancePortal**
    - **Type**: `bool`
    - **Default**: `false`
    - **Description**: Disables distance portal mechanics. Disabling this could
      break portal functionality.

3. **DebugForceUpdatePositionDelay**
    - **Type**: `float`
    - **Default**: `0f`
    - **Description**: Sets a delay for forcefully updating position. Acceptable
      range: 0 to 5 seconds.

4. **DebugForceUpdatePositionAfterTeleport**
    - **Type**: `bool`
    - **Default**: `false`
    - **Description**: Forces an update of position after teleportation.
      Disabling this could break portal functionality.

## Event Listeners

- The `DisabledLoginApiIntegrationsString` configuration entry has a listener
  that updates integrations whenever its value changes.

## Usage

To bind the configuration, call `DynamicLocationsConfig.BindConfig(configFile)`
where `configFile` is an instance of `ConfigFile`.

## Conclusion

This configuration class provides essential options to customize the behavior of
the Dynamic Locations mod in a user-friendly manner. Be sure to adjust settings
based on your gameplay needs while considering their impact on performance and
game mechanics.

Here's the structured documentation for the commands defined in your
`DynamicLocations.Commands` namespace:

---

## Command Classes

### 1. **KeyValueSerializer**

A utility class for serializing objects into a string format.

#### Methods

- **Serialize(object obj, int indentLevel = 0)**
    - **Parameters**:
        - `obj`: The object to serialize.
        - `indentLevel`: The level of indentation (default is 0).
    - **Returns**: A serialized string representation of the object.

### 2. **ListAllKeysCommand**

Lists all dynamic location keys in the current world.

#### Properties

- **Name**: `"list-all-keys"`
- **Help**: `"list all keys"`

#### Methods

- **Run(string[] args)**
    - **Parameters**:
        - `args`: Command arguments.
    - **Behavior**: Calls the `LocationController.DEBUGCOMMAND_ListAllKeys()`
      method to list all keys if arguments are provided.

### 3. **ModIntegrationsCommand**

Lists all mod integrations that use the Dynamic Locations plugin.

#### Properties

- **Name**: `"mod-integrations"`
- **Help**:
    - Lists all mods using this plugin.
    - To see detailed object information, use the `-v` or `--verbose` flag.

#### Methods

- **Run(string[] args)**
    - **Parameters**:
        - `args`: Command arguments.
    - **Behavior**: Calls `ModIntegrationsCommands(bool isVerbose)` to list
      integrations based on the verbosity flag.

- **CommandOptionList()**: Returns a list of command options, including `-v`.

### 4. **MoveToCommand**

Moves the player to a specified location type.

#### Properties

- **Name**: `"move-to"`
- **Help**: `"Moves to logout point. Requires Admin privileges"`

#### Methods

- **Run(string[] args)**
    - **Parameters**:
        - `args`: Command arguments.
    - **Behavior**: Parses the first argument as a location type and moves the
      player to that location if valid.

- **CommandOptionList()**: Returns a list of location options, including
  `LocationVariationUtils.LogoutString` and
  `LocationVariationUtils.SpawnString`.

### 5. **DynamicLocationsCommands**

Embeds various commands related to dynamic locations.

#### Properties

- **Name**: `"dynamic-locations"`
- **Help**: Provides help information for all commands in the dynamic locations
  context.

#### Methods

- **Run(string[] args)**
    - **Parameters**:
        - `args`: Command arguments.
    - **Behavior**: Parses the first argument and executes the corresponding
      command.

- **CommandOptionList()**: Returns a list of available commands, including
  `playerClearAll`, `playerClearLogout`, `MoveToCommand`, `ListAllKeysCommand`,
  and `ModIntegrationsCommand`.

#### Private Methods

- **OnHelp()**: Returns a formatted help string for available commands.
- **GetCommandArg(string commandString)**: Matches command strings to their
  corresponding enum value for command execution.
- **PlayerClearAll(string[] args)**: Clears all dynamic login and spawn points
  for a player in the current world.
- **PlayerClearLogout()**: Clears the logout point for the current world.

## Usage

To use these commands, you can invoke them from the console with the specified
names and required parameters. Admin privileges are required for certain
commands like `MoveToCommand`.

### Example Commands

- `list-all-keys`: Lists all dynamic location keys.
- `mod-integrations -v`: Lists all mod integrations in verbose mode.
- `move-to logout`: Moves the player to the logout point (requires admin).

---

Feel free to adjust or expand on any section as needed!