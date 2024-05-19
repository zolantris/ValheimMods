## Settings file was created by plugin ValheimRAFT v2.0.4

## Plugin GUID: zolantris.ValheimRAFT

[Config]

## Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their manager renames the folder, r2modman has a fallback case added to search for zolantris-ValheimRAFTDefault search values are an ordered list first one is always matching non-empty strings from this pluginFolderName.Folder Matches are:  zolantris-ValheimRAFT, zolantris-ValheimRAFT Zolantris-ValheimRAFT, and ValheimRAFT

# Setting type: String

# Default value:

pluginFolderName =

## Set the raft health when used with wearNTear, lowest value is 100f

# Setting type: Single

# Default value: 500

raftHealth = 500

## Sets the raftcreative command height, raftcreative is relative to the current height of the ship, negative numbers will sink your ship temporarily

# Setting type: Single

# Default value: 5

RaftCreativeHeight = 5

## Anchor keyboard hotkey. Only applies to keyboard

# Setting type: KeyboardShortcut

# Default value: LeftShift

AnchorKeyboardShortcut = LeftShift

Initial Floor Height = 0.6

[Debug]

## Enable sentry debug logging. Requires sentry logging plugin installed to work. Sentry Logging plugin will make it easier to troubleshoot raft errors and detect performance bottlenecks. The bare minimum is collected, and only data related to ValheimRaft. See https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT#logging-metrics for more details about what is collected

# Setting type: Boolean

# Default value: true

Enable Sentry Metrics (requires sentryUnityPlugin) = true

## Outputs more debug logs for the Vehicle components. Useful for troubleshooting errors, but will spam logs

# Setting type: Boolean

# Default value: false

Debug logging for Vehicle/Raft = false

## Removes the start scene background, only use this if you want to speedup start time

# Setting type: Boolean

# Default value: false

DebugRemoveStartMenuBackground = true

# Setting type: Boolean

# Default value: true

ShowShipState = true

## Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only.

# Setting type: Boolean

# Default value: false

HasDebugSails = false

enableMetrics, (sentryPlugin) = true

HasDebugBase = true

[Deprecated Config]

## Allows users to set the raft floor spawn height. 0.45 was the original height in 1.4.9 but it looked a bit too low. Now people can customize it

# Setting type: Single

# Default value: 0.6

Initial Floor Height (V1 raft) = 0.6

[Floatation]

## Makes the Ship Hull prefabs be the sole source of collisions, meaning ships with wider tops will not collide at bottom terrain due to their width above water. Requires a Hull, without a hull it will previous box around all items in ship

# Setting type: Boolean

# Default value: true

Only Use Hulls For Floatation Collisions = true

[Graphics]

## Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled

# Setting type: Boolean

# Default value: true

Sails Fade In Fog = true

[Patches]

## Fixes the PlanBuild mod position problems with ValheimRaft so it uses localPosition of items based on the parent raft. This MUST be enabled to support PlanBuild but can be disabled when the mod owner adds direct support for this part of ValheimRAFT. PlanBuild mod can be found here. https://thunderstore.io/c/valheim/p/MathiasDecrock/PlanBuild/

# Setting type: Boolean

# Default value: false

Enable PlanBuild Patches (required to be on if you installed PlanBuild) = false

fixPlanBuildPositionIssues = false

[Propulsion]

## Sets the absolute max speed a ship can ever hit. This is capped on the vehicle, so no forces applied will be able to exceed this value. 20-30f is safe, higher numbers could let the ship fail off the map

# Setting type: Single

# Default value: 30

MaxPropulsionSpeed = 30

## Sets the absolute max speed a ship can ever hit with sails. Prevents or enables space launches, cannot exceed MaxPropulsionSpeed.

# Setting type: Single

# Default value: 10

MaxSailSpeed = 10

## Sets the mass percentage of the ship that will slow down the sails

# Setting type: Single

# Default value: 55

MassPercentage = 55

## Sets the speed at which it becomes significantly harder to gain speed per sail area

# Setting type: Single

# Default value: 1

SpeedCapMultiplier = 1

## Set the Back speed of rudder, this will apply with sails

# Setting type: Single

# Default value: 1

Rudder Back Speed = 30

## Set the Half speed of rudder, this will apply with sails

# Setting type: Single

# Default value: 1

Rudder Slow Speed = 5

## Set the Slow speed of rudder, this will apply with sails

# Setting type: Single

# Default value: 0

Rudder Half Speed = 5

## Set the Full speed of rudder, this will apply with sails

# Setting type: Single

# Default value: 0

Rudder Full Speed = 50

## enables ship weight calculations for sail-force (sailing speed) and future propulsion, makes larger ships require more sails and smaller ships require less

# Setting type: Boolean

# Default value: true

HasShipWeightCalculations = true

## enables ship weight calculations for containers which affects sail-force (sailing speed) and future propulsion calculations. Makes ships with lots of containers require more sails

# Setting type: Boolean

# Default value: true

HasShipContainerWeightCalculations = true

## Enables all custom propulsion values

# Setting type: Boolean

# Default value: false

EnableCustomPropulsionConfig = false

## Manual sets the sail wind area multiplier the custom tier1 sail. Currently there is only 1 tier

# Setting type: Single

# Default value: 0.5

SailCustomAreaTier1Multiplier = 0.5

## Manual sets the sail wind area of the tier 1 sail.

# Setting type: Single

# Default value: 5

SailTier1Area = 5

## Manual sets the sail wind area of the tier 2 sail.

# Setting type: Single

# Default value: 7.5

SailTier2Area = 7.5

## Manual sets the sail wind area of the tier 3 sail.

# Setting type: Single

# Default value: 10

SailTier3Area = 10

## Saves the user's fingers by allowing the ship to continue to climb or descend without needing to hold the button

# Setting type: Boolean

# Default value: true

Flight Vertical Continues UntilToggled = true

## Flight allows for different rudder speeds, only use those and ignore sails

# Setting type: Boolean

# Default value: false

Only allow rudder speeds during flight = true

[Server config]

## Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer

# Setting type: Boolean

# Default value: true

Protect Vehicle pieces from breaking on Error = true

## ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart

# Setting type: Boolean

# Default value: false

AdminsCanOnlyBuildRaft = false

## Allows the V1 Raft to be built, this Raft is not performant, but remains in >=v2.0.0 as a Fallback in case there are problems with the new raft

# Setting type: Boolean

# Default value: false

AllowOldV1RaftRecipe = false

## Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.

# Setting type: Single

# Default value: 10

ServerRaftUpdateZoneInterval = 10

## Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.

# Setting type: Boolean

# Default value: true

MakeAllPiecesWaterProof = true

## Allow the raft to fly (jump\crouch to go up and down)

# Setting type: Boolean

# Default value: false

AllowFlight = false

## Allow the raft to use custom rudder speeds set by the player, these speeds are applied alongside sails at half and full speed

# Setting type: Boolean

# Default value: true

AllowCustomRudderSpeeds = true

[Sounds]

## Toggles the ship sail sounds.

# Setting type: Boolean

# Default value: true

Ship Sailing Sounds = false

## Toggles Ship Wake sounds. Can be pretty loud

# Setting type: Boolean

# Default value: true

Ship Wake Sounds = false

## Toggles ShipInWater Sounds, the sound of the hull hitting water

# Setting type: Boolean

# Default value: true

Ship In-Water Sounds = false

[Vehicles]

## Hull Floatation Collider will determine the location the ship floats and hovers above the sea. Average is the average height of all Vehicle Hull Pieces attached to the vehicle. The point calculate is the center of the prefab. Center is the center point of all the float boats. This center point is determined by the max and min height points included for ship hulls. Lowest is the lowest most hull piece will determine the float height, allowing users to easily raise the ship if needed by adding a piece at the lowest point of the ship.

# Setting type: HullFloatation

# Default value: Average

# Acceptable values: Average, Center, Bottom, Top

HullFloatationColliderLocation = Center

## Enables the debug menu, for showing colliders or rotating the ship

# Setting type: Boolean

# Default value: false

HasVehicleDebug = true

## Ensures that a piece placed within the raft is included in the float collider correctly. May not be accurate if the parent GameObjects are changing their scales above or below 1,1,1. Mods like Gizmo could be incompatible

# Setting type: Boolean

# Default value: false

EnableExactVehicleBounds = true

