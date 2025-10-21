using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

  public Cloth m_sailCloth;

  public List<Vector3> m_sailCorners = new();

  public float m_sailSubdivision = 0.5f;

  public static List<SailComponent> m_sailComponents = new();

  public static float m_maxDistanceSqr = 1024f;

  private static EditSailComponentPanel? m_editPanel = null;

  public SailFlags m_sailFlags;

  public float m_windMultiplier = 10f;

  public float m_clothRandomAccelerationFactor = 0.5f;

  public SailLockedSide m_lockedSailSides;

  public SailLockedSide m_lockedSailCorners;

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
    m_sailComponents.Add(this);
    m_mastComponent = GetComponent<MastComponent>();
    m_mastComponent.m_allowSailRotation = false;
    m_sailCloth = GetComponent<Cloth>();
    m_mastComponent.m_sailCloth = m_sailCloth;
    m_mesh = GetComponent<SkinnedMeshRenderer>();
    customMaterial = m_mesh.material;

    m_meshCollider = GetComponent<MeshCollider>();
    m_nview = GetComponent<ZNetView>();

    AddDefaultSailsToTextures();
  }

  public static void AddDefaultSailsToTextures()
  {
    var drakkalMaterial = OverrideMaterial_DrakkalShipSail();
    var vikingMaterial = OverrideMaterial_VikingShipSail();
    var raftShipSailMaterial = OverrideMaterial_RaftShipSail();

    var sailsGroup = CustomTextureGroup.Get("Sails");

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

    if (timer.ElapsedMilliseconds > maxWaitTime)
    {
      LoggerProvider.LogDev("Exiting WaitForInitialization due to timeout");
      yield break;
    }

    RegisterRPC();
    LoadZDO();
  }

  public static Material OverrideMaterial_VikingShipSail()
  {
    return LoadValheimAssets.vikingShipPrefab.transform
      .Find("ship/visual/Mast/Sail").GetComponentInChildren<SkinnedMeshRenderer>().material;
  }

  public static Material OverrideMaterial_DrakkalShipSail()
  {
    return LoadValheimAssets.drakkarPrefab.transform
      .Find("ship/visual/Mast/Sail").GetComponentInChildren<SkinnedMeshRenderer>().material;
  }

  public static Material OverrideMaterial_RaftShipSail()
  {
    return LoadValheimAssets.raftMast.transform
      .Find("Sail").GetComponentInChildren<SkinnedMeshRenderer>().material;
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
    if (!EnvMan.instance || !m_sailCloth) return;
    var vector = EnvMan.instance.GetWindForce();
    m_sailCloth.externalAcceleration = vector * m_windMultiplier;
    m_sailCloth.randomAcceleration =
      vector * (m_windMultiplier * m_clothRandomAccelerationFactor);
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
      StartCoroutine(WaitForInitialization());
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
    m_sailComponents.Remove(this);
  }

  public void OnDisable()
  {
    CancelInvoke();
    StopAllCoroutines();
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
    var zdoCorners = zdo.GetInt(m_sailCornersCountHash);
    var hasInitialized = zdo.GetBool(HasInitializedHash);

    if (!hasInitialized)
    {
      return false;
    }

    // for 4 point sails
    if (zdoCorners == 0 || m_sailCorners.Count != zdoCorners || m_sailCorners.Count == 0)
    {
      var impossibleVector3 = new Vector3(100001f, 100001f, 100001f);

      var corner1 = zdo.GetVec3(m_sailCorner1Hash, impossibleVector3);
      var corner2 = zdo.GetVec3(m_sailCorner2Hash, impossibleVector3);
      var corner3 = zdo.GetVec3(m_sailCorner3Hash, impossibleVector3);
      var corner4 = zdo.GetVec3(m_sailCorner4Hash, impossibleVector3);

      if (corner1 != impossibleVector3 && corner2 != impossibleVector3 && corner3 != impossibleVector3 && corner4 != impossibleVector3)
      {
        m_sailCorners.Clear();
        m_sailCorners.AddRange([corner1, corner2, corner3, corner4]);
        zdo.Set(m_sailCornersCountHash, 4);
        zdoCorners = 4;
      }
    }

    if (m_sailCorners.Count != 3 &&
        m_sailCorners.Count != 4)
    {
      return false;
    }

    if (zdoCorners is 3 or 4)
    {
      zdo.Set(HasInitializedHash, true);
    }
    else
    {
      return false;
    }

    return true;
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
    var sailMaterial = GetSailMaterial();
    m_mainColor = sailMaterial.GetColor(m_materialVariant == MaterialVariant.Custom ? MainColor : VegetationColor);
    m_mistAlpha = 1f;
    var mainTex = sailMaterial.GetTexture(MainTex);

    if (mainTex != null)
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

    // do not set incompatible shader values.
    if (m_materialVariant != MaterialVariant.Custom) return;

    var patternTex = sailMaterial.GetTexture(PatternTex);
    var patternGroup = CustomTextureGroup.Get("Patterns")?
      .GetTextureByHash(patternTex.name.GetStableHashCode());
    if (patternGroup != null)
    {
      m_patternHash = patternTex.name.GetStableHashCode();
    }
    else
    {
      LoggerProvider.LogWarning("Error pattern group not found. Ensure you have the assets folder under this mod.");
    }

    m_patternScale = sailMaterial.GetTextureScale(PatternTex);
    m_patternOffset = sailMaterial.GetTextureOffset(PatternTex);
    m_patternColor = sailMaterial.GetColor(PatternColor);
    m_patternRotation = sailMaterial.GetFloat(PatternRotation);
    var logoTex = sailMaterial.GetTexture(LogoTex);
    var logoGroup = CustomTextureGroup.Get("Logos")?
      .GetTextureByHash(logoTex.name.GetStableHashCode());
    if (logoGroup != null)
    {
      m_logoHash = logoTex.name.GetStableHashCode();
    }
    else
    {
      LoggerProvider.LogWarning("Error pattern group not found. Ensure you have the assets folder under this mod.");
    }


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

  public Material GetSailMaterial()
  {
    switch (m_materialVariant)
    {
      case MaterialVariant.Custom:
        return customMaterial;
      case MaterialVariant.Karve:
        return OverrideMaterial_VikingShipSail();
      case MaterialVariant.Drakkal:
        return OverrideMaterial_DrakkalShipSail();
      case MaterialVariant.Raft:
        return OverrideMaterial_RaftShipSail();
      default:
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
    m_nview.InvokeRPC(nameof(RPC_SyncSailData));
  }

  public void RPC_SyncSailData(long sender)
  {
    if (!isActiveAndEnabled) return;
    if (LoadZDOCoroutine != null) return;
    LoadZDOCoroutine = StartCoroutine(Debounce_LoadZDO());
  }

  private Coroutine? LoadZDOCoroutine = null;

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
    LoadZDOCoroutine = null;
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
    m_sailCloth.enabled = false;
    if (m_sailCorners.Count < 3) return;

    var vertices = new List<Vector3>();
    var uvs = new List<Vector2>();
    var triangles = new List<int>();

    // if (m_sailCorners.Count == 3)
    // {
    //   vertices.Add(m_sailCorners[0]);
    //   vertices.Add(m_sailCorners[1]);
    //   vertices.Add(m_sailCorners[2]);
    //   triangles.Add(0);
    //   triangles.Add(1);
    //   triangles.Add(2);
    //   uvs.Add(new Vector2
    //   {
    //     x = 0f,
    //     y = 0f
    //   });
    //   uvs.Add(new Vector2
    //   {
    //     x = 1f,
    //     y = 0f
    //   });
    //   uvs.Add(new Vector2
    //   {
    //     x = 1f,
    //     y = 1f
    //   });
    // }
    if (m_sailCorners.Count == 3)
    {
      // Add vertices in clockwise order for proper normal calculation
      vertices.Add(m_sailCorners[0]);
      vertices.Add(m_sailCorners[1]);
      vertices.Add(m_sailCorners[2]);

      // Add front face triangle
      triangles.Add(0);
      triangles.Add(1);
      triangles.Add(2);

      // Use proper UV mapping that covers the full texture space
      uvs.Add(new Vector2(0f, 0f)); // Bottom-left
      uvs.Add(new Vector2(1f, 0f)); // Bottom-right
      uvs.Add(new Vector2(0.5f, 1f)); // Top-center
    }
    else if (m_sailCorners.Count == 4)
    {
      var dx = (m_sailCorners[1] - m_sailCorners[0]).magnitude;
      var dy = (m_sailCorners[2] - m_sailCorners[0]).magnitude;
      var dxs = Mathf.Round(dx / m_sailSubdivision);
      var dys = Mathf.Round(dy / m_sailSubdivision);
      for (var x2 = 0; (float)x2 <= dxs; x2++)
      for (var y2 = 0; (float)y2 <= dys; y2++)
      {
        var xs1 = Vector3.Lerp(m_sailCorners[0], m_sailCorners[1],
          (float)x2 / dxs);
        var xs2 = Vector3.Lerp(m_sailCorners[3], m_sailCorners[2],
          (float)x2 / dxs);
        var ys1 = Vector3.Lerp(xs1, xs2, (float)y2 / dys);
        vertices.Add(ys1);
        uvs.Add(new Vector2
        {
          x = (float)x2 / dxs,
          y = (float)y2 / dys
        });
      }

      dxs += 1f;
      dys += 1f;
      for (var x = 0; (float)x < dxs - 1f; x++)
      for (var y = 0; (float)y < dys - 1f; y++)
      {
        triangles.Add((int)(dys * (float)x + (float)y) + 1);
        triangles.Add((int)(dys * (float)x + (float)y));
        triangles.Add((int)(dys * (float)x + (float)y) + (int)dys);
        triangles.Add((int)(dys * (float)x + (float)y) + 1);
        triangles.Add((int)(dys * (float)x + (float)y) + (int)dys);
        triangles.Add((int)(dys * (float)x + (float)y) + (int)dys + 1);
      }
    }

    var mesh = new Mesh();
    mesh.SetVertices(vertices);
    mesh.SetTriangles(triangles, 0);
    mesh.SetUVs(0, uvs);

    // mesh.Optimize();
    mesh.RecalculateNormals();
    mesh.RecalculateTangents();

    if (m_sailCorners.Count == 3)
    {
      var sqrSubDist = m_sailSubdivision * m_sailSubdivision;
      var subdivisionCount = 0;
      var maxSubdivisions = 3; // Adjust based on your needs

      while (subdivisionCount < maxSubdivisions)
      {
        var dist = (mesh.vertices[mesh.triangles[0]] -
                    mesh.vertices[mesh.triangles[1]])
          .sqrMagnitude;

        if (dist < sqrSubDist) break;

        MeshUtils.Subdivide(mesh);
        subdivisionCount++;
      }

      mesh.RecalculateNormals();
      mesh.RecalculateTangents();
    }


    m_mesh.sharedMesh = mesh;

    // todo see if the collision mesh can be fixed as it probably is more performant
    UpdateCoefficients();
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
    m_sailCloth.enabled = !m_sailFlags.HasFlag(SailFlags.DisableCloth);

    UpdateSailArea();

    var mesh = m_mesh.sharedMesh;
    var coefficients = mesh.vertexCount == m_sailCloth.coefficients.Length
      ? m_sailCloth.coefficients
      : new ClothSkinningCoefficient[mesh.vertexCount];
    for (var i = 0; i < coefficients.Length; i++)
    {
      coefficients[i].maxDistance = float.MaxValue;
      coefficients[i].collisionSphereDistance = float.MaxValue;
    }

    if (m_sailCorners.Count == 3)
    {
      m_lockedSailCorners &= ~SailLockedSide.D;
      m_lockedSailSides &= ~SailLockedSide.D;
      if (m_lockedSailCorners == SailLockedSide.None &&
          m_lockedSailSides == SailLockedSide.None)
      {
        m_lockedSailCorners = SailLockedSide.Everything;
        m_lockedSailSides = SailLockedSide.Everything;
      }

      var sideA2 = (m_sailCorners[0] - m_sailCorners[1]).normalized;
      var sideB2 = (m_sailCorners[1] - m_sailCorners[2]).normalized;
      var sideC2 = (m_sailCorners[2] - m_sailCorners[0]).normalized;
      for (var k = 0; k < mesh.vertices.Length; k++)
      {
        if (m_lockedSailCorners.HasFlag(SailLockedSide.A) &&
            mesh.vertices[k] == m_sailCorners[0])
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.B) &&
            mesh.vertices[k] == m_sailCorners[1])
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.C) &&
            mesh.vertices[k] == m_sailCorners[2])
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.A) &&
            Mathf.Abs(Vector3.Dot(
              (m_sailCorners[0] - mesh.vertices[k]).normalized, sideA2)) >=
            0.9999f)
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.B) &&
            Mathf.Abs(Vector3.Dot(
              (m_sailCorners[1] - mesh.vertices[k]).normalized, sideB2)) >=
            0.9999f)
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.C) &&
            Mathf.Abs(Vector3.Dot(
              (m_sailCorners[2] - mesh.vertices[k]).normalized, sideC2)) >=
            0.9999f)
          coefficients[k].maxDistance = 0f;
      }
    }
    else if (m_sailCorners.Count == 4)
    {
      if (m_lockedSailCorners == SailLockedSide.None &&
          m_lockedSailSides == SailLockedSide.None)
      {
        m_lockedSailCorners = SailLockedSide.Everything;
        m_lockedSailSides = SailLockedSide.Everything;
      }

      var sideA = (m_sailCorners[0] - m_sailCorners[1]).normalized;
      var sideB = (m_sailCorners[1] - m_sailCorners[2]).normalized;
      var sideC = (m_sailCorners[2] - m_sailCorners[3]).normalized;
      var sideD = (m_sailCorners[3] - m_sailCorners[0]).normalized;
      for (var j = 0; j < mesh.vertices.Length; j++)
      {
        if (m_lockedSailCorners.HasFlag(SailLockedSide.A) &&
            mesh.vertices[j] == m_sailCorners[0])
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.B) &&
            mesh.vertices[j] == m_sailCorners[1])
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.C) &&
            mesh.vertices[j] == m_sailCorners[2])
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.D) &&
            mesh.vertices[j] == m_sailCorners[3])
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.A) &&
            Mathf.Abs(Vector3.Dot(
              (m_sailCorners[0] - mesh.vertices[j]).normalized, sideA)) >=
            0.9999f)
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.B) &&
            Mathf.Abs(Vector3.Dot(
              (m_sailCorners[1] - mesh.vertices[j]).normalized, sideB)) >=
            0.9999f)
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.C) &&
            Mathf.Abs(Vector3.Dot(
              (m_sailCorners[2] - mesh.vertices[j]).normalized, sideC)) >=
            0.9999f)
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.D) &&
            Mathf.Abs(Vector3.Dot(
              (m_sailCorners[3] - mesh.vertices[j]).normalized, sideD)) >=
            0.9999f)
          coefficients[j].maxDistance = 0f;
      }
    }

    m_sailCloth.coefficients = coefficients;


    if (!Config_AllowMeshCollision)
    {
      if (m_meshCollider)
      {
        m_meshCollider.enabled = false;
      }
    }
    else
    {
      if (m_meshCollider)
      {
        m_meshCollider.sharedMesh = CreateCollisionMesh(m_sailCorners.Count);
        m_meshCollider.convex = true; // required for triangle meshes to interact with physics
        m_meshCollider.enabled = true; // ensure it's not disabled
      }
    }
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
    var customtexture =
      CustomTextureGroup.Get("Patterns").GetTextureByHash(hash);
    if (customtexture != null && (bool)customtexture.Texture && (bool)m_mesh)
    {
      m_mesh.material.SetTexture(PatternTex, customtexture.Texture);
      if ((bool)customtexture.Normal)
        m_mesh.material.SetTexture(PatternNormal, customtexture.Normal);
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
    var sailCustomGroup = CustomTextureGroup.Get("Sails");
    if (sailCustomGroup == null)
    {
      LoggerProvider.LogDebug("sailCustomGroup (from assets) error. Textures not set correctly. This means your sails will be using built-in mod textures only.");
      return;
    }
    var textureGroupFromHash = sailCustomGroup.GetTextureByHash(hash);
    var sailTexture = textureGroupFromHash.Texture;
    var sailNormal = textureGroupFromHash.Normal;
    if (!(bool)sailTexture) return;
    m_mesh.material.SetTexture(MainTex, sailTexture);
    if ((bool)sailNormal)
      m_mesh.material.SetTexture(BumpMap, sailNormal);
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
    var customtexture =
      CustomTextureGroup.Get("Logos").GetTextureByHash(hash);
    if (customtexture != null && (bool)customtexture.Texture && (bool)m_mesh)
    {
      m_mesh.material.SetTexture(LogoTex, customtexture.Texture);
      if ((bool)customtexture.Normal)
        m_mesh.material.SetTexture(LogoNormal, customtexture.Normal);
    }
  }

  public string GetHoverName()
  {
    return "";
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
      if (!IsInvoking(nameof(TryEdit)))
      {
        m_nview.ClaimOwnership();
        InvokeRepeating(nameof(TryEdit), 0.5f, 0.5f);
      }
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
}