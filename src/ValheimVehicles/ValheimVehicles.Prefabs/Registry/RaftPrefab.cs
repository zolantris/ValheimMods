using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs.Registry;

public class RaftPrefab : IRegisterPrefab
{
  public static readonly RaftPrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var mbRaftPrefab =
      prefabManager.CreateClonedPrefab("MBRaft", LoadValheimRaftAssets.vanillaRaftPrefab);

    mbRaftPrefab.transform.Find("ship/visual/mast").gameObject.SetActive(false);
    mbRaftPrefab.transform.Find("interactive/mast").gameObject.SetActive(false);
    mbRaftPrefab.GetComponent<Rigidbody>().mass = 1000f;

    // WIP These destroy values may not apply
    Object.Destroy(mbRaftPrefab.transform.Find("ship/colliders/log").gameObject);
    Object.Destroy(mbRaftPrefab.transform.Find("ship/colliders/log (1)").gameObject);
    Object.Destroy(mbRaftPrefab.transform.Find("ship/colliders/log (2)").gameObject);
    Object.Destroy(mbRaftPrefab.transform.Find("ship/colliders/log (3)").gameObject);

    var mbRaftPrefabPiece = mbRaftPrefab.GetComponent<Piece>();
    mbRaftPrefabPiece.m_name = "$mb_raft";
    mbRaftPrefabPiece.m_description = "$mb_raft_desc";

    PrefabRegistryController.AddToRaftPrefabPieces(mbRaftPrefabPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(mbRaftPrefab);
    var wnt = PrefabRegistryHelpers.GetWearNTearSafe(mbRaftPrefab);

    wnt.m_health = Math.Max(100f, ValheimRaftPlugin.Instance.RaftHealth.Value);
    wnt.m_noRoofWear = false;

    var impact = mbRaftPrefab.GetComponent<ImpactEffect>();
    impact.m_damageToSelf = false;
    pieceManager.AddPiece(new CustomPiece(mbRaftPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_raft_desc",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 20,
          Item = "Wood"
        }
      }
    }));
  }
}