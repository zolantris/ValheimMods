using BepInEx;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using Jotunn;
using UnityEngine;
using ValheimRAFT.Patches;

namespace ValheimRAFT;

[BepInPlugin(BepInGuid, ModName, Version)]
[BepInDependency(Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
public class ValheimRaftPlugin : BaseUnityPlugin
{
  /*
   * @note keeping this as Sarcen for now since there are low divergences from the original codebase and patches already mapped to sarcen's mod
   */
  public const string Author = "Sarcen";
  private const string Version = "1.6.7";
  internal const string ModName = "ValheimRAFT";
  public const string BepInGuid = $"BepIn.{Author}.{ModName}";
  private const string HarmonyGuid = $"Harmony.{Author}.{ModName}";
  internal static int CustomRaftLayer = 29;
  public static AssetBundle m_assetBundle;
  private bool m_customItemsAdded;
  public CustomLocalization localization;
  private PrefabController _prefabController;

  public static ValheimRaftPlugin Instance { get; private set; }

  public ConfigEntry<bool> MakeAllPiecesWaterProof { get; set; }

  public ConfigEntry<bool> AllowFlight { get; set; }

  public ConfigEntry<string> PluginFolderName { get; set; }
  public ConfigEntry<float> InitialRaftFloorHeight { get; set; }
  public ConfigEntry<bool> PatchPlanBuildPositionIssues { get; set; }
  public ConfigEntry<float> RaftHealth { get; set; }
  public ConfigEntry<float> ServerRaftUpdateZoneInterval { get; set; }
  public ConfigEntry<float> RaftSailForceMultiplier { get; set; }
  public ConfigEntry<bool> DisplacedRaftAutoFix { get; set; }
  public ConfigEntry<bool> AdminsCanOnlyBuildRaft { get; set; }


  // Propulsion Configs
  public ConfigEntry<bool> EnableCustomPropulsionConfig { get; set; }

  // probably deprecated
  public ConfigEntry<float> SailAreaThrottle { get; set; }
  public ConfigEntry<float> MaxPropulsionSpeed { get; set; }
  public ConfigEntry<float> MaxSailSpeed { get; set; }
  public ConfigEntry<float> SpeedCapMultiplier { get; set; }

  // for those that want to cruise with rudder
  public ConfigEntry<bool> AllowRudderSpeed { get; set; }
  public ConfigEntry<float> RudderSpeed2 { get; set; }
  public ConfigEntry<float> RudderSpeed3 { get; set; }
  public ConfigEntry<float> SailTier1Area { get; set; }
  public ConfigEntry<float> SailTier2Area { get; set; }
  public ConfigEntry<float> SailTier3Area { get; set; }
  public ConfigEntry<float> SailCustomAreaTier1Multiplier { get; set; }
  public ConfigEntry<float> BoatDragCoefficient { get; set; }
  public ConfigEntry<float> MastShearForceThreshold { get; set; }
  public ConfigEntry<bool> HasDebugSails { get; set; }
  public ConfigEntry<bool> HasShipWeightCalculations { get; set; }
  public ConfigEntry<float> MassPercentageFactor { get; set; }

  /**
   * These folder names are matched for the CustomTexturesGroup
   */
  public string[] possibleModFolderNames =
  [
    $"{Author}-{ModName}", $"zolantris-{ModName}", $"Zolantris-{ModName}", ModName
  ];

  private ConfigDescription CreateConfigDescription(string description, bool isAdmin = false)
  {
    return new ConfigDescription(
      description,
      (AcceptableValueBase)null, new object[1]
      {
        (object)new ConfigurationManagerAttributes()
        {
          IsAdminOnly = true
        }
      });
  }

  private void CreatePropulsionConfig()
  {
    MaxPropulsionSpeed = Config.Bind("Propulsion", "MaxSailSpeed", 25f,
      CreateConfigDescription(
        "Sets the absolute max speed a ship can ever hit. Prevents or enables space launches",
        true));
    MaxSailSpeed = Config.Bind("Propulsion", "MaxSailSpeed", 15f,
      CreateConfigDescription(
        "Sets the absolute max speed a ship can ever hit with sails. Prevents or enables space launches, cannot exceed MaxPropulsionSpeed.",
        true));
    MassPercentageFactor = Config.Bind("Propulsion", "MassPercentage", 33f, CreateConfigDescription(
      "Sets the mass percentage of the ship that will slow down the sails",
      true));
    SpeedCapMultiplier = Config.Bind("Propulsion", "SpeedCapMultiplier", 2f,
      CreateConfigDescription(
        "Sets the speed at which it becomes significantly harder to gain speed per sail area",
        true));

    RudderSpeed2 = Config.Bind("Propulsion", "RudderSpeed2", 5f,
      CreateConfigDescription(
        "Max speed at rudder speed 2.", true));
    RudderSpeed3 = Config.Bind("Propulsion", "RudderSpeed3", 10f,
      CreateConfigDescription(
        "", true));
    AllowRudderSpeed = Config.Bind("Propulsion", "AllowRudderSpeed", true,
      CreateConfigDescription(
        "", true));

    HasShipWeightCalculations = Config.Bind("Propulsion", "HasShipWeightCalculations", true,
      CreateConfigDescription(
        "enables ship weight calcs for sailforce and future propulsion, makes larger ships require more sails and smaller ships require less"));


    HasDebugSails = Config.Bind("Debug", "HasDebugSails", false,
      CreateConfigDescription(
        "Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only."));

    EnableCustomPropulsionConfig = Config.Bind("Propulsion",
      "EnableCustomPropulsionConfig", SailAreaForce.HasPropulsionConfigOverride,
      CreateConfigDescription("Enables all custom propulsion values", false));

    SailAreaThrottle = Config.Bind("Propulsion",
      "SailAreaThrottle", SailAreaForce.SailAreaThrottle,
      CreateConfigDescription(
        "Throttles the sail area, having this value high will prevent a boat with many sails and small area from breaking the sails. This value is meant to be left alone and will not apply unless the HasCustomSailConfig is enabled",
        true));


    SailCustomAreaTier1Multiplier = Config.Bind("Propulsion",
      "SailCustomAreaTier1Multiplier", SailAreaForce.CustomTier1AreaForceMultiplier,
      CreateConfigDescription(
        "Manual sets the area multiplier the custom tier1 sail. Currently there is only 1 tier",
        true)
    );

    SailTier1Area = Config.Bind("Propulsion",
      "SailTier1Area", SailAreaForce.Tier1,
      CreateConfigDescription("Manual sets the area of the tier 1 sail.", true)
    );

    SailTier2Area = Config.Bind("Propulsion",
      "SailTier2Area", SailAreaForce.Tier2,
      CreateConfigDescription("Manual sets the area of the tier 2 sail.", true));

    SailTier3Area = Config.Bind("Propulsion",
      "SailTier3Area", SailAreaForce.Tier3,
      CreateConfigDescription("Manual sets the area of the tier 3 sail.", true));
  }

  private void CreateServerConfig()
  {
    AdminsCanOnlyBuildRaft = Config.Bind("Server config", "AdminsCanOnlyBuildRaft", false,
      new ConfigDescription(
        "ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = true
          }
        }));

    ServerRaftUpdateZoneInterval = Config.Bind<float>("Server config",
      "ServerRaftUpdateZoneInterval",
      10f,
      new ConfigDescription(
        "Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = true
          }
        }));

    MakeAllPiecesWaterProof = Config.Bind<bool>("Server config",
      "MakeAllPiecesWaterProof", true, new ConfigDescription(
        "Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = true
          }
        }));
    AllowFlight = Config.Bind<bool>("Server config", "AllowFlight", false,
      new ConfigDescription("Allow the raft to fly (jump\\crouch to go up and down)",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = true
          }
        }));
  }

  private void CreateDebugConfig()
  {
    DisplacedRaftAutoFix = Config.Bind("Debug",
      "DisplacedRaftAutoFix", false,
      "Automatically fix a displaced glitched out raft if the player is standing on the raft. This will make the player fall into the water briefly but avoid having to run 'raftoffset 0 0 0'");
  }

  private void CreatePrefabConfig()
  {
    RaftHealth = Config.Bind<float>("Config", "raftHealth", 500f,
      "Set the raft health when used with wearNTear, lowest value is 100f");
  }

  private void CreateBaseConfig()
  {
    PatchPlanBuildPositionIssues = Config.Bind<bool>("Patches",
      "fixPlanBuildPositionIssues", true, new ConfigDescription(
        "Fixes the PlanBuild mod position problems with ValheimRaft so it uses localPosition of items based on the parent raft. This MUST be enabled to support PlanBuild but can be disabled when the mod owner adds direct support for this part of ValheimRAFT.",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = false
          }
        }));

    InitialRaftFloorHeight = Config.Bind<float>("Config",
      "initialRaftFloorHeight", 0.5f, new ConfigDescription(
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
    CreatePrefabConfig();
    CreateDebugConfig();
    CreatePropulsionConfig();
    CreateServerConfig();
  }

  /**
   * this is a placeholder fix until the asset bundle packs the translations and does the following
   * https://valheim-modding.github.io/Jotunn/tutorials/localization.html#example-json-file
   */
  private void InitLocalization()
  {
    // localization = LocalizationManager.Instance.GetLocalization();
    // localization.AddTranslation("English", new Dictionary<string, string>
    // {
    //   { "mb_anchor_disabled", "\\n(anchored)\\nLShift to remove Anchor while steering" },
    //   { "mb_anchor_enabled", "\\nLShift to Anchor" }
    // });
  }

  public void Awake()
  {
    Instance = this;
    CreateConfig();
    InitLocalization();
    PatchController.Apply(HarmonyGuid);

    var layer = LayerMask.NameToLayer("vehicle");
    for (var index = 0; index < 32; ++index)
      Physics.IgnoreLayerCollision(CustomRaftLayer, index,
        Physics.GetIgnoreLayerCollision(layer, index));
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("vehicle"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("piece"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("character"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("smoke"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("character_ghost"), true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("weapon"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("blocker"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("pathblocker"), true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("viewblock"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("character_net"), true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("character_noenv"), true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("Default_small"), false);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("Default"),
      false);
    CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new CreativeModeConsoleCommand());
    CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new MoveRaftConsoleCommand());
    CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new HideRaftConsoleCommand());
    CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new RecoverRaftConsoleCommand());

    /*
     * @todo add a way to skip LoadCustomTextures when on server. This check when used here crashes the Plugin.
     */
    PrefabManager.OnVanillaPrefabsAvailable += new Action(LoadCustomTextures);
    PrefabManager.OnVanillaPrefabsAvailable += new Action(AddCustomPieces);
  }

  private void LoadCustomTextures()
  {
    var sails = CustomTextureGroup.Load("Sails");
    for (var k = 0; k < sails.Textures.Count; k++)
    {
      var texture3 = sails.Textures[k];
      texture3.Texture.wrapMode = TextureWrapMode.Clamp;
      if ((bool)texture3.Normal) texture3.Normal.wrapMode = TextureWrapMode.Clamp;
    }

    var patterns = CustomTextureGroup.Load("Patterns");
    for (var j = 0; j < patterns.Textures.Count; j++)
    {
      var texture2 = patterns.Textures[j];
      texture2.Texture.filterMode = FilterMode.Point;
      texture2.Texture.wrapMode = TextureWrapMode.Repeat;
      if ((bool)texture2.Normal) texture2.Normal.wrapMode = TextureWrapMode.Repeat;
    }

    var logos = CustomTextureGroup.Load("Logos");
    for (var i = 0; i < logos.Textures.Count; i++)
    {
      var texture = logos.Textures[i];
      texture.Texture.wrapMode = TextureWrapMode.Clamp;
      if ((bool)texture.Normal) texture.Normal.wrapMode = TextureWrapMode.Clamp;
    }
  }

  internal void AddCustomPieces()
  {
    if (m_customItemsAdded) return;

    m_customItemsAdded = true;
    m_assetBundle =
      AssetUtils.LoadAssetBundleFromResources("valheimraft", Assembly.GetExecutingAssembly());

    // Registers all prefabs
    _prefabController = gameObject.AddComponent<PrefabController>();
    _prefabController.Init();
  }
}