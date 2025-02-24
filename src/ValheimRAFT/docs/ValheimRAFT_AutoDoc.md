
## Debug

### Enable Sentry Metrics (requires sentryUnityPlugin) 
- Description: Enable sentry debug logging. Requires sentry logging plugin installed to work. Sentry Logging plugin will make it easier to troubleshoot raft errors and detect performance bottlenecks. The bare minimum is collected, and only data related to ValheimRaft. See https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT#logging-metrics for more details about what is collected
- Default Value: True

### Debug logging for Vehicle/Raft 
- Description: Outputs more debug logs for the Vehicle components. Useful for troubleshooting errors, but will spam logs
- Default Value: False

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
- Default Value: False

## Server config

### Protect Vehicle pieces from breaking on Error 
- Description: Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer
- Default Value: True

### AdminsCanOnlyBuildRaft 
- Description: ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart
- Default Value: False

### AllowOldV1RaftRecipe 
- Description: Allows the V1 Raft to be built, this Raft is not performant, but remains in >=v2.0.0 as a Fallback in case there are problems with the new raft
- Default Value: False

### AllowExperimentalPrefabs 
- Description: Allows >=v2.0.0 experimental prefabs such as Iron variants of slabs, hulls, and ribs. They do not look great so they are disabled by default
- Default Value: False

## Rendering

### Force Ship Owner Piece Update Per Frame 
- Description: Forces an update during the Update sync of unity meaning it fires every frame for the Ship owner who also owns Physics. This will possibly make updates better for non-boat owners. Noting that the boat owner is determined by the first person on the boat, otherwise the game owns it.
- Default Value: False

## Server config

### ServerRaftUpdateZoneInterval 
- Description: Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.
- Default Value: 5

### MakeAllPiecesWaterProof 
- Description: Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.
- Default Value: True

### AllowFlight 
- Description: Allow the raft to fly (jump\crouch to go up and down)
- Default Value: False

### AllowCustomRudderSpeeds 
- Description: Allow the raft to use custom rudder speeds set by the player, these speeds are applied alongside sails at half and full speed. See advanced section for the actual speed settings.
- Default Value: True

## Config

### RaftCreativeHeight 
- Description: Sets the raftcreative command height, raftcreative is relative to the current height of the ship, negative numbers will sink your ship temporarily
- Default Value: 5

### AnchorKeyboardShortcut 
- Description: Anchor keyboard hotkey. Only applies to keyboard
- Default Value: LeftShift

## Debug

### ShowShipState 
- Description: 
- Default Value: True

## Propulsion

### MaxSailSpeed 
- Description: Sets the absolute max speed a ship can ever hit with sails. Prevents or enables space launches, cannot exceed MaxPropulsionSpeed.
- Default Value: 30

### MassPercentage 
- Description: Sets the mass percentage of the ship that will slow down the sails
- Default Value: 55

### SpeedCapMultiplier 
- Description: Sets the speed at which it becomes significantly harder to gain speed per sail area
- Default Value: 1

### HasShipWeightCalculations 
- Description: enables ship weight calculations for sail-force (sailing speed) and future propulsion, makes larger ships require more sails and smaller ships require less
- Default Value: True

### HasShipContainerWeightCalculations 
- Description: enables ship weight calculations for containers which affects sail-force (sailing speed) and future propulsion calculations. Makes ships with lots of containers require more sails
- Default Value: False

## Debug

### HasDebugSails 
- Description: Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only.
- Default Value: False

## Propulsion

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

## Graphics

### Sails Fade In Fog 
- Description: Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled
- Default Value: True

## Sounds

### Ship Sailing Sounds 
- Description: Toggles the ship sail sounds.
- Default Value: True

### Ship Wake Sounds 
- Description: Toggles Ship Wake sounds. Can be pretty loud
- Default Value: True

### Ship In-Water Sounds 
- Description: Toggles ShipInWater Sounds, the sound of the hull hitting water
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

### Enable PlanBuild Patches (required to be on if you installed PlanBuild) 
- Description: Fixes the PlanBuild mod position problems with ValheimRaft so it uses localPosition of items based on the parent raft. This MUST be enabled to support PlanBuild but can be disabled when the mod owner adds direct support for this part of ValheimRAFT. PlanBuild mod can be found here. https://thunderstore.io/c/valheim/p/MathiasDecrock/PlanBuild/
- Default Value: False

## Rams

### ramDamageEnabled 
- Description: Will keep the prefab available for aethetics only, will not do any damage nor will it initialize anything related to damage. Alternatives are using the damage tweaks.
- Default Value: True

### maximumDamage 
- Description: Maximum damage for all damages combined. This will throttle any calcs based on each damage value. The throttling is balanced and will fit the ratio of every damage value set. This allows for velocity to increase ram damage but still prevent total damage over specific values
- Default Value: 200

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

### percentageDamageToSelf 
- Description: Percentage Damage applied to the Ram piece per hit. Number between 0-1.
- Default Value: 0.01

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

### RamHitInterval 
- Description: Every X seconds, the ram will apply this damage
- Default Value: 1

### RamsCanBeRepaired 
- Description: Allows rams to be repaired
- Default Value: False

### minimumVelocityToTriggerHit 
- Description: Minimum velocity required to activate the ram's damage
- Default Value: 1

### RamMaxVelocityMultiplier 
- Description: Damage of the ram is increased by an additional % based on the additional weight of the ship. 1500 mass at 1% would be 5 extra damage. IE 1500-1000 = 500 * 0.01 = 5.
- Default Value: 1

### VehicleHullMassMultiplierDamage 
- Description: Multiplier per each single point of mass the vehicle has how much additional damage is done, multiplied by the velocity.
- Default Value: 0.1

### VehicleHullTier 
- Description: The tier damage a vehicle can do to a rock or other object it hits. To be balanced this should be a lower value IE (1) bronze. But ashlands will require a higher tier to smash spires FYI.
- Default Value: 100

### VehicleHullsAreRams 
- Description: Each vehicle has a Ram added it's mesh. Vehicle hull ram damage is calculated with different values.
- Default Value: True

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

### GlassDefaultColor 
- Description: Set the experimental glass color for your vehicle. This will be used for most glass meshes. This is the default color. Eventually players can customize the color of the glass.
- Default Value: RGBA(0.600, 0.600, 0.600, 0.050)

### enableLandVehicles 
- Description: Vehicles land vehicle prefab will be enabled. LandVehicles will be available for all version above V3.0.0
- Default Value: False

## Vehicle Debugging

### DebugMetricsEnabled 
- Description: Will locally log metrics for ValheimVehicles mods. Meant for debugging functional delays
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

## Debug

### SyncShipPhysicsOnAllClients 
- Description: Makes all clients sync physics
- Default Value: False

### VehiclePieceBoundsRecalculationDelay 
- Description: The delay time at which the vehicle will recalculate bounds after placing a piece. This recalculation can be a bit heavy so it's debounced a minimum of 1 seconds but could be increased up to 30 seconds for folks that want to build a pause for a bit.
- Default Value: 10

## Vehicle Debugging

### WindowPosX 
- Description: 
- Default Value: 0

### WindowPosY 
- Description: 
- Default Value: 0

### ButtonFontSize 
- Description: 
- Default Value: 16

### LabelFontSize 
- Description: 
- Default Value: 22

## Propulsion

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

### BallastClimbingOffset 
- Description: Ascent and Descent speed for the vehicle in the air. This value is interpolated to prevent jitters.
- Default Value: 2

### VerticalSmoothingSpeed 
- Description: This applies to both Ballast and Flight modes. The vehicle will use this value to interpolate the climbing offset. Meaning low value will be slower climbing/ballast and high values will be instant and match the offset. High values will result in jitters and potentially could throw people off the vehicle. Expect values of 0.01 and 1. IE 1% and 100%
- Default Value: 0.5

### WheelDeadZone 
- Description: Plus or minus deadzone of the wheel when turning. Setting this to 0 will disable this feature. This will zero out the rudder if the user attempts to navigate with a value lower than this threshold range
- Default Value: 0.02

## ModSupport:DynamicLocations

### DynamicLocationLoginMovesPlayerToBed 
- Description: login/logoff point moves player to last interacted bed or first bed on ship
- Default Value: True

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

### AllowMonsterEntitesUnderwater 
- Description: Allows Monster entities onto the ship and underwater. This means they can go underwater similar to player.
- Default Value: True

### AllowedEntiesList 
- Description: List separated by comma for entities that are allowed on the ship. For simplicity consider enabling monsters and tame creatures.
- Default Value: 

### AllowTamedEntiesUnderwater 
- Description: Lets tamed animals underwater too. Could break or kill them depending on config.
- Default Value: False

### FlipWatermeshMode 
- Description: Flips the water mesh underwater. This can cause some jitters. Turn it on at your own risk. It's improve immersion. Recommended to keep off if you dislike seeing a bit of tearing in the water meshes. Flipping camera above to below surface should fix things.
- Default Value: Disabled

### UnderwaterShipCameraZoom 
- Description: Zoom value to allow for underwater zooming. Will allow camera to go underwater at values above 0. 0 will reset camera back to default.
- Default Value: 5000

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

## Physics: Propulsion

### VehicleLand LockXZRotation 
- Description: Prevents XZ rotation on landvehicles DEBUG ONLY. Vehicles will always remain perfectly flat to horizon but this will limit upwards/downwards angular traversal
- Default Value: False

### VehicleLand WheelMass 
- Description: The weight per wheel of the vehicle. This will allow more traction, but could slow down the vehicle at higher values. Experimental only
- Default Value: 200

### LandVehicle WheelRadius 
- Description: Wheel radius. Larger wheels have more traction. But may be less realistic.
- Default Value: 1

### LandVehicle ShouldHideWheels 
- Description: Hides the wheel visual as wheels are not perfectly synced.
- Default Value: False

### LandVehicle ShouldSyncWheelPositions 
- Description: Toggles syncing of wheels to their actual collider position. Can cause desync with tracks.
- Default Value: False

### LandVehicle Suspension Distance 
- Description: Distance suspension distance between vehicle position and wheel position. Higher values push the vehicle up and make it more bouncy.
- Default Value: 2.25

### LandVehicle SuspensionSpring 
- Description: Suspension spring value. This will control how much the vehicle bounces when it drops. No suspension will be a bit jarring but high suspension can cause lots of screen jump. Ensure a higher SuspensionSpringDamper to fix the bounce continuing.
- Default Value: 35000

### LandVehicle SuspensionSpringDamper 
- Description: Suspension spring damper value. This will control how much the vehicle stops bouncing. Higher values must be supplied for higher suspension spring values.
- Default Value: 1500

### LandVehicle wheelSuspensionSpringTarget 
- Description: Suspension target. Between 0 and 1 it will determine the target spring position. This can allow for high suspension but also high targets
- Default Value: 0.4

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

### LandVehicle Max Tread Width 
- Description: Max width the treads can expand to.
- Default Value: 8

### LandVehicle Max Tread Length 
- Description: Max length the treads can expand to.
- Default Value: 20

### Vehicle CenterOfMassOffset 
- Description: Offset the center of mass by a percentage of vehicle total height. Should always be a positive number. Higher values will make the vehicle more sturdy as it will pivot lower. Too high a value will make the ship behave weirdly possibly flipping. 0 will be the center of all colliders within the physics of the vehicle. 100% will be 50% lower than the vehicle's collider. 50% will be the very bottom of the vehicle's collider.
- Default Value: 0.65

## Vehicle Physics

### flightDamping_3.0.1 
- Description: Controls how much the water pushes the boat upwards directly. This value may affect angular damping too. Recommended to keep the original value. But tweaking can remove or add additional jitter. Higher values likely will add more jitter.
- Default Value: 1

### flightSidewaysDamping_3.0.1 
- Description: Controls how much the water pushes the boat sideways based on wind direction and velocity.
- Default Value: 2

### flightAngularDamping_3.0.1 
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

### forceDistance_3.0.1 
- Description: EXPERIMENTAL_FORCE_DISTANCE
- Default Value: 1

### force_3.0.1 
- Description: EXPERIMENTAL_FORCE
- Default Value: 1

### backwardForce_3.0.1 
- Description: EXPERIMENTAL_BackwardFORCE
- Default Value: 1

### waterSteerForce 
- Description: 
- Default Value: 1

### waterDamping_3.0.1 
- Description: Controls how much the water pushes the boat upwards directly. This value may affect angular damping too. Recommended to keep the original value. But tweaking can remove or add additional jitter. Higher values likely will add more jitter.
- Default Value: 1

### waterSidewaysDamping_3.0.1 
- Description: Controls how much the water pushes the boat sideways based on wind direction and velocity.
- Default Value: 2

### waterAngularDamping_3.0.1 
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

### submersibleDamping_3.0.1 
- Description: Controls how much the water pushes the boat upwards directly. This value may affect angular damping too. Recommended to keep the original value. But tweaking can remove or add additional jitter. Higher values likely will add more jitter.
- Default Value: 1

### submersibleSidewaysDamping_3.0.1 
- Description: Controls how much the water pushes the boat sideways based on wind direction and velocity.
- Default Value: 2

### submersibleAngularDamping_3.0.1 
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

## Physics: Propulsion

### LandVehicle TreadOffset 
- Description: Wheel offset. Allowing for raising the treads higher. May require increasing suspension distance so the treads spawn then push the vehicle upwards. Negative lowers the wheels. Positive raises the treads
- Default Value: 0

## Vehicle Physics

### MaxVehicleLinearVelocity 
- Description: Sets the absolute max speed a vehicle can ever move in. This is X Y Z directions. This will prevent the ship from rapidly flying away. Try staying between 5 and 20. Higher values will increase potential of vehicle flying off to space
- Default Value: 10

### MaxVehicleLinearYVelocity 
- Description: Sets the absolute max speed a vehicle can ever move in vertical direction. This will limit the ship capability to launch into space. Lower values are safer. Too low and the vehicle will not use gravity well
- Default Value: 3

### MaxVehicleAngularVelocity 
- Description: Sets the absolute max speed a vehicle can ROTATE in. Having a high value means the vehicle can spin out of control.
- Default Value: 5

## Vehicle Physics: Floatation

### HullFloatationColliderLocation 
- Description: Hull Floatation Collider will determine the location the ship floats and hovers above the sea. Average is the average height of all Vehicle Hull Pieces attached to the vehicle. The point calculate is the center of the prefab. Center is the center point of all the float boats. This center point is determined by the max and min height points included for ship hulls. Lowest is the lowest most hull piece will determine the float height, allowing users to easily raise the ship if needed by adding a piece at the lowest point of the ship. Custom allows for setting floatation between -20 and 20
- Default Value: Custom

### HullFloatation Custom Offset 
- Description: DEPRECATED!!! Will be removed soon, values of -2 and 2 are allowed. Anything above means you are likely not using this correctly. Please use CenterOfMass instead if your vehicle needs to pivot lower. Hull Floatation Collider Customization. Set this value and it will always make the ship float at that offset, will only work when HullFloatationColliderLocation=Custom. Positive numbers sink ship, negative will make ship float higher.
- Default Value: 0

### EnableExactVehicleBounds_3.0.1 
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

### CameraOcclusionEnabled 
- Description: Enables hiding active raft pieces at specific intervals. This will hide only the rendered texture.
- Default Value: False

### DistanceToKeepObjects 
- Description: Threshold at which to retain a object even if it's through a wall.
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
