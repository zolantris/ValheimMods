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
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;
using Zolantris.Shared;
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
          PrefabNames.IsHull(obj) && obj.name.Contains("(Clone)"))
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

#if DEBUG
  public static void LogRegisteredPieces()
  {
    LoggerProvider.LogInfo($"Piece table registered? VehicleHammerTable is null: {VehicleHammerTableRegistry.VehicleHammerTable == null}");

    foreach (var table in Resources.FindObjectsOfTypeAll<PieceTable>())
    {
      var pieces = table.m_pieces;
      var name = table.name;

      LoggerProvider.LogInfo($"Piece table: {name}, has {pieces.Count} pieces");

      foreach (var piece in pieces)
      {
        LoggerProvider.LogInfo($" - Piece: {piece?.name}");
      }
    }

    if (VehicleHammerTableRegistry.VehicleHammerTable?.PieceTable == null)
    {
      LoggerProvider.LogError("VehicleHammerTable or its PieceTable is null.");
      return;
    }

    LoggerProvider.LogInfo($"VehicleHammerTable real name: {VehicleHammerTableRegistry.VehicleHammerTable.PieceTable.name}");
    LoggerProvider.LogInfo($"Registered pieces in VehicleHammerTable:");

    foreach (var piece in VehicleHammerTableRegistry.VehicleHammerTable.PieceTable.m_pieces)
    {
      LoggerProvider.LogInfo($" - {piece.name}");
    }
  }
#endif

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
      // Assembly.GetExecutingAssembly if this mod is migrated to a BepInExPlugin
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


#if DEBUG
      var canLog = false;
      if (canLog)
      {
        LogRegisteredPieces();
      }
#endif
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Error occurred during InitAfterVanillaItemsAndPrefabsAvailable call. \nException:\n {e}");
      return;
    }

    HasRunInitSuccessfully = true;
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
    MechanismPrefabs.Register();
    CustomMeshPrefabs.Instance.Register(prefabManager, pieceManager);

    SwivelPrefab.Register();

    ShipRudderPrefabs.Instance.Register(prefabManager, pieceManager);

    CannonPrefabs.Register();

    // Raft Structure
    ShipHullPrefab.Instance.Register(prefabManager, pieceManager);

    // VehiclePrefabs
    VehiclePiecesPrefab.Instance.Register(prefabManager, pieceManager);

    RamPrefabs.Instance.Register(prefabManager, pieceManager);

    // new way to register components
    CustomVehicleMastRegistry.Register();

    // register experimental prefabs.
    ExperimentalPrefabRegistry.Register();

    // sails and masts
    SailPrefabs.Instance.Register(prefabManager, pieceManager);

    // Rope items
    RopeAnchorPrefabRegistry.Register();
    RopeLadderPrefabRegistry.Register();
    AnchorPrefabs.Register();

    // pier components
    PierPrefabRegistry.Register();

    // Ramps
    RampPrefabRegistry.Register();
    // Floors
    DirtFloorPrefabRegistry.Register();
  }
}