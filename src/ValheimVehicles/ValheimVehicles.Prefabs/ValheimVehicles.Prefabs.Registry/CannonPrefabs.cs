using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class CannonPrefabs : RegisterPrefab<CannonPrefabs>
{

  private void RegisterCannonFixedPrefab()
  {
    var asset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_fixed");
    var sprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite("cannon_fixed");

    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.CannonFixedTier1,
      asset);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.CannonFixedTier1, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_cannon_fixed_tier1",
      Description = "$valheim_vehicles_cannon_fixed_tier1_desc",
      Icon = sprite
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.CannonFixedTier1, prefab);
    var wearNTear = PrefabRegistryHelpers.SetWearNTear(prefab, 1);
    // main toggle switch.
    var cannonController = prefab.AddComponent<CannonControllerBridge>();
    cannonController.firingMode = CannonController.FiringMode.Manual;
    cannonController.canRotateFiringRangeY = false;

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
      Enabled = true,
      Requirements = PrefabRecipeConfig.GetRequirements(prefab.name)
    }));
  }

  private void RegisterCannonTurretPrefab()
  {
    var asset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_turret");
    var sprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite("cannon_turret");

    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.CannonTurretTier1,
      asset);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.CannonTurretTier1, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_cannon_turret_tier1",
      Description = "$valheim_vehicles_cannon_turret_tier1_desc",
      Icon = sprite
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.CannonTurretTier1, prefab);
    var wearNTear = PrefabRegistryHelpers.SetWearNTear(prefab, 1);

    // main toggle switch.
    var cannonController = prefab.AddComponent<CannonControllerBridge>();
    cannonController.firingMode = CannonController.FiringMode.Auto;
    cannonController.canRotateFiringRangeY = true;

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
      Enabled = true,
      Requirements = PrefabRecipeConfig.GetRequirements(prefab.name)
    }));
  }

  private void RegisterCannonballSolidItemPrefab()
  {
    var prefabAsset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_ball_bronze");
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.CannonballSolid, prefabAsset);
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("cannon_ball_bronze");
    if (!prefab)
    {
      LoggerProvider.LogError("cannon_ball_bronze not found!");
      return;
    }

    var cannonBall = prefab.AddComponent<Cannonball>();
    cannonBall.cannonballType = Cannonball.CannonballType.Solid;

    CannonController.CannonballSolidPrefab = cannonBall;

    var nv = PrefabRegistryHelpers.AddTempNetView(prefab);
    nv.m_distant = true;
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();

    // must have zsync transform in order to sync the projectile.
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
      m_toolTier = 50,
      m_weight = PrefabConfig.CannonBallInventoryWeight.Value, // this could be 12-24lbs...but that would make the game less fun
      m_skillType = Skills.SkillType.None,
      m_itemType = ItemDrop.ItemData.ItemType.AmmoNonEquipable
    };

    var itemConfig = new ItemConfig
    {
      Name = "$valheim_vehicles_cannonball_solid",
      Description = "$valheim_vehicles_cannonball_solid_desc",
      Icon = icon,
      StackSize = 200,
      PieceTable = PrefabRegistryController.GetPieceTableName()
    };

    var customItem = new CustomItem(prefab, true, itemConfig);
    var success = ItemManager.Instance.AddItem(customItem);

    var customRecipe = new CustomRecipe(new RecipeConfig
    {
      Name = "Recipe_CannonballSolid", // Unique name, can be arbitrary
      Item = PrefabNames.CannonballSolid, // The prefab name you registered above
      Amount = 10, // Number of items crafted per craft
      CraftingStation = "forge", // Or your desired station,
      Requirements = PrefabRecipeConfig.GetRequirements(PrefabNames.CannonballSolid)
    });
    ItemManager.Instance.AddRecipe(customRecipe);

    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.CannonballSolid}");
    }
  }

  private void RegisterCannonballExplosiveItemPrefab()
  {
    var prefabAsset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_ball_blackmetal");
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("cannon_ball_blackmetal");
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.CannonballExplosive, prefabAsset);
    if (!prefab)
    {
      LoggerProvider.LogError("VehicleHammerPrefab not found!");
      return;
    }

    var cannonBall = prefab.AddComponent<Cannonball>();
    cannonBall.cannonballType = Cannonball.CannonballType.Explosive;

    CannonController.CannonballExplosivePrefab = cannonBall;

    var nv = PrefabRegistryHelpers.AddTempNetView(prefab);
    nv.m_distant = true;
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();

    // must have zsync transform in order to sync the projectile.
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
      m_toolTier = 50,
      m_maxStackSize = 200,
      m_skillType = Skills.SkillType.None,
      m_itemType = ItemDrop.ItemData.ItemType.AmmoNonEquipable
    };

    var itemConfig = new ItemConfig
    {
      Name = "$valheim_vehicles_cannonball_explosive",
      Description = "$valheim_vehicles_cannonball_explosive_desc",
      Icon = icon,
      CraftingStation = "forge",
      Requirements = PrefabRecipeConfig.GetRequirements(prefab.name),
      PieceTable = PrefabRegistryController.GetPieceTableName()
    };

    var customItem = new CustomItem(prefab, true, itemConfig);

    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.CannonballExplosive}");
    }
  }

#if DEBUG
  private void RegisterHandCannonPrefab()
  {
    var prefabAssetName = "cannon_turret_tier1";
    var prefabAsset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>(prefabAssetName);
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(prefabAssetName);
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.CannonballExplosive, prefabAsset);

    if (!prefab)
    {
      LoggerProvider.LogError($"{prefabAssetName} not found!");
      return;
    }

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();

    // hammerPrefab.AddComponent<VehicleBuildHammer>();
    // hammerPrefab.AddComponent<VehicleHammerInputListener>();

    // verbosely add these.
    zSyncTransform.m_syncBodyVelocity = false;
    zSyncTransform.m_syncRotation = true;
    zSyncTransform.m_syncPosition = true;

    var itemDrop = prefab.AddComponent<ItemDrop>();
    if (itemDrop.m_nview == null)
    {
      itemDrop.m_nview = nv;
    }

    itemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData
    {
      m_name = PrefabNames.VehicleHammer,
      m_maxQuality = 5,
      m_useDurability = true,
      m_useDurabilityDrain = 1f,
      m_durabilityDrain = 0f,
      m_durabilityPerLevel = 200f,
      m_maxDurability = 100f,
      m_icons = [icon],
      m_buildPieces = PrefabRegistryController.GetPieceTable(),
      m_toolTier = 50,
      m_animationState = ItemDrop.ItemData.AnimationState.OneHanded,
      // must be 0 to override default of 1. 1 adds an equip delay automatically
      m_equipDuration = 0,
      // See In HammerItemElement.IsHammer (valheim.dll) regarding hammer logic matchers
      // requires
      // - m_skillType =  Skills.SkillType.Swords
      // - m_itemType = ItemDrop.ItemData.ItemType.Tool
      // - prefab name must container "hammer" lower case.
      m_skillType = Skills.SkillType.Swords,
      m_itemType = ItemDrop.ItemData.ItemType.Tool
    };

    if (itemDrop.m_itemData.m_shared.m_attack == null)
    {
      itemDrop.m_itemData.m_shared.m_attack = new Attack();
    }

    itemDrop.m_itemData.m_shared.m_attack.m_attackAnimation = "swing_hammer";
    itemDrop.m_itemData.m_shared.m_attack.m_attackType = Attack.AttackType.TriggerProjectile;
    itemDrop.m_itemData.m_shared.m_attack.m_attackStamina = 5;
    itemDrop.m_itemData.m_shared.m_attack.m_hitTerrain = true;

    // secondary attacks are invalid as valheim uses Player To destroy the piece and do a raycast when pressing center button.
    // if (itemDrop.m_itemData.m_shared.m_secondaryAttack == null)
    // {
    //   itemDrop.m_itemData.m_shared.m_secondaryAttack = new Attack();
    // }
    // itemDrop.m_itemData.m_shared.m_secondaryAttack.m_attackAnimation = "";
    // itemDrop.m_itemData.m_shared.m_secondaryAttack.m_attackType = Attack.AttackType.Horizontal;
    // itemDrop.m_itemData.m_shared.m_secondaryAttack.m_attackAnimation = "swing_hammer";
    // itemDrop.m_itemData.m_shared.m_secondaryAttack.m_attackStamina = 20;
    // itemDrop.m_itemData.m_shared.m_attack.m_speedFactor = 0.2f; // default value. can remove 
    // itemDrop.m_itemData.m_shared.m_attack.m_speedFactorRotation = 0.2f; // default value. can remove.

    var itemConfig = new ItemConfig
    {
      Name = "$valheim_vehicles_hammer_name",
      Description = "$valheim_vehicles_hammer_description",
      Icon = icon,
      RepairStation = "piece_workbench",
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 3,
          Item = "Wood"
        }
      ],
      PieceTable = PrefabRegistryController.GetPieceTableName()
    };

    var customItem = new CustomItem(prefab, true, itemConfig);
    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.VehicleHammer}");
    }

    LoggerProvider.LogMessage("Registered HandCannon");
  }

  private void RegisterTelescopePrefab()
  {
    var prefabAssetName = "telescope";
    var prefabAsset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>(prefabAssetName);
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(prefabAssetName);
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.TelescopeItem, prefabAsset);
    if (!prefab)
    {
      LoggerProvider.LogError($"{prefabAsset} not found!");
      return;
    }

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();

    // hammerPrefab.AddComponent<VehicleBuildHammer>();
    // hammerPrefab.AddComponent<VehicleHammerInputListener>();

    // verbosely add these.
    zSyncTransform.m_syncRotation = true;
    zSyncTransform.m_syncPosition = true;

    var itemDrop = prefab.AddComponent<ItemDrop>();
    if (itemDrop.m_nview == null)
    {
      itemDrop.m_nview = nv;
    }


    itemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData
    {
      m_name = PrefabNames.VehicleHammer,
      m_maxQuality = 5,
      m_useDurability = true,
      m_useDurabilityDrain = 1f,
      m_durabilityDrain = 0f,
      m_durabilityPerLevel = 200f,
      m_maxDurability = 100f,
      m_icons = [icon],
      m_buildPieces = PrefabRegistryController.GetPieceTable(),
      m_toolTier = 50,
      m_animationState = ItemDrop.ItemData.AnimationState.OneHanded,
      m_equipDuration = 0,
      m_skillType = Skills.SkillType.Swords,
      m_itemType = ItemDrop.ItemData.ItemType.Tool
    };

    if (itemDrop.m_itemData.m_shared.m_attack == null)
    {
      itemDrop.m_itemData.m_shared.m_attack = new Attack();
    }

    itemDrop.m_itemData.m_shared.m_attack.m_attackAnimation = "swing_hammer";
    itemDrop.m_itemData.m_shared.m_attack.m_attackType = Attack.AttackType.TriggerProjectile;
    itemDrop.m_itemData.m_shared.m_attack.m_attackStamina = 5;
    itemDrop.m_itemData.m_shared.m_attack.m_hitTerrain = true;

    var itemConfig = new ItemConfig
    {
      Name = "$valheim_vehicles_hammer_name",
      Description = "$valheim_vehicles_hammer_description",
      Icon = icon,
      RepairStation = "piece_workbench",
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 3,
          Item = "Wood"
        }
      ],
      PieceTable = PrefabRegistryController.GetPieceTableName()
    };

    var customItem = new CustomItem(prefab, true, itemConfig);

    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.TelescopeItem}");
    }

    LoggerProvider.LogMessage("Registered Telescope");
  }
#endif

  private void RegisterPowderBarrelPrefab()
  {
    var asset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("powder_barrel");
    var sprite = LoadValheimVehicleAssets.VehicleSprites.GetSprite("powder_barrel");

    if (!asset)
    {
      LoggerProvider.LogError("powder_barrel not found!");
      return;
    }

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

    var wearNTear = PrefabRegistryHelpers.SetWearNTear(prefab, 1);

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

    RegisterCannonFixedPrefab();
    RegisterCannonTurretPrefab();
    RegisterPowderBarrelPrefab();

    RegisterTelescopePrefab();
    RegisterHandCannonPrefab();
  }
}