using BepInEx.Configuration;
using ValheimScalability.Rendering.Clustering;

namespace ValheimScalability.BepInExConfig;

/// <summary>
/// Local/client-only ValheimScalability settings.
/// These are intentionally not ServerSync values because they only affect local rendering.
/// </summary>
public static class ValheimScalabilityConfig
{
  private const string SectorClusteringSection = "Rendering.SectorClustering";
  private const string FilterSection = "Rendering.SectorClustering.Filters";
  private const string InstancingSection = "Rendering.SectorClustering.Instancing";
  private const string CacheSection = "Rendering.SectorClustering.Cache";

  private static bool _isBound;

  public static bool IsBound => _isBound;

  // Core / presentation.
  public static ConfigEntry<ClusterPresentationMode> ClusterRenderingMode { get; private set; } = null!;
  public static ConfigEntry<int> SectorCellDivisions { get; private set; } = null!;
  public static ConfigEntry<float> PlayerUnclusterRadius { get; private set; } = null!;
  public static ConfigEntry<float> PlayerProximityPollInterval { get; private set; } = null!;
  public static ConfigEntry<bool> RequireCameraVisibilityToUncluster { get; private set; } = null!;
  public static ConfigEntry<bool> AlwaysClusterPieceLayerNearPlayer { get; private set; } = null!;
  public static ConfigEntry<bool> FullClusterUsesSingleCellPerSector { get; private set; } = null!;
  public static ConfigEntry<bool> LogBatchDiagnostics { get; private set; } = null!;

  // Lifecycle.
  public static ConfigEntry<float> SectorMaintenanceInterval { get; private set; } = null!;
  public static ConfigEntry<float> SectorInitialBuildDelay { get; private set; } = null!;
  public static ConfigEntry<float> SectorRegistrationSettleDelay { get; private set; } = null!;
  public static ConfigEntry<float> SectorRemovalRebuildDelay { get; private set; } = null!;
  public static ConfigEntry<float> SectorDamageCooldown { get; private set; } = null!;
  public static ConfigEntry<float> SectorVisibilityHeight { get; private set; } = null!;
  public static ConfigEntry<bool> SectorUseLowestLod { get; private set; } = null!;
  public static ConfigEntry<bool> SectorSkipPropertyBlockRenderers { get; private set; } = null!;
  public static ConfigEntry<int> SectorMinimumEstimatedDrawCallSavings { get; private set; } = null!;

  // Warm-zone cache. Limits apply only to inactive cached sectors, never the currently active area.
  public static ConfigEntry<bool> WarmCacheEnabled { get; private set; } = null!;
  public static ConfigEntry<int> MaxWarmZones { get; private set; } = null!;
  public static ConfigEntry<int> MaxWarmMeshMemoryMB { get; private set; } = null!;
  public static ConfigEntry<int> MaxSingleWarmZoneMB { get; private set; } = null!;
  public static ConfigEntry<float> WarmZoneRetentionSeconds { get; private set; } = null!;
  public static ConfigEntry<int> FrequentVisitThreshold { get; private set; } = null!;
  public static ConfigEntry<int> MaxFrequentWarmZones { get; private set; } = null!;
  public static ConfigEntry<float> FrequentWarmZoneRetentionSeconds { get; private set; } = null!;
  public static ConfigEntry<bool> LogCacheDiagnostics { get; private set; } = null!;

  // User filtering. Names are case-insensitive substring matches.
  public static ConfigEntry<string> IncludedLayers { get; private set; } = null!;
  public static ConfigEntry<string> ExcludedLayers { get; private set; } = null!;
  public static ConfigEntry<string> IncludedObjectNames { get; private set; } = null!;
  public static ConfigEntry<string> ExcludedObjectNames { get; private set; } = null!;
  public static ConfigEntry<string> IncludedObjectRegexList { get; private set; } = null!;
  public static ConfigEntry<string> ExcludedObjectRegexList { get; private set; } = null!;
  public static ConfigEntry<string> ExcludedMaterialNames { get; private set; } = null!;
  public static ConfigEntry<string> ExcludedMaterialRegexList { get; private set; } = null!;
  public static ConfigEntry<string> ExcludedShaderNames { get; private set; } = null!;
  public static ConfigEntry<string> ExcludedShaderRegexList { get; private set; } = null!;

  // Instancing / wind.
  public static ConfigEntry<bool> EnableGpuInstancing { get; private set; } = null!;
  public static ConfigEntry<string> InstancedObjectNameHints { get; private set; } = null!;
  public static ConfigEntry<string> InstancedMaterialNameHints { get; private set; } = null!;
  public static ConfigEntry<string> InstancedShaderNameHints { get; private set; } = null!;
  public static ConfigEntry<string> InstancedCandidateRegexList { get; private set; } = null!;
  public static ConfigEntry<string> WindShaderPropertyNames { get; private set; } = null!;

  public static ClusterPresentationMode Mode =>
    ClusterRenderingMode?.Value ?? ClusterPresentationMode.Adaptive;

  public static int CellDivisions =>
    SectorCellDivisions?.Value ?? 1;

  public static float UnclusterRadius =>
    PlayerUnclusterRadius?.Value ?? 18f;

  public static float ProximityPollInterval =>
    PlayerProximityPollInterval?.Value ?? 0.15f;

  public static bool OnlyUnclusterVisibleCells =>
    RequireCameraVisibilityToUncluster?.Value ?? true;

  public static bool KeepPieceLayerClusteredNearPlayer =>
    AlwaysClusterPieceLayerNearPlayer?.Value ?? false;

  public static bool UseSingleCellInFullCluster =>
    FullClusterUsesSingleCellPerSector?.Value ?? true;

  public static bool IsBatchDiagnosticsEnabled =>
    LogBatchDiagnostics?.Value ?? false;

  public static float MaintenanceInterval =>
    SectorMaintenanceInterval?.Value ?? 0.20f;

  public static float InitialBuildDelay =>
    SectorInitialBuildDelay?.Value ?? 0.75f;

  public static float RegistrationSettleDelay =>
    SectorRegistrationSettleDelay?.Value ?? 0.35f;

  public static float RemovalRebuildDelay =>
    SectorRemovalRebuildDelay?.Value ?? 0.20f;

  public static float DamageCooldown =>
    SectorDamageCooldown?.Value ?? 8f;

  public static float VisibilityHeight =>
    SectorVisibilityHeight?.Value ?? 2048f;

  public static bool UseLowestLod =>
    SectorUseLowestLod?.Value ?? true;

  public static bool SkipPropertyBlockRenderers =>
    SectorSkipPropertyBlockRenderers?.Value ?? true;

  public static int MinimumEstimatedDrawCallSavings =>
    SectorMinimumEstimatedDrawCallSavings?.Value ?? 1;

  public static bool IsWarmCacheEnabled =>
    WarmCacheEnabled?.Value ?? true;

  public static int WarmZoneLimit =>
    MaxWarmZones?.Value ?? 96;

  public static long WarmMeshMemoryLimitBytes =>
    (long) (MaxWarmMeshMemoryMB?.Value ?? 768) *
    1024L *
    1024L;

  public static long SingleWarmZoneMemoryLimitBytes =>
    (long) (MaxSingleWarmZoneMB?.Value ?? 256) *
    1024L *
    1024L;

  public static float WarmRetentionSeconds =>
    WarmZoneRetentionSeconds?.Value ?? 300f;

  public static int FrequentZoneVisitThreshold =>
    FrequentVisitThreshold?.Value ?? 3;

  public static int FrequentWarmZoneLimit =>
    MaxFrequentWarmZones?.Value ?? 16;

  public static float FrequentWarmRetentionSeconds =>
    FrequentWarmZoneRetentionSeconds?.Value ?? 1800f;

  public static bool IsCacheDiagnosticsEnabled =>
    LogCacheDiagnostics?.Value ?? false;

  public static bool IsGpuInstancingEnabled =>
    EnableGpuInstancing?.Value ?? false;

  public static void Bind(ConfigFile config)
  {
    if (_isBound)
    {
      return;
    }

    ClusterRenderingMode = config.Bind(
      SectorClusteringSection,
      "Mode",
      ClusterPresentationMode.Adaptive,
      "Off uses original renderers only. Adaptive restores originals near the local player and optimizes distant cells. FullCluster ignores player proximity and uses optimized rendering everywhere it is safe.");

    SectorCellDivisions = config.Bind(
      SectorClusteringSection,
      "CellDivisionsPerSector",
      1,
      new ConfigDescription(
        "Subdivides each Valheim sector into independent rendering cells. 1 gives the strongest batching and is recommended by default. Higher values improve Adaptive-mode culling/proximity granularity at the cost of more render batches.",
        new AcceptableValueRange<int>(1, 8)));

    PlayerUnclusterRadius = config.Bind(
      SectorClusteringSection,
      "PlayerUnclusterRadius",
      18f,
      new ConfigDescription(
        "Adaptive mode only. Cardinal/Manhattan world-space radius around the local player that restores original renderers.",
        new AcceptableValueRange<float>(0f, 128f)));

    PlayerProximityPollInterval = config.Bind(
      SectorClusteringSection,
      "PlayerProximityPollInterval",
      0.15f,
      new ConfigDescription(
        "Seconds between local-player proximity checks.",
        new AcceptableValueRange<float>(0.05f, 2f)));

    RequireCameraVisibilityToUncluster = config.Bind(
      SectorClusteringSection,
      "RequireCameraVisibilityToUncluster",
      true,
      "Adaptive mode: only restore nearby cells when they are also in the local camera frustum.");

    AlwaysClusterPieceLayerNearPlayer = config.Bind(
      SectorClusteringSection,
      "AlwaysClusterPieceLayerNearPlayer",
      false,
      "Experimental. Keeps eligible 'piece' layer batches optimized even when an Adaptive cell is near the player.");

    FullClusterUsesSingleCellPerSector = config.Bind(
      SectorClusteringSection,
      "FullClusterUsesSingleCellPerSector",
      true,
      "Recommended. FullCluster has no player-proximity reason to subdivide a sector, so use one batch cell per Valheim sector to minimize generated renderers and draw-call fragmentation.");

    LogBatchDiagnostics = config.Bind(
      SectorClusteringSection,
      "LogBatchDiagnostics",
      false,
      "Logs combined-batch keys/counts when cells rebuild. Useful for diagnosing materials that unexpectedly split into multiple batches.");

    SectorMaintenanceInterval = config.Bind(
      SectorClusteringSection,
      "MaintenanceInterval",
      0.20f,
      new ConfigDescription(
        "Seconds between build/rebuild/cleanup maintenance passes.",
        new AcceptableValueRange<float>(0.05f, 2f)));

    SectorInitialBuildDelay = config.Bind(
      SectorClusteringSection,
      "InitialBuildDelay",
      0.75f,
      new ConfigDescription(
        "Delay before a newly-created cell can build its first optimized representation.",
        new AcceptableValueRange<float>(0f, 5f)));

    SectorRegistrationSettleDelay = config.Bind(
      SectorClusteringSection,
      "RegistrationSettleDelay",
      0.35f,
      new ConfigDescription(
        "Debounce after additional objects register into a loaded cell.",
        new AcceptableValueRange<float>(0.05f, 2f)));

    SectorRemovalRebuildDelay = config.Bind(
      SectorClusteringSection,
      "RemovalRebuildDelay",
      0.20f,
      new ConfigDescription(
        "Debounce before rebuilding when previously-batched source geometry disappears.",
        new AcceptableValueRange<float>(0f, 2f)));

    SectorDamageCooldown = config.Bind(
      SectorClusteringSection,
      "DamageCooldown",
      8f,
      new ConfigDescription(
        "Seconds after the latest damage before an affected cell may rebuild. Repeated damage extends this timer.",
        new AcceptableValueRange<float>(0f, 60f)));

    SectorVisibilityHeight = config.Bind(
      SectorClusteringSection,
      "VisibilityHeight",
      2048f,
      new ConfigDescription(
        "Vertical size used only for cell camera-frustum checks.",
        new AcceptableValueRange<float>(64f, 8192f)));

    SectorUseLowestLod = config.Bind(
      SectorClusteringSection,
      "UseLowestLod",
      true,
      "Uses the lowest available LOD renderer when building far-cell optimized representations.");

    SectorSkipPropertyBlockRenderers = config.Bind(
      SectorClusteringSection,
      "SkipMaterialPropertyBlockRenderers",
      true,
      "Recommended. Keeps renderers using MaterialPropertyBlocks original because arbitrary per-renderer shader state cannot be safely merged.");

    SectorMinimumEstimatedDrawCallSavings = config.Bind(
      SectorClusteringSection,
      "MinimumEstimatedDrawCallSavings",
      1,
      new ConfigDescription(
        "A cell must save at least this many estimated draw submissions before its optimized representation is used.",
        new AcceptableValueRange<int>(1, 1024)));

    // Warm-zone cache.
    WarmCacheEnabled = config.Bind(
      CacheSection,
      "Enabled",
      true,
      "Keeps a bounded set of recently/frequently used inactive sector clusters in memory for fast revisits and teleports. Active sectors are never limited by this cache.");

    MaxWarmZones = config.Bind(
      CacheSection,
      "MaxWarmZones",
      96,
      new ConfigDescription(
        "Maximum number of inactive sector clusters retained in the warm cache. 0 = unlimited by count. Memory and TTL limits still apply.",
        new AcceptableValueRange<int>(0, 4096)));

    MaxWarmMeshMemoryMB = config.Bind(
      CacheSection,
      "MaxWarmMeshMemoryMB",
      768,
      new ConfigDescription(
        "Approximate generated-mesh memory budget for inactive warm sectors only. Active sectors are not counted. 0 = unlimited. Increase this on high-memory systems if you frequently teleport between very large builds.",
        new AcceptableValueRange<int>(0, 32768)));

    MaxSingleWarmZoneMB = config.Bind(
      CacheSection,
      "MaxSingleWarmZoneMB",
      256,
      new ConfigDescription(
        "Do not retain one inactive sector in the warm cache if its generated rendering data exceeds this approximate size. The sector still works while active and will rebuild when revisited. 0 = unlimited.",
        new AcceptableValueRange<int>(0, 8192)));

    WarmZoneRetentionSeconds = config.Bind(
      CacheSection,
      "WarmZoneRetentionSeconds",
      300f,
      new ConfigDescription(
        "Normal inactive sectors may remain warm for this many seconds before becoming eligible for cold eviction.",
        new AcceptableValueRange<float>(0f, 86400f)));

    FrequentVisitThreshold = config.Bind(
      CacheSection,
      "FrequentVisitThreshold",
      3,
      new ConfigDescription(
        "A sector visited at least this many distinct times during the current world session is treated as frequently used and receives longer warm-cache retention.",
        new AcceptableValueRange<int>(1, 1000)));

    MaxFrequentWarmZones = config.Bind(
      CacheSection,
      "MaxFrequentWarmZones",
      16,
      new ConfigDescription(
        "Maximum number of frequently visited inactive sectors that receive extended retention. 0 disables the dedicated frequent-zone allowance.",
        new AcceptableValueRange<int>(0, 1024)));

    FrequentWarmZoneRetentionSeconds = config.Bind(
      CacheSection,
      "FrequentWarmZoneRetentionSeconds",
      1800f,
      new ConfigDescription(
        "Retention time for frequently visited inactive sectors.",
        new AcceptableValueRange<float>(0f, 86400f)));

    LogCacheDiagnostics = config.Bind(
      CacheSection,
      "LogCacheDiagnostics",
      false,
      "Logs warm-cache entry, reactivation, eviction, and approximate generated-mesh memory usage.");

    // Layer filters.
    IncludedLayers = config.Bind(
      FilterSection,
      "IncludedLayers",
      "Default,static_solid,Default_small,piece",
      "Comma-separated layer names or numeric layer IDs eligible for batching. Empty means all layers. Example: Default,static_solid,piece");

    ExcludedLayers = config.Bind(
      FilterSection,
      "ExcludedLayers",
      "",
      "Comma-separated layer names or numeric layer IDs that must remain original. Excludes override IncludedLayers.");

    // Object filters.
    IncludedObjectNames = config.Bind(
      FilterSection,
      "IncludedObjectNames",
      "",
      "Optional comma-separated case-insensitive object-name substrings. Empty means all object names are eligible.");

    ExcludedObjectNames = config.Bind(
      FilterSection,
      "ExcludedObjectNames",
      "portal,door,chest,cart,wheel,mechanism,Destruction,Portal_destruction,Destruction_Cube,vehicle_water_mesh,animated,energy_level,banner,flag,cloth,tapestry,drape,pennant,sail",
      "Comma-separated case-insensitive object-name substrings that must remain original. Checked against both the registered root and child renderer GameObject names.");

    IncludedObjectRegexList = config.Bind(
      FilterSection,
      "IncludedObjectRegexList",
      "",
      "Optional semicolon-separated regular expressions for object names. Empty means no regex include restriction. When name/regex includes are configured, matching either include mechanism is sufficient.");

    ExcludedObjectRegexList = config.Bind(
      FilterSection,
      "ExcludedObjectRegexList",
      "",
      "Semicolon-separated regular expressions for object names. Example: ^.*portal.*$;^MyMod_Animated_.*$");

    ExcludedMaterialNames = config.Bind(
      FilterSection,
      "ExcludedMaterialNames",
      "",
      "Comma-separated case-insensitive material-name substrings that must remain original.");

    ExcludedMaterialRegexList = config.Bind(
      FilterSection,
      "ExcludedMaterialRegexList",
      "",
      "Semicolon-separated regular expressions for material names that must remain original.");

    ExcludedShaderNames = config.Bind(
      FilterSection,
      "ExcludedShaderNames",
      "",
      "Comma-separated case-insensitive shader-name substrings that must remain original.");

    ExcludedShaderRegexList = config.Bind(
      FilterSection,
      "ExcludedShaderRegexList",
      "",
      "Semicolon-separated regular expressions for shader names that must remain original.");

    // Instancing / wind handling.
    EnableGpuInstancing = config.Bind(
      InstancingSection,
      "Enabled",
      false,
      "Experimental. Routes vegetation/wind-style renderers through manual GPU instancing. Disabled by default because Valheim/Unity may already batch vegetation efficiently; wind candidates stay original when this is off.");

    InstancedObjectNameHints = config.Bind(
      InstancingSection,
      "ObjectNameHints",
      "tree,bush,sapling,beech,birch,fir,pine",
      "Comma-separated case-insensitive object-name hints that prefer GPU instancing. Dynamic Rigidbody objects are still rejected.");

    InstancedMaterialNameHints = config.Bind(
      InstancingSection,
      "MaterialNameHints",
      "leaf,leaves,branch,foliage,tree,bush,vegetation",
      "Comma-separated case-insensitive material-name hints that prefer GPU instancing.");

    InstancedShaderNameHints = config.Bind(
      InstancingSection,
      "ShaderNameHints",
      "vegetation,tree,foliage,wind,plant",
      "Comma-separated case-insensitive shader-name hints that prefer GPU instancing.");

    InstancedCandidateRegexList = config.Bind(
      InstancingSection,
      "CandidateRegexList",
      "",
      "Optional semicolon-separated regexes matched against a descriptor containing root, renderer, material and shader names. Matching routes the renderer to GPU instancing.");

    WindShaderPropertyNames = config.Bind(
      InstancingSection,
      "WindShaderPropertyNames",
      "_Wind,_WindStrength,_WindSpeed,_WindIntensity,_Sway,_SwaySpeed,_WindData",
      "Comma-separated shader property names. A material exposing one of these properties is treated as a wind/instancing candidate.");

    _isBound = true;

    // Parsed filters are cached for build performance.
    SubscribeRebuildSetting(ClusterRenderingMode);
    SubscribeRebuildSetting(FullClusterUsesSingleCellPerSector);
    SubscribeRebuildSetting(IncludedLayers);
    SubscribeRebuildSetting(ExcludedLayers);
    SubscribeRebuildSetting(IncludedObjectNames);
    SubscribeRebuildSetting(ExcludedObjectNames);
    SubscribeRebuildSetting(IncludedObjectRegexList);
    SubscribeRebuildSetting(ExcludedObjectRegexList);
    SubscribeRebuildSetting(ExcludedMaterialNames);
    SubscribeRebuildSetting(ExcludedMaterialRegexList);
    SubscribeRebuildSetting(ExcludedShaderNames);
    SubscribeRebuildSetting(ExcludedShaderRegexList);
    SubscribeRebuildSetting(EnableGpuInstancing);
    SubscribeRebuildSetting(InstancedObjectNameHints);
    SubscribeRebuildSetting(InstancedMaterialNameHints);
    SubscribeRebuildSetting(InstancedShaderNameHints);
    SubscribeRebuildSetting(InstancedCandidateRegexList);
    SubscribeRebuildSetting(WindShaderPropertyNames);
    SubscribeRebuildSetting(SectorUseLowestLod);
    SubscribeRebuildSetting(SectorSkipPropertyBlockRenderers);
    SubscribeRebuildSetting(SectorMinimumEstimatedDrawCallSavings);
    SubscribeRebuildSetting(SectorCellDivisions);
  }

  private static void SubscribeRebuildSetting<T>(ConfigEntry<T> entry)
  {
    entry.SettingChanged += (_, _) =>
    {
      ClusterBatchFilter.Invalidate();
      SectorMeshClusterManager.Instance?.RebuildAllForConfigurationChange();
    };
  }
}
