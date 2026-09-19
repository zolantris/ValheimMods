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

  private static bool _isBound;

  public static bool IsBound => _isBound;

  // Core / presentation.
  public static ConfigEntry<bool> SectorClusteringEnabled { get; private set; } = null!;
  public static ConfigEntry<ClusterPresentationMode> SectorPresentationMode { get; private set; } = null!;
  public static ConfigEntry<int> SectorCellDivisions { get; private set; } = null!;
  public static ConfigEntry<float> PlayerUnclusterRadius { get; private set; } = null!;
  public static ConfigEntry<float> PlayerProximityPollInterval { get; private set; } = null!;
  public static ConfigEntry<bool> RequireCameraVisibilityToUncluster { get; private set; } = null!;
  public static ConfigEntry<bool> AlwaysClusterPieceLayerNearPlayer { get; private set; } = null!;

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

  public static bool IsSectorClusteringEnabled =>
    SectorClusteringEnabled?.Value ?? true;

  public static ClusterPresentationMode PresentationMode =>
    SectorPresentationMode?.Value ?? ClusterPresentationMode.Adaptive;

  public static int CellDivisions =>
    SectorCellDivisions?.Value ?? 4;

  public static float UnclusterRadius =>
    PlayerUnclusterRadius?.Value ?? 18f;

  public static float ProximityPollInterval =>
    PlayerProximityPollInterval?.Value ?? 0.15f;

  public static bool OnlyUnclusterVisibleCells =>
    RequireCameraVisibilityToUncluster?.Value ?? true;

  public static bool KeepPieceLayerClusteredNearPlayer =>
    AlwaysClusterPieceLayerNearPlayer?.Value ?? false;

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

  public static bool IsGpuInstancingEnabled =>
    EnableGpuInstancing?.Value ?? true;

  public static void Bind(ConfigFile config)
  {
    if (_isBound)
    {
      return;
    }

    SectorClusteringEnabled = config.Bind(
      SectorClusteringSection,
      "Enabled",
      true,
      "Enables client-side sector/cell optimized rendering. Registration remains active while disabled so it can be toggled at runtime.");

    SectorPresentationMode = config.Bind(
      SectorClusteringSection,
      "PresentationMode",
      ClusterPresentationMode.Adaptive,
      "Adaptive restores originals near the local player. FullCluster ignores player proximity. OriginalsOnly never presents generated/instanced batches.");

    SectorCellDivisions = config.Bind(
      SectorClusteringSection,
      "CellDivisionsPerSector",
      4,
      new ConfigDescription(
        "Subdivides each Valheim sector into independent rendering cells. 4 = 4x4 = 16 cells.",
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
      "portal,door,chest,cart,wheel,mechanism,Destruction,Portal_destruction,Destruction_Cube,vehicle_water_mesh,animated,energy_level",
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
      true,
      "Routes vegetation/wind-style renderers to GPU instancing instead of Mesh.CombineMeshes. If instancing is unsupported, those renderers stay original.");

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