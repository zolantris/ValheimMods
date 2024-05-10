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
    public const string Iron = "Iron";
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
           goName.Contains(PrefabNames.ShipHullRibWoodPrefabName) ||
           goName.Contains(PrefabNames.ShipHullSlabIronPrefabName) ||
           goName.Contains(PrefabNames.ShipHullSlabWoodPrefabName);
  }

  public static string GetVanillaPrefab(string hullMaterial, HullOrientation hullOrientation)
  {
    string prefabMaterialSegment = "";
    string prefabOrientationSegment = "";
    switch (hullMaterial)
    {
      case HullMaterial.CoreWood:
        prefabMaterialSegment = "wood_wall_log";
        break;
      case HullMaterial.Wood:
        prefabMaterialSegment = "wood_beam";
        break;
    }

    switch (hullOrientation)
    {
      case HullOrientation.Horizontal:
        if (hullMaterial == HullMaterial.CoreWood)
        {
          prefabOrientationSegment = "_4x0.5";
        }

        break;
      case HullOrientation.FortyFive:
        if (hullMaterial == HullMaterial.CoreWood)
        {
          prefabOrientationSegment = "_45";
        }

        break;
    }

    return $"{prefabMaterialSegment}{prefabOrientationSegment}";
  }

  public static string GetHullPrefabName(string hullMaterial, HullOrientation hullOrientation)
  {
    return $"vv_ship_hull_{GetVanillaPrefab(hullMaterial, hullOrientation)}";
  }

  public static string GetHullTranslations(string hullMaterial, HullOrientation hullOrientation)
  {
    if (hullMaterial == HullMaterial.CoreWood)
    {
      if (hullOrientation == HullOrientation.Horizontal)
      {
        return "$mb_ship_hull_corewood_0";
      }
    }

    return "$mb_ship_hull_corewood_0";
  }
}