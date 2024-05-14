using UnityEngine;

namespace ValheimVehicles.Prefabs;

public abstract class ShipHulls
{
  public enum HullOrientation
  {
    Horizontal = 0,
    Vertical = 90,
    FortyFive = 45,
    TwentySix = 26,
  }

  public abstract class HullMaterial
  {
    public const string CoreWood = "core_wood";
    public const string ElderWood = "elder_wood";
    public const string Wood = "wood";
    public const string Iron = "iron";
    public const string YggdrasilWood = "yggdrasil_wood";
  }

  public static void SetMaterialValues(string hullMaterial, WearNTear wnt, int pieces)
  {
    switch (hullMaterial)
    {
      case HullMaterial.Iron:
        PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.Iron);
        wnt.m_health = 1200f * pieces;
        break;
      case HullMaterial.Wood:
        wnt.m_health = 600f * pieces;
        break;
    }
  }

  public static bool IsHull(GameObject go)
  {
    var goName = go.name;
    return goName.Contains(PrefabNames.ShipHullCenterWoodPrefabName) ||
           goName.Contains(PrefabNames.ShipHullCenterIronPrefabName) ||
           goName.Contains(PrefabNames.ShipHullRibIronPrefabName) ||
           goName.Contains(PrefabNames.ShipHullRibWoodPrefabName)
           || goName.Contains(PrefabNames.HullWall) || goName.Contains(PrefabNames.HullSlab);
  }
}