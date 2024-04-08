// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using Jotunn;
// using Jotunn.Configs;
// using Jotunn.Entities;
// using Jotunn.Managers;
// using Jotunn.Utils;
// using Unity.IO.LowLevel.Unsafe;
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.U2D;
// using ValheimVehicles.Prefabs;
// using ValheimVehicles.Propulsion.Rudder;
// using ValheimVehicles.ValheimVehicles.Prefabs;
// using ValheimVehicles.Vehicles;
// using ValheimVehicles.Vehicles.Components;
// using Logger = Jotunn.Logger;
//
// namespace ValheimRAFT;
//
// public class SnapPoint : MonoBehaviour
// {
//   private string name = "_snappoint";
//   private string tag = "snappoint";
// }
//
// public class PrefabController : MonoBehaviour
// {
//   public static PrefabManager prefabManager;
//   private PieceManager pieceManager;
//   private SpriteAtlas sprites;
//   private GameObject boarding_ramp;
//   private GameObject mbBoardingRamp;
//   private GameObject steering_wheel;
//   private GameObject rope_ladder;
//   private GameObject rope_anchor;
//   private GameObject ship_hull;
//   private GameObject raftMast;
//   private GameObject dirtFloor;
//   private Material sailMat;
//   private Piece woodFloorPiece;
//   private WearNTear woodFloorPieceWearNTear;
//   private SynchronizationManager synchronizationManager;
//   private List<Piece> raftPrefabPieces = new();
//   private bool prefabsEnabled = true;
//   private GameObject vanillaRaftPrefab;
//   private GameObject vikingShipPrefab;
//   private const string PrefabPrefix = "ValheimVehicles";
//   public const string WaterVehiclePrefabName = $"{PrefabPrefix}_WaterVehicle";
//   private static GameObject? _waterVehiclePrefab;
//   private static GameObject? _shipHullPrefab;
//   public const string ShipHullPrefabName = $"{PrefabPrefix}_ShipHull_Wood";
//   public static PrefabController Instance;
//   public const string SailBoxColliderName = $"{PrefabPrefix}_SailBoxCollider";
//
//   public static GameObject WaterVehiclePrefab =>
//     _waterVehiclePrefab;
//
//   public static GameObject ShipHullPrefab =>
//     _shipHullPrefab;
//
//   /*
//    * ship related items
//    * Todo make a vehicle specific prefab within the assetbundle for ValheimRAFT ships
//    *
//    * These values will be accessed by the WaterVehicleController (or alternatively a custom ship prefab will be created using these values
//    *
//    * All values will come from the VikingShip instead of the raft
//    */
//   public static Component waterMask;
//
//   public const string Tier1RaftMastName = "MBRaftMast";
//   public const string Tier2RaftMastName = "MBKarveMast";
//   public const string Tier3RaftMastName = "MBVikingShipMast";
//   public const string Tier1CustomSailName = "MBSail";
//   private const string ValheimRaftMenuName = "Raft";
//
//   private void Awake()
//   {
//     Instance = this;
//   }
//
//
//   // todo this should come from config
//   private float wearNTearBaseHealth = 250f;
//
//   private void UpdatePrefabs(bool isPrefabEnabled)
//   {
//     foreach (var piece in raftPrefabPieces)
//     {
//       var pmPiece = pieceManager.GetPiece(piece.name);
//       if (pmPiece == null)
//       {
//         Logger.LogWarning(
//           $"ValheimRaft attempted to run UpdatePrefab on {piece.name} but jotunn pieceManager did not find that piece name");
//         continue;
//       }
//
//       Logger.LogDebug($"Setting m_enabled: to {isPrefabEnabled}, for name {piece.name}");
//       pmPiece.Piece.m_enabled = isPrefabEnabled;
//     }
//
//     prefabsEnabled = isPrefabEnabled;
//   }
//
//   public void UpdatePrefabStatus()
//   {
//     if (!ValheimRaftPlugin.Instance.AdminsCanOnlyBuildRaft.Value && prefabsEnabled)
//     {
//       return;
//     }
//
//     Logger.LogDebug(
//       $"ValheimRAFT: UpdatePrefabStatusCalled with AdminsCanOnlyBuildRaft set as {ValheimRaftPlugin.Instance.AdminsCanOnlyBuildRaft.Value}, updating prefabs and player access");
//     var isAdmin = SynchronizationManager.Instance.PlayerIsAdmin;
//     UpdatePrefabs(isAdmin);
//   }
//
//   public void UpdatePrefabStatus(object obj, ConfigurationSynchronizationEventArgs e)
//   {
//     UpdateRaftSailDescriptions();
//     UpdatePrefabStatus();
//   }
//
//   private void AddToRaftPrefabPieces(Piece raftPiece)
//   {
//     raftPrefabPieces.Add(raftPiece);
//   }
//
//   private static ZNetView AddNetViewWithPersistence(GameObject prefab)
//   {
//     var netView = prefab.GetComponent<ZNetView>();
//     if (!(bool)netView)
//     {
//       netView = prefab.AddComponent<ZNetView>();
//     }
//
//     if (!netView)
//     {
//       Logger.LogError("Unable to register NetView, ValheimRAFT could be broken without netview");
//       return netView;
//     }
//
//     netView.m_persistent = true;
//
//     return netView;
//   }
//
//   public void RegisterAllPrefabs()
//   {
//     // VehiclePrefabs
//     RegisterWaterVehicle();
//
//     // Masts
//     RegisterRaftMast();
//     RegisterKarveMast();
//     RegisterVikingMast();
//     RegisterCustomSail();
//
//     // Sail creators
//     RegisterCustomSailCreator(3);
//     RegisterCustomSailCreator(4);
//
//     // Rope items
//     RegisterRopeAnchor();
//     RegisterRopeLadder();
//
//     // pier components
//     RegisterPierPole();
//     RegisterPierWall();
//
//     // Ramps
//     RegisterBoardingRamp();
//     RegisterBoardingRampWide();
//     // Floors
//     RegisterDirtFloor(1);
//     RegisterDirtFloor(2);
//   }
//
//   public void RegisterStructure(string name)
//   {
//   }
//
//   /*
//    * adds a snapoint to the center and all corners of object
//    *
//    * add snappoints to surface area
//    * https://github.com/Valheim-Modding/Wiki/wiki/Snappoints
//    */
//   private void AddSnapPointsToExterior(GameObject prefabObject)
//   {
//     // var t = prefabObject.GetComponentsInChildren<Transform>(true);
//     // Logger.LogDebug($"AddSnapPointsToExterior called transforms length: {t.Length}");
//     // for (var i = 0; i < t.Length; i++)
//     // {
//     //   Logger.LogDebug($"ItemTag: {t[i].tag}");
//     //   if (t[i].tag.Contains("snappoint"))
//     //   {
//     //     Logger.LogDebug(
//     //       $"LocalVector scale of snappoint {t[i].localScale} new scale mult {prefabObject.transform.localScale}");
//     //     t[i].localPosition = Vector3.Scale(t[i].localScale, prefabObject.transform.localScale);
//     //   }
//     // }
//
//
//     // var children = piece.gameObject.GetComponentsInChildren<GameObject>();
//     // Logger.LogDebug($"Piece {piece.GetSnapPoints()}");
//     // for (var x = 0; x < piece.transform.localScale.x; x++)
//     // {
//     //   for (var z = 0; z < piece.transform.localScale.z; z++)
//     //   {
//     //     var snap = piece.gameObject.AddComponent<SnapPoint>();
//     //     // probably not necessary to set parent and child as this is done automatically in theory
//     //     snap.transform.position = piece.transform.position;
//     //     snap.transform.localPosition = new Vector3(x, snap.transform.localPosition.y, z);
//     //     Logger.LogDebug(
//     //       $"piece pos: {piece.transform.position} snap pos: {snap.transform.position} snaplocal {snap.transform.localPosition}");
//     //     // {
//     //     //   // size = new Vector3(1, 1f, 1),
//     //     //   gameObject =
//     //     //   {
//     //     //     name = "_snappoint",
//     //     //     tag = "snappoint",
//     //     //     // layer = LayerMask.NameToLayer("piece_nonsolid")
//     //     //   },
//     //     //   transform =
//     //     //   {
//     //     //     localPosition = new Vector3(x, 1f, z)
//     //     //     // position = 
//     //     //   }
//     //     // };
//     //     // Instantiate(snap);
//     //   }
//     // }
//   }
//
//   private LODGroup vehicleLOD;
//
//   public void AddVehicleLODs(GameObject prefab)
//   {
//     vehicleLOD = prefab.AddComponent<LODGroup>();
//     // add LOD levels.
//     // Create a GUI that allows for forcing a specific LOD level.
//     // Add 4 LOD levels
//     // var hull = prefab.transform.Find("")
//     LOD[] lods = new LOD[1];
//     for (int i = 0; i < lods.Length; i++)
//     {
//       // PrimitiveType primType = PrimitiveType.Cube;
//       // switch (i)
//       // {
//       //   case 1:
//       //     primType = PrimitiveType.Capsule;
//       //     break;
//       //   case 2:
//       //     primType = PrimitiveType.Sphere;
//       //     break;
//       //   case 3:
//       //     primType = PrimitiveType.Cylinder;
//       //     break;
//       // }
//
//       // GameObject go = GameObject.CreatePrimitive(primType);
//       // go.transform.parent = prefab.transform;
//       Renderer[] renderers = new Renderer[1];
//       // renderers[0] = go.GetComponent<Renderer>();
//
//       for (int r = 0; r < renderers.Length; r++)
//       {
//         switch (r)
//         {
//           case 0:
//             renderers[r] = waterMask.GetComponentInChildren<MeshRenderer>();
//             break;
//           // case 1:
//           //   renderers[r] = waterMask.GetComponentInChildren<MeshRenderer>();
//           //   break;
//           // case 2:
//           //   renderers[r] = waterMask.GetComponentInChildren<MeshRenderer>();
//           //   break;
//           // case 3:
//           //   renderers[r] = waterMask.GetComponentInChildren<MeshRenderer>();
//           //   break;
//         }
//       }
//
//
//       lods[i] = new LOD(1.0F / (i + 2), renderers);
//       lods[i].screenRelativeTransitionHeight = 25f;
//     }
//
//     vehicleLOD.SetLODs(lods);
//     vehicleLOD.RecalculateBounds();
//   }
//
//   public void Init()
//   {
//     boarding_ramp =
//       ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/boarding_ramp.prefab");
//     ship_hull =
//       ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/ship_hull.prefab");
//     steering_wheel =
//       ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/steering_wheel.prefab");
//     rope_ladder =
//       ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/rope_ladder.prefab");
//     rope_anchor =
//       ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("Assets/rope_anchor.prefab");
//     sprites =
//       ValheimRaftPlugin.m_assetBundle.LoadAsset<SpriteAtlas>("Assets/icons.spriteatlas");
//     sailMat = ValheimRaftPlugin.m_assetBundle.LoadAsset<Material>("Assets/SailMat.mat");
//     dirtFloor = ValheimRaftPlugin.m_assetBundle.LoadAsset<GameObject>("dirt_floor.prefab");
//
//     prefabManager = PrefabManager.Instance;
//     pieceManager = PieceManager.Instance;
//
//     var woodFloorPrefab = prefabManager.GetPrefab("wood_floor");
//     woodFloorPiece = woodFloorPrefab.GetComponent<Piece>();
//     woodFloorPieceWearNTear = woodFloorPiece.GetComponent<WearNTear>();
//
//     vanillaRaftPrefab = prefabManager.GetPrefab("Raft");
//     vikingShipPrefab = prefabManager.GetPrefab("VikingShip");
//
//     // shipFloatCollider = vikingShipPrefab.transform.Find("FloatCollider")
//     // .GetComponentInChildren<BoxCollider>();
//     waterMask = vikingShipPrefab.transform.Find("ship/visual/watermask");
//
//     // registers all prefabs
//     RegisterAllPrefabs();
//
//     /*
//      * listens for admin status updates and changes prefab active status
//      */
//     SynchronizationManager.OnConfigurationSynchronized += UpdatePrefabStatus;
//     SynchronizationManager.OnAdminStatusChanged += UpdatePrefabStatus;
//     UpdatePrefabStatus();
//   }
//
//   private WearNTear GetWearNTearSafe(GameObject prefabComponent)
//   {
//     var wearNTearComponent = prefabComponent.GetComponent<WearNTear>();
//     if (!(bool)wearNTearComponent)
//     {
//       // Many components do not have WearNTear so they must be added to the prefabPiece
//       wearNTearComponent = prefabComponent.AddComponent<WearNTear>();
//       if (!wearNTearComponent)
//         Logger.LogError(
//           $"error setting WearNTear for RAFT prefab {prefabComponent.name}, the ValheimRAFT mod may be unstable without WearNTear working properly");
//     }
//
//     return wearNTearComponent;
//   }
//
//   private WearNTear SetWearNTear(GameObject prefabComponent, int tierMultiplier = 1,
//     bool canFloat = false)
//   {
//     var wearNTearComponent = GetWearNTearSafe(prefabComponent);
//
//     wearNTearComponent.m_noSupportWear = canFloat;
//
//     wearNTearComponent.m_health = wearNTearBaseHealth * tierMultiplier;
//     wearNTearComponent.m_noRoofWear = false;
//     wearNTearComponent.m_destroyedEffect = woodFloorPieceWearNTear.m_destroyedEffect;
//     wearNTearComponent.m_hitEffect = woodFloorPieceWearNTear.m_hitEffect;
//
//     return wearNTearComponent;
//   }
//
//   private WearNTear SetWearNTearSupport(WearNTear wntComponent, WearNTear.MaterialType materialType)
//   {
//     // this will use the base material support provided by valheim for support. This should be balanced for wood. Stone may need some tweaks for buoyancy and other balancing concerns
//     wntComponent.m_materialType = materialType;
//
//     return wntComponent;
//   }
//
//   /**
//    * todo this needs to be fixed so the mast blocks only with the mast part and ignores the non-sail area.
//    * if the collider is too big it also pushes the rigidbody system underwater (IE Raft sinks)
//    *
//    * May be easier to just get the game object structure for each sail and do a search for the sail and master parts.
//    */
//   public void AddBoundsToAllChildren(GameObject parent, GameObject componentToEncapsulate)
//   {
//     var boxCol = parent.GetComponent<BoxCollider>();
//     if (boxCol == null)
//     {
//       boxCol = parent.AddComponent<BoxCollider>();
//     }
//
//     boxCol.name = SailBoxColliderName;
//
//     Bounds bounds = new Bounds(parent.transform.position, Vector3.zero);
//
//     var allDescendants = componentToEncapsulate.GetComponentsInChildren<Transform>();
//     foreach (Transform desc in allDescendants)
//     {
//       Renderer childRenderer = desc.GetComponent<Renderer>();
//       if (childRenderer != null)
//       {
//         bounds.Encapsulate(childRenderer.bounds);
//         Logger.LogDebug(childRenderer.bounds);
//       }
//
//       boxCol.center = new Vector3(0, bounds.max.y,
//         0);
//       boxCol.size = boxCol.center * 2;
//     }
//   }
// }

