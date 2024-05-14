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
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.MBRaft,
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

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var raftPrefab = GetTransformedRaft();

    pieceManager.AddPiece(new CustomPiece(raftPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_raft_desc",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 20,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }
}