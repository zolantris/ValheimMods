using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Prefabs;

/// <summary>
/// TODO from the exported assets bundle auto generate this...good luck.
/// </summary>
public abstract class SpriteNames
{
  public const string VikingMast = "vikingmast";
  public const string ShipSteeringWheel = "steering_wheel";
  public const string DirtFloor = "dirtfloor_icon";
  public const string BoardingRamp = "boarding_ramp";
  public const string WaterOpacityBucket = "water_opacity_bucket";
  public const string WaterFloatationHeight = "sailing_boat_float";
  public const string TankTreadIcon = "tank_tread_icon";
  public const string ShipRudderAdvancedWood = "rudder_advanced_single_wood";
  public const string ShipRudderAdvancedIron = "rudder_advanced_single_iron";
  public const string Anchor = "anchor";

  // Items
  public const string VehicleHammer = "vehicle_hammer";

  // Walls/Floors
  public const string ShipRudderAdvancedDoubleWood =
    "rudder_advanced_double_wood";

  public const string LandVehicle = "vehicle_land";

  public const string ShipRudderAdvancedDoubleIron =
    "rudder_advanced_double_iron";

  public const string ShipRudderBasic = "rudder_basic";
  public const string ShipKeel = "keel";

  public const string HullCenterWood = "hull_center_wood";
  public const string HullCenterIron = "hull_center_iron";
  public const string RopeLadder = "rope_ladder";

  public const string HullWall = "hull_wall";

  // a mixin that must be combined with material and variant
  public const string HullSlab = "hull_slab";
  public const string HullSlabIron = "hull_slab_iron";
  public const string HullSlabWood = "hull_slab_wood";

  public const string HullRibWood = "hull_rib_wood";
  public const string HullRibIron = "hull_rib_iron";

  // Windows
  public const string WindowPortholeStandalone = "window_porthole_standalone";

  public const string WindowWallPorthole2x2 =
    "hull_wall_window_porthole_iron_2x2";

  public const string WindowWallPorthole4x4 =
    "hull_wall_window_porthole_iron_4x4";

  public const string WindowWallPorthole8x4 =
    "hull_wall_window_porthole_iron_8x4";

  public const string WindowFloorPorthole4x4Prefab =
    "hull_floor_window_porthole_iron_4x4";

  public const string WindowWallSquareIron = "hull_wall_window_square_iron_2x2";
  public const string WindowWallSquareWood = "hull_wall_window_square_wood_2x2";

  // Experimental Prefabs
  public const string Nautilus = "nautilus";

  // Mechanisms
  public const string MechanismSwitch = "mechanism_switch";
  public const string ElectricPylon = "mechanism_electric_pylon";
  public const string CoalEngine = "mechanism_engine_coal";
  public const string MechanismSwivel = "swivel";


  /// <summary>
  /// Can be top,bottom,left,right this helper is likely not needed, but provided for organization
  /// </summary>
  /// <param name="dir"></param>
  /// <returns></returns>
  public static string GetRamBladeName(string dir)
  {
    return $"ram_blade_{dir}";
  }

  // Should be 1:1 prefab and prefab icon.
  public static string GetCustomMastName(string mastLevel)
  {
    return PrefabNames.GetMastByLevelFromAssetBundle(mastLevel);
  }

  public static string GetRamStakeName(string material, int size)
  {
    var sizeString = size == 1 ? "1x2" : "2x4";
    return $"ram_stake_{material}_{sizeString}";
  }
}