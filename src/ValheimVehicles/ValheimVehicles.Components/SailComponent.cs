using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Injections;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Storage.Serialization;
using ValheimVehicles.UI;
using ZdoWatcher;
using Zolantris.Shared;
using Zolantris.Shared.Debug;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Components;

public class SailComponent : MonoBehaviour, Interactable, Hoverable, INetView
{
  public const string RuntimeImplementationVersion = "MC2_CUTOUT_DUAL_FACE_V9";
  private static bool _runtimeVersionLogged;

  [Flags]
  public enum SailFlags
  {
    None = 0,
    AllowSailShrinking = 1,
    DisableCloth = 2,
    AllowSailRotation = 4
  }

  [Flags]
  public enum SailLockedSide
  {
    None = 0,
    A = 1,
    B = 2,
    C = 4,
    D = 8,
    Everything = 0xF
  }


  public static int m_mainHashRefHash = "m_mainHash".GetStableHashCode();

  public static int m_sailCornersCountHash =
    "m_sailCornersCountHash".GetStableHashCode();

  public static int m_sailCorner1Hash = "m_sailCorner1Hash".GetStableHashCode();
  public static int m_sailCorner2Hash = "m_sailCorner2Hash".GetStableHashCode();
  public static int m_sailCorner3Hash = "m_sailCorner3Hash".GetStableHashCode();
  public static int m_sailCorner4Hash = "m_sailCorner4Hash".GetStableHashCode();
  public static int m_lockedSailSidesHash = "m_lockedSailSides".GetStableHashCode();

  public static int m_lockedSailCornersHash =
    "m_lockedSailCorners".GetStableHashCode();

  public static int m_mainScaleHash = "m_mainScale".GetStableHashCode();
  public static int m_mainOffsetHash = "m_mainOffset".GetStableHashCode();
  public static int m_mainColorHash = "m_mainColor".GetStableHashCode();
  public static int m_patternScaleHash = "m_patternScale".GetStableHashCode();
  public static int m_patternOffsetHash = "m_patternOffset".GetStableHashCode();
  public static int m_patternColorHash = "m_patternColor".GetStableHashCode();
  public static int m_patternZDOHash = "m_patternHash".GetStableHashCode();
  public static int m_patternRotationHash = "m_patternRotation".GetStableHashCode();
  public static int m_logoZdoHash = "m_logoHash".GetStableHashCode();
  public static int m_logoColorHash = "m_logoColor".GetStableHashCode();
  public static int m_logoScaleHash = "m_logoScale".GetStableHashCode();
  public static int m_logoRotationHash = "m_logoRotation".GetStableHashCode();
  public static int m_logoOffsetHash = "m_logoOffset".GetStableHashCode();
  public static int m_sailFlagsHash = "m_sailFlagsHash".GetStableHashCode();
  public static int HasInitializedHash = "HasInitialized".GetStableHashCode();
  public static int SailParentIdHash = "SailParentId".GetStableHashCode();
  public static int SailParentPositionHash = "SailParentPosition".GetStableHashCode();
  public static int SailParentRotationHash = "SailParentRotation".GetStableHashCode();

  // for switching between custom/and other built-in sail textures.
  public static int m_sailMaterialVariantHash = "SailMaterialVariant".GetStableHashCode();

  private MastComponent m_mastComponent;

  public SkinnedMeshRenderer m_mesh;

  public MeshCollider m_meshCollider;

  public static bool Config_AllowMeshCollision = false;

  public MagicaCloth m_sailCloth;

  public List<Vector3> m_sailCorners = new();

  public float m_sailSubdivision = 0.5f;

  /*
   * Render the sail as a real two-sided shell instead of depending on a
   * two-sided shader. Front and back receive separate vertices/normals and
   * identical Magica vertex attributes. A few millimeters is enough to avoid
   * opposite-side lighting/fade artifacts without materially changing sail
   * dimensions. Set to 0 to keep coincident front/back surfaces while still
   * retaining proper independent normals on each side.
   */
  public float m_sailThickness = 0.004f;

  /*
   * Render the back surface with the same shading basis as the front surface.
   * The front/back surfaces are separate submeshes with opposite culling, so
   * both sides can use the same normal/tangent orientation without relying on
   * a two-sided shader's VFACE/backface normal behavior.
   */
  public bool m_matchBackFaceLighting = true;

  public static List<SailComponent> m_sailComponents = new();

  public static float m_maxDistanceSqr = 1024f;

  private static EditSailComponentPanel? m_editPanel = null;

  public SailFlags m_sailFlags;

  public float m_windMultiplier = 10f;

  public float m_clothRandomAccelerationFactor = 0.5f;

  public SailLockedSide m_lockedSailSides = SailLockedSide.Everything;

  public SailLockedSide m_lockedSailCorners = SailLockedSide.Everything;

  public int m_patternHash;

  public Vector2 m_patternScale;

  public Vector2 m_patternOffset;

  public Color m_patternColor = new(1, 1, 1, 0);

  public float m_patternRotation;

  public int m_logoHash;

  public Vector2 m_logoScale;

  public Vector2 m_logoOffset;

  public Color m_logoColor = new(1, 1, 1, 0);

  public float m_logoRotation;

  public int m_mainHash;

  public Vector2 m_mainScale;

  public Vector2 m_mainOffset;

  public Color m_mainColor = Color.white;

  public float m_mistAlpha = 1f;
  public CoroutineHandle sailParentRoutine;
  private CoroutineHandle _waitForInitRoutine;
  private CoroutineHandle _loadZDORoutine;
  private CoroutineHandle _clothRebuildRoutine;

  /*
   * MagicaCloth2 construction data is immutable once BuildAndRun() starts.
   * Keep explicit ownership of the generated source mesh and rebuild state so
   * ZDO/material reloads cannot accidentally rebuild an already-built cloth.
   */
  private Mesh? _generatedSailMesh;
  private Mesh? _generatedCollisionMesh;
  private Mesh? _pendingSailMesh;
  private Mesh? _rebuildSailMesh;

  /*
   * Render-only shell metadata. Magica vertex attributes must be evaluated
   * against the original center surface, not against the +/- thickness offset
   * or the independent side-wall vertices.
   */
  private Vector3[] _sailAttributeReferenceVertices = Array.Empty<Vector3>();

  /*
   * The custom sail is rendered with independent front/back materials. This
   * lets both surfaces use the same lighting normals while culling opposite
   * windings. _variantSailMaterial and _backSailMaterial are owned by this
   * component and are destroyed with it.
   */
  private Material? _variantSailMaterial;
  private Material? _backSailMaterial;
  private bool _warnedMissingCullProperty;

  private SailClothBuildState _activeClothBuildState;
  private SailClothBuildState _pendingClothBuildState;
  private SailClothBuildState _rebuildClothBuildState;

  private bool _hasActiveClothBuildState;
  private bool _hasPendingClothBuildState;
  private bool _hasRebuildClothBuildState;
  private bool _clothBuildInProgress;
  private bool _buildAfterParent;

  /*
   * Custom sail furling uses a row-bone skinning rig. The old four-corner rig
   * could only shrink the cloth vertically, which made a furled sail look as
   * if the whole sheet was simply being lifted. Row bones let the lower cloth
   * wrap into a compact roll while the exposed section remains at its original
   * size and position.
   *
   * Magica continues to simulate one stable SkinnedMeshRenderer; furling only
   * changes the source animation pose and never rescales/rebuilds the cloth.
   */
  private Transform? _sailRigRoot;
  private readonly List<Transform?> _sailRigBones = new();
  private SailClothBuildState _sailRigBuildState;
  private bool _hasSailRigBuildState;
  private int _sailRigBoneCount;
  private float _requestedSailPosition = 1f;

  public float m_sailFurlRollRadius = 0.065f;
  public float m_sailFurlTurns = 2.25f;

  private const int MinimumFurlBoneCount = 10;
  private const int MaximumFurlBoneCount = 32;
  private const string SailRigRootName = "__ValheimRAFT_SailRig";

  private static bool _defaultSailsRegistered;

  private float m_sailArea = 0f;
  private static bool DebugBoxCollider = true;
  private static readonly int MistAlpha = Shader.PropertyToID("_MistAlpha");
  private static readonly int MainColor = Shader.PropertyToID("_MainColor");

  private static readonly int VegetationColor = Shader.PropertyToID("_Color");

  private static readonly int PatternColor =
    Shader.PropertyToID("_PatternColor");

  private static readonly int PatternTex = Shader.PropertyToID("_PatternTex");

  private static readonly int PatternRotation =
    Shader.PropertyToID("_PatternRotation");

  private static readonly int PatternNormal =
    Shader.PropertyToID("_PatternNormal");

  private static readonly int MainTex = Shader.PropertyToID("_MainTex");
  private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
  private static readonly int MainNormal = Shader.PropertyToID("_MainNormal");
  private static readonly int LogoTex = Shader.PropertyToID("_LogoTex");
  private static readonly int LogoColor = Shader.PropertyToID("_LogoColor");

  private static readonly int LogoRotation =
    Shader.PropertyToID("_LogoRotation");

  private static readonly int LogoNormal = Shader.PropertyToID("_LogoNormal");
  public bool hasRegisteredRPC = false;

  private const float SailPinTolerance = 0.001f;

  private readonly struct SailClothBuildState : IEquatable<SailClothBuildState>
  {
    public readonly int CornerCount;
    public readonly Vector3 CornerA;
    public readonly Vector3 CornerB;
    public readonly Vector3 CornerC;
    public readonly Vector3 CornerD;
    public readonly float Subdivision;
    public readonly float Thickness;
    public readonly SailLockedSide LockedCorners;
    public readonly SailLockedSide LockedSides;

    public SailClothBuildState(
      IReadOnlyList<Vector3> corners,
      float subdivision,
      float thickness,
      SailLockedSide lockedCorners,
      SailLockedSide lockedSides)
    {
      CornerCount = corners.Count;
      CornerA = corners.Count > 0 ? corners[0] : Vector3.zero;
      CornerB = corners.Count > 1 ? corners[1] : Vector3.zero;
      CornerC = corners.Count > 2 ? corners[2] : Vector3.zero;
      CornerD = corners.Count > 3 ? corners[3] : Vector3.zero;
      Subdivision = subdivision;
      Thickness = thickness;
      LockedCorners = lockedCorners;
      LockedSides = lockedSides;
    }

    public bool Equals(SailClothBuildState other)
    {
      return CornerCount == other.CornerCount &&
             CornerA == other.CornerA &&
             CornerB == other.CornerB &&
             CornerC == other.CornerC &&
             CornerD == other.CornerD &&
             Mathf.Approximately(Subdivision, other.Subdivision) &&
             Mathf.Approximately(Thickness, other.Thickness) &&
             LockedCorners == other.LockedCorners &&
             LockedSides == other.LockedSides;
    }

    public override bool Equals(object? obj)
    {
      return obj is SailClothBuildState other && Equals(other);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        var hash = CornerCount;
        hash = hash * 397 ^ CornerA.GetHashCode();
        hash = hash * 397 ^ CornerB.GetHashCode();
        hash = hash * 397 ^ CornerC.GetHashCode();
        hash = hash * 397 ^ CornerD.GetHashCode();
        hash = hash * 397 ^ Subdivision.GetHashCode();
        hash = hash * 397 ^ Thickness.GetHashCode();
        hash = hash * 397 ^ (int)LockedCorners;
        hash = hash * 397 ^ (int)LockedSides;
        return hash;
      }
    }
  }

  public enum MaterialVariant
  {
    Custom,
    Karve,
    Drakkal,
    Raft
  }

  // used to restore material if using an override in-game variant.
  private Material customMaterial;
  public MaterialVariant m_materialVariant = MaterialVariant.Custom;

  public void Awake()
  {
    if (!_runtimeVersionLogged)
    {
      _runtimeVersionLogged = true;
      Logger.LogInfo($"SailComponent runtime: {RuntimeImplementationVersion}");
    }

    sailParentRoutine = new CoroutineHandle(this);
    _waitForInitRoutine = new CoroutineHandle(this);
    _loadZDORoutine = new CoroutineHandle(this);
    _clothRebuildRoutine = new CoroutineHandle(this);

    m_sailComponents.Add(this);

    m_mastComponent =
      GetComponent<MastComponent>();

    m_mesh =
      GetComponent<SkinnedMeshRenderer>();

    m_meshCollider =
      GetComponent<MeshCollider>();

    m_nview =
      GetComponent<ZNetView>();

    /*
     * Disable Magica's Start()-time auto build during Awake. All custom sails
     * are built manually after their generated mesh and fixed vertex attributes
     * have been assigned.
     */
    EnsureSailCloth();

    if (m_mastComponent)
    {
      m_mastComponent.m_allowSailRotation = false;
      m_mastComponent.m_sailCloth = m_sailCloth;
      m_mastComponent.m_customSailComponent = this;
    }

    if (m_mesh)
    {
      // This is only an initial fallback. LoadFromMaterial() refreshes it from
      // the renderer because dynamically-created sails can receive their source
      // material after SailComponent.Awake() has already run.
      customMaterial =
        m_mesh.material;
    }

    AddDefaultSailsToTextures();
  }

  private bool EnsureSailCloth()
  {
    if (!m_mesh)
    {
      m_mesh = GetComponent<SkinnedMeshRenderer>();
    }

    if (!m_mesh)
    {
      LoggerProvider.LogError(
        $"SailComponent '{name}' has no SkinnedMeshRenderer.");

      return false;
    }

    if (!m_sailCloth)
    {
      m_sailCloth = GetComponent<MagicaCloth>();
    }

    if (!m_sailCloth)
    {
      m_sailCloth = CreateReplacementSailCloth(null);
    }

    if (!m_sailCloth)
    {
      LoggerProvider.LogError(
        $"Unable to create MagicaCloth for sail '{name}'.");

      return false;
    }

    /*
     * Runtime-generated sails must never auto-build against the prefab/default
     * mesh in Start(). Construction happens only after the final hierarchy,
     * source mesh, skinning rig and vertex attributes are ready.
     */
    m_sailCloth.DisableAutoBuild();

    return true;
  }

  private MagicaCloth? CreateReplacementSailCloth(MagicaCloth? sourceCloth)
  {
    var replacement = gameObject.AddComponent<MagicaCloth>();

    if (!replacement)
    {
      return null;
    }

    replacement.DisableAutoBuild();

    try
    {
      if (sourceCloth)
      {
        /*
         * Copy parameter data only. Renderer lists, paint/construction data and
         * transform references belong to the old build and are deliberately not
         * copied. This preserves Valheim/Magica tuning across geometry rebuilds.
         */
        replacement.SerializeData.Import(
          sourceCloth.SerializeData,
          false);
      }
      else
      {
        TryImportVanillaSailClothParameters(replacement);
      }
    }
    catch (Exception e)
    {
      LoggerProvider.LogWarning(
        $"Unable to import MagicaCloth sail parameters for '{name}'. Using Magica defaults.\n{e}");
    }

    return replacement;
  }

  private static void TryImportVanillaSailClothParameters(
    MagicaCloth targetCloth)
  {
    if (!targetCloth)
    {
      return;
    }

    var shipPrefab = LoadValheimAssets.vikingShipPrefab;

    if (!shipPrefab)
    {
      return;
    }

    var sourceShip = shipPrefab.GetComponent<Ship>();

    if (!sourceShip || !sourceShip.m_sailCloth)
    {
      return;
    }

    /*
     * false = parameter-only import. Do not inherit the vanilla prefab's
     * renderer, collider or transform references.
     */
    targetCloth.SerializeData.Import(
      sourceShip.m_sailCloth.SerializeData,
      false);
  }

  public static void AddDefaultSailsToTextures()
  {
    if (_defaultSailsRegistered)
    {
      return;
    }

    var sailsGroup = CustomTextureGroup.Get("Sails");
    if (sailsGroup == null)
    {
      /*
       * Asset texture groups may not exist yet during early prefab/component
       * creation. Do not latch the failure; a later sail Awake can retry.
       */
      LoggerProvider.LogWarning(
        "AddDefaultSailsToTextures(): Sails texture group is not initialized yet.");

      return;
    }

    var drakkalMaterial = OverrideMaterial_DrakkalShipSail();
    var vikingMaterial = OverrideMaterial_VikingShipSail();
    var raftShipSailMaterial = OverrideMaterial_RaftShipSail();

    sailsGroup.AddTexture(new CustomTexture
    {
      Texture = drakkalMaterial.GetTexture(MainTex),
      Normal = drakkalMaterial.GetTexture(BumpMap)
    });
    sailsGroup.AddTexture(new CustomTexture
    {
      Texture = vikingMaterial.GetTexture(MainTex),
      Normal = vikingMaterial.GetTexture(BumpMap)
    });
    sailsGroup.AddTexture(new CustomTexture
    {
      Texture = raftShipSailMaterial.GetTexture(MainTex),
      Normal = raftShipSailMaterial.GetTexture(BumpMap)
    });

    _defaultSailsRegistered = true;
  }

  public void RegisterRPC()
  {
    if (hasRegisteredRPC)
    {
      return;
    }
    m_nview.Register(nameof(RPC_SyncSailData), RPC_SyncSailData);
    hasRegisteredRPC = true;
  }

  public void UnregisterRPC()
  {
    m_nview.Unregister(nameof(RPC_SyncSailData));
    hasRegisteredRPC = false;
  }

  private IEnumerator WaitForInitialization()
  {
    var timer = DebugSafeTimer.StartNew();
    const int maxWaitTime = 20000;
    while (timer.ElapsedMilliseconds < maxWaitTime && !GetIsInitialized())
    {
      yield return new WaitForFixedUpdate();
    }


    if (timer.ElapsedMilliseconds >= maxWaitTime)
    {
      LoggerProvider.LogDev("Exiting WaitForInitialization due to timeout");
      yield break;
    }

    // One extra frame — lets any remaining ZDO corner vectors arrive before we read them
    yield return new WaitForFixedUpdate();

    RegisterRPC();
    LoadZDO();
  }

  public static Material OverrideMaterial_VikingShipSail()
  {
    return GetVanillaSailMaterial(LoadValheimAssets.vikingShipPrefab);
  }

  public static Material OverrideMaterial_DrakkalShipSail()
  {
    return GetVanillaSailMaterial(LoadValheimAssets.drakkarPrefab);
  }

  public static Material OverrideMaterial_RaftShipSail()
  {
    return GetVanillaSailMaterial(LoadValheimAssets.vanillaRaftPrefab);
  }

  private static Material GetVanillaSailMaterial(GameObject shipPrefab)
  {
    // The old Sail object is absent or inactive in 1.0. The cloth's renderer
    // list identifies the active sail and avoids Drakkar's duplicate names.
    var cloth = shipPrefab.GetComponent<Ship>().m_sailCloth;
    var renderer = cloth.SerializeData.sourceRenderers.FirstOrDefault(candidate => candidate);
    if (!renderer || !renderer.sharedMaterial)
      throw new InvalidOperationException($"{shipPrefab.name}: vanilla sail material is missing.");
    return renderer.sharedMaterial;
  }

  public void FixedUpdate()
  {
    if (_hasPendingClothBuildState &&
        _pendingSailMesh &&
        !_clothBuildInProgress &&
        !_clothRebuildRoutine.IsRunning)
    {
      _clothRebuildRoutine.Start(ProcessSailClothRebuildQueue());
    }

    UpdateSailClothWind();

    if (PrefabConfig.Graphics_AllowSailsFadeInFog.Value)
    {
      UpdateMistAlphaForPlayerCamera();
    }


  }

  public void UpdateSailClothWind()
  {
    if (!EnvMan.instance || !m_sailCloth || !m_sailCloth.IsValid())
      return;

    var acceleration =
      EnvMan.instance.GetWindForce() * m_windMultiplier;

    var magnitude = acceleration.magnitude;
    if (magnitude <= Mathf.Epsilon)
      return;

    var deltaVelocity = magnitude * Time.fixedDeltaTime;

    m_sailCloth.AddForce(
      acceleration / magnitude,
      deltaVelocity,
      ClothForceMode.VelocityAddWithoutDepth
    );
  }

  private void OnEnable()
  {
    m_nview = GetComponent<ZNetView>();
    Initialize();
  }

  public void Initialize()
  {
    if (GetIsInitialized())
    {
      RegisterRPC();
      LoadZDO();
    }
    else
    {
      // Stop any previous wait — zone reloads call OnEnable multiple times
      _waitForInitRoutine.Stop();
      _waitForInitRoutine.Start(WaitForInitialization());
    }
  }


  private void OnDrawGizmos()
  {
    if (DebugBoxCollider)
    {
      Gizmos.color = Color.green;
      Gizmos.matrix = transform.localToWorldMatrix;
      Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }

    for (var i = 0; i < m_sailCorners.Count; i++)
      Gizmos.DrawSphere(transform.position + m_sailCorners[i], 0.1f);
  }

  public void OnDestroy()
  {
    ReleasePendingSailMeshes();
    ReleaseGeneratedMeshes();
    ReleaseOwnedSailMaterials();

    if (m_mastComponent)
    {
      if (m_mastComponent.m_sailCloth == m_sailCloth)
      {
        m_mastComponent.m_sailCloth = null;
      }

      if (m_mastComponent.m_customSailComponent == this)
      {
        m_mastComponent.m_customSailComponent = null;
      }
    }

    m_sailComponents.Remove(this);
  }

  public void OnDisable()
  {
    CancelInvoke();
    _waitForInitRoutine.Stop();
    _loadZDORoutine.Stop();
    _clothRebuildRoutine.Stop();
    sailParentRoutine.Stop();
    StopAllCoroutines();

    ReleasePendingSailMeshes();

    /*
     * Do not destroy _generatedSailMesh here. A temporarily disabled sail can
     * be enabled again and its MagicaCloth still owns that source topology.
     */
    UnregisterRPC();
  }

  private void DestroySelfOnError()
  {
    try
    {
      if (ZNetScene.instance != null)
      {
        ZNetScene.instance.Destroy(gameObject);
      }
      CancelInvoke();
    }
    catch (Exception e)
    {
      LoggerProvider.LogDebug($"Problem occurred while attempting to destroy invalid sail \n{e}");
    }
  }

  public void ApplyLoadedSailData(StoredSailData data)
  {
    if (data == null)
    {
      LoggerProvider.LogError("Sail data sync is corrupt.");
      return;
    }

    m_sailCorners = data.SailCorners.Select(corner => corner.ToVector3()).ToList();
    m_lockedSailSides = (SailLockedSide)data.LockedSides;
    m_lockedSailCorners = (SailLockedSide)data.LockedCorners;

    // For 3-point sails: strip the D bit from the persistent fields (it is meaningless
    // for triangles) and apply a sane default when the values are unset (fresh spawn).
    if (m_sailCorners.Count == 3)
    {
      m_lockedSailSides &= ~SailLockedSide.D;
      m_lockedSailCorners &= ~SailLockedSide.D;
      if (m_lockedSailSides == SailLockedSide.None && m_lockedSailCorners == SailLockedSide.None)
      {
        m_lockedSailSides = SailLockedSide.A | SailLockedSide.B | SailLockedSide.C;
        m_lockedSailCorners = SailLockedSide.A | SailLockedSide.B | SailLockedSide.C;
      }
    }

    SetMaterialVariant((MaterialVariant)data.MaterialVariant);

    SetMain(data.MainHash);
    SetMainColor(data.MainColor.ToColor());
    SetMainOffset(data.MainOffset.ToVector2());
    SetMainScale(data.MainScale.ToVector2());

    if (m_materialVariant == MaterialVariant.Custom)
    {
      SetPattern(data.PatternHash);
      SetPatternColor(data.PatternColor.ToColor());
      SetPatternOffset(data.PatternOffset.ToVector2());
      SetPatternScale(data.PatternScale.ToVector2());
      SetPatternRotation(data.PatternRotation);

      SetLogo(data.LogoHash);
      SetLogoColor(data.LogoColor.ToColor());
      SetLogoOffset(data.LogoOffset.ToVector2());
      SetLogoScale(data.LogoScale.ToVector2());
      SetLogoRotation(data.LogoRotation);
    }


    SetSailMastSetting(SailFlags.AllowSailShrinking,
      ((SailFlags)data.SailFlags).HasFlag(SailFlags.AllowSailShrinking));

    SetSailMastSetting(SailFlags.DisableCloth,
      ((SailFlags)data.SailFlags).HasFlag(SailFlags.DisableCloth));

    UpdateSailArea();

    /*
     * A loaded sail must not build Magica until it has reached its final parent
     * and saved local transform. Building first and reparenting afterward is a
     * timing-dependent source of displaced/crumpled cloth state.
     */
    if (GetSailParentId() != 0)
    {
      UpdateSailParent(true);
    }
    else
    {
      CreateSailMesh();
    }
  }

  public IEnumerator WaitForSailParent(int sailParentId)
  {
    GameObject? sailParent = null;
    var timer = Stopwatch.StartNew();

    while (isActiveAndEnabled &&
           sailParent == null &&
           timer.ElapsedMilliseconds < 5000)
    {
      yield return null;
      sailParent = ZdoWatchController.Instance.GetGameObject(sailParentId);
    }

    if (sailParent == null)
    {
      LoggerProvider.LogWarning(
        $"WaitForSailParent(): Unable to resolve sail parent {sailParentId} for '{name}'. Cloth build remains deferred.");

      yield break;
    }

    var parentMastComponent =
      sailParent.GetComponent<MastComponent>();

    if (!parentMastComponent ||
        !parentMastComponent.m_rotationTransform)
    {
      LoggerProvider.LogWarning(
        $"WaitForSailParent(): Parent '{sailParent.name}' has no usable mast rotation transform.");

      yield break;
    }

    transform.SetParent(
      parentMastComponent.m_rotationTransform,
      false);

    if (this.IsNetViewValid(out var netView))
    {
      var zdo = netView.GetZDO();

      transform.localPosition =
        zdo.GetVec3(
          SailParentPositionHash,
          Vector3.zero);

      transform.localRotation =
        Quaternion.Euler(
          zdo.GetVec3(
            SailParentRotationHash,
            transform.localRotation.eulerAngles));
    }
    else
    {
      transform.localPosition = Vector3.zero;
    }

    /*
     * Give Unity one frame to settle the final hierarchy and renderer transform
     * before Magica captures its initialization pose.
     */
    yield return null;

    if (_buildAfterParent && isActiveAndEnabled)
    {
      _buildAfterParent = false;
      CreateSailMesh();
    }
  }

  public void UpdateSailParent()
  {
    UpdateSailParent(false);
  }

  private void UpdateSailParent(bool buildAfterParent)
  {
    var sailParentId = GetSailParentId();

    if (sailParentId == 0)
    {
      if (buildAfterParent)
      {
        CreateSailMesh();
      }

      return;
    }

    if (buildAfterParent)
    {
      _buildAfterParent = true;
    }

    if (sailParentRoutine.IsRunning)
    {
      return;
    }

    sailParentRoutine.Start(
      WaitForSailParent(sailParentId));
  }

  private int GetSailParentId()
  {
    if (!this.IsNetViewValid(out var netView))
    {
      return 0;
    }

    return netView.GetZDO().GetInt(SailParentIdHash);
  }


  private bool GetIsInitialized()
  {
    if (!this.IsNetViewValid(out var netView)) return false;
    var zdo = netView.GetZDO();
    if (zdo == null) return false;

    if (!zdo.GetBool(HasInitializedHash)) return false;

    var zdoCorners = zdo.GetInt(m_sailCornersCountHash);

    // Legacy migration: count was never written but individual corner keys may exist (old saves).
    // Detect all 4 corners present and upgrade the ZDO in place — only when count is 0.
    if (zdoCorners == 0)
    {
      var impossible = new Vector3(100001f, 100001f, 100001f);
      var c1 = zdo.GetVec3(m_sailCorner1Hash, impossible);
      var c2 = zdo.GetVec3(m_sailCorner2Hash, impossible);
      var c3 = zdo.GetVec3(m_sailCorner3Hash, impossible);
      var c4 = zdo.GetVec3(m_sailCorner4Hash, impossible);

      if (c1 != impossible && c2 != impossible && c3 != impossible && c4 != impossible)
      {
        // Write the count so we never fall into this branch again
        zdo.Set(m_sailCornersCountHash, 4);
        zdoCorners = 4;
      }
    }

    // ZDO must declare a valid corner count — that's all we need to know we're ready.
    // m_sailCorners is NOT checked here: it is always empty on a fresh zone reload
    // and is only populated by ApplyLoadedSailData which runs after this gate.
    return zdoCorners is 3 or 4;
  }


  public void SetSailMastSetting(SailFlags flag, bool allow)
  {
    if ((bool)m_mastComponent)
    {
      m_sailFlags = allow
        ? m_sailFlags | flag
        : m_sailFlags & ~flag;

      switch (flag)
      {
        case SailFlags.DisableCloth:
          m_mastComponent.m_disableCloth = allow;
          if ((bool)m_sailCloth && m_sailCloth.enabled != !allow)
            m_sailCloth.enabled = !allow;
          break;
        case SailFlags.AllowSailShrinking:
          m_mastComponent.m_allowSailShrinking = allow;
          break;
        case SailFlags.AllowSailRotation:
          Logger.LogInfo(
            "SailFlags for AllowSailRotation not supported, setting to false. This line should not be reached and will be removed in later versions of raft");
          m_mastComponent.m_allowSailRotation = false;
          break;
        case SailFlags.None:
        default:
          Logger.LogWarning(
            $"SetSailMastSetting called with flag {flag}, but flag does not exist in SailFlags");
          break;
      }
    }
  }

  public void LoadFromMaterial()
  {
    if (!m_mesh)
    {
      LoggerProvider.LogError(
        $"LoadFromMaterial(): Sail '{name}' has no SkinnedMeshRenderer.");

      return;
    }

    Material sailMaterial;

    if (m_materialVariant == MaterialVariant.Custom)
    {
      /*
       * Awake can run before SailCreator assigns the source material.
       * The renderer is therefore authoritative when importing a custom sail.
       */
      sailMaterial = GetPrimarySailRenderMaterial();
      customMaterial = sailMaterial;
    }
    else
    {
      sailMaterial = GetSailMaterial();
    }

    if (!sailMaterial)
    {
      LoggerProvider.LogError(
        $"LoadFromMaterial(): Sail '{name}' has no usable material.");

      return;
    }

    m_mainColor = sailMaterial.GetColor(
      m_materialVariant == MaterialVariant.Custom
        ? MainColor
        : VegetationColor);

    m_mistAlpha = 1f;

    var mainTex = sailMaterial.GetTexture(MainTex);

    if (mainTex)
    {
      m_mainHash = mainTex.name.GetStableHashCode();
    }
    else
    {
      mainTex = LoadValheimRaftAssets.sailTexture;
      m_mainHash = mainTex.name.GetStableHashCode();
    }

    m_mainScale = sailMaterial.GetTextureScale(MainTex);
    m_mainOffset = sailMaterial.GetTextureOffset(MainTex);

    // Do not read custom shader properties from vanilla sail materials.
    if (m_materialVariant != MaterialVariant.Custom)
    {
      return;
    }

    var patternTex = sailMaterial.GetTexture(PatternTex);
    m_patternHash = ResolveMaterialTextureHash(
      patternTex,
      "Patterns",
      "pattern");

    m_patternScale = sailMaterial.GetTextureScale(PatternTex);
    m_patternOffset = sailMaterial.GetTextureOffset(PatternTex);
    m_patternColor = sailMaterial.GetColor(PatternColor);
    m_patternRotation = sailMaterial.GetFloat(PatternRotation);

    var logoTex = sailMaterial.GetTexture(LogoTex);
    m_logoHash = ResolveMaterialTextureHash(
      logoTex,
      "Logos",
      "logo");

    m_logoScale = sailMaterial.GetTextureScale(LogoTex);
    m_logoOffset = sailMaterial.GetTextureOffset(LogoTex);
    m_logoColor = sailMaterial.GetColor(LogoColor);
    m_logoRotation = sailMaterial.GetFloat(LogoRotation);

    if (VehicleGuiMenuConfig.HasDebugSails.Value)
    {
      LoggerProvider.LogDebug($"m_lockedSailSides {m_lockedSailSides}");
      LoggerProvider.LogDebug($"m_lockedSailCorners {m_lockedSailCorners}");
      LoggerProvider.LogDebug($"m_mainScale {m_mainScale}");
      LoggerProvider.LogDebug($"m_mainOffset {m_mainOffset}");
      LoggerProvider.LogDebug($"m_mainColor {m_mainColor}");
      LoggerProvider.LogDebug($"m_mainHash {m_mainHash}");
      LoggerProvider.LogDebug($"m_patternScale {m_patternScale}");
      LoggerProvider.LogDebug($"m_patternOffset {m_patternOffset}");
      LoggerProvider.LogDebug($"m_patternColor {m_patternColor}");
      LoggerProvider.LogDebug($"m_patternHash {m_patternHash}");
      LoggerProvider.LogDebug($"m_patternRotation {m_patternRotation}");
      LoggerProvider.LogDebug($"m_logoScale {m_logoScale}");
      LoggerProvider.LogDebug($"m_logoOffset {m_logoOffset}");
      LoggerProvider.LogDebug($"m_logoColor {m_logoColor}");
      LoggerProvider.LogDebug($"m_logoHash {m_logoHash}");
      LoggerProvider.LogDebug($"m_logoRotation {m_logoRotation}");
      LoggerProvider.LogDebug($"m_sailFlags {m_sailFlags}");
    }
  }

  private static int ResolveMaterialTextureHash(
    Texture? texture,
    string groupName,
    string textureRole)
  {
    if (!texture)
    {
      LoggerProvider.LogDebug(
        $"LoadFromMaterial(): Custom sail has no {textureRole} texture.");

      return 0;
    }

    var hash = texture.name.GetStableHashCode();
    var group = CustomTextureGroup.Get(groupName);

    if (group == null)
    {
      /*
       * Keep the hash even if registration is late. Once the asset group has
       * finished loading, the normal ZDO apply path can resolve this hash.
       */
      LoggerProvider.LogWarning(
        $"LoadFromMaterial(): Texture group '{groupName}' is not initialized yet. " +
        $"{textureRole} texture='{texture.name}', hash={hash}.");

      return hash;
    }

    if (group.GetTextureByHash(hash) == null)
    {
      LoggerProvider.LogWarning(
        $"LoadFromMaterial(): {textureRole} texture '{texture.name}' " +
        $"(hash={hash}) is not registered in texture group '{groupName}'.");
    }

    return hash;
  }

  public Material GetSailMaterial()
  {
    switch (m_materialVariant)
    {
      case MaterialVariant.Custom:
        if (customMaterial)
        {
          return customMaterial;
        }

        if (m_mesh)
        {
          customMaterial = GetPrimarySailRenderMaterial();
          return customMaterial;
        }

        throw new InvalidOperationException(
          $"Sail '{name}' has no custom material or renderer.");

      case MaterialVariant.Karve:
        return OverrideMaterial_VikingShipSail();

      case MaterialVariant.Drakkal:
        return OverrideMaterial_DrakkalShipSail();

      case MaterialVariant.Raft:
        return OverrideMaterial_RaftShipSail();

      default:
        if (!m_mesh)
        {
          throw new InvalidOperationException(
            $"Sail '{name}' has no renderer.");
        }

        return GetPrimarySailRenderMaterial();
    }
  }

  private Material GetPrimarySailRenderMaterial()
  {
    if (!m_mesh)
    {
      throw new InvalidOperationException(
        $"Sail '{name}' has no SkinnedMeshRenderer.");
    }

    var materials = m_mesh.sharedMaterials;

    if (materials.Length > 0 && materials[0])
    {
      return materials[0];
    }

    if (m_mesh.sharedMaterial)
    {
      return m_mesh.sharedMaterial;
    }

    // Last-resort material getter creates an instance, which is acceptable
    // here because the renderer had no usable shared material reference.
    return m_mesh.material;
  }

  private Material GetOrCreateFrontSailMaterial()
  {
    if (m_materialVariant == MaterialVariant.Custom)
    {
      if (!customMaterial)
      {
        customMaterial = GetPrimarySailRenderMaterial();
      }

      return customMaterial;
    }

    if (_variantSailMaterial)
    {
      return _variantSailMaterial;
    }

    var source = GetSailMaterial();

    _variantSailMaterial = new Material(source)
    {
      name = $"{source.name}_ValheimRAFT_{m_materialVariant}_Front"
    };

    return _variantSailMaterial;
  }

  private void ConfigureSailRenderMaterials()
  {
    if (!m_mesh)
    {
      return;
    }

    var frontMaterial = GetOrCreateFrontSailMaterial();

    if (!frontMaterial)
    {
      return;
    }

    if (_backSailMaterial)
    {
      Destroy(_backSailMaterial);
      _backSailMaterial = null;
    }

    _backSailMaterial = new Material(frontMaterial)
    {
      name = $"{frontMaterial.name}_Back"
    };

    /*
     * V7 uses reversed triangle winding for the back submesh while both
     * surfaces reference the exact same vertices. Therefore BOTH materials
     * use ordinary back-face culling.
     *
     * This is intentionally different from V6. V6 used two physically
     * separated render layers and Cull Front on the back material. At grazing
     * angles Magica could deform/map those close layers differently, exposing
     * one through the other and producing the apparent transparency/colour
     * bleeding seen from the side.
     *
     * With coincident faces only one winding is visible from a given side, so
     * there is no second broad surface behind it to alpha blend through.
     */
    var frontCullSet = TrySetSailCullMode(
      frontMaterial,
      UnityEngine.Rendering.CullMode.Back);

    var backCullSet = TrySetSailCullMode(
      _backSailMaterial,
      UnityEngine.Rendering.CullMode.Back);

    if ((!frontCullSet || !backCullSet) &&
        !_warnedMissingCullProperty)
    {
      _warnedMissingCullProperty = true;

      LoggerProvider.LogWarning(
        $"Sail shader '{frontMaterial.shader?.name ?? "<null>"}' does not expose a supported cull property. " +
        "V7 uses reversed winding for the back face and expects normal Cull Back behaviour. " +
        "If the shader hard-codes Cull Off, both submeshes can still be drawn together and alpha-blend. " +
        "Expose _Cull / _CullMode and bind it to the shader Cull state for deterministic two-sided rendering.");
    }

    var subMeshCount =
      m_mesh.sharedMesh
        ? Mathf.Max(1, m_mesh.sharedMesh.subMeshCount)
        : 1;

    if (subMeshCount >= 2)
    {
      m_mesh.sharedMaterials =
      [
        frontMaterial,
        _backSailMaterial
      ];
    }
    else
    {
      m_mesh.sharedMaterial = frontMaterial;
    }

    SetMaterialRenderQueue();
  }

  private bool SupportsSailCullControl()
  {
    Material? material = null;

    try
    {
      material = m_materialVariant == MaterialVariant.Custom
        ? customMaterial
        : GetSailMaterial();

      if (!material && m_mesh)
      {
        material = GetPrimarySailRenderMaterial();
      }
    }
    catch
    {
      // Mesh generation can run during early initialization. A missing material
      // simply selects the compatibility winding path for this build.
    }

    return material && HasSupportedCullProperty(material);
  }

  private static bool HasSupportedCullProperty(
    Material material)
  {
    return material.HasProperty("_Cull") ||
           material.HasProperty("_CullMode") ||
           material.HasProperty("_CullModeForward") ||
           material.HasProperty("_CullModeForwardOnly");
  }

  private static bool TrySetSailCullMode(
    Material material,
    UnityEngine.Rendering.CullMode mode)
  {
    var changed = false;

    if (material.HasProperty("_Cull"))
    {
      material.SetInt(
        "_Cull",
        (int)mode);
      changed = true;
    }

    if (material.HasProperty("_CullMode"))
    {
      material.SetInt(
        "_CullMode",
        (int)mode);
      changed = true;
    }

    if (material.HasProperty("_CullModeForward"))
    {
      material.SetInt(
        "_CullModeForward",
        (int)mode);
      changed = true;
    }

    if (material.HasProperty("_CullModeForwardOnly"))
    {
      material.SetInt(
        "_CullModeForwardOnly",
        (int)mode);
      changed = true;
    }

    return changed;
  }

  private void ApplyToSailMaterials(
    Action<Material> apply)
  {
    if (apply == null)
    {
      return;
    }

    var applied = new HashSet<Material>();

    if (m_mesh)
    {
      foreach (var material in m_mesh.sharedMaterials)
      {
        if (!material || !applied.Add(material))
        {
          continue;
        }

        apply(material);
      }
    }

    if (customMaterial && applied.Add(customMaterial))
    {
      apply(customMaterial);
    }

    if (_variantSailMaterial &&
        applied.Add(_variantSailMaterial))
    {
      apply(_variantSailMaterial);
    }

    if (_backSailMaterial &&
        applied.Add(_backSailMaterial))
    {
      apply(_backSailMaterial);
    }
  }

  private void ReleaseOwnedSailMaterials()
  {
    if (_backSailMaterial)
    {
      Destroy(_backSailMaterial);
      _backSailMaterial = null;
    }

    if (_variantSailMaterial)
    {
      Destroy(_variantSailMaterial);
      _variantSailMaterial = null;
    }
  }

  private void UpdateMistAlphaForPlayerCamera()
  {
    var player = Player.m_localPlayer;
    if (!player) return;
    var playerBiome =
      WorldGenerator.instance.GetBiome(player.transform.position);
    var sailBiome = WorldGenerator.instance.GetBiome(transform.position);

    var isInAshlands = playerBiome == Heightmap.Biome.AshLands ||
                       sailBiome == Heightmap.Biome.AshLands;
    var isPlayerInsideMister = Mister.InsideMister(player.transform.position);
    var isSailInsideMister = Mister.InsideMister(transform.position);

    if (!isInAshlands && !isPlayerInsideMister && !isSailInsideMister)
    {
      SetMistAlpha(1);
      return;
    }

    if (isPlayerInsideMister || isSailInsideMister || isInAshlands)
    {
      var distance =
        Vector3.Distance(player.transform.position, transform.position);
      if (distance > 60f && !isInAshlands)
      {
        SetMistAlpha(0);
        return;
      }

      var playerDistanceFromSail =
        Mathf.Clamp(distance, 0f, 20f);

      // ashlands has a haze (but not much)
      var alphaFromDistance = isInAshlands
        ? Mathf.Clamp((playerDistanceFromSail - 30f) / 10f, 0, 0.2f)
        : Mathf.Clamp(
          (playerDistanceFromSail * playerDistanceFromSail - 20) / 20f, 0,
          0.95f);
      SetMistAlpha(1 - alphaFromDistance);
    }
    else
    {
      SetMistAlpha(1);
    }
  }

  private void SetMistAlpha(float alpha)
  {
    if (Mathf.Approximately(m_mistAlpha, alpha)) return;
    m_mistAlpha = alpha;
    ApplyToSailMaterials(
      material => material.SetFloat(
        MistAlpha,
        Mathf.Clamp(alpha, 0, 1)));
  }

  public static int RenderQueueLevel = 2450;

  private void SetMaterialRenderQueue()
  {
    ApplyToSailMaterials(
      material => material.renderQueue = RenderQueueLevel);
  }

  public void LoadZDO()
  {
    if (!m_nview || m_nview.m_zdo == null || !GetIsInitialized()) return;

    var zdo = m_nview.m_zdo;

    var data = StoredSailDataExtensions.GetSerializableData(zdo, this);
    ApplyLoadedSailData(data);
  }

  /**
   * Using a single ReadColor per stream is not super efficient but it will eliminate any order dependent issues per ZDO sync
   */
  public Color GetColorFromByteStream(byte[] bytes)
  {
    var stream = new MemoryStream(bytes);
    var reader = new BinaryReader(stream);
    return reader.ReadColor();
  }

  public byte[] ConvertColorToByteStream(Color inputColor)
  {
    var stream = new MemoryStream();
    var writer = new BinaryWriter(stream);

    writer.Write(inputColor);

    return stream.ToArray();
  }

  private StoredSailData CreateStoredSailData()
  {
    return new StoredSailData
    {
      SailCorners = m_sailCorners.Select(v => new SerializableVector3(v)).ToList(),
      LockedSides = (int)m_lockedSailSides,
      LockedCorners = (int)m_lockedSailCorners,

      // for full overrides of custom sail
      MaterialVariant = (int)m_materialVariant,

      MainHash = m_mainHash,
      MainScale = new SerializableVector2(m_mainScale),
      MainOffset = new SerializableVector2(m_mainOffset),
      MainColor = new SerializableColor(m_mainColor),

      PatternHash = m_patternHash,
      PatternScale = new SerializableVector2(m_patternScale),
      PatternOffset = new SerializableVector2(m_patternOffset),
      PatternColor = new SerializableColor(m_patternColor),
      PatternRotation = m_patternRotation,

      LogoHash = m_logoHash,
      LogoScale = new SerializableVector2(m_logoScale),
      LogoOffset = new SerializableVector2(m_logoOffset),
      LogoColor = new SerializableColor(m_logoColor),
      LogoRotation = m_logoRotation,

      SailFlags = (int)m_sailFlags
    };
  }


  public void SaveZdo()
  {
    if (m_nview == null || m_nview.m_zdo == null) return;

    var zdo = m_nview.m_zdo;
    var data = CreateStoredSailData();

    /*
     * This instance is the authoritative source for the state being written.
     *
     * On a newly-created sail, OnEnable() has already started
     * WaitForInitialization(). ApplySerializableData() sets HasInitialized=true,
     * which used to wake that coroutine and immediately LoadZDO() back into the
     * same instance. The readback then called CreateSailMesh() a second time and
     * Magica correctly rejected it as "Already built".
     *
     * Stop the local initialization waiter before publishing HasInitialized.
     * Remote peers still receive/read the ZDO normally, while this client keeps
     * the exact in-memory state it just authored.
     */
    if (_waitForInitRoutine.IsRunning)
    {
      _waitForInitRoutine.Stop();
    }

    data.ApplySerializableData(zdo, this);

    // Saving establishes a fully initialized local sail; do not require the
    // initialization coroutine to register RPCs by rereading our own ZDO.
    RegisterRPC();

    Logger.LogDebug(
      $"SaveZdo(): wrote authoritative local sail state ({RuntimeImplementationVersion}); " +
      $"corners={m_sailCorners.Count}, clothBuilt={_hasActiveClothBuildState}, " +
      $"clothBuilding={_clothBuildInProgress}.");
  }

  /// <summary>
  /// Request for all peers to sync this new data. Does not send the data. This expects the data to arrive within a short period of time.
  /// </summary>
  public void RequestSyncZDOData()
  {
    if (m_nview == null || m_nview.m_zdo == null) return;
    m_nview.InvokeRPC(0L, nameof(RPC_SyncSailData));
  }

  public void RPC_SyncSailData(long sender)
  {
    if (!isActiveAndEnabled) return;
    // The sender already called LoadZDO directly after saving — skip the redundant reload.
    if (sender == ZNet.GetUID()) return;
    if (_loadZDORoutine.IsRunning) return;
    _loadZDORoutine.Start(Debounce_LoadZDO());
  }

  public IEnumerator Debounce_LoadZDO()
  {
    yield return new WaitForSeconds(0.1f);
    yield return new WaitForFixedUpdate();
    try
    {
      LoadZDO();
    }
    catch (Exception e)
    {
      // ignored
    }
  }

  /// <summary>
  /// Creates collision mesh, currently breaks for 3 
  /// </summary>
  /// todo May need to inflate the mesh
  /// <param name="size"></param>
  /// <returns></returns>
  public Mesh? CreateCollisionMesh(int size)
  {
    var collisionMesh = new Mesh();

    if (size == 3)
    {
      collisionMesh.name = "SailCollider_Tetrahedron";

      var a = m_sailCorners[0];
      var b = m_sailCorners[1];
      var c = m_sailCorners[2];

      // Compute normal and offset a bit along it to create a 4th vertex
      var normal = Vector3.Cross(b - a, c - a).normalized;
      var offset = normal * 0.01f;

      // Add 4th point slightly above the center of the triangle
      var d = (a + b + c) / 3f + offset;

      // Add 4 vertices (triangle base + top point)
      var vertices = new List<Vector3> { a, b, c, d };

      // Build a tetrahedron: base triangle + 3 side faces
      var triangles = new List<int>
      {
        0, 1, 2, // base
        0, 1, 3, // side 1
        1, 2, 3, // side 2
        2, 0, 3 // side 3
      };

      collisionMesh.SetVertices(vertices);
      collisionMesh.SetTriangles(triangles, 0);
      collisionMesh.RecalculateNormals();
      collisionMesh.RecalculateBounds();

      return collisionMesh;
    }

    if (size == 4)
    {
      collisionMesh.name = "SailCollider_Quad";
      collisionMesh.SetVertices(new List<Vector3>
      {
        m_sailCorners[0],
        m_sailCorners[1],
        m_sailCorners[2],
        m_sailCorners[3]
      });

      collisionMesh.SetTriangles(new[]
      {
        0, 1, 2,
        0, 2, 3
      }, 0);

      collisionMesh.RecalculateNormals();
      collisionMesh.RecalculateBounds();
    }

    return collisionMesh;
  }


  public void CreateSailMesh()
  {
    Logger.LogDebug(
      $"CreateSailMesh(): {m_sailCorners.Count} m_lockedSailCorners: {m_lockedSailCorners} ({(int)m_lockedSailCorners}) m_lockedSailSides: {m_lockedSailSides} ({(int)m_lockedSailSides})");

    if (m_sailCorners.Count is not (3 or 4))
    {
      LoggerProvider.LogError(
        $"CreateSailMesh(): Expected 3 or 4 sail corners, got {m_sailCorners.Count}.");

      return;
    }

    var buildState = new SailClothBuildState(
      m_sailCorners,
      m_sailSubdivision,
      m_sailThickness,
      m_lockedSailCorners,
      m_lockedSailSides);

    /*
     * LoadZDO() can run repeatedly without geometry changing. Material/ZDO
     * reloads must never rebuild a MagicaCloth that already represents this
     * exact construction state.
     */
    if (_hasActiveClothBuildState &&
        _activeClothBuildState.Equals(buildState))
    {
      Logger.LogDebug(
        "CreateSailMesh(): construction state already active; skipping Magica rebuild.");
      SyncSailClothEnabledState();
      return;
    }

    if (_hasPendingClothBuildState &&
        _pendingClothBuildState.Equals(buildState))
    {
      Logger.LogDebug(
        "CreateSailMesh(): identical construction state already pending; skipping duplicate request.");
      return;
    }

    if (_hasRebuildClothBuildState &&
        _rebuildClothBuildState.Equals(buildState))
    {
      Logger.LogDebug(
        "CreateSailMesh(): identical construction state already rebuilding; skipping duplicate request.");
      return;
    }

    if (!EnsureSailCloth())
    {
      LoggerProvider.LogError(
        $"CreateSailMesh(): No usable MagicaCloth on '{name}'.");

      return;
    }

    var mesh = CreateGeneratedSailMesh();

    if (!mesh)
    {
      LoggerProvider.LogError(
        $"CreateSailMesh(): Failed to generate sail mesh for '{name}'.");

      return;
    }

    QueueSailClothRebuild(mesh, buildState);
  }

  private Mesh? CreateGeneratedSailMesh()
  {
    var vertices = new List<Vector3>();
    var uvs = new List<Vector2>();
    var triangles = new List<int>();

    if (m_sailCorners.Count == 3)
    {
      vertices.Add(m_sailCorners[0]);
      vertices.Add(m_sailCorners[1]);
      vertices.Add(m_sailCorners[2]);

      triangles.Add(0);
      triangles.Add(1);
      triangles.Add(2);

      uvs.Add(new Vector2(0f, 0f));
      uvs.Add(new Vector2(1f, 0f));
      uvs.Add(new Vector2(0.5f, 1f));
    }
    else if (m_sailCorners.Count == 4)
    {
      /*
       * Canonical quad topology:
       *
       *   3 (TL) -------- 0 (TR)
       *     |              |
       *     |              |
       *   2 (BL) -------- 1 (BR)
       *
       * Use actual edge lengths, never the TR->BL diagonal, to choose grid
       * subdivision counts.
       */
      var verticalLength = Mathf.Max(
        (m_sailCorners[1] - m_sailCorners[0]).magnitude,
        (m_sailCorners[2] - m_sailCorners[3]).magnitude);

      var horizontalLength = Mathf.Max(
        (m_sailCorners[3] - m_sailCorners[0]).magnitude,
        (m_sailCorners[2] - m_sailCorners[1]).magnitude);

      var verticalSegments = Mathf.Max(
        1,
        Mathf.RoundToInt(
          verticalLength / m_sailSubdivision));

      var horizontalSegments = Mathf.Max(
        1,
        Mathf.RoundToInt(
          horizontalLength / m_sailSubdivision));

      for (var vertical = 0;
           vertical <= verticalSegments;
           vertical++)
      {
        var verticalT =
          (float)vertical / verticalSegments;

        var right = Vector3.Lerp(
          m_sailCorners[0],
          m_sailCorners[1],
          verticalT);

        var left = Vector3.Lerp(
          m_sailCorners[3],
          m_sailCorners[2],
          verticalT);

        for (var horizontal = 0;
             horizontal <= horizontalSegments;
             horizontal++)
        {
          var horizontalT =
            (float)horizontal / horizontalSegments;

          vertices.Add(
            Vector3.Lerp(
              right,
              left,
              horizontalT));

          /*
           * Preserve the historical custom-sail UV orientation: U runs down
           * the sail and V runs from right to left.
           */
          uvs.Add(
            new Vector2(
              verticalT,
              horizontalT));
        }
      }

      var rowSize = horizontalSegments + 1;

      for (var vertical = 0;
           vertical < verticalSegments;
           vertical++)
      {
        for (var horizontal = 0;
             horizontal < horizontalSegments;
             horizontal++)
        {
          var current =
            vertical * rowSize + horizontal;

          var nextRow =
            current + rowSize;

          triangles.Add(current + 1);
          triangles.Add(current);
          triangles.Add(nextRow);

          triangles.Add(current + 1);
          triangles.Add(nextRow);
          triangles.Add(nextRow + 1);
        }
      }
    }

    if (vertices.Count == 0 ||
        triangles.Count == 0)
    {
      return null;
    }

    var mesh = new Mesh
    {
      name = $"ValheimRAFT_Sail_{GetInstanceID()}"
    };

    mesh.SetVertices(vertices);
    mesh.SetTriangles(triangles, 0);
    mesh.SetUVs(0, uvs);
    mesh.RecalculateNormals();
    mesh.RecalculateTangents();
    mesh.RecalculateBounds();

    if (m_sailCorners.Count == 3)
    {
      var sqrSubDist =
        m_sailSubdivision * m_sailSubdivision;

      var subdivisionCount = 0;
      const int maxSubdivisions = 3;

      while (subdivisionCount < maxSubdivisions)
      {
        var meshVertices = mesh.vertices;
        var meshTriangles = mesh.triangles;

        if (meshTriangles.Length < 2)
        {
          break;
        }

        var dist =
          (meshVertices[meshTriangles[0]] -
           meshVertices[meshTriangles[1]])
          .sqrMagnitude;

        if (dist < sqrSubDist)
        {
          break;
        }

        MeshUtils.Subdivide(mesh);
        subdivisionCount++;
      }

      mesh.RecalculateNormals();
      mesh.RecalculateTangents();
      mesh.RecalculateBounds();
    }

    ConfigureGeneratedMeshSkinning(mesh);
    ExpandGeneratedMeshToTwoSidedShell(mesh);

    return mesh;
  }

  private void ExpandGeneratedMeshToTwoSidedShell(Mesh mesh)
  {
    /*
     * V7 rendering architecture
     * -------------------------
     *
     * DO NOT create two physically separated broad cloth surfaces.
     *
     * A thin closed shell looks attractive while static, but it is a poor
     * source mesh for Magica MeshCloth. The front and back render vertices are
     * independent vertices. Even when proxy reduction merges them closely, the
     * render mapping can diverge while the cloth bends. At grazing angles that
     * exposes the opposite layer, producing the dark/light wedges and apparent
     * transparency seen in V6.
     *
     * Instead, front and back are two submeshes which reference ONE shared
     * vertex set:
     *
     *   submesh 0: original winding
     *   submesh 1: reversed winding
     *
     * Both faces therefore have exactly the same simulated position at all
     * times. There is mathematically no gap for one side to bleed through.
     *
     * Normals/tangents remain the front-face basis for both submeshes. The
     * reversed back triangles control visibility, not shading. That preserves
     * the matched front/back colour response without relying on VFACE or a
     * second physical cloth layer.
     *
     * m_sailThickness is intentionally NOT applied to the Magica source mesh
     * anymore. True geometric thickness must be a render-only effect separate
     * from the simulated MeshCloth. Keeping thickness inside this source mesh
     * reintroduces the V6 layer-separation problem.
     */
    var baseVertices = mesh.vertices;
    var baseTriangles = mesh.triangles;
    var baseNormals = mesh.normals;
    var baseTangents = mesh.tangents;
    var baseBoneWeights = mesh.boneWeights;
    var bindPoses = mesh.bindposes;

    if (baseVertices.Length == 0 ||
        baseTriangles.Length == 0)
    {
      _sailAttributeReferenceVertices = Array.Empty<Vector3>();
      return;
    }

    if (baseNormals.Length != baseVertices.Length)
    {
      mesh.RecalculateNormals();
      baseNormals = mesh.normals;
    }

    if (baseTangents.Length != baseVertices.Length)
    {
      mesh.RecalculateTangents();
      baseTangents = mesh.tangents;
    }

    var baseUvs = new List<Vector2>();
    mesh.GetUVs(0, baseUvs);

    if (baseUvs.Count != baseVertices.Length)
    {
      baseUvs.Clear();

      for (var i = 0; i < baseVertices.Length; i++)
      {
        baseUvs.Add(Vector2.zero);
      }
    }

    var backTriangles =
      new List<int>(baseTriangles.Length);

    for (var i = 0; i < baseTriangles.Length; i += 3)
    {
      var a = baseTriangles[i];
      var b = baseTriangles[i + 1];
      var c = baseTriangles[i + 2];

      // Reverse winding only. The vertex normal/tangent basis remains shared
      // with the front face so both sides receive the same lighting basis.
      backTriangles.Add(a);
      backTriangles.Add(c);
      backTriangles.Add(b);
    }

    mesh.Clear();

    if (baseVertices.Length > ushort.MaxValue)
    {
      mesh.indexFormat =
        UnityEngine.Rendering.IndexFormat.UInt32;
    }

    mesh.SetVertices(baseVertices);
    mesh.SetNormals(baseNormals);
    mesh.SetTangents(baseTangents);
    mesh.SetUVs(0, baseUvs);

    mesh.subMeshCount = 2;
    mesh.SetTriangles(baseTriangles, 0, false);
    mesh.SetTriangles(backTriangles, 1, false);

    if (baseBoneWeights.Length == baseVertices.Length)
    {
      mesh.boneWeights = baseBoneWeights;
      mesh.bindposes = bindPoses;
    }

    /*
     * There is now exactly one render/simulation vertex for every logical sail
     * vertex, so pinning can use the original center-surface positions
     * directly.
     */
    _sailAttributeReferenceVertices =
      baseVertices.ToArray();

    // Never recalculate normals/tangents after adding the reversed submesh.
    // They deliberately represent one shared lighting basis.
    mesh.RecalculateBounds();
  }

  private static Vector4 NormalizeTangent(
    Vector4 tangent,
    Vector3 normal)
  {
    var tangent3 = new Vector3(
      tangent.x,
      tangent.y,
      tangent.z);

    tangent3 = Vector3.ProjectOnPlane(
      tangent3,
      normal);

    if (tangent3.sqrMagnitude <= Mathf.Epsilon)
    {
      return BuildFallbackTangent(normal);
    }

    tangent3.Normalize();

    return new Vector4(
      tangent3.x,
      tangent3.y,
      tangent3.z,
      Mathf.Approximately(tangent.w, 0f)
        ? 1f
        : Mathf.Sign(tangent.w));
  }

  private static Vector4 BuildFallbackTangent(
    Vector3 normal)
  {
    var reference = Mathf.Abs(
      Vector3.Dot(normal, Vector3.up)) < 0.95f
      ? Vector3.up
      : Vector3.right;

    var tangent = Vector3.Cross(
      reference,
      normal).normalized;

    return new Vector4(
      tangent.x,
      tangent.y,
      tangent.z,
      1f);
  }

  private static void AddIndependentShellBoundaryGeometry(
    IReadOnlyList<Vector3> baseVertices,
    IReadOnlyList<Vector3> baseNormals,
    IReadOnlyList<Vector4> baseTangents,
    IReadOnlyList<Vector2> baseUvs,
    IReadOnlyList<BoneWeight> baseBoneWeights,
    IReadOnlyList<int> baseTriangles,
    float halfThickness,
    List<Vector3> shellVertices,
    List<Vector3> shellNormals,
    List<Vector4> shellTangents,
    List<Vector2> shellUvs,
    List<BoneWeight> shellBoneWeights,
    List<Vector3> attributeReferenceVertices,
    ICollection<int> sideTriangles)
  {
    var boundaryEdges =
      CollectBoundaryEdges(baseTriangles);

    var center = Vector3.zero;

    for (var i = 0; i < baseVertices.Count; i++)
    {
      center += baseVertices[i];
    }

    center /= Mathf.Max(1, baseVertices.Count);

    foreach (var edge in boundaryEdges)
    {
      var start = edge.Start;
      var end = edge.End;

      var startNormal = baseNormals[start].sqrMagnitude > Mathf.Epsilon
        ? baseNormals[start].normalized
        : Vector3.forward;

      var endNormal = baseNormals[end].sqrMagnitude > Mathf.Epsilon
        ? baseNormals[end].normalized
        : startNormal;

      var frontStart =
        baseVertices[start] + startNormal * halfThickness;

      var frontEnd =
        baseVertices[end] + endNormal * halfThickness;

      var backStart =
        baseVertices[start] - startNormal * halfThickness;

      var backEnd =
        baseVertices[end] - endNormal * halfThickness;

      var edgeDirection =
        baseVertices[end] - baseVertices[start];

      if (edgeDirection.sqrMagnitude <= Mathf.Epsilon)
      {
        continue;
      }

      edgeDirection.Normalize();

      var averageSurfaceNormal =
        (startNormal + endNormal).normalized;

      if (averageSurfaceNormal.sqrMagnitude <= Mathf.Epsilon)
      {
        averageSurfaceNormal = startNormal;
      }

      var wallNormal = Vector3.Cross(
        edgeDirection,
        averageSurfaceNormal).normalized;

      var edgeMidpoint =
        (baseVertices[start] + baseVertices[end]) * 0.5f;

      if (Vector3.Dot(
            wallNormal,
            edgeMidpoint - center) < 0f)
      {
        wallNormal = -wallNormal;
      }

      var wallTangent = new Vector4(
        edgeDirection.x,
        edgeDirection.y,
        edgeDirection.z,
        1f);

      var first = shellVertices.Count;

      // Independent wall vertices: never share smoothing data with the faces.
      shellVertices.Add(frontStart);
      shellVertices.Add(frontEnd);
      shellVertices.Add(backEnd);
      shellVertices.Add(backStart);

      for (var i = 0; i < 4; i++)
      {
        shellNormals.Add(wallNormal);
        shellTangents.Add(wallTangent);
      }

      shellUvs.Add(baseUvs[start]);
      shellUvs.Add(baseUvs[end]);
      shellUvs.Add(baseUvs[end]);
      shellUvs.Add(baseUvs[start]);

      attributeReferenceVertices.Add(baseVertices[start]);
      attributeReferenceVertices.Add(baseVertices[end]);
      attributeReferenceVertices.Add(baseVertices[end]);
      attributeReferenceVertices.Add(baseVertices[start]);

      if (baseBoneWeights.Count == baseVertices.Count)
      {
        shellBoneWeights.Add(baseBoneWeights[start]);
        shellBoneWeights.Add(baseBoneWeights[end]);
        shellBoneWeights.Add(baseBoneWeights[end]);
        shellBoneWeights.Add(baseBoneWeights[start]);
      }
      else
      {
        shellBoneWeights.Add(default);
        shellBoneWeights.Add(default);
        shellBoneWeights.Add(default);
        shellBoneWeights.Add(default);
      }

      var a = first;
      var b = first + 1;
      var c = first + 2;
      var d = first + 3;

      var generatedNormal = Vector3.Cross(
        shellVertices[b] - shellVertices[a],
        shellVertices[c] - shellVertices[a]);

      if (Vector3.Dot(generatedNormal, wallNormal) >= 0f)
      {
        sideTriangles.Add(a);
        sideTriangles.Add(b);
        sideTriangles.Add(c);

        sideTriangles.Add(a);
        sideTriangles.Add(c);
        sideTriangles.Add(d);
      }
      else
      {
        sideTriangles.Add(a);
        sideTriangles.Add(c);
        sideTriangles.Add(b);

        sideTriangles.Add(a);
        sideTriangles.Add(d);
        sideTriangles.Add(c);
      }
    }
  }

  private static List<BoundaryEdgeInfo> CollectBoundaryEdges(
    IReadOnlyList<int> triangles)
  {
    var edges =
      new Dictionary<(int Min, int Max), BoundaryEdgeInfo>();

    void AddEdge(int start, int end)
    {
      var key = start < end
        ? (start, end)
        : (end, start);

      if (edges.TryGetValue(key, out var existing))
      {
        edges[key] = new BoundaryEdgeInfo(
          existing.Start,
          existing.End,
          existing.Count + 1);

        return;
      }

      edges[key] = new BoundaryEdgeInfo(
        start,
        end,
        1);
    }

    for (var i = 0; i < triangles.Count; i += 3)
    {
      var a = triangles[i];
      var b = triangles[i + 1];
      var c = triangles[i + 2];

      AddEdge(a, b);
      AddEdge(b, c);
      AddEdge(c, a);
    }

    return edges.Values
      .Where(edge => edge.Count == 1)
      .ToList();
  }

  private readonly struct BoundaryEdgeInfo
  {
    public readonly int Start;
    public readonly int End;
    public readonly int Count;

    public BoundaryEdgeInfo(
      int start,
      int end,
      int count)
    {
      Start = start;
      End = end;
      Count = count;
    }
  }

  private void ConfigureGeneratedMeshSkinning(Mesh mesh)
  {
    if (m_sailCorners.Count == 4)
    {
      ConfigureQuadSkinning(mesh);
      return;
    }

    if (m_sailCorners.Count == 3)
    {
      ConfigureTriangleSkinning(mesh);
    }
  }

  private void ConfigureQuadSkinning(Mesh mesh)
  {
    ConfigureFurlRowSkinning(mesh, 4);
  }

  private void ConfigureTriangleSkinning(Mesh mesh)
  {
    ConfigureFurlRowSkinning(mesh, 3);
  }

  private void ConfigureFurlRowSkinning(
    Mesh mesh,
    int cornerCount)
  {
    var vertices = mesh.vertices;

    if (vertices.Length == 0)
    {
      return;
    }

    var boneCount =
      CalculateFurlBoneCount(
        cornerCount,
        m_sailCorners[0],
        m_sailCorners[1],
        m_sailCorners[2],
        cornerCount == 4
          ? m_sailCorners[3]
          : Vector3.zero);

    var weights = new BoneWeight[vertices.Length];
    var bindPoses = new Matrix4x4[boneCount];

    for (var boneIndex = 0;
         boneIndex < boneCount;
         boneIndex++)
    {
      var t = boneCount <= 1
        ? 0f
        : (float)boneIndex / (boneCount - 1);

      var center = GetSailCenterAtT(
        cornerCount,
        m_sailCorners[0],
        m_sailCorners[1],
        m_sailCorners[2],
        cornerCount == 4
          ? m_sailCorners[3]
          : Vector3.zero,
        t);

      bindPoses[boneIndex] =
        Matrix4x4.Translate(-center);
    }

    for (var i = 0;
         i < vertices.Length;
         i++)
    {
      var verticalT = EstimateSailVerticalT(
        vertices[i],
        cornerCount,
        m_sailCorners[0],
        m_sailCorners[1],
        m_sailCorners[2],
        cornerCount == 4
          ? m_sailCorners[3]
          : Vector3.zero);

      var scaled =
        verticalT * (boneCount - 1);

      var lower = Mathf.Clamp(
        Mathf.FloorToInt(scaled),
        0,
        boneCount - 1);

      var upper = Mathf.Min(
        lower + 1,
        boneCount - 1);

      var upperWeight =
        upper == lower
          ? 0f
          : scaled - lower;

      weights[i] = new BoneWeight
      {
        boneIndex0 = lower,
        weight0 = 1f - upperWeight,
        boneIndex1 = upper,
        weight1 = upperWeight,
        boneIndex2 = 0,
        weight2 = 0f,
        boneIndex3 = 0,
        weight3 = 0f
      };
    }

    mesh.boneWeights = weights;
    mesh.bindposes = bindPoses;
  }

  private int CalculateFurlBoneCount(
    int cornerCount,
    Vector3 cornerA,
    Vector3 cornerB,
    Vector3 cornerC,
    Vector3 cornerD)
  {
    var height = CalculateSailCenterlineLength(
      cornerCount,
      cornerA,
      cornerB,
      cornerC,
      cornerD);

    var spacing = Mathf.Max(
      0.15f,
      m_sailSubdivision);

    var segmentCount = Mathf.Clamp(
      Mathf.CeilToInt(height / spacing),
      MinimumFurlBoneCount - 1,
      MaximumFurlBoneCount - 1);

    return segmentCount + 1;
  }

  private static float CalculateSailCenterlineLength(
    int cornerCount,
    Vector3 cornerA,
    Vector3 cornerB,
    Vector3 cornerC,
    Vector3 cornerD)
  {
    var top = GetSailCenterAtT(
      cornerCount,
      cornerA,
      cornerB,
      cornerC,
      cornerD,
      0f);

    var bottom = GetSailCenterAtT(
      cornerCount,
      cornerA,
      cornerB,
      cornerC,
      cornerD,
      1f);

    return Mathf.Max(
      0.001f,
      Vector3.Distance(top, bottom));
  }

  private static Vector3 GetSailCenterAtT(
    int cornerCount,
    Vector3 cornerA,
    Vector3 cornerB,
    Vector3 cornerC,
    Vector3 cornerD,
    float t)
  {
    t = Mathf.Clamp01(t);

    if (cornerCount == 4)
    {
      // Quad convention: A=TR, B=BR, C=BL, D=TL.
      var right = Vector3.Lerp(
        cornerA,
        cornerB,
        t);

      var left = Vector3.Lerp(
        cornerD,
        cornerC,
        t);

      return (right + left) * 0.5f;
    }

    // Triangle convention: A=BL, B=BR, C=TOP.
    var bottomCenter =
      (cornerA + cornerB) * 0.5f;

    return Vector3.Lerp(
      cornerC,
      bottomCenter,
      t);
  }

  private static float EstimateSailVerticalT(
    Vector3 point,
    int cornerCount,
    Vector3 cornerA,
    Vector3 cornerB,
    Vector3 cornerC,
    Vector3 cornerD)
  {
    if (cornerCount == 4)
    {
      return EstimateQuadVerticalT(
        point,
        cornerA,
        cornerB,
        cornerC,
        cornerD);
    }

    var bottomCenter =
      (cornerA + cornerB) * 0.5f;

    return SegmentProjection01(
      point,
      cornerC,
      bottomCenter);
  }

  private static Vector3 CalculateSailFrontNormal(
    SailClothBuildState buildState)
  {
    Vector3 normal;

    if (buildState.CornerCount == 4)
    {
      normal = Vector3.Cross(
        buildState.CornerA - buildState.CornerD,
        buildState.CornerB - buildState.CornerA);
    }
    else
    {
      normal = Vector3.Cross(
        buildState.CornerB - buildState.CornerA,
        buildState.CornerC - buildState.CornerA);
    }

    if (normal.sqrMagnitude <= Mathf.Epsilon)
    {
      return Vector3.forward;
    }

    return normal.normalized;
  }

  private static float EstimateQuadVerticalT(
    Vector3 point,
    Vector3 rightTop,
    Vector3 rightBottom,
    Vector3 leftBottom,
    Vector3 leftTop)
  {
    var rightT = SegmentProjection01(
      point,
      rightTop,
      rightBottom);

    var leftT = SegmentProjection01(
      point,
      leftTop,
      leftBottom);

    return Mathf.Clamp01(
      (rightT + leftT) * 0.5f);
  }

  private static float SegmentProjection01(
    Vector3 point,
    Vector3 start,
    Vector3 end)
  {
    var segment = end - start;
    var lengthSqr = segment.sqrMagnitude;

    if (lengthSqr <= Mathf.Epsilon)
    {
      return 0f;
    }

    return Mathf.Clamp01(
      Vector3.Dot(
        point - start,
        segment) /
      lengthSqr);
  }

  private static Vector3 CalculateBarycentric(
    Vector3 point,
    Vector3 a,
    Vector3 b,
    Vector3 c)
  {
    var v0 = b - a;
    var v1 = c - a;
    var v2 = point - a;

    var d00 = Vector3.Dot(v0, v0);
    var d01 = Vector3.Dot(v0, v1);
    var d11 = Vector3.Dot(v1, v1);
    var d20 = Vector3.Dot(v2, v0);
    var d21 = Vector3.Dot(v2, v1);

    var denominator =
      d00 * d11 - d01 * d01;

    if (Mathf.Abs(denominator) <= Mathf.Epsilon)
    {
      return new Vector3(1f, 0f, 0f);
    }

    var v =
      (d11 * d20 - d01 * d21) /
      denominator;

    var w =
      (d00 * d21 - d01 * d20) /
      denominator;

    var u = 1f - v - w;

    return new Vector3(
      Mathf.Clamp01(u),
      Mathf.Clamp01(v),
      Mathf.Clamp01(w));
  }

  private void QueueSailClothRebuild(
    Mesh mesh,
    SailClothBuildState buildState)
  {
    /*
     * Coalesce multiple ZDO/edit updates. If a newer geometry request arrives
     * while Magica is being torn down, only the newest pending mesh is built.
     */
    if (_pendingSailMesh)
    {
      Destroy(_pendingSailMesh);
    }

    _pendingSailMesh = mesh;
    _pendingClothBuildState = buildState;
    _hasPendingClothBuildState = true;

    if (!_clothRebuildRoutine.IsRunning)
    {
      _clothRebuildRoutine.Start(ProcessSailClothRebuildQueue());
    }
  }

  private IEnumerator ProcessSailClothRebuildQueue()
  {
    if (_clothBuildInProgress)
    {
      yield break;
    }

    while (_hasPendingClothBuildState &&
           _pendingSailMesh)
    {
      _rebuildSailMesh = _pendingSailMesh;
      _rebuildClothBuildState = _pendingClothBuildState;
      _hasRebuildClothBuildState = true;

      _pendingSailMesh = null;
      _hasPendingClothBuildState = false;

      var oldCloth = m_sailCloth;
      var oldClothIsBuilt =
        oldCloth &&
        (_hasActiveClothBuildState || oldCloth.IsValid());

      var cloth = oldCloth;

      if (oldClothIsBuilt)
      {
        /*
         * Create the replacement before destroying the current instance so its
         * complete runtime parameter tuning can be imported parameter-only.
         */
        var replacement =
          CreateReplacementSailCloth(oldCloth);

        oldCloth.enabled = false;

        if (m_mastComponent &&
            m_mastComponent.m_sailCloth == oldCloth)
        {
          m_mastComponent.m_sailCloth = null;
        }

        m_sailCloth = null;
        _hasActiveClothBuildState = false;

        Destroy(oldCloth);

        /*
         * Magica owns renderer/proxy resources. Wait until Unity has actually
         * destroyed the old component before replacing renderer construction
         * state.
         */
        yield return null;

        if (!isActiveAndEnabled)
        {
          if (replacement)
          {
            Destroy(replacement);
          }

          ReleaseRebuildSailMesh();
          yield break;
        }

        if (_hasPendingClothBuildState &&
            _pendingSailMesh)
        {
          /*
           * The current geometry was superseded while the old cloth was being
           * removed. Keep the parameter-preserving replacement for the newest
           * request but discard this intermediate mesh.
           */
          m_sailCloth = replacement;
          ReleaseRebuildSailMesh();
          continue;
        }

        cloth = replacement;
        m_sailCloth = cloth;
      }

      if (!cloth)
      {
        cloth = CreateReplacementSailCloth(null);
        m_sailCloth = cloth;
      }

      if (!cloth)
      {
        LoggerProvider.LogError(
          $"ProcessSailClothRebuildQueue(): Unable to create MagicaCloth for '{name}'.");

        ReleaseRebuildSailMesh();
        continue;
      }

      cloth.DisableAutoBuild();

      if (_generatedSailMesh &&
          _generatedSailMesh != _rebuildSailMesh)
      {
        if (m_mesh &&
            m_mesh.sharedMesh == _generatedSailMesh)
        {
          m_mesh.sharedMesh = null;
        }

        Destroy(_generatedSailMesh);
      }

      _generatedSailMesh = _rebuildSailMesh;
      _rebuildSailMesh = null;

      ConfigureSailRigForBuildState(
        _rebuildClothBuildState);

      m_mesh.sharedMesh = _generatedSailMesh;
      m_mesh.localBounds = _generatedSailMesh.bounds;
      m_mesh.updateWhenOffscreen = true;

      ConfigureSailRenderMaterials();

      ConfigureSailClothForMesh(
        cloth,
        _generatedSailMesh);

      UpdateMeshCollider();

      /*
       * Let SkinnedMeshRenderer consume the newly assigned mesh/bones once
       * before Magica captures its initialization pose. This removes another
       * timing-sensitive source of bad initial proxy positions.
       */
      yield return null;

      if (!isActiveAndEnabled)
      {
        yield break;
      }

      if (_hasPendingClothBuildState &&
          _pendingSailMesh)
      {
        /*
         * This Magica component has not been built yet, so it is safe to reuse
         * it for the newest source mesh.
         */
        _hasActiveClothBuildState = false;
        _hasRebuildClothBuildState = false;
        continue;
      }

      _activeClothBuildState =
        _rebuildClothBuildState;

      _hasActiveClothBuildState = true;
      _hasRebuildClothBuildState = false;

      var expectedState =
        _activeClothBuildState;

      cloth.OnBuildComplete +=
        (completedCloth, success) =>
        {
          OnSailClothBuildComplete(
            completedCloth,
            expectedState,
            success);
        };

      cloth.enabled = true;

      if (!cloth.BuildAndRun())
      {
        LoggerProvider.LogError(
          $"CreateSailMesh(): MagicaCloth BuildAndRun could not start for '{name}'.");

        _clothBuildInProgress = false;
        _hasActiveClothBuildState = false;

        if (m_mastComponent &&
            m_mastComponent.m_sailCloth == cloth)
        {
          m_mastComponent.m_sailCloth = null;
        }

        if (m_sailCloth == cloth)
        {
          m_sailCloth = null;
        }

        Destroy(cloth);
        continue;
      }

      _clothBuildInProgress = true;

      if (m_mastComponent)
      {
        m_mastComponent.m_sailCloth = cloth;
        m_mastComponent.m_customSailComponent = this;
      }

      /*
       * Never tear down a cloth while BuildAndRun() is still constructing it.
       * If a newer edit arrives, the callback clears _clothBuildInProgress and
       * FixedUpdate starts the queued replacement afterward.
       */
      yield break;
    }

    _hasRebuildClothBuildState = false;
  }

  private void ConfigureSailClothForMesh(
    MagicaCloth cloth,
    Mesh mesh)
  {
    var serializeData = cloth.SerializeData;
    var serializeData2 = cloth.GetSerializeData2();

    /*
     * Construction-specific values only. All physical tuning imported from the
     * existing/vanilla cloth is intentionally preserved.
     */
    serializeData.updateMode =
      ClothUpdateMode.UnityPhysics;

    serializeData.clothType =
      ClothProcess.ClothType.MeshCloth;

    serializeData.paintMode =
      ClothSerializeData.PaintMode.Manual;

    /*
     * Normal-mapped sails need Magica to update tangents as the cloth bends.
     * PositionAndNormal alone leaves bind-pose tangents behind and causes
     * normal-map lighting to drift or invert as the sail deforms.
     */
    serializeData.meshWriteMode =
      ClothMeshWriteMode.PositionAndNormalTangent;

    EnsureShellProxyReduction(
      serializeData,
      mesh);

    serializeData.stablizationTimeAfterReset =
      Mathf.Max(
        serializeData.stablizationTimeAfterReset,
        0.15f);

    serializeData.sourceRenderers.Clear();
    serializeData.sourceRenderers.Add(m_mesh);

    var attributes =
      BuildSailVertexAttributes(mesh);

    serializeData2.vertexAttributeList.Clear();
    serializeData2.vertexAttributeList.Add(attributes);
  }

  private void EnsureShellProxyReduction(
    ClothSerializeData serializeData,
    Mesh mesh)
  {
    /*
     * V7 has no duplicated broad render layer to merge. Front and back
     * submeshes reference the same vertices, so Magica already sees exactly
     * one cloth surface.
     *
     * Keep this method as the lifecycle hook used by ConfigureSailClothForMesh
     * rather than removing it; future render-only thickness must not alter the
     * simulation reduction settings here.
     */
  }

  private void ConfigureSailRigForBuildState(
    SailClothBuildState buildState)
  {
    var boneCount =
      CalculateFurlBoneCount(
        buildState.CornerCount,
        buildState.CornerA,
        buildState.CornerB,
        buildState.CornerC,
        buildState.CornerD);

    EnsureSailRig(boneCount);

    _sailRigBuildState = buildState;
    _hasSailRigBuildState = true;
    _sailRigBoneCount = boneCount;

    var bones =
      new Transform[boneCount];

    for (var i = 0;
         i < boneCount;
         i++)
    {
      var bone = _sailRigBones[i];

      if (!bone)
      {
        continue;
      }

      var t = boneCount <= 1
        ? 0f
        : (float)i / (boneCount - 1);

      bone.localPosition = GetSailCenterAtT(
        buildState.CornerCount,
        buildState.CornerA,
        buildState.CornerB,
        buildState.CornerC,
        buildState.CornerD,
        t);

      bone.localRotation = Quaternion.identity;
      bone.localScale = Vector3.one;
      bone.gameObject.SetActive(true);

      bones[i] = bone;
    }

    for (var i = boneCount;
         i < _sailRigBones.Count;
         i++)
    {
      var unusedBone = _sailRigBones[i];

      if (unusedBone)
      {
        unusedBone.gameObject.SetActive(false);
      }
    }

    m_mesh.rootBone = _sailRigRoot;
    m_mesh.bones = bones;

    ApplySailRigPosition(
      _requestedSailPosition);
  }

  private void EnsureSailRig(int requiredBoneCount)
  {
    if (!_sailRigRoot)
    {
      var existing =
        transform.Find(SailRigRootName);

      if (existing)
      {
        _sailRigRoot = existing;
      }
      else
      {
        var root =
          new GameObject(SailRigRootName);

        _sailRigRoot = root.transform;
        _sailRigRoot.SetParent(transform, false);
      }
    }

    _sailRigRoot.localPosition = Vector3.zero;
    _sailRigRoot.localRotation = Quaternion.identity;
    _sailRigRoot.localScale = Vector3.one;

    for (var i = _sailRigBones.Count;
         i < requiredBoneCount;
         i++)
    {
      var boneName =
        $"FurlRow_{i:D2}";

      var existingBone =
        _sailRigRoot.Find(boneName);

      if (existingBone)
      {
        _sailRigBones.Add(existingBone);
        continue;
      }

      var boneObject =
        new GameObject(boneName);

      var boneTransform =
        boneObject.transform;

      boneTransform.SetParent(
        _sailRigRoot,
        false);

      _sailRigBones.Add(
        boneTransform);
    }
  }

  public void SetSailPosition(float sailPosition)
  {
    _requestedSailPosition =
      Mathf.Clamp01(sailPosition);

    ApplySailRigPosition(
      _requestedSailPosition);
  }

  private void ApplySailRigPosition(float sailPosition)
  {
    if (!_sailRigRoot ||
        !_hasSailRigBuildState ||
        _sailRigBoneCount < 2)
    {
      return;
    }

    var position =
      Mathf.Clamp01(sailPosition);

    var buildState =
      _sailRigBuildState;

    var topCenter = GetSailCenterAtT(
      buildState.CornerCount,
      buildState.CornerA,
      buildState.CornerB,
      buildState.CornerC,
      buildState.CornerD,
      0f);

    var bottomCenter = GetSailCenterAtT(
      buildState.CornerCount,
      buildState.CornerA,
      buildState.CornerB,
      buildState.CornerC,
      buildState.CornerD,
      1f);

    var downDirection =
      bottomCenter - topCenter;

    if (downDirection.sqrMagnitude <= Mathf.Epsilon)
    {
      return;
    }

    downDirection.Normalize();

    var surfaceNormal =
      CalculateSailFrontNormal(buildState);

    surfaceNormal = Vector3.ProjectOnPlane(
      surfaceNormal,
      downDirection);

    if (surfaceNormal.sqrMagnitude <= Mathf.Epsilon)
    {
      surfaceNormal = Vector3.Cross(
        Vector3.right,
        downDirection);
    }

    if (surfaceNormal.sqrMagnitude <= Mathf.Epsilon)
    {
      surfaceNormal = Vector3.Cross(
        Vector3.up,
        downDirection);
    }

    surfaceNormal.Normalize();

    var rollStart = GetSailCenterAtT(
      buildState.CornerCount,
      buildState.CornerA,
      buildState.CornerB,
      buildState.CornerC,
      buildState.CornerD,
      position);

    var radius = Mathf.Max(
      0.015f,
      m_sailFurlRollRadius);

    var furlAmount =
      1f - position;

    /*
     * As progressively more cloth is furled, increase the number of visual
     * wraps. This intentionally compresses fabric length slightly rather than
     * forcing a physically exact many-turn spiral; the latter aliases badly
     * with a practical number of skinning rows and can destabilize Magica.
     */
    var activeTurns =
      Mathf.Max(0.15f, m_sailFurlTurns) *
      Mathf.SmoothStep(0f, 1f, furlAmount);

    var maximumAngle =
      activeTurns * Mathf.PI * 2f;

    for (var i = 0;
         i < _sailRigBoneCount;
         i++)
    {
      var bone = _sailRigBones[i];

      if (!bone)
      {
        continue;
      }

      var t = _sailRigBoneCount <= 1
        ? 0f
        : (float)i / (_sailRigBoneCount - 1);

      var bindCenter = GetSailCenterAtT(
        buildState.CornerCount,
        buildState.CornerA,
        buildState.CornerB,
        buildState.CornerC,
        buildState.CornerD,
        t);

      /*
       * Rows above the current furl boundary remain exactly in their bind pose.
       * Rows below it are consumed into the roll. At position=0 the boundary is
       * the top edge, so the entire sail is rolled there instead of translated
       * upward as one flat sheet.
       */
      if (position >= 0.9999f ||
          t <= position)
      {
        bone.localPosition = bindCenter;
        bone.localRotation = Quaternion.identity;
        bone.localScale = Vector3.one;
        continue;
      }

      var denominator =
        Mathf.Max(0.0001f, 1f - position);

      var rolledT =
        Mathf.Clamp01(
          (t - position) / denominator);

      var angle =
        maximumAngle * rolledT;

      var cos = Mathf.Cos(angle);
      var sin = Mathf.Sin(angle);

      /*
       * Circle whose first point is exactly rollStart and whose initial tangent
       * points down the sail. The circle center sits one radius behind the
       * cloth, producing a compact rolled bundle around the horizontal span.
       */
      bone.localPosition =
        rollStart +
        surfaceNormal * radius * (cos - 1f) +
        downDirection * radius * sin;

      var tangent =
        downDirection * cos -
        surfaceNormal * sin;

      if (tangent.sqrMagnitude > Mathf.Epsilon)
      {
        bone.localRotation =
          Quaternion.FromToRotation(
            downDirection,
            tangent.normalized);
      }
      else
      {
        bone.localRotation =
          Quaternion.identity;
      }

      bone.localScale = Vector3.one;
    }
  }

  private void OnSailClothBuildComplete(
    MagicaCloth cloth,
    SailClothBuildState expectedState,
    bool success)
  {
    _clothBuildInProgress = false;

    /*
     * Ignore callbacks from a cloth that was superseded/destroyed while its
     * asynchronous construction was still in flight.
     */
    if (!cloth ||
        cloth != m_sailCloth ||
        !_hasActiveClothBuildState ||
        !_activeClothBuildState.Equals(expectedState))
    {
      return;
    }

    if (!success)
    {
      LoggerProvider.LogError(
        $"MagicaCloth asynchronous build failed for '{name}'.");

      _hasActiveClothBuildState = false;

      if (m_mastComponent &&
          m_mastComponent.m_sailCloth == cloth)
      {
        m_mastComponent.m_sailCloth = null;
      }

      m_sailCloth = null;
      Destroy(cloth);
      return;
    }

    if (m_mastComponent)
    {
      m_mastComponent.m_sailCloth = cloth;
      m_mastComponent.m_customSailComponent = this;
    }

    ApplySailRigPosition(
      _requestedSailPosition);

    SyncSailClothEnabledState();
  }

  private void SyncSailClothEnabledState()
  {
    if (!m_sailCloth)
    {
      return;
    }

    m_sailCloth.enabled =
      !m_sailFlags.HasFlag(
        SailFlags.DisableCloth);
  }

  private void ReleasePendingSailMeshes()
  {
    if (_pendingSailMesh)
    {
      Destroy(_pendingSailMesh);
      _pendingSailMesh = null;
    }

    ReleaseRebuildSailMesh();

    _hasPendingClothBuildState = false;
    _hasRebuildClothBuildState = false;
  }

  private void ReleaseRebuildSailMesh()
  {
    if (_rebuildSailMesh)
    {
      Destroy(_rebuildSailMesh);
      _rebuildSailMesh = null;
    }

    _hasRebuildClothBuildState = false;
  }

  private void ReleaseGeneratedMeshes()
  {
    if (_generatedSailMesh)
    {
      Destroy(_generatedSailMesh);
      _generatedSailMesh = null;
    }

    if (_generatedCollisionMesh)
    {
      Destroy(_generatedCollisionMesh);
      _generatedCollisionMesh = null;
    }

    _hasActiveClothBuildState = false;
    _clothBuildInProgress = false;
    _sailRigBoneCount = 0;
    _hasSailRigBuildState = false;
    _sailAttributeReferenceVertices = Array.Empty<Vector3>();
  }

  public float GetSailArea()
  {
    if (m_sailArea == 0f) UpdateSailArea();

    return m_sailArea;
  }

  private void UpdateSailArea()
  {
    if (m_sailCorners.Count is not (3 or 4))
    {
      Logger.LogError(
        $"CalculateSailArea exited due to not enough vertices provided, max vertices should be 3 or 4, got {m_sailCorners.Count}");
      return;
    }


    var surfaceAreaInForwardDirection = Area(m_sailCorners);
    m_sailArea = surfaceAreaInForwardDirection;

    Logger.LogDebug($"SailComponent UpdateSailArea: {m_sailArea}");
  }

  public float Area(List<Vector3> vertices)
  {
    var result = Vector3.zero;
    for (int p = vertices.Count - 1, q = 0; q < vertices.Count; p = q++)
      result += Vector3.Cross(vertices[q], vertices[p]);

    result *= 0.5f;
    return result.magnitude;
  }

  /**
   * mesh area may still be useful.
   */
  private float CalculateFacingArea(Mesh mesh, Vector3 direction)
  {
    direction = direction.normalized;
    var triangles = mesh.triangles;
    var vertices = mesh.vertices;

    var sum = 0.0;

    for (var i = 0; i < triangles.Length; i += 3)
    {
      var corner = vertices[triangles[i]];
      var a = vertices[triangles[i + 1]] - corner;
      var b = vertices[triangles[i + 2]] - corner;

      var projection = Vector3.Dot(Vector3.Cross(b, a), direction);
      if (projection > 0f)
        sum += projection;
    }

    return (float)(sum / 2.0);
  }

  public void UpdateCoefficients()
  {
    /*
     * Retained for compatibility with existing callers.
     *
     * Magica vertex attributes are construction data. Once BuildAndRun() has
     * started they cannot be safely replaced in-place; CreateSailMesh() owns
     * rebuilds and will replace the MagicaCloth component when required.
     */
    if (_hasActiveClothBuildState)
    {
      LoggerProvider.LogDebug(
        "UpdateCoefficients(): Cloth is already built. Use CreateSailMesh() to rebuild changed construction data.");

      return;
    }

    if (!m_sailCloth || !_generatedSailMesh)
    {
      LoggerProvider.LogWarning(
        "UpdateCoefficients(): Missing MagicaCloth or generated sail mesh.");

      return;
    }

    if (m_sailCorners.Count is not (3 or 4))
    {
      LoggerProvider.LogWarning(
        $"UpdateCoefficients(): Expected 3 or 4 sail corners, got {m_sailCorners.Count}.");

      return;
    }

    ConfigureSailClothForMesh(
      m_sailCloth,
      _generatedSailMesh);

    UpdateMeshCollider();
  }

  private void UpdateMeshCollider()
  {
    if (!m_meshCollider)
    {
      return;
    }

    if (!Config_AllowMeshCollision)
    {
      m_meshCollider.enabled = false;
      m_meshCollider.sharedMesh = null;

      if (_generatedCollisionMesh)
      {
        Destroy(_generatedCollisionMesh);
        _generatedCollisionMesh = null;
      }

      return;
    }

    if (m_sailCorners.Count is not (3 or 4))
    {
      m_meshCollider.enabled = false;
      m_meshCollider.sharedMesh = null;

      if (_generatedCollisionMesh)
      {
        Destroy(_generatedCollisionMesh);
        _generatedCollisionMesh = null;
      }

      return;
    }

    var collisionMesh =
      CreateCollisionMesh(
        m_sailCorners.Count);

    if (!collisionMesh)
    {
      LoggerProvider.LogWarning(
        $"UpdateMeshCollider(): Failed to create collision mesh for {m_sailCorners.Count} sail corners.");

      m_meshCollider.enabled = false;
      m_meshCollider.sharedMesh = null;
      return;
    }

    m_meshCollider.enabled = false;
    m_meshCollider.sharedMesh = null;

    if (_generatedCollisionMesh)
    {
      Destroy(_generatedCollisionMesh);
    }

    _generatedCollisionMesh = collisionMesh;

    m_meshCollider.sharedMesh =
      _generatedCollisionMesh;

    // Required for a MeshCollider attached beneath a moving Rigidbody.
    m_meshCollider.convex = true;
    m_meshCollider.enabled = true;
  }

  private VertexAttribute[] BuildSailVertexAttributes(Mesh mesh)
  {
    var vertices = mesh.vertices;
    var attributes = new VertexAttribute[vertices.Length];

    // Equivalent to the old:
    //
    // coefficients[i].maxDistance = float.MaxValue;
    //
    // Everything moves unless explicitly pinned.
    for (var i = 0; i < attributes.Length; i++)
    {
      attributes[i] = VertexAttribute.Move;
    }

    var lockedCorners = m_lockedSailCorners;
    var lockedSides = m_lockedSailSides;

    // D does not exist on triangular sails.
    if (m_sailCorners.Count == 3)
    {
      lockedCorners &= ~SailLockedSide.D;
      lockedSides &= ~SailLockedSide.D;

      if (lockedCorners == SailLockedSide.None &&
          lockedSides == SailLockedSide.None)
      {
        lockedCorners =
          SailLockedSide.A |
          SailLockedSide.B |
          SailLockedSide.C;

        lockedSides =
          SailLockedSide.A |
          SailLockedSide.B |
          SailLockedSide.C;
      }
    }
    else
    {
      // Preserve your current default behavior without mutating the
      // persistent ZDO-backed fields from this method.
      if (lockedCorners == SailLockedSide.None &&
          lockedSides == SailLockedSide.None)
      {
        lockedCorners = SailLockedSide.Everything;
        lockedSides = SailLockedSide.Everything;
      }
    }

    /*
     * V7 uses one physical vertex set for both front and back submeshes.
     * The reference array therefore maps one-to-one with the Magica source
     * vertices and is completely independent of which side is being rendered.
     */
    var hasReferenceVertices =
      _sailAttributeReferenceVertices.Length == vertices.Length;

    for (var i = 0; i < vertices.Length; i++)
    {
      var attributeReferenceVertex = hasReferenceVertices
        ? _sailAttributeReferenceVertices[i]
        : vertices[i];

      if (ShouldPinVertex(
            attributeReferenceVertex,
            lockedCorners,
            lockedSides))
      {
        attributes[i] = VertexAttribute.Fixed;
      }
    }

    return attributes;
  }

  private bool ShouldPinVertex(
    Vector3 vertex,
    SailLockedSide lockedCorners,
    SailLockedSide lockedSides)
  {
    var cornerCount = m_sailCorners.Count;

    if (cornerCount < 3)
      return false;

    //
    // Corners
    //

    if (lockedCorners.HasFlag(SailLockedSide.A) &&
        ApproximatelySamePoint(vertex, m_sailCorners[0]))
    {
      return true;
    }

    if (lockedCorners.HasFlag(SailLockedSide.B) &&
        ApproximatelySamePoint(vertex, m_sailCorners[1]))
    {
      return true;
    }

    if (lockedCorners.HasFlag(SailLockedSide.C) &&
        ApproximatelySamePoint(vertex, m_sailCorners[2]))
    {
      return true;
    }

    if (cornerCount == 4 &&
        lockedCorners.HasFlag(SailLockedSide.D) &&
        ApproximatelySamePoint(vertex, m_sailCorners[3]))
    {
      return true;
    }

    //
    // Edges
    //
    // A = corner 0 -> 1
    // B = corner 1 -> 2
    // C = corner 2 -> (3 for quad, 0 for triangle)
    // D = corner 3 -> 0
    //

    if (lockedSides.HasFlag(SailLockedSide.A) &&
        IsPointOnSegment(
          vertex,
          m_sailCorners[0],
          m_sailCorners[1]))
    {
      return true;
    }

    if (lockedSides.HasFlag(SailLockedSide.B) &&
        IsPointOnSegment(
          vertex,
          m_sailCorners[1],
          m_sailCorners[2]))
    {
      return true;
    }

    if (lockedSides.HasFlag(SailLockedSide.C))
    {
      var cEnd = cornerCount == 4
        ? m_sailCorners[3]
        : m_sailCorners[0];

      if (IsPointOnSegment(
            vertex,
            m_sailCorners[2],
            cEnd))
      {
        return true;
      }
    }

    if (cornerCount == 4 &&
        lockedSides.HasFlag(SailLockedSide.D) &&
        IsPointOnSegment(
          vertex,
          m_sailCorners[3],
          m_sailCorners[0]))
    {
      return true;
    }

    return false;
  }

  private static bool ApproximatelySamePoint(Vector3 a, Vector3 b)
  {
    return (a - b).sqrMagnitude <=
           SailPinTolerance * SailPinTolerance;
  }

  private static bool IsPointOnSegment(
    Vector3 point,
    Vector3 start,
    Vector3 end)
  {
    var segment = end - start;
    var segmentLengthSqr = segment.sqrMagnitude;

    if (segmentLengthSqr <= Mathf.Epsilon)
    {
      return ApproximatelySamePoint(point, start);
    }

    var t = Vector3.Dot(point - start, segment) /
            segmentLengthSqr;

    // Must actually be between the two corners.
    if (t < 0f || t > 1f)
      return false;

    var closestPoint = start + segment * t;

    return (point - closestPoint).sqrMagnitude <=
           SailPinTolerance * SailPinTolerance;
  }

  public bool IsNotCustom => m_materialVariant != MaterialVariant.Custom;

  public void SetPatternScale(Vector2 vector2)
  {
    if (IsNotCustom) return;
    if (!(m_patternScale == vector2))
    {
      m_patternScale = vector2;
      ApplyToSailMaterials(material => material.SetTextureScale(PatternTex, m_patternScale));
    }
  }

  public void SetPatternOffset(Vector2 vector2)
  {
    if (IsNotCustom) return;
    if (!(m_patternOffset == vector2))
    {
      m_patternOffset = vector2;
      ApplyToSailMaterials(material => material.SetTextureOffset(PatternTex, m_patternOffset));
    }
  }

  public void SetPatternColor(Color color)
  {
    if (IsNotCustom) return;
    m_patternColor = color;
    ApplyToSailMaterials(material => material.SetColor(PatternColor, color));
  }

  public void SetPatternRotation(float rotation)
  {
    if (IsNotCustom) return;
    m_patternRotation = rotation;
    ApplyToSailMaterials(material => material.SetFloat(PatternRotation, rotation));
  }

  public void SetPattern(int hash)
  {
    if (IsNotCustom) return;

    m_patternHash = hash;

    var patternGroup =
      CustomTextureGroup.Get("Patterns");

    if (patternGroup == null)
    {
      LoggerProvider.LogWarning(
        $"SetPattern(): Patterns texture group is not initialized. hash={hash}.");

      return;
    }

    var customtexture =
      patternGroup.GetTextureByHash(hash);

    if (customtexture != null &&
        (bool)customtexture.Texture &&
        (bool)m_mesh)
    {
      ApplyToSailMaterials(
        material => material.SetTexture(
          PatternTex,
          customtexture.Texture));

      if ((bool)customtexture.Normal)
      {
        ApplyToSailMaterials(
          material => material.SetTexture(
            PatternNormal,
            customtexture.Normal));
      }
    }
  }

  public void SetMainScale(Vector2 vector2)
  {
    m_mainScale = vector2;
    ApplyToSailMaterials(material => material.SetTextureScale(MainTex, m_mainScale));
  }

  public void SetMainOffset(Vector2 vector2)
  {
    m_mainOffset = vector2;
    ApplyToSailMaterials(material => material.SetTextureOffset(MainTex, m_mainOffset));
  }

  public void SetMainColor(Color color)
  {
    m_mainColor = color;
    ApplyToSailMaterials(material => material.SetColor(MainColor, color));
  }

  public void SetMain(int hash)
  {
    if (m_materialVariant != MaterialVariant.Custom) return;

    m_mainHash = hash;

    var sailCustomGroup =
      CustomTextureGroup.Get("Sails");

    if (sailCustomGroup == null)
    {
      LoggerProvider.LogDebug(
        "SetMain(): Sails texture group is not initialized. Textures cannot be resolved yet.");

      return;
    }

    var textureGroupFromHash =
      sailCustomGroup.GetTextureByHash(hash);

    if (textureGroupFromHash == null)
    {
      LoggerProvider.LogWarning(
        $"SetMain(): Sail texture hash {hash} was not found in the Sails texture group.");

      return;
    }

    var sailTexture = textureGroupFromHash.Texture;
    var sailNormal = textureGroupFromHash.Normal;

    if (!(bool)sailTexture || !(bool)m_mesh)
    {
      return;
    }

    ApplyToSailMaterials(
      material => material.SetTexture(
        MainTex,
        sailTexture));

    if ((bool)sailNormal)
    {
      ApplyToSailMaterials(
        material =>
        {
          // Vanilla sail materials use _BumpMap while VehicleSailShader
          // historically used _MainNormal. Keep both populated so the
          // generated front/back material instances always receive the
          // selected sail normal map.
          if (material.HasProperty(BumpMap))
          {
            material.SetTexture(BumpMap, sailNormal);
          }

          if (material.HasProperty(MainNormal))
          {
            material.SetTexture(MainNormal, sailNormal);
          }
        });
    }
  }

  public void SetLogoScale(Vector2 vector2)
  {
    if (IsNotCustom) return;
    m_logoScale = vector2;
    ApplyToSailMaterials(material => material.SetTextureScale(LogoTex, m_logoScale));
  }

  public void SetLogoOffset(Vector2 vector2)
  {
    if (IsNotCustom) return;
    m_logoOffset = vector2;
    ApplyToSailMaterials(material => material.SetTextureOffset(LogoTex, m_logoOffset));
  }

  public void SetLogoColor(Color color)
  {
    if (IsNotCustom) return;
    m_logoColor = color;
    ApplyToSailMaterials(material => material.SetColor(LogoColor, color));
  }

  public void SetLogoRotation(float rotation)
  {
    if (IsNotCustom) return;
    m_logoRotation = rotation;
    ApplyToSailMaterials(material => material.SetFloat(LogoRotation, rotation));
  }

  public void SetLogo(int hash)
  {
    if (IsNotCustom) return;

    m_logoHash = hash;

    var logoGroup =
      CustomTextureGroup.Get("Logos");

    if (logoGroup == null)
    {
      LoggerProvider.LogWarning(
        $"SetLogo(): Logos texture group is not initialized. hash={hash}.");

      return;
    }

    var customtexture =
      logoGroup.GetTextureByHash(hash);

    if (customtexture != null &&
        (bool)customtexture.Texture &&
        (bool)m_mesh)
    {
      ApplyToSailMaterials(
        material => material.SetTexture(
          LogoTex,
          customtexture.Texture));

      if ((bool)customtexture.Normal)
      {
        ApplyToSailMaterials(
          material => material.SetTexture(
            LogoNormal,
            customtexture.Normal));
      }
    }
  }

  public string GetHoverName()
  {
    return "";
  }

  public float GetHoverOffset()
  {
    return 0f;
  }

  public string GetHoverText()
  {
    return Localization.instance.Localize(
      $"[<color=yellow><b>$KEY_Use</b></color>] $mb_sail_edit \narea ({Math.Round(m_sailArea)})");
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (m_editPanel == null) m_editPanel = new EditSailComponentPanel();

    TryEdit();
    return true;
  }

  public void TryEdit()
  {
    if (!m_nview.IsOwner())
    {
      m_nview.ClaimOwnership();
    }
    else
    {
      CancelInvoke(nameof(TryEdit));
      if (m_editPanel == null) return;
      m_editPanel.ShowPanel(this);
    }
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void SetMaterialVariant(MaterialVariant materialVariant)
  {
    if (m_materialVariant != materialVariant &&
        _variantSailMaterial)
    {
      Destroy(_variantSailMaterial);
      _variantSailMaterial = null;
    }

    m_materialVariant = materialVariant;

    if (!m_mesh)
    {
      return;
    }

    ConfigureSailRenderMaterials();
  }

  internal void StartEdit()
  {
    CancelInvoke(nameof(LoadZDO));
  }

  internal void EndEdit()
  {
    LoadZDO();
  }
  public ZNetView? m_nview
  {
    get;
    set;
  }
  public ZDO? m_zdo
  {
    get;
    set;
  }
}