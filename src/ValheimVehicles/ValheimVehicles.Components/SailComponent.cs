using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Config;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Injections;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Storage.Serialization;
using ValheimVehicles.UI;
using Zolantris.Shared.Debug;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Components;

public class SailComponent : MonoBehaviour, Interactable, Hoverable
{
  [Flags]
  public enum SailFlags
  {
    None = 0,
    AllowSailShrinking = 1,
    DisableCloth = 2,
    AllowSailRotation = 4,
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


  public int m_mainHashRefHash = "m_mainHash".GetStableHashCode();

  public int m_sailCornersCountHash =
    "m_sailCornersCountHash".GetStableHashCode();

  public int m_sailCorner1Hash = "m_sailCorner1Hash".GetStableHashCode();
  public int m_sailCorner2Hash = "m_sailCorner2Hash".GetStableHashCode();
  public int m_sailCorner3Hash = "m_sailCorner3Hash".GetStableHashCode();
  public int m_sailCorner4Hash = "m_sailCorner4Hash".GetStableHashCode();
  public int m_lockedSailSidesHash = "m_lockedSailSides".GetStableHashCode();

  public int m_lockedSailCornersHash =
    "m_lockedSailCorners".GetStableHashCode();

  public int m_mainScaleHash = "m_mainScale".GetStableHashCode();
  public int m_mainOffsetHash = "m_mainOffset".GetStableHashCode();
  public int m_mainColorHash = "m_mainColor".GetStableHashCode();
  public int m_patternScaleHash = "m_patternScale".GetStableHashCode();
  public int m_patternOffsetHash = "m_patternOffset".GetStableHashCode();
  public int m_patternColorHash = "m_patternColor".GetStableHashCode();
  public int m_patternZDOHash = "m_patternHash".GetStableHashCode();
  public int m_patternRotationHash = "m_patternRotation".GetStableHashCode();
  public int m_logoZdoHash = "m_logoHash".GetStableHashCode();
  public int m_logoColorHash = "m_logoColor".GetStableHashCode();
  public int m_logoScaleHash = "m_logoScale".GetStableHashCode();
  public int m_logoRotationHash = "m_logoRotation".GetStableHashCode();
  public int m_logoOffsetHash = "m_logoOffset".GetStableHashCode();
  public int m_sailFlagsHash = "m_sailFlagsHash".GetStableHashCode();
  public int HasInitialized = "HasInitialized".GetStableHashCode();

  private MastComponent m_mastComponent;

  public ZNetView m_nview;

  public SkinnedMeshRenderer m_mesh;

  public MeshCollider m_meshCollider;

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

  public Color m_patternColor;

  public float m_patternRotation;

  public int m_logoHash;

  public Vector2 m_logoScale;

  public Vector2 m_logoOffset;

  public Color m_logoColor;

  public float m_logoRotation;

  public int m_mainHash;

  public Vector2 m_mainScale;

  public Vector2 m_mainOffset;

  public Color m_mainColor;

  public float m_mistAlpha = 1f;

  private float m_sailArea = 0f;
  private static bool DebugBoxCollider = true;
  private static readonly int MistAlpha = Shader.PropertyToID("_MistAlpha");
  private static readonly int MainColor = Shader.PropertyToID("_MainColor");

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

  public void Awake()
  {
    m_sailComponents.Add(this);
    m_mastComponent = GetComponent<MastComponent>();
    m_mastComponent.m_allowSailRotation = false;
    m_sailCloth = GetComponent<Cloth>();
    m_mastComponent.m_sailCloth = m_sailCloth;
    m_mesh = GetComponent<SkinnedMeshRenderer>();
    m_meshCollider = GetComponent<MeshCollider>();
    m_nview = GetComponent<ZNetView>();
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
      Gizmos.matrix = this.transform.localToWorldMatrix;
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
    m_sailCorners = data.SailCorners.Select(corner => corner.ToVector3()).ToList();
    m_lockedSailSides = (SailLockedSide)data.LockedSides;
    m_lockedSailCorners = (SailLockedSide)data.LockedCorners;

    SetMain(data.MainHash);
    SetMainColor(data.MainColor.ToColor());
    SetMainOffset(data.MainOffset.ToVector2());
    SetMainScale(data.MainScale.ToVector2());

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

    SetSailMastSetting(SailFlags.AllowSailShrinking,
      ((SailFlags)data.SailFlags).HasFlag(SailFlags.AllowSailShrinking));

    SetSailMastSetting(SailFlags.DisableCloth,
      ((SailFlags)data.SailFlags).HasFlag(SailFlags.DisableCloth));

    UpdateSailArea();
    CreateSailMesh();
  }


  private bool GetIsInitialized()
  {
    if (!m_nview) return false;
    var zdo = m_nview.GetZDO();
    if (zdo == null) return false;
    var zdoCorners = zdo.GetInt(m_sailCornersCountHash);
    var hasInitialized = zdo.GetBool(HasInitialized);

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
      zdo.Set(HasInitialized, true);
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
    m_mainColor = sailMaterial.GetColor(MainColor);
    m_mistAlpha = 1f;
    var mainTex = sailMaterial.GetTexture(MainTex);

    var sailTexture = LoadValheimRaftAssets.sailTexture;
    if (sailTexture != null) m_mainHash = mainTex.name.GetStableHashCode();

    m_mainScale = sailMaterial.GetTextureScale(MainTex);
    m_mainOffset = sailMaterial.GetTextureOffset(MainTex);

    var patternTex = sailMaterial.GetTexture(PatternTex);
    var patternGroup = CustomTextureGroup.Get("Patterns")
      .GetTextureByHash(patternTex.name.GetStableHashCode());
    if (patternGroup != null)
      m_patternHash = patternTex.name.GetStableHashCode();

    m_patternScale = sailMaterial.GetTextureScale(PatternTex);
    m_patternOffset = sailMaterial.GetTextureOffset(PatternTex);
    m_patternColor = sailMaterial.GetColor(PatternColor);
    m_patternRotation = sailMaterial.GetFloat(PatternRotation);
    var logoTex = sailMaterial.GetTexture(LogoTex);
    var logoGroup = CustomTextureGroup.Get("Logos")
      .GetTextureByHash(logoTex.name.GetStableHashCode());
    if (logoGroup != null) m_logoHash = logoTex.name.GetStableHashCode();

    m_logoScale = sailMaterial.GetTextureScale(LogoTex);
    m_logoOffset = sailMaterial.GetTextureOffset(LogoTex);
    m_logoColor = sailMaterial.GetColor(LogoColor);
    m_logoRotation = sailMaterial.GetFloat(LogoRotation);

    if (VehicleDebugConfig.HasDebugSails.Value)
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
    return m_mesh.material;
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

    var data = StoredSailDataExtensions.LoadFromZDO(zdo, this);
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
    data.ApplyToZDO(zdo, this);
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
      collisionMesh.SetVertices(new Vector3[3]
      {
        m_sailCorners[0],
        m_sailCorners[1],
        m_sailCorners[2]
      });
      collisionMesh.SetTriangles(new int[6] { 0, 1, 2, 0, 2, 1 }, 0);
      collisionMesh.Optimize();
    }

    if (size == 4)
    {
      collisionMesh.SetVertices(new Vector3[4]
      {
        m_sailCorners[0],
        m_sailCorners[1],
        m_sailCorners[2],
        m_sailCorners[3]
      });
      collisionMesh.SetTriangles(new int[12]
      {
        0, 1, 2, 1, 0, 2, 1, 2, 3, 2,
        1, 3
      }, 0);
      collisionMesh.Optimize();
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
    
    if (m_sailCorners.Count == 3)
    {
      vertices.Add(m_sailCorners[0]);
      vertices.Add(m_sailCorners[1]);
      vertices.Add(m_sailCorners[2]);
      triangles.Add(0);
      triangles.Add(1);
      triangles.Add(2);
      uvs.Add(new Vector2
      {
        x = 0f,
        y = 0f
      });
      uvs.Add(new Vector2
      {
        x = 1f,
        y = 0f
      });
      uvs.Add(new Vector2
      {
        x = 1f,
        y = 1f
      });
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
    
    mesh.Optimize();
    mesh.RecalculateNormals();
    mesh.RecalculateTangents();

    if (m_sailCorners.Count == 3)
    {
      var sqrSubDist = m_sailSubdivision * m_sailSubdivision;
      while (true)
      {
        var dist = (mesh.vertices[mesh.triangles[0]] -
                    mesh.vertices[mesh.triangles[1]])
          .sqrMagnitude;
        if (dist < sqrSubDist) break;

        MeshUtils.Subdivide(mesh);
      }
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
    m_meshCollider.sharedMesh = mesh;
  }

  public void SetPatternScale(Vector2 vector2)
  {
    if (!(m_patternScale == vector2))
    {
      m_patternScale = vector2;
      m_mesh.material.SetTextureScale(PatternTex, m_patternScale);
    }
  }

  public void SetPatternOffset(Vector2 vector2)
  {
    if (!(m_patternOffset == vector2))
    {
      m_patternOffset = vector2;
      m_mesh.material.SetTextureOffset(PatternTex, m_patternOffset);
    }
  }

  public void SetPatternColor(Color color)
  {
    m_patternColor = color;
    m_mesh.material.SetColor(PatternColor, color);
  }

  public void SetPatternRotation(float rotation)
  {
    m_patternRotation = rotation;
    m_mesh.material.SetFloat(PatternRotation, rotation);
  }

  public void SetPattern(int hash)
  {
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
    m_mainHash = hash;
    // var customtexture =
    //   CustomTextureGroup.Get("Sails").GetTextureByHash(hash);
    var sailTexture = LoadValheimRaftAssets.sailTexture;
    var sailNormal = LoadValheimRaftAssets.sailTextureNormal;
    if (!(bool)sailTexture) return;
    m_mesh.material.SetTexture(MainTex, sailTexture);
    if ((bool)sailNormal)
      m_mesh.material.SetTexture(BumpMap, sailNormal);
  }

  public void SetLogoScale(Vector2 vector2)
  {
    m_logoScale = vector2;
    m_mesh.material.SetTextureScale(LogoTex, m_logoScale);
  }

  public void SetLogoOffset(Vector2 vector2)
  {
    m_logoOffset = vector2;
    m_mesh.material.SetTextureOffset(LogoTex, m_logoOffset);
  }

  public void SetLogoColor(Color color)
  {
    m_logoColor = color;
    m_mesh.material.SetColor(LogoColor, color);
  }

  public void SetLogoRotation(float rotation)
  {
    m_logoRotation = rotation;
    m_mesh.material.SetFloat(LogoRotation, rotation);
  }

  public void SetLogo(int hash)
  {
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

  internal void StartEdit()
  {
    CancelInvoke(nameof(LoadZDO));
  }

  internal void EndEdit()
  {
    LoadZDO();
  }
}