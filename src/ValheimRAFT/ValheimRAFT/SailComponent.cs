using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ValheimRAFT.UI;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class SailComponent : MonoBehaviour, Interactable, Hoverable
{
  [Flags]
  public enum SailFlags
  {
    None = 0,
    AllowSailShrinking = 1,
    DisableCloth = 2,
    AllowSailRotation = 3,
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


  private int m_mainHashRefHash = "m_mainHash".GetStableHashCode();
  private int m_sailCornersCountHash = "m_sailCornersCountHash".GetStableHashCode();
  private int m_sailCorner1Hash = "m_sailCorner1Hash".GetStableHashCode();
  private int m_sailCorner2Hash = "m_sailCorner2Hash".GetStableHashCode();
  private int m_sailCorner3Hash = "m_sailCorner3Hash".GetStableHashCode();
  private int m_sailCorner4Hash = "m_sailCorner4Hash".GetStableHashCode();
  private int m_lockedSailSidesHash = "m_lockedSailSides".GetStableHashCode();
  private int m_lockedSailCornersHash = "m_lockedSailCorners".GetStableHashCode();
  private int m_mainScaleHash = "m_mainScale".GetStableHashCode();
  private int m_mainOffsetHash = "m_mainOffset".GetStableHashCode();
  private int m_mainColorHash = "m_mainColor".GetStableHashCode();
  private int m_patternScaleHash = "m_patternScale".GetStableHashCode();
  private int m_patternOffsetHash = "m_patternOffset".GetStableHashCode();
  private int m_patternColorHash = "m_patternColor".GetStableHashCode();
  private int m_patternZDOHash = "m_patternHash".GetStableHashCode();
  private int m_patternRotationHash = "m_patternRotation".GetStableHashCode();
  private int m_logoZdoHash = "m_logoHash".GetStableHashCode();
  private int m_logoColorHash = "m_logoColor".GetStableHashCode();
  private int m_logoScaleHash = "m_logoScale".GetStableHashCode();
  private int m_logoRotationHash = "m_logoRotation".GetStableHashCode();
  private int m_logoOffsetHash = "m_logoOffset".GetStableHashCode();
  private int m_sailFlagsHash = "m_sailFlagsHash".GetStableHashCode();

  private MastComponent m_mastComponent;

  public ZNetView m_nview;

  public SkinnedMeshRenderer m_mesh;

  public MeshCollider m_meshCollider;

  public Cloth m_sailCloth;

  public GameObject m_sailObject;

  public List<Vector3> m_sailCorners = new();

  public float m_sailSubdivision = 0.5f;

  public static List<SailComponent> m_sailComponents = new();

  public static float m_maxDistanceSqr = 1024f;

  private static EditSailComponentPanel m_editPanel = null;

  internal static bool m_sailInit = true;

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

  private int m_mainHash;

  private Vector2 m_mainScale;

  private Vector2 m_mainOffset;

  private Color m_mainColor;

  private float m_sailArea = 0f;

  public void Awake()
  {
    m_sailComponents.Add(this);
    m_mastComponent = GetComponent<MastComponent>();
    m_sailObject = transform.Find("Sail").gameObject;
    m_sailCloth = m_sailObject.GetComponent<Cloth>();
    if ((bool)m_sailCloth)
    {
      m_sailCloth.useTethers = false;
      m_sailCloth.useGravity = true;
      m_sailCloth.bendingStiffness = 1f;
    }

    m_mesh = m_sailObject.GetComponent<SkinnedMeshRenderer>();
    m_meshCollider = m_sailObject.GetComponent<MeshCollider>();
    m_nview = GetComponent<ZNetView>();
    if (m_sailInit)
      LoadZDO();
    else if (!ZNetView.m_forceDisableInit) InvokeRepeating(nameof(LoadZDO), 5f, 5f);
  }

  public void Update()
  {
    var vector = EnvMan.instance.GetWindForce();
    m_sailCloth.externalAcceleration = vector * m_windMultiplier;
    m_sailCloth.randomAcceleration = vector * (m_windMultiplier * m_clothRandomAccelerationFactor);
  }

  public void OnDestroy()
  {
    m_sailComponents.Remove(this);
  }

  private void DestroySelfOnError()
  {
    m_nview.m_zdo.Reset();
    Destroy(this);
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
          if ((bool)m_sailCloth && m_sailCloth.enabled != !allow) m_sailCloth.enabled = !allow;
          break;
        case SailFlags.AllowSailRotation:
          m_mastComponent.m_allowSailRotation = allow;
          if (allow == false)
          {
            m_mastComponent.transform.localRotation = new Quaternion();
          }

          break;
        case SailFlags.AllowSailShrinking:
          m_mastComponent.m_allowSailShrinking = allow;
          break;
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
    var mainTex = sailMaterial.GetTexture("_MainTex");
    var mainGroup = CustomTextureGroup.Get("Sails")
      .GetTextureByHash(mainTex.name.GetStableHashCode());
    if (mainGroup != null) m_mainHash = mainTex.name.GetStableHashCode();

    m_mainScale = sailMaterial.GetTextureScale("_MainTex");
    m_mainOffset = sailMaterial.GetTextureOffset("_MainTex");
    m_mainColor = sailMaterial.GetColor("_MainColor");

    var patternTex = sailMaterial.GetTexture("_PatternTex");
    var patternGroup = CustomTextureGroup.Get("Patterns")
      .GetTextureByHash(patternTex.name.GetStableHashCode());
    if (patternGroup != null) m_patternHash = patternTex.name.GetStableHashCode();

    m_patternScale = sailMaterial.GetTextureScale("_PatternTex");
    m_patternOffset = sailMaterial.GetTextureOffset("_PatternTex");
    m_patternColor = sailMaterial.GetColor("_PatternColor");
    m_patternRotation = sailMaterial.GetFloat("_PatternRotation");
    var logoTex = sailMaterial.GetTexture("_LogoTex");
    var logoGroup = CustomTextureGroup.Get("Logos")
      .GetTextureByHash(logoTex.name.GetStableHashCode());
    if (logoGroup != null) m_logoHash = logoTex.name.GetStableHashCode();

    m_logoScale = sailMaterial.GetTextureScale("_LogoTex");
    m_logoOffset = sailMaterial.GetTextureOffset("_LogoTex");
    m_logoColor = sailMaterial.GetColor("_LogoColor");
    m_logoRotation = sailMaterial.GetFloat("_LogoRotation");

    if (ValheimRaftPlugin.Instance.HasDebugSails.Value)
    {
      Logger.LogDebug("AFTER LOAD FROM MATERIAL");
      Logger.LogDebug($"m_lockedSailSides {m_lockedSailSides}");
      Logger.LogDebug($"m_lockedSailCorners {m_lockedSailCorners}");
      Logger.LogDebug($"m_mainScale {m_mainScale}");
      Logger.LogDebug($"m_mainOffset {m_mainOffset}");
      Logger.LogDebug($"m_mainColor {m_mainColor}");
      Logger.LogDebug($"m_mainHash {m_mainHash}");
      Logger.LogDebug($"m_patternScale {m_patternScale}");
      Logger.LogDebug($"m_patternOffset {m_patternOffset}");
      Logger.LogDebug($"m_patternColor {m_patternColor}");
      Logger.LogDebug($"m_patternHash {m_patternHash}");
      Logger.LogDebug($"m_patternRotation {m_patternRotation}");
      Logger.LogDebug($"m_logoScale {m_logoScale}");
      Logger.LogDebug($"m_logoOffset {m_logoOffset}");
      Logger.LogDebug($"m_logoColor {m_logoColor}");
      Logger.LogDebug($"m_logoHash {m_logoHash}");
      Logger.LogDebug($"m_logoRotation {m_logoRotation}");
      Logger.LogDebug($"m_sailFlags {m_sailFlags}");
    }
  }

  public Material GetSailMaterial()
  {
    return m_mesh.material;
  }

  public void LoadZDO()
  {
    if (!m_nview || m_nview.m_zdo == null) return;
    var zdo = m_nview.m_zdo;

    var meshUpdateRequired = false;
    var coefficientUpdateRequired = false;
    var zdo_corners = zdo.GetInt(m_sailCornersCountHash);

    if (zdo_corners == 0 && m_sailCorners.Count != 3 && m_sailCorners.Count != 4)
    {
      Logger.LogError(
        $"SailCornersCount: {m_sailCorners.Count} is corrupt or mismatches the last ValheimRAFT version. Destroying this component to prevent errors");
      DestroySelfOnError();
      return;
    }

    if (m_sailCorners.Count != zdo_corners)
    {
      m_sailCorners.Clear();
      meshUpdateRequired = true;
    }

    for (var i = 0; i < zdo_corners; i++)
    {
      Vector3 sailCorner = default;

      switch (i)
      {
        case 0:
          sailCorner = zdo.GetVec3(m_sailCorner1Hash, sailCorner);
          break;
        case 1:
          sailCorner = zdo.GetVec3(m_sailCorner2Hash, sailCorner);
          break;
        case 2:
          sailCorner = zdo.GetVec3(m_sailCorner3Hash, sailCorner);
          break;
        case 3:
          sailCorner = zdo.GetVec3(m_sailCorner4Hash, sailCorner);
          break;
      }

      if (sailCorner == default)
      {
        Logger.LogError("SailCorner not detected when it should exist");
        continue;
      }

      if (m_sailCorners.Count > i)
      {
        if (m_sailCorners[i] == sailCorner) continue;
        m_sailCorners[i] = sailCorner;
        meshUpdateRequired = true;
      }
      else
      {
        m_sailCorners.Add(sailCorner);
      }
    }

    var zdo_lockedSailSides =
      (SailLockedSide)zdo.GetInt(m_lockedSailCornersHash, (int)m_lockedSailCorners);
    var zdo_lockedSailCorners =
      (SailLockedSide)zdo.GetInt(m_lockedSailSidesHash, (int)m_lockedSailSides);

    var zdo_mainHash = zdo.GetInt(m_mainHashRefHash, m_mainHash);
    var zdo_mainScale = zdo.GetVec3(m_mainScaleHash, m_mainScale);
    var zdo_mainOffset = zdo.GetVec3(m_mainOffsetHash, m_mainOffset);
    var zdo_mainColor = GetColorFromByteStream(zdo.GetByteArray(m_mainColorHash));

    var zdo_patternScale = zdo.GetVec3(m_patternScaleHash, m_patternScale);
    var zdo_patternOffset = zdo.GetVec3(m_patternOffsetHash, m_patternOffset);
    var zdo_patternColor = GetColorFromByteStream(zdo.GetByteArray(m_patternColorHash));
    var zdo_patternHash = zdo.GetInt(m_patternZDOHash, m_patternHash);
    var zdo_patternRotation = zdo.GetFloat(m_patternRotationHash, m_patternRotation);

    var zdo_logoScale = zdo.GetVec3(m_logoScaleHash, m_logoScale);
    var zdo_logoOffset = zdo.GetVec3(m_logoOffsetHash, m_logoOffset);
    var zdo_logoColor = GetColorFromByteStream(zdo.GetByteArray(m_logoColorHash));
    var zdo_logoHash = zdo.GetInt(m_logoZdoHash, m_logoHash);
    var zdo_logoRotation = zdo.GetFloat(m_logoRotationHash, m_logoRotation);
    var zdo_sailFlags = (SailFlags)zdo.GetInt(m_sailFlagsHash, (int)m_sailFlags);

    if (zdo_mainColor != m_mainColor)
      Logger.LogDebug(
        $"ZDO color: {zdo_mainColor} updated on LoadZDO of mesh color {m_mainColor} from sailMaterialColor: {GetSailMaterial().GetColor("_MainColor")}");

    if (ValheimRaftPlugin.Instance.HasDebugSails.Value)
    {
      Logger.LogDebug($"zdo_lockedSailSides {zdo_lockedSailSides}");
      Logger.LogDebug($"zdo_lockedSailCorners {zdo_lockedSailCorners}");
      Logger.LogDebug($"zdo_mainScale {zdo_mainScale}");
      Logger.LogDebug($"zdo_mainOffset {zdo_mainOffset}");
      Logger.LogDebug($"zdo_mainColor {zdo_mainColor}");
      Logger.LogDebug($"zdo_mainHash {zdo_mainHash}");
      Logger.LogDebug($"zdo_patternScale {zdo_patternScale}");
      Logger.LogDebug($"zdo_patternOffset {zdo_patternOffset}");
      Logger.LogDebug($"zdo_patternColor {zdo_patternColor}");
      Logger.LogDebug($"zdo_patternHash {zdo_patternHash}");
      Logger.LogDebug($"zdo_patternRotation {zdo_patternRotation}");
      Logger.LogDebug($"zdo_logoScale {zdo_logoScale}");
      Logger.LogDebug($"zdo_logoOffset {zdo_logoOffset}");
      Logger.LogDebug($"zdo_logoColor {zdo_logoColor}");
      Logger.LogDebug($"zdo_logoHash {zdo_logoHash}");
      Logger.LogDebug($"zdo_logoRotation {zdo_logoRotation}");
      Logger.LogDebug($"zdo_sailFlags {zdo_sailFlags}");
    }

    if (zdo_lockedSailSides != m_lockedSailSides)
    {
      coefficientUpdateRequired = true;
      m_lockedSailSides = zdo_lockedSailSides;
    }

    if (zdo_lockedSailCorners != m_lockedSailCorners)
    {
      coefficientUpdateRequired = true;
      m_lockedSailCorners = zdo_lockedSailCorners;
    }

    SetMain(zdo_mainHash);
    SetMainColor(zdo_mainColor);
    SetMainOffset(zdo_mainOffset);
    SetMainScale(zdo_mainScale);
    SetPattern(zdo_patternHash);
    SetPatternColor(zdo_patternColor);
    SetPatternOffset(zdo_patternOffset);
    SetPatternScale(zdo_patternScale);
    SetPatternRotation(zdo_patternRotation);
    SetLogo(zdo_logoHash);
    SetLogoColor(zdo_logoColor);
    SetLogoOffset(zdo_logoOffset);
    SetLogoScale(zdo_logoScale);
    SetLogoRotation(zdo_logoRotation);


    SetSailMastSetting(SailFlags.AllowSailShrinking,
      zdo_sailFlags.HasFlag(SailFlags.AllowSailShrinking));
    SetSailMastSetting(SailFlags.DisableCloth, zdo_sailFlags.HasFlag(SailFlags.DisableCloth));
    SetSailMastSetting(SailFlags.AllowSailRotation,
      zdo_sailFlags.HasFlag(SailFlags.AllowSailRotation));

    UpdateSailArea();
    Logger.LogDebug($"MeshupdateRequiredStatus: {meshUpdateRequired}");
    if (meshUpdateRequired)
      CreateSailMesh();
    else if (coefficientUpdateRequired) UpdateCoefficients();
  }

  /**
   * Using a single ReadColor per stream is not super efficient but it will eliminate any order dependent issues per ZDO sync
   */
  private Color GetColorFromByteStream(byte[] bytes)
  {
    var stream = new MemoryStream(bytes);
    var reader = new BinaryReader(stream);
    return reader.ReadColor();
  }

  private byte[] ConvertColorToByteStream(Color inputColor)
  {
    var stream = new MemoryStream();
    var writer = new BinaryWriter(stream);

    writer.Write(inputColor);

    return stream.ToArray();
  }

  public void SaveZDO()
  {
    if ((bool)m_nview && m_nview.m_zdo != null)
    {
      if (ValheimRaftPlugin.Instance.HasDebugSails.Value)
      {
        Logger.LogDebug($"m_lockedSailSides {m_lockedSailSides}");
        Logger.LogDebug($"m_lockedSailCorners {m_lockedSailCorners}");
        Logger.LogDebug($"m_mainScale {m_mainScale}");
        Logger.LogDebug($"m_mainOffset {m_mainOffset}");
        Logger.LogDebug($"m_mainColor {m_mainColor}");
        Logger.LogDebug($"m_mainHash {m_mainHash}");
        Logger.LogDebug($"m_patternScale {m_patternScale}");
        Logger.LogDebug($"m_patternOffset {m_patternOffset}");
        Logger.LogDebug($"m_patternColor {m_patternColor}");
        Logger.LogDebug($"m_patternHash {m_patternHash}");
        Logger.LogDebug($"m_patternRotation {m_patternRotation}");
        Logger.LogDebug($"m_logoScale {m_logoScale}");
        Logger.LogDebug($"m_logoOffset {m_logoOffset}");
        Logger.LogDebug($"m_logoColor {m_logoColor}");
        Logger.LogDebug($"m_logoHash {m_logoHash}");
        Logger.LogDebug($"m_logoRotation {m_logoRotation}");
        Logger.LogDebug($"m_sailFlags {m_sailFlags}");
      }


      var zdo = m_nview.GetZDO();

      zdo.Set(m_sailCornersCountHash, m_sailCorners.Count);
      for (var i = 0; i < m_sailCorners.Count; i++)
        switch (i)
        {
          case 0:
            zdo.Set(m_sailCorner1Hash, m_sailCorners[i]);
            break;
          case 1:
            zdo.Set(m_sailCorner2Hash, m_sailCorners[i]);
            break;
          case 2:
            zdo.Set(m_sailCorner3Hash, m_sailCorners[i]);
            break;
          case 3:
            zdo.Set(m_sailCorner4Hash, m_sailCorners[i]);
            break;
        }

      zdo.Set(m_lockedSailSidesHash, (int)m_lockedSailSides);
      zdo.Set(m_lockedSailCornersHash, (int)m_lockedSailCorners);


      var mainColorByteArray = ConvertColorToByteStream(m_mainColor);
      var patternColorByteArray = ConvertColorToByteStream(m_patternColor);
      var logoColorByteArray = ConvertColorToByteStream(m_logoColor);

      zdo.Set(m_mainHashRefHash, m_mainHash);
      zdo.Set(m_mainScaleHash, m_mainScale);
      zdo.Set(m_mainOffsetHash, m_mainOffset);
      zdo.Set(m_mainColorHash, mainColorByteArray);

      zdo.Set(m_patternScaleHash, m_patternScale);
      zdo.Set(m_patternOffsetHash, m_patternOffset);
      zdo.Set(m_patternColorHash, patternColorByteArray);
      zdo.Set(m_patternZDOHash, m_patternHash);
      zdo.Set(m_patternRotationHash, m_patternRotation);

      zdo.Set(m_logoScaleHash, m_logoScale);
      zdo.Set(m_logoOffsetHash, m_logoOffset);
      zdo.Set(m_logoColorHash, logoColorByteArray);
      zdo.Set(m_logoZdoHash, m_logoHash);
      zdo.Set(m_logoRotationHash, m_logoRotation);
      zdo.Set(m_sailFlagsHash, (int)m_sailFlags);
    }
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
    var collisionMesh = new Mesh();
    if (m_sailCorners.Count == 3)
    {
      collisionMesh.SetVertices(new Vector3[3]
      {
        m_sailCorners[0],
        m_sailCorners[1],
        m_sailCorners[2]
      });
      collisionMesh.SetTriangles(new int[6] { 0, 1, 2, 0, 2, 1 }, 0);
      collisionMesh.Optimize();
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
      var dx = (m_sailCorners[1] - m_sailCorners[0]).magnitude;
      var dy = (m_sailCorners[2] - m_sailCorners[0]).magnitude;
      var dxs = Mathf.Round(dx / m_sailSubdivision);
      var dys = Mathf.Round(dy / m_sailSubdivision);
      for (var x2 = 0; (float)x2 <= dxs; x2++)
      for (var y2 = 0; (float)y2 <= dys; y2++)
      {
        var xs1 = Vector3.Lerp(m_sailCorners[0], m_sailCorners[1], (float)x2 / dxs);
        var xs2 = Vector3.Lerp(m_sailCorners[3], m_sailCorners[2], (float)x2 / dxs);
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
    if (m_sailCorners.Count == 3)
    {
      var sqrSubDist = m_sailSubdivision * m_sailSubdivision;
      while (true)
      {
        var dist = (mesh.vertices[mesh.triangles[0]] - mesh.vertices[mesh.triangles[1]])
          .sqrMagnitude;
        if (dist < sqrSubDist) break;

        MeshHelper.Subdivide(mesh);
      }
    }

    mesh.Optimize();
    mesh.RecalculateNormals();
    m_mesh.sharedMesh = mesh;
    m_meshCollider.sharedMesh = collisionMesh;
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
      if (m_lockedSailCorners == SailLockedSide.None && m_lockedSailSides == SailLockedSide.None)
      {
        m_lockedSailCorners = SailLockedSide.Everything;
        m_lockedSailSides = SailLockedSide.Everything;
      }

      var sideA2 = (m_sailCorners[0] - m_sailCorners[1]).normalized;
      var sideB2 = (m_sailCorners[1] - m_sailCorners[2]).normalized;
      var sideC2 = (m_sailCorners[2] - m_sailCorners[0]).normalized;
      for (var k = 0; k < mesh.vertices.Length; k++)
      {
        if (m_lockedSailCorners.HasFlag(SailLockedSide.A) && mesh.vertices[k] == m_sailCorners[0])
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.B) && mesh.vertices[k] == m_sailCorners[1])
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.C) && mesh.vertices[k] == m_sailCorners[2])
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.A) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[0] - mesh.vertices[k]).normalized, sideA2)) >=
            0.9999f)
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.B) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[1] - mesh.vertices[k]).normalized, sideB2)) >=
            0.9999f)
          coefficients[k].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.C) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[2] - mesh.vertices[k]).normalized, sideC2)) >=
            0.9999f)
          coefficients[k].maxDistance = 0f;
      }
    }
    else if (m_sailCorners.Count == 4)
    {
      if (m_lockedSailCorners == SailLockedSide.None && m_lockedSailSides == SailLockedSide.None)
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
        if (m_lockedSailCorners.HasFlag(SailLockedSide.A) && mesh.vertices[j] == m_sailCorners[0])
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.B) && mesh.vertices[j] == m_sailCorners[1])
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.C) && mesh.vertices[j] == m_sailCorners[2])
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailCorners.HasFlag(SailLockedSide.D) && mesh.vertices[j] == m_sailCorners[3])
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.A) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[0] - mesh.vertices[j]).normalized, sideA)) >=
            0.9999f)
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.B) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[1] - mesh.vertices[j]).normalized, sideB)) >=
            0.9999f)
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.C) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[2] - mesh.vertices[j]).normalized, sideC)) >=
            0.9999f)
          coefficients[j].maxDistance = 0f;

        if (m_lockedSailSides.HasFlag(SailLockedSide.D) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[3] - mesh.vertices[j]).normalized, sideD)) >=
            0.9999f)
          coefficients[j].maxDistance = 0f;
      }
    }

    m_sailCloth.coefficients = coefficients;
  }

  public void SetPatternScale(Vector2 vector2)
  {
    if (!(m_patternScale == vector2))
    {
      m_patternScale = vector2;
      m_mesh.material.SetTextureScale("_PatternTex", m_patternScale);
    }
  }

  public void SetPatternOffset(Vector2 vector2)
  {
    if (!(m_patternOffset == vector2))
    {
      m_patternOffset = vector2;
      m_mesh.material.SetTextureOffset("_PatternTex", m_patternOffset);
    }
  }

  public void SetPatternColor(Color color)
  {
    if (!(m_patternColor == color))
    {
      m_patternColor = color;
      m_mesh.material.SetColor("_PatternColor", color);
    }
  }

  public void SetPatternRotation(float rotation)
  {
    if (m_patternRotation != rotation)
    {
      m_patternRotation = rotation;
      m_mesh.material.SetFloat("_PatternRotation", rotation);
    }
  }

  public void SetPattern(int hash)
  {
    if (m_patternHash == hash) return;

    m_patternHash = hash;
    var customtexture =
      CustomTextureGroup.Get("Patterns").GetTextureByHash(hash);
    if (customtexture != null && (bool)customtexture.Texture && (bool)m_mesh)
    {
      m_mesh.material.SetTexture("_PatternTex", customtexture.Texture);
      if ((bool)customtexture.Normal)
        m_mesh.material.SetTexture("_PatternNormal", customtexture.Normal);
    }
  }

  public void SetMainScale(Vector2 vector2)
  {
    if (!(m_mainScale == vector2))
    {
      m_mainScale = vector2;
      m_mesh.material.SetTextureScale("_MainTex", m_mainScale);
    }
  }

  public void SetMainOffset(Vector2 vector2)
  {
    if (!(m_mainOffset == vector2))
    {
      m_mainOffset = vector2;
      m_mesh.material.SetTextureOffset("_MainTex", m_mainOffset);
    }
  }

  public void SetMainColor(Color color)
  {
    if (!(m_mainColor == color))
    {
      m_mainColor = color;
      m_mesh.material.SetColor("_MainColor", color);
    }
  }

  public void SetMain(int hash)
  {
    if (m_mainHash == hash) return;

    m_mainHash = hash;
    var customtexture =
      CustomTextureGroup.Get("Sails").GetTextureByHash(hash);
    if (customtexture != null && (bool)customtexture.Texture && (bool)m_mesh)
    {
      m_mesh.material.SetTexture("_MainTex", customtexture.Texture);
      if ((bool)customtexture.Normal)
        m_mesh.material.SetTexture("_MainNormal", customtexture.Normal);
    }
  }

  public void SetLogoScale(Vector2 vector2)
  {
    if (!(m_logoScale == vector2))
    {
      m_logoScale = vector2;
      m_mesh.material.SetTextureScale("_LogoTex", m_logoScale);
    }
  }

  public void SetLogoOffset(Vector2 vector2)
  {
    if (!(m_logoOffset == vector2))
    {
      m_logoOffset = vector2;
      m_mesh.material.SetTextureOffset("_LogoTex", m_logoOffset);
    }
  }

  public void SetLogoColor(Color color)
  {
    if (!(m_logoColor == color))
    {
      m_logoColor = color;
      m_mesh.material.SetColor("_LogoColor", color);
    }
  }

  public void SetLogoRotation(float rotation)
  {
    if (m_logoRotation != rotation)
    {
      m_logoRotation = rotation;
      m_mesh.material.SetFloat("_LogoRotation", rotation);
    }
  }

  public void SetLogo(int hash)
  {
    if (m_logoHash == hash) return;

    m_logoHash = hash;
    var customtexture =
      CustomTextureGroup.Get("Logos").GetTextureByHash(hash);
    if (customtexture != null && (bool)customtexture.Texture && (bool)m_mesh)
    {
      m_mesh.material.SetTexture("_LogoTex", customtexture.Texture);
      if ((bool)customtexture.Normal)
        m_mesh.material.SetTexture("_LogoNormal", customtexture.Normal);
    }
  }

  private void OnDrawGizmos()
  {
    for (var i = 0; i < m_sailCorners.Count; i++)
      Gizmos.DrawSphere(transform.position + m_sailCorners[i], 0.1f);
  }

  public string GetHoverName()
  {
    return "";
  }

  public string GetHoverText()
  {
    return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $mb_sail_edit");
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
      if (!IsInvoking("TryEdit"))
      {
        m_nview.ClaimOwnership();
        InvokeRepeating("TryEdit", 0.5f, 0.5f);
      }
    }
    else
    {
      CancelInvoke("TryEdit");
      m_editPanel.ShowPanel(this);
    }
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  internal void StartEdit()
  {
    CancelInvoke("LoadZDO");
  }

  internal void EndEdit()
  {
    InvokeRepeating("LoadZDO", 5f, 5f);
  }
}