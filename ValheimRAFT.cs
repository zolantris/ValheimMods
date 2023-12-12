// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.ValheimRAFT
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

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
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace ValheimRAFT
{
  [BepInPlugin("BepIn.Sarcen.ValheimRAFT", "ValheimRAFT", "1.4.9")]
  [BepInDependency]
  [NetworkCompatibility]
  public class ValheimRAFT : BaseUnityPlugin
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

    public static ValheimRAFT.ValheimRAFT Instance { get; private set; }

    public ConfigEntry<bool> MakeAllPiecesWaterProof { get; set; }

    public ConfigEntry<bool> AllowFlight { get; set; }

    private void Awake()
    {
      ValheimRAFT.ValheimRAFT.Instance = this;
      this.MakeAllPiecesWaterProof = this.Config.Bind<bool>("Server config", "MakeAllPiecesWaterProof", true, new ConfigDescription("Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.", (AcceptableValueBase) null, new object[1]
      {
        (object) new ConfigurationManagerAttributes()
        {
          IsAdminOnly = true
        }
      }));
      this.AllowFlight = this.Config.Bind<bool>("Server config", "AllowFlight", false, new ConfigDescription("Allow the raft to fly (jump\\crouch to go up and down)", (AcceptableValueBase) null, new object[1]
      {
        (object) new ConfigurationManagerAttributes()
        {
          IsAdminOnly = true
        }
      }));
      ValheimRAFT.ValheimRAFT.m_harmony = new Harmony("Harmony.Sarcen.ValheimRAFT");
      ValheimRAFT.ValheimRAFT.m_harmony.PatchAll();
      int layer = LayerMask.NameToLayer("vehicle");
      for (int index = 0; index < 32; ++index)
        Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, index, Physics.GetIgnoreLayerCollision(layer, index));
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("vehicle"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("piece"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("character"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("smoke"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("character_ghost"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("weapon"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("blocker"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("pathblocker"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("viewblock"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("character_net"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("character_noenv"), true);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("Default_small"), false);
      Physics.IgnoreLayerCollision(ValheimRAFT.ValheimRAFT.CustomRaftLayer, LayerMask.NameToLayer("Default"), false);
      CommandManager.Instance.AddConsoleCommand((ConsoleCommand) new CreativeModeConsoleCommand());
      CommandManager.Instance.AddConsoleCommand((ConsoleCommand) new MoveRaftConsoleCommand());
      CommandManager.Instance.AddConsoleCommand((ConsoleCommand) new HideRaftConsoleCommand());
      CommandManager.Instance.AddConsoleCommand((ConsoleCommand) new RecoverRaftConsoleCommand());
      PrefabManager.OnVanillaPrefabsAvailable += new Action(this.LoadCustomTextures);
      PrefabManager.OnVanillaPrefabsAvailable += new Action(this.AddCustomPieces);
    }

    private void LoadCustomTextures()
    {
      CustomTextureGroup customTextureGroup1 = CustomTextureGroup.Load("Sails");
      for (int index = 0; index < customTextureGroup1.Textures.Count; ++index)
      {
        CustomTextureGroup.CustomTexture texture = customTextureGroup1.Textures[index];
        texture.Texture.wrapMode = (TextureWrapMode) 1;
        if (Object.op_Implicit((Object) texture.Normal))
          texture.Normal.wrapMode = (TextureWrapMode) 1;
      }
      CustomTextureGroup customTextureGroup2 = CustomTextureGroup.Load("Patterns");
      for (int index = 0; index < customTextureGroup2.Textures.Count; ++index)
      {
        CustomTextureGroup.CustomTexture texture = customTextureGroup2.Textures[index];
        texture.Texture.filterMode = (FilterMode) 0;
        texture.Texture.wrapMode = (TextureWrapMode) 0;
        if (Object.op_Implicit((Object) texture.Normal))
          texture.Normal.wrapMode = (TextureWrapMode) 0;
      }
      CustomTextureGroup customTextureGroup3 = CustomTextureGroup.Load("Logos");
      for (int index = 0; index < customTextureGroup3.Textures.Count; ++index)
      {
        CustomTextureGroup.CustomTexture texture = customTextureGroup3.Textures[index];
        texture.Texture.wrapMode = (TextureWrapMode) 1;
        if (Object.op_Implicit((Object) texture.Normal))
          texture.Normal.wrapMode = (TextureWrapMode) 1;
      }
    }

    internal void AddCustomPieces()
    {
      if (this.m_customItemsAdded)
        return;
      this.m_customItemsAdded = true;
      ValheimRAFT.ValheimRAFT.m_assetBundle = AssetUtils.LoadAssetBundleFromResources("valheimraft", Assembly.GetExecutingAssembly());
      GameObject gameObject1 = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("Assets/boarding_ramp.prefab");
      GameObject gameObject2 = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("Assets/steering_wheel.prefab");
      GameObject gameObject3 = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("Assets/rope_ladder.prefab");
      GameObject gameObject4 = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("Assets/rope_anchor.prefab");
      SpriteAtlas spriteAtlas = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<SpriteAtlas>("Assets/icons.spriteatlas");
      Material material = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<Material>("Assets/SailMat.mat");
      PrefabManager instance1 = PrefabManager.Instance;
      GameObject prefab1 = instance1.GetPrefab("Raft");
      Piece component1 = instance1.GetPrefab("wood_pole2").GetComponent<Piece>();
      Piece component2 = instance1.GetPrefab("wood_floor").GetComponent<Piece>();
      WearNTear component3 = ((Component) component2).GetComponent<WearNTear>();
      GameObject gameObject5 = ((Component) prefab1.transform.Find("ship/visual/mast")).gameObject;
      GameObject gameObject6 = ((Component) instance1.GetPrefab("Karve").transform.Find("ship/mast")).gameObject;
      GameObject gameObject7 = ((Component) instance1.GetPrefab("VikingShip").transform.Find("ship/visual/Mast")).gameObject;
      PieceManager instance2 = PieceManager.Instance;
      GameObject clonedPrefab1 = instance1.CreateClonedPrefab("MBRaft", prefab1);
      ((Component) clonedPrefab1.transform.Find("ship/visual/mast")).gameObject.SetActive(false);
      ((Component) clonedPrefab1.transform.Find("interactive/mast")).gameObject.SetActive(false);
      clonedPrefab1.GetComponent<Rigidbody>().mass = 1000f;
      Object.Destroy((Object) ((Component) clonedPrefab1.transform.Find("ship/colliders/log")).gameObject);
      Object.Destroy((Object) ((Component) clonedPrefab1.transform.Find("ship/colliders/log (1)")).gameObject);
      Object.Destroy((Object) ((Component) clonedPrefab1.transform.Find("ship/colliders/log (2)")).gameObject);
      Object.Destroy((Object) ((Component) clonedPrefab1.transform.Find("ship/colliders/log (3)")).gameObject);
      Piece component4 = clonedPrefab1.GetComponent<Piece>();
      component4.m_name = "$mb_raft";
      component4.m_description = "$mb_raft_desc";
      clonedPrefab1.GetComponent<ZNetView>().m_persistent = true;
      WearNTear component5 = clonedPrefab1.GetComponent<WearNTear>();
      component5.m_health = 10000f;
      component5.m_noRoofWear = false;
      clonedPrefab1.GetComponent<ImpactEffect>().m_damageToSelf = false;
      PieceManager pieceManager1 = instance2;
      GameObject gameObject8 = clonedPrefab1;
      PieceConfig pieceConfig1 = new PieceConfig();
      pieceConfig1.PieceTable = "Hammer";
      pieceConfig1.Description = "$mb_raft_desc";
      pieceConfig1.Category = nameof (ValheimRAFT);
      pieceConfig1.Requirements = new RequirementConfig[1]
      {
        new RequirementConfig() { Amount = 20, Item = "Wood" }
      };
      PieceConfig pieceConfig2 = pieceConfig1;
      CustomPiece customPiece1 = new CustomPiece(gameObject8, false, pieceConfig2);
      pieceManager1.AddPiece(customPiece1);
      GameObject clonedPrefab2 = instance1.CreateClonedPrefab("MBRaftMast", gameObject5);
      Piece piece1 = clonedPrefab2.AddComponent<Piece>();
      piece1.m_name = "$mb_raft_mast";
      piece1.m_description = "$mb_raft_mast_desc";
      piece1.m_placeEffect = component2.m_placeEffect;
      clonedPrefab2.AddComponent<ZNetView>().m_persistent = true;
      MastComponent mastComponent1 = clonedPrefab2.AddComponent<MastComponent>();
      mastComponent1.m_sailObject = ((Component) clonedPrefab2.transform.Find("Sail")).gameObject;
      mastComponent1.m_sailCloth = mastComponent1.m_sailObject.GetComponentInChildren<Cloth>();
      WearNTear wearNtear1 = clonedPrefab2.AddComponent<WearNTear>();
      wearNtear1.m_health = 1000f;
      wearNtear1.m_destroyedEffect = component3.m_destroyedEffect;
      wearNtear1.m_hitEffect = component3.m_hitEffect;
      wearNtear1.m_noRoofWear = false;
      ValheimRAFT.ValheimRAFT.FixedRopes(clonedPrefab2);
      this.FixCollisionLayers(clonedPrefab2);
      PieceManager pieceManager2 = instance2;
      GameObject gameObject9 = clonedPrefab2;
      PieceConfig pieceConfig3 = new PieceConfig();
      pieceConfig3.PieceTable = "Hammer";
      pieceConfig3.Description = "$mb_raft_mast_desc";
      pieceConfig3.Icon = spriteAtlas.GetSprite("raftmast");
      pieceConfig3.Category = nameof (ValheimRAFT);
      pieceConfig3.Requirements = new RequirementConfig[2]
      {
        new RequirementConfig()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig()
        {
          Amount = 6,
          Item = "DeerHide",
          Recover = true
        }
      };
      PieceConfig pieceConfig4 = pieceConfig3;
      CustomPiece customPiece2 = new CustomPiece(gameObject9, false, pieceConfig4);
      pieceManager2.AddPiece(customPiece2);
      GameObject clonedPrefab3 = instance1.CreateClonedPrefab("MBKarveMast", gameObject6);
      Piece piece2 = clonedPrefab3.AddComponent<Piece>();
      piece2.m_name = "$mb_karve_mast";
      piece2.m_description = "$mb_karve_mast_desc";
      piece2.m_placeEffect = component2.m_placeEffect;
      clonedPrefab3.AddComponent<ZNetView>().m_persistent = true;
      MastComponent mastComponent2 = clonedPrefab3.AddComponent<MastComponent>();
      mastComponent2.m_sailObject = ((Component) clonedPrefab3.transform.Find("Sail")).gameObject;
      mastComponent2.m_sailCloth = mastComponent2.m_sailObject.GetComponentInChildren<Cloth>();
      WearNTear wearNtear2 = clonedPrefab3.AddComponent<WearNTear>();
      wearNtear2.m_health = 1000f;
      wearNtear2.m_noRoofWear = false;
      wearNtear2.m_destroyedEffect = component3.m_destroyedEffect;
      wearNtear2.m_hitEffect = component3.m_hitEffect;
      ValheimRAFT.ValheimRAFT.FixedRopes(clonedPrefab3);
      this.FixCollisionLayers(clonedPrefab3);
      PieceManager pieceManager3 = instance2;
      GameObject gameObject10 = clonedPrefab3;
      PieceConfig pieceConfig5 = new PieceConfig();
      pieceConfig5.PieceTable = "Hammer";
      pieceConfig5.Description = "$mb_karve_mast_desc";
      pieceConfig5.Icon = spriteAtlas.GetSprite("karvemast");
      pieceConfig5.Category = nameof (ValheimRAFT);
      pieceConfig5.Requirements = new RequirementConfig[3]
      {
        new RequirementConfig()
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig()
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        },
        new RequirementConfig()
        {
          Amount = 6,
          Item = "TrollHide",
          Recover = true
        }
      };
      PieceConfig pieceConfig6 = pieceConfig5;
      CustomPiece customPiece3 = new CustomPiece(gameObject10, false, pieceConfig6);
      pieceManager3.AddPiece(customPiece3);
      GameObject clonedPrefab4 = instance1.CreateClonedPrefab("MBVikingShipMast", gameObject7);
      Piece piece3 = clonedPrefab4.AddComponent<Piece>();
      piece3.m_name = "$mb_vikingship_mast";
      piece3.m_description = "$mb_vikingship_mast_desc";
      piece3.m_placeEffect = component2.m_placeEffect;
      clonedPrefab4.AddComponent<ZNetView>().m_persistent = true;
      MastComponent mastComponent3 = clonedPrefab4.AddComponent<MastComponent>();
      mastComponent3.m_sailObject = ((Component) clonedPrefab4.transform.Find("Sail")).gameObject;
      mastComponent3.m_sailCloth = mastComponent3.m_sailObject.GetComponentInChildren<Cloth>();
      WearNTear wearNtear3 = clonedPrefab4.AddComponent<WearNTear>();
      wearNtear3.m_health = 1000f;
      wearNtear3.m_noRoofWear = false;
      wearNtear3.m_destroyedEffect = component3.m_destroyedEffect;
      wearNtear3.m_hitEffect = component3.m_hitEffect;
      ValheimRAFT.ValheimRAFT.FixedRopes(clonedPrefab4);
      this.FixCollisionLayers(clonedPrefab4);
      PieceManager pieceManager4 = instance2;
      GameObject gameObject11 = clonedPrefab4;
      PieceConfig pieceConfig7 = new PieceConfig();
      pieceConfig7.PieceTable = "Hammer";
      pieceConfig7.Description = "$mb_vikingship_mast_desc";
      pieceConfig7.Icon = spriteAtlas.GetSprite("vikingmast");
      pieceConfig7.Category = nameof (ValheimRAFT);
      pieceConfig7.Requirements = new RequirementConfig[3]
      {
        new RequirementConfig()
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig()
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        },
        new RequirementConfig()
        {
          Amount = 6,
          Item = "WolfPelt",
          Recover = true
        }
      };
      PieceConfig pieceConfig8 = pieceConfig7;
      CustomPiece customPiece4 = new CustomPiece(gameObject11, false, pieceConfig8);
      pieceManager4.AddPiece(customPiece4);
      GameObject clonedPrefab5 = instance1.CreateClonedPrefab("MBRudder", gameObject2);
      Piece piece4 = clonedPrefab5.AddComponent<Piece>();
      piece4.m_name = "$mb_rudder";
      piece4.m_description = "$mb_rudder_desc";
      piece4.m_placeEffect = component2.m_placeEffect;
      clonedPrefab5.AddComponent<ZNetView>().m_persistent = true;
      RudderComponent rudderComponent = clonedPrefab5.AddComponent<RudderComponent>();
      rudderComponent.m_controls = clonedPrefab5.AddComponent<ShipControlls>();
      rudderComponent.m_controls.m_hoverText = "$mb_rudder_use";
      rudderComponent.m_controls.m_attachPoint = clonedPrefab5.transform.Find("attachpoint");
      rudderComponent.m_controls.m_attachAnimation = "Standing Torch Idle right";
      rudderComponent.m_controls.m_detachOffset = new Vector3(0.0f, 0.0f, 0.0f);
      rudderComponent.m_wheel = clonedPrefab5.transform.Find("controls/wheel");
      rudderComponent.UpdateSpokes();
      WearNTear wearNtear4 = clonedPrefab5.AddComponent<WearNTear>();
      wearNtear4.m_health = 1000f;
      wearNtear4.m_noRoofWear = false;
      wearNtear4.m_destroyedEffect = component3.m_destroyedEffect;
      wearNtear4.m_hitEffect = component3.m_hitEffect;
      this.FixSnapPoints(clonedPrefab5);
      this.FixCollisionLayers(clonedPrefab5);
      PieceManager pieceManager5 = instance2;
      GameObject gameObject12 = clonedPrefab5;
      PieceConfig pieceConfig9 = new PieceConfig();
      pieceConfig9.PieceTable = "Hammer";
      pieceConfig9.Description = "$mb_rudder_desc";
      pieceConfig9.Icon = spriteAtlas.GetSprite("steering_wheel");
      pieceConfig9.Category = nameof (ValheimRAFT);
      pieceConfig9.Requirements = new RequirementConfig[1]
      {
        new RequirementConfig()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      };
      PieceConfig pieceConfig10 = pieceConfig9;
      CustomPiece customPiece5 = new CustomPiece(gameObject12, false, pieceConfig10);
      pieceManager5.AddPiece(customPiece5);
      GameObject clonedPrefab6 = instance1.CreateClonedPrefab("MBRopeLadder", gameObject3);
      Piece piece5 = clonedPrefab6.AddComponent<Piece>();
      piece5.m_name = "$mb_rope_ladder";
      piece5.m_description = "$mb_rope_ladder_desc";
      piece5.m_placeEffect = component2.m_placeEffect;
      ((StaticTarget) piece5).m_primaryTarget = false;
      ((StaticTarget) piece5).m_randomTarget = false;
      clonedPrefab6.AddComponent<ZNetView>().m_persistent = true;
      RopeLadderComponent ropeLadderComponent = clonedPrefab6.AddComponent<RopeLadderComponent>();
      LineRenderer componentInChildren1 = gameObject5.GetComponentInChildren<LineRenderer>(true);
      ropeLadderComponent.m_ropeLine = ((Component) ropeLadderComponent).GetComponent<LineRenderer>();
      ((Renderer) ropeLadderComponent.m_ropeLine).material = new Material(((Renderer) componentInChildren1).material);
      ropeLadderComponent.m_ropeLine.textureMode = (LineTextureMode) 1;
      ropeLadderComponent.m_ropeLine.widthMultiplier = 0.05f;
      ropeLadderComponent.m_stepObject = ((Component) ((Component) ropeLadderComponent).transform.Find("step")).gameObject;
      ((Renderer) ropeLadderComponent.m_stepObject.GetComponentInChildren<MeshRenderer>()).material = new Material(((Renderer) ((Component) component2).GetComponentInChildren<MeshRenderer>()).material);
      WearNTear wearNtear5 = clonedPrefab6.AddComponent<WearNTear>();
      wearNtear5.m_health = 10000f;
      wearNtear5.m_destroyedEffect = component3.m_destroyedEffect;
      wearNtear5.m_hitEffect = component3.m_hitEffect;
      wearNtear5.m_noRoofWear = false;
      wearNtear5.m_supports = false;
      this.FixCollisionLayers(clonedPrefab6);
      PieceManager pieceManager6 = instance2;
      GameObject gameObject13 = clonedPrefab6;
      PieceConfig pieceConfig11 = new PieceConfig();
      pieceConfig11.PieceTable = "Hammer";
      pieceConfig11.Description = "$mb_rope_ladder_desc";
      pieceConfig11.Icon = spriteAtlas.GetSprite("rope_ladder");
      pieceConfig11.Category = nameof (ValheimRAFT);
      pieceConfig11.Requirements = new RequirementConfig[1]
      {
        new RequirementConfig()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      };
      PieceConfig pieceConfig12 = pieceConfig11;
      CustomPiece customPiece6 = new CustomPiece(gameObject13, false, pieceConfig12);
      pieceManager6.AddPiece(customPiece6);
      GameObject clonedPrefab7 = instance1.CreateClonedPrefab("MBRopeAnchor", gameObject4);
      Piece piece6 = clonedPrefab7.AddComponent<Piece>();
      piece6.m_name = "$mb_rope_anchor";
      piece6.m_description = "$mb_rope_anchor_desc";
      piece6.m_placeEffect = component2.m_placeEffect;
      clonedPrefab7.AddComponent<ZNetView>().m_persistent = true;
      RopeAnchorComponent ropeAnchorComponent = clonedPrefab7.AddComponent<RopeAnchorComponent>();
      LineRenderer componentInChildren2 = gameObject5.GetComponentInChildren<LineRenderer>(true);
      ropeAnchorComponent.m_rope = clonedPrefab7.AddComponent<LineRenderer>();
      ((Renderer) ropeAnchorComponent.m_rope).material = new Material(((Renderer) componentInChildren2).material);
      ropeAnchorComponent.m_rope.widthMultiplier = 0.05f;
      ((Renderer) ropeAnchorComponent.m_rope).enabled = false;
      WearNTear wearNtear6 = clonedPrefab7.AddComponent<WearNTear>();
      wearNtear6.m_health = 1000f;
      wearNtear6.m_destroyedEffect = component3.m_destroyedEffect;
      wearNtear6.m_hitEffect = component3.m_hitEffect;
      wearNtear6.m_noRoofWear = false;
      wearNtear6.m_supports = false;
      this.FixCollisionLayers(clonedPrefab7);
      PieceManager pieceManager7 = instance2;
      GameObject gameObject14 = clonedPrefab7;
      PieceConfig pieceConfig13 = new PieceConfig();
      pieceConfig13.PieceTable = "Hammer";
      pieceConfig13.Description = "$mb_rope_anchor_desc";
      pieceConfig13.Icon = spriteAtlas.GetSprite("rope_anchor");
      pieceConfig13.Category = nameof (ValheimRAFT);
      pieceConfig13.Requirements = new RequirementConfig[2]
      {
        new RequirementConfig()
        {
          Amount = 1,
          Item = "Iron",
          Recover = true
        },
        new RequirementConfig()
        {
          Amount = 4,
          Item = "IronNails",
          Recover = true
        }
      };
      PieceConfig pieceConfig14 = pieceConfig13;
      CustomPiece customPiece7 = new CustomPiece(gameObject14, false, pieceConfig14);
      pieceManager7.AddPiece(customPiece7);
      GameObject emptyPrefab1 = instance1.CreateEmptyPrefab("MBSail", true);
      Object.Destroy((Object) emptyPrefab1.GetComponent<BoxCollider>());
      Object.Destroy((Object) emptyPrefab1.GetComponent<MeshFilter>());
      Piece piece7 = emptyPrefab1.AddComponent<Piece>();
      piece7.m_name = "$mb_sail";
      piece7.m_description = "$mb_sail_desc";
      piece7.m_placeEffect = component2.m_placeEffect;
      emptyPrefab1.GetComponent<ZNetView>().m_persistent = true;
      GameObject gameObject15 = new GameObject("Sail");
      gameObject15.transform.parent = emptyPrefab1.transform;
      gameObject15.layer = LayerMask.NameToLayer("piece_nonsolid");
      SailComponent sailComponent = emptyPrefab1.AddComponent<SailComponent>();
      sailComponent.m_sailObject = gameObject15;
      sailComponent.m_sailCloth = gameObject15.AddComponent<Cloth>();
      sailComponent.m_meshCollider = gameObject15.AddComponent<MeshCollider>();
      sailComponent.m_mesh = gameObject15.GetComponent<SkinnedMeshRenderer>();
      ((Renderer) sailComponent.m_mesh).shadowCastingMode = (ShadowCastingMode) 2;
      ((Renderer) sailComponent.m_mesh).sharedMaterial = material;
      WearNTear wearNtear7 = emptyPrefab1.AddComponent<WearNTear>();
      wearNtear7.m_health = 1000f;
      wearNtear7.m_noRoofWear = false;
      wearNtear7.m_noSupportWear = false;
      wearNtear7.m_destroyedEffect = component3.m_destroyedEffect;
      wearNtear7.m_hitEffect = component3.m_hitEffect;
      MastComponent mastComponent4 = emptyPrefab1.AddComponent<MastComponent>();
      mastComponent4.m_sailObject = gameObject15;
      mastComponent4.m_sailCloth = sailComponent.m_sailCloth;
      mastComponent4.m_allowSailRotation = false;
      mastComponent4.m_allowSailShrinking = true;
      emptyPrefab1.layer = LayerMask.NameToLayer("piece_nonsolid");
      SailCreatorComponent.m_sailPrefab = emptyPrefab1;
      PrefabManager.Instance.AddPrefab(emptyPrefab1);
      GameObject emptyPrefab2 = instance1.CreateEmptyPrefab("MBSailCreator_4", false);
      Piece piece8 = emptyPrefab2.AddComponent<Piece>();
      piece8.m_name = "$mb_sail_4";
      piece8.m_description = "$mb_sail_4_desc";
      piece8.m_placeEffect = component2.m_placeEffect;
      emptyPrefab2.AddComponent<SailCreatorComponent>().m_sailSize = 4;
      ((Component) emptyPrefab2.GetComponent<MeshRenderer>()).transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
      emptyPrefab2.layer = LayerMask.NameToLayer("piece_nonsolid");
      instance2.AddPiece(new CustomPiece(emptyPrefab2, false, new PieceConfig()
      {
        PieceTable = "Hammer",
        Description = "$mb_sail_4_desc",
        Category = nameof (ValheimRAFT),
        Icon = spriteAtlas.GetSprite("customsail")
      }));
      GameObject emptyPrefab3 = instance1.CreateEmptyPrefab("MBSailCreator_3", false);
      Piece piece9 = emptyPrefab3.AddComponent<Piece>();
      piece9.m_name = "$mb_sail_3";
      piece9.m_description = "$mb_sail_3_desc";
      piece9.m_placeEffect = component2.m_placeEffect;
      emptyPrefab3.AddComponent<SailCreatorComponent>().m_sailSize = 3;
      ((Component) emptyPrefab3.GetComponent<MeshRenderer>()).transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
      emptyPrefab3.layer = LayerMask.NameToLayer("piece_nonsolid");
      instance2.AddPiece(new CustomPiece(emptyPrefab3, false, new PieceConfig()
      {
        PieceTable = "Hammer",
        Name = "$mb_sail",
        Description = "$mb_sail_3_desc",
        Category = nameof (ValheimRAFT),
        Icon = spriteAtlas.GetSprite("customsail_tri")
      }));
      GameObject prefab2 = instance1.GetPrefab("wood_pole_log_4");
      GameObject clonedPrefab8 = instance1.CreateClonedPrefab("MBPier_Pole", prefab2);
      clonedPrefab8.GetComponent<WearNTear>().m_noRoofWear = false;
      Piece component6 = clonedPrefab8.GetComponent<Piece>();
      component6.m_waterPiece = true;
      PierComponent pierComponent1 = clonedPrefab8.AddComponent<PierComponent>();
      pierComponent1.m_segmentObject = instance1.CreateClonedPrefab("MBPier_Pole_Segment", prefab2);
      Object.Destroy((Object) pierComponent1.m_segmentObject.GetComponent<ZNetView>());
      Object.Destroy((Object) pierComponent1.m_segmentObject.GetComponent<Piece>());
      Object.Destroy((Object) pierComponent1.m_segmentObject.GetComponent<WearNTear>());
      this.FixSnapPoints(clonedPrefab8);
      Transform[] componentsInChildren1 = pierComponent1.m_segmentObject.GetComponentsInChildren<Transform>();
      for (int index = 0; index < componentsInChildren1.Length; ++index)
      {
        if (Object.op_Implicit((Object) componentsInChildren1[index]) && ((Component) componentsInChildren1[index]).CompareTag("snappoint"))
          Object.Destroy((Object) componentsInChildren1[index]);
      }
      pierComponent1.m_segmentHeight = 4f;
      pierComponent1.m_baseOffset = -1f;
      PieceManager pieceManager8 = instance2;
      GameObject gameObject16 = clonedPrefab8;
      PieceConfig pieceConfig15 = new PieceConfig();
      pieceConfig15.PieceTable = "Hammer";
      pieceConfig15.Name = "$mb_pier (" + component6.m_name + ")";
      pieceConfig15.Description = "$mb_pier_desc\n " + component6.m_description;
      pieceConfig15.Category = nameof (ValheimRAFT);
      pieceConfig15.Icon = component6.m_icon;
      pieceConfig15.Requirements = new RequirementConfig[1]
      {
        new RequirementConfig()
        {
          Amount = 4,
          Item = "RoundLog",
          Recover = true
        }
      };
      PieceConfig pieceConfig16 = pieceConfig15;
      CustomPiece customPiece8 = new CustomPiece(gameObject16, false, pieceConfig16);
      pieceManager8.AddPiece(customPiece8);
      GameObject prefab3 = instance1.GetPrefab("stone_wall_4x2");
      GameObject clonedPrefab9 = instance1.CreateClonedPrefab("MBPier_Stone", prefab3);
      Piece component7 = clonedPrefab9.GetComponent<Piece>();
      component7.m_waterPiece = true;
      PierComponent pierComponent2 = clonedPrefab9.AddComponent<PierComponent>();
      pierComponent2.m_segmentObject = instance1.CreateClonedPrefab("MBPier_Stone_Segment", prefab3);
      Object.Destroy((Object) pierComponent2.m_segmentObject.GetComponent<ZNetView>());
      Object.Destroy((Object) pierComponent2.m_segmentObject.GetComponent<Piece>());
      Object.Destroy((Object) pierComponent2.m_segmentObject.GetComponent<WearNTear>());
      this.FixSnapPoints(clonedPrefab9);
      Transform[] componentsInChildren2 = pierComponent2.m_segmentObject.GetComponentsInChildren<Transform>();
      for (int index = 0; index < componentsInChildren2.Length; ++index)
      {
        if (Object.op_Implicit((Object) componentsInChildren2[index]) && ((Component) componentsInChildren2[index]).CompareTag("snappoint"))
          Object.Destroy((Object) componentsInChildren2[index]);
      }
      pierComponent2.m_segmentHeight = 2f;
      pierComponent2.m_baseOffset = 0.0f;
      PieceManager pieceManager9 = instance2;
      GameObject gameObject17 = clonedPrefab9;
      PieceConfig pieceConfig17 = new PieceConfig();
      pieceConfig17.PieceTable = "Hammer";
      pieceConfig17.Name = "$mb_pier (" + component7.m_name + ")";
      pieceConfig17.Description = "$mb_pier_desc\n " + component7.m_description;
      pieceConfig17.Category = nameof (ValheimRAFT);
      pieceConfig17.Icon = component7.m_icon;
      pieceConfig17.Requirements = new RequirementConfig[1]
      {
        new RequirementConfig()
        {
          Amount = 12,
          Item = "Stone",
          Recover = true
        }
      };
      PieceConfig pieceConfig18 = pieceConfig17;
      CustomPiece customPiece9 = new CustomPiece(gameObject17, false, pieceConfig18);
      pieceManager9.AddPiece(customPiece9);
      GameObject clonedPrefab10 = instance1.CreateClonedPrefab("MBBoardingRamp", gameObject1);
      GameObject gameObject18 = ((Component) clonedPrefab10.transform.Find("Ramp/Segment/SegmentAnchor/Floor")).gameObject;
      GameObject gameObject19 = Object.Instantiate<GameObject>(((Component) ((Component) component2).transform.Find("New/_Combined Mesh [high]")).gameObject, gameObject18.transform.parent, false);
      Object.Destroy((Object) gameObject18);
      gameObject19.transform.localPosition = new Vector3(1f, -52.55f, 0.5f);
      gameObject19.transform.localScale = Vector3.one;
      gameObject19.transform.localRotation = Quaternion.Euler(0.0f, 90f, 0.0f);
      Material sharedMaterial1 = ((Renderer) ((Component) ((Component) component1).transform.Find("New")).GetComponent<MeshRenderer>()).sharedMaterial;
      ((Renderer) ((Component) clonedPrefab10.transform.Find("Winch1/Pole")).GetComponent<MeshRenderer>()).sharedMaterial = sharedMaterial1;
      ((Renderer) ((Component) clonedPrefab10.transform.Find("Winch2/Pole")).GetComponent<MeshRenderer>()).sharedMaterial = sharedMaterial1;
      ((Renderer) ((Component) clonedPrefab10.transform.Find("Ramp/Segment/SegmentAnchor/Pole1")).GetComponent<MeshRenderer>()).sharedMaterial = sharedMaterial1;
      ((Renderer) ((Component) clonedPrefab10.transform.Find("Ramp/Segment/SegmentAnchor/Pole2")).GetComponent<MeshRenderer>()).sharedMaterial = sharedMaterial1;
      ((Renderer) ((Component) clonedPrefab10.transform.Find("Winch1/Cylinder")).GetComponent<MeshRenderer>()).sharedMaterial = sharedMaterial1;
      ((Renderer) ((Component) clonedPrefab10.transform.Find("Winch2/Cylinder")).GetComponent<MeshRenderer>()).sharedMaterial = sharedMaterial1;
      Material sharedMaterial2 = ((Renderer) gameObject5.GetComponentInChildren<LineRenderer>(true)).sharedMaterial;
      ((Renderer) ((Component) clonedPrefab10.transform.Find("Rope1")).GetComponent<LineRenderer>()).sharedMaterial = sharedMaterial2;
      ((Renderer) ((Component) clonedPrefab10.transform.Find("Rope2")).GetComponent<LineRenderer>()).sharedMaterial = sharedMaterial2;
      Piece piece10 = clonedPrefab10.AddComponent<Piece>();
      piece10.m_name = "$mb_boarding_ramp";
      piece10.m_description = "$mb_boarding_ramp_desc";
      piece10.m_placeEffect = component2.m_placeEffect;
      clonedPrefab10.AddComponent<ZNetView>().m_persistent = true;
      BoardingRampComponent boardingRampComponent = clonedPrefab10.AddComponent<BoardingRampComponent>();
      boardingRampComponent.m_stateChangeDuration = 0.3f;
      boardingRampComponent.m_segments = 5;
      WearNTear wearNtear8 = clonedPrefab10.AddComponent<WearNTear>();
      wearNtear8.m_health = 1000f;
      wearNtear8.m_destroyedEffect = component3.m_destroyedEffect;
      wearNtear8.m_hitEffect = component3.m_hitEffect;
      wearNtear8.m_noRoofWear = false;
      wearNtear8.m_supports = false;
      this.FixCollisionLayers(clonedPrefab10);
      this.FixSnapPoints(clonedPrefab10);
      PieceManager pieceManager10 = instance2;
      GameObject gameObject20 = clonedPrefab10;
      PieceConfig pieceConfig19 = new PieceConfig();
      pieceConfig19.PieceTable = "Hammer";
      pieceConfig19.Description = "$mb_boarding_ramp_desc";
      pieceConfig19.Icon = spriteAtlas.GetSprite("boarding_ramp");
      pieceConfig19.Category = nameof (ValheimRAFT);
      pieceConfig19.Requirements = new RequirementConfig[2]
      {
        new RequirementConfig()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig()
        {
          Amount = 4,
          Item = "IronNails",
          Recover = true
        }
      };
      PieceConfig pieceConfig20 = pieceConfig19;
      CustomPiece customPiece10 = new CustomPiece(gameObject20, false, pieceConfig20);
      pieceManager10.AddPiece(customPiece10);
      GameObject gameObject21 = clonedPrefab10;
      GameObject clonedPrefab11 = instance1.CreateClonedPrefab("MBBoardingRamp_Wide", gameObject21);
      Piece component8 = clonedPrefab11.GetComponent<Piece>();
      component8.m_name = "$mb_boarding_ramp_wide";
      component8.m_description = "$mb_boarding_ramp_wide_desc";
      BoardingRampComponent component9 = clonedPrefab11.GetComponent<BoardingRampComponent>();
      component9.m_stateChangeDuration = 0.3f;
      component9.m_segments = 5;
      clonedPrefab11.transform.localScale = new Vector3(2f, 1f, 1f);
      this.FixSnapPoints(clonedPrefab11);
      PieceManager pieceManager11 = instance2;
      GameObject gameObject22 = clonedPrefab11;
      PieceConfig pieceConfig21 = new PieceConfig();
      pieceConfig21.PieceTable = "Hammer";
      pieceConfig21.Description = "$mb_boarding_ramp_wide_desc";
      pieceConfig21.Icon = spriteAtlas.GetSprite("boarding_ramp");
      pieceConfig21.Category = nameof (ValheimRAFT);
      pieceConfig21.Requirements = new RequirementConfig[2]
      {
        new RequirementConfig()
        {
          Amount = 20,
          Item = "Wood",
          Recover = true
        },
        new RequirementConfig()
        {
          Amount = 8,
          Item = "IronNails",
          Recover = true
        }
      };
      PieceConfig pieceConfig22 = pieceConfig21;
      CustomPiece customPiece11 = new CustomPiece(gameObject22, false, pieceConfig22);
      pieceManager11.AddPiece(customPiece11);
      GameObject gameObject23 = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
      GameObject clonedPrefab12 = instance1.CreateClonedPrefab("MBDirtfloor_2x2", gameObject23);
      clonedPrefab12.transform.localScale = new Vector3(2f, 1f, 2f);
      clonedPrefab12.AddComponent<ZNetView>().m_persistent = true;
      clonedPrefab12.AddComponent<WearNTear>().m_health = 1000f;
      clonedPrefab12.AddComponent<Piece>().m_placeEffect = component2.m_placeEffect;
      clonedPrefab12.AddComponent<CultivatableComponent>();
      this.FixCollisionLayers(clonedPrefab12);
      this.FixSnapPoints(clonedPrefab12);
      PieceManager pieceManager12 = instance2;
      GameObject gameObject24 = clonedPrefab12;
      PieceConfig pieceConfig23 = new PieceConfig();
      pieceConfig23.PieceTable = "Hammer";
      pieceConfig23.Name = "$mb_dirt_floor_2x2";
      pieceConfig23.Description = "$mb_dirt_floor_2x2_desc";
      pieceConfig23.Category = nameof (ValheimRAFT);
      pieceConfig23.Icon = spriteAtlas.GetSprite("dirtfloor_icon");
      pieceConfig23.Requirements = new RequirementConfig[1]
      {
        new RequirementConfig()
        {
          Amount = 4,
          Item = "Stone",
          Recover = true
        }
      };
      PieceConfig pieceConfig24 = pieceConfig23;
      CustomPiece customPiece12 = new CustomPiece(gameObject24, false, pieceConfig24);
      pieceManager12.AddPiece(customPiece12);
      GameObject gameObject25 = ValheimRAFT.ValheimRAFT.m_assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
      GameObject clonedPrefab13 = instance1.CreateClonedPrefab("MBDirtfloor_1x1", gameObject25);
      clonedPrefab13.transform.localScale = new Vector3(1f, 1f, 1f);
      clonedPrefab13.AddComponent<ZNetView>().m_persistent = true;
      clonedPrefab13.AddComponent<WearNTear>().m_health = 1000f;
      clonedPrefab13.AddComponent<Piece>().m_placeEffect = component2.m_placeEffect;
      clonedPrefab13.AddComponent<CultivatableComponent>();
      this.FixCollisionLayers(clonedPrefab13);
      this.FixSnapPoints(clonedPrefab13);
      PieceManager pieceManager13 = instance2;
      GameObject gameObject26 = clonedPrefab13;
      PieceConfig pieceConfig25 = new PieceConfig();
      pieceConfig25.PieceTable = "Hammer";
      pieceConfig25.Name = "$mb_dirt_floor_1x1";
      pieceConfig25.Description = "$mb_dirt_floor_1x1_desc";
      pieceConfig25.Category = nameof (ValheimRAFT);
      pieceConfig25.Icon = spriteAtlas.GetSprite("dirtfloor_icon");
      pieceConfig25.Requirements = new RequirementConfig[1]
      {
        new RequirementConfig()
        {
          Amount = 1,
          Item = "Stone",
          Recover = true
        }
      };
      PieceConfig pieceConfig26 = pieceConfig25;
      CustomPiece customPiece13 = new CustomPiece(gameObject26, false, pieceConfig26);
      pieceManager13.AddPiece(customPiece13);
    }

    private void FixSnapPoints(GameObject r)
    {
      Transform[] componentsInChildren = r.GetComponentsInChildren<Transform>(true);
      for (int index = 0; index < componentsInChildren.Length; ++index)
      {
        if (((Object) componentsInChildren[index]).name.StartsWith("_snappoint"))
          ((Component) componentsInChildren[index]).tag = "snappoint";
      }
    }

    private void FixCollisionLayers(GameObject r)
    {
      int layer = LayerMask.NameToLayer("piece");
      r.layer = layer;
      foreach (Component componentsInChild in ((Component) r.transform).GetComponentsInChildren<Transform>(true))
        componentsInChild.gameObject.layer = layer;
    }

    private static void FixedRopes(GameObject r)
    {
      LineAttach[] componentsInChildren = r.GetComponentsInChildren<LineAttach>();
      for (int index = 0; index < componentsInChildren.Length; ++index)
      {
        ((Component) componentsInChildren[index]).GetComponent<LineRenderer>().positionCount = 2;
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
        stringBuilder.Append(LayerMask.LayerToName(index1).PadLeft(20) + index1.ToString().PadLeft(3));
        for (int index2 = 0; index2 < 32; ++index2)
        {
          bool flag = !Physics.GetIgnoreLayerCollision(index1, index2);
          stringBuilder.Append(flag ? "[X]" : "[ ]");
        }
        stringBuilder.AppendLine("");
      }
      stringBuilder.AppendLine("");
      ZLog.Log((object) stringBuilder.ToString());
    }
  }
}
