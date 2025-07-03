using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class CannonPrefabs : RegisterPrefab<CannonPrefabs>
{

  private void RegisterCannonPrefab()
  {
    var asset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_fixed");
    var sprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite("cannon_fixed");

    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.CannonTier1,
      asset);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.CannonTier1, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_cannon_tier1",
      Description = "$valheim_vehicles_cannon_tier1_desc",
      Icon = sprite
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.CannonTier1, prefab);
    var wearNTear = PrefabRegistryHelpers.SetWearNTear(prefab, 3);
    // main toggle switch.
    prefab.AddComponent<CannonController>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 4,
          Item = "Bronze",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }

  private void RegisterCannonballSolidItemPrefab()
  {
    var prefab = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_ball_bronze");
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("cannon_ball_bronze");
    if (!prefab)
    {
      LoggerProvider.LogError("cannon_ball_bronze not found!");
      return;
    }


    var cannonBall = prefab.AddComponent<Cannonball>();
    cannonBall.cannonballType = Cannonball.CannonballType.Solid;

    CannonController.CannonballSolidPrefab = cannonBall;

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();

    // verbosely add these.
    zSyncTransform.m_syncBodyVelocity = true;
    zSyncTransform.m_syncRotation = true;
    zSyncTransform.m_syncPosition = true;

    var itemDrop = prefab.AddComponent<ItemDrop>();
    if (itemDrop.m_nview == null)
    {
      itemDrop.m_nview = nv;
    }

    itemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData
    {
      m_name = PrefabNames.CannonballSolid,
      m_icons = [icon],
      m_buildPieces = PrefabRegistryController.GetPieceTable(),
      m_toolTier = 0,
      m_equipDuration = 0,
      m_skillType = Skills.SkillType.None,
      m_itemType = ItemDrop.ItemData.ItemType.AmmoNonEquipable
    };

    var itemConfig = new ItemConfig
    {
      Name = "$valheim_vehicles_cannonball_solid",
      Description = "$valheim_vehicles_cannonball_solid_desc",
      Icon = icon,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 1,
          Item = "BlackMetal"
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "Coal"
        }
      ],
      PieceTable = PrefabRegistryController.GetPieceTableName()
    };

    var customItem = new CustomItem(prefab, true, itemConfig);

    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.CannonballSolid}");
    }
  }

  private void RegisterCannonballExplosiveItemPrefab()
  {
    var prefab = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_ball_blackmetal");
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("cannon_ball_blackmetal");
    if (!prefab)
    {
      LoggerProvider.LogError("VehicleHammerPrefab not found!");
      return;
    }

    var cannonBall = prefab.AddComponent<Cannonball>();
    cannonBall.cannonballType = Cannonball.CannonballType.Explosive;

    CannonController.CannonballExplosivePrefab = cannonBall;

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();

    // verbosely add these.
    zSyncTransform.m_syncBodyVelocity = true;
    zSyncTransform.m_syncRotation = true;
    zSyncTransform.m_syncPosition = true;

    var itemDrop = prefab.AddComponent<ItemDrop>();
    if (itemDrop.m_nview == null)
    {
      itemDrop.m_nview = nv;
    }

    itemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData
    {
      m_name = PrefabNames.CannonballExplosive,
      m_icons = [icon],
      m_buildPieces = PrefabRegistryController.GetPieceTable(),
      m_toolTier = 0,
      m_equipDuration = 0,
      m_skillType = Skills.SkillType.None,
      m_itemType = ItemDrop.ItemData.ItemType.AmmoNonEquipable
    };

    var itemConfig = new ItemConfig
    {
      Name = "$valheim_vehicles_cannonball_explosive",
      Description = "$valheim_vehicles_cannonball_explosive_desc",
      Icon = icon,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 1,
          Item = "BlackMetal"
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "Coal"
        }
      ],
      PieceTable = PrefabRegistryController.GetPieceTableName()
    };

    var customItem = new CustomItem(prefab, true, itemConfig);

    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.CannonballExplosive}");
    }
  }

  private void RegisterPowderBarrelPrefab()
  {
    var asset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("powder_barrel");
    var sprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite("powder_barrel");

    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.PowderBarrel,
      asset);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.PowderBarrel, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_powder_barrel",
      Description = "$valheim_vehicles_powder_barrel_desc",
      Icon = sprite
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.PowderBarrel, prefab);

    var wearNTear = PrefabRegistryHelpers.SetWearNTear(prefab, 3);

    // very high health but it can immediately blow up on any damage to consume all health.
    wearNTear.m_health = 100f;

    // main toggle switch.
    var powderBarrel = prefab.AddComponent<PowderBarrel>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 20,
          Item = "Coal",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }


  public override void OnRegister()
  {
    RegisterCannonballExplosiveItemPrefab();
    RegisterCannonballSolidItemPrefab();

    RegisterCannonPrefab();
    RegisterPowderBarrelPrefab();
  }
}