using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Jotunn;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using ValheimRAFT.Patches;
using ValheimRAFT.Util;

namespace ValheimRAFT;

[BepInPlugin(BepInGuid, ModName, Version)]
[BepInDependency(Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
public class ValheimRaftPlugin : BaseUnityPlugin
{
  /*
   * @note keeping this as Sarcen for now since there are low divergences from the original codebase and patches already mapped to sarcen's mod
   */
  public const string Author = "Sarcen";
  private const string Version = "1.6.4";
  internal const string ModName = "ValheimRAFT";
  public const string BepInGuid = $"BepIn.{Author}.{ModName}";
  private const string HarmonyGuid = $"Harmony.{Author}.{ModName}";
  internal static int CustomRaftLayer = 29;
  public static AssetBundle m_assetBundle;
  private bool m_customItemsAdded;
  public CustomLocalization localization;

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


  public ConfigEntry<bool> EnableCustomPropulsionConfig { get; set; }
  public ConfigEntry<float> SailAreaThrottle { get; set; }
  public ConfigEntry<float> SailTier1Area { get; set; }
  public ConfigEntry<float> SailTier2Area { get; set; }
  public ConfigEntry<float> SailTier3Area { get; set; }
  public ConfigEntry<float> SailCustomAreaTier1Multiplier { get; set; }
  public ConfigEntry<float> BoatDragCoefficient { get; set; }
  public ConfigEntry<float> MastShearForceThreshold { get; set; }

  /**
   * These folder names are matched for the CustomTexturesGroup
   */
  public string[] possibleModFolderNames =
  [
    $"{Author}-{ModName}", $"zolantris-{ModName}", $"Zolantris-{ModName}", ModName
  ];

  private class ConfigData<T>
  {
    public string section;
    public string key;

    public string description;

    public ConfigEntry<T> bindVariable;

    public T defaultValue;

    public float dvFloat;

    public string dvString;

    public bool dvBool;

    public bool isAdminOnly = false;

    // public string GetDefault(string v)
    // {
    //   dvString = v;
    //   return dvString;
    // }
    //
    // public bool GetDefault(bool v)
    // {
    //   dvBool = v;
    //   return dvBool;
    // }
    //
    // public float GetDefault(float v)
    // {
    //   dvFloat = v;
    //   return dvFloat;
    // }
  }

  private void CreateConfig()
  {
    List<ConfigData<bool>> configListBool =
    [
      new ConfigData<bool>()
      {
        bindVariable = EnableCustomPropulsionConfig,
        section = "Propulsion",
        key = "EnableCustomPropulsionConfig",
        defaultValue = false,
        description =
          "Enables all custom propulsion values",
      },
    ];

    List<ConfigData<float>> configListFloat =
    [
      new ConfigData<float>()
      {
        bindVariable = SailAreaThrottle,
        section = "Propulsion",
        key = "SailAreaThrottle",
        defaultValue = 10f,
        description =
          "Throttles the sail area, having this value high will prevent a boat with many sails and small area from breaking the sails. This value is meant to be left alone and will not apply unless the HasCustomSailConfig is enabled",
      },
      new ConfigData<float>()
      {
        bindVariable = SailTier1Area,
        section = "Propulsion",
        key = "SailTier1Area",
        defaultValue = SailAreaForce.Tier1,
        description = "Manual sets the area of the tier 1 sail."
      },
      new ConfigData<float>()
      {
        bindVariable = SailTier2Area,
        section = "Propulsion",
        key = "SailTier2Area",
        defaultValue = SailAreaForce.Tier2,
        description = "Manual sets the area of the tier 2 sail."
      },
      new ConfigData<float>()
      {
        bindVariable = SailTier3Area,
        section = "Propulsion",
        key = "SailTier3Area",
        defaultValue = SailAreaForce.Tier3,
        description = "Manual sets the area of the tier 3 sail."
      },
      new ConfigData<float>()
      {
        bindVariable = SailCustomAreaTier1Multiplier,
        section = "Propulsion",
        key = "SailCustomAreaTier1Multiplier",
        defaultValue = SailAreaForce.CustomTier1AreaForceMultiplier,
        description =
          "Manual sets the area multiplier the custom tier1 sail. Currently there is only 1 tier"
      },
      new ConfigData<float>()
      {
        bindVariable = BoatDragCoefficient,
        section = "Propulsion",
        key = "BoatDragCoefficient",
        defaultValue = 0.2f,
        description =
          "Manually set the boat drag coefficient. This value will make boats that do not have a vertical design or are top heavy much slower and require many more sails"
      },
      new ConfigData<float>()
      {
        bindVariable = MastShearForceThreshold,
        section = "Propulsion",
        key = "MastShearForceThreshold",
        defaultValue = 0.2f,
        description =
          "Mast"
      },
    ];

    foreach (var configData in configListBool)
    {
      configData.bindVariable = Config.Bind(configData.section,
        configData.key, configData.defaultValue, new ConfigDescription(
          configData.description,
          (AcceptableValueBase)null, new object[1]
          {
            (object)new ConfigurationManagerAttributes()
            {
              IsAdminOnly = configData.isAdminOnly
            }
          }));
    }

    foreach (var configData in configListFloat)
    {
      configData.bindVariable = Config.Bind(configData.section,
        configData.key, configData.defaultValue, new ConfigDescription(
          configData.description,
          (AcceptableValueBase)null, new object[1]
          {
            (object)new ConfigurationManagerAttributes()
            {
              IsAdminOnly = configData.isAdminOnly
            }
          }));
    }


    /*
     * @todo move this into the larger config object
     * the old ugly way to add config.
     */
    DisplacedRaftAutoFix = Config.Bind("Debug",
      "DisplacedRaftAutoFix", false,
      "Automatically fix a displaced glitched out raft if the player is standing on the raft. This will make the player fall into the water briefly but avoid having to run 'raftoffset 0 0 0'");
    RaftSailForceMultiplier = Config.Bind("Config", "RaftSailForceMultiplier", 4f,
      "Set the sailforce multipler of the raft. 1 was the original value");
    RaftHealth = Config.Bind<float>("Config", "raftHealth", 500f,
      "Set the raft health when used with wearNTear, lowest value is 100f");
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

  /**
   * this is a placeholder fix until the asset bundle packs the translations and does the following
   * https://valheim-modding.github.io/Jotunn/tutorials/localization.html#example-json-file
   */
  private void InitLocalization()
  {
    localization = LocalizationManager.Instance.GetLocalization();
    localization.AddTranslation("English", new Dictionary<string, string>
    {
      { "mb_anchor_disabled", "\\n(anchored)\\nLShift to remove Anchor while steering" },
      { "mb_anchor_enabled", "\\nLShift to Anchor" }
    });
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
    var boarding_ramp = m_assetBundle.LoadAsset<GameObject>("Assets/boarding_ramp.prefab");
    var steering_wheel =
      m_assetBundle.LoadAsset<GameObject>("Assets/steering_wheel.prefab");
    var rope_ladder = m_assetBundle.LoadAsset<GameObject>("Assets/rope_ladder.prefab");
    var rope_anchor = m_assetBundle.LoadAsset<GameObject>("Assets/rope_anchor.prefab");
    var sprites = m_assetBundle.LoadAsset<SpriteAtlas>("Assets/icons.spriteatlas");
    var sailMat = m_assetBundle.LoadAsset<Material>("Assets/SailMat.mat");
    var prefabMan = PrefabManager.Instance;
    var raft = prefabMan.GetPrefab("Raft");
    var wood_pole2 = prefabMan.GetPrefab("wood_pole2").GetComponent<Piece>();
    var wood_floor = prefabMan.GetPrefab("wood_floor").GetComponent<Piece>();
    var wood_floor_wnt = wood_floor.GetComponent<WearNTear>();
    var raftMast = raft.transform.Find("ship/visual/mast").gameObject;
    var karve = prefabMan.GetPrefab("Karve");
    var karveMast = karve.transform.Find("ship/mast").gameObject;
    var vikingship = prefabMan.GetPrefab("VikingShip");
    var vikingshipMast = vikingship.transform.Find("ship/visual/Mast").gameObject;
    var pieceMan = PieceManager.Instance;

    var r16 = prefabMan.CreateClonedPrefab("MBRaft", raft);
    r16.transform.Find("ship/visual/mast").gameObject.SetActive(false);
    r16.transform.Find("interactive/mast").gameObject.SetActive(false);
    r16.GetComponent<Rigidbody>().mass = 1000f;

    Destroy(r16.transform.Find("ship/colliders/log").gameObject);
    Destroy(r16.transform.Find("ship/colliders/log (1)").gameObject);
    Destroy(r16.transform.Find("ship/colliders/log (2)").gameObject);
    Destroy(r16.transform.Find("ship/colliders/log (3)").gameObject);

    var piece16 = r16.GetComponent<Piece>();
    piece16.m_name = "$mb_raft";
    piece16.m_description = "$mb_raft_desc";
    var nv9 = r16.GetComponent<ZNetView>();
    nv9.m_persistent = true;
    var wnt12 = r16.GetComponent<WearNTear>();

    // Lowest is 100f
    wnt12.m_health = Math.Max(100f, RaftHealth.Value);
    wnt12.m_noRoofWear = false;
    var impact = r16.GetComponent<ImpactEffect>();
    impact.m_damageToSelf = false;
    pieceMan.AddPiece(new CustomPiece(r16, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_raft_desc",
      Category = "ValheimRAFT",
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 20,
          Item = "Wood"
        }
      }
    }));
    var r15 = prefabMan.CreateClonedPrefab("MBRaftMast", raftMast);
    var piece15 = r15.AddComponent<Piece>();
    piece15.m_name = "$mb_raft_mast";
    piece15.m_description = "$mb_raft_mast_desc";
    piece15.m_placeEffect = wood_floor.m_placeEffect;
    var nv8 = r15.AddComponent<ZNetView>();
    nv8.m_persistent = true;
    var mast4 = r15.AddComponent<MastComponent>();
    mast4.m_sailObject = r15.transform.Find("Sail").gameObject;
    mast4.m_sailCloth = mast4.m_sailObject.GetComponentInChildren<Cloth>();
    var wnt11 = r15.AddComponent<WearNTear>();
    wnt11.m_health = 1000f;
    wnt11.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
    wnt11.m_hitEffect = wood_floor_wnt.m_hitEffect;
    wnt11.m_noRoofWear = false;
    FixedRopes(r15);
    FixCollisionLayers(r15);
    pieceMan.AddPiece(new CustomPiece(r15, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_raft_mast_desc",
      Icon = sprites.GetSprite("raftmast"),
      Category = "ValheimRAFT",
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
    var r14 = prefabMan.CreateClonedPrefab("MBKarveMast", karveMast);
    var piece14 = r14.AddComponent<Piece>();
    piece14.m_name = "$mb_karve_mast";
    piece14.m_description = "$mb_karve_mast_desc";
    piece14.m_placeEffect = wood_floor.m_placeEffect;
    var nv7 = r14.AddComponent<ZNetView>();
    nv7.m_persistent = true;
    var mast3 = r14.AddComponent<MastComponent>();
    mast3.m_sailObject = r14.transform.Find("Sail").gameObject;
    mast3.m_sailCloth = mast3.m_sailObject.GetComponentInChildren<Cloth>();
    var wnt10 = r14.AddComponent<WearNTear>();
    wnt10.m_health = 1000f;
    wnt10.m_noRoofWear = false;
    wnt10.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
    wnt10.m_hitEffect = wood_floor_wnt.m_hitEffect;
    FixedRopes(r14);
    FixCollisionLayers(r14);
    pieceMan.AddPiece(new CustomPiece(r14, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_karve_mast_desc",
      Icon = sprites.GetSprite("karvemast"),
      Category = "ValheimRAFT",
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
    var r13 = prefabMan.CreateClonedPrefab("MBVikingShipMast", vikingshipMast);
    var piece13 = r13.AddComponent<Piece>();
    piece13.m_name = "$mb_vikingship_mast";
    piece13.m_description = "$mb_vikingship_mast_desc";
    piece13.m_placeEffect = wood_floor.m_placeEffect;
    var nv6 = r13.AddComponent<ZNetView>();
    nv6.m_persistent = true;
    var mast2 = r13.AddComponent<MastComponent>();
    mast2.m_sailObject = r13.transform.Find("Sail").gameObject;
    mast2.m_sailCloth = mast2.m_sailObject.GetComponentInChildren<Cloth>();
    var wnt9 = r13.AddComponent<WearNTear>();
    wnt9.m_health = 1000f;
    wnt9.m_noRoofWear = false;
    wnt9.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
    wnt9.m_hitEffect = wood_floor_wnt.m_hitEffect;
    FixedRopes(r13);
    FixCollisionLayers(r13);
    pieceMan.AddPiece(new CustomPiece(r13, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_vikingship_mast_desc",
      Icon = sprites.GetSprite("vikingmast"),
      Category = "ValheimRAFT",
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
    var r12 = prefabMan.CreateClonedPrefab("MBRudder", steering_wheel);
    var piece12 = r12.AddComponent<Piece>();
    piece12.m_name = "$mb_rudder";
    piece12.m_description = "$mb_rudder_desc";
    piece12.m_placeEffect = wood_floor.m_placeEffect;
    var nv5 = r12.AddComponent<ZNetView>();
    nv5.m_persistent = true;
    var rudder = r12.AddComponent<RudderComponent>();
    rudder.m_controls = r12.AddComponent<ShipControlls>();
    rudder.m_controls.m_hoverText = "Derp derp use!";
    rudder.m_controls.m_attachPoint = r12.transform.Find("attachpoint");
    rudder.m_controls.m_attachAnimation = "Standing Torch Idle right";
    rudder.m_controls.m_detachOffset = new Vector3(0f, 0f, 0f);
    rudder.m_wheel = r12.transform.Find("controls/wheel");
    rudder.UpdateSpokes();
    var wnt8 = r12.AddComponent<WearNTear>();
    wnt8.m_health = 1000f;
    wnt8.m_noRoofWear = false;
    wnt8.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
    wnt8.m_hitEffect = wood_floor_wnt.m_hitEffect;
    FixSnapPoints(r12);
    FixCollisionLayers(r12);
    pieceMan.AddPiece(new CustomPiece(r12, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_rudder_desc",
      Icon = sprites.GetSprite("steering_wheel"),
      Category = "ValheimRAFT",
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
    var r11 = prefabMan.CreateClonedPrefab("MBRopeLadder", rope_ladder);
    var piece11 = r11.AddComponent<Piece>();
    piece11.m_name = "$mb_rope_ladder";
    piece11.m_description = "$mb_rope_ladder_desc";
    piece11.m_placeEffect = wood_floor.m_placeEffect;
    piece11.m_primaryTarget = false;
    piece11.m_randomTarget = false;
    var nv4 = r11.AddComponent<ZNetView>();
    nv4.m_persistent = true;
    var ropeLadder = r11.AddComponent<RopeLadderComponent>();
    var rope = raftMast.GetComponentInChildren<LineRenderer>(true);
    ropeLadder.m_ropeLine = ropeLadder.GetComponent<LineRenderer>();
    ropeLadder.m_ropeLine.material = new Material(rope.material);
    ropeLadder.m_ropeLine.textureMode = LineTextureMode.Tile;
    ropeLadder.m_ropeLine.widthMultiplier = 0.05f;
    ropeLadder.m_stepObject = ropeLadder.transform.Find("step").gameObject;
    var ladderMesh = ropeLadder.m_stepObject.GetComponentInChildren<MeshRenderer>();
    ladderMesh.material =
      new Material(wood_floor.GetComponentInChildren<MeshRenderer>().material);
    var wnt7 = r11.AddComponent<WearNTear>();
    wnt7.m_health = 10000f;
    wnt7.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
    wnt7.m_hitEffect = wood_floor_wnt.m_hitEffect;
    wnt7.m_noRoofWear = false;
    wnt7.m_supports = false;
    FixCollisionLayers(r11);
    pieceMan.AddPiece(new CustomPiece(r11, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_rope_ladder_desc",
      Icon = sprites.GetSprite("rope_ladder"),
      Category = "ValheimRAFT",
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
    var r10 = prefabMan.CreateClonedPrefab("MBRopeAnchor", rope_anchor);
    var piece10 = r10.AddComponent<Piece>();
    piece10.m_name = "$mb_rope_anchor";
    piece10.m_description = "$mb_rope_anchor_desc";
    piece10.m_placeEffect = wood_floor.m_placeEffect;
    var nv3 = r10.AddComponent<ZNetView>();
    nv3.m_persistent = true;
    var ropeanchor = r10.AddComponent<RopeAnchorComponent>();
    var baseRope = raftMast.GetComponentInChildren<LineRenderer>(true);
    ropeanchor.m_rope = r10.AddComponent<LineRenderer>();
    ropeanchor.m_rope.material = new Material(baseRope.material);
    ropeanchor.m_rope.widthMultiplier = 0.05f;
    ropeanchor.m_rope.enabled = false;
    var wnt6 = r10.AddComponent<WearNTear>();
    wnt6.m_health = 1000f;
    wnt6.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
    wnt6.m_hitEffect = wood_floor_wnt.m_hitEffect;
    wnt6.m_noRoofWear = false;
    wnt6.m_supports = false;
    FixCollisionLayers(r10);
    pieceMan.AddPiece(new CustomPiece(r10, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_rope_anchor_desc",
      Icon = sprites.GetSprite("rope_anchor"),
      Category = "ValheimRAFT",
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
    var r9 = prefabMan.CreateEmptyPrefab("MBSail");
    Destroy(r9.GetComponent<BoxCollider>());
    Destroy(r9.GetComponent<MeshFilter>());
    var piece9 = r9.AddComponent<Piece>();
    piece9.m_name = "$mb_sail";
    piece9.m_description = "$mb_sail_desc";
    piece9.m_placeEffect = wood_floor.m_placeEffect;
    var nv2 = r9.GetComponent<ZNetView>();
    nv2.m_persistent = true;
    var sailObject = new GameObject("Sail");
    sailObject.transform.parent = r9.transform;
    sailObject.layer = LayerMask.NameToLayer("piece_nonsolid");
    var sail = r9.AddComponent<SailComponent>();
    sail.m_sailObject = sailObject;
    sail.m_sailCloth = sailObject.AddComponent<Cloth>();
    sail.m_meshCollider = sailObject.AddComponent<MeshCollider>();
    sail.m_mesh = sailObject.GetComponent<SkinnedMeshRenderer>();
    sail.m_mesh.shadowCastingMode = ShadowCastingMode.TwoSided;
    sail.m_mesh.sharedMaterial = sailMat;
    var wnt5 = r9.AddComponent<WearNTear>();
    wnt5.m_health = 1000f;
    wnt5.m_noRoofWear = false;
    wnt5.m_noSupportWear = false;
    wnt5.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
    wnt5.m_hitEffect = wood_floor_wnt.m_hitEffect;
    var mast = r9.AddComponent<MastComponent>();
    mast.m_sailObject = sailObject;
    mast.m_sailCloth = sail.m_sailCloth;
    mast.m_allowSailRotation = false;
    mast.m_allowSailShrinking = true;
    r9.layer = LayerMask.NameToLayer("piece_nonsolid");
    SailCreatorComponent.m_sailPrefab = r9;
    PrefabManager.Instance.AddPrefab(r9);
    var r8 = prefabMan.CreateEmptyPrefab("MBSailCreator_4", false);
    var piece8 = r8.AddComponent<Piece>();
    piece8.m_name = "$mb_sail_4";
    piece8.m_description = "$mb_sail_4_desc";
    piece8.m_placeEffect = wood_floor.m_placeEffect;
    var sailCreator2 = r8.AddComponent<SailCreatorComponent>();
    sailCreator2.m_sailSize = 4;
    var mesh2 = r8.GetComponent<MeshRenderer>();
    mesh2.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    r8.layer = LayerMask.NameToLayer("piece_nonsolid");
    pieceMan.AddPiece(new CustomPiece(r8, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_sail_4_desc",
      Category = "ValheimRAFT",
      Icon = sprites.GetSprite("customsail")
    }));
    var r7 = prefabMan.CreateEmptyPrefab("MBSailCreator_3", false);
    var piece7 = r7.AddComponent<Piece>();
    piece7.m_name = "$mb_sail_3";
    piece7.m_description = "$mb_sail_3_desc";
    piece7.m_placeEffect = wood_floor.m_placeEffect;
    var sailCreator = r7.AddComponent<SailCreatorComponent>();
    sailCreator.m_sailSize = 3;
    var mesh = r7.GetComponent<MeshRenderer>();
    mesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    r7.layer = LayerMask.NameToLayer("piece_nonsolid");
    pieceMan.AddPiece(new CustomPiece(r7, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = "$mb_sail",
      Description = "$mb_sail_3_desc",
      Category = "ValheimRAFT",
      Icon = sprites.GetSprite("customsail_tri")
    }));
    var sourceObject4 = prefabMan.GetPrefab("wood_pole_log_4");
    var r6 = prefabMan.CreateClonedPrefab("MBPier_Pole", sourceObject4);
    var wnt4 = r6.GetComponent<WearNTear>();
    wnt4.m_noRoofWear = false;
    var piece6 = r6.GetComponent<Piece>();
    piece6.m_waterPiece = true;
    var pier2 = r6.AddComponent<PierComponent>();
    pier2.m_segmentObject = prefabMan.CreateClonedPrefab("MBPier_Pole_Segment", sourceObject4);
    Destroy(pier2.m_segmentObject.GetComponent<ZNetView>());
    Destroy(pier2.m_segmentObject.GetComponent<Piece>());
    Destroy(pier2.m_segmentObject.GetComponent<WearNTear>());
    FixSnapPoints(r6);
    var transforms2 = pier2.m_segmentObject.GetComponentsInChildren<Transform>();
    for (var j = 0; j < transforms2.Length; j++)
      if ((bool)transforms2[j] && transforms2[j].CompareTag("snappoint"))
        Destroy(transforms2[j]);

    pier2.m_segmentHeight = 4f;
    pier2.m_baseOffset = -1f;
    pieceMan.AddPiece(new CustomPiece(r6, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = "$mb_pier (" + piece6.m_name + ")",
      Description = "$mb_pier_desc\n " + piece6.m_description,
      Category = "ValheimRAFT",
      Icon = piece6.m_icon,
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 4,
          Item = "RoundLog",
          Recover = true
        }
      }
    }));
    var sourceObject3 = prefabMan.GetPrefab("stone_wall_4x2");
    var r5 = prefabMan.CreateClonedPrefab("MBPier_Stone", sourceObject3);
    var piece5 = r5.GetComponent<Piece>();
    piece5.m_waterPiece = true;
    var pier = r5.AddComponent<PierComponent>();
    pier.m_segmentObject = prefabMan.CreateClonedPrefab("MBPier_Stone_Segment", sourceObject3);
    Destroy(pier.m_segmentObject.GetComponent<ZNetView>());
    Destroy(pier.m_segmentObject.GetComponent<Piece>());
    Destroy(pier.m_segmentObject.GetComponent<WearNTear>());
    FixSnapPoints(r5);
    var transforms = pier.m_segmentObject.GetComponentsInChildren<Transform>();
    for (var i = 0; i < transforms.Length; i++)
      if ((bool)transforms[i] && transforms[i].CompareTag("snappoint"))
        Destroy(transforms[i]);

    pier.m_segmentHeight = 2f;
    pier.m_baseOffset = 0f;
    pieceMan.AddPiece(new CustomPiece(r5, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = "$mb_pier (" + piece5.m_name + ")",
      Description = "$mb_pier_desc\n " + piece5.m_description,
      Category = "ValheimRAFT",
      Icon = piece5.m_icon,
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 12,
          Item = "Stone",
          Recover = true
        }
      }
    }));
    var r4 = prefabMan.CreateClonedPrefab("MBBoardingRamp", boarding_ramp);
    var floor = r4.transform.Find("Ramp/Segment/SegmentAnchor/Floor").gameObject;
    var new_floor = Instantiate(
      wood_floor.transform.Find("New/_Combined Mesh [high]").gameObject, floor.transform.parent,
      false);
    Destroy(floor);
    new_floor.transform.localPosition = new Vector3(1f, -52.55f, 0.5f);
    new_floor.transform.localScale = Vector3.one;
    new_floor.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
    var woodMat =
      wood_pole2.transform.Find("New").GetComponent<MeshRenderer>().sharedMaterial;
    r4.transform.Find("Winch1/Pole").GetComponent<MeshRenderer>().sharedMaterial = woodMat;
    r4.transform.Find("Winch2/Pole").GetComponent<MeshRenderer>().sharedMaterial = woodMat;
    r4.transform.Find("Ramp/Segment/SegmentAnchor/Pole1").GetComponent<MeshRenderer>()
      .sharedMaterial = woodMat;
    r4.transform.Find("Ramp/Segment/SegmentAnchor/Pole2").GetComponent<MeshRenderer>()
      .sharedMaterial = woodMat;
    r4.transform.Find("Winch1/Cylinder").GetComponent<MeshRenderer>().sharedMaterial = woodMat;
    r4.transform.Find("Winch2/Cylinder").GetComponent<MeshRenderer>().sharedMaterial = woodMat;
    var ropeMat = raftMast.GetComponentInChildren<LineRenderer>(true)
      .sharedMaterial;
    r4.transform.Find("Rope1").GetComponent<LineRenderer>().sharedMaterial = ropeMat;
    r4.transform.Find("Rope2").GetComponent<LineRenderer>().sharedMaterial = ropeMat;
    var piece4 = r4.AddComponent<Piece>();
    piece4.m_name = "$mb_boarding_ramp";
    piece4.m_description = "$mb_boarding_ramp_desc";
    piece4.m_placeEffect = wood_floor.m_placeEffect;
    var nv = r4.AddComponent<ZNetView>();
    nv.m_persistent = true;
    var boardingRamp2 = r4.AddComponent<BoardingRampComponent>();
    boardingRamp2.m_stateChangeDuration = 0.3f;
    boardingRamp2.m_segments = 5;
    var wnt3 = r4.AddComponent<WearNTear>();
    wnt3.m_health = 1000f;
    wnt3.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
    wnt3.m_hitEffect = wood_floor_wnt.m_hitEffect;
    wnt3.m_noRoofWear = false;
    wnt3.m_supports = false;
    FixCollisionLayers(r4);
    FixSnapPoints(r4);
    pieceMan.AddPiece(new CustomPiece(r4, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_boarding_ramp_desc",
      Icon = sprites.GetSprite("boarding_ramp"),
      Category = "ValheimRAFT",
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
    boarding_ramp = r4;
    var r3 = prefabMan.CreateClonedPrefab("MBBoardingRamp_Wide", boarding_ramp);
    var piece3 = r3.GetComponent<Piece>();
    piece3.m_name = "$mb_boarding_ramp_wide";
    piece3.m_description = "$mb_boarding_ramp_wide_desc";
    var boardingRamp = r3.GetComponent<BoardingRampComponent>();
    boardingRamp.m_stateChangeDuration = 0.3f;
    boardingRamp.m_segments = 5;
    r3.transform.localScale = new Vector3(2f, 1f, 1f);
    FixSnapPoints(r3);
    pieceMan.AddPiece(new CustomPiece(r3, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_boarding_ramp_wide_desc",
      Icon = sprites.GetSprite("boarding_ramp"),
      Category = "ValheimRAFT",
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
    var sourceObject2 = m_assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
    var r2 = prefabMan.CreateClonedPrefab("MBDirtfloor_2x2", sourceObject2);
    r2.transform.localScale = new Vector3(2f, 1f, 2f);
    var netview2 = r2.AddComponent<ZNetView>();
    netview2.m_persistent = true;
    var wnt2 = r2.AddComponent<WearNTear>();
    wnt2.m_health = 1000f;
    var piece2 = r2.AddComponent<Piece>();
    piece2.m_placeEffect = wood_floor.m_placeEffect;
    var cultivatable2 = r2.AddComponent<CultivatableComponent>();
    FixCollisionLayers(r2);
    FixSnapPoints(r2);
    pieceMan.AddPiece(new CustomPiece(r2, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = "$mb_dirt_floor_2x2",
      Description = "$mb_dirt_floor_2x2_desc",
      Category = "ValheimRAFT",
      Icon = sprites.GetSprite("dirtfloor_icon"),
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 4,
          Item = "Stone",
          Recover = true
        }
      }
    }));
    var sourceObject = m_assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
    var r = prefabMan.CreateClonedPrefab("MBDirtfloor_1x1", sourceObject);
    r.transform.localScale = new Vector3(1f, 1f, 1f);
    var netview = r.AddComponent<ZNetView>();
    netview.m_persistent = true;
    var wnt = r.AddComponent<WearNTear>();
    wnt.m_health = 1000f;
    var piece = r.AddComponent<Piece>();
    piece.m_placeEffect = wood_floor.m_placeEffect;
    var cultivatable = r.AddComponent<CultivatableComponent>();
    FixCollisionLayers(r);
    FixSnapPoints(r);
    pieceMan.AddPiece(new CustomPiece(r, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = "$mb_dirt_floor_1x1",
      Description = "$mb_dirt_floor_1x1_desc",
      Category = "ValheimRAFT",
      Icon = sprites.GetSprite("dirtfloor_icon"),
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 1,
          Item = "Stone",
          Recover = true
        }
      }
    }));
  }

  private void FixSnapPoints(GameObject r)
  {
    var t = r.GetComponentsInChildren<Transform>(true);
    for (var i = 0; i < t.Length; i++)
      if (t[i].name.StartsWith("_snappoint"))
        t[i].tag = "snappoint";
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