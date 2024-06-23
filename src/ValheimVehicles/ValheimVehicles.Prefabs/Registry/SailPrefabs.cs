using System.Linq;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimRAFT;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class SailPrefabs : IRegisterPrefab
{
  public static readonly SailPrefabs Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterRaftMast(prefabManager, pieceManager);
    RegisterKarveMast(prefabManager, pieceManager);
    RegisterVikingMast(prefabManager, pieceManager);
    RegisterDrakkalMast(prefabManager, pieceManager);
    RegisterCustomSail(prefabManager, pieceManager);
    RegisterCustomSailCreator(prefabManager, pieceManager, 3);
    RegisterCustomSailCreator(prefabManager, pieceManager, 4);
  }

  public static bool IsSail(string objName) => !objName.StartsWith(PrefabNames.Tier1RaftMastName) &&
                                               !objName.StartsWith(PrefabNames
                                                 .Tier1CustomSailName) &&
                                               !objName.StartsWith(PrefabNames.Tier2RaftMastName) &&
                                               !objName.StartsWith(PrefabNames.Tier3RaftMastName) &&
                                               !objName.StartsWith(PrefabNames.Tier4RaftMastName);

  private void RegisterVikingMast(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var vikingShipMast = LoadValheimAssets.vikingShipPrefab.transform.Find("ship/visual/Mast")
      .gameObject;

    var vikingShipMastPrefab =
      prefabManager.CreateClonedPrefab(PrefabNames.Tier3RaftMastName, vikingShipMast);
    var vikingShipMastPrefabPiece = vikingShipMastPrefab.AddComponent<Piece>();

    // The connector is off by a bit, translating downwards should help but it doesn't work for the vikingmast
    vikingShipMastPrefabPiece.transform.localScale = new Vector3(2f, 2f, 2f);
    vikingShipMastPrefab.transform.localPosition = new Vector3(0, -1, 0);
    vikingShipMastPrefabPiece.m_name = "$mb_vikingship_mast";
    vikingShipMastPrefabPiece.m_description = GetTieredSailAreaText(3);
    vikingShipMastPrefabPiece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;
    PrefabRegistryController.AddToRaftPrefabPieces(vikingShipMastPrefabPiece);

    PrefabRegistryHelpers.AddNetViewWithPersistence(vikingShipMastPrefab);

    var vikingShipMastComponent = vikingShipMastPrefab.AddComponent<MastComponent>();
    vikingShipMastComponent.m_sailObject = vikingShipMastPrefab.transform.Find("Sail").gameObject;

    vikingShipMastComponent.m_sailCloth =
      vikingShipMastComponent.m_sailObject.GetComponentInChildren<Cloth>();
    vikingShipMastComponent.m_allowSailRotation = true;
    vikingShipMastComponent.m_allowSailShrinking = true;

    // shipCollider
    PrefabRegistryHelpers.AddSnapPoint("$hud_snappoint_bottom", vikingShipMastPrefab);

    // Set wear and tear can be abstracted
    PrefabRegistryHelpers.SetWearNTear(vikingShipMastPrefab, 3);

    PrefabRegistryHelpers.FixRopes(vikingShipMastPrefab);
    PrefabRegistryHelpers.FixCollisionLayers(vikingShipMastPrefab);

    pieceManager.AddPiece(new CustomPiece(vikingShipMastPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.VikingMast),
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "WolfPelt",
          Recover = true
        }
      ]
    }));
  }

  private void RegisterDrakkalMast(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var drakkalMast = LoadValheimAssets.drakkarPrefab.transform.Find("ship/visual/Mast")
      .gameObject;

    var prefab =
      prefabManager.CreateClonedPrefab(PrefabNames.Tier4RaftMastName, drakkalMast);
    var vikingShipMastPrefabPiece = prefab.AddComponent<Piece>();

    vikingShipMastPrefabPiece.transform.localScale = new Vector3(1, 1, 1);
    vikingShipMastPrefabPiece.m_name = "$valheim_vehicles_drakkalship_mast";
    vikingShipMastPrefabPiece.m_description = GetTieredSailAreaText(4);
    vikingShipMastPrefabPiece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;
    PrefabRegistryController.AddToRaftPrefabPieces(vikingShipMastPrefabPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var mastComponent = prefab.AddComponent<MastComponent>();
    var clothObj = prefab.GetComponentsInChildren<Cloth>()[0];
    mastComponent.m_sailObject = clothObj.transform.parent.gameObject;

    mastComponent.m_sailCloth = clothObj;
    mastComponent.m_allowSailRotation = true;
    mastComponent.m_allowSailShrinking = true;

    // shipCollider
    PrefabRegistryHelpers.AddSnapPoint("$hud_snappoint_bottom", prefab);

    // Set wear and tear can be abstracted
    PrefabRegistryHelpers.SetWearNTear(prefab, 3);

    PrefabRegistryHelpers.FixRopes(prefab);
    PrefabRegistryHelpers.FixCollisionLayers(prefab);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.VikingMast),
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 10,
          Item = "YggdrasilWood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 20,
          Item = "LinenThread",
          Recover = true
        }
      ]
    }));
  }


  private void RegisterCustomSail(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var prefab = prefabManager.CreateClonedPrefab(PrefabNames.Tier1CustomSailName,
      LoadValheimVehicleAssets.CustomSail);

    var mbSailPrefabPiece = prefab.AddComponent<Piece>();
    mbSailPrefabPiece.m_name = "$mb_sail";
    mbSailPrefabPiece.m_description = "$mb_sail_desc";
    mbSailPrefabPiece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryController.AddToRaftPrefabPieces(mbSailPrefabPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var sail = prefab.AddComponent<SailComponent>();

    // this is a tier 1 sail
    PrefabRegistryHelpers.SetWearNTear(prefab, 1);
    PrefabRegistryHelpers.FixSnapPoints(prefab);

    // mast should allowSailShrinking
    var mast = prefab.AddComponent<MastComponent>();
    mast.m_sailObject = prefab;
    mast.m_sailCloth = sail.m_sailCloth;
    mast.m_allowSailRotation = false;
    mast.m_allowSailShrinking = true;

    PrefabManager.Instance.AddPrefab(prefab);
    SailCreatorComponent.sailPrefab =
      PrefabManager.Instance.GetPrefab(PrefabNames.Tier1CustomSailName);
  }

  /**
   * this allows for registering N types of sails. Maybe in the future there will be more vertices supported
   */
  public void RegisterCustomSailCreator(PrefabManager prefabManager, PieceManager pieceManager,
    int sailCount)
  {
    if (sailCount is not (3 or 4))
    {
      Logger.LogError($"Attempted to register a sail that is not of type 3 or 4. Got {sailCount}");
      return;
    }

    var prefab = prefabManager.CreateEmptyPrefab($"MBSailCreator_{sailCount}", false);
    prefab.layer = LayerMask.NameToLayer("piece_nonsolid");

    var piece = prefab.AddComponent<Piece>();
    var pieceName = sailCount == 3 ? "$mb_sail" : $"$mb_sail_{sailCount}";
    piece.m_name = pieceName;
    piece.m_description = $"$mb_sail_{sailCount}_desc";
    piece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryController.AddToRaftPrefabPieces(piece);

    var sailCreatorComponent = prefab.AddComponent<SailCreatorComponent>();
    sailCreatorComponent.m_sailSize = sailCount;

    var mesh = prefab.GetComponent<MeshRenderer>();
    var unlitColor = LoadValheimVehicleAssets.PieceShader;
    var material = new Material(unlitColor)
    {
      color = Color.green
    };
    mesh.sharedMaterial = material;
    mesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    var sailIcon = sailCount == 3
      ? LoadValheimVehicleAssets.VehicleSprites.GetSprite("customsail_tri")
      : LoadValheimVehicleAssets.VehicleSprites.GetSprite("customsail");

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = $"$mb_sail_{sailCount}_desc",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Icon = sailIcon
    }));
  }

  public static string GetTieredSailAreaText(int tier)
  {
    string description = tier switch
    {
      1 =>
        $"$mb_raft_mast_desc\n$mb_raft_mast_generic_wind_desc [<color=yellow><b>{ValheimRaftPlugin.Instance.SailTier1Area.Value}</b></color>]",
      2 =>
        $"$mb_karve_mast_desc\n$mb_raft_mast_generic_wind_desc [<color=yellow><b>{ValheimRaftPlugin.Instance.SailTier2Area.Value}</b></color>]",
      3 =>
        $"$mb_vikingship_mast_desc\n$mb_raft_mast_generic_wind_desc [<color=yellow><b>{ValheimRaftPlugin.Instance.SailTier3Area.Value}</b></color>]",
      4 =>
        $"$valheim_vehicles_drakkalship_mast_desc\n$mb_raft_mast_generic_wind_desc [<color=yellow><b>{ValheimRaftPlugin.Instance.SailTier4Area.Value}</b></color>]",
      _ => ""
    };

    return description;
  }

  private void RegisterRaftMast(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var mbRaftMastPrefab =
      prefabManager.CreateClonedPrefab(PrefabNames.Tier1RaftMastName,
        LoadValheimAssets.raftMast);

    var mbRaftMastPrefabPiece = mbRaftMastPrefab.AddComponent<Piece>();
    mbRaftMastPrefabPiece.m_name = "$mb_raft_mast";
    mbRaftMastPrefabPiece.m_description = "$mb_raft_mast_desc";
    mbRaftMastPrefabPiece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;
    // PrefabRegistryHelpers.AddBoundsToAllChildren(mbRaftMastPrefab, mbRaftMastPrefab);

    PrefabRegistryController.AddToRaftPrefabPieces(mbRaftMastPrefabPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(mbRaftMastPrefab);

    var mastComponent = mbRaftMastPrefab.AddComponent<MastComponent>();
    mastComponent.m_allowSailRotation = true;
    mastComponent.m_allowSailShrinking = true;
    mastComponent.m_sailObject = mbRaftMastPrefab.transform.Find("Sail").gameObject;
    mastComponent.m_sailCloth = mastComponent.m_sailObject.GetComponentInChildren<Cloth>();

    PrefabRegistryHelpers.SetWearNTear(mbRaftMastPrefab);

    PrefabRegistryHelpers.AddSnapPoint("$hud_snappoint_bottom", mbRaftMastPrefab);

    PrefabRegistryHelpers.FixRopes(mbRaftMastPrefab);
    PrefabRegistryHelpers.FixCollisionLayers(mbRaftMastPrefab);
    pieceManager.AddPiece(new CustomPiece(mbRaftMastPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description =
        GetTieredSailAreaText(1),
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("raftmast"),
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "DeerHide",
          Recover = true
        }
      ]
    }));
  }

  public void RegisterKarveMast(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var karve = prefabManager.GetPrefab("Karve");
    var karveMast = karve.transform.Find("ship/mast").gameObject;
    var mbKarveMastPrefab =
      prefabManager.CreateClonedPrefab(PrefabNames.Tier2RaftMastName, karveMast);

    var mbKarveMastPiece = mbKarveMastPrefab.AddComponent<Piece>();
    mbKarveMastPiece.m_name = "$mb_karve_mast";
    mbKarveMastPiece.m_description = "$mb_karve_mast_desc";
    mbKarveMastPiece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;

    // PrefabRegistryHelpers.AddBoundsToAllChildren(mbKarveMastPrefab, mbKarveMastPrefab);
    PrefabRegistryController.AddToRaftPrefabPieces(mbKarveMastPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(mbKarveMastPrefab);

    // tweak the mast
    var mast = mbKarveMastPrefab.AddComponent<MastComponent>();
    mast.m_sailObject =
      mbKarveMastPrefab.transform.Find("Sail").gameObject;
    mast.m_sailCloth = mast.m_sailObject.GetComponentInChildren<Cloth>();
    mast.m_allowSailShrinking = true;
    mast.m_allowSailRotation = true;

    // Abstract wnt for masts
    PrefabRegistryHelpers.SetWearNTear(mbKarveMastPrefab, 2);

    PrefabRegistryHelpers.AddSnapPoint("$hud_snappoint_bottom", mbKarveMastPrefab);

    PrefabRegistryHelpers.FixRopes(mbKarveMastPrefab);
    PrefabRegistryHelpers.FixCollisionLayers(mbKarveMastPrefab);

    pieceManager.AddPiece(new CustomPiece(mbKarveMastPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = GetTieredSailAreaText(2),
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("karvemast"),
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[3]
      {
        new()
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new()
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        },
        new()
        {
          Amount = 6,
          Item = "TrollHide",
          Recover = true
        }
      }
    }));
  }
}