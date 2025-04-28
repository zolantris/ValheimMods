
## Debug

### Enable Sentry Metrics (requires sentryUnityPlugin) 
- Description: Enable sentry debug logging. Requires sentry logging plugin installed to work. Sentry Logging plugin will make it easier to troubleshoot raft errors and detect performance bottlenecks. The bare minimum is collected, and only data related to ValheimRaft. See https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT#logging-metrics for more details about what is collected
- Default Value: True

## Patches

### DynamicLocations 
- Description: Enables DynamicLocations mod to access ValheimRAFT/Vehicles identifiers.
- Default Value: True

### ComfyGizmo - Enable Patch 
- Description: Patches relative rotation allowing for copying rotation and building while the raft is at movement, this toggle is only provided in case patches regress anything in Gizmos and players need a work around.
- Default Value: False

### ComfyGizmo - Vehicle Creative zero Y rotation 
- Description: Vehicle/Raft Creative mode will set all axises to 0 for rotation instead keeping the turn axis. Gizmo has issues with rotated vehicles, so zeroing things out is much safer. Works regardless of patch if mod exists
- Default Value: True

### Vehicles Prevent Pausing 
- Description: Prevents pausing on a boat, pausing causes a TON of desync problems and can make your boat crash or other players crash
- Default Value: True

### Vehicles Prevent Pausing SinglePlayer 
- Description: Prevents pausing on a boat during singleplayer. Must have the Vehicle Prevent Pausing patch as well
- Default Value: True

### Disable Planbuild auto-patches 
- Description: Disable planbuild patches. This will prevent planbuild from working well. Only use this if valheim raft is causing planbuild to crash.
- Default Value: False

### Ram MineRock patches 
- Description: Enable MineRock5 patches to so vehicle and rams prefabs do not trigger errors when hitting areas over the default radius size
- Default Value: True

## Ram: Prefabs

### ramDamageEnabled 
- Description: Will keep the prefab available for aethetics only, will not do any damage nor will it initialize anything related to damage. Alternatives are using the damage tweaks.
- Default Value: True

## Ram: Vehicles

### CanHitWhileHauling 
- Description: Allows the vehicle to continue hitting objects while it's being hauled/moved.
- Default Value: True

## Ram: Prefabs

### maximumDamage 
- Description: Maximum damage for all damages combined. This will throttle any calcs based on each damage value. The throttling is balanced and will fit the ratio of every damage value set. This allows for velocity to increase ram damage but still prevent total damage over specific values
- Default Value: 200

## Ram: Vehicles

### maximumDamage 
- Description: Maximum damage for all damages combined. This will throttle any calcs based on each damage value. The throttling is balanced and will fit the ratio of every damage value set. This allows for velocity to increase ram damage but still prevent total damage over specific values
- Default Value: 200

## Ram: Prefabs

### maxDamageCap 
- Description: enable damage caps
- Default Value: True

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
- Default Value: 20

### pierceDamage 
- Description: Pierce damage for Ram Stakes. the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass. Will damage rocks as well as other entities
- Default Value: 20

## Ram: Vehicles

### slashDamage 
- Description: slashDamage for Ram Blades. the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.
- Default Value: 0

### bluntDamage 
- Description: bluntDamage the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.
- Default Value: 0

### chopDamage 
- Description: chopDamage for Ram Blades excludes Ram Stakes. the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass.. Will damage trees depending on tool tier settings
- Default Value: 100

### pickaxeDamage 
- Description: pickDamage the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass. Will damage rocks as well as other entities
- Default Value: 100

### pierceDamage 
- Description: Pierce damage for Ram Stakes. the base applied per hit on all items within the hit area. This damage is affected by velocity and ship mass. Will damage rocks as well as other entities
- Default Value: 0

## Ram: Prefabs

### percentageDamageToSelf 
- Description: Percentage Damage applied to the Ram piece per hit. Number between 0-1. This will damage the vehicle in the area hit.
- Default Value: 0.01

## Ram: Vehicles

### percentageDamageToSelf 
- Description: Percentage Damage applied to the Vehicle pieces per hit. Number between 0-1. This will damage the vehicle in the area hit.
- Default Value: 0.01

## Ram: Prefabs

### AllowContinuousDamage 
- Description: Rams will continue to apply damage based on their velocity even after the initial impact
- Default Value: True

### RamDamageToolTier 
- Description: allows rams to damage both rocks, ores, and higher tier trees and/or prefabs. Tier 1 is bronze. Setting to 100 will allow damage to all types of materials
- Default Value: 100

### CanHitCharacters 
- Description: allows rams to hit characters/entities
- Default Value: True

### CanHitEnemies 
- Description: allows rams to hit enemies
- Default Value: True

### CanHitFriendly 
- Description: allows rams to hit friendlies
- Default Value: True

### CanDamageSelf 
- Description: allows rams to be damaged. The values set for the damage will be calculated
- Default Value: True

### CanHitEnvironmentOrTerrain 
- Description: allows rams to hit friendlies
- Default Value: True

### HitRadius 
- Description: The base ram hit radius area. Stakes are always half the size, this will hit all pieces within this radius, capped between 5 and 10, but 50 is max. Stakes are half this value. Blades are equivalent to this value.
- Default Value: 5

## Ram: Vehicles

### CanHitCharacters 
- Description: allows vehicle rams to hit characters/entities
- Default Value: True

### CanHitEnemies 
- Description: allows vehicle rams to hit enemies
- Default Value: True

### CanHitFriendly 
- Description: allows vehicle rams to hit friendlies
- Default Value: True

### CanDamageSelf 
- Description: allows vehicle rams to be damaged. The values set for the damage will be calculated at same time hits are calculated. This config does not work yet so it's set to false currently on all releases
- Default Value: False

### CanHitEnvironmentOrTerrain 
- Description: allows vehicle rams to hit friendlies
- Default Value: True

### HitRadius 
- Description: The base hit radius of vehicle bodies. This will also effect self-damage to vehicle based on the radius.
- Default Value: 5

## Ram: Prefabs

### RamHitInterval 
- Description: Every X seconds, the ram will apply this damage
- Default Value: 1

### RamsCanBeRepaired 
- Description: Allows rams to be repaired
- Default Value: False

### minimumVelocityToTriggerHit 
- Description: Minimum velocity required to activate the ram's damage
- Default Value: 1

### MaxVelocityMultiplier 
- Description: Damage of the ram is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.
- Default Value: 1

## Ram: Vehicles

### VehicleMinimumVelocityToTriggerHit 
- Description: Minimum velocity required to activate the vehicle hull's damage
- Default Value: 1

### MaxVelocityMultiplier 
- Description: Damage of the vehicle hull is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.
- Default Value: 1

### VehicleHullMassMultiplierDamage 
- Description: Multiplier per each single point of mass the vehicle which adds additional damage. This value is multiplied by the velocity.
- Default Value: 0.1

### WaterVehicleRamToolTier 
- Description: The tier damage a water vehicle can do to a rock or other object it hits. To be balanced this should be a lower value IE (1) bronze. But ashlands will require a higher tier to smash spires FYI.
- Default Value: 100

### LandVehicleRamToolTier 
- Description: The tier damage a Land vehicle can do to a rock or other object it hits. This should be set to maximum as land vehicles are black metal tier.
- Default Value: 100

### WaterVehiclesAreRams 
- Description: Adds ram damage to a water vehicle's combined hull mesh. This affects all water vehicles vehicles. This will turn off all rams for water vehicles if set to false.
- Default Value: True

### LandVehiclesAreRams 
- Description: Adds ram damage to a land vehicle's combined hull mesh. This affects all land vehicles vehicles. This will turn off all rams for land vehicles if set to false.
- Default Value: True

## Ram: Prefabs

### DamageIncreasePercentagePerTier 
- Description: Damage Multiplier per tier. So far only HardWood (Tier1) Iron (Tier3) available. With base value 1 a Tier 3 mult at 25% additive additional damage would be 1.5. IE (1 * 0.25 * 2 + 1) = 1.5
- Default Value: 0.25

## PrefabConfig

### RopeLadderEjectionPoint 
- Description: The place the player is placed after they leave the ladder. Defaults to Y +0.25 and Z +0.5 meaning you are placed forwards of the ladder.
- Default Value: (0.00, 0.00, 0.00)

### PrefabConfig 
- Description: Allows you to customize what piece the raft initializes with. Admins only as this can be overpowered.
- Default Value: Hull4X8

### ropeLadderRunClimbSpeedMult 
- Description: Allows you to customize how fast you can climb a ladder when in run mode
- Default Value: 2

### ropeLadderHints 
- Description: Shows the controls required to auto ascend/descend and run to speedup ladder
- Default Value: True

### Protect Vehicle pieces from breaking on Error 
- Description: Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer
- Default Value: True

### GlassDefaultColor 
- Description: Set the experimental glass color for your vehicle. This will be used for most glass meshes. This is the default color. Eventually players can customize the color of the glass.
- Default Value: RGBA(0.600, 0.600, 0.600, 0.050)

### enableLandVehicles 
- Description: Vehicles land vehicle prefab will be enabled. LandVehicles will be available for all version above V3.0.0
- Default Value: False

### VehicleStaminaHaulingCost 
- Description: The cost per 1 meter of hauling a vehicle. This cost is on incurred if the vehicle is being pulled towards the player. When stamina runs out, the player is damaged by this amount until they release the vehicle.
- Default Value: 5

### VehicleHaulingSnapsOnStaminaZero 
- Description: Instead of allowing the viking to use health. The vehicle hauling line will snap when you have zero stamina doing a single one-time damage.
- Default Value: False

### Experimental_TreadScaleX 
- Description: Set the tank per tread piece X scale (width). This will make the treads larger or smaller allowing more/less grip.
- Default Value: 1

## Server config

### AdminsCanOnlyBuildRaft 
- Description: ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart
- Default Value: False

## PrefabConfig

### AllowExperimentalPrefabs 
- Description: Allows >=v2.0.0 experimental prefabs such as Iron variants of slabs, hulls, and ribs. They do not look great so they are disabled by default
- Default Value: False

## Graphics

### Sails Fade In Fog 
- Description: Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled
- Default Value: True

## Server config

### MakeAllPiecesWaterProof 
- Description: Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.
- Default Value: True

## Vehicle Debugging

### AllowDebugCommandsForNonAdmins 
- Description: Will will allow all debug commands for non-admins. Turning this to false will only allow debug (cheat) commands if the user is an admin.
- Default Value: True

## Config

### VehicleCreativeHeight 
- Description: Sets the vehicle creative command height, this value is relative to the current height of the ship, negative numbers will sink your ship temporarily
- Default Value: 0

## Vehicle Debugging

### DebugMetricsEnabled 
- Description: Will locally log metrics for ValheimVehicles mods. Meant for debugging functional delays, convexHull logic, and other long running processes. This can be log heavy but important to enable if the mod is having problems in order to report issues.
- Default Value: False

### DebugMetricsTimer 
- Description: The interval in seconds that the logs output. Lower is performance heavy. Do not have this set to a low value. Requires EnableDebugMetrics to be enabled to update.
- Default Value: 1

### HasAutoAnchorDelay 
- Description: For realism, the ship continues even when nobody is onboard. This is meant for debugging logout points but also could be useful for realism
- Default Value: False

### AutoAnchorDelayTimeInSeconds 
- Description: For realism, the ship continues for X amount of time until it either unrenders or a player stops it.
- Default Value: 10

### Always Show Vehicle Colliders 
- Description: Automatically shows the vehicle colliders useful for debugging the vehicle
- Default Value: False

### Vehicle Debug Menu 
- Description: Enable the VehicleDebugMenu. This shows a GUI menu which has a few shortcuts to debugging/controlling vehicles.
- Default Value: False

### SyncShipPhysicsOnAllClients 
- Description: Makes all clients sync physics. This will likely cause a desync in physics but could fix some problems with physics not updating in time for some clients as all clients would control physics.
- Default Value: False

## Vehicle Pieces

### VehicleBoundsRebuildDelayPerPiece 
- Description: The delay time that is added per piece the vehicle has on it for recalculating vehicle bounds. Example 2000 * 0.02 = 40seconds delay.  Values are clamped at 0.1 and max value: 60 so even smaller vehicles rebuild at the min value and large >2k piece vehicles build at the max value.
- Default Value: 0.02

### DisableVehicleCube 
- Description: The raft will no longer be a cube. It will place pieces in world position. This will allow for teleporting and other rapid location / login fixes to work better. It might cause large vehicles to clip/break if they are rendered out of a zone.
- Default Value: False

## Debug

### HasDebugSails 
- Description: Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only.
- Default Value: False

### HasDebugPieces 
- Description: Outputs more debug information for the vehicle pieces controller which manages all pieces placement. Meant for debugging mod issues. Will cause performance issues and lots of logging when enabled.
- Default Value: False

## Vehicle Debugging

### CommandsWindowPosX 
- Description: 
- Default Value: 0

### CommandsWindowPosY 
- Description: 
- Default Value: 0

### ConfigWindowPosX 
- Description: 
- Default Value: 0

### ConfigWindowPosY 
- Description: 
- Default Value: 0

### ButtonFontSize 
- Description: 
- Default Value: 18

### LabelFontSize 
- Description: 
- Default Value: 22

## Propulsion

### Rudder Back Speed 
- Description: Set the Back speed of rudder, this will apply with sails
- Default Value: 1

### Rudder Slow Speed 
- Description: Set the Slow speed of rudder, this will apply with sails
- Default Value: 1

### Rudder Half Speed 
- Description: Set the Half speed of rudder, this will apply with sails
- Default Value: 0

### Rudder Full Speed 
- Description: Set the Full speed of rudder, this will apply with sails
- Default Value: 0

### LandVehicle Back Speed 
- Description: Set the Back speed of land vehicle.
- Default Value: 1

### LandVehicle Slow Speed 
- Description: Set the Slow speed of land vehicle.
- Default Value: 1

### LandVehicle Half Speed 
- Description: Set the Half speed of land vehicle.
- Default Value: 1

### LandVehicle Full Speed 
- Description: Set the Full speed of land vehicle.
- Default Value: 1

### LandVehicle Turn Speed 
- Description: Turn speed for landvehicles. Zero is half the normal speed, 50% is normal speed, and 100% is double normal speed.
- Default Value: 0.5

### AllowFlight 
- Description: Allow the raft to fly (jump\crouch to go up and down)
- Default Value: False

### MassPercentage 
- Description: Sets the mass percentage of the ship that will slow down the sails
- Default Value: 0.5

### VehiclePhysicsMode 
- Description: ForceSyncedRigidbody ignores all allowances that toggle SyncRigidbody related to flight. This will require a flight ascend value of 1 otherwise flight will be broken. Use this is there is problems with SyncRigidbody
Other methods removed after 2.5.0
- Default Value: ForceSyncedRigidbody

### EXPERIMENTAL_LeanTowardsWindSailDirection 
- Description: Toggles a lean while sailing with wind power. Cosmetic only and does not work in multiplayer yet. Warning for those with motion sickness, this will increase your symptoms. Prepare your dramamine!
- Default Value: False

### EXPERIMENTAL_LeanTowardsWindSailDirectionMaxAngle 
- Description: Set the max lean angle when wind is hitting sides directly
- Default Value: 10

### turningPowerNoRudder 
- Description: Set the base turning power of the steering wheel without a rudder
- Default Value: 0.7

### turningPowerWithRudder 
- Description: Set the turning power with a rudder prefab attached to the boat. This value overrides the turningPowerNoRudder config.
- Default Value: 1

### slowAndReverseWithoutControls 
- Description: Vehicles do not require controls while in slow and reverse with a person on them
- Default Value: False

### enableBaseGameSailRotation 
- Description: Lets the baseGame sails Tiers1-4 to rotate based on wind direction
- Default Value: True

### shouldLiftAnchorOnSpeedChange 
- Description: Lifts the anchor when using a speed change key, this is a QOL to prevent anchor from being required to be pressed when attempting to change the ship speed
- Default Value: False

### FlightClimbingOffset 
- Description: Ascent and Descent speed for the vehicle in the air. This value is interpolated to prevent jitters.
- Default Value: 2

### BallastClimbingOffset 
- Description: Ascent and Descent speed for the vehicle in the water. This value is interpolated to prevent jitters.
- Default Value: 2

### VerticalSmoothingSpeed 
- Description: This applies to both Ballast and Flight modes. The vehicle will use this value to interpolate the climbing offset. Meaning low value will be slower climbing/ballast and high values will be instant and match the offset. High values will result in jitters and potentially could throw people off the vehicle. Expect values of 0.01 and 1. IE 1% and 100%
- Default Value: 0.5

## Debug

### ShowShipState 
- Description: 
- Default Value: True

## Propulsion

### MaxSailSpeed 
- Description: Sets the absolute max speed a ship can ever hit with sails. Prevents or enables space launches, cannot exceed MaxPropulsionSpeed.
- Default Value: 30

### SpeedCapMultiplier 
- Description: Sets the speed at which it becomes significantly harder to gain speed per sail area
- Default Value: 1

### EnableCustomPropulsionConfig 
- Description: Enables all custom propulsion values
- Default Value: False

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
- Default Value: True

### FlightHasRudderOnly 
- Description: Flight allows for different rudder speeds. Use rudder speed only. Do not use sail speed.
- Default Value: False

### AllowCustomRudderSpeeds 
- Description: Allow the raft to use custom rudder speeds set by the player, these speeds are applied alongside sails at half and full speed. See advanced section for the actual speed settings.
- Default Value: True

### WheelDeadZone 
- Description: Plus or minus deadzone of the wheel when turning. Setting this to 0 will disable this feature. This will zero out the rudder if the user attempts to navigate with a value lower than this threshold range
- Default Value: 0.02

## ModSupport:Assets

### pluginFolderName 
- Description: Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their manager renames the folder, r2modman has a fallback case added to search for the mod folder.Default search values are an ordered list first one is always matching non-empty strings from this pluginFolderName.Folder Matches are: zolantris-ValheimRAFT, Zolantris-ValheimRAFT, and ValheimRAFT
- Default Value: 

## ModSupport:DynamicLocations

### DynamicLocationLoginMovesPlayerToBed 
- Description: login/logoff point moves player to last interacted bed or first bed on ship
- Default Value: True

## ModSupport:DebugOptimizations

### RemoveStartMenuBackground 
- Description: Removes the start scene background, only use this if you want to speedup start time and lower GPU power cost significantly if you are idle on the start menu.
- Default Value: False

## CustomMesh

### Water Mask Prefabs Enabled 
- Description: Allows placing a dynamically sized cube that removes all water meshes intersecting with it. This also removes all water meshes when looking through it. So use it wisely, it's not perfect
- Default Value: True

### Enable Testing 4x4 Water Mask Prefabs, these are meant for demoing water obstruction. 
- Description: login/logoff point moves player to last interacted bed or first bed on ship
- Default Value: False

## Underwater: Debug

### DEBUG_WaterDisplacementMeshPrimitive 
- Description: Lets you choose from the water displacement mesh primitives. These will be stored as ZDOs. Not super user friendly yet...
- Default Value: Cube

### DEBUG_HasDepthOverrides 
- Description: Enables depth overrides
- Default Value: False

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
- Default Value: False

### AboveSurfaceBallastMaxShipSizeAboveWater 
- Description: A fixed value to set for all vehicles. Will not be applied if config <EXPERIMENTAL_AboveSurfaceBallastUsesShipMass> is enabled and the ship weight is more than this value. Set it to 100% to always allow the full height of the ship above the surface.
- Default Value: 0.5

### EXPERIMENTAL_AboveSurfaceBallastUsesShipMass 
- Description: Ships with high mass to volume will not be able to ballast well above the surface. This adds a ship mass to onboard volume calculation. The calculation is experimental so it might be inaccurate. For now mass to volume includes the length width heigth in a box around the ship. It's unrealistic as of 2.4.0 as this includes emptyspace above water. Eventually this will be calculated via displacement (empty volume with wall all around it) calculation.
- Default Value: False

### AllowMonsterCharactersUnderwater 
- Description: Allows Monster characters (untamed, and enemies too) onto the ship and underwater. This means they can go underwater similar to player.
- Default Value: True

### AllowedCharacterList 
- Description: List separated by comma for entities that are allowed on the ship. For simplicity consider enabling monsters and tame creatures.
- Default Value: 

### AllowTamedCharactersUnderwater 
- Description: Lets tamed animals underwater too. Could break or kill them depending on config.
- Default Value: True

### FlipWatermeshMode 
- Description: Flips the water mesh underwater. This can cause some jitters. Turn it on at your own risk. It's improve immersion. Recommended to keep off if you dislike seeing a bit of tearing in the water meshes. Flipping camera above to below surface should fix things.
- Default Value: Disabled

### UnderwaterFogEnabled 
- Description: Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.
- Default Value: True

### UnderwaterFogColor 
- Description: Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.
- Default Value: RGBA(0.100, 0.230, 0.070, 1.000)

### UnderwaterFogIntensity 
- Description: Adds fog to make underwater appear more realistic. This should be disabled if using Vikings do swim as this mod section is not compatible yet.
- Default Value: 0.03

### UnderwaterAccessMode 
- Description: Allows for walking underwater, anywhere, or onship, or eventually within the water displaced area only. Disabled with remove all water logic. DEBUG_WaterZoneOnly is not supported yet
- Default Value: Disabled

### HasUnderwaterHullBubbleEffect 
- Description: Adds an underwater bubble conforming around the vehicle hull. Allowing for a underwater like distortion effect without needing to use fog.
- Default Value: True

### UnderwaterBubbleEffectColor 
- Description: Set the underwater bubble color
- Default Value: RGBA(0.000, 0.400, 0.400, 0.800)

## Vehicle Physics

### LandVehicle Max Tread Width 
- Description: Max width the treads can expand to. Lower values will let you make motor bikes. This affects all vehicles. In future version there will be an individual config setting.
- Default Value: 8

### LandVehicle Max Tread Length 
- Description: Max length the treads can expand to. In future version there will be an individual config setting.
- Default Value: 20

### Vehicle CenterOfMassOffset 
- Description: Offset the center of mass by a percentage of vehicle total height. Should always be a positive number. Higher values will make the vehicle more sturdy as it will pivot lower. Too high a value will make the ship behave weirdly possibly flipping. 0 will be the center of all colliders within the physics of the vehicle. 
100% will be 50% lower than the vehicle's collider. 
50% will be the very bottom of the vehicle's collider. In future version there will be an individual config setting.
- Default Value: 0.65

### flightDamping_3.2.0 
- Description: Controls how much the water pushes the boat upwards directly. This value may affect angular damping too. Recommended to keep the original value. But tweaking can remove or add additional jitter. Higher values likely will add more jitter.
- Default Value: 1

### flightSidewaysDamping_3.2.0 
- Description: Controls how much the water pushes the boat sideways based on wind direction and velocity.
- Default Value: 2

### flightAngularDamping_3.2.0 
- Description: Controls how much the water pushes the boat from a vertical angle based on water and velocity. Lower values will cause more rocking and allow better turn rates. Higher values will make the vehicle more stable, but less turning angle and possibly less realistic. If you get motion-sickness this can allow tweaking sway without disabling it all and also prevent rapid turning.
- Default Value: 1

### flightSteerForce 
- Description: DEBUG, tweak sailing math. Not supported or tested. Do not mess with defaults. Do not use this unless you know what your doing.
- Default Value: 1

### UNSTABLE_flightSailForceFactor 
- Description: DEBUG, tweak sailing math. Not supported or tested. Do not mess with defaults. Do not use this unless you know what your doing.
- Default Value: 0.075

### flightDrag 
- Description: 
- Default Value: 1.2

### flightAngularDrag 
- Description: 
- Default Value: 1.2

### force_3.2.0 
- Description: EXPERIMENTAL_FORCE. Lower values will not allow the vehicle to balance fast when tilted. Lower values can reduce bobbing, but must be below the forceDistance value.
- Default Value: 2

### forceDistance_3.2.0 
- Description: EXPERIMENTAL_FORCE_DISTANCE should always be above the value of force. Otherwise bobbing will occur. Lower values will not allow the vehicle to balance fast when tilted
- Default Value: 10

### backwardForce_3.2.0 
- Description: EXPERIMENTAL_BackwardFORCE
- Default Value: 1

### waterSteerForce 
- Description: 
- Default Value: 1

### waterDamping_3.2.0 
- Description: Controls how much the water pushes the boat upwards directly. This value may affect angular damping too. Recommended to keep the original value. But tweaking can remove or add additional jitter. Higher values likely will add more jitter.
- Default Value: 1

### waterSidewaysDamping_3.2.0 
- Description: Controls how much the water pushes the boat sideways based on wind direction and velocity.
- Default Value: 2

### waterAngularDamping_3.2.0 
- Description: Controls how much the water pushes the boat from a vertical angle based on water and velocity. Lower values will cause more rocking and allow better turn rates. Higher values will make the vehicle more stable, but less turning angle and possibly less realistic. If you get motion-sickness this can allow tweaking sway without disabling it all and also prevent rapid turning.
- Default Value: 1

### UNSTABLE_waterSailForceFactor 
- Description: DEBUG, tweak sailing math. Not supported or tested. Do not mess with defaults. Do not use this unless you know what your doing.
- Default Value: 0.05

### waterDrag 
- Description: 
- Default Value: 0.8

### waterAngularDrag 
- Description: 
- Default Value: 0.8

### submersibleDamping_3.2.0 
- Description: Controls how much the water pushes the boat upwards directly. This value may affect angular damping too. Recommended to keep the original value. But tweaking can remove or add additional jitter. Higher values likely will add more jitter.
- Default Value: 1

### submersibleSidewaysDamping_3.2.0 
- Description: Controls how much the water pushes the boat sideways based on wind direction and velocity.
- Default Value: 2

### submersibleAngularDamping_3.2.0 
- Description: Controls how much the water pushes the boat from a vertical angle based on water and velocity. Lower values will cause more rocking and allow better turn rates. Higher values will make the vehicle more stable, but less turning angle and possibly less realistic. If you get motion-sickness this can allow tweaking sway without disabling it all and also prevent rapid turning.
- Default Value: 1

### submersibleSteerForce 
- Description: 
- Default Value: 1

### UNSTABLE_submersibleSailForceFactor 
- Description: DEBUG, tweak sailing math. Not supported or tested. Do not mess with defaults. Do not use this unless you know what your doing.
- Default Value: 0.05

### submersibleDrag 
- Description: 
- Default Value: 1.5

### submersibleAngularDrag 
- Description: 
- Default Value: 1.5

### landDrag 
- Description: 
- Default Value: 0.05

### landAngularDrag 
- Description: 
- Default Value: 1.2

### LandVehicle TreadOffset 
- Description: Wheel offset. Allowing for raising the treads higher. May require increasing suspension distance so the treads spawn then push the vehicle upwards. Negative lowers the wheels. Positive raises the treads
- Default Value: -1

### MaxVehicleLinearVelocity_1.0.x 
- Description: Sets the absolute max speed a vehicle can ever move in. This is X Y Z directions. This will prevent the ship from rapidly flying away. Try staying between 5 and 100. Higher values will increase potential of vehicle flying off to space or rapidly accelerating through objects before physics can apply to an unloaded zone.
- Default Value: 100

### MaxVehicleLinearYVelocity_1.0.x 
- Description: Sets the absolute max speed a vehicle can ever move in vertical direction. This will limit the ship capability to launch into space. Lower values are safer. Too low and the vehicle will not use gravity well
- Default Value: 50

### MaxVehicleAngularVelocity 
- Description: Sets the absolute max speed a vehicle can ROTATE in. Having a high value means the vehicle can spin out of control.
- Default Value: 5

## Vehicle Physics: Floatation

### HullFloatationColliderLocation 
- Description: Hull Floatation Collider will determine the location the ship floats and hovers above the sea. Average is the average height of all Vehicle Hull Pieces attached to the vehicle. The point calculate is the center of the prefab. Center is the center point of all the float boats. This center point is determined by the max and min height points included for ship hulls. Lowest is the lowest most hull piece will determine the float height, allowing users to easily raise the ship if needed by adding a piece at the lowest point of the ship. Custom allows for setting floatation between -20 and 20
- Default Value: Fixed

### EnableExactVehicleBounds_3.2.0 
- Description: Ensures that a piece placed within the raft is included in the float collider correctly. May not be accurate if the parent GameObjects are changing their scales above or below 1,1,1. Mods like Gizmo could be incompatible. This is enabled by default but may change per update if things are determined to be less stable. Changes Per mod version
- Default Value: True

### vehiclePiecesShipCollisionDetectionMode 
- Description: Set the collision mode of the vehicle ship pieces container. This the container that people walk on and use the boat. Collision Continuous will prevent people from passing through the boat. Other modes might improve performance like Discrete but cost in more jitter or lag.
- Default Value: Continuous

### convexHullJoinDistanceThreshold 
- Description: The threshold at which a vehicle's colliders are joined with another pieces colliders to make a singular hull. Higher numbers will join multiple pieces together into a singular hull. Lower numbers allow for splitting hulls out at the cost of performance.
- Default Value: 3

### convexHullDebuggerColor 
- Description: Allows the user to set the debugger hull color.
- Default Value: RGBA(0.100, 0.230, 0.070, 0.500)

### convexHullDebuggerForceEnabled 
- Description: Force enables the convex hull. This will be turned off if other commands are run or re-enabled if toggled.
- Default Value: False

### convexHullPreviewOffset 
- Description: Sets the hull preview offset, this will allow previewing the hull side by side with your vehicle. This can only be seen if the Vehicle Physics: Floatation.convexHullDebuggerForceEnabled is true.
- Default Value: (0.00, 0.00, 0.00)

## Vehicle Physics: Velocity Mode

### floatationVelocityMode 
- Description: EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for
- Default Value: VelocityChange

### flyingVelocityMode 
- Description: EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for
- Default Value: VelocityChange

### turningVelocityMode 
- Description: EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for
- Default Value: VelocityChange

### sailingVelocityMode 
- Description: EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for
- Default Value: VelocityChange

### rudderVelocityMode 
- Description: EXPERIMENTAL VelocityMode changeable in debug only. Override so mass and vehicle size are accounted for
- Default Value: VelocityChange

## Vehicle Physics

### EXPERIMENTAL removeCameraCollisionWithObjectsOnBoat 
- Description: EXPERIMENTAL removes all collision of camera for objects on boat. Should significantly lower jitter when camera smashes into objects on boat it will force camera through it instead of pushing rapidly forward with vehicle force too. This will cause objects to pop in and out of view.
- Default Value: False

### waterDeltaForceMultiplier 
- Description: Water delta force multiplier
- Default Value: 50

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
- Default Value: False

### VisibleVehicleRadius 
- Description: A radius in which all vehicles are revealed. This is more balanced than ShowAllVehicles.
- Default Value: 50

### ShowBedsOnVehicles 
- Description: Will show your bed on you vehicle. This requires DynamicLocations to be enabled. This config may be migrated to dynamic locations.
- Default Value: True

## Hud

### HudAnchorTextAboveAnchors 
- Description: Shows the anchored status above vehicle anchors prefab. This text will update based on state change
- Default Value: True

### HideAnchorStateMessageTimer 
- Description: Hides the BepInEx.Configuration.ConfigDescription after X seconds a specific amount of time has passed. Setting this to 0 will mean it never hides
- Default Value: 3

### HudAnchorTextSize 
- Description: Sets the anchor text size. Potentially Useful for those with different monitor sizes
- Default Value: 4

## Camera Optimizations

### CameraOcclusionInterval 
- Description: Interval in seconds at which the camera will hide meshes in attempt to consolidate FPS / GPU memory.
- Default Value: 0.1

### UNSTABLE_CameraOcclusionEnabled 
- Description: Unstable config, this will possible get you more performance but parts of the vehicle will be hidden when rapidly panning. This Enables hiding active raft pieces at specific intervals. This will hide only the rendered texture.
- Default Value: False

### UNSTABLE_DistanceToKeepObjects 
- Description: Threshold at which to retain a object even if it's through a wall.
- Default Value: 5

## Camera Zoom

### VehicleCameraZoom_Enabled 
- Description: Overrides the camera zoom while on the vehicle. Values are configured through other keys.
- Default Value: False

### VehicleCameraZoomMaxDistance 
- Description: Allows the camera to zoom out between 8 and 64 meters. Percentage based zoom.
- Default Value: 0.5

## Hud

### EnableVehicleClusterRendering 
- Description: Cluster rendering efficiently improves how the raft renders. It will offer 50% boost in FPS for larger ships. You can reach upwards of 90 FPS on a 3000 piece ship vs 40-45fps. It does this by combining meshes so editing and damaging these components is a bit more abrupt. WearNTear animations go away, but the items can still be broken. Updates require re-building the meshes affected so this can be a bit heavy, but not as heavy as bounds collider rebuild.
- Default Value: False

### ClusterRenderingPieceThreshold 
- Description: Set the number of pieces to render threshold for using cluster rendering. smaller ships will not have cluster rendering apply. Lowest number of items possible is 10 as it's less efficient to run this on smaller vehicles. Recommended range is above 100 and max is 1000 which will significant improve the ship. If you do not want it enable turn off the feature via the key: <EnableVehicleClusterRendering>.
- Default Value: 500

### EnableWorldClusterMeshRendering 
- Description: Cluster rendering efficiently improves how the whole world renders and shares meshes. It will allow for significantly higher FPS at the potential cost of wearNTear latency. It is debug only provided and will not be enabled until wearNtear can be optimize with this.
- Default Value: False

## VehicleGlobal:Sound

### Ship Sailing Sounds 
- Description: Toggles the ship sail sounds.
- Default Value: True

### Ship Wake Sounds 
- Description: Toggles Ship Wake sounds. Can be pretty loud
- Default Value: True

### Ship In-Water Sounds 
- Description: Toggles ShipInWater Sounds, the sound of the hull hitting water
- Default Value: True

## Rendering

### Force Ship Owner Piece Update Per Frame 
- Description: Forces an update during the Update sync of unity meaning it fires every frame for the Ship owner who also owns Physics. This will possibly make updates better for non-boat owners. Noting that the boat owner is determined by the first person on the boat, otherwise the game owns it.
- Default Value: False

## VehicleGlobal:Updates

### ServerRaftUpdateZoneInterval 
- Description: Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.
- Default Value: 5

## QuickStartWorld

### ServerOnlineBackendType 
- Description: 
- Default Value: Steamworks

### QuickStartWorldName 
- Description: Set the quick start World Name
- Default Value: 

### QuickStartWorldPassword 
- Description: Set the quick start world password
- Default Value: 

### IsOpenServer 
- Description: Set if hosted server is opened allowing other players to connect to the server.
- Default Value: False

### IsPublicServer 
- Description: Set the hosted server is public and listed.
- Default Value: False

### IsServer 
- Description: Set if server is public
- Default Value: False

### IsJoinServer 
- Description: Join a server instead of hosting a server automatically.
- Default Value: False

### JoinServerUrl 
- Description: Set the join server URL. This can be an IP address or a web url if hosted server can resolve from a url.
- Default Value: 

### JoinServerPort 
- Description: Set the join server URL. This can be an IP address or a web url if hosted server can resolve from a url.
- Default Value: 2456

### QuickStartEnabled 
- Description: Enable Quick start
- Default Value: False

### QuickStartWorldPlayerName 
- Description: Quick start player name. Must be valid to start the quick start
- Default Value: 
