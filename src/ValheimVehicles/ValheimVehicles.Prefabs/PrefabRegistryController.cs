using System;
using System.Collections.Generic;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Registry;
using UnityEngine;
using UnityEngine.U2D;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;

using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs;

public static class PrefabRegistryController
{
  public static PrefabManager prefabManager;
  public static PieceManager pieceManager;
  private static SynchronizationManager synchronizationManager;
  private static List<Piece> raftPrefabPieces = new();
  private static bool prefabsEnabled = true;

  public static AssetBundle vehicleAssetBundle;

  private static bool HasRunInitSuccessfully = false;

  private static string ValheimDefaultPieceTableName = "Hammer";

  /// <summary>
  /// Gets the PieceTableName and falls back to the original piece name.
  /// </summary>
  /// <returns></returns>
  public static string GetPieceTableName()
  {
    return VehicleHammerTableRegistry.VehicleHammerTable != null ? VehicleHammerTableRegistry.VehicleHammerTableName : ValheimDefaultPieceTableName;
  }

  private static PieceTable? _cachedValheimHammerPieceTable = null;

  /// <summary>
  /// Gets the custom-table name or fallsback to original hammer table.
  /// </summary>
  /// <returns></returns>
  public static PieceTable GetPieceTable()
  {
    if (VehicleHammerTableRegistry.VehicleHammerTable != null) return VehicleHammerTableRegistry.VehicleHammerTable.PieceTable;

    if (_cachedValheimHammerPieceTable != null)
    {
      return _cachedValheimHammerPieceTable;
    }

#if DEBUG
    var allTables = PieceManager.Instance.GetPieceTables();

    // for debugging names.
    foreach (var pieceTable in allTables)
    {
      if (pieceTable == null) continue;
      LoggerProvider.LogDebug(pieceTable.name);
    }
#endif

    _cachedValheimHammerPieceTable = PieceManager.Instance.GetPieceTable(ValheimDefaultPieceTableName);

    return _cachedValheimHammerPieceTable;
  }

  public static string SetCategoryName(string val)
  {
    if (VehicleHammerTableRegistry.VehicleHammerTable != null)
    {
      return val;
    }

    // fallback name.
    return PrefabNames.DEPRECATED_ValheimRaftMenuName;
  }

  /// <summary>
  /// For debugging and nuking rafts, not to be included in releases
  /// </summary>
  public static void DebugDestroyAllRaftObjects()
  {
    var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
    foreach (var obj in allObjects)
      if (obj.name.Contains($"{PrefabNames.WaterVehicleShip}(Clone)") ||
          (PrefabNames.IsHull(obj) && obj.name.Contains("(Clone)")))
      {
        var wnt = obj.GetComponent<WearNTear>();
        if (wnt)
          wnt.Destroy();
        else
          Object.Destroy(obj);
      }
  }

  /// <summary>
  /// Requires assetbundle values to be set already
  /// </summary>
  private static void SetupComponents()
  {
    Vector3Logger.LoggerAPI = Logger.LogDebug;
    // ConvexHullMeshGeneratorAPI.IsAllowedAsHullOverride =
    //   PrefabNames.IsHull;
    ConvexHullAPI.DebugMaterial =
      LoadValheimVehicleAssets.DoubleSidedTransparentMat;
    // ConvexHullMeshGeneratorAPI.GeneratedMeshNamePrefix = PrefabNames.ConvexHull;
  }

  // todo this should come from config
  public static float wearNTearBaseHealth = 250f;

  private static void UpdatePrefabs(bool isPrefabEnabled)
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

      Logger.LogDebug(
        $"Setting m_enabled: to {isPrefabEnabled}, for name {piece.name}");
      pmPiece.Piece.m_enabled = isPrefabEnabled;
    }

    prefabsEnabled = isPrefabEnabled;
  }

  public static void UpdatePrefabStatus()
  {
    if (!PrefabConfig.AdminsCanOnlyBuildRaft.Value &&
        prefabsEnabled)
      return;

    Logger.LogDebug(
      $"ValheimRAFT: UpdatePrefabStatusCalled with AdminsCanOnlyBuildRaft set as {PrefabConfig.AdminsCanOnlyBuildRaft.Value}, updating prefabs and player access");
    var isAdmin = SynchronizationManager.Instance.PlayerIsAdmin;
    UpdatePrefabs(isAdmin);
  }

  public static void UpdatePrefabStatus(object obj,
    ConfigurationSynchronizationEventArgs e)
  {
    UpdateRaftSailDescriptions();
    UpdatePrefabStatus();
  }


  private static void UpdateRaftSailDescriptions()
  {
    var tier1 = pieceManager.GetPiece(PrefabNames.Tier1RaftMastName);
    tier1.Piece.m_description = SailPrefabs.GetTieredSailAreaText(1);

    var tier2 = pieceManager.GetPiece(PrefabNames.Tier2RaftMastName);
    tier2.Piece.m_description = SailPrefabs.GetTieredSailAreaText(2);

    var tier3 = pieceManager.GetPiece(PrefabNames.Tier3RaftMastName);
    tier3.Piece.m_description = SailPrefabs.GetTieredSailAreaText(3);

    var tier4 = pieceManager.GetPiece(PrefabNames.Tier4RaftMastName);
    tier4.Piece.m_description = SailPrefabs.GetTieredSailAreaText(4);
  }


  /**
   * initializes the bundle for ValheimVehicles
   *
   * InitPrefabs will work with both items and prefab items for OnVanillaItems/Prefabs are ready.
   */
  public static void InitAfterVanillaItemsAndPrefabsAreAvailable()
  {
    if (HasRunInitSuccessfully)
    {
      LoggerProvider.LogInfo("skipping PrefabRegistryController.Init as it has already been done.");
      return;
    }

    try
    {
// todo call Assembly.GetExecutingAssembly if we move the asset bundle into ValheimVehicles mod.
      vehicleAssetBundle =
        AssetUtils.LoadAssetBundleFromResources("valheim-vehicles",
          Assembly.GetCallingAssembly());

      prefabManager = PrefabManager.Instance;
      pieceManager = PieceManager.Instance;

      LoadValheimAssets.Instance.Init(prefabManager);

      // dependent on ValheimVehiclesShared
      LoadValheimRaftAssets.Instance.Init(vehicleAssetBundle);
      // dependent on ValheimVehiclesShared and RaftAssetBundle
      LoadValheimVehicleAssets.Instance.Init(vehicleAssetBundle);

      // ValheimVehicle HammerTab, must be done before items and prefab generic registrations
      new VehicleHammerTableRegistry().Register();

      // must be called after assets are loaded
      PrefabRegistryHelpers.Init();

      RegisterAllItemPrefabs();
      RegisterAllPiecePrefabs();

      // must be called after RegisterAllPrefabs and AssetBundle assignment to be safe.
      SetupComponents();
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error occurred during InitAfterVanillaItemsAndPrefabsAvailable call. \nException:\n {e}");
      return;
    }

    HasRunInitSuccessfully = true;
  }

  public static void AddToRaftPrefabPieces(Piece raftPiece)
  {
    raftPrefabPieces.Add(raftPiece);
  }

  public static void RegisterValheimVehiclesPrefabs()
  {
    SwitchAndLeverPrefabs.Instance.Register(prefabManager, pieceManager);
    CustomMeshPrefabs.Instance.Register(prefabManager, pieceManager);

    AnchorPrefabs.Instance.Register(prefabManager, pieceManager);

    ShipRudderPrefabs.Instance.Register(prefabManager, pieceManager);

    // Raft Structure
    ShipHullPrefab.Instance.Register(prefabManager, pieceManager);

    // VehiclePrefabs
    VehiclePiecesPrefab.Instance.Register(prefabManager, pieceManager);

    RamPrefabs.Instance.Register(prefabManager, pieceManager);
  }

  public static void RegisterAllItemPrefabs()
  {
    // main hammer for opening the custom vehicle build menu.
    new VehicleHammerItemRegistry().Register();
  }

  public static void RegisterAllPiecePrefabs()
  {
    // Critical Items
    VehiclePrefabs.Instance.Register(prefabManager, pieceManager);
    ShipSteeringWheelPrefab.Instance.Register(prefabManager, pieceManager);

    // ValheimVehicle Prefabs
    RegisterValheimVehiclesPrefabs();

    // sails and masts
    SailPrefabs.Instance.Register(prefabManager, pieceManager);

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


  private static void RegisterRopeLadder()
  {
    var mbRopeLadderPrefab =
      prefabManager.CreateClonedPrefab(PrefabNames.MBRopeLadder,
        LoadValheimRaftAssets.ropeLadder);

    var mbRopeLadderPrefabPiece = mbRopeLadderPrefab.AddComponent<Piece>();
    mbRopeLadderPrefabPiece.m_name = "$mb_rope_ladder";
    mbRopeLadderPrefabPiece.m_description = "$mb_rope_ladder_desc";
    mbRopeLadderPrefabPiece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;
    mbRopeLadderPrefabPiece.m_primaryTarget = false;
    mbRopeLadderPrefabPiece.m_randomTarget = false;

    AddToRaftPrefabPieces(mbRopeLadderPrefabPiece);
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
    pieceManager.AddPiece(new CustomPiece(mbRopeLadderPrefab, false,
      new PieceConfig
      {
        PieceTable = GetPieceTableName(),
        Description = "$mb_rope_ladder_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .RopeLadder),
        Category = SetCategoryName(VehicleHammerTableCategories.Structure),
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

  private static void RegisterRopeAnchor()
  {
    var prefab =
      prefabManager.CreateClonedPrefab(PrefabNames.MBRopeAnchor,
        LoadValheimRaftAssets.rope_anchor);

    var mbRopeAnchorPrefabPiece = prefab.AddComponent<Piece>();
    mbRopeAnchorPrefabPiece.m_name = "$mb_rope_anchor";
    mbRopeAnchorPrefabPiece.m_description = "$mb_rope_anchor_desc";
    mbRopeAnchorPrefabPiece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbRopeAnchorPrefabPiece);
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
    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = GetPieceTableName(),
      Description = "$mb_rope_anchor_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite("rope_anchor"),
      Category = SetCategoryName(VehicleHammerTableCategories.Structure),
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


  private static void RegisterPierPole()
  {
    var woodPolePrefab = prefabManager.GetPrefab("wood_pole_log_4");
    var mbPierPolePrefab =
      prefabManager.CreateClonedPrefab("MBPier_Pole", woodPolePrefab);

    // Less complicated wnt so re-usable method is not used
    var pierPoleWearNTear = mbPierPolePrefab.GetComponent<WearNTear>();
    pierPoleWearNTear.m_noRoofWear = false;

    var pierPolePrefabPiece = mbPierPolePrefab.GetComponent<Piece>();
    pierPolePrefabPiece.m_waterPiece = true;

    AddToRaftPrefabPieces(pierPolePrefabPiece);

    var pierComponent = mbPierPolePrefab.AddComponent<PierComponent>();
    pierComponent.m_segmentObject =
      prefabManager.CreateClonedPrefab("MBPier_Pole_Segment", woodPolePrefab);
    Object.Destroy(pierComponent.m_segmentObject.GetComponent<ZNetView>());
    Object.Destroy(pierComponent.m_segmentObject.GetComponent<Piece>());
    Object.Destroy(pierComponent.m_segmentObject.GetComponent<WearNTear>());
    PrefabRegistryHelpers.FixSnapPoints(mbPierPolePrefab);

    var transforms2 =
      pierComponent.m_segmentObject.GetComponentsInChildren<Transform>();
    for (var j = 0; j < transforms2.Length; j++)
      if ((bool)transforms2[j] && transforms2[j].CompareTag("snappoint"))
        Object.Destroy(transforms2[j]);

    pierComponent.m_segmentHeight = 4f;
    pierComponent.m_baseOffset = -1f;

    var customPiece = new CustomPiece(mbPierPolePrefab, false, new PieceConfig
    {
      PieceTable = GetPieceTableName(),
      Name = "$mb_pier (" + pierPolePrefabPiece.m_name + ")",
      Description = "$mb_pier_desc\n " + pierPolePrefabPiece.m_description,
      Category = SetCategoryName(VehicleHammerTableCategories.Structure),
      Enabled = true,
      Icon = pierPolePrefabPiece.m_icon,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 4,
          Item = "RoundLog",
          Recover = true
        }
      ]
    });

    // this could be off with the name since the name is overridden it may not apply until after things are run.
    AddToRaftPrefabPieces(customPiece.Piece);

    pieceManager.AddPiece(customPiece);
  }

  private static void RegisterPierWall()
  {
    var stoneWallPrefab = prefabManager.GetPrefab("stone_wall_4x2");
    var pierWallPrefab =
      prefabManager.CreateClonedPrefab("MBPier_Stone", stoneWallPrefab);
    var pierWallPrefabPiece = pierWallPrefab.GetComponent<Piece>();
    pierWallPrefabPiece.m_waterPiece = true;

    var pier = pierWallPrefab.AddComponent<PierComponent>();
    pier.m_segmentObject =
      prefabManager.CreateClonedPrefab("MBPier_Stone_Segment", stoneWallPrefab);
    Object.Destroy(pier.m_segmentObject.GetComponent<ZNetView>());
    Object.Destroy(pier.m_segmentObject.GetComponent<Piece>());
    Object.Destroy(pier.m_segmentObject.GetComponent<WearNTear>());
    PrefabRegistryHelpers.FixSnapPoints(pierWallPrefab);

    var transforms = pier.m_segmentObject.GetComponentsInChildren<Transform>();
    for (var i = 0; i < transforms.Length; i++)
      if ((bool)transforms[i] && transforms[i].CompareTag("snappoint"))
        Object.Destroy(transforms[i]);

    pier.m_segmentHeight = 2f;
    pier.m_baseOffset = 0f;

    var customPiece = new CustomPiece(pierWallPrefab, false, new PieceConfig
    {
      PieceTable = GetPieceTableName(),
      Name = "$mb_pier (" + pierWallPrefabPiece.m_name + ")",
      Description = "$mb_pier_desc\n " + pierWallPrefabPiece.m_description,
      Category = SetCategoryName(VehicleHammerTableCategories.Structure),
      Enabled = true,
      Icon = pierWallPrefabPiece.m_icon,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 12,
          Item = "Stone",
          Recover = true
        }
      ]
    });

    AddToRaftPrefabPieces(customPiece.Piece);

    pieceManager.AddPiece(customPiece);
  }

  private static void RegisterBoardingRamp()
  {
    var woodPole2PrefabPiece =
      prefabManager.GetPrefab("wood_pole2").GetComponent<Piece>();

    var mbBoardingRamp =
      prefabManager.CreateClonedPrefab(PrefabNames.BoardingRamp,
        LoadValheimRaftAssets.boardingRampAsset);
    var floor = mbBoardingRamp.transform
      .Find("Ramp/Segment/SegmentAnchor/Floor").gameObject;
    var newFloor = Object.Instantiate(
      LoadValheimAssets.woodFloorPiece.transform
        .Find("New/_Combined Mesh [high]").gameObject,
      floor.transform.parent,
      false);
    Object.Destroy(floor);
    newFloor.transform.localPosition = new Vector3(1f, -52.55f, 0.5f);
    newFloor.transform.localScale = Vector3.one;
    newFloor.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

    var woodMat =
      woodPole2PrefabPiece.transform.Find("New").GetComponent<MeshRenderer>()
        .sharedMaterial;
    mbBoardingRamp.transform.Find("Winch1/Pole").GetComponent<MeshRenderer>()
        .sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Winch2/Pole").GetComponent<MeshRenderer>()
        .sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Ramp/Segment/SegmentAnchor/Pole1")
      .GetComponent<MeshRenderer>()
      .sharedMaterial = woodMat;
    mbBoardingRamp.transform.Find("Ramp/Segment/SegmentAnchor/Pole2")
      .GetComponent<MeshRenderer>()
      .sharedMaterial = woodMat;
    mbBoardingRamp.transform.Find("Winch1/Cylinder")
        .GetComponent<MeshRenderer>().sharedMaterial =
      woodMat;
    mbBoardingRamp.transform.Find("Winch2/Cylinder")
        .GetComponent<MeshRenderer>().sharedMaterial =
      woodMat;

    var ropeMat = LoadValheimAssets.raftMast
      .GetComponentInChildren<LineRenderer>(true)
      .sharedMaterial;
    mbBoardingRamp.transform.Find("Rope1").GetComponent<LineRenderer>()
      .sharedMaterial = ropeMat;
    mbBoardingRamp.transform.Find("Rope2").GetComponent<LineRenderer>()
      .sharedMaterial = ropeMat;

    var mbBoardingRampPiece = mbBoardingRamp.AddComponent<Piece>();
    mbBoardingRampPiece.m_name = "$mb_boarding_ramp";
    mbBoardingRampPiece.m_description = "$mb_boarding_ramp_desc";
    mbBoardingRampPiece.m_placeEffect =
      LoadValheimAssets.woodFloorPiece.m_placeEffect;

    AddToRaftPrefabPieces(mbBoardingRampPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(mbBoardingRamp);

    var boardingRamp2 = mbBoardingRamp.AddComponent<BoardingRampComponent>();
    boardingRamp2.m_stateChangeDuration = 0.3f;
    boardingRamp2.m_segments = 5;

    // previously was 1000f
    var mbBoardingRampWearNTear =
      PrefabRegistryHelpers.SetWearNTear(mbBoardingRamp, 1);
    mbBoardingRampWearNTear.m_supports = false;

    PrefabRegistryHelpers.FixCollisionLayers(mbBoardingRamp);

    pieceManager.AddPiece(new CustomPiece(mbBoardingRamp, false, new PieceConfig
    {
      PieceTable = GetPieceTableName(),
      Description = "$mb_boarding_ramp_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .BoardingRamp),
      Category = SetCategoryName(VehicleHammerTableCategories.Structure),
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
          Amount = 4,
          Item = "IronNails",
          Recover = true
        }
      ]
    }));
  }

  /**
   * must be called after RegisterBoardingRamp
   */
  private static void RegisterBoardingRampWide()
  {
    var mbBoardingRampWide =
      prefabManager.CreateClonedPrefab(PrefabNames.BoardingRampWide,
        prefabManager.GetPrefab(PrefabNames.BoardingRamp));
    var mbBoardingRampWidePiece = mbBoardingRampWide.GetComponent<Piece>();
    mbBoardingRampWidePiece.m_name = "$mb_boarding_ramp_wide";
    mbBoardingRampWidePiece.m_description = "$mb_boarding_ramp_wide_desc";
    mbBoardingRampWide.transform.localScale = new Vector3(2f, 1f, 1f);

    AddToRaftPrefabPieces(mbBoardingRampWidePiece);

    var boardingRamp = mbBoardingRampWide.GetComponent<BoardingRampComponent>();
    boardingRamp.m_stateChangeDuration = 0.3f;
    boardingRamp.m_segments = 5;

    PrefabRegistryHelpers.SetWearNTear(mbBoardingRampWide, 1);
    PrefabRegistryHelpers.FixSnapPoints(mbBoardingRampWide);


    pieceManager.AddPiece(new CustomPiece(mbBoardingRampWide, false,
      new PieceConfig
      {
        PieceTable = GetPieceTableName(),
        Description = "$mb_boarding_ramp_wide_desc",
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .BoardingRamp),
        Category = SetCategoryName(VehicleHammerTableCategories.Structure),
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 20,
            Item = "Wood",
            Recover = true
          },
          new RequirementConfig
          {
            Amount = 8,
            Item = "IronNails",
            Recover = true
          }
        ]
      }));
  }

  private static void RegisterDirtFloor(int size)
  {
    var prefabSizeString = $"{size}x{size}";
    var prefabName = $"MBDirtFloor_{prefabSizeString}";
    var mbDirtFloorPrefab =
      prefabManager.CreateClonedPrefab(prefabName,
        LoadValheimRaftAssets.dirtFloor);

    mbDirtFloorPrefab.transform.localScale = new Vector3(size, 1f, size);

    var mbDirtFloorPrefabPiece = mbDirtFloorPrefab.AddComponent<Piece>();
    mbDirtFloorPrefabPiece.m_placeEffect =
      LoadValheimAssets.stoneFloorPiece.m_placeEffect;
    mbDirtFloorPrefabPiece.m_allowedInDungeons = true;

    AddToRaftPrefabPieces(mbDirtFloorPrefabPiece);

    PrefabRegistryHelpers.AddNetViewWithPersistence(mbDirtFloorPrefab);

    var wnt = PrefabRegistryHelpers.SetWearNTear(mbDirtFloorPrefab, 2);
    wnt.m_haveRoof = false;

    // Makes the component cultivatable
    mbDirtFloorPrefab.AddComponent<CultivatableComponent>();

    PrefabRegistryHelpers.FixCollisionLayers(mbDirtFloorPrefab);
    PrefabRegistryHelpers.FixSnapPoints(mbDirtFloorPrefab);

    pieceManager.AddPiece(new CustomPiece(mbDirtFloorPrefab, false,
      new PieceConfig
      {
        PieceTable = GetPieceTableName(),
        Name = $"$mb_dirt_floor_{prefabSizeString}",
        Description = $"$mb_dirt_floor_{prefabSizeString}_desc",
        Category = SetCategoryName(VehicleHammerTableCategories.Structure),
        Enabled = true,
        Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
          .DirtFloor),
        Requirements =
        [
          new RequirementConfig
          {
            // this may cause issues it's just size^2 but Math.Pow returns a double
            Amount = (int)Math.Pow(size, 2),
            Item = "Stone",
            Recover = true
          }
        ]
      }));
  }
}