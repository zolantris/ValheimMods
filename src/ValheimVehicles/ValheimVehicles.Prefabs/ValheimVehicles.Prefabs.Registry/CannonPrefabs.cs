using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Controllers;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.ValheimVehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class CannonPrefabs : RegisterPrefab<CannonPrefabs>
{

  public static GameObject CannonballSolidProjectile;
  public static GameObject CannonballExplosiveProjectile;

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
    cannonController.cannonFiringMode = CannonFiringMode.Manual;
    cannonController.canRotateFiringRangeY = false;

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      Enabled = CannonPrefabConfig.EnableCannons.Value,
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
      Requirements = PrefabRecipeConfig.GetRequirements(prefab.name)
    }));
  }

  // not ready for prod.
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
    cannonController.cannonFiringMode = CannonFiringMode.Auto;
    cannonController.canRotateFiringRangeY = true;

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      Enabled = CannonPrefabConfig.EnableCannons.Value,
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
      Requirements = PrefabRecipeConfig.GetRequirements(prefab.name)
    }));
  }

  private void RegisterCannonballSolidProjectilePrefab()
  {
    var prefabAsset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_ball_bronze");
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.CannonballSolidProjectile, prefabAsset);

    if (!prefab)
    {
      LoggerProvider.LogError("cannon_ball_bronze projectile not found!");
      return;
    }

    var nv = PrefabRegistryHelpers.AddTempNetView(prefab, true);
    nv.m_distant = true;

    var cannonBall = prefab.AddComponent<Cannonball>();
    cannonBall.cannonballVariant = CannonballVariant.Solid;

    CannonController.CannonballSolidPrefab = prefab;
    CannonballSolidProjectile = prefab;
  }

  private void RegisterCannonballExplosiveProjectilePrefab()
  {
    var prefabAsset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_ball_blackmetal");
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.CannonballExplosiveProjectile, prefabAsset);
    if (!prefab)
    {
      LoggerProvider.LogError("VehicleHammerPrefab not found!");
      return;
    }

    var nv = PrefabRegistryHelpers.AddTempNetView(prefab, true);
    nv.m_distant = true;

    var cannonBall = prefab.AddComponent<Cannonball>();
    cannonBall.cannonballVariant = CannonballVariant.Explosive;

    CannonController.CannonballExplosivePrefab = prefab;
    CannonballExplosiveProjectile = prefab;
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

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
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
      m_maxStackSize = 200,
      m_weight = CannonPrefabConfig.CannonBallInventoryWeight.Value, // this could be 12-24lbs...but that would make the game less fun
      m_skillType = Skills.SkillType.None,
      m_ammoType = PrefabItemNameToken.CannonAmmoType,
      m_itemType = ItemDrop.ItemData.ItemType.Ammo
    };

    var itemConfig = new ItemConfig
    {
      Name = PrefabItemNameToken.CannonSolidAmmo,
      Description = $"{PrefabItemNameToken.CannonSolidAmmo}_desc",
      Icon = icon,
      StackSize = 200,
      CraftingStation = "forge",
      RepairStation = "forge",
      PieceTable = PrefabRegistryController.GetPieceTableName()
    };

    var customItem = new CustomItem(prefab, true, itemConfig);
    var success = ItemManager.Instance.AddItem(customItem);

    var customRecipe = new CustomRecipe(new RecipeConfig
    {
      Enabled = CannonPrefabConfig.EnableCannons.Value,
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

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();
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
      m_name = PrefabNames.CannonballExplosive,
      m_icons = [icon],
      m_buildPieces = PrefabRegistryController.GetPieceTable(),
      m_toolTier = 50,
      m_maxStackSize = 200,
      m_weight = CannonPrefabConfig.CannonBallInventoryWeight.Value, // this could be 12-24lbs...but that would make the game less fun
      m_skillType = Skills.SkillType.Bows,
      m_ammoType = PrefabItemNameToken.CannonAmmoType,
      m_itemType = ItemDrop.ItemData.ItemType.Ammo
    };

    var itemConfig = new ItemConfig
    {
      Name = PrefabItemNameToken.CannonExplosiveAmmo,
      Description = $"{PrefabItemNameToken.CannonExplosiveAmmo}_desc",
      Icon = icon,
      StackSize = 200,
      CraftingStation = "forge",
      RepairStation = "forge",
      Enabled = CannonPrefabConfig.EnableCannons.Value,
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

  private void RegisterCannonHandHeldPrefab()
  {
    var asset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>("cannon_handheld");
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("cannon_handheld");
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.CannonHandHeldItem, asset);

    if (!prefab)
    {
      LoggerProvider.LogError($"cannon_turret not found!");
      return;
    }

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    // always need rb + zsync transform when dropping.
    var rb = prefab.AddComponent<Rigidbody>();
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();

    var cannonControllerTransform = prefab.transform.Find("attach/cannon_handheld");
    var cannonController = cannonControllerTransform.gameObject.AddComponent<CannonController>();
    var ammoController = cannonControllerTransform.gameObject.AddComponent<AmmoController>();
    ammoController.IsHandheld = true;
    var handCannon = cannonControllerTransform.gameObject.AddComponent<CannonHandHeldController>();

    cannonController.cannonFiringMode = CannonFiringMode.Manual;
    cannonController.cannonVariant = CannonVariant.HandHeld; // should auto do this, but this makes it a bit safer.
    cannonController.canRotateFiringRangeY = false;

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
      m_name = PrefabNames.CannonHandHeldItem,
      m_maxQuality = 5,
      m_weight = 5,
      m_useDurability = true,
      m_useDurabilityDrain = 1f,
      m_durabilityDrain = 1f,
      m_durabilityPerLevel = 200f,
      m_maxDurability = 300f,
      m_icons = [icon],
      m_toolTier = 50,
      m_ammoType = PrefabItemNameToken.CannonAmmoType,
      m_animationState = ItemDrop.ItemData.AnimationState.Crossbow,
      m_equipDuration = 0,
      m_skillType = Skills.SkillType.Crossbows,
      m_itemType = ItemDrop.ItemData.ItemType.TwoHandedWeapon
    };

    if (itemDrop.m_itemData.m_shared.m_attack == null)
    {
      itemDrop.m_itemData.m_shared.m_attack = new Attack();
    }

    var attack = itemDrop.m_itemData.m_shared.m_attack;

    // needs to be cross-bow or something custom.
    attack.m_attackAnimation = "crossbow_fire";
    attack.m_attackType = Attack.AttackType.TriggerProjectile;
    attack.m_attackStamina = 5;
    attack.m_attackHealth = 0;
    attack.m_hitTerrain = true;
    attack.m_requiresReload = true;
    attack.m_reloadTime = CannonPrefabConfig.CannonHandHeld_ReloadTime.Value;
    attack.m_reloadAnimation = "reload_crossbow";
    attack.m_speedFactor = 0.2f;
    attack.m_reloadStaminaDrain = 10f;
    attack.m_hitTerrain = true;
    attack.m_speedFactorRotation = 0.2f; // default value. can remove.

    var itemConfig = new ItemConfig
    {
      Name = PrefabItemNameToken.CannonHandHeldName,
      Description = $"{PrefabItemNameToken.CannonHandHeldName}_desc",
      Icon = icon,
      CraftingStation = "forge",
      RepairStation = "forge",
      Enabled = CannonPrefabConfig.EnableCannons.Value,
      Requirements = PrefabRecipeConfig.GetRequirements(prefab.name)
    };

    var customItem = new CustomItem(prefab, true, itemConfig);
    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.VehicleHammer}");
    }

    LoggerProvider.LogMessage("Registered HandCannon");
  }

  private void RegisterTelescopeItemPrefab()
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
    var rb = prefab.AddComponent<Rigidbody>();
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();

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
      m_name = PrefabNames.TelescopeItem,
      m_maxQuality = 5,
      m_useDurability = true,
      m_useDurabilityDrain = 1f,
      m_durabilityDrain = 0f,
      m_durabilityPerLevel = 200f,
      m_maxDurability = 100f,
      m_icons = [icon],
      m_toolTier = 50,
      m_animationState = ItemDrop.ItemData.AnimationState.Crossbow,
      m_equipDuration = 0,
      m_skillType = Skills.SkillType.Bows,
      m_itemType = ItemDrop.ItemData.ItemType.TwoHandedWeapon
    };

    if (itemDrop.m_itemData.m_shared.m_attack == null)
    {
      itemDrop.m_itemData.m_shared.m_attack = new Attack();
    }

    itemDrop.m_itemData.m_shared.m_attack.m_attackAnimation = "swing_sword";
    itemDrop.m_itemData.m_shared.m_attack.m_attackType = Attack.AttackType.TriggerProjectile;
    itemDrop.m_itemData.m_shared.m_attack.m_attackStamina = 5;
    itemDrop.m_itemData.m_shared.m_attack.m_hitTerrain = true;

    var itemConfig = new ItemConfig
    {
      Name = PrefabItemNameToken.TelescopeName,
      Description = $"{PrefabItemNameToken.TelescopeName}_desc",
      Icon = icon,
      Icons = [icon],
      CraftingStation = "forge",
      RepairStation = "forge",
      Enabled = CannonPrefabConfig.EnableCannons.Value,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 3,
          Item = "Wood"
        }
      ]
    };

    var customItem = new CustomItem(prefab, true, itemConfig);

    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.TelescopeItem}");
    }

    LoggerProvider.LogMessage("Registered Telescope");
  }

  private void RegisterCannonAreaControllerTelescopePrefab()
  {
    var prefabAssetName = "cannon_control_center";
    var prefabAsset = LoadValheimVehicleAssets._bundle.LoadAsset<GameObject>(prefabAssetName);
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(prefabAssetName);
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.CannonControlCenter, prefabAsset);
    if (!prefab)
    {
      LoggerProvider.LogError($"{prefabAsset} not found!");
      return;
    }

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.CannonControlCenter, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_cannon_control_center",
      Description = "$valheim_vehicles_cannon_control_center_desc",
      Icon = icon
    });
    var piece = PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.CannonControlCenter, prefab);
    piece.m_primaryTarget = true;
    var wearNTear = PrefabRegistryHelpers.SetWearNTear(prefab, 1);

    // very high enough health to survive weaker hits. Otherwise brittle (if this breaks all defense break.)
    wearNTear.m_health = 50f;

    // main toggle switch.
    var targetController = prefab.AddComponent<TargetController>();
    targetController.cannonDetectionMode = TargetController.CannonDetectionMode.Cast;

    prefab.AddComponent<TargetControlsInteractive>();
    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
      Enabled = CannonPrefabConfig.EnableCannons.Value,
      Requirements = PrefabRecipeConfig.GetRequirements(prefab.name)
    }));
  }


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
    wearNTear.m_health = 250f;

    // main toggle switch.
    var powderBarrel = prefab.AddComponent<PowderBarrel>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
      Enabled = CannonPrefabConfig.EnableCannons.Value,
      Requirements = PrefabRecipeConfig.GetRequirements(prefab.name)
    }));
  }

  public static void OnEnabledChange()
  {
    UpdatePieceEnabledState(PrefabNames.CannonFixedTier1, CannonPrefabConfig.EnableCannons.Value);
    UpdatePieceEnabledState(PrefabNames.CannonTurretTier1, CannonPrefabConfig.EnableCannons.Value);
    UpdatePieceEnabledState(PrefabNames.PowderBarrel, CannonPrefabConfig.EnableCannons.Value);

    UpdateRecipeEnabledState(PrefabNames.CannonballSolid, CannonPrefabConfig.EnableCannons.Value);
    UpdateRecipeEnabledState(PrefabNames.CannonballExplosive, CannonPrefabConfig.EnableCannons.Value);
    UpdateRecipeEnabledState(PrefabNames.CannonHandHeldItem, CannonPrefabConfig.EnableCannons.Value);
  }

  private static void UpdatePieceEnabledState(string prefabName, bool isEnabled)
  {
    var piece = PieceManager.Instance.GetPiece(prefabName);
    if (piece?.Piece != null)
    {
      piece.Piece.m_enabled = isEnabled;
      LoggerProvider.LogMessage($"Updated piece {prefabName} enabled: {isEnabled}");
    }
  }

  private static void UpdateRecipeEnabledState(string recipeName, bool isEnabled)
  {
    var recipe = ItemManager.Instance.GetRecipe(recipeName);
    if (recipe?.Recipe != null)
    {
      recipe.Recipe.m_enabled = isEnabled;
      LoggerProvider.LogMessage($"Updated recipe {recipeName} enabled: {isEnabled}");
    }
  }


  public override void OnRegister()
  {
    // persistent inventory items
    RegisterCannonballSolidItemPrefab();
    RegisterCannonballExplosiveItemPrefab();

#if DEBUG
    // not ready for prod.
    RegisterTelescopeItemPrefab();
#endif
    // projectiles are not persistent.
    RegisterCannonballSolidProjectilePrefab();
    RegisterCannonballExplosiveProjectilePrefab();

    RegisterCannonFixedPrefab();

#if DEBUG
    // not ready for prod.
    RegisterCannonTurretPrefab();
#endif

    RegisterPowderBarrelPrefab();
    RegisterCannonAreaControllerTelescopePrefab();

    RegisterCannonHandHeldPrefab();
  }
}