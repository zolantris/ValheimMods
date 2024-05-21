
## Debug

### Enable Sentry Metrics (requires sentryUnityPlugin) 
    * Description: Enable sentry debug logging. Requires sentry logging plugin installed to work. Sentry Logging plugin will make it easier to troubleshoot raft errors and detect performance bottlenecks. The bare minimum is collected, and only data related to ValheimRaft. See https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT#logging-metrics for more details about what is collected
    * Default Value: true

### Debug logging for Vehicle/Raft 
    * Description: Outputs more debug logs for the Vehicle components. Useful for troubleshooting errors, but will spam logs
    * Default Value: false

## Patches

### Enable PlanBuild Patches (required to be on if you installed PlanBuild) 
    * Description: Fixes the PlanBuild mod position problems with ValheimRaft so it uses localPosition of items based on the parent raft. This MUST be enabled to support PlanBuild but can be disabled when the mod owner adds direct support for this part of ValheimRAFT. PlanBuild mod can be found here. https://thunderstore.io/c/valheim/p/MathiasDecrock/PlanBuild/
    * Default Value: false

## Deprecated Config

### Initial Floor Height (V1 raft) 
    * Description: Allows users to set the raft floor spawn height. 0.45 was the original height in 1.4.9 but it looked a bit too low. Now people can customize it
    * Default Value: 0.6

## Config

### pluginFolderName 
    * Description: Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their manager renames the folder, r2modman has a fallback case added to search for zolantris-ValheimRAFTDefault search values are an ordered list first one is always matching non-empty strings from this pluginFolderName.Folder Matches are:  zolantris-ValheimRAFT, zolantris-ValheimRAFT Zolantris-ValheimRAFT, and ValheimRAFT
    * Default Value: 

### raftHealth 
    * Description: Set the raft health when used with wearNTear, lowest value is 100f
    * Default Value: 500

## Debug

### DebugRemoveStartMenuBackground 
    * Description: Removes the start scene background, only use this if you want to speedup start time
    * Default Value: true

## Server config

### Protect Vehicle pieces from breaking on Error 
    * Description: Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer
    * Default Value: true

### AdminsCanOnlyBuildRaft 
    * Description: ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart
    * Default Value: false

### AllowOldV1RaftRecipe 
    * Description: Allows the V1 Raft to be built, this Raft is not performant, but remains in >=v2.0.0 as a Fallback in case there are problems with the new raft
    * Default Value: false

### ServerRaftUpdateZoneInterval 
    * Description: Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.
    * Default Value: 10

### MakeAllPiecesWaterProof 
    * Description: Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.
    * Default Value: true

### AllowFlight 
    * Description: Allow the raft to fly (jump\crouch to go up and down)
    * Default Value: true

### AllowCustomRudderSpeeds 
    * Description: Allow the raft to use custom rudder speeds set by the player, these speeds are applied alongside sails at half and full speed
    * Default Value: true

## Config

### RaftCreativeHeight 
    * Description: Sets the raftcreative command height, raftcreative is relative to the current height of the ship, negative numbers will sink your ship temporarily
    * Default Value: 5

## Floatation

### Only Use Hulls For Floatation Collisions 
    * Description: Makes the Ship Hull prefabs be the sole source of collisions, meaning ships with wider tops will not collide at bottom terrain due to their width above water. Requires a Hull, without a hull it will previous box around all items in ship
    * Default Value: true

## Config

### AnchorKeyboardShortcut 
    * Description: Anchor keyboard hotkey. Only applies to keyboard
    * Default Value: LeftShift

## Vehicles

### HullFloatationColliderLocation 
    * Description: Hull Floatation Collider will determine the location the ship floats and hovers above the sea. Average is the average height of all Vehicle Hull Pieces attached to the vehicle. The point calculate is the center of the prefab. Center is the center point of all the float boats. This center point is determined by the max and min height points included for ship hulls. Lowest is the lowest most hull piece will determine the float height, allowing users to easily raise the ship if needed by adding a piece at the lowest point of the ship.
    * Default Value: Average

### HasVehicleDebug 
    * Description: Enables the debug menu, for showing colliders or rotating the ship
    * Default Value: true

### EnableExactVehicleBounds 
    * Description: Ensures that a piece placed within the raft is included in the float collider correctly. May not be accurate if the parent GameObjects are changing their scales above or below 1,1,1. Mods like Gizmo could be incompatible
    * Default Value: false

## Debug

### ShowShipState 
    * Description: 
    * Default Value: true

## Propulsion

### MaxPropulsionSpeed 
    * Description: Sets the absolute max speed a ship can ever hit. This is capped on the vehicle, so no forces applied will be able to exceed this value. 20-30f is safe, higher numbers could let the ship fail off the map
    * Default Value: 30

### MaxSailSpeed 
    * Description: Sets the absolute max speed a ship can ever hit with sails. Prevents or enables space launches, cannot exceed MaxPropulsionSpeed.
    * Default Value: 10

### MassPercentage 
    * Description: Sets the mass percentage of the ship that will slow down the sails
    * Default Value: 55

### SpeedCapMultiplier 
    * Description: Sets the speed at which it becomes significantly harder to gain speed per sail area
    * Default Value: 1

### Rudder Back Speed 
    * Description: Set the Back speed of rudder, this will apply with sails
    * Default Value: 30

### Rudder Slow Speed 
    * Description: Set the Half speed of rudder, this will apply with sails
    * Default Value: 5

### Rudder Half Speed 
    * Description: Set the Slow speed of rudder, this will apply with sails
    * Default Value: 5

### Rudder Full Speed 
    * Description: Set the Full speed of rudder, this will apply with sails
    * Default Value: 50

### HasShipWeightCalculations 
    * Description: enables ship weight calculations for sail-force (sailing speed) and future propulsion, makes larger ships require more sails and smaller ships require less
    * Default Value: true

### HasShipContainerWeightCalculations 
    * Description: enables ship weight calculations for containers which affects sail-force (sailing speed) and future propulsion calculations. Makes ships with lots of containers require more sails
    * Default Value: true

## Debug

### HasDebugSails 
    * Description: Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only.
    * Default Value: false

## Propulsion

### EnableCustomPropulsionConfig 
    * Description: Enables all custom propulsion values
    * Default Value: false

### SailCustomAreaTier1Multiplier 
    * Description: Manual sets the sail wind area multiplier the custom tier1 sail. Currently there is only 1 tier
    * Default Value: 0.5

### SailTier1Area 
    * Description: Manual sets the sail wind area of the tier 1 sail.
    * Default Value: 5

### SailTier2Area 
    * Description: Manual sets the sail wind area of the tier 2 sail.
    * Default Value: 7.5

### SailTier3Area 
    * Description: Manual sets the sail wind area of the tier 3 sail.
    * Default Value: 10

### Flight Vertical Continues UntilToggled 
    * Description: Saves the user's fingers by allowing the ship to continue to climb or descend without needing to hold the button
    * Default Value: true

### Only allow rudder speeds during flight 
    * Description: Flight allows for different rudder speeds, only use those and ignore sails
    * Default Value: true

## Graphics

### Sails Fade In Fog 
    * Description: Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled
    * Default Value: true

## Sounds

### Ship Sailing Sounds 
    * Description: Toggles the ship sail sounds.
    * Default Value: false

### Ship Wake Sounds 
    * Description: Toggles Ship Wake sounds. Can be pretty loud
    * Default Value: false

### Ship In-Water Sounds 
    * Description: Toggles ShipInWater Sounds, the sound of the hull hitting water
    * Default Value: false
