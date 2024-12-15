namespace ValheimVehicles.Prefabs;

public abstract class SpriteNames
{
  public const string VikingMast = "vikingmast";
  public const string ShipSteeringWheel = "steering_wheel";
  public const string DirtFloor = "dirtfloor_icon";
  public const string BoardingRamp = "boarding_ramp";
  public const string WaterOpacityBucket = "water_opacity_bucket";
  public const string ShipRudderAdvancedWood = "rudder_advanced_single_wood";
  public const string ShipRudderAdvancedIron = "rudder_advanced_single_iron";

  public const string ShipRudderAdvancedDoubleWood =
    "rudder_advanced_double_wood";

  public const string ShipRudderAdvancedDoubleIron =
    "rudder_advanced_double_iron";

  public const string ShipRudderBasic = "rudder_basic";
  public const string ShipKeel = "keel";
  public const string VehicleSwitch = "mechanical_switch";
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
  public const string WindowPortholeStandalone = "window_porthole_standalone";
  public const string WindowWallPorthole = "hull_wall_window_porthole_iron_2x2";
  public const string WindowWallPorthole4x4 = "hull_wall_window_porthole_iron_4x4";
  public const string WindowWallSquareIron = "hull_wall_window_square_iron_2x2";
  public const string WindowWallSquareWood = "hull_wall_window_square_wood_2x2";

  public const string Nautilus = "nautilus";

  /// <summary>
  /// Can be top,bottom,left,right this helper is likely not needed, but provided for organization
  /// </summary>
  /// <param name="dir"></param>
  /// <returns></returns>
  public static string GetRamBladeName(string dir)
  {
    return $"ram_blade_{dir}";
  }

  public static string GetRamStakeName(string material, int size)
  {
    var sizeString = size == 1 ? "1x2" : "2x4";
    return $"ram_stake_{material}_{sizeString}";
  }
}