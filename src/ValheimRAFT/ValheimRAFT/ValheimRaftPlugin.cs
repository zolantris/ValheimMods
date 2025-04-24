using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Bootstrap;
using DynamicLocations;
using DynamicLocations.API;
using DynamicLocations.Controllers;
using DynamicLocations.Structs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using ValheimRAFT.Config;
using ValheimRAFT.Patches;
using ValheimRAFT.Util;
using ValheimVehicles;
using ValheimVehicles.Config;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Constants;
using ValheimVehicles.Injections;
using ValheimVehicles.ModSupport;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Propulsion.Sail;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.ValheimVehicles.Providers;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;
using ZdoWatcher;
using Zolantris.Shared;
using Zolantris.Shared.BepInExAutoDoc;
using Logger = Jotunn.Logger;
namespace ValheimRAFT;

internal abstract class PluginDependencies
{
  public const string JotunnModGuid = Jotunn.Main.ModGuid;
}

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
  public const string Version = "3.2.0";
  public const string ModName = "ValheimRAFT";
  public const string ModNameBeta = "ValheimRAFTBETA";
  public const string ModGuid = $"{Author}.{ModName}";
  public static string HarmonyGuid => ModGuid;
  public const string ModDescription =
    "Valheim Mod for building on the sea, requires Jotunn to be installed.";
  public const string CopyRight = "Copyright © 2023-2024, GNU-v3 licensed";
  // ReSharper restore MemberCanBePrivate.Global

  public MapPinSync MapPinSync;

  public static VehicleGui Gui;
  public static GameObject GuiObj;

  public static ValheimRaftPlugin Instance { get; private set; }

  public ConfigEntry<bool> MakeAllPiecesWaterProof { get; set; }

  public ConfigEntry<bool> Graphics_AllowSailsFadeInFog { get; set; }
  public ConfigEntry<bool> AllowCustomRudderSpeeds { get; set; }

  public ConfigEntry<string> PluginFolderName { get; set; }
  public ConfigEntry<float> InitialRaftFloorHeight { get; set; }

  public ConfigEntry<float> ServerRaftUpdateZoneInterval { get; set; }
  public ConfigEntry<float> RaftSailForceMultiplier { get; set; }
  public ConfigEntry<bool> AdminsCanOnlyBuildRaft { get; set; }
  public ConfigEntry<bool> AllowOldV1RaftRecipe { get; set; }
  public ConfigEntry<bool> AllowExperimentalPrefabs { get; set; }
  public ConfigEntry<bool> ForceShipOwnerUpdatePerFrame { get; set; }
  
  public ConfigEntry<float> BoatDragCoefficient { get; set; }
  public ConfigEntry<float> MastShearForceThreshold { get; set; }
  public ConfigEntry<bool> HasDebugBase { get; set; }

  public ConfigEntry<KeyboardShortcut> AnchorKeyboardShortcut { get; set; }
  public ConfigEntry<bool> EnableMetrics { get; set; }

  public ConfigEntry<bool> ProtectVehiclePiecesOnErrorFromWearNTearDamage
  {
    get;
    set;
  }

  public ConfigEntry<bool> DebugRemoveStartMenuBackground { get; set; }

  // sounds for VehicleShip Effects
  public ConfigEntry<bool> EnableShipWakeSounds { get; set; }
  public ConfigEntry<bool> EnableShipInWaterSounds { get; set; }
  public ConfigEntry<bool> EnableShipSailSounds { get; set; }


  /**
   * These folder names are matched for the CustomTexturesGroup
   */
  public string[] possibleModFolderNames =
  [
    $"{Author}-{ModName}", $"zolantris-{ModName}", $"Zolantris-{ModName}",
    ModName, $"{Author}-{ModNameBeta}", $"zolantris-{ModNameBeta}",
    $"Zolantris-{ModNameBeta}",
    ModNameBeta
  ];

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

  private void CreateServerConfig()
  {
    ProtectVehiclePiecesOnErrorFromWearNTearDamage = Config.Bind(
      "Server config",
      "Protect Vehicle pieces from breaking on Error", true,
      CreateConfigDescription(
        "Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer",
        true, true));
    AdminsCanOnlyBuildRaft = Config.Bind("Server config",
      "AdminsCanOnlyBuildRaft", false,
      CreateConfigDescription(
        "ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart",
        true, true));
    AllowOldV1RaftRecipe = Config.Bind("Server config", "AllowOldV1RaftRecipe",
      false,
      CreateConfigDescription(
        "Allows the V1 Raft to be built, this Raft is not performant, but remains in >=v2.0.0 as a Fallback in case there are problems with the new raft",
        true, true));
    AllowExperimentalPrefabs = Config.Bind("Server config",
      "AllowExperimentalPrefabs", false,
      CreateConfigDescription(
        "Allows >=v2.0.0 experimental prefabs such as Iron variants of slabs, hulls, and ribs. They do not look great so they are disabled by default",
        true, true));

    ForceShipOwnerUpdatePerFrame = Config.Bind("Rendering",
      "Force Ship Owner Piece Update Per Frame", false,
      CreateConfigDescription(
        "Forces an update during the Update sync of unity meaning it fires every frame for the Ship owner who also owns Physics. This will possibly make updates better for non-boat owners. Noting that the boat owner is determined by the first person on the boat, otherwise the game owns it.",
        true, true));

    ServerRaftUpdateZoneInterval = Config.Bind("Server config",
      "ServerRaftUpdateZoneInterval",
      5f,
      CreateConfigDescription(
        "Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.",
        true, true));

    MakeAllPiecesWaterProof = Config.Bind<bool>("Server config",
      "MakeAllPiecesWaterProof", true, CreateConfigDescription(
        "Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.",
        true
      ));
    AllowCustomRudderSpeeds = Config.Bind("Server config",
      "AllowCustomRudderSpeeds", true,
      CreateConfigDescription(
        "Allow the raft to use custom rudder speeds set by the player, these speeds are applied alongside sails at half and full speed. See advanced section for the actual speed settings.",
        true));
  }

  private void CreateDebugConfig()
  {
    DebugRemoveStartMenuBackground =
      Config.Bind("Debug", "RemoveStartMenuBackground", false,
        CreateConfigDescription(
          "Removes the start scene background, only use this if you want to speedup start time",
          false, true));
  }

  private void CreateGraphicsConfig()
  {
    Graphics_AllowSailsFadeInFog = Config.Bind("Graphics", "Sails Fade In Fog",
      true,
      "Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled");
  }

  private void CreateSoundConfig()
  {
    EnableShipSailSounds = Config.Bind("Sounds", "Ship Sailing Sounds", true,
      "Toggles the ship sail sounds.");
    EnableShipWakeSounds = Config.Bind("Sounds", "Ship Wake Sounds", true,
      "Toggles Ship Wake sounds. Can be pretty loud");
    EnableShipInWaterSounds = Config.Bind("Sounds", "Ship In-Water Sounds",
      true,
      "Toggles ShipInWater Sounds, the sound of the hull hitting water");
  }

  private void CreateBaseConfig()
  {
    EnableMetrics = Config.Bind("Debug",
      "Enable Sentry Metrics (requires sentryUnityPlugin)", true,
      CreateConfigDescription(
        "Enable sentry debug logging. Requires sentry logging plugin installed to work. Sentry Logging plugin will make it easier to troubleshoot raft errors and detect performance bottlenecks. The bare minimum is collected, and only data related to ValheimRaft. See https://github.com/zolantris/ValheimMods/tree/main/src/ValheimRAFT#logging-metrics for more details about what is collected"));

    HasDebugBase = Config.Bind("Debug", "Debug logging for Vehicle/Raft", false,
      CreateConfigDescription(
        "Outputs more debug logs for the Vehicle components. Useful for troubleshooting errors, but will spam logs"));


    InitialRaftFloorHeight = Config.Bind<float>("Deprecated Config",
      "Initial Floor Height (V1 raft)", 0.6f, new ConfigDescription(
        "Allows users to set the raft floor spawn height. 0.45 was the original height in 1.4.9 but it looked a bit too low. Now people can customize it",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = false
          }
        }));

    PluginFolderName = Config.Bind<string>("Config",
      "pluginFolderName", "", new ConfigDescription(
        "Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their" +
        $" manager renames the folder, r2modman has a fallback case added to search for {Author}-{ModName}" +
        "Default search values are an ordered list first one is always matching non-empty strings from this pluginFolderName." +
        $"Folder Matches are:  {Author}-{ModName}, zolantris-{ModName} Zolantris-{ModName}, and {ModName}",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = false
          }
        }));
    PluginFolderName = Config.Bind<string>("Config",
      "pluginFolderName", "", new ConfigDescription(
        "Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their" +
        $" manager renames the folder, r2modman has a fallback case added to search for {Author}-{ModName}" +
        "Default search values are an ordered list first one is always matching non-empty strings from this pluginFolderName." +
        $"Folder Matches are:  {Author}-{ModName}, zolantris-{ModName} Zolantris-{ModName}, and {ModName}",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = false
          }
        }));
  }

  private void CreateKeyboardSetup()
  {
    AnchorKeyboardShortcut =
      Config.Bind("Config", "AnchorKeyboardShortcut",
        new KeyboardShortcut(KeyCode.LeftShift),
        new ConfigDescription(
          "Anchor keyboard hotkey. Only applies to keyboard"));
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
    CreateDebugConfig();
    CreateServerConfig();
    CreateKeyboardSetup();
    // for graphics QOL but maybe less FPS friendly
    CreateGraphicsConfig();
    CreateSoundConfig();


    // new way to do things. Makes life easier for config
    PatchConfig.BindConfig(Config);
    RamConfig.BindConfig(Config);
    PrefabConfig.BindConfig(Config);
    VehicleDebugConfig.BindConfig(Config);
    PropulsionConfig.BindConfig(Config);
    ModSupportConfig.BindConfig(Config);
    CustomMeshConfig.BindConfig(Config);
    WaterConfig.BindConfig(Config);
    PhysicsConfig.BindConfig(Config);
    MinimapConfig.BindConfig(Config);
    HudConfig.BindConfig(Config);
    CameraConfig.BindConfig(Config);
    RenderingConfig.BindConfig(Config);

#if DEBUG
    // Meant for only being run in debug builds for testing quickly
    QuickStartWorldConfig.BindConfig(Config);
#endif
  }

  internal void ApplyMetricIfAvailable()
  {
    var @namespace = "SentryUnityWrapper";
    var @pluginClass = "SentryUnityWrapperPlugin";
    Logger.LogDebug(
      $"contains sentryunitywrapper: {Chainloader.PluginInfos.ContainsKey("zolantris.SentryUnityWrapper")}");

    Logger.LogDebug($"plugininfos {Chainloader.PluginInfos}");

    if (!EnableMetrics.Value ||
        !Chainloader.PluginInfos.ContainsKey("zolantris.SentryUnityWrapper"))
      return;
    Logger.LogDebug("Made it to sentry check");
    SentryMetrics.ApplyMetrics();
  }

  public void Awake()
  {    
    Instance = this;
    gameObject.AddComponent<BatchedLogger>();

    CreateConfig();
    PatchController.Apply(HarmonyGuid);

    // critical for valheim api matching, must be called after config init.
    ProviderInitializers.InitProviders();

    AddPhysicsSettings();

    RegisterConsoleCommands();
    RegisterVehicleConsoleCommands();

    EnableShipSailSounds.SettingChanged += VehicleShip.UpdateAllShipSounds;
    EnableShipWakeSounds.SettingChanged += VehicleShip.UpdateAllShipSounds;
    EnableShipInWaterSounds.SettingChanged += VehicleShip.UpdateAllShipSounds;
    AllowExperimentalPrefabs.SettingChanged +=
      VehiclePrefabs.Instance.OnExperimentalPrefabSettingsChange;

    PrefabManager.OnVanillaPrefabsAvailable += () =>
    {
      // do not load custom textures on a dedicated server. This will do nothing but cause an error.
      if (ZNet.instance == null || ZNet.instance.IsDedicated() == false)
      {
        LoadCustomTextures();
      }
      AddCustomItemsAndPieces();
    };

    MapPinSync = gameObject.AddComponent<MapPinSync>();

    ZdoWatcherDelegate.RegisterToZdoManager();
    // PlayerSpawnController.PlayerMoveToVehicleCallback =
    // VehiclePiecesController.OnPlayerSpawnInVehicle;
    AddModSupport();

    var renderPipeline = GraphicsSettings.defaultRenderPipeline;
    if (renderPipeline != null)
      Logger.LogDebug(
        $"Valheim GameEngine is using: <{renderPipeline}> graphics pipeline ");

    Localization.OnLanguageChange += VehicleAnchorMechanismController.setLocalizedStates;
  }

  private void OnDestroy()
  {
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

  public void RegisterConsoleCommands()
  {
    CommandManager.Instance.AddConsoleCommand(new MoveRaftConsoleCommand());
    CommandManager.Instance.AddConsoleCommand(new HideRaftConsoleCommand());
    CommandManager.Instance.AddConsoleCommand(new RecoverRaftConsoleCommand());
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
    AddRemoveVehicleGui();

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

  /**
   * todo: move to Vehicles plugin when it is ready
   */
  public void AddRemoveVehicleGui()
  {
    if (!GuiObj)
    {
      GuiObj = new GameObject("ValheimVehicles_VehicleGui")
      {
        transform = { parent = transform },
        layer = LayerHelpers.UILayer
      };
    }
    Gui = GuiObj.GetComponent<VehicleGui>();
    if (!VehicleGui.hasConfigPanelOpened && Gui)
    {
      Destroy(GuiObj);
    }
    else if (!Gui)
      Gui = GuiObj.AddComponent<VehicleGui>();


    if (VehicleGui.Instance != null)
    {
      VehicleGui.Instance.InitPanel();
      VehicleGui.SetCommandsPanelState(VehicleDebugConfig.VehicleDebugMenuEnabled.Value);
    }
  }

  private void AddCustomItemsAndPieces()
  {
    PrefabRegistryController.InitAfterVanillaItemsAndPrefabsAreAvailable();
  }
}