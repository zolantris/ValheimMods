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
    RegisterCustomSail(prefabManager, pieceManager);
    RegisterCustomSailCreator(prefabManager, pieceManager, 3);
    RegisterCustomSailCreator(prefabManager, pieceManager, 4);
  }


  private void RegisterVikingMast(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var vikingShipMast = LoadValheimRaftAssets.vikingShipPrefab.transform.Find("ship/visual/Mast")
      .gameObject;

    var vikingShipMastPrefab =
      prefabManager.CreateClonedPrefab(PrefabNames.Tier3RaftMastName, vikingShipMast);
    var vikingShipMastPrefabPiece = vikingShipMastPrefab.AddComponent<Piece>();

    // The connector is off by a bit, translating downwards should help but it doesn't work for the vikingmast
    vikingShipMastPrefabPiece.transform.localScale = new Vector3(2f, 2f, 2f);
    vikingShipMastPrefab.transform.localPosition = new Vector3(0, -1, 0);
    vikingShipMastPrefabPiece.m_name = "$mb_vikingship_mast";
    vikingShipMastPrefabPiece.m_description = "$mb_vikingship_mast_desc";
    vikingShipMastPrefabPiece.m_placeEffect = LoadValheimRaftAssets.woodFloorPiece.m_placeEffect;
    PrefabRegistryController.AddToRaftPrefabPieces(vikingShipMastPrefabPiece);

    PrefabRegistryHelpers.AddNetViewWithPersistence(vikingShipMastPrefab);

    var vikingShipMastComponent = vikingShipMastPrefab.AddComponent<MastComponent>();
    vikingShipMastComponent.m_sailObject = vikingShipMastPrefab.transform.Find("Sail").gameObject;

    // PrefabRegistryHelpers.AddBoundsToAllChildren(vikingShipMastPrefab, vikingShipMastPrefab);


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
      Description = GetTieredSailAreaText(3),
      Icon = LoadValheimRaftAssets.sprites.GetSprite("vikingmast"),
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
          Item = "WolfPelt",
          Recover = true
        }
      }
    }));
  }


  private void RegisterCustomSail(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var mbSailPrefab = prefabManager.CreateEmptyPrefab(PrefabNames.Tier1CustomSailName);
    Object.Destroy(mbSailPrefab.GetComponent<BoxCollider>());
    Object.Destroy(mbSailPrefab.GetComponent<MeshFilter>());

    var mbSailPrefabPiece = mbSailPrefab.AddComponent<Piece>();
    mbSailPrefabPiece.m_name = "$mb_sail";
    mbSailPrefabPiece.m_description = "$mb_sail_desc";
    mbSailPrefabPiece.m_placeEffect = LoadValheimRaftAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryController.AddToRaftPrefabPieces(mbSailPrefabPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(mbSailPrefab);

    var sailObject = new GameObject("Sail")
    {
      transform =
      {
        parent = mbSailPrefab.transform
      },
      layer = LayerMask.NameToLayer("piece_nonsolid")
    };

    var sail = mbSailPrefab.AddComponent<SailComponent>();
    sail.m_sailObject = sailObject;
    sail.m_sailCloth = sailObject.AddComponent<Cloth>();
    sail.m_meshCollider = sailObject.AddComponent<MeshCollider>();
    sail.m_mesh = sailObject.GetComponent<SkinnedMeshRenderer>();
    sail.m_mesh.shadowCastingMode = ShadowCastingMode.TwoSided;
    sail.m_mesh.sharedMaterial = LoadValheimRaftAssets.sailMat;

    // this is a tier 1 sail
    PrefabRegistryHelpers.SetWearNTear(mbSailPrefab, 1);

    // mast should allowSailShrinking
    var mast = mbSailPrefab.AddComponent<MastComponent>();
    mast.m_sailObject = sailObject;
    mast.m_sailCloth = sail.m_sailCloth;
    mast.m_allowSailRotation = false;
    mast.m_allowSailShrinking = true;

    mbSailPrefab.layer = LayerMask.NameToLayer("piece_nonsolid");
    SailCreatorComponent.m_sailPrefab = mbSailPrefab;
    PrefabManager.Instance.AddPrefab(mbSailPrefab);
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
    piece.m_placeEffect = LoadValheimRaftAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryController.AddToRaftPrefabPieces(piece);

    var sailCreatorComponent = prefab.AddComponent<SailCreatorComponent>();
    sailCreatorComponent.m_sailSize = sailCount;

    var mesh = prefab.GetComponent<MeshRenderer>();
    mesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    var sailIcon = sailCount == 3
      ? LoadValheimRaftAssets.sprites.GetSprite("customsail_tri")
      : LoadValheimRaftAssets.sprites.GetSprite("customsail");

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
      _ => ""
    };

    return description;
  }

  private void RegisterRaftMast(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var mbRaftMastPrefab =
      prefabManager.CreateClonedPrefab(PrefabNames.Tier1RaftMastName,
        LoadValheimRaftAssets.raftMast);

    var mbRaftMastPrefabPiece = mbRaftMastPrefab.AddComponent<Piece>();
    mbRaftMastPrefabPiece.m_name = "$mb_raft_mast";
    mbRaftMastPrefabPiece.m_description = "$mb_raft_mast_desc";
    mbRaftMastPrefabPiece.m_placeEffect = LoadValheimRaftAssets.woodFloorPiece.m_placeEffect;
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
      Icon = LoadValheimRaftAssets.sprites.GetSprite("raftmast"),
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[2]
      {
        new()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        },
        new()
        {
          Amount = 6,
          Item = "DeerHide",
          Recover = true
        }
      }
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
    mbKarveMastPiece.m_placeEffect = LoadValheimRaftAssets.woodFloorPiece.m_placeEffect;

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
      Icon = LoadValheimRaftAssets.sprites.GetSprite("karvemast"),
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