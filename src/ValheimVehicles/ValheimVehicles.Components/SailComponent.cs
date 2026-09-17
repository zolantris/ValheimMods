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

  private SailClothBuildState _activeClothBuildState;
  private SailClothBuildState _pendingClothBuildState;
  private SailClothBuildState _rebuildClothBuildState;

  private bool _hasActiveClothBuildState;
  private bool _hasPendingClothBuildState;
  private bool _hasRebuildClothBuildState;

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
    public readonly SailLockedSide LockedCorners;
    public readonly SailLockedSide LockedSides;

    public SailClothBuildState(
      IReadOnlyList<Vector3> corners,
      float subdivision,
      SailLockedSide lockedCorners,
      SailLockedSide lockedSides)
    {
      CornerCount = corners.Count;
      CornerA = corners.Count > 0 ? corners[0] : Vector3.zero;
      CornerB = corners.Count > 1 ? corners[1] : Vector3.zero;
      CornerC = corners.Count > 2 ? corners[2] : Vector3.zero;
      CornerD = corners.Count > 3 ? corners[3] : Vector3.zero;
      Subdivision = subdivision;
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
      /*
       * Custom sails are dynamically generated, so unlike vanilla sails
       * it is valid for us to create/configure our own MagicaCloth.
       */
      m_sailCloth = gameObject.AddComponent<MagicaCloth>();
    }

    if (!m_sailCloth)
    {
      LoggerProvider.LogError(
        $"Unable to create MagicaCloth for sail '{name}'.");

      return false;
    }

    /*
     * Do not call Initialize() here.
     *
     * MagicaCloth2's runtime-construction flow is:
     *   Add component -> configure construction data -> BuildAndRun().
     *
     * DisableAutoBuild() must happen before Start() gets a chance to build the
     * component with the prefab/default mesh.
     */
    m_sailCloth.DisableAutoBuild();

    var serializeData =
      m_sailCloth.SerializeData;

    serializeData.updateMode =
      ClothUpdateMode.UnityPhysics;

    serializeData.clothType =
      ClothProcess.ClothType.MeshCloth;

    serializeData.paintMode =
      ClothSerializeData.PaintMode.Manual;

    return true;
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

    // only updates the sail parent if applicable. This is not related to StoredSailData.
    UpdateSailParent();
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
    CreateSailMesh();
  }

  public IEnumerator WaitForSailParent(int sailParentId)
  {
    GameObject? sailParent = null;
    var timer = Stopwatch.StartNew();
    while (isActiveAndEnabled && sailParent == null && timer.ElapsedMilliseconds < 5000)
    {
      yield return null;
      sailParent = ZdoWatchController.Instance.GetGameObject(sailParentId);
    }

    if (sailParent == null) yield break;

    var parentMastComponent = sailParent.GetComponent<MastComponent>();

    if (!parentMastComponent) yield break;
    if (!parentMastComponent.m_rotationTransform) yield break;

    transform.SetParent(parentMastComponent.m_rotationTransform);
    if (this.IsNetViewValid(out var netView))
    {
      transform.localPosition = netView.GetZDO().GetVec3(SailParentPositionHash, Vector3.zero);
      transform.localRotation = Quaternion.Euler(netView.m_zdo.GetVec3(SailParentRotationHash, transform.localRotation.eulerAngles));
    }
    else
    {
      transform.localPosition = Vector3.zero;
    }
  }

  public void UpdateSailParent()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (sailParentRoutine.IsRunning) return;
    var sailParentId = netView.GetZDO().GetInt(SailParentIdHash);
    if (sailParentId == 0) return;
    sailParentRoutine.Start(WaitForSailParent(sailParentId));
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
      sailMaterial = m_mesh.material;
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
          customMaterial = m_mesh.material;
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

        return m_mesh.material;
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
    m_mesh.material.SetFloat(MistAlpha, Mathf.Clamp(alpha, 0, 1));
  }

  public static int RenderQueueLevel = 3000;

  private void SetMaterialRenderQueue()
  {
    m_mesh.material.renderQueue = RenderQueueLevel;
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
    data.ApplySerializableData(zdo, this);
  }

  /// <summary>
  /// Request for all peers to sync this new data. Does not send the data. This expects the data to arrive within a short period of time.
  /// </summary>
  public void RequestSyncZDOData()
  {
    if (m_nview == null || m_nview.m_zdo == null) return;
    m_nview.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SyncSailData));
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
      SyncSailClothEnabledState();
      return;
    }

    if (_hasPendingClothBuildState &&
        _pendingClothBuildState.Equals(buildState))
    {
      return;
    }

    if (_hasRebuildClothBuildState &&
        _rebuildClothBuildState.Equals(buildState))
    {
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
      // Add vertices in clockwise order for proper normal calculation.
      vertices.Add(m_sailCorners[0]);
      vertices.Add(m_sailCorners[1]);
      vertices.Add(m_sailCorners[2]);

      // Add front face triangle.
      triangles.Add(0);
      triangles.Add(1);
      triangles.Add(2);

      // Use proper UV mapping that covers the full texture space.
      uvs.Add(new Vector2(0f, 0f));
      uvs.Add(new Vector2(1f, 0f));
      uvs.Add(new Vector2(0.5f, 1f));
    }
    else if (m_sailCorners.Count == 4)
    {
      var dx = (m_sailCorners[1] - m_sailCorners[0]).magnitude;
      var dy = (m_sailCorners[2] - m_sailCorners[0]).magnitude;
      var dxs = Mathf.Max(1, Mathf.RoundToInt(dx / m_sailSubdivision));
      var dys = Mathf.Max(1, Mathf.RoundToInt(dy / m_sailSubdivision));

      for (var x = 0; x <= dxs; x++)
      {
        for (var y = 0; y <= dys; y++)
        {
          var xs1 = Vector3.Lerp(
            m_sailCorners[0],
            m_sailCorners[1],
            (float)x / dxs);

          var xs2 = Vector3.Lerp(
            m_sailCorners[3],
            m_sailCorners[2],
            (float)x / dxs);

          var vertex = Vector3.Lerp(
            xs1,
            xs2,
            (float)y / dys);

          vertices.Add(vertex);
          uvs.Add(new Vector2(
            (float)x / dxs,
            (float)y / dys));
        }
      }

      var rowSize = dys + 1;

      for (var x = 0; x < dxs; x++)
      {
        for (var y = 0; y < dys; y++)
        {
          var current = rowSize * x + y;
          var nextColumn = current + rowSize;

          triangles.Add(current + 1);
          triangles.Add(current);
          triangles.Add(nextColumn);

          triangles.Add(current + 1);
          triangles.Add(nextColumn);
          triangles.Add(nextColumn + 1);
        }
      }
    }

    if (vertices.Count == 0 || triangles.Count == 0)
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
      var sqrSubDist = m_sailSubdivision * m_sailSubdivision;
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

    return mesh;
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
    while (_hasPendingClothBuildState && _pendingSailMesh)
    {
      _rebuildSailMesh = _pendingSailMesh;
      _rebuildClothBuildState = _pendingClothBuildState;
      _hasRebuildClothBuildState = true;

      _pendingSailMesh = null;
      _hasPendingClothBuildState = false;

      var oldCloth = m_sailCloth;

      /*
       * The first dynamically-created cloth is still unbuilt and can be used.
       * Once any build has started (or an unexpected auto-build is valid), the
       * component must be destroyed before changing renderer/source topology.
       */
      var mustReplaceCloth =
        oldCloth &&
        (_hasActiveClothBuildState || oldCloth.IsValid());

      if (mustReplaceCloth)
      {
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
         * Destroy() is deferred. Wait until the old MagicaCloth has released
         * its renderer/proxy state before assigning a new source mesh.
         */
        yield return null;

        if (!isActiveAndEnabled)
        {
          ReleaseRebuildSailMesh();
          yield break;
        }

        /*
         * A newer edit arrived while the old cloth was being destroyed.
         * Discard this intermediate mesh and build only the newest request.
         */
        if (_hasPendingClothBuildState && _pendingSailMesh)
        {
          ReleaseRebuildSailMesh();
          continue;
        }
      }

      if (!m_sailCloth)
      {
        m_sailCloth = gameObject.AddComponent<MagicaCloth>();
      }

      if (!m_sailCloth)
      {
        LoggerProvider.LogError(
          $"ProcessSailClothRebuildQueue(): Unable to create MagicaCloth for '{name}'.");

        ReleaseRebuildSailMesh();
        continue;
      }

      var cloth = m_sailCloth;

      cloth.DisableAutoBuild();

      /*
       * Release the previous source mesh only after the old MagicaCloth has
       * been destroyed. Magica may keep references to that topology while it
       * owns the renderer.
       */
      if (_generatedSailMesh &&
          _generatedSailMesh != _rebuildSailMesh)
      {
        Destroy(_generatedSailMesh);
      }

      _generatedSailMesh = _rebuildSailMesh;
      _rebuildSailMesh = null;

      m_mesh.sharedMesh = _generatedSailMesh;

      ConfigureSailClothForMesh(
        cloth,
        _generatedSailMesh);

      UpdateMeshCollider();

      _activeClothBuildState =
        _rebuildClothBuildState;

      _hasActiveClothBuildState = true;
      _hasRebuildClothBuildState = false;

      var expectedCloth = cloth;
      var expectedState = _activeClothBuildState;

      cloth.OnBuildComplete += (completedCloth, success) =>
      {
        OnSailClothBuildComplete(
          completedCloth,
          expectedState,
          success);
      };  

      // Keep the component enabled while Magica performs its asynchronous build.
      // If DisableCloth is set, OnBuildComplete will disable simulation afterward.
      cloth.enabled = true;

      if (!cloth.BuildAndRun())
      {
        LoggerProvider.LogError(
          $"CreateSailMesh(): MagicaCloth BuildAndRun could not start for '{name}'.");

        if (m_sailCloth == cloth)
        {
          m_sailCloth = null;
        }

        if (m_mastComponent &&
            m_mastComponent.m_sailCloth == cloth)
        {
          m_mastComponent.m_sailCloth = null;
        }

        _hasActiveClothBuildState = false;
        Destroy(cloth);

        continue;
      }

      if (m_mastComponent)
      {
        m_mastComponent.m_sailCloth = cloth;
      }

      /*
       * BuildAndRun is asynchronous. OnBuildComplete applies the final enabled
       * state once construction succeeds.
       */
    }

    _hasRebuildClothBuildState = false;
  }

  private void ConfigureSailClothForMesh(
    MagicaCloth cloth,
    Mesh mesh)
  {
    var serializeData = cloth.SerializeData;
    var serializeData2 = cloth.GetSerializeData2();

    serializeData.updateMode =
      ClothUpdateMode.UnityPhysics;

    serializeData.clothType =
      ClothProcess.ClothType.MeshCloth;

    serializeData.paintMode =
      ClothSerializeData.PaintMode.Manual;

    serializeData.sourceRenderers.Clear();
    serializeData.sourceRenderers.Add(m_mesh);

    var attributes = BuildSailVertexAttributes(mesh);

    serializeData2.vertexAttributeList.Clear();
    serializeData2.vertexAttributeList.Add(attributes);
  }

  private void OnSailClothBuildComplete(
    MagicaCloth cloth,
    SailClothBuildState expectedState,
    bool success)
  {
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
    }

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

    for (var i = 0; i < vertices.Length; i++)
    {
      var vertex = vertices[i];

      if (ShouldPinVertex(
            vertex,
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
      m_mesh.material.SetTextureScale(PatternTex, m_patternScale);
    }
  }

  public void SetPatternOffset(Vector2 vector2)
  {
    if (IsNotCustom) return;
    if (!(m_patternOffset == vector2))
    {
      m_patternOffset = vector2;
      m_mesh.material.SetTextureOffset(PatternTex, m_patternOffset);
    }
  }

  public void SetPatternColor(Color color)
  {
    if (IsNotCustom) return;
    m_patternColor = color;
    m_mesh.material.SetColor(PatternColor, color);
  }

  public void SetPatternRotation(float rotation)
  {
    if (IsNotCustom) return;
    m_patternRotation = rotation;
    m_mesh.material.SetFloat(PatternRotation, rotation);
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
      m_mesh.material.SetTexture(
        PatternTex,
        customtexture.Texture);

      if ((bool)customtexture.Normal)
      {
        m_mesh.material.SetTexture(
          PatternNormal,
          customtexture.Normal);
      }
    }
  }

  public void SetMainScale(Vector2 vector2)
  {
    m_mainScale = vector2;
    m_mesh.material.SetTextureScale(MainTex, m_mainScale);
  }

  public void SetMainOffset(Vector2 vector2)
  {
    m_mainOffset = vector2;
    m_mesh.material.SetTextureOffset(MainTex, m_mainOffset);
  }

  public void SetMainColor(Color color)
  {
    m_mainColor = color;
    m_mesh.material.SetColor(MainColor, color);
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

    m_mesh.material.SetTexture(
      MainTex,
      sailTexture);

    if ((bool)sailNormal)
    {
      m_mesh.material.SetTexture(
        BumpMap,
        sailNormal);
    }
  }

  public void SetLogoScale(Vector2 vector2)
  {
    if (IsNotCustom) return;
    m_logoScale = vector2;
    m_mesh.material.SetTextureScale(LogoTex, m_logoScale);
  }

  public void SetLogoOffset(Vector2 vector2)
  {
    if (IsNotCustom) return;
    m_logoOffset = vector2;
    m_mesh.material.SetTextureOffset(LogoTex, m_logoOffset);
  }

  public void SetLogoColor(Color color)
  {
    if (IsNotCustom) return;
    m_logoColor = color;
    m_mesh.material.SetColor(LogoColor, color);
  }

  public void SetLogoRotation(float rotation)
  {
    if (IsNotCustom) return;
    m_logoRotation = rotation;
    m_mesh.material.SetFloat(LogoRotation, rotation);
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
      m_mesh.material.SetTexture(
        LogoTex,
        customtexture.Texture);

      if ((bool)customtexture.Normal)
      {
        m_mesh.material.SetTexture(
          LogoNormal,
          customtexture.Normal);
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
    m_materialVariant = materialVariant;
    m_mesh.material = GetSailMaterial();
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