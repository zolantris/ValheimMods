
## Debug

### Enable Sentry Metrics (requires sentryUnityPlugin) 
- Description: Enable sentry debug logging. Requires sentry logging plugin installed to work. Sentry Logging plugin will make it easier to troubleshoot raft errors and detect performance bottlenecks. The bare minimum is collected, and only data related to ValheimRaft. See https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT#logging-metrics for more details about what is collected
- Default Value: true

### Debug logging for Vehicle/Raft 
- Description: Outputs more debug logs for the Vehicle components. Useful for troubleshooting errors, but will spam logs
- Default Value: false

## Deprecated Config

### Initial Floor Height (V1 raft) 
- Description: Allows users to set the raft floor spawn height. 0.45 was the original height in 1.4.9 but it looked a bit too low. Now people can customize it
- Default Value: 0.6

## Config

### pluginFolderName 
- Description: Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their manager renames the folder, r2modman has a fallback case added to search for zolantris-ValheimRAFTDefault search values are an ordered list first one is always matching non-empty strings from this pluginFolderName.Folder Matches are:  zolantris-ValheimRAFT, zolantris-ValheimRAFT Zolantris-ValheimRAFT, and ValheimRAFT
- Default Value: 

## Debug

### RemoveStartMenuBackground 
- Description: Removes the start scene background, only use this if you want to speedup start time
- Default Value: true

## Server config

### Protect Vehicle pieces from breaking on Error 
- Description: Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer
- Default Value: true

### AdminsCanOnlyBuildRaft 
- Description: ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart
- Default Value: false

### AllowOldV1RaftRecipe 
- Description: Allows the V1 Raft to be built, this Raft is not performant, but remains in >=v2.0.0 as a Fallback in case there are problems with the new raft
- Default Value: false

### AllowExperimentalPrefabs 
- Description: Allows >=v2.0.0 experimental prefabs such as Iron variants of slabs, hulls, and ribs. They do not look great so they are disabled by default
- Default Value: false

## Rendering

### Force Ship Owner Piece Update Per Frame 
- Description: Forces an update during the Update sync of unity meaning it fires every frame for the Ship owner who also owns Physics. This will possibly make updates better for non-boat owners. Noting that the boat owner is determined by the first person on the boat, otherwise the game owns it.
- Default Value: false

## Server config

### ServerRaftUpdateZoneInterval 
- Description: Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.
- Default Value: 5

### MakeAllPiecesWaterProof 
- Description: Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.
- Default Value: true

### AllowFlight 
- Description: Allow the raft to fly (jump\crouch to go up and down)
- Default Value: false

### AllowCustomRudderSpeeds 
- Description: Allow the raft to use custom rudder speeds set by the player, these speeds are applied alongside sails at half and full speed. See advanced section for the actual speed settings.
- Default Value: true

## Config

### RaftCreativeHeight 
- Description: Sets the raftcreative command height, raftcreative is relative to the current height of the ship, negative numbers will sink your ship temporarily
- Default Value: 5

## Floatation

### Only Use Hulls For Floatation Collisions 
- Description: Makes the Ship Hull prefabs be the sole source of collisions, meaning ships with wider tops will not collide at bottom terrain due to their width above water. Requires a Hull, without a hull it will previous box around all items in ship
- Default Value: true

## Config

### AnchorKeyboardShortcut 
- Description: Anchor keyboard hotkey. Only applies to keyboard
- Default Value: LeftShift

## Vehicles

### HullFloatationColliderLocation 
- Description: Hull Floatation Collider will determine the location the ship floats and hovers above the sea. Average is the average height of all Vehicle Hull Pieces attached to the vehicle. The point calculate is the center of the prefab. Center is the center point of all the float boats. This center point is determined by the max and min height points included for ship hulls. Lowest is the lowest most hull piece will determine the float height, allowing users to easily raise the ship if needed by adding a piece at the lowest point of the ship. Custom allows for setting floatation between -20 and 20
- Default Value: Custom

### HullFloatation Custom Offset 
- Description: Hull Floatation Collider Customization. Set this value and it will always make the ship float at that offset, will only work when HullFloatationColliderLocation=Custom. Positive numbers sink ship, negative will make ship float higher.
- Default Value: 0

### EnableExactVehicleBounds 
- Description: Ensures that a piece placed within the raft is included in the float collider correctly. May not be accurate if the parent GameObjects are changing their scales above or below 1,1,1. Mods like Gizmo could be incompatible
- Default Value: true

## Debug

### ShowShipState 
- Description: 
- Default Value: true

## Propulsion

### MaxPropulsionSpeed 
- Description: Sets the absolute max speed a ship can ever hit. This is capped on the vehicle, so no forces applied will be able to exceed this value. 20-30f is safe, higher numbers could let the ship fail off the map
- Default Value: 45

### MaxSailSpeed 
- Description: Sets the absolute max speed a ship can ever hit with sails. Prevents or enables space launches, cannot exceed MaxPropulsionSpeed.
- Default Value: 30

### MassPercentage 
- Description: Sets the mass percentage of the ship that will slow down the sails
- Default Value: 55

### SpeedCapMultiplier 
- Description: Sets the speed at which it becomes significantly harder to gain speed per sail area
- Default Value: 1

### Rudder Back Speed 
- Description: Set the Back speed of rudder, this will apply with sails
- Default Value: 5

### Rudder Slow Speed 
- Description: Set the Slow speed of rudder, this will apply with sails
- Default Value: 4

### Rudder Half Speed 
- Description: Set the Half speed of rudder, this will apply with sails
- Default Value: 5

### Rudder Full Speed 
- Description: Set the Full speed of rudder, this will apply with sails
- Default Value: 30

### HasShipWeightCalculations 
- Description: enables ship weight calculations for sail-force (sailing speed) and future propulsion, makes larger ships require more sails and smaller ships require less
- Default Value: true

### HasShipContainerWeightCalculations 
- Description: enables ship weight calculations for containers which affects sail-force (sailing speed) and future propulsion calculations. Makes ships with lots of containers require more sails
- Default Value: false

## Debug

### HasDebugSails 
- Description: Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only.
- Default Value: false

## Propulsion

### EnableCustomPropulsionConfig 
- Description: Enables all custom propulsion values
- Default Value: false

### SailCustomAreaTier1Multiplier 
- Description: Manual sets the sail wind area multiplier the custom tier1 sail. Currently there is only 1 tier
- Default Value: 0.5

### SailTier1Area 
- Description: Manual sets the sail wind area of the tier 1 sail.
- Default Value: 5

### SailTier2Area 
- Description: Manual sets the sail wind area of the tier 2 sail.
- Default Value: 10

### SailTier3Area 
- Description: Manual sets the sail wind area of the tier 3 sail.
- Default Value: 20

### SailTier4Area 
- Description: Manual sets the sail wind area of the tier 4 sail.
- Default Value: 40

### FlightVerticalToggle 
- Description: Flight Vertical Continues UntilToggled: Saves the user's fingers by allowing the ship to continue to climb or descend without needing to hold the button
- Default Value: true

### FlightHasRudderOnly 
- Description: Flight allows for different rudder speeds. Use rudder speed only. Do not use sail speed.
- Default Value: false

## Graphics

### Sails Fade In Fog 
- Description: Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled
- Default Value: true

## Sounds

### Ship Sailing Sounds 
- Description: Toggles the ship sail sounds.
- Default Value: true

### Ship Wake Sounds 
- Description: Toggles Ship Wake sounds. Can be pretty loud
- Default Value: true

### Ship In-Water Sounds 
- Description: Toggles ShipInWater Sounds, the sound of the hull hitting water
- Default Value: true

## Patches

### DynamicLocations 
- Description: Enables DynamicLocations mod to access ValheimRAFT/Vehicles identifiers.
- Default Value: true

### ComfyGizmo - Enable Patch 
- Description: Patches relative rotation allowing for copying rotation and building while the raft is at movement, this toggle is only provided in case patches regress anything in Gizmos and players need a work around.
- Default Value: false

### ComfyGizmo - Vehicle Creative zero Y rotation 
- Description: Vehicle/Raft Creative mode will set all axises to 0 for rotation instead keeping the turn axis. Gizmo has issues with rotated vehicles, so zeroing things out is much safer. Works regardless of patch if mod exists
- Default Value: true

### Vehicles Prevent Pausing 
- Description: Prevents pausing on a boat, pausing causes a TON of desync problems and can make your boat crash or other players crash
- Default Value: true

### Vehicles Prevent Pausing SinglePlayer 
- Description: Prevents pausing on a boat during singleplayer. Must have the Vehicle Prevent Pausing patch as well
- Default Value: true

### Enable PlanBuild Patches (required to be on if you installed PlanBuild) 
- Description: Fixes the PlanBuild mod position problems with ValheimRaft so it uses localPosition of items based on the parent raft. This MUST be enabled to support PlanBuild but can be disabled when the mod owner adds direct support for this part of ValheimRAFT. PlanBuild mod can be found here. https://thunderstore.io/c/valheim/p/MathiasDecrock/PlanBuild/
- Default Value: false

## Rams

### ramDamageEnabled 
- Description: Will keep the prefab available for aethetics only, will not do any damage nor will it initialize anything related to damage. Alternatives are using the damage tweaks.
- Default Value: true

### maximumDamage 
- Description: Maximum damage for all damages combined. This will throttle any calcs based on each damage value. The throttling is balanced and will fit the ratio of every damage value set. This allows for velocity to increase ram damage but still prevent total damage over specific values
- Default Value: 200

### maxDamageCap 
- Description: enable damage caps
- Default Value: true

### slashDamage 
- Description: slashDamage for Ram Blades. the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.
- Default Value: 0

### bluntDamage 
- Description: bluntDamage the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.
- Default Value: 10

### chopDamage 
- Description: chopDamage for Ram Blades excludes Ram Stakes. the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.. Will damage trees dependending on tool tier settings
- Default Value: 5

### pickaxeDamage 
- Description: pickDamage the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass. Will damage rocks as well as other entities
- Default Value: 220

### pierceDamage 
- Description: Pierce damage for Ram Stakes. the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass. Will damage rocks as well as other entities
- Default Value: 55

### percentageDamageToSelf 
- Description: Percentage Damage applied to the Ram piece per hit. Number between 0-1.
- Default Value: 0.01

### AllowContinuousDamage 
- Description: Rams will continue to apply damage based on their velocity even after the initial impact
- Default Value: true

### RamDamageToolTier 
- Description: allows rams to damage both rocks, ores, and higher tier trees and/or prefabs. Tier 1 is bronze. Setting to 100 will allow damage to all types of materials
- Default Value: 100

### CanHitCharacters 
- Description: allows rams to hit characters/entities
- Default Value: true

### CanHitEnemies 
- Description: allows rams to hit enemies
- Default Value: true

### CanHitFriendly 
- Description: allows rams to hit friendlies
- Default Value: true

### CanDamageSelf 
- Description: allows rams to be damaged. The values set for the damage will be calculated
- Default Value: true

### CanHitEnvironmentOrTerrain 
- Description: allows rams to hit friendlies
- Default Value: true

### HitRadius 
- Description: The base ram hit radius area. Stakes are always half the size, this will hit all pieces within this radius, capped between 0.1 and 10 for balance and framerate stability
- Default Value: 40

### RamHitInterval 
- Description: Every X seconds, the ram will apply this damage
- Default Value: 1

### RamsCanBeRepaired 
- Description: Allows rams to be repaired
- Default Value: false

### minimumVelocityToTriggerHit 
- Description: Minimum velocity required to activate the ram's damage
- Default Value: 1

### RamMaxVelocityMultiplier 
- Description: Damage of the ram is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.
- Default Value: 1

### DamageIncreasePercentagePerTier 
- Description: Damage Multiplier per tier. So far only HardWood (Tier1) Iron (Tier3) available. With base value 1 a Tier 3 mult at 25% additive additional damage would be 1.5. IE (1 * 0.25 * 2 + 1) = 1.5
- Default Value: 0.25

## PrefabConfig

### RopeLadderEjectionPoint 
- Description: The place the player is placed after they leave the ladder. Defaults to Y +0.25 and Z +0.5 meaning you are placed forwards of the ladder.
- Default Value: {"x":0.0,"y":0.0,"z":0.0}

### startingPiece 
- Description: Allows you to customize what piece the raft initializes with. Admins only as this can be overpowered.
- Default Value: Hull4X8

### ropeLadderRunClimbSpeedMult 
- Description: Allows you to customize how fast you can climb a ladder when in run mode
- Default Value: 2

### ropeLadderHints 
- Description: Shows the controls required to auto ascend/descend and run to speedup ladder
- Default Value: true

## Vehicle Debugging

### DebugMetricsEnabled 
- Description: Will locally log metrics for ValheimVehicles mods. Meant for debugging functional delays
- Default Value: false

### DebugMetricsTimer 
- Description: The interval in seconds that the logs output. Lower is performance heavy. Do not have this set to a low value. Requires EnableDebugMetrics to be enabled to update.
- Default Value: 1

### HasAutoAnchorDelay 
- Description: For realism, the ship continues even when nobody is onboard. This is meant for debugging logout points but also could be useful for realism
- Default Value: false

### AutoAnchorDelayTimeInSeconds 
- Description: For realism, the ship continues for X amount of time until it either unrenders or a player stops it.
- Default Value: 10

### Always Show Vehicle Colliders 
- Description: Automatically shows the vehicle colliders useful for debugging the vehicle
- Default Value: false

### Vehicle Debug Menu 
- Description: Enable the VehicleDebugMenu. This shows a GUI menu which has a few shortcuts to debugging/controlling vehicles.
- Default Value: true

### UNSTABLE_PositionAutoFix 
- Description: UNSTABLE due to vehicle center point shifting and not being in center of actual vehicle pieces - Automatically moves the vehicle if buried/stuck underground. The close to 0 the closer it will be to teleporting the vehicle above the ground. The higher the number the more lenient it is. Recommended to keep this number above 10. Lower can make the vehicle trigger an infinite loop of teleporting upwards and then falling and re-telporting while gaining velocity
- Default Value: true

### positionAutoFixThreshold 
- Description: Threshold for autofixing stuck vehicles. Large values are less accurate but smaller values may trigger the autofix too frequently
- Default Value: 5

## Debug

### SyncShipPhysicsOnAllClients 
- Description: Makes all clients sync physics
- Default Value: false

## Propulsion

### VehiclePhysicsMode 
- Description: SyncRigidbody - Accurately syncs physics across clients, it causes jitters during high speed.
ForceSyncedRigidbody ignores all allowances that toggle SyncRigidbody related to flight. This will require a flight ascend value of 1 otherwise flight will be broken. Use this is there is problems with SyncRigidbody
DesyncedJointRigidbodyBody - is a new UNSTABLE (you have been warned) config that allows the player to smoothly move around the raft at high speeds even if they are not the host. Can cause the ship to glitch with anything that has to do with physics including ramps and other mods that add moving parts that could be added to the boat.
- Default Value: ForceSyncedRigidbody

### turningPowerNoRudder 
- Description: Set the base turning power of the steering wheel
- Default Value: 1

### turningPowerWithRudder 
- Description: Set the turning power with a rudder
- Default Value: 2

### slowAndReverseWithoutControls 
- Description: Vehicles do not require controls while in slow and reverse with a person on them
- Default Value: false

### enableLandVehicles 
- Description: Vehicles can now float on land. What is realism. Experimental only until wheels are invented. Must use rudder speeds to move forwards.
- Default Value: false

### enableBaseGameSailRotation 
- Description: Lets the baseGame sails Tiers1-4 to rotate based on wind direction
- Default Value: true

### shouldLiftAnchorOnSpeedChange 
- Description: Lifts the anchor when using a speed change key, this is a QOL to prevent anchor from being required to be pressed when attempting to change the ship speed
- Default Value: false

### FlightClimbingOffset 
- Description: Ascent and Descent speed for the vehicle in the air. This value is interpolated to prevent jitters.
- Default Value: 1

### UnderwaterClimbingOffset 
- Description: Ascent and Descent speed for the vehicle in the water. This value is interpolated to prevent jitters.
- Default Value: 1

## ModSupport:DynamicLocations

### DynamicLocationLoginMovesPlayerToBed 
- Description: login/logoff point moves player to last interacted bed or first bed on ship
- Default Value: true

## CustomMesh

### Water Mask Prefabs Enabled 
- Description: Allows placing a dynamically sized cube that removes all water meshes intersecting with it. This also removes all water meshes when looking through it. So use it wisely, it's not perfect
- Default Value: true

### Enable Testing 4x4 Water Mask Prefabs, these are meant for demoing water obstruction. 
- Description: login/logoff point moves player to last interacted bed or first bed on ship
- Default Value: false

## Underwater: Debug

### DEBUG_ManualBallastOffsetEnabled 
- Description: Enable manual ballast testing.
- Default Value: false

### DEBUG_ManualBallastOffset 
- Description: Lets you test how ballast works by setting a value.
- Default Value: 0

### DEBUG_WaterDisplacementMeshPrimitive 
- Description: Lets you choose from the water displacement mesh primitives. These will be stored as ZDOs. Not super user friendly yet...
- Default Value: Cube

### DEBUG_HasDepthOverrides 
- Description: Enables depth overrides
- Default Value: false

### DEBUG_WaterDepthOverride 
- Description: Force Overrides the WATER depth for character on boats. Useful for testing how a player can swim to the lowest depth (liquid depth).
- Default Value: 30

### DEBUG_LiquidCacheDepthOverride 
- Description: Force Overrides the liquid CACHED depth for character on boats.
- Default Value: 0

### DEBUG_LiquidDepthOverride 
- Description: Force Overrides the LIQUID depth for character on boats.
- Default Value: 15

### DEBUG_SwimDepthOverride 
- Description: Force Overrides the Swim depth for character on boats. Values above the swim depth force the player into a swim animation.
- Default Value: 15

## Underwater

### DEBUG_WaveSizeMultiplier 
- Description: Make the big waves applies to DEBUG builds only. This is a direct multiplier to height might not work as expected. Debug value for fun
- Default Value: 1

### WaterBallastEnabled 
- Description: Similar to flight mechanics but at sea. Defaults with Space/Jump to increase height and Sneak/Shift to decrease height uses the same flight comamnds.
- Default Value: true

### AboveSurfaceBallastMaxShipSizeAboveWater 
- Description: Ships a fixed value to set for all ships. Will not be applied if config <EXPERIMENTAL_AboveSurfaceBallastUsesShipMass> is enabled and the ship weight is more than this value.
- Default Value: 0.5164319

### EXPERIMENTAL_AboveSurfaceBallastUsesShipMass 
- Description: Ships with high mass to volume will not be able to ballast well above the surface. This adds a ship mass to onboard volume calculation. The calculation is experimental so it might be inaccurate.
- Default Value: false

### BlockingColliderOffset 
- Description: Sets the relative offset from the float collider. Can be negative or positive. Recommended is near the float collider. Slightly above it.
- Default Value: 0.2

### BallastSpeed 
- Description: Adds more balast offset
- Default Value: 1

### AllowTamedEntiesUnderwater 
- Description: Lets tamed animals underwater too. Could break or kill them depending on config.
- Default Value: false

### FlipWatermeshMode 
- Description: Flips the water mesh underwater. This can cause some jitters. Turn it on at your own risk. It's improve immersion. Recommended to keep off if you dislike seeing a bit of tearing in the water meshes. Flipping camera above to below surface should fix things.
- Default Value: Everywhere

### UnderwaterShipCameraZoom 
- Description: Zoom value to allow for underwater zooming. Will allow camera to go underwater at values above 0. 0 will reset camera back to default.
- Default Value: 5000

### AllowMonsterEntitesUnderwater 
- Description: Allows Monster entities onto the ship and underwater. This means they can go underwater similar to player.
- Default Value: true

### AllowedEntiesList 
- Description: List separated by comma for entities that are allowed on the ship
- Default Value: 

### Use Underwater Fog 
- Description: Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.
- Default Value: true

### Underwater fog color 
- Description: Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.
- Default Value: 1A3B12FF

### Underwater Fog Intensity 
- Description: Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.
- Default Value: 0.03

### UnderwaterAccessMode 
- Description: Allows for walking underwater, anywhere, or onship, or eventually within the water displaced area only. Disabled with remove all water logic. DEBUG_WaterZoneOnly is not supported yet
- Default Value: OnboardOnly

## Vehicle Physics

### flightAngularDamping 
- Description: 
- Default Value: 1

### flightSidewaysDamping 
- Description: 
- Default Value: 1

### flightDamping 
- Description: 
- Default Value: 1

### flightSteerForce 
- Description: 
- Default Value: 1

### flightSailForceFactor 
- Description: 
- Default Value: 0.1

### flightDrag 
- Description: 
- Default Value: 1.2

### flightAngularDrag 
- Description: 
- Default Value: 1.2

### waterSteerForce 
- Description: 
- Default Value: 1

### waterSidewaysDamping 
- Description: 
- Default Value: 0.3

### waterAngularDamping 
- Description: 
- Default Value: 0.5

### waterDamping 
- Description: 
- Default Value: 0.1

### waterSailForceFactor 
- Description: 
- Default Value: 0.05

### waterDrag 
- Description: 
- Default Value: 0.8

### waterAngularDrag 
- Description: 
- Default Value: 0.8

### submersibleAngularDamping 
- Description: 
- Default Value: 0.5

### submersibleSidewaysDamping 
- Description: 
- Default Value: 0.5

### submersibleDamping 
- Description: 
- Default Value: 0.5

### submersibleSteerForce 
- Description: 
- Default Value: 0.5

### submersibleSailForceFactor 
- Description: 
- Default Value: 0

### submersibleDrag 
- Description: 
- Default Value: 1

### submersibleAngularDrag 
- Description: 
- Default Value: 2

## MinimapConfig

### BedPinSyncInterval 
- Description: The interval in seconds at which DynamicSpawn Player pins are synced to the client.
- Default Value: 3

### VehiclePinSyncInterval 
- Description: The interval in seconds at which vehicle pins are synced to the client.
- Default Value: 3

### VehicleNameTag 
- Description: Set the name of the vehicle icon.
- Default Value: Vehicle

### ShowAllVehiclesOnMap 
- Description: Shows all vehicles on global map. All vehicles will update their position.
- Default Value: false

### VisibleVehicleRadius 
- Description: A radius in which all vehicles are revealed. This is more balanced than ShowAllVehicles.
- Default Value: 50

### ShowBedsOnVehicles 
- Description: Will show your bed on you vehicle. This requires DynamicLocations to be enabled. This config may be migrated to dynamic locations.
- Default Value: true

## Quick Start (DEBUG-ONLY)

### QuickStartWorldName 
- Description: Set the quick start World Name
- Default Value: ZolVehicles1

### QuickStartWorldPassword 
- Description: Set the quick start world password
- Default Value: 123456

### QuickStartEnabled 
- Description: Enable Quick start
- Default Value: true

### QuickStartWorldPlayerName 
- Description: Quick start player name. Must be valid to start the quick start
- Default Value: Zol
