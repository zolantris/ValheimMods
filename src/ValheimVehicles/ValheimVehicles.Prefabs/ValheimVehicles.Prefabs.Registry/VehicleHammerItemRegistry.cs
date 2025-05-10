using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Valheim.UI;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Prefabs.Registry;

public class VehicleHammerItemRegistry : GuardedRegistry<VehicleHammerItemRegistry>
{
  public class VehicleBuildHammer : MonoBehaviour, Interactable
  {
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
      LoggerProvider.LogMessage("Interact called for vehicle hammer");
      return false;
    }
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
      LoggerProvider.LogMessage("Called UseItem for vehicle hammer");
      return false;
    }
  }

  public static void RegisterVehicleHammer()
  {
    // var hammerPrefab = PrefabManager.Instance.GetPrefab("VehicleHammerPrefab");
    var hammerPrefab = LoadValheimVehicleAssets.VehicleHammer;
    if (!hammerPrefab)
    {
      LoggerProvider.LogError("VehicleHammerPrefab not found!");
      return;
    }

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(hammerPrefab);
    var zSyncTransform = hammerPrefab.AddComponent<ZSyncTransform>();

    // hammerPrefab.AddComponent<VehicleBuildHammer>();
    // hammerPrefab.AddComponent<VehicleHammerInputListener>();

    // verbosely add these.
    zSyncTransform.m_syncBodyVelocity = false;
    zSyncTransform.m_syncRotation = true;
    zSyncTransform.m_syncPosition = true;

    var itemDrop = hammerPrefab.AddComponent<ItemDrop>();
    if (itemDrop.m_nview == null)
    {
      itemDrop.m_nview = nv;
    }

    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.VehicleHammer);

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
      m_toolTier = 0,
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
    itemDrop.m_itemData.m_shared.m_attack.m_attackType = Attack.AttackType.Horizontal;
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

    var customItem = new CustomItem(hammerPrefab, true, itemConfig);

    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.VehicleHammer}");
    }

    LoggerProvider.LogMessage("Registered VehicleHammer with empty custom build menu.");
  }

  public override void OnRegister()
  {
    RegisterVehicleHammer();
  }
}