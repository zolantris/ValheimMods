// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region Usings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Newtonsoft.Json;
using Registry;
using UnityEngine;
using UnityEngine.U2D;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Components;
using ValheimVehicles.Constants;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;
using Zolantris.Shared;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

#endregion

namespace ValheimVehicles.Prefabs
{
  /// <summary>
  /// Central prefab & piece registry controller.
  /// - Wraps Jötunn calls via AddPiece/AddPrefab delegates (call these from your registries).
  /// - Creates per-item config toggles (with exclusion list/regex).
  /// - Finalizes once to apply enables and emit a sorted, versioned JSON snapshot.
  /// - Preserves your original initialization flow and helpers.
  /// </summary>
  public static class PrefabRegistryController
  {

  #region Static fields (state)

    // Jötunn managers / state you already had
    public static PrefabManager prefabManager;
    public static PieceManager pieceManager;
    private static SynchronizationManager synchronizationManager;
    private static readonly List<Piece> raftPrefabPieces = new();
    private static bool prefabsEnabled = true;

    public static AssetBundle vehicleAssetBundle;

    private static bool HasRunInitSuccessfully = false;

    private static readonly string ValheimDefaultPieceTableName = "Hammer";

    private static PieceTable _cachedValheimHammerPieceTable = null;

    // todo this should come from config
    public static float wearNTearBaseHealth = 250f;

    // New registry layer state
    private static readonly object _lock = new();
    private static bool _layerInitialized;
    private static bool _finalized;

    private static ConfigFile _config;
    private static string _modGuid = "unknown.mod.guid";
    private static string _modVersion = "0.0.0";
    private static string _snapshotDir;

    // Exclusion controls
    private static ConfigEntry<string> _excludedPrefabNamesCsv; // exact matches
    private static ConfigEntry<string> _excludedPrefabRegexCsv; // regex patterns

    private static HashSet<string> _excludedNames = new(StringComparer.Ordinal);
    private static List<Regex> _excludedRegex = new();

    // Prefab bookkeeping (pre-finalize)
    private sealed class PrefabEntry
    {
      public string Name = "";
      public GameObject Prefab; // provided at call site (can be null for tracking-only)
      public bool DefaultEnabled = true;
      public bool Configurable = true;
      public bool? FinalEnabled; // resolved at finalize
      public bool RegisteredWithPrefabManager; // true if we called PrefabManager.AddPrefab
      public ConfigEntry<bool> EnabledConfig; // created if configurable
      public int SnapshotHash;
    }

    // Piece bookkeeping (build-table items)
    private sealed class PieceEntry
    {
      public string Name = "";
      public bool DefaultEnabled = true;
      public bool Configurable = true;
      public WeakReference<Piece> PieceRef = new(null!);
      public bool? FinalEnabled; // resolved at finalize
      public ConfigEntry<bool> EnabledConfig; // created if configurable
      public int SnapshotHash;
    }

    private static readonly Dictionary<string, PrefabEntry> _prefabEntries = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, PieceEntry> _pieceEntries = new(StringComparer.Ordinal);

    // Snapshot model (single, unified array)
    private sealed class Snapshot
    {
      public string ModGuid = "";
      public string ModVersion = "";
      public string GeneratedAtUtc = "";
      public string Game = "Valheim";
      public string Tool = "PrefabRegistryController v2";
      public List<SnapshotItem> Items = new();
    }

    private sealed class SnapshotItem
    {
      public string Name = "";
      public string Kind = "Prefab"; // "Prefab" or "Piece"
      public bool Enabled;
      public bool Configurable;
      public int Hash;
    }

  #endregion

  #region Public: Initialization

    /// <summary>
    /// Call once in your plugin Awake() to enable per-item config + snapshot.
    /// Example:
    /// PrefabRegistryController.Initialize(Config, "com.virtualize.valheimvehicles", Info.Metadata.Version.ToString());
    /// </summary>
    public static void Initialize(ConfigFile config, string modGuid, string modVersion, string snapshotSubdirName = "PrefabSnapshots")
    {
      if (config == null) throw new ArgumentNullException(nameof(config));
      if (string.IsNullOrWhiteSpace(modGuid)) throw new ArgumentException("modGuid is required.", nameof(modGuid));
      if (string.IsNullOrWhiteSpace(modVersion)) throw new ArgumentException("modVersion is required.", nameof(modVersion));

      lock (_lock)
      {
        if (_layerInitialized) return;

        _config = config;
        _modGuid = modGuid.Trim();
        _modVersion = modVersion.Trim();

        // kept in release but not executed.
        if (ModEnvironment.IsDebug)
        {
          var baseDir = BepInEx.Paths.PluginPath;
          _snapshotDir = Path.Combine(baseDir, _modGuid, snapshotSubdirName);
          Directory.CreateDirectory(_snapshotDir);
        }

        _excludedPrefabNamesCsv = _config.Bind(
          new ConfigDefinition("PrefabRegistry", "ExcludedPrefabs"),
          "",
          new ConfigDescription("Comma/semicolon/whitespace separated list of prefab/piece names that should NOT be configurable (always enabled)."));

        _excludedPrefabRegexCsv = _config.Bind(
          new ConfigDefinition("PrefabRegistry", "ExcludedPrefabRegex"),
          "",
          new ConfigDescription("Comma/semicolon/whitespace separated regex patterns. Any matching prefab/piece will NOT be configurable (always enabled)."));

        ParseExclusions();

        _layerInitialized = true;
      }
    }

    /// <summary>
    /// (Optional) Re-read exclusion config before finalize (e.g., after a /reload).
    /// </summary>
    public static void RefreshExclusionsFromConfig()
    {
      EnsureLayerInitialized();
      lock (_lock)
      {
        ThrowIfFinalized();
        ParseExclusions();
      }
    }

    // Hash→Hash for fast runtime resolution
    private static readonly ConcurrentDictionary<int, int> _prefabAliasMap = new();

    // Keep human-friendly metadata for debugging/logging
    private sealed class AliasInfo
    {
      public string OldName;
      public string NewName;
      public int OldHash;
      public int NewHash;
    }

    // OldHash→AliasInfo
    private static readonly ConcurrentDictionary<int, AliasInfo> _aliasInfoMap = new();

    public static void AddPrefabAlias(string oldName, string newName)
    {
      if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
        return;

      var oldHash = oldName.GetStableHashCode();
      var newHash = newName.GetStableHashCode();

      var info = new AliasInfo
      {
        OldName = oldName,
        NewName = newName,
        OldHash = oldHash,
        NewHash = newHash
      };

      _prefabAliasMap[oldHash] = newHash;
      _aliasInfoMap[oldHash] = info;

      LoggerProvider.LogDebug($"[Alias] {oldName} (0x{oldHash:X8}) → {newName} (0x{newHash:X8})");
    }

    public static void AddPrefabAliases(IEnumerable<(string oldName, string newName)> pairs)
    {
      foreach (var (oldName, newName) in pairs)
        AddPrefabAlias(oldName, newName);
    }

    public static int ResolveAliasedHash(int hash)
    {
      return _prefabAliasMap.TryGetValue(hash, out var mapped) ? mapped : hash;
    }

    // Debug-friendly listing
    public static IReadOnlyCollection<string> GetAliasSummaries()
    {
      return _aliasInfoMap.Values
        .OrderBy(x => x.OldName, StringComparer.Ordinal)
        .Select(x => $"{x.OldName} (0x{x.OldHash:X8}) → {x.NewName} (0x{x.NewHash:X8})")
        .ToList();
    }

    // Optional raw info lookup
    public static bool TryGetAliasInfo(int oldHash, out string oldName, out string newName)
    {
      if (_aliasInfoMap.TryGetValue(oldHash, out var info))
      {
        oldName = info.OldName;
        newName = info.NewName;
        return true;
      }
      oldName = null;
      newName = null;
      return false;
    }

  #endregion

  #region Public: Delegates (preferred call sites)

    /// <summary>
    /// Replacement for PrefabRegistryController.AddPiece(new CustomPiece(...)).
    /// Calls Jötunn immediately, then tracks piece for config + snapshot (enable enforced at finalize).
    /// </summary>
    public static bool AddPiece(CustomPiece customPiece, bool defaultEnabled = true)
    {
      if (customPiece == null) throw new ArgumentNullException(nameof(customPiece));

      // 1) Original behavior
      if (!PieceManager.Instance.AddPiece(customPiece))
      {
        LoggerProvider.LogWarning($"[PrefabRegistryController.AddPiece] track failed: {customPiece.Piece.name}");
        return false;
      }

      // 2) Track for config + snapshot
      try
      {
        var pieceName = ResolvePieceName(customPiece);
        if (!string.IsNullOrEmpty(pieceName))
        {
          RegisterPiece(customPiece, defaultEnabled);
        }
        return true;
      }
      catch (Exception ex)
      {
        LoggerProvider.LogWarning($"[PrefabRegistryController.AddPiece] track failed: {ex}");
        return false;
      }
    }

    /// <summary>
    /// Convenience overload mirroring common call sites: prefab + PieceConfig (+ optional isPrivateArea).
    /// </summary>
    public static void AddPiece(GameObject prefab, PieceConfig config, bool isPrivateArea = false, bool defaultEnabled = true)
    {
      if (prefab == null) throw new ArgumentNullException(nameof(prefab));
      if (config == null) throw new ArgumentNullException(nameof(config));

      var cp = new CustomPiece(prefab, isPrivateArea, config);
      AddPiece(cp, defaultEnabled);
    }

    public static bool TryAddPiece(CustomPiece customPiece, bool defaultEnabled = true)
    {
      try
      {
        AddPiece(customPiece, defaultEnabled);
        return true;
      }
      catch (Exception ex)
      {
        LoggerProvider.LogError($"[PrefabRegistryController.TryAddPiece] {ex}");
        return false;
      }
    }

    /// <summary>
    /// Replacement for PrefabManager.Instance.AddPrefab(go).
    /// Calls Jötunn immediately, then tracks prefab for config + snapshot.
    /// </summary>
    public static void AddPrefab(GameObject prefab, bool defaultEnabled = true)
    {
      if (prefab == null) throw new ArgumentNullException(nameof(prefab));

      PrefabManager.Instance.AddPrefab(prefab);

      try
      {
        RegisterPrefab(prefab, prefab.name, defaultEnabled);
      }
      catch (Exception ex)
      {
        LoggerProvider.LogWarning($"[PrefabRegistryController.AddPrefab] track failed: {ex}");
      }
    }

    public static bool TryAddPrefab(GameObject prefab, bool defaultEnabled = true)
    {
      try
      {
        AddPrefab(prefab, defaultEnabled);
        return true;
      }
      catch (Exception ex)
      {
        LoggerProvider.LogError($"[PrefabRegistryController.TryAddPrefab] {ex}");
        return false;
      }
    }

  #endregion

  #region Public: Registration (tracking only; called by delegates or manually)

    /// <summary>
    /// Track a prefab by name so we can create a config toggle and include it in the snapshot.
    /// </summary>
    public static void RegisterPrefab(GameObject prefab, string prefabName, bool defaultEnabled = true)
    {
      if (string.IsNullOrWhiteSpace(prefabName)) throw new ArgumentException("prefabName is required.", nameof(prefabName));
      EnsureLayerInitialized();
      lock (_lock)
      {
        ThrowIfFinalized();
        var key = prefabName.Trim();
        _prefabEntries[key] = new PrefabEntry
        {
          Name = key,
          Prefab = prefab,
          DefaultEnabled = defaultEnabled
        };
      }
    }

    /// <summary>
    /// Track a piece by name so we can create a config toggle and include it in the snapshot.
    /// </summary>
    public static void RegisterPiece(CustomPiece customPiece, bool defaultEnabled = true)
    {
      if (customPiece == null) throw new ArgumentNullException(nameof(customPiece));
      var name = ResolvePieceName(customPiece);
      if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Could not resolve piece name from CustomPiece.");

      EnsureLayerInitialized();
      lock (_lock)
      {
        ThrowIfFinalized();

        _pieceEntries[name] = new PieceEntry
        {
          Name = name,
          DefaultEnabled = defaultEnabled,
          PieceRef = new WeakReference<Piece>(customPiece.Piece)
        };
      }
    }

  #endregion

  #region Public: Finalize + queries

    /// <summary>
    /// After all registrations:
    /// - Create per-item config entries (unless excluded)
    /// - Resolve enabled state
    /// - Register enabled prefabs (idempotent)
    /// - Apply piece enabled flags
    /// - Emit single, sorted, versioned JSON snapshot (prefabs + pieces mixed; Kind field differentiates)
    /// </summary>
    public static void FinalizeRegistration()
    {
      EnsureLayerInitialized();
      lock (_lock)
      {
        ThrowIfFinalized();

        // Prefabs: compute configurability & create config entries
        foreach (var e in _prefabEntries.Values) e.Configurable = !IsExcluded(e.Name);
        foreach (var e in _prefabEntries.Values)
        {
          if (!e.Configurable) continue;
          const string section = "Prefabs";
          e.EnabledConfig = _config.Bind(
            new ConfigDefinition(section, e.Name),
            e.DefaultEnabled,
            new ConfigDescription($"Enable/disable prefab '{e.Name}'."));
        }

        // Pieces: compute configurability & create config entries
        foreach (var e in _pieceEntries.Values) e.Configurable = !IsExcluded(e.Name);
        foreach (var e in _pieceEntries.Values)
        {
          if (!e.Configurable) continue;
          const string section = "Pieces";
          e.EnabledConfig = _config.Bind(
            new ConfigDefinition(section, e.Name),
            e.DefaultEnabled,
            new ConfigDescription($"Enable/disable piece '{e.Name}'."));
        }

        // Prefabs: resolve enabled + register
        var sortedPrefabs = _prefabEntries.Values.OrderBy(v => v.Name, StringComparer.Ordinal).ToList();
        foreach (var e in sortedPrefabs)
        {
          var enabled = e.Configurable ? e.EnabledConfig.Value : true;
          e.FinalEnabled = enabled;
          e.SnapshotHash = ComputeStableHash(e.Name);

          if (enabled && e.Prefab != null)
          {
            // Jötunn guards duplicates; safe if already added via delegate.
            PrefabManager.Instance.AddPrefab(e.Prefab);
            e.RegisteredWithPrefabManager = true;
          }
        }

        // Pieces: resolve enabled + flip m_enabled in the table
        var sortedPieces = _pieceEntries.Values.OrderBy(v => v.Name, StringComparer.Ordinal).ToList();
        foreach (var e in sortedPieces)
        {
          var enabled = e.Configurable ? e.EnabledConfig.Value : true;
          e.FinalEnabled = enabled;
          e.SnapshotHash = ComputeStableHash(e.Name);

          Piece? pieceComp = null;

          // Prefer the tracked live reference
          if (e.PieceRef.TryGetTarget(out var tracked))
            pieceComp = tracked;

          // Fallback: look it up from PieceManager if we didn't have a ref (or it got GC’d)
          if (pieceComp == null)
            pieceComp = pieceManager?.GetPiece(e.Name)?.Piece;

          if (pieceComp != null)
          {
            pieceComp.m_enabled = enabled;
          }
          else
          {
            LoggerProvider.LogWarning($"[FinalizeRegistration] Piece '{e.Name}' not found to apply enabled={enabled}");
          }
        }

        if (ModEnvironment.IsDebug)
        {
          // Emit snapshot (single mixed array, alpha-sorted)
          WriteSnapshot(sortedPrefabs, sortedPieces);
        }

        _finalized = true;
      }
    }

    /// <summary> Query enabled after finalize. </summary>
    public static bool IsEnabled(string name)
    {
      if (string.IsNullOrWhiteSpace(name)) return false;
      EnsureLayerInitialized();
      lock (_lock)
      {
        var key = name.Trim();
        if (_prefabEntries.TryGetValue(key, out var p)) return p.FinalEnabled == true;
        if (_pieceEntries.TryGetValue(key, out var c)) return c.FinalEnabled == true;
        return false;
      }
    }

    /// <summary> Names tracked by this layer (optionally only enabled). </summary>
    public static IReadOnlyList<string> GetTrackedNames(bool onlyEnabled = false)
    {
      EnsureLayerInitialized();
      lock (_lock)
      {
        var names = new List<string>();
        foreach (var e in _prefabEntries.Values)
          if (!onlyEnabled || e.FinalEnabled == true)
            names.Add(e.Name);
        foreach (var e in _pieceEntries.Values)
          if (!onlyEnabled || e.FinalEnabled == true)
            names.Add(e.Name);
        names.Sort(StringComparer.Ordinal);
        return names.ToArray();
      }
    }

  #endregion

  #region Your existing helpers (kept intact)

    /// <summary> Gets the PieceTableName and falls back to the original piece name. </summary>
    public static string GetPieceTableName()
    {
      return VehicleHammerTableRegistry.VehicleHammerTable != null
        ? VehicleHammerTableRegistry.VehicleHammerTableName
        : ValheimDefaultPieceTableName;
    }

    /// <summary> Gets the custom-table name or falls back to original hammer table. </summary>
    public static PieceTable GetPieceTable()
    {
      if (VehicleHammerTableRegistry.VehicleHammerTable != null)
        return VehicleHammerTableRegistry.VehicleHammerTable.PieceTable;

      if (_cachedValheimHammerPieceTable != null)
        return _cachedValheimHammerPieceTable;

#if DEBUG
      var allTables = PieceManager.Instance.GetPieceTables();
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
        return val;
      return PrefabNames.DEPRECATED_ValheimRaftMenuName;
    }

    /// <summary> For debugging and nuking rafts, not to be included in releases </summary>
    public static void DebugDestroyAllRaftObjects()
    {
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
      foreach (var obj in allObjects)
      {
        if (obj.name.Contains($"{PrefabNames.WaterVehicleShip}(Clone)") ||
            PrefabNames.IsHull(obj) && obj.name.Contains("(Clone)"))
        {
          var wnt = obj.GetComponent<WearNTear>();
          if (wnt) wnt.Destroy();
          else Object.Destroy(obj);
        }
      }
    }

    /// <summary> Requires assetbundle values to be set already </summary>
    private static void SetupComponents()
    {
      Vector3Logger.LoggerAPI = Logger.LogDebug;
      ConvexHullAPI.DebugMaterial = LoadValheimVehicleAssets.DoubleSidedTransparentMat;
    }

    private static void UpdatePrefabs(bool isPrefabEnabled)
    {
      foreach (var piece in raftPrefabPieces)
      {
        var pmPiece = pieceManager.GetPiece(piece.name);
        if (pmPiece == null)
        {
          Logger.LogWarning($"ValheimRaft UpdatePrefab failed: piece '{piece.name}' not found in PieceManager");
          continue;
        }

        Logger.LogDebug($"Setting m_enabled = {isPrefabEnabled} for piece '{piece.name}'");
        pmPiece.Piece.m_enabled = isPrefabEnabled;
      }

      prefabsEnabled = isPrefabEnabled;
    }

    public static void UpdatePrefabStatus()
    {
      if (!PrefabConfig.AdminsCanOnlyBuildRaft.Value && prefabsEnabled)
        return;

      Logger.LogDebug($"ValheimRAFT: UpdatePrefabStatus called. AdminsCanOnlyBuildRaft={PrefabConfig.AdminsCanOnlyBuildRaft.Value}");
      var isAdmin = SynchronizationManager.Instance.PlayerIsAdmin;
      UpdatePrefabs(isAdmin);
    }

    public static void UpdatePrefabStatus(object obj, ConfigurationSynchronizationEventArgs e)
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
      LoggerProvider.LogInfo("Registered pieces in VehicleHammerTable:");
      foreach (var piece in VehicleHammerTableRegistry.VehicleHammerTable.PieceTable.m_pieces)
      {
        LoggerProvider.LogInfo($" - {piece.name}");
      }
    }
#endif

    public static AssetBundle LoadAssetBundleFromResources(string bundleName, Assembly resourceAssembly)
    {
      if (vehicleAssetBundle != null) return vehicleAssetBundle;
      if (resourceAssembly == null)
      {
        throw new ArgumentNullException("Parameter resourceAssembly can not be null.");
      }

      string resourceName = null;
      try
      {
        resourceName = resourceAssembly.GetManifestResourceNames().Single(str => str.EndsWith(bundleName));
      }
      catch (Exception) {}

      if (resourceName == null)
      {
        Logger.LogError($"AssetBundle {bundleName} not found in assembly manifest");
        return null;
      }

      AssetBundle ret;
      using (var stream = resourceAssembly.GetManifestResourceStream(resourceName))
      {
        ret = AssetBundle.LoadFromStream(stream);
      }

      return ret;
    }

    /// <summary>
    /// Must be called from valheim-vehicles plugin
    /// </summary>
    public static void InitValheimVehiclesAssetBundle()
    {
      if (vehicleAssetBundle != null) return;
      try
      {
        var assembly = Assembly.GetExecutingAssembly();
        if (!assembly.FullName.Contains("ValheimVehicles"))
        {
          LoggerProvider.LogDebug($"Error finding correct assembly expected ValheimVehicles got {assembly.FullName}");
          assembly = Assembly.GetCallingAssembly();
        }

        // vehicleAssetBundle = AssetUtils.LoadAssetBundleFromResources("valheim-vehicles", assembly);
        vehicleAssetBundle = LoadAssetBundleFromResources("valheim-vehicles", assembly);

        // dependent on ValheimVehiclesShared
        LoadValheimRaftAssets.Instance.Init(vehicleAssetBundle);
        // dependent on ValheimVehiclesShared and RaftAssetBundle
        LoadValheimVehicleAssets.Instance.Init(vehicleAssetBundle);
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"Critical error while loading asset bundle {e.Message}");
      }
    }

    /// <summary>
    /// Initializes the bundle for ValheimVehicles and performs your existing registration pipeline.
    /// If Initialize(...) was called earlier, this will also call FinalizeRegistration() at the end.
    /// </summary>
    public static void InitAfterVanillaItemsAndPrefabsAreAvailable()
    {
      if (HasRunInitSuccessfully)
      {
        return;
      }

      try
      {

        if (!vehicleAssetBundle)
        {
          InitValheimVehiclesAssetBundle();
        }

        prefabManager = PrefabManager.Instance;
        pieceManager = PieceManager.Instance;

        LoadValheimAssets.Instance.Init(prefabManager);

        // ValheimVehicle HammerTab, must be done before items and prefab generic registrations
        VehicleHammerTableRegistry.Register();

        // must be called after assets are loaded
        PrefabRegistryHelpers.Init();

        RegisterAllItemPrefabs();
        RegisterAllPiecePrefabs();

        // must be called after RegisterAllPrefabs and AssetBundle assignment to be safe.
        SetupComponents();

        // Finalize the registry layer if wired
        if (_layerInitialized)
        {
          // Optionally auto-track current table pieces so toggles exist even if some registries haven't been updated to call AddPiece delegate yet.
          AutoTrackVehicleHammerPieces();

          FinalizeRegistration();
        }

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
        LoggerProvider.LogError($"Error during InitAfterVanillaItemsAndPrefabsAreAvailable.\n{e}");
        return;
      }

      HasRunInitSuccessfully = true;
    }

    public static void RegisterAllItemPrefabs()
    {
      // main hammer for opening the custom vehicle build menu.
      VehicleHammerItemRegistry.Register();
    }

    public static void RegisterAllPiecePrefabs()
    {
      // Critical Items
      VehiclePrefabs.Register();
      ShipSteeringWheelPrefab.Register();

      // ValheimVehicle Prefabs
      MechanismPrefabs.Register();
      CustomMeshPrefabs.Register();

      SwivelPrefab.Register();

      ShipRudderPrefabs.Register();

      CannonPrefabs.Register();

      // Raft Structure
      ShipHullPrefabRegistry.Register();

      // VehiclePrefabs
      VehiclePiecesPrefab.Register();

      RamPrefabRegistry.Register();

      // new way to register components
      CustomVehicleMastRegistry.Register();

      // register experimental prefabs.
      ExperimentalPrefabRegistry.Register();

      // sails and masts
      SailPrefabs.Register();

      // Rope items
      RopeAnchorPrefabRegistry.Register();
      AnchorPrefabs.Register();

      // pier components
      PierPrefabRegistry.Register();

      // Ramps
      RampPrefabRegistry.Register();
      // Floors
      DirtFloorPrefabRegistry.Register();
    }

  #endregion

  #region Internals

    private static void EnsureLayerInitialized()
    {
      if (!_layerInitialized)
        throw new InvalidOperationException("PrefabRegistryController.Initialize must be called before using the registry layer.");
    }

    private static void ThrowIfFinalized()
    {
      if (_finalized)
        throw new InvalidOperationException("FinalizeRegistration has already run; no further registrations are allowed this session.");
    }

    private static void ParseExclusions()
    {
      _excludedNames = new HashSet<string>(SplitList(_excludedPrefabNamesCsv?.Value), StringComparer.Ordinal);
      _excludedRegex = SplitList(_excludedPrefabRegexCsv?.Value)
        .Select(p =>
        {
          try { return new Regex(p, RegexOptions.Compiled | RegexOptions.CultureInvariant); }
          catch { return null; }
        })
        .Where(r => r != null)
        .ToList();
    }

    private static IEnumerable<string> SplitList(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw)) yield break;
      var parts = raw.Split(new[] { ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var p in parts)
      {
        var s = p.Trim();
        if (!string.IsNullOrEmpty(s)) yield return s;
      }
    }

    private static bool IsExcluded(string name)
    {
      if (_excludedNames.Contains(name)) return true;
      foreach (var rx in _excludedRegex)
      {
        if (rx.IsMatch(name)) return true;
      }
      return false;
    }

    private static int ComputeStableHash(string s)
    {
      unchecked
      {
        var hash = 23;
        for (var i = 0; i < s.Length; i++)
          hash = hash * 31 + s[i];
        return hash;
      }
    }

    private static void WriteSnapshot(List<PrefabEntry> sortedPrefabs, List<PieceEntry> sortedPieces)
    {
      var items = new List<SnapshotItem>(sortedPrefabs.Count + sortedPieces.Count);

      foreach (var e in sortedPrefabs)
      {
        items.Add(new SnapshotItem
        {
          Name = e.Name,
          Kind = "Prefab",
          Enabled = e.FinalEnabled ?? false,
          Configurable = e.Configurable,
          Hash = e.SnapshotHash
        });
      }

      foreach (var e in sortedPieces)
      {
        items.Add(new SnapshotItem
        {
          Name = e.Name,
          Kind = "Piece",
          Enabled = e.FinalEnabled ?? false,
          Configurable = e.Configurable,
          Hash = e.SnapshotHash
        });
      }

      // single alpha-numeric (Ordinal) sort so diffs are stable regardless of registration order
      items = items
        .OrderBy(i => i.Name, StringComparer.Ordinal)
        .ThenBy(i => i.Kind, StringComparer.Ordinal)
        .ToList();

      var snapshot = new Snapshot
      {
        ModGuid = _modGuid,
        ModVersion = _modVersion,
        GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
        Items = items
      };

      var fileName = $"prefabs-{SanitizeForFile(_modVersion)}.json";
      var path = Path.Combine(_snapshotDir, fileName);

      var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
      File.WriteAllText(path, json, Encoding.UTF8);
    }

    private static string SanitizeForFile(string s)
    {
      var invalid = Path.GetInvalidFileNameChars();
      var sb = new StringBuilder(s.Length);
      foreach (var c in s)
        sb.Append(invalid.Contains(c) ? '_' : c);
      return sb.ToString();
    }

    /// <summary> Use Piece + PiecePrefab (no piece.Prefab). </summary>
    private static string ResolvePieceName(CustomPiece piece)
    {
      if (piece == null) return null;
      if (piece.Piece != null && !string.IsNullOrWhiteSpace(piece.Piece.name))
        return piece.Piece.name.Trim();
      if (piece.PiecePrefab != null && !string.IsNullOrWhiteSpace(piece.PiecePrefab.name))
        return piece.PiecePrefab.name.Trim();
      return null;
    }

    /// <summary>
    /// Convenience to auto-track all pieces in our custom hammer table, so config toggles exist
    /// even before every registry is migrated to call AddPiece delegate.
    /// </summary>
    private static void AutoTrackVehicleHammerPieces()
    {
      if (pieceManager == null) return;

      var table = GetPieceTable();
      if (table == null || table.m_pieces == null) return;

      foreach (var p in table.m_pieces)
      {
        if (p == null) continue;
        var piece = p.GetComponent<CustomPiece>();
        if (piece == null) continue;
        RegisterPiece(piece, true);
      }
    }

  #endregion

  }
}