using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs.Registry;

public class RaftPrefab : IRegisterPrefab
{
  public static readonly RaftPrefab Instance = new();

  public GameObject GetTransformedRaft()
  {
    var raftPrefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.m_raft,
        LoadValheimAssets.vanillaRaftPrefab);

    raftPrefab.transform.Find("ship/visual/mast").gameObject.SetActive(false);
    raftPrefab.transform.Find("interactive/mast").gameObject.SetActive(false);
    raftPrefab.GetComponent<Rigidbody>().mass = 1000f;

    // WIP These destroy values may not apply
    Object.Destroy(raftPrefab.transform.Find("ship/colliders/log").gameObject);
    Object.Destroy(raftPrefab.transform.Find("ship/colliders/log (1)").gameObject);
    Object.Destroy(raftPrefab.transform.Find("ship/colliders/log (2)").gameObject);
    Object.Destroy(raftPrefab.transform.Find("ship/colliders/log (3)").gameObject);

    var piece = raftPrefab.GetComponent<Piece>();
    piece.m_name = "$mb_raft";
    piece.m_description = "$mb_raft_desc";

    var wnt = PrefabRegistryHelpers.GetWearNTearSafe(raftPrefab);

    wnt.m_health = Math.Max(100f, ValheimRaftPlugin.Instance.RaftHealth.Value);
    wnt.m_noRoofWear = false;

    var impact = raftPrefab.GetComponent<ImpactEffect>();
    impact.m_damageToSelf = false;

    PrefabRegistryController.AddToRaftPrefabPieces(piece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(raftPrefab);

    return raftPrefab;
  }

  // public GameObject RegisterUpgradedRaft()
  // {
  //   // var mbRaftPrefab = GetTransformedRaft();
  //   // var raftShip = mbRaftPrefab.GetComponent<Ship>();
  //   // Object.Destroy(raftShip);
  //   // var shipInstance = mbRaftPrefab.AddComponent<VehicleShip>();
  //   // mbRaftPrefab.AddComponent<ValheimShipControls>();
  //   //
  //   // var waterEffects = mbRaftPrefab.transform.Find("WaterEffects").gameObject;
  //   // var shipEffects = waterEffects.GetComponent<ShipEffects>();
  //   // var vehicleShipEffects = waterEffects.AddComponent<VehicleShipEffects>();
  //   // VehicleShipEffects.CloneShipEffectsToInstance(vehicleShipEffects, shipEffects);
  //   // Object.Destroy(shipEffects);
  //   //
  //   // vehicleShipEffects.transform.localPosition = new Vector3(0, -2, 0);
  //   // shipInstance.ShipEffectsObj = vehicleShipEffects.gameObject;
  //   // shipInstance.ShipEffects = vehicleShipEffects;
  //   // shipInstance.m_controlGuiPos = shipControlsGui.transform;
  //   //
  //   //
  //   // shipInstance.FloatColliderObj = floatColliderObj.gameObject;
  //   // shipInstance.FloatCollider = floatBoxCollider;
  //
  //   return mbRaftPrefab;
  // }
  // todo likely switch this to a command. This should not be part of release as it likely could cause regressions, better to have an upgrade command.
  public GameObject RegisterRaftV2Compat()
  {
    var raftPrefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.m_raft,
        LoadValheimAssets.vanillaRaftPrefab);

    // values to destroy in v2.0.0
    var shipControls = raftPrefab.GetComponentInChildren<ShipControlls>();
    var wearNTear = raftPrefab.GetComponent<WearNTear>();
    var ship = raftPrefab.GetComponent<Ship>();
    var zSyncTransform = raftPrefab.GetComponent<ZSyncTransform>();
    var rigidbody = raftPrefab.GetComponent<Rigidbody>();

    Object.Destroy(shipControls);
    Object.Destroy(wearNTear);
    Object.Destroy(ship);
    Object.Destroy(zSyncTransform);
    Object.Destroy(rigidbody);

    var prefab = WaterVehiclePrefab.CreateWaterVehiclePrefab(raftPrefab);


    var piece = prefab.AddComponent<Piece>();
    piece.m_waterPiece = true;
    piece.m_icon = LoadValheimAssets.vanillaRaftPrefab.GetComponent<Piece>().m_icon;
    piece.m_name = "$mb_raft";
    piece.m_description = "$mb_raft_desc";

    PrefabRegistryController.AddToRaftPrefabPieces(piece);

    return prefab;
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var raftPrefab = GetTransformedRaft();
    // GameObject raftPrefab;
    // raftPrefab = ValheimRaftPlugin.Instance.AutoUpgradeV1Raft.Value
    //   ? RegisterRaftV2Compat()
    //   : GetTransformedRaft();

    pieceManager.AddPiece(new CustomPiece(raftPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_raft_desc",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 20,
          Item = "Wood"
        }
      ]
    }));
  }
}