using System;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.LayerUtils;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class WaterVehiclePrefab : IRegisterPrefab
{
  public static readonly WaterVehiclePrefab Instance = new();

  public static GameObject CreateClonedPrefab(string prefabName)
  {
    return PrefabManager.Instance.CreateClonedPrefab(prefabName,
      LoadValheimVehicleAssets.VehicleShipAsset);
  }

  /**
   * todo it's possible this all needs to be done in the Awake method to safely load valheim.
   * Should test this in development build of valheim
   */
  private static GameObject CreateWaterVehiclePrefab(
    GameObject prefab)
  {
    // top level netview must be passed along to other components from VehicleShip
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab, true);
    PrefabRegistryHelpers.GetOrAddMovementZSyncTransform(prefab);

    // colliders already have a rigidbody on them from unity prefab
    var vehicleMovementColliders =
      VehicleShip.GetVehicleMovementCollidersObj(prefab.transform);

    var floatColliderObj =
      vehicleMovementColliders.transform.Find(
        PrefabNames.WaterVehicleFloatCollider);
    var blockingColliderObj =
      vehicleMovementColliders.transform.Find(PrefabNames
        .WaterVehicleBlockingCollider);
    var onboardColliderObj =
      vehicleMovementColliders.transform.Find(PrefabNames
        .WaterVehicleOnboardCollider);

    onboardColliderObj.name = PrefabNames.WaterVehicleOnboardCollider;
    floatColliderObj.name = PrefabNames.WaterVehicleFloatCollider;
    blockingColliderObj.name = PrefabNames.WaterVehicleBlockingCollider;

    /*
     * ShipControls were a gameObject with a script attached to them. This approach directly attaches the script instead of having the rudder show.
     */

    var shipInstance = prefab.AddComponent<VehicleShip>();
    var shipControls = prefab.AddComponent<VehicleMovementController>();
    shipInstance.ColliderParentObj = vehicleMovementColliders.gameObject;
    shipControls.ShipDirection =
      floatColliderObj.FindDeepChild(PrefabNames
        .VehicleShipMovementOrientation);
    shipInstance.MovementController = shipControls;
    shipInstance.gameObject.layer = LayerHelpers.CustomRaftLayer;

    // todo fix ship water effects so they do not cause ship materials to break

    var waterEffects =
      Object.Instantiate(LoadValheimAssets.shipWaterEffects, prefab.transform);
    waterEffects.name = PrefabNames.VehicleShipEffects;
    var shipEffects = waterEffects.GetComponent<ShipEffects>();
    var vehicleShipEffects = waterEffects.AddComponent<VehicleShipEffects>();
    VehicleShipEffects.CloneShipEffectsToInstance(vehicleShipEffects,
      shipEffects);
    Object.Destroy(shipEffects);

    vehicleShipEffects.transform.localPosition = new Vector3(0, -2, 0);
    shipInstance.ShipEffectsObj = vehicleShipEffects.gameObject;
    shipInstance.ShipEffects = vehicleShipEffects;

    // WearNTear may need to be removed or tweaked
    prefab.AddComponent<WearNTear>();
    var woodWnt = LoadValheimAssets.woodFloorPiece.GetComponent<WearNTear>();
    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 1, true);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt,
      WearNTear.MaterialType.HardWood);

    wnt.m_onDestroyed += woodWnt.m_onDestroyed;
    // triggerPrivateArea will damage enemies/pieces when within it
    wnt.m_triggerPrivateArea = true;

    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.enabled = false;

    // todo ImpactEffect likely never should have been added like this
    // todo remove if unnecessary
    var impactEffect = prefab.AddComponent<ImpactEffect>();
    impactEffect.m_triggerMask = LayerMask.GetMask("Default", "character",
      "piece", "terrain",
      "static_solid", "Default_small", "character_net", "vehicle",
      LayerMask.LayerToName(29));
    impactEffect.m_toolTier = 1000;

    impactEffect.m_damages.m_blunt = 50;
    impactEffect.m_interval = 0.5f;
    impactEffect.m_damagePlayers = true;
    impactEffect.m_damageToSelf = false;
    impactEffect.m_damageFish = true;
    impactEffect.m_hitType = HitData.HitType.Boat;
    impactEffect.m_minVelocity = 0.1f;
    impactEffect.m_maxVelocity = 7;

    return prefab;
  }

  private static void RegisterWaterVehicleShipPrefab()
  {
    var prefab = CreateClonedPrefab(PrefabNames.WaterVehicleShip);
    var waterVehiclePrefab = CreateWaterVehiclePrefab(prefab);

    var piece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.WaterVehicleShip,
        prefab);
    piece.m_primaryTarget = true;
    piece.m_randomTarget = true;
    piece.m_targetNonPlayerBuilt = true;
    piece.m_waterPiece = true;
    piece.m_noClipping = true;
    piece.m_canRotate = true;

    PieceManager.Instance.AddPiece(new CustomPiece(waterVehiclePrefab, true,
      new PieceConfig
      {
        PieceTable = "Hammer",
        Category = PrefabNames.ValheimRaftMenuName,
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 16,
            Item = "Wood",
            Recover = false,
          }
        ]
      }));
  }

  private static void RegisterNautilusVehicleShipPrefab()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.Nautilus,
      LoadValheimVehicleAssets.ShipNautilus);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var piece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Nautilus, prefab);
    // var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 3);
    // wnt.m_health = 7000f;
    // wnt.m_burnable = false;
    // wnt.m_supports = true;
    // wnt.m_damages = new HitData.DamageModifiers()
    // {
    //   m_slash = HitData.DamageModifier.Resistant,
    //   m_blunt = HitData.DamageModifier.Resistant,
    //   m_pierce = HitData.DamageModifier.VeryWeak,
    //   m_lightning = HitData.DamageModifier.VeryWeak,
    //   m_poison = HitData.DamageModifier.Immune,
    //   m_frost = HitData.DamageModifier.Immune,
    //   m_fire = HitData.DamageModifier.Resistant,
    // };

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 200,
          Item = "Bronze",
          Recover = true
        }
      ]
    }));
  }

  public void OnExperimentalPrefabSettingsChange(object sender,
    EventArgs eventArgs)
  {
    if (PrefabManager.Instance == null || PieceManager.Instance == null) return;
    RegisterNautilus();
  }

  public void RegisterNautilus()
  {
    var shouldEnable =
      ValheimRaftPlugin.Instance.AllowExperimentalPrefabs.Value;

    var nautilus = PrefabManager.Instance?.GetPrefab(PrefabNames.Nautilus);

    if (shouldEnable && !nautilus)
    {
      RegisterNautilusVehicleShipPrefab();
    }

    if (!shouldEnable && nautilus)
    {
      var piece = PieceManager.Instance?.GetPiece(PrefabNames.Nautilus);
      if (piece != null)
      {
        piece.Piece.enabled = false;
      }
    }
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterWaterVehicleShipPrefab();
    RegisterNautilus();
  }
}