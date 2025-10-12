using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class RopeAnchorPrefabRegistry : RegisterPrefab<RopeAnchorPrefabRegistry>
{
  public override void OnRegister()
  {
    RegisterRopeAnchor();
  }

  private static void RegisterRopeAnchor()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.MBRopeAnchor,
        LoadValheimRaftAssets.anchor_rope);

    var mbRopeAnchorPrefabPiece = prefab.AddComponent<Piece>();
    mbRopeAnchorPrefabPiece.m_name = "$mb_rope_anchor";
    mbRopeAnchorPrefabPiece.m_description = "$mb_rope_anchor_desc";
    mbRopeAnchorPrefabPiece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var ropeAnchorComponent = prefab.AddComponent<RopeAnchorComponent>();
    var baseRope =
      LoadValheimAssets.raftMast.GetComponentInChildren<LineRenderer>(true);

    ropeAnchorComponent.m_rope = prefab.AddComponent<LineRenderer>();
    ropeAnchorComponent.m_rope.material = new Material(baseRope.material);
    ropeAnchorComponent.m_rope.widthMultiplier = 0.05f;
    ropeAnchorComponent.m_rope.enabled = false;

    var ropeAnchorComponentWearNTear =
      PrefabRegistryHelpers.SetWearNTear(prefab, 3);
    ropeAnchorComponentWearNTear.m_supports = false;

    PrefabRegistryHelpers.FixCollisionLayers(prefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);

    /*
     * @todo ropeAnchor recipe may need to be tweaked to require flax or some fiber
     * Maybe a weaker rope could be made as a lower tier with much lower health
     */
    PrefabRegistryController.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Description = "$mb_rope_anchor_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("rope_anchor"),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 1,
          Item = "Iron",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "IronNails",
          Recover = true
        }
      ]
    }));
  }
}