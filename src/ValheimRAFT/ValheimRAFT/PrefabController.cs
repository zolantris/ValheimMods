using System;
using System.Collections.Generic;
using System.Text;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class PrefabController : MonoBehaviour
{
  private PrefabManager prefabManager;
  private PieceManager pieceManager;
  private SpriteAtlas sprites;
  private GameObject boarding_ramp;
  private GameObject mbBoardingRamp;
  private GameObject steering_wheel;
  private GameObject rope_ladder;
  private GameObject rope_anchor;
  private GameObject raftMast;
  private GameObject dirtFloor;
  private Material sailMat;
  private Piece woodFloorPiece;
  private WearNTear woodFloorPieceWearNTear;
  private SynchronizationManager synchronizationManager;
  private List<Piece> raftPrefabPieces = new();
  private bool prefabsEnabled = true;

  private const string ValheimRaftMenuName = "Raft";


  // todo this should come from config
  private float wearNTearBaseHealth = 250f;

  private void UpdatePrefabs(bool isPrefabEnabled)
  {
    foreach (var piece in raftPrefabPieces)
    {
      var pmPiece = pieceManager.GetPiece(piece.name);
      if (pmPiece == null)
      {
        Logger.LogWarning(
          $"ValheimRaft attempted to run UpdatePrefab on {piece.name} but jotunn pieceManager did not find that piece name");
        continue;
      }

      Logger.LogDebug($"Setting m_enabled: to {isPrefabEnabled}, for name {piece.name}");
      pmPiece.Piece.m_enabled = isPrefabEnabled;
    }

    prefabsEnabled = isPrefabEnabled;
  }

  public void UpdatePrefabStatus()
  {
    if (!ValheimRaftPlugin.Instance.AdminsCanOnlyBuildRaft.Value && prefabsEnabled)
    {
      return;
    }

    Logger.LogDebug(
      $"ValheimRAFT: UpdatePrefabStatusCalled with AdminsCanOnlyBuildRaft set as {ValheimRaftPlugin.Instance.AdminsCanOnlyBuildRaft.Value}, updating prefabs and player access");
    var isAdmin = SynchronizationManager.Instance.PlayerIsAdmin;
    UpdatePrefabs(isAdmin);
  }

  public void UpdatePrefabStatus(object obj, ConfigurationSynchronizationEventArgs e)
  {
    UpdatePrefabStatus();
  }

  private void AddToRaftPrefabPieces(Piece raftPiece)
  {
    raftPrefabPieces.Add(raftPiece);
  }

  private static void AddNetViewWithPersistence(GameObject prefab)
  {
    var netView = prefab.GetComponent<ZNetView>();
    if (!netView)
    {
      netView = prefab.AddComponent<ZNetView>();
    }

    if (!netView)
    {
      Logger.LogError("Unable to register NetView, ValheimRAFT could be broken without netview");
      return;
    }

    netView.m_persistent = true;
  }

  public void RegisterAllPrefabs()
  {
    // Critical Items
    RegisterRaftPrefab();
    RegisterRudder();

    // Masts
    RegisterRaftMast();
    RegisterKarveMast();
    RegisterVikingMast();
    RegisterCustomSail();

    // Sail creators
    RegisterCustomSailCreator(3);
    RegisterCustomSailCreator(4);

    // Rope items
    RegisterRopeAnchor();
    RegisterRopeLadder();

    // pier components
    RegisterPierPole();
    RegisterPierWall();

    // Ramps
    RegisterBoardingRamp();
    RegisterBoardingRampWide();
    // Floors
    RegisterDirtFloor(1);
    RegisterDirtFloor(2);
  }

  public void Init()
  {
    boarding_ramp =
      ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/boarding_ramp.prefab");
    steering_wheel =
      ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/steering_wheel.prefab");
    rope_ladder =
      ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/rope_ladder.prefab");
    rope_anchor =
      ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/rope_anchor.prefab");
    sprites =
      ValheimRaftPlugin.m_assetBundle.LoadAsset<SpriteAtlas>("Assets/icons.spriteatlas");
    sailMat = ValheimRaftPlugin.m_assetBundle.LoadAsset<Material>("Assets/SailMat.mat");
    dirtFloor = ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");

    prefabManager = PrefabManager.Instance;
    pieceManager = PieceManager.Instance;

    var woodFloorPrefab = prefabManager.GetPrefab("wood_floor");
    woodFloorPiece = woodFloorPrefab.GetComponent<Piece>();
    woodFloorPieceWearNTear = woodFloorPiece.GetComponent<WearNTear>();

    // registers all prefabs
    RegisterAllPrefabs();

    /*
     * listens for admin status updates and changes prefab active status
     */
    SynchronizationManager.OnConfigurationSynchronized += UpdatePrefabStatus;
    SynchronizationManager.OnAdminStatusChanged += UpdatePrefabStatus;
    UpdatePrefabStatus();
  }

  private WearNTear SetWearNTear(GameObject prefabComponent, int tierMultiplier = 1)
  {
    var wearNTearComponent = prefabComponent.GetComponent<WearNTear>();
    if (!wearNTearComponent)
    {
      // Many components do not have WearNTear so they must be added to the prefabPiece
      wearNTearComponent = prefabComponent.AddComponent<WearNTear>();
      if (!wearNTearComponent)
        Logger.LogError($"error setting WearNTear for RAFT prefab {prefabComponent.name}");

      return wearNTearComponent;
    }

    wearNTearComponent.m_health = wearNTearBaseHealth * tierMultiplier;
    wearNTearComponent.m_noRoofWear = false;
    wearNTearComponent.m_destroyedEffect = woodFloorPieceWearNTear.m_destroyedEffect;
    wearNTearComponent.m_hitEffect = woodFloorPieceWearNTear.m_hitEffect;

    return wearNTearComponent;
  }


  private void RegisterVikingMast()
  {
    var vikingShipPrefab = prefabManager.GetPrefab("VikingShip");
    var vikingShipMast = vikingShipPrefab.transform.Find("ship/visual/Mast").gameObject;

    var vikingShipMastPrefab = prefabManager.CreateClonedPrefab("MBVikingShipMast", vikingShipMast);
    var vikingShipMastPrefabPiece = vikingShipMastPrefab.AddComponent<Piece>();
    // The connector is off by a bit, translating downwards should help but it doesn't work for the vikingmast
    vikingShipMastPrefabPiece.transform.localScale = new Vector3(2f, 2f, 2f);
    vikingShipMastPrefabPiece.m_name = "$mb_vikingship_mast";
    vikingShipMastPrefabPiece.m_description = "$mb_vikingship_mast_desc";
    vikingShipMastPrefabPiece.m_placeEffect = woodFloorPiece.m_placeEffect;
    AddToRaftPrefabPieces(vikingShipMastPrefabPiece);

    AddNetViewWithPersistence(vikingShipMastPrefab);
    SetSailConfig(vikingShipPrefab);
    // Set wear and tear can be abstracted
    SetWearNTear(vikingShipMastPrefab, 3);

    FixedRopes(vikingShipMastPrefab);
    FixCollisionLayers(vikingShipMastPrefab);

    pieceManager.AddPiece(new CustomPiece(vikingShipMastPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_vikingship_mast_desc",
      Icon = sprites.GetSprite("vikingmast"),
      Category = ValheimRaftMenuName,
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

  private void RegisterRaftPrefab()
  {
    var raft = prefabManager.GetPrefab("Raft");
    raftMast = raft.transform.Find("ship/visual/mast").gameObject;
    var mbRaftPrefab = prefabManager.CreateClonedPrefab("MBRaft", raft);

    mbRaftPrefab.transform.Find("ship/visual/mast").gameObject.SetActive(false);
    mbRaftPrefab.transform.Find("interactive/mast").gameObject.SetActive(false);
    mbRaftPrefab.GetComponent<Rigidbody>().mass = 1000f;

    Destroy(mbRaftPrefab.transform.Find("ship/colliders/log").gameObject);
    Destroy(mbRaftPrefab.transform.Find("ship/colliders/log (1)").gameObject);
    Destroy(mbRaftPrefab.transform.Find("ship/colliders/log (2)").gameObject);
    Destroy(mbRaftPrefab.transform.Find("ship/colliders/log (3)").gameObject);

    var mbRaftPrefabPiece = mbRaftPrefab.GetComponent<Piece>();
    mbRaftPrefabPiece.m_name = "$mb_raft";
    mbRaftPrefabPiece.m_description = "$mb_raft_desc";
    AddToRaftPrefabPieces(mbRaftPrefabPiece);

    AddNetViewWithPersistence(mbRaftPrefab);

    var mbRaftPrefabWearNTear = mbRaftPrefab.GetComponent<WearNTear>();

    mbRaftPrefabWearNTear.m_health = Math.Max(100f, ValheimRaftPlugin.Instance.RaftHealth.Value);
    mbRaftPrefabWearNTear.m_noRoofWear = false;


    var impact = mbRaftPrefab.GetComponent<ImpactEffect>();
    impact.m_damageToSelf = false;
    pieceManager.AddPiece(new CustomPiece(mbRaftPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_raft_desc",
      Category = ValheimRaftMenuName,
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

  private void RegisterRaftMast()
  {
    var mbRaftMastPrefab = prefabManager.CreateClonedPrefab("MBRaftMast", raftMast);

    var mbRaftMastPrefabPiece = mbRaftMastPrefab.AddComponent<Piece>();
    mbRaftMastPrefabPiece.m_name = "$mb_raft_mast";
    mbRaftMastPrefabPiece.m_description = "$mb_raft_mast_desc";
    mbRaftMastPrefabPiece.m_placeEffect = woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbRaftMastPrefabPiece);
    AddNetViewWithPersistence(mbRaftMastPrefab);

    SetSailConfig(mbRaftMastPrefab);
    SetWearNTear(mbRaftMastPrefab);

    FixedRopes(mbRaftMastPrefab);
    FixCollisionLayers(mbRaftMastPrefab);
    pieceManager.AddPiece(new CustomPiece(mbRaftMastPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_raft_mast_desc",
      Icon = sprites.GetSprite("raftmast"),
      Category = ValheimRaftMenuName,
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

  void SetSailConfig(GameObject prefab)
  {
    var mast = prefab.AddComponent<MastComponent>();
    mast.m_sailObject =
      prefab.transform.Find("Sail").gameObject;
    mast.m_sailCloth = mast.m_sailObject.GetComponentInChildren<Cloth>();
    mast.m_allowSailShrinking = true;
    mast.m_allowSailRotation = true;
  }

  public void RegisterKarveMast()
  {
    var karve = prefabManager.GetPrefab("Karve");
    var karveMast = karve.transform.Find("ship/mast").gameObject;
    var mbKarveMastPrefab = prefabManager.CreateClonedPrefab("MBKarveMast", karveMast);

    var mbKarveMastPiece = mbKarveMastPrefab.AddComponent<Piece>();
    mbKarveMastPiece.m_name = "$mb_karve_mast";
    mbKarveMastPiece.m_description = "$mb_karve_mast_desc";
    mbKarveMastPiece.m_placeEffect = woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbKarveMastPiece);
    AddNetViewWithPersistence(mbKarveMastPrefab);

    // tweak the mast
    SetSailConfig(mbKarveMastPrefab);

    // Abstract wnt for masts
    SetWearNTear(mbKarveMastPrefab, 2);

    FixedRopes(mbKarveMastPrefab);
    FixCollisionLayers(mbKarveMastPrefab);

    pieceManager.AddPiece(new CustomPiece(mbKarveMastPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_karve_mast_desc",
      Icon = sprites.GetSprite("karvemast"),
      Category = ValheimRaftMenuName,
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

  private void RegisterRudder()
  {
    var mbRudderPrefab = prefabManager.CreateClonedPrefab("MBRudder", steering_wheel);

    var mbRudderPrefabPiece = mbRudderPrefab.AddComponent<Piece>();
    mbRudderPrefabPiece.m_name = "$mb_rudder";
    mbRudderPrefabPiece.m_description = "$mb_rudder_desc";
    mbRudderPrefabPiece.m_placeEffect = woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbRudderPrefabPiece);
    AddNetViewWithPersistence(mbRudderPrefab);

    var rudder = mbRudderPrefab.AddComponent<RudderComponent>();
    rudder.m_controls = mbRudderPrefab.AddComponent<ShipControlls>();
    rudder.m_controls.m_hoverText = "$mb_rudder_use";
    rudder.m_controls.m_attachPoint = mbRudderPrefab.transform.Find("attachpoint");
    rudder.m_controls.m_attachAnimation = "Standing Torch Idle right";
    rudder.m_controls.m_detachOffset = new Vector3(0f, 0f, 0f);
    rudder.m_wheel = mbRudderPrefab.transform.Find("controls/wheel");
    rudder.UpdateSpokes();

    SetWearNTear(mbRudderPrefab);

    FixSnapPoints(mbRudderPrefab);
    FixCollisionLayers(mbRudderPrefab);
    pieceManager.AddPiece(new CustomPiece(mbRudderPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_rudder_desc",
      Icon = sprites.GetSprite("steering_wheel"),
      Category = ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      }
    }));
  }

  private void RegisterRopeLadder()
  {
    var mbRopeLadderPrefab = prefabManager.CreateClonedPrefab("MBRopeLadder", rope_ladder);

    var mbRopeLadderPrefabPiece = mbRopeLadderPrefab.AddComponent<Piece>();
    mbRopeLadderPrefabPiece.m_name = "$mb_rope_ladder";
    mbRopeLadderPrefabPiece.m_description = "$mb_rope_ladder_desc";
    mbRopeLadderPrefabPiece.m_placeEffect = woodFloorPiece.m_placeEffect;
    mbRopeLadderPrefabPiece.m_primaryTarget = false;
    mbRopeLadderPrefabPiece.m_randomTarget = false;

    AddToRaftPrefabPieces(mbRopeLadderPrefabPiece);
    AddNetViewWithPersistence(mbRopeLadderPrefab);

    var ropeLadder = mbRopeLadderPrefab.AddComponent<RopeLadderComponent>();
    var rope = raftMast.GetComponentInChildren<LineRenderer>(true);
    ropeLadder.m_ropeLine = ropeLadder.GetComponent<LineRenderer>();
    ropeLadder.m_ropeLine.material = new Material(rope.material);
    ropeLadder.m_ropeLine.textureMode = LineTextureMode.Tile;
    ropeLadder.m_ropeLine.widthMultiplier = 0.05f;
    ropeLadder.m_stepObject = ropeLadder.transform.Find("step").gameObject;

    var ladderMesh = ropeLadder.m_stepObject.GetComponentInChildren<MeshRenderer>();
    ladderMesh.material =
      new Material(woodFloorPiece.GetComponentInChildren<MeshRenderer>().material);

    /*
     * previously ladder has 10k (10000f) health...way over powered
     *
     * m_support means ladders cannot have items attached to them.
     */
    var mbRopeLadderPrefabWearNTear = SetWearNTear(mbRopeLadderPrefab);
    mbRopeLadderPrefabWearNTear.m_supports = false;

    FixCollisionLayers(mbRopeLadderPrefab);
    pieceManager.AddPiece(new CustomPiece(mbRopeLadderPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_rope_ladder_desc",
      Icon = sprites.GetSprite("rope_ladder"),
      Category = ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true,
        }
      }
    }));
  }

  private void RegisterRopeAnchor()
  {
    var mbRopeAnchorPrefab = prefabManager.CreateClonedPrefab("MBRopeAnchor", rope_anchor);

    var mbRopeAnchorPrefabPiece = mbRopeAnchorPrefab.AddComponent<Piece>();
    mbRopeAnchorPrefabPiece.m_name = "$mb_rope_anchor";
    mbRopeAnchorPrefabPiece.m_description = "$mb_rope_anchor_desc";
    mbRopeAnchorPrefabPiece.m_placeEffect = woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbRopeAnchorPrefabPiece);
    AddNetViewWithPersistence(mbRopeAnchorPrefab);

    var ropeAnchorComponent = mbRopeAnchorPrefab.AddComponent<RopeAnchorComponent>();
    var baseRope = raftMast.GetComponentInChildren<LineRenderer>(true);

    ropeAnchorComponent.m_rope = mbRopeAnchorPrefab.AddComponent<LineRenderer>();
    ropeAnchorComponent.m_rope.material = new Material(baseRope.material);
    ropeAnchorComponent.m_rope.widthMultiplier = 0.05f;
    ropeAnchorComponent.m_rope.enabled = false;

    var ropeAnchorComponentWearNTear = SetWearNTear(mbRopeAnchorPrefab, 3);
    ropeAnchorComponentWearNTear.m_supports = false;

    FixCollisionLayers(mbRopeAnchorPrefab);

    /*
     * @todo ropeAnchor recipe may need to be tweaked to require flax or some fiber
     * Maybe a weaker rope could be made as a lower tier with much lower health
     */
    pieceManager.AddPiece(new CustomPiece(mbRopeAnchorPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_rope_anchor_desc",
      Icon = sprites.GetSprite("rope_anchor"),
      Category = ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[2]
      {
        new()
        {
          Amount = 1,
          Item = "Iron",
          Recover = true
        },
        new()
        {
          Amount = 4,
          Item = "IronNails",
          Recover = true
        }
      }
    }));
  }

  private void RegisterCustomSail()
  {
    var mbSailPrefab = prefabManager.CreateEmptyPrefab("MBSail");
    Destroy(mbSailPrefab.GetComponent<BoxCollider>());
    Destroy(mbSailPrefab.GetComponent<MeshFilter>());

    var mbSailPrefabPiece = mbSailPrefab.AddComponent<Piece>();
    mbSailPrefabPiece.m_name = "$mb_sail";
    mbSailPrefabPiece.m_description = "$mb_sail_desc";
    mbSailPrefabPiece.m_placeEffect = woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbSailPrefabPiece);
    AddNetViewWithPersistence(mbSailPrefab);

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
    sail.m_mesh.sharedMaterial = sailMat;

    // this is a tier 1 sail
    SetWearNTear(mbSailPrefab, 1);

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
  public void RegisterCustomSailCreator(int sailCount)
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
    piece.m_placeEffect = woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(piece);

    var sailCreatorComponent = prefab.AddComponent<SailCreatorComponent>();
    sailCreatorComponent.m_sailSize = sailCount;

    var mesh = prefab.GetComponent<MeshRenderer>();
    mesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    var sailIcon = sailCount == 3
      ? sprites.GetSprite("customsail_tri")
      : sprites.GetSprite("customsail");

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = $"$mb_sail_{sailCount}_desc",
      Category = ValheimRaftMenuName,
      Enabled = true,
      Icon = sailIcon
    }));
  }

  private void RegisterPierPole()
  {
    var woodPolePrefab = prefabManager.GetPrefab("wood_pole_log_4");
    var mbPierPolePrefab = prefabManager.CreateClonedPrefab("MBPier_Pole", woodPolePrefab);

    // Less complicated wnt so re-usable method is not used
    var pierPoleWearNTear = mbPierPolePrefab.GetComponent<WearNTear>();
    pierPoleWearNTear.m_noRoofWear = false;

    var pierPolePrefabPiece = mbPierPolePrefab.GetComponent<Piece>();
    pierPolePrefabPiece.m_waterPiece = true;

    AddToRaftPrefabPieces(pierPolePrefabPiece);

    var pierComponent = mbPierPolePrefab.AddComponent<PierComponent>();
    pierComponent.m_segmentObject =
      prefabManager.CreateClonedPrefab("MBPier_Pole_Segment", woodPolePrefab);
    Destroy(pierComponent.m_segmentObject.GetComponent<ZNetView>());
    Destroy(pierComponent.m_segmentObject.GetComponent<Piece>());
    Destroy(pierComponent.m_segmentObject.GetComponent<WearNTear>());
    FixSnapPoints(mbPierPolePrefab);

    var transforms2 = pierComponent.m_segmentObject.GetComponentsInChildren<Transform>();
    for (var j = 0; j < transforms2.Length; j++)
      if ((bool)transforms2[j] && transforms2[j].CompareTag("snappoint"))
        Destroy(transforms2[j]);

    pierComponent.m_segmentHeight = 4f;
    pierComponent.m_baseOffset = -1f;

    var customPiece = new CustomPiece(mbPierPolePrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = "$mb_pier (" + pierPolePrefabPiece.m_name + ")",
      Description = "$mb_pier_desc\n " + pierPolePrefabPiece.m_description,
      Category = ValheimRaftMenuName,
      Enabled = true,
      Icon = pierPolePrefabPiece.m_icon,
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 4,
          Item = "RoundLog",
          Recover = true
        }
      }
    });

    // this could be off with the name since the name is overridden it may not apply until after things are run.
    AddToRaftPrefabPieces(customPiece.Piece);

    pieceManager.AddPiece(customPiece);
  }

  private void RegisterPierWall()
  {
    var stoneWallPrefab = prefabManager.GetPrefab("stone_wall_4x2");
    var pierWallPrefab = prefabManager.CreateClonedPrefab("MBPier_Stone", stoneWallPrefab);
    var pierWallPrefabPiece = pierWallPrefab.GetComponent<Piece>();
    pierWallPrefabPiece.m_waterPiece = true;

    var pier = pierWallPrefab.AddComponent<PierComponent>();
    pier.m_segmentObject =
      prefabManager.CreateClonedPrefab("MBPier_Stone_Segment", stoneWallPrefab);
    Destroy(pier.m_segmentObject.GetComponent<ZNetView>());
    Destroy(pier.m_segmentObject.GetComponent<Piece>());
    Destroy(pier.m_segmentObject.GetComponent<WearNTear>());
    FixSnapPoints(pierWallPrefab);

    var transforms = pier.m_segmentObject.GetComponentsInChildren<Transform>();
    for (var i = 0; i < transforms.Length; i++)
      if ((bool)transforms[i] && transforms[i].CompareTag("snappoint"))
        Destroy(transforms[i]);

    pier.m_segmentHeight = 2f;
    pier.m_baseOffset = 0f;

    var customPiece = new CustomPiece(pierWallPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = "$mb_pier (" + pierWallPrefabPiece.m_name + ")",
      Description = "$mb_pier_desc\n " + pierWallPrefabPiece.m_description,
      Category = ValheimRaftMenuName,
      Enabled = true,
      Icon = pierWallPrefabPiece.m_icon,
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 12,
          Item = "Stone",
          Recover = true
        }
      }
    });

    AddToRaftPrefabPieces(customPiece.Piece);

    pieceManager.AddPiece(customPiece);
  }

  private void RegisterBoardingRamp()
  {
    var woodPole2PrefabPiece = prefabManager.GetPrefab("wood_pole2").GetComponent<Piece>();

    mbBoardingRamp = prefabManager.CreateClonedPrefab("MBBoardingRamp", boarding_ramp);
    var floor = mbBoardingRamp.transform.Find("Ramp/Segment/SegmentAnchor/Floor").gameObject;
    var newFloor = Instantiate(
      woodFloorPiece.transform.Find("New/_Combined Mesh [high]").gameObject, floor.transform.parent,
      false);
    Destroy(floor);
    newFloor.transform.localPosition = new Vector3(1f, -52.55f, 0.5f);
    newFloor.transform.localScale = Vector3.one;
    newFloor.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

    var woodMat =
      woodPole2PrefabPiece.transform.Find("New").GetComponent<MeshRenderer>().sharedMaterial;
    mbBoardingRamp.transform.Find("Winch1/Pole").GetComponent<MeshRenderer>().sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Winch2/Pole").GetComponent<MeshRenderer>().sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Ramp/Segment/SegmentAnchor/Pole1").GetComponent<MeshRenderer>()
      .sharedMaterial = woodMat;
    mbBoardingRamp.transform.Find("Ramp/Segment/SegmentAnchor/Pole2").GetComponent<MeshRenderer>()
      .sharedMaterial = woodMat;
    mbBoardingRamp.transform.Find("Winch1/Cylinder").GetComponent<MeshRenderer>().sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Winch2/Cylinder").GetComponent<MeshRenderer>().sharedMaterial =
      woodMat;

    var ropeMat = raftMast.GetComponentInChildren<LineRenderer>(true)
      .sharedMaterial;
    mbBoardingRamp.transform.Find("Rope1").GetComponent<LineRenderer>().sharedMaterial = ropeMat;
    mbBoardingRamp.transform.Find("Rope2").GetComponent<LineRenderer>().sharedMaterial = ropeMat;

    var mbBoardingRampPiece = mbBoardingRamp.AddComponent<Piece>();
    mbBoardingRampPiece.m_name = "$mb_boarding_ramp";
    mbBoardingRampPiece.m_description = "$mb_boarding_ramp_desc";
    mbBoardingRampPiece.m_placeEffect = woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbBoardingRampPiece);
    AddNetViewWithPersistence(mbBoardingRamp);

    var boardingRamp2 = mbBoardingRamp.AddComponent<BoardingRampComponent>();
    boardingRamp2.m_stateChangeDuration = 0.3f;
    boardingRamp2.m_segments = 5;

    // previously was 1000f
    var mbBoardingRampWearNTear = SetWearNTear(mbBoardingRamp, 1);
    mbBoardingRampWearNTear.m_supports = false;

    FixCollisionLayers(mbBoardingRamp);
    FixSnapPoints(mbBoardingRamp);

    pieceManager.AddPiece(new CustomPiece(mbBoardingRamp, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_boarding_ramp_desc",
      Icon = sprites.GetSprite("boarding_ramp"),
      Category = ValheimRaftMenuName,
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
          Amount = 4,
          Item = "IronNails",
          Recover = true
        }
      }
    }));
  }

  /**
   * must be called after RegisterBoardingRamp
   */
  private void RegisterBoardingRampWide()
  {
    var mbBoardingRampWide =
      prefabManager.CreateClonedPrefab("MBBoardingRamp_Wide", mbBoardingRamp);
    var mbBoardingRampWidePiece = mbBoardingRampWide.GetComponent<Piece>();
    mbBoardingRampWidePiece.m_name = "$mb_boarding_ramp_wide";
    mbBoardingRampWidePiece.m_description = "$mb_boarding_ramp_wide_desc";

    AddToRaftPrefabPieces(mbBoardingRampWidePiece);

    var boardingRamp = mbBoardingRampWide.GetComponent<BoardingRampComponent>();
    boardingRamp.m_stateChangeDuration = 0.3f;
    boardingRamp.m_segments = 5;

    SetWearNTear(mbBoardingRampWide, 1);


    mbBoardingRampWide.transform.localScale = new Vector3(2f, 1f, 1f);
    FixSnapPoints(mbBoardingRampWide);

    pieceManager.AddPiece(new CustomPiece(mbBoardingRampWide, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_boarding_ramp_wide_desc",
      Icon = sprites.GetSprite("boarding_ramp"),
      Category = ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[2]
      {
        new()
        {
          Amount = 20,
          Item = "Wood",
          Recover = true
        },
        new()
        {
          Amount = 8,
          Item = "IronNails",
          Recover = true
        }
      }
    }));
  }

  private void RegisterDirtFloor(int size)
  {
    var prefabSizeString = $"{size}x{size}";
    var prefabName = $"MBDirtFloor_{prefabSizeString}";
    var mbDirtFloorPrefab = prefabManager.CreateClonedPrefab(prefabName, dirtFloor);

    mbDirtFloorPrefab.transform.localScale = new Vector3(size, 1f, size);

    var mbDirtFloorPrefabPiece = mbDirtFloorPrefab.AddComponent<Piece>();
    mbDirtFloorPrefabPiece.m_placeEffect = woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbDirtFloorPrefabPiece);
    AddNetViewWithPersistence(mbDirtFloorPrefab);

    var dirtFloorWearNTear = mbDirtFloorPrefab.AddComponent<WearNTear>();
    dirtFloorWearNTear.m_health = wearNTearBaseHealth;

    // Makes the component cultivatable
    mbDirtFloorPrefab.AddComponent<CultivatableComponent>();

    FixCollisionLayers(mbDirtFloorPrefab);
    FixSnapPoints(mbDirtFloorPrefab);

    pieceManager.AddPiece(new CustomPiece(mbDirtFloorPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = $"$mb_dirt_floor_{prefabSizeString}",
      Description = $"$mb_dirt_floor_{prefabSizeString}_desc",
      Category = ValheimRaftMenuName,
      Enabled = true,
      Icon = sprites.GetSprite("dirtfloor_icon"),
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          // this may cause issues it's just size^2 but Math.Pow returns a double
          Amount = (int)Math.Pow(size, 2),
          Item = "Stone",
          Recover = true
        }
      }
    }));
  }

  private void FixCollisionLayers(GameObject r)
  {
    var piece = r.layer = LayerMask.NameToLayer("piece");
    var comps = r.transform.GetComponentsInChildren<Transform>(true);
    for (var i = 0; i < comps.Length; i++) comps[i].gameObject.layer = piece;
  }

  private static void FixedRopes(GameObject r)
  {
    var ropes = r.GetComponentsInChildren<LineAttach>();
    for (var i = 0; i < ropes.Length; i++)
    {
      ropes[i].GetComponent<LineRenderer>().positionCount = 2;
      ropes[i].m_attachments.Clear();
      ropes[i].m_attachments.Add(r.transform);
    }
  }

  private void FixSnapPoints(GameObject r)
  {
    var t = r.GetComponentsInChildren<Transform>(true);
    for (var i = 0; i < t.Length; i++)
      if (t[i].name.StartsWith("_snappoint"))
        t[i].tag = "snappoint";
  }

  private void PrintCollisionMatrix()
  {
    var sb = new StringBuilder();
    sb.AppendLine("");
    sb.Append(" ".PadLeft(23));
    for (var i = 0; i < 32; i++) sb.Append(i.ToString().PadRight(3));

    sb.AppendLine("");
    for (var j = 0; j < 32; j++)
    {
      sb.Append(LayerMask.LayerToName(j).PadLeft(20) + j.ToString().PadLeft(3));
      for (var k = 0; k < 32; k++)
      {
        var hit = !Physics.GetIgnoreLayerCollision(j, k);
        sb.Append(hit ? "[X]" : "[ ]");
      }

      sb.AppendLine("");
    }

    sb.AppendLine("");
    ZLog.Log(sb.ToString());
  }
}