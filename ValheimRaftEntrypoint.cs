// Decompiled with JetBrains decompiler
// Type: ValheimRAFT
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace ValheimRAFT
{
  [BepInPlugin("BepIn.Sarcen.ValheimRAFT", "ValheimRAFT", "1.5.0")]
  // [BepInDependency]
  [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
  public class ValheimRaftEntrypoint : BaseUnityPlugin
  {
    internal const string Author = "Sarcen";
    internal const string Name = "ValheimRAFT";
    internal const string Version = "1.4.9";
    internal const string BepInGUID = "BepIn.Sarcen.ValheimRAFT";
    internal const string HarmonyGUID = "Harmony.Sarcen.ValheimRAFT";
    internal static Harmony m_harmony;
    internal static int CustomRaftLayer = 29;
    public static AssetBundle m_assetBundle;
    private bool m_customItemsAdded;

    public static ValheimRaftEntrypoint Instance { get; private set; }

    public ConfigEntry<bool> MakeAllPiecesWaterProof { get; set; }

    public ConfigEntry<bool> AllowFlight { get; set; }

    private void Awake()
    {
      Instance = this;
      this.MakeAllPiecesWaterProof = this.Config.Bind<bool>("Server config",
        "MakeAllPiecesWaterProof", true, new ConfigDescription(
          "Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.",
          (AcceptableValueBase)null, new object[1]
          {
            (object)new ConfigurationManagerAttributes()
            {
              IsAdminOnly = true
            }
          }));
      this.AllowFlight = this.Config.Bind<bool>("Server config", "AllowFlight", false,
        new ConfigDescription("Allow the raft to fly (jump\\crouch to go up and down)",
          (AcceptableValueBase)null, new object[1]
          {
            (object)new ConfigurationManagerAttributes()
            {
              IsAdminOnly = true
            }
          }));
      m_harmony = new Harmony("Harmony.Sarcen.ValheimRAFT");
      m_harmony.PatchAll();
      int layer = LayerMask.NameToLayer("vehicle");
      for (int index = 0; index < 32; ++index)
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
      PrefabManager.OnVanillaPrefabsAvailable += new Action(this.LoadCustomTextures);
      PrefabManager.OnVanillaPrefabsAvailable += new Action(this.AddCustomPieces);
    }

    private void LoadCustomTextures()
    {
      CustomTextureGroup sails = CustomTextureGroup.Load("Sails");
      for (int k = 0; k < sails.Textures.Count; k++)
      {
        CustomTextureGroup.CustomTexture texture3 = sails.Textures[k];
        texture3.Texture.wrapMode = TextureWrapMode.Clamp;
        if ((bool)texture3.Normal)
        {
          texture3.Normal.wrapMode = TextureWrapMode.Clamp;
        }
      }

      CustomTextureGroup patterns = CustomTextureGroup.Load("Patterns");
      for (int j = 0; j < patterns.Textures.Count; j++)
      {
        CustomTextureGroup.CustomTexture texture2 = patterns.Textures[j];
        texture2.Texture.filterMode = FilterMode.Point;
        texture2.Texture.wrapMode = TextureWrapMode.Repeat;
        if ((bool)texture2.Normal)
        {
          texture2.Normal.wrapMode = TextureWrapMode.Repeat;
        }
      }

      CustomTextureGroup logos = CustomTextureGroup.Load("Logos");
      for (int i = 0; i < logos.Textures.Count; i++)
      {
        CustomTextureGroup.CustomTexture texture = logos.Textures[i];
        texture.Texture.wrapMode = TextureWrapMode.Clamp;
        if ((bool)texture.Normal)
        {
          texture.Normal.wrapMode = TextureWrapMode.Clamp;
        }
      }
    }

    internal void AddCustomPieces()
    {
      if (m_customItemsAdded)
      {
        return;
      }

      m_customItemsAdded = true;
      m_assetBundle =
        AssetUtils.LoadAssetBundleFromResources("valheimraft", Assembly.GetExecutingAssembly());
      Logger.Log(LogLevel.Debug,
        $"asset_bundle loaded {m_assetBundle.GetAllAssetNames().Join()} {m_assetBundle.GetAllScenePaths().Join()}");
      GameObject boarding_ramp = m_assetBundle.LoadAsset<GameObject>("boarding_ramp");
      GameObject steering_wheel =
        m_assetBundle.LoadAsset<GameObject>("steering_wheel");
      Logger.Log(LogLevel.Debug,
        $"loading status steering_wheel: {steering_wheel.gameObject.name}");

      GameObject rope_ladder = m_assetBundle.LoadAsset<GameObject>("rope_ladder");
      GameObject rope_anchor = m_assetBundle.LoadAsset<GameObject>("rope_anchor");
      Material sailMat = m_assetBundle.LoadAsset<Material>("SailMat.mat");
      PrefabManager prefabMan = PrefabManager.Instance;
      GameObject raft = prefabMan.GetPrefab("Raft");
      Piece wood_pole2 = prefabMan.GetPrefab("wood_pole2").GetComponent<Piece>();
      Piece wood_floor = prefabMan.GetPrefab("wood_floor").GetComponent<Piece>();
      WearNTear wood_floor_wnt = wood_floor.GetComponent<WearNTear>();
      GameObject raftMast = raft.transform.Find("ship/visual/mast").gameObject;
      GameObject karve = prefabMan.GetPrefab("Karve");
      GameObject karveMast = karve.transform.Find("ship/mast").gameObject;
      GameObject vikingship = prefabMan.GetPrefab("VikingShip");
      GameObject vikingshipMast = vikingship.transform.Find("ship/visual/Mast").gameObject;
      PieceManager pieceMan = PieceManager.Instance;

      GameObject r16 = prefabMan.CreateClonedPrefab("MBRaft", raft);
      r16.transform.Find("ship/visual/mast").gameObject.SetActive(value: false);
      r16.transform.Find("interactive/mast").gameObject.SetActive(value: false);
      r16.GetComponent<Rigidbody>().mass = 1000f;
      Destroy(r16.transform.Find("ship/colliders/log").gameObject);
      Destroy(r16.transform.Find("ship/colliders/log (1)").gameObject);
      Destroy(r16.transform.Find("ship/colliders/log (2)").gameObject);
      Destroy(r16.transform.Find("ship/colliders/log (3)").gameObject);
      Piece piece16 = r16.GetComponent<Piece>();

      piece16.m_name = "$mb_raft";
      piece16.m_description = "$mb_raft_desc";
      ZNetView nv9 = r16.GetComponent<ZNetView>();
      nv9.m_persistent = true;
      WearNTear wnt12 = r16.GetComponent<WearNTear>();
      wnt12.m_health = 10000f;
      wnt12.m_noRoofWear = false;
      ImpactEffect impact = r16.GetComponent<ImpactEffect>();
      impact.m_damageToSelf = false;
      PieceConfig val = new PieceConfig
      {
        PieceTable = null,
        Category = null,
        CraftingStation = null,
        ExtendStation = null,
        Name = null,
        Description = null,
        Enabled = false,
        AllowedInDungeons = false,
        Icon = null,
        Requirements = new RequirementConfig[]
        {
        }
      };
      val.PieceTable = "Hammer";
      val.Description = "$mb_raft_desc";
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[1]
      {
        new RequirementConfig
        {
          Amount = 20,
          Item = "Wood"
        }
      };
      pieceMan.AddPiece(new CustomPiece(r16, false, val));

      GameObject r15 = prefabMan.CreateClonedPrefab("MBRaftMast", raftMast);
      Piece piece15 = r15.AddComponent<Piece>();
      piece15.m_name = "$mb_raft_mast";
      piece15.m_description = "$mb_raft_mast_desc";
      piece15.m_placeEffect = wood_floor.m_placeEffect;
      ZNetView nv8 = r15.AddComponent<ZNetView>();
      nv8.m_persistent = true;
      MastComponent mast4 = r15.AddComponent<MastComponent>();
      mast4.m_sailObject = r15.transform.Find("Sail").gameObject;
      mast4.m_sailCloth = mast4.m_sailObject.GetComponentInChildren<Cloth>();
      WearNTear wnt11 = r15.AddComponent<WearNTear>();
      wnt11.m_health = 1000f;
      wnt11.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
      wnt11.m_hitEffect = wood_floor_wnt.m_hitEffect;
      wnt11.m_noRoofWear = false;
      FixedRopes(r15);
      FixCollisionLayers(r15);
      val = new PieceConfig
      {
        PieceTable = null,
        Category = null,
        CraftingStation = null,
        ExtendStation = null,
        Name = null,
        Description = null,
        Enabled = false,
        AllowedInDungeons = false,
        Icon = null,
        Requirements = new RequirementConfig[]
        {
        }
      };
      val.PieceTable = "Hammer";
      val.Description = "$mb_raft_mast_desc";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/raftmast");
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[2]
      {
        new RequirementConfig
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "DeerHide",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r15, false, val));
      GameObject r14 = prefabMan.CreateClonedPrefab("MBKarveMast", karveMast);
      Piece piece14 = r14.AddComponent<Piece>();
      piece14.m_name = "$mb_karve_mast";
      piece14.m_description = "$mb_karve_mast_desc";
      piece14.m_placeEffect = wood_floor.m_placeEffect;
      ZNetView nv7 = r14.AddComponent<ZNetView>();
      nv7.m_persistent = true;
      MastComponent mast3 = r14.AddComponent<MastComponent>();
      mast3.m_sailObject = r14.transform.Find("Sail").gameObject;
      mast3.m_sailCloth = mast3.m_sailObject.GetComponentInChildren<Cloth>();
      WearNTear wnt10 = r14.AddComponent<WearNTear>();
      wnt10.m_health = 1000f;
      wnt10.m_noRoofWear = false;
      wnt10.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
      wnt10.m_hitEffect = wood_floor_wnt.m_hitEffect;
      FixedRopes(r14);
      FixCollisionLayers(r14);

      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Description = "$mb_karve_mast_desc";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/karvemast");
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[3]
      {
        new RequirementConfig
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "TrollHide",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r14, false, val));
      GameObject r13 = prefabMan.CreateClonedPrefab("MBVikingShipMast", vikingshipMast);
      Piece piece13 = r13.AddComponent<Piece>();
      piece13.m_name = "$mb_vikingship_mast";
      piece13.m_description = "$mb_vikingship_mast_desc";
      piece13.m_placeEffect = wood_floor.m_placeEffect;
      ZNetView nv6 = r13.AddComponent<ZNetView>();
      nv6.m_persistent = true;
      MastComponent mast2 = r13.AddComponent<MastComponent>();
      mast2.m_sailObject = r13.transform.Find("Sail").gameObject;
      mast2.m_sailCloth = mast2.m_sailObject.GetComponentInChildren<Cloth>();
      WearNTear wnt9 = r13.AddComponent<WearNTear>();
      wnt9.m_health = 1000f;
      wnt9.m_noRoofWear = false;
      wnt9.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
      wnt9.m_hitEffect = wood_floor_wnt.m_hitEffect;
      FixedRopes(r13);
      FixCollisionLayers(r13);
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Description = "$mb_vikingship_mast_desc";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/vikingmast");
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[3]
      {
        new RequirementConfig
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "WolfPelt",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r13, false, val));
      GameObject r12 = prefabMan.CreateClonedPrefab("MBRudder", steering_wheel);
      Piece piece12 = r12.AddComponent<Piece>();
      piece12.m_name = "$mb_rudder";
      piece12.m_description = "$mb_rudder_desc";
      piece12.m_placeEffect = wood_floor.m_placeEffect;
      ZNetView nv5 = r12.AddComponent<ZNetView>();
      nv5.m_persistent = true;
      RudderComponent rudder = r12.AddComponent<RudderComponent>();
      rudder.m_controls = r12.AddComponent<ShipControlls>();
      rudder.m_controls.m_hoverText = "$mb_rudder_use";
      rudder.m_controls.m_attachPoint = r12.transform.Find("attachpoint");
      rudder.m_controls.m_attachAnimation = "Standing Torch Idle right";
      rudder.m_controls.m_detachOffset = new Vector3(0f, 0f, 0f);
      rudder.m_wheel = r12.transform.Find("controls/wheel");
      rudder.UpdateSpokes();
      WearNTear wnt8 = r12.AddComponent<WearNTear>();
      wnt8.m_health = 1000f;
      wnt8.m_noRoofWear = false;
      wnt8.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
      wnt8.m_hitEffect = wood_floor_wnt.m_hitEffect;
      FixSnapPoints(r12);
      FixCollisionLayers(r12);
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Description = "$mb_rudder_desc";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/steering_wheel");
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[1]
      {
        new RequirementConfig
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r12, false, val));
      GameObject r11 = prefabMan.CreateClonedPrefab("MBRopeLadder", rope_ladder);
      Piece piece11 = r11.AddComponent<Piece>();
      piece11.m_name = "$mb_rope_ladder";
      piece11.m_description = "$mb_rope_ladder_desc";
      piece11.m_placeEffect = wood_floor.m_placeEffect;
      piece11.m_primaryTarget = false;
      piece11.m_randomTarget = false;
      ZNetView nv4 = r11.AddComponent<ZNetView>();
      nv4.m_persistent = true;
      RopeLadderComponent ropeLadder = r11.AddComponent<RopeLadderComponent>();
      LineRenderer rope = raftMast.GetComponentInChildren<LineRenderer>(includeInactive: true);
      ropeLadder.m_ropeLine = ropeLadder.GetComponent<LineRenderer>();
      ropeLadder.m_ropeLine.material = new Material(rope.material);
      ropeLadder.m_ropeLine.textureMode = LineTextureMode.Tile;
      ropeLadder.m_ropeLine.widthMultiplier = 0.05f;
      ropeLadder.m_stepObject = ropeLadder.transform.Find("step").gameObject;
      MeshRenderer ladderMesh = ropeLadder.m_stepObject.GetComponentInChildren<MeshRenderer>();
      ladderMesh.material =
        new Material(wood_floor.GetComponentInChildren<MeshRenderer>().material);
      WearNTear wnt7 = r11.AddComponent<WearNTear>();
      wnt7.m_health = 10000f;
      wnt7.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
      wnt7.m_hitEffect = wood_floor_wnt.m_hitEffect;
      wnt7.m_noRoofWear = false;
      wnt7.m_supports = false;
      FixCollisionLayers(r11);
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Description = "$mb_rope_ladder_desc";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/rope_ladder");
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[1]
      {
        new RequirementConfig
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r11, false, val));
      GameObject r10 = prefabMan.CreateClonedPrefab("MBRopeAnchor", rope_anchor);
      Piece piece10 = r10.AddComponent<Piece>();
      piece10.m_name = "$mb_rope_anchor";
      piece10.m_description = "$mb_rope_anchor_desc";
      piece10.m_placeEffect = wood_floor.m_placeEffect;
      ZNetView nv3 = r10.AddComponent<ZNetView>();
      nv3.m_persistent = true;
      RopeAnchorComponent ropeanchor = r10.AddComponent<RopeAnchorComponent>();
      LineRenderer baseRope = raftMast.GetComponentInChildren<LineRenderer>(includeInactive: true);
      ropeanchor.m_rope = r10.AddComponent<LineRenderer>();
      ropeanchor.m_rope.material = new Material(baseRope.material);
      ropeanchor.m_rope.widthMultiplier = 0.05f;
      ropeanchor.m_rope.enabled = false;
      WearNTear wnt6 = r10.AddComponent<WearNTear>();
      wnt6.m_health = 1000f;
      wnt6.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
      wnt6.m_hitEffect = wood_floor_wnt.m_hitEffect;
      wnt6.m_noRoofWear = false;
      wnt6.m_supports = false;
      FixCollisionLayers(r10);
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Description = "$mb_rope_anchor_desc";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/rope_anchor");
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[2]
      {
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
      };
      pieceMan.AddPiece(new CustomPiece(r10, false, val));
      GameObject r9 = prefabMan.CreateEmptyPrefab("MBSail", true);
      Destroy(r9.GetComponent<BoxCollider>());
      Destroy(r9.GetComponent<MeshFilter>());
      Piece piece9 = r9.AddComponent<Piece>();
      piece9.m_name = "$mb_sail";
      piece9.m_description = "$mb_sail_desc";
      piece9.m_placeEffect = wood_floor.m_placeEffect;
      ZNetView nv2 = r9.GetComponent<ZNetView>();
      nv2.m_persistent = true;
      GameObject sailObject = new GameObject("Sail");
      sailObject.transform.parent = r9.transform;
      sailObject.layer = LayerMask.NameToLayer("piece_nonsolid");
      SailComponent sail = r9.AddComponent<SailComponent>();
      sail.m_sailObject = sailObject;
      sail.m_sailCloth = sailObject.AddComponent<Cloth>();
      sail.m_meshCollider = sailObject.AddComponent<MeshCollider>();
      sail.m_mesh = sailObject.GetComponent<SkinnedMeshRenderer>();
      sail.m_mesh.shadowCastingMode = ShadowCastingMode.TwoSided;
      sail.m_mesh.sharedMaterial = sailMat;
      WearNTear wnt5 = r9.AddComponent<WearNTear>();
      wnt5.m_health = 1000f;
      wnt5.m_noRoofWear = false;
      wnt5.m_noSupportWear = false;
      wnt5.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
      wnt5.m_hitEffect = wood_floor_wnt.m_hitEffect;
      MastComponent mast = r9.AddComponent<MastComponent>();
      mast.m_sailObject = sailObject;
      mast.m_sailCloth = sail.m_sailCloth;
      mast.m_allowSailRotation = false;
      mast.m_allowSailShrinking = true;
      r9.layer = LayerMask.NameToLayer("piece_nonsolid");
      SailCreatorComponent.m_sailPrefab = r9;
      PrefabManager.Instance.AddPrefab(r9);
      GameObject r8 = prefabMan.CreateEmptyPrefab("MBSailCreator_4", false);
      Piece piece8 = r8.AddComponent<Piece>();
      piece8.m_name = "$mb_sail_4";
      piece8.m_description = "$mb_sail_4_desc";
      piece8.m_placeEffect = wood_floor.m_placeEffect;
      SailCreatorComponent sailCreator2 = r8.AddComponent<SailCreatorComponent>();
      sailCreator2.m_sailSize = 4;
      MeshRenderer mesh2 = r8.GetComponent<MeshRenderer>();
      mesh2.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
      r8.layer = LayerMask.NameToLayer("piece_nonsolid");
      pieceMan.AddPiece(new CustomPiece(r8, false, new PieceConfig
      {
        PieceTable = "Hammer",
        Description = "$mb_sail_4_desc",
        Category = "ValheimRAFT",
        Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/customsail")
      }));
      GameObject r7 = prefabMan.CreateEmptyPrefab("MBSailCreator_3", false);
      Piece piece7 = r7.AddComponent<Piece>();
      piece7.m_name = "$mb_sail_3";
      piece7.m_description = "$mb_sail_3_desc";
      piece7.m_placeEffect = wood_floor.m_placeEffect;
      SailCreatorComponent sailCreator = r7.AddComponent<SailCreatorComponent>();
      sailCreator.m_sailSize = 3;
      MeshRenderer mesh = r7.GetComponent<MeshRenderer>();
      mesh.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
      r7.layer = LayerMask.NameToLayer("piece_nonsolid");
      pieceMan.AddPiece(new CustomPiece(r7, false, new PieceConfig
      {
        PieceTable = "Hammer",
        Name = "$mb_sail",
        Description = "$mb_sail_3_desc",
        Category = "ValheimRAFT",
        Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/customsail_tri")
      }));
      GameObject sourceObject4 = prefabMan.GetPrefab("wood_pole_log_4");
      GameObject r6 = prefabMan.CreateClonedPrefab("MBPier_Pole", sourceObject4);
      WearNTear wnt4 = r6.GetComponent<WearNTear>();
      wnt4.m_noRoofWear = false;
      Piece piece6 = r6.GetComponent<Piece>();
      piece6.m_waterPiece = true;
      PierComponent pier2 = r6.AddComponent<PierComponent>();
      pier2.m_segmentObject = prefabMan.CreateClonedPrefab("MBPier_Pole_Segment", sourceObject4);
      Destroy(pier2.m_segmentObject.GetComponent<ZNetView>());
      Destroy(pier2.m_segmentObject.GetComponent<Piece>());
      Destroy(pier2.m_segmentObject.GetComponent<WearNTear>());
      FixSnapPoints(r6);
      Transform[] transforms2 = pier2.m_segmentObject.GetComponentsInChildren<Transform>();
      for (int j = 0; j < transforms2.Length; j++)
      {
        if ((bool)transforms2[j] && transforms2[j].CompareTag("snappoint"))
        {
          Destroy(transforms2[j]);
        }
      }

      pier2.m_segmentHeight = 4f;
      pier2.m_baseOffset = -1f;
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Name = "$mb_pier (" + piece6.m_name + ")";
      val.Description = "$mb_pier_desc\n " + piece6.m_description;
      val.Category = "ValheimRAFT";
      val.Icon = piece6.m_icon;
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[1]
      {
        new RequirementConfig
        {
          Amount = 4,
          Item = "RoundLog",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r6, false, val));
      GameObject sourceObject3 = prefabMan.GetPrefab("stone_wall_4x2");
      GameObject r5 = prefabMan.CreateClonedPrefab("MBPier_Stone", sourceObject3);
      Piece piece5 = r5.GetComponent<Piece>();
      piece5.m_waterPiece = true;
      PierComponent pier = r5.AddComponent<PierComponent>();
      pier.m_segmentObject = prefabMan.CreateClonedPrefab("MBPier_Stone_Segment", sourceObject3);
      Destroy(pier.m_segmentObject.GetComponent<ZNetView>());
      Destroy(pier.m_segmentObject.GetComponent<Piece>());
      Destroy(pier.m_segmentObject.GetComponent<WearNTear>());
      FixSnapPoints(r5);
      Transform[] transforms = pier.m_segmentObject.GetComponentsInChildren<Transform>();
      for (int i = 0; i < transforms.Length; i++)
      {
        if ((bool)transforms[i] && transforms[i].CompareTag("snappoint"))
        {
          Destroy(transforms[i]);
        }
      }

      pier.m_segmentHeight = 2f;
      pier.m_baseOffset = 0f;
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Name = "$mb_pier (" + piece5.m_name + ")";
      val.Description = "$mb_pier_desc\n " + piece5.m_description;
      val.Category = "ValheimRAFT";
      val.Icon = piece5.m_icon;
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[1]
      {
        new RequirementConfig
        {
          Amount = 12,
          Item = "Stone",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r5, false, val));
      GameObject r4 = prefabMan.CreateClonedPrefab("MBBoardingRamp", boarding_ramp);
      GameObject floor = r4.transform.Find("Ramp/Segment/SegmentAnchor/Floor").gameObject;
      GameObject new_floor = Object.Instantiate(
        wood_floor.transform.Find("New/_Combined Mesh [high]").gameObject, floor.transform.parent,
        worldPositionStays: false);
      Destroy(floor);
      new_floor.transform.localPosition = new Vector3(1f, -52.55f, 0.5f);
      new_floor.transform.localScale = Vector3.one;
      new_floor.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
      Material woodMat =
        wood_pole2.transform.Find("New").GetComponent<MeshRenderer>().sharedMaterial;
      r4.transform.Find("Winch1/Pole").GetComponent<MeshRenderer>().sharedMaterial = woodMat;
      r4.transform.Find("Winch2/Pole").GetComponent<MeshRenderer>().sharedMaterial = woodMat;
      r4.transform.Find("Ramp/Segment/SegmentAnchor/Pole1").GetComponent<MeshRenderer>()
        .sharedMaterial = woodMat;
      r4.transform.Find("Ramp/Segment/SegmentAnchor/Pole2").GetComponent<MeshRenderer>()
        .sharedMaterial = woodMat;
      r4.transform.Find("Winch1/Cylinder").GetComponent<MeshRenderer>().sharedMaterial = woodMat;
      r4.transform.Find("Winch2/Cylinder").GetComponent<MeshRenderer>().sharedMaterial = woodMat;
      Material ropeMat = raftMast.GetComponentInChildren<LineRenderer>(includeInactive: true)
        .sharedMaterial;
      r4.transform.Find("Rope1").GetComponent<LineRenderer>().sharedMaterial = ropeMat;
      r4.transform.Find("Rope2").GetComponent<LineRenderer>().sharedMaterial = ropeMat;
      Piece piece4 = r4.AddComponent<Piece>();
      piece4.m_name = "$mb_boarding_ramp";
      piece4.m_description = "$mb_boarding_ramp_desc";
      piece4.m_placeEffect = wood_floor.m_placeEffect;
      ZNetView nv = r4.AddComponent<ZNetView>();
      nv.m_persistent = true;
      BoardingRampComponent boardingRamp2 = r4.AddComponent<BoardingRampComponent>();
      boardingRamp2.m_stateChangeDuration = 0.3f;
      boardingRamp2.m_segments = 5;
      WearNTear wnt3 = r4.AddComponent<WearNTear>();
      wnt3.m_health = 1000f;
      wnt3.m_destroyedEffect = wood_floor_wnt.m_destroyedEffect;
      wnt3.m_hitEffect = wood_floor_wnt.m_hitEffect;
      wnt3.m_noRoofWear = false;
      wnt3.m_supports = false;
      FixCollisionLayers(r4);
      FixSnapPoints(r4);
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Description = "$mb_boarding_ramp_desc";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/boarding_ramp");
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[2]
      {
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
      };
      pieceMan.AddPiece(new CustomPiece(r4, false, val));
      boarding_ramp = r4;
      GameObject r3 = prefabMan.CreateClonedPrefab("MBBoardingRamp_Wide", boarding_ramp);
      Piece piece3 = r3.GetComponent<Piece>();
      piece3.m_name = "$mb_boarding_ramp_wide";
      piece3.m_description = "$mb_boarding_ramp_wide_desc";
      BoardingRampComponent boardingRamp = r3.GetComponent<BoardingRampComponent>();
      boardingRamp.m_stateChangeDuration = 0.3f;
      boardingRamp.m_segments = 5;
      r3.transform.localScale = new Vector3(2f, 1f, 1f);
      FixSnapPoints(r3);
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Description = "$mb_boarding_ramp_wide_desc";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/boarding_ramp");
      val.Category = "ValheimRAFT";
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[2]
      {
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
      };
      pieceMan.AddPiece(new CustomPiece(r3, false, val));
      GameObject sourceObject2 = m_assetBundle.LoadAsset<GameObject>("dirt_floor");
      GameObject r2 = prefabMan.CreateClonedPrefab("MBDirtfloor_2x2", sourceObject2);
      r2.transform.localScale = new Vector3(2f, 1f, 2f);
      ZNetView netview2 = r2.AddComponent<ZNetView>();
      netview2.m_persistent = true;
      WearNTear wnt2 = r2.AddComponent<WearNTear>();
      wnt2.m_health = 1000f;
      Piece piece2 = r2.AddComponent<Piece>();
      piece2.m_placeEffect = wood_floor.m_placeEffect;
      CultivatableComponent cultivatable2 = r2.AddComponent<CultivatableComponent>();
      FixCollisionLayers(r2);
      FixSnapPoints(r2);
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Name = "$mb_dirt_floor_2x2";
      val.Description = "$mb_dirt_floor_2x2_desc";
      val.Category = "ValheimRAFT";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/dirtfloor_icon");
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[1]
      {
        new RequirementConfig
        {
          Amount = 4,
          Item = "Stone",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r2, false, val));
      GameObject sourceObject = m_assetBundle.LoadAsset<GameObject>("dirt_floor");
      GameObject r = prefabMan.CreateClonedPrefab("MBDirtfloor_1x1", sourceObject);
      r.transform.localScale = new Vector3(1f, 1f, 1f);
      ZNetView netview = r.AddComponent<ZNetView>();
      netview.m_persistent = true;
      WearNTear wnt = r.AddComponent<WearNTear>();
      wnt.m_health = 1000f;
      Piece piece = r.AddComponent<Piece>();
      piece.m_placeEffect = wood_floor.m_placeEffect;
      CultivatableComponent cultivatable = r.AddComponent<CultivatableComponent>();
      FixCollisionLayers(r);
      FixSnapPoints(r);
      val = new PieceConfig();
      val.PieceTable = "Hammer";
      val.Name = "$mb_dirt_floor_1x1";
      val.Description = "$mb_dirt_floor_1x1_desc";
      val.Category = "ValheimRAFT";
      val.Icon = m_assetBundle.LoadAsset<Sprite>("Assets/Sprite/dirtfloor_icon");
      val.Requirements = (RequirementConfig[])(object)new RequirementConfig[1]
      {
        new RequirementConfig
        {
          Amount = 1,
          Item = "Stone",
          Recover = true
        }
      };
      pieceMan.AddPiece(new CustomPiece(r, false, val));
    }

    private void FixSnapPoints(GameObject r)
    {
      Transform[] componentsInChildren = r.GetComponentsInChildren<Transform>(true);
      for (int index = 0; index < componentsInChildren.Length; ++index)
      {
        if (((Object)componentsInChildren[index]).name.StartsWith("_snappoint"))
          ((Component)componentsInChildren[index]).tag = "snappoint";
      }
    }

    private void FixCollisionLayers(GameObject r)
    {
      int layer = LayerMask.NameToLayer("piece");
      r.layer = layer;
      foreach (Component componentsInChild in ((Component)r.transform)
               .GetComponentsInChildren<Transform>(true))
        componentsInChild.gameObject.layer = layer;
    }

    private static void FixedRopes(GameObject r)
    {
      LineAttach[] componentsInChildren = r.GetComponentsInChildren<LineAttach>();
      for (int index = 0; index < componentsInChildren.Length; ++index)
      {
        ((Component)componentsInChildren[index]).GetComponent<LineRenderer>().positionCount = 2;
        componentsInChildren[index].m_attachments.Clear();
        componentsInChildren[index].m_attachments.Add(r.transform);
      }
    }

    private void PrintCollisionMatrix()
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.AppendLine("");
      stringBuilder.Append(" ".PadLeft(23));
      for (int index = 0; index < 32; ++index)
        stringBuilder.Append(index.ToString().PadRight(3));
      stringBuilder.AppendLine("");
      for (int index1 = 0; index1 < 32; ++index1)
      {
        stringBuilder.Append(LayerMask.LayerToName(index1).PadLeft(20) +
                             index1.ToString().PadLeft(3));
        for (int index2 = 0; index2 < 32; ++index2)
        {
          bool flag = !Physics.GetIgnoreLayerCollision(index1, index2);
          stringBuilder.Append(flag ? "[X]" : "[ ]");
        }

        stringBuilder.AppendLine("");
      }

      stringBuilder.AppendLine("");
      ZLog.Log((object)stringBuilder.ToString());
    }
  }
}