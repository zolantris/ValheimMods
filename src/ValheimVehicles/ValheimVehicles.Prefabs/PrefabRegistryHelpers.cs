using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Prefabs.Registry;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs;

public abstract class PrefabRegistryHelpers
{
  public const string SnappointTag = "snappoint";
  public static int PieceLayer;

  public struct PieceData
  {
    public string Name;
    public string Description;
    public Sprite Icon;
  }

  // may use for complex shared variant prefabs
  // idea is to send in the keys and then register the PieceData
  public static void RegisterPieceWithVariant(string prefabName, string translationKey,
    string hullMaterials, PrefabNames.PrefabSizeVariant sizeVariant)
  {
  }

  public static readonly Dictionary<string, PieceData> PieceDataDictionary = new();

  private static void RegisterRamPieces()
  {
    var ramNoseIcon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.RamNose);

    PieceDataDictionary.Add(
      PrefabNames.RamNose, new PieceData()
      {
        Name = "valheim_vehicles_ram_nose $valheim_vehicles_material_bronze",
        Description = "valheim_vehicles_ram_nose_desc",
        Icon = ramNoseIcon
      });

    string[] bladeDirs = ["top", "bottom", "left", "right"];

    foreach (var bladeDir in bladeDirs)
    {
      PieceDataDictionary.Add(
        PrefabNames.GetRamBladeName(bladeDir), new PieceData()
        {
          Name =
            $"valheim_vehicles_ram_blade $valheim_vehicles_direction_{bladeDir} $valheim_vehicles_material_bronze",
          Description = "valheim_vehicles_ram_blade_desc",
          Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(
            SpriteNames.GetRamBladeName(bladeDir))
        });
    }
  }

  private static void RegisterExternalShips()
  {
    if (!ValheimRaftPlugin.Instance.AllowExperimentalPrefabs.Value) return;

    const string prefabName = "Nautilus Submarine";
    const string description = $"Experimental Nautilus technology discovered. Have Fun!";
    PieceDataDictionary.Add(
      PrefabNames.Nautilus, new PieceData()
      {
        Name = prefabName,
        Description = description,
        Icon = LoadValheimAssets.vanillaRaftPrefab.GetComponent<Piece>().m_icon
      });
  }

// todo consider using Jotunn.Manager.RenderManager for these Icon generation
  /// todo auto generate this from the translations json
  /// 4x4 and 2x2 icons look similar, may remove 4x4
  public static void Init()
  {
    PieceLayer = LayerMask.NameToLayer("piece");
    var spriteAtlas = LoadValheimVehicleAssets.VehicleSprites;

    RegisterRamPieces();
    RegisterExternalShips();
    // slabs
    const string slabName = "valheim_vehicles_hull_slab";
    const string slabDescription = $"{slabName}_desc";
    PieceDataDictionary.Add(
      PrefabNames.GetHullSlabVariants(ShipHulls.HullMaterial.Wood,
        PrefabNames.PrefabSizeVariant.Two), new PieceData()
      {
        Name = $"{slabName} 2x2 $valheim_vehicles_material_wood",
        Description = $"{slabDescription}",
        Icon = spriteAtlas.GetSprite("hull_slab_2x2_wood")
      });

    PieceDataDictionary.Add(
      PrefabNames.GetHullSlabVariants(ShipHulls.HullMaterial.Wood,
        PrefabNames.PrefabSizeVariant.Four), new PieceData()
      {
        Name = $"{slabName} 4x4 $valheim_vehicles_material_wood",
        Description = $"{slabDescription}",
        Icon = spriteAtlas.GetSprite("hull_slab_4x4_wood")
      });

    PieceDataDictionary.Add(
      PrefabNames.GetHullSlabVariants(ShipHulls.HullMaterial.Iron,
        PrefabNames.PrefabSizeVariant.Two), new PieceData()
      {
        Name = $"{slabName} 2x2 $valheim_vehicles_material_iron",
        Description = $"{slabDescription}",
        Icon = spriteAtlas.GetSprite("hull_slab_2x2_iron")
      });

    PieceDataDictionary.Add(
      PrefabNames.GetHullSlabVariants(ShipHulls.HullMaterial.Iron,
        PrefabNames.PrefabSizeVariant.Four), new PieceData()
      {
        Name = $"{slabName} 4x4 $valheim_vehicles_material_iron",
        Description = $"{slabDescription}",
        Icon = spriteAtlas.GetSprite("hull_slab_4x4_iron")
      });

    // Hull walls
    const string wallName = "valheim_vehicles_hull_wall";
    const string wallDescription = $"{wallName}_desc";

    PieceDataDictionary.Add(
      PrefabNames.GetHullWallVariants(ShipHulls.HullMaterial.Wood,
        PrefabNames.PrefabSizeVariant.Two), new PieceData()
      {
        Name = $"{wallName} 2x2 $valheim_vehicles_material_wood",
        Description = $"{wallDescription}",
        Icon = spriteAtlas.GetSprite("hull_wall_2x2_wood")
      });

    PieceDataDictionary.Add(
      PrefabNames.GetHullWallVariants(ShipHulls.HullMaterial.Wood,
        PrefabNames.PrefabSizeVariant.Four), new PieceData()
      {
        Name = $"{wallName} 4x4 $valheim_vehicles_material_wood",
        Description = $"{wallDescription}",
        Icon = spriteAtlas.GetSprite("hull_wall_4x4_wood")
      });

    PieceDataDictionary.Add(
      PrefabNames.GetHullWallVariants(ShipHulls.HullMaterial.Iron,
        PrefabNames.PrefabSizeVariant.Two), new PieceData()
      {
        Name = $"{wallName} 2x2 $valheim_vehicles_material_iron",
        Description = $"{wallDescription}",
        Icon = spriteAtlas.GetSprite("hull_wall_2x2_iron")
      });

    PieceDataDictionary.Add(
      PrefabNames.GetHullWallVariants(ShipHulls.HullMaterial.Iron,
        PrefabNames.PrefabSizeVariant.Four), new PieceData()
      {
        Name = $"{wallName} 4x4 $valheim_vehicles_material_iron",
        Description = $"{wallDescription}",
        Icon = spriteAtlas.GetSprite("hull_wall_4x4_iron")
      });

    // end of hulls

    PieceDataDictionary.Add(PrefabNames.WaterVehicleShip, new PieceData()
    {
      Name = "valheim_vehicles_water_vehicle",
      Description = "valheim_vehicles_water_vehicle_desc",
      Icon = LoadValheimAssets.vanillaRaftPrefab.GetComponent<Piece>().m_icon
    });

    // hull rib variants
    PieceDataDictionary.Add(PrefabNames.ShipHullRibIronPrefabName, new PieceData()
    {
      Name = "valheim_vehicles_hull_rib_iron",
      Description = "valheim_vehicles_hull_rib_iron_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.HullRibIron)
    });

    PieceDataDictionary.Add(PrefabNames.ShipHullRibWoodPrefabName, new PieceData()
    {
      Name = "valheim_vehicles_hull_rib_wood",
      Description = "valheim_vehicles_hull_rib_wood_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.HullRibWood)
    });

    // hull center variants
    PieceDataDictionary.Add(PrefabNames.ShipHullCenterWoodPrefabName, new PieceData()
    {
      Name = "valheim_vehicles_hull_center_wood",
      Description = "valheim_vehicles_hull_center_wood_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.HullCenterWood)
    });

    PieceDataDictionary.Add(PrefabNames.ShipHullCenterIronPrefabName, new PieceData()
    {
      Name = "valheim_vehicles_hull_center_iron",
      Description = "valheim_vehicles_hull_center_iron_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.HullCenterIron)
    });


    PieceDataDictionary.Add(PrefabNames.ShipSteeringWheel, new PieceData()
    {
      Name = "valheim_vehicles_wheel",
      Description = "valheim_vehicles_wheel_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.ShipSteeringWheel)
    });

    PieceDataDictionary.Add(PrefabNames.ShipKeel, new PieceData()
    {
      Name = "valheim_vehicles_keel",
      Description = "valheim_vehicles_keel_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.ShipKeel)
    });

    PieceDataDictionary.Add(PrefabNames.ShipRudderBasic, new PieceData()
    {
      Name = "valheim_vehicles_rudder_basic",
      Description = "valheim_vehicles_rudder_basic_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.ShipRudderBasic)
    });

    PieceDataDictionary.Add(PrefabNames.ShipRudderAdvancedWood, new PieceData()
    {
      Name = "valheim_vehicles_rudder_advanced $valheim_vehicles_material_wood",
      Description = "valheim_vehicles_rudder_advanced_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipRudderAdvancedWood)
    });
    PieceDataDictionary.Add(PrefabNames.ShipRudderAdvancedIron, new PieceData()
    {
      Name = "valheim_vehicles_rudder_advanced $valheim_vehicles_material_iron",
      Description = "valheim_vehicles_rudder_advanced_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipRudderAdvancedIron)
    });

    PieceDataDictionary.Add(PrefabNames.ShipRudderAdvancedDoubleWood, new PieceData()
    {
      Name = $"valheim_vehicles_rudder_advanced_double $valheim_vehicles_material_wood",
      Description = "valheim_vehicles_rudder_advanced_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipRudderAdvancedDoubleWood)
    });
    PieceDataDictionary.Add(PrefabNames.ShipRudderAdvancedDoubleIron, new PieceData()
    {
      Name = $"valheim_vehicles_rudder_advanced_double $valheim_vehicles_material_iron",
      Description = "valheim_vehicles_rudder_advanced_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ShipRudderAdvancedDoubleIron)
    });

    PieceDataDictionary.Add(PrefabNames.VehicleToggleSwitch, new PieceData()
    {
      Name = "valheim_vehicles_toggle_switch",
      Description = "valheim_vehicles_toggle_switch_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.VehicleSwitch)
    });
  }

  /// <summary>
  /// Auto sets up new, worn, broken for wnt
  /// </summary>
  /// <param name="prefab"></param>
  /// <param name="wnt"></param>
  public static void AddNewOldPiecesToWearNTear(GameObject prefab, WearNTear wnt)
  {
    var wntNew = prefab.transform.FindDeepChild("new");
    var wntWorn = prefab.transform.FindDeepChild("worn");
    var wntBroken = prefab.transform.FindDeepChild("broken");

    if (!(bool)wntNew) return;
    wnt.m_new = wntNew.gameObject;
    if (!(bool)wntWorn) return;
    wnt.m_worn = wntWorn.gameObject;
    wnt.m_broken = wntWorn.gameObject;
    if (!(bool)wntBroken) return;
    wnt.m_broken = wntBroken.gameObject;
  }

  public static string GetPieceNameFromPrefab(string name)
  {
    return Localization.instance.Localize(PieceDataDictionary.GetValueSafe(name).Name);
  }

  public static Piece AddPieceForPrefab(string prefabName, GameObject prefab)
  {
    var pieceInformation = PieceDataDictionary.GetValueSafe(prefabName);

    var piece = prefab.AddComponent<Piece>();

    // dollar sign added for translation reference
    piece.m_name = $"${pieceInformation.Name}";
    piece.m_description = $"${pieceInformation.Description}";

    piece.m_icon = pieceInformation.Icon;

    return piece;
  }

  public static ZNetView AddNetViewWithPersistence(GameObject prefab)
  {
    var netView = prefab.GetComponent<ZNetView>();
    if (!(bool)netView)
    {
      netView = prefab.AddComponent<ZNetView>();
    }

    if (!netView)
    {
      Logger.LogError("Unable to register NetView, ValheimRAFT could be broken without netview");
      return netView;
    }

    netView.m_persistent = true;

    return netView;
  }

  public static WearNTear GetWearNTearSafe(GameObject prefabComponent)
  {
    var wearNTearComponent = prefabComponent.GetComponent<WearNTear>();
    if (!(bool)wearNTearComponent)
    {
      // Many components do not have WearNTear so they must be added to the prefabPiece
      wearNTearComponent = prefabComponent.AddComponent<WearNTear>();
      if (!wearNTearComponent)
        Logger.LogError(
          $"error setting WearNTear for RAFT prefab {prefabComponent.name}, the ValheimRAFT mod may be unstable without WearNTear working properly");
    }

    return wearNTearComponent;
  }

  public static WearNTear SetWearNTear(GameObject prefabComponent, int tierMultiplier = 1,
    bool canFloat = false)
  {
    var wearNTearComponent = GetWearNTearSafe(prefabComponent);

    wearNTearComponent.m_noSupportWear = canFloat;
    wearNTearComponent.m_destroyedEffect =
      LoadValheimAssets.woodFloorPieceWearNTear.m_destroyedEffect;
    wearNTearComponent.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;

    if (tierMultiplier == 1)
    {
      wearNTearComponent.m_materialType = WearNTear.MaterialType.Wood;
      wearNTearComponent.m_destroyedEffect =
        LoadValheimAssets.woodFloorPieceWearNTear.m_destroyedEffect;
    }
    else if (tierMultiplier == 2)
    {
      wearNTearComponent.m_materialType = WearNTear.MaterialType.Stone;
      wearNTearComponent.m_destroyedEffect =
        LoadValheimAssets.stoneFloorPieceWearNTear.m_destroyedEffect;
      wearNTearComponent.m_hitEffect = LoadValheimAssets.stoneFloorPieceWearNTear.m_hitEffect;
    }
    else if (tierMultiplier == 3)
    {
      wearNTearComponent.m_materialType = WearNTear.MaterialType.Iron;
      wearNTearComponent.m_destroyedEffect =
        LoadValheimAssets.stoneFloorPieceWearNTear.m_destroyedEffect;
      wearNTearComponent.m_hitEffect = LoadValheimAssets.stoneFloorPieceWearNTear.m_hitEffect;
    }

    wearNTearComponent.m_health = PrefabRegistryController.wearNTearBaseHealth * tierMultiplier;
    wearNTearComponent.m_noRoofWear = false;

    return wearNTearComponent;
  }

  /**
   * experimentally add snappoints
   */
  public static void AddSnapPoint(string name, GameObject parentObj)
  {
    var snappointObj = new GameObject()
    {
      name = name,
      tag = SnappointTag
    };
    Object.Instantiate(snappointObj, parentObj.transform);
  }

  public static void FixCollisionLayers(GameObject r)
  {
    var piece = r.layer = LayerMask.NameToLayer("piece");
    var comps = r.transform.GetComponentsInChildren<Transform>(true);
    for (var i = 0; i < comps.Length; i++) comps[i].gameObject.layer = piece;
  }

  public static WearNTear SetWearNTearSupport(WearNTear wntComponent,
    WearNTear.MaterialType materialType)
  {
    // this will use the base material support provided by valheim for support. This should be balanced for wood. Stone may need some tweaks for buoyancy and other balancing concerns
    wntComponent.m_materialType = materialType;

    return wntComponent;
  }


  /**
  * todo this needs to be fixed so the mast blocks only with the mast part and ignores the non-sail area.
  * if the collider is too big it also pushes the rigidbody system underwater (IE Raft sinks)
  *
  * May be easier to just get the game object structure for each sail and do a search for the sail and master parts.
  */
  public static void AddBoundsToAllChildren(string colliderName, GameObject parent,
    GameObject componentToEncapsulate)
  {
    var boxCol = parent.GetComponent<BoxCollider>();
    if (boxCol == null)
    {
      boxCol = parent.AddComponent<BoxCollider>();
    }

    boxCol.name = colliderName;

    Bounds bounds = new Bounds(parent.transform.position, Vector3.zero);

    var allDescendants = componentToEncapsulate.GetComponentsInChildren<Transform>();
    foreach (Transform desc in allDescendants)
    {
      Renderer childRenderer = desc.GetComponent<Renderer>();
      if (childRenderer != null)
      {
        bounds.Encapsulate(childRenderer.bounds);
      }

      boxCol.center = new Vector3(0, bounds.max.y,
        0);
      boxCol.size = boxCol.center * 2;
    }
  }

  public static void FixRopes(GameObject r)
  {
    var ropes = r.GetComponentsInChildren<LineAttach>();
    for (var i = 0; i < ropes.Length; i++)
    {
      ropes[i].GetComponent<LineRenderer>().positionCount = 2;
      ropes[i].m_attachments.Clear();
      ropes[i].m_attachments.Add(r.transform);
    }
  }

  /**
   * Deprecated...but still needed for a few older raft components
   */
  public static void FixSnapPoints(GameObject r)
  {
    var t = r.GetComponentsInChildren<Transform>(true);
    foreach (var t1 in t)
      if (t1.name.StartsWith($"_{SnappointTag}"))
        t1.tag = SnappointTag;
  }


  public static void HoistSnapPointsToPrefab(GameObject prefab)
  {
    HoistSnapPointsToPrefab(prefab, prefab.transform);
  }

// Use this to work around object resizing requiring repeated movement of child snappoints. This way snappoints can stay in the relative object without issue
  public static void HoistSnapPointsToPrefab(GameObject prefab, Transform parent,
    string[]? hoistParentNameFilters = null)
  {
    var transformObjs = parent.GetComponentsInChildren<Transform>(true);
    foreach (var transformObj in transformObjs)
    {
      if (transformObj.tag != SnappointTag)
        continue;
      if (hoistParentNameFilters != null)
      {
        foreach (var hoistName in hoistParentNameFilters)
        {
          if (!transformObj.parent.name.StartsWith(hoistName)) continue;
          transformObj.SetParent(prefab.transform);
          transformObj.gameObject.SetActive(false);
        }
      }
      else
      {
        transformObj.SetParent(prefab.transform);
        transformObj.gameObject.SetActive(false);
      }
    }
  }
}