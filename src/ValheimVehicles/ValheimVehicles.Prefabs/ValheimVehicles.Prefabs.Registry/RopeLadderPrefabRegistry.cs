using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class RopeLadderPrefabRegistry : RegisterPrefab<RopeLadderPrefabRegistry>
{
  public override void OnRegister()
  {
    RegisterRopeLadder();
  }
  private static void RegisterRopeLadder()
  {
    var mbRopeLadderPrefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.MBRopeLadder,
        LoadValheimRaftAssets.ropeLadder);

    var mbRopeLadderPrefabPiece = mbRopeLadderPrefab.AddComponent<Piece>();
    mbRopeLadderPrefabPiece.m_name = "$mb_rope_ladder";
    mbRopeLadderPrefabPiece.m_description = "$mb_rope_ladder_desc";
    mbRopeLadderPrefabPiece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    mbRopeLadderPrefabPiece.m_primaryTarget = false;
    mbRopeLadderPrefabPiece.m_randomTarget = false;

    PrefabRegistryHelpers.AddNetViewWithPersistence(mbRopeLadderPrefab);
    PrefabRegistryHelpers.FixSnapPoints(mbRopeLadderPrefab);

    var ropeLadder = mbRopeLadderPrefab.AddComponent<RopeLadderComponent>();
    var rope =
      LoadValheimAssets.raftMast.GetComponentInChildren<LineRenderer>(true);
    ropeLadder.m_ropeLine = ropeLadder.GetComponent<LineRenderer>();
    ropeLadder.m_ropeLine.material = new Material(rope.material);
    ropeLadder.m_ropeLine.textureMode = LineTextureMode.Tile;
    ropeLadder.m_ropeLine.widthMultiplier = 0.05f;
    ropeLadder.m_stepObject = ropeLadder.transform.Find("step").gameObject;

    var ladderMesh =
      ropeLadder.m_stepObject.GetComponentInChildren<MeshRenderer>();
    ladderMesh.material =
      new Material(LoadValheimAssets.woodFloorPiece
        .GetComponentInChildren<MeshRenderer>()
        .material);

    /*
     * previously ladder has 10k (10000f) health...way over powered
     *
     * m_support means ladders cannot have items attached to them.
     */
    var mbRopeLadderPrefabWearNTear =
      PrefabRegistryHelpers.SetWearNTear(mbRopeLadderPrefab);
    mbRopeLadderPrefabWearNTear.m_supports = false;

    PrefabRegistryHelpers.FixCollisionLayers(mbRopeLadderPrefab);
    PrefabRegistryController.AddPiece(new CustomPiece(mbRopeLadderPrefab, false,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Description = "$mb_rope_ladder_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .RopeLadder),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 10,
            Item = "Wood",
            Recover = true
          }
        ]
      }));
  }
}