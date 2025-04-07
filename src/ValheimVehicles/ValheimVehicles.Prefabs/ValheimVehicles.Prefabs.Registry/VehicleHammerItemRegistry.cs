using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Prefabs.Registry;

public class VehicleHammerItemRegistry : GuardedRegistry<VehicleHammerTableRegistry>
{
  public class VehicleHammer : MonoBehaviour
  {
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
    var itemDrop = hammerPrefab.AddComponent<ItemDrop>();
    if (itemDrop.m_nview == null)
    {
      itemDrop.m_nview = nv;
    }

    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.VehicleHammer);

    itemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData
    {
      m_name = "$valheim_vehicles_hammer_name",
      // Required setup
      m_useDurability = true,
      m_durabilityDrain = 0f,
      m_durabilityPerLevel = 200f,


      m_icons = [icon],
      // m_animationState = ItemDrop.ItemData.AnimationState.Torch,

      m_buildPieces = PrefabRegistryController.GetPieceTable(),
      m_toolTier = 1,
      m_itemType = ItemDrop.ItemData.ItemType.None
      // m_attack =
      // {
      //   m_attackAnimation = "swing_pickaxe", // avoid weapon behavior
      //   m_attackType = Attack.AttackType.None
      // }
    };

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

    // var originalHammer = ItemManager.Instance.GetItem("Hammer");
    //
    // if (originalHammer != null)
    // {
    //   LoggerProvider.LogDebug("Found original hammer");
    // }

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
