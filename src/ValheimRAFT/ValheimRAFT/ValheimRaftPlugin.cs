using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using BepInEx.Bootstrap;
using DynamicLocations;
using DynamicLocations.API;
using DynamicLocations.Controllers;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimVehicles;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Constants;
using ValheimVehicles.Injections;
using ValheimVehicles.ModSupport;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Providers;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Patches;
using ZdoWatcher;
using Zolantris.Shared;
using Zolantris.Shared.BepInExAutoDoc;
namespace ValheimRAFT;

internal abstract class PluginDependencies
{
  public const string JotunnModGuid = Jotunn.Main.ModGuid;
}

/// <summary>
/// ValheimRAFTPlugin is mostly a wrapper around ValheimVehicles which was added >=2.0.0. As of 3.2.0 ValheimVehicles contains 99% of the code.
/// </summary>
// [SentryDSN()]
[BepInPlugin(ModGuid, ModName, Version)]
[BepInDependency(ZdoWatcherPlugin.ModGuid)]
[BepInDependency(DynamicLocationsPlugin.BepInGuid,
  DynamicLocationsPlugin.Version)]
[BepInDependency(PluginDependencies.JotunnModGuid)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod,
  VersionStrictness.Minor)]
public class ValheimRaftPlugin : BaseUnityPlugin
{
  // ReSharper disable MemberCanBePrivate.Global
  public const string Author = "zolantris";
  public const string Version = "3.2.1";
  public const string ModName = "ValheimRAFT";
  public const string ModNameBeta = "ValheimRAFTBETA";
  public const string ModGuid = $"{Author}.{ModName}";
  public static string HarmonyGuid => ModGuid;
  public const string ModDescription =
    "Valheim Mod for building on the sea, requires Jotunn to be installed.";
  public const string CopyRight = "Copyright © 2023-2024, GNU-v3 licensed";
  // ReSharper restore MemberCanBePrivate.Global
  
  public static ValheimRaftPlugin Instance { get; private set; }
  
  public ConfigEntry<bool> EnableMetrics { get; set; }

  private ConfigDescription CreateConfigDescription(string description,
    bool isAdmin = false,
    bool isAdvanced = false, AcceptableValueBase? acceptableValues = null)
  {
    return new ConfigDescription(
      description,
      acceptableValues,
      new ConfigurationManagerAttributes()
      {
        IsAdminOnly = isAdmin,
        IsAdvanced = isAdvanced
      }
    );
  }

  [UsedImplicitly]
  public static string GetVersion()
  {
    return Version;
  }


  private void CreateBaseConfig()
  {
    EnableMetrics = Config.Bind("Debug",
      "Enable Sentry Metrics (requires sentryUnityPlugin)", true,
      CreateConfigDescription(
        "Enable sentry debug logging. Requires sentry logging plugin installed to work. Sentry Logging plugin will make it easier to troubleshoot raft errors and detect performance bottlenecks. The bare minimum is collected, and only data related to ValheimRaft. See https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT#logging-metrics for more details about what is collected"));
  }

  /*
   * aggregates all config creators.
   *
   * Future plans:
   * - Abstract specific config directly into related files and call init here to set those values in the associated classes.
   * - Most likely those items will need to be "static" values.
   * - Add a watcher so those items can take the new config and process it as things update.
   */

  private void CreateConfig()
  {
    CreateBaseConfig();
    ValheimVehiclesPlugin.CreateConfigFromRAFTConfig(Config);
  }

  internal void ApplyMetricIfAvailable()
  {
#if DEBUG
    
    var @namespace = "SentryUnityWrapper";
    var @pluginClass = "SentryUnityWrapperPlugin";
    Logger.LogDebug(
      $"contains sentryunitywrapper: {Chainloader.PluginInfos.ContainsKey("zolantris.SentryUnityWrapper")}");


    if (!EnableMetrics.Value ||
        !Chainloader.PluginInfos.ContainsKey("zolantris.SentryUnityWrapper"))
      return;
    Logger.LogDebug("Made it to sentry check");
    SentryMetrics.ApplyMetrics();
#endif
  }

  public void Awake()
  {    
    Instance = this;

    // RegisterHost command might need to be called before AddComponent
    // - ValheimRAFT_API allows for accessing ValheimRAFT old apis through reflection

    // critical for valheim api matching which must be called after config init.
    // must be done before any other services that require LoggerProvider otherwise it will not work.
    ProviderInitializers.InitProviders(Logger);
    
    ValheimVehicles.Compat.ValheimRAFT_API.RegisterHost(Instance);
    gameObject.AddComponent<BatchedLogger>();

    CreateConfig();
    PatchController.Apply(HarmonyGuid);

    AddPhysicsSettings();

    RegisterVehicleConsoleCommands();




    PrefabManager.OnVanillaPrefabsAvailable += () =>
    {
      // do not load custom textures on a dedicated server. This will do nothing but cause an error.
      if (ZNet.instance == null || ZNet.instance.IsDedicated() == false)
      {
        LoadCustomTextures();
      }
      AddCustomItemsAndPieces();
    };

    ZdoWatcherDelegate.RegisterToZdoManager();
    AddModSupport();

    var renderPipeline = GraphicsSettings.defaultRenderPipeline;
    if (renderPipeline != null)
      Logger.LogDebug(
        $"Valheim GameEngine is using: <{renderPipeline}> graphics pipeline ");

    gameObject.AddComponent<ValheimVehiclesPlugin>();
  }

  private void OnDestroy()
  {
    Localization.OnLanguageChange -= ModTranslations.UpdateTranslations;
    Localization.OnLanguageChange -= VehicleAnchorMechanismController.setLocalizedStates;
    PatchController.UnpatchSelf();
  }

  public void AddModSupport()
  {
    AddModSupportDynamicLocations();
  }

  /// <summary>
  /// DynamicLocations to allow for respawning on moving boats, or be placed on the boat when logging in.
  /// </summary>
  public void AddModSupportDynamicLocations()
  {
    var dynamicLocationLoginIntegrationConfig =
      DynamicLoginIntegration.CreateConfig(this, PrefabNames.WaterVehicleShip);
    var integrationInstance =
      new DynamicLocationsLoginIntegration(
        dynamicLocationLoginIntegrationConfig);
    LoginAPIController.AddLoginApiIntegration(
      integrationInstance);
  }


  // this will be removed when vehicles becomes independent of valheim raft.
  public void RegisterVehicleConsoleCommands()
  {
    CommandManager.Instance.AddConsoleCommand(new VehicleCommands());
  }

  private void Start()
  {
  
    // SentryLoads after
    ApplyMetricIfAvailable();
    
    if (ModEnvironment.IsDebug)
      new BepInExConfigAutoDoc().Generate(this, Config, "ValheimRAFT");
  }

  /**
   * Important for raft collisions to only include water and landmass colliders.
   *
   * Other collisions on the piece level are not handled on the LayerHelpers.CustomRaftLayer
   *
   * todo remove LayerHelpers.CustomRaftLayer and use the VehicleLayer instead.
   * - Requires adding explicit collision ignores for the rigidbody attached to VehicleInstance (m_body)
   */
  private void AddPhysicsSettings()
  {
    var layer = LayerMask.NameToLayer("vehicle");

    for (var index = 0; index < 32; ++index)
      Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer, index,
        Physics.GetIgnoreLayerCollision(layer, index));

    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("vehicle"),
      true);

    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("piece"),
      false);

    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("character"),
      true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("smoke"),
      true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("character_ghost"), true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("weapon"),
      true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("blocker"),
      true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("pathblocker"), true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("viewblock"),
      true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("character_net"), true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("character_noenv"), true);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("Default_small"), false);
    Physics.IgnoreLayerCollision(LayerHelpers.CustomRaftLayer,
      LayerMask.NameToLayer("Default"),
      false);
  }

  private void LoadCustomTextures()
  {
    var sails = CustomTextureGroup.Load("Sails");
    foreach (var texture3 in sails.Textures)
    {
      texture3.Texture.wrapMode = TextureWrapMode.Clamp;
      if ((bool)texture3.Normal)
        texture3.Normal.wrapMode = TextureWrapMode.Clamp;
    }

    var patterns = CustomTextureGroup.Load("Patterns");
    foreach (var texture2 in patterns.Textures)
    {
      texture2.Texture.filterMode = FilterMode.Point;
      texture2.Texture.wrapMode = TextureWrapMode.Repeat;
      if ((bool)texture2.Normal)
        texture2.Normal.wrapMode = TextureWrapMode.Repeat;
    }

    var logos = CustomTextureGroup.Load("Logos");
    foreach (var texture in logos.Textures)
    {
      texture.Texture.wrapMode = TextureWrapMode.Clamp;
      if ((bool)texture.Normal) texture.Normal.wrapMode = TextureWrapMode.Clamp;
    }
  }

  private void AddCustomItemsAndPieces()
  {
    PrefabRegistryController.InitAfterVanillaItemsAndPrefabsAreAvailable();
  }
}