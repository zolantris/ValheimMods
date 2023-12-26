using System;
using System.Collections.Generic;
using System.IO;
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
    DisableCloth = 2
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

  private MastComponent m_mastComponent;

  public ZNetView m_nview;

  public SkinnedMeshRenderer m_mesh;

  public MeshCollider m_meshCollider;

  public Cloth m_sailCloth;

  public GameObject m_sailObject;

  public List<Vector3> m_sailCorners = new List<Vector3>();

  public float m_sailSubdivision = 0.5f;

  public static List<SailComponent> m_sailComponents = new List<SailComponent>();

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

  private float m_sailArea;

  public void Awake()
  {
    m_sailComponents.Add(this);
    m_mastComponent = GetComponent<MastComponent>();
    m_sailObject = base.transform.Find("Sail").gameObject;
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
    {
      LoadZDO();
    }
    else if (!ZNetView.m_forceDisableInit)
    {
      InvokeRepeating("LoadZDO", 5f, 5f);
    }
  }

  public void Update()
  {
    Vector3 vector = EnvMan.instance.GetWindForce();
    m_sailCloth.externalAcceleration = vector * m_windMultiplier;
    m_sailCloth.randomAcceleration = vector * m_windMultiplier * m_clothRandomAccelerationFactor;
  }

  public void OnDestroy()
  {
    m_sailComponents.Remove(this);
  }

  public void SetAllowSailShrinking(bool allow)
  {
    if ((bool)m_mastComponent)
    {
      m_sailFlags = (allow
        ? (m_sailFlags | SailFlags.AllowSailShrinking)
        : (m_sailFlags & ~SailFlags.AllowSailShrinking));
      m_mastComponent.m_allowSailShrinking = allow;
    }
  }

  public void SetDisableCloth(bool allow)
  {
    if ((bool)m_mastComponent)
    {
      m_sailFlags = (allow
        ? (m_sailFlags | SailFlags.DisableCloth)
        : (m_sailFlags & ~SailFlags.DisableCloth));
      m_mastComponent.m_disableCloth = allow;
      if ((bool)m_sailCloth && m_sailCloth.enabled != !allow)
      {
        m_sailCloth.enabled = !allow;
      }
    }
  }

  public void LoadFromMaterial()
  {
    Material sailMaterial = GetSailMaterial();
    Texture mainTex = sailMaterial.GetTexture("_MainTex");
    CustomTextureGroup.CustomTexture mainGroup = CustomTextureGroup.Get("Sails")
      .GetTextureByHash(mainTex.name.GetStableHashCode());
    if (mainGroup != null)
    {
      m_mainHash = mainTex.name.GetStableHashCode();
    }

    m_mainScale = sailMaterial.GetTextureScale("_MainTex");
    m_mainOffset = sailMaterial.GetTextureOffset("_MainTex");
    m_mainColor = sailMaterial.GetColor("_MainColor");
    Texture patternTex = sailMaterial.GetTexture("_PatternTex");
    CustomTextureGroup.CustomTexture patternGroup = CustomTextureGroup.Get("Patterns")
      .GetTextureByHash(patternTex.name.GetStableHashCode());
    if (patternGroup != null)
    {
      m_patternHash = patternTex.name.GetStableHashCode();
    }

    m_patternScale = sailMaterial.GetTextureScale("_PatternTex");
    m_patternOffset = sailMaterial.GetTextureOffset("_PatternTex");
    m_patternColor = sailMaterial.GetColor("_PatternColor");
    m_patternRotation = sailMaterial.GetFloat("_PatternRotation");
    Texture logoTex = sailMaterial.GetTexture("_LogoTex");
    CustomTextureGroup.CustomTexture logoGroup = CustomTextureGroup.Get("Logos")
      .GetTextureByHash(logoTex.name.GetStableHashCode());
    if (logoGroup != null)
    {
      m_logoHash = logoTex.name.GetStableHashCode();
    }

    m_logoScale = sailMaterial.GetTextureScale("_LogoTex");
    m_logoOffset = sailMaterial.GetTextureOffset("_LogoTex");
    m_logoColor = sailMaterial.GetColor("_LogoColor");
    m_logoRotation = sailMaterial.GetFloat("_LogoRotation");
  }

  public Material GetSailMaterial()
  {
    return m_mesh.material;
  }

  public void LoadZDO()
  {
    if (!m_nview || m_nview.m_zdo == null)
    {
      return;
    }

    byte[] bytes = m_nview.m_zdo.GetByteArray("MB_sailConfig");

    if (bytes == null)
    {
      return;
    }

    MemoryStream stream = new MemoryStream(bytes);
    BinaryReader reader = new BinaryReader(stream);
    byte version = reader.ReadByte();
    if (version != 1)
    {
      return;
    }

    bool meshUpdateRequired = false;
    bool coefficientUpdateRequired = false;
    int zdo_corners = reader.ReadByte();
    if (m_sailCorners.Count != zdo_corners)
    {
      meshUpdateRequired = true;
      m_sailCorners.Clear();
      for (int i = 0; i < zdo_corners; i++)
      {
        m_sailCorners.Add(reader.ReadVector3());
      }
    }
    else
    {
      for (int j = 0; j < zdo_corners; j++)
      {
        Vector3 v = reader.ReadVector3();
        if (m_sailCorners[j] != v)
        {
          m_sailCorners[j] = v;
          meshUpdateRequired = true;
        }
      }
    }

    SailLockedSide zdo_lockedSailSides = (SailLockedSide)reader.ReadByte();
    SailLockedSide zdo_lockedSailCorners = (SailLockedSide)reader.ReadByte();
    Vector2 zdo_mainScale = reader.ReadVector2();
    Vector2 zdo_mainOffset = reader.ReadVector2();
    Color zdo_mainColor = reader.ReadColor();
    int zdo_mainHash = reader.ReadInt32();
    Vector2 zdo_patternScale = reader.ReadVector2();
    Vector2 zdo_patternOffset = reader.ReadVector2();
    Color zdo_patternColor = reader.ReadColor();
    int zdo_patternHash = reader.ReadInt32();
    float zdo_patternRotation = reader.ReadSingle();
    Vector2 zdo_logoScale = reader.ReadVector2();
    Vector2 zdo_logoOffset = reader.ReadVector2();
    Color zdo_logoColor = reader.ReadColor();
    int zdo_logoHash = reader.ReadInt32();
    float zdo_logoRotation = reader.ReadSingle();
    SailFlags zdo_sailFlags = (SailFlags)reader.ReadByte();
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
    SetAllowSailShrinking(zdo_sailFlags.HasFlag(SailFlags.AllowSailShrinking));
    SetDisableCloth(zdo_sailFlags.HasFlag(SailFlags.DisableCloth));
    UpdateSailArea();
    if (meshUpdateRequired)
    {
      CreateSailMesh();
    }
    else if (coefficientUpdateRequired)
    {
      UpdateCoefficients();
    }
  }

  public void SaveZDO()
  {
    if ((bool)m_nview && m_nview.m_zdo != null)
    {
      MemoryStream stream = new MemoryStream();
      BinaryWriter writer = new BinaryWriter(stream);
      writer.Write((byte)1);
      writer.Write((byte)m_sailCorners.Count);
      for (int i = 0; i < m_sailCorners.Count; i++)
      {
        writer.Write(m_sailCorners[i]);
      }

      writer.Write((byte)m_lockedSailSides);
      writer.Write((byte)m_lockedSailCorners);
      writer.Write(m_mainScale);
      writer.Write(m_mainOffset);
      writer.Write(m_mainColor);
      writer.Write(m_mainHash);
      writer.Write(m_patternScale);
      writer.Write(m_patternOffset);
      writer.Write(m_patternColor);
      writer.Write(m_patternHash);
      writer.Write(m_patternRotation);
      writer.Write(m_logoScale);
      writer.Write(m_logoOffset);
      writer.Write(m_logoColor);
      writer.Write(m_logoHash);
      writer.Write(m_logoRotation);
      writer.Write((byte)m_sailFlags);
      m_nview.m_zdo.Set("MB_sailConfig", stream.ToArray());
    }
  }

  public void CreateSailMesh()
  {
    ZLog.Log(
      $"CreateSailMesh(): {m_sailCorners.Count} m_lockedSailCorners: {m_lockedSailCorners} ({(int)m_lockedSailCorners}) m_lockedSailSides: {m_lockedSailSides} ({(int)m_lockedSailSides})");
    m_sailCloth.enabled = false;
    if (m_sailCorners.Count < 3)
    {
      return;
    }

    foreach (var VARIABLE in m_sailCorners)
    {
      ZLog.Log($"SAILCORNER: {VARIABLE}");
    }

    List<Vector3> vertices = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<int> triangles = new List<int>();
    Mesh collisionMesh = new Mesh();
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
      float dx = (m_sailCorners[1] - m_sailCorners[0]).magnitude;
      float dy = (m_sailCorners[2] - m_sailCorners[0]).magnitude;
      float dxs = Mathf.Round(dx / m_sailSubdivision);
      float dys = Mathf.Round(dy / m_sailSubdivision);
      for (int x2 = 0; (float)x2 <= dxs; x2++)
      {
        for (int y2 = 0; (float)y2 <= dys; y2++)
        {
          Vector3 xs1 = Vector3.Lerp(m_sailCorners[0], m_sailCorners[1], (float)x2 / dxs);
          Vector3 xs2 = Vector3.Lerp(m_sailCorners[3], m_sailCorners[2], (float)x2 / dxs);
          Vector3 ys1 = Vector3.Lerp(xs1, xs2, (float)y2 / dys);
          vertices.Add(ys1);
          uvs.Add(new Vector2
          {
            x = (float)x2 / dxs,
            y = (float)y2 / dys
          });
        }
      }

      dxs += 1f;
      dys += 1f;
      for (int x = 0; (float)x < dxs - 1f; x++)
      {
        for (int y = 0; (float)y < dys - 1f; y++)
        {
          triangles.Add((int)(dys * (float)x + (float)y) + 1);
          triangles.Add((int)(dys * (float)x + (float)y));
          triangles.Add((int)(dys * (float)x + (float)y) + (int)dys);
          triangles.Add((int)(dys * (float)x + (float)y) + 1);
          triangles.Add((int)(dys * (float)x + (float)y) + (int)dys);
          triangles.Add((int)(dys * (float)x + (float)y) + (int)dys + 1);
        }
      }
    }

    Mesh mesh = new Mesh();
    mesh.SetVertices(vertices);
    mesh.SetTriangles(triangles, 0);
    mesh.SetUVs(0, uvs);
    if (m_sailCorners.Count == 3)
    {
      float sqrSubDist = m_sailSubdivision * m_sailSubdivision;
      while (true)
      {
        float dist = (mesh.vertices[mesh.triangles[0]] - mesh.vertices[mesh.triangles[1]])
          .sqrMagnitude;
        if (dist < sqrSubDist)
        {
          break;
        }

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
    if (m_sailArea == 0f)
    {
      UpdateSailArea();
    }

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
    Vector3 result = Vector3.zero;
    for (int p = vertices.Count - 1, q = 0; q < vertices.Count; p = q++)
    {
      result += Vector3.Cross(vertices[q], vertices[p]);
    }

    result *= 0.5f;
    return result.magnitude;
  }

  /**
   * mesh area may still be useful.
   */
  float CalculateFacingArea(Mesh mesh, Vector3 direction)
  {
    direction = direction.normalized;
    var triangles = mesh.triangles;
    var vertices = mesh.vertices;

    double sum = 0.0;

    for (int i = 0; i < triangles.Length; i += 3)
    {
      Vector3 corner = vertices[triangles[i]];
      Vector3 a = vertices[triangles[i + 1]] - corner;
      Vector3 b = vertices[triangles[i + 2]] - corner;

      float projection = Vector3.Dot(Vector3.Cross(b, a), direction);
      if (projection > 0f)
        sum += projection;
    }

    return (float)(sum / 2.0);
  }

  public void UpdateCoefficients()
  {
    m_sailCloth.enabled = !m_sailFlags.HasFlag(SailFlags.DisableCloth);

    UpdateSailArea();

    Mesh mesh = m_mesh.sharedMesh;
    ClothSkinningCoefficient[] coefficients = ((mesh.vertexCount == m_sailCloth.coefficients.Length)
      ? m_sailCloth.coefficients
      : new ClothSkinningCoefficient[mesh.vertexCount]);
    for (int i = 0; i < coefficients.Length; i++)
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

      Vector3 sideA2 = (m_sailCorners[0] - m_sailCorners[1]).normalized;
      Vector3 sideB2 = (m_sailCorners[1] - m_sailCorners[2]).normalized;
      Vector3 sideC2 = (m_sailCorners[2] - m_sailCorners[0]).normalized;
      for (int k = 0; k < mesh.vertices.Length; k++)
      {
        if (m_lockedSailCorners.HasFlag(SailLockedSide.A) && mesh.vertices[k] == m_sailCorners[0])
        {
          coefficients[k].maxDistance = 0f;
        }

        if (m_lockedSailCorners.HasFlag(SailLockedSide.B) && mesh.vertices[k] == m_sailCorners[1])
        {
          coefficients[k].maxDistance = 0f;
        }

        if (m_lockedSailCorners.HasFlag(SailLockedSide.C) && mesh.vertices[k] == m_sailCorners[2])
        {
          coefficients[k].maxDistance = 0f;
        }

        if (m_lockedSailSides.HasFlag(SailLockedSide.A) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[0] - mesh.vertices[k]).normalized, sideA2)) >=
            0.9999f)
        {
          coefficients[k].maxDistance = 0f;
        }

        if (m_lockedSailSides.HasFlag(SailLockedSide.B) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[1] - mesh.vertices[k]).normalized, sideB2)) >=
            0.9999f)
        {
          coefficients[k].maxDistance = 0f;
        }

        if (m_lockedSailSides.HasFlag(SailLockedSide.C) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[2] - mesh.vertices[k]).normalized, sideC2)) >=
            0.9999f)
        {
          coefficients[k].maxDistance = 0f;
        }
      }
    }
    else if (m_sailCorners.Count == 4)
    {
      if (m_lockedSailCorners == SailLockedSide.None && m_lockedSailSides == SailLockedSide.None)
      {
        m_lockedSailCorners = SailLockedSide.Everything;
        m_lockedSailSides = SailLockedSide.Everything;
      }

      Vector3 sideA = (m_sailCorners[0] - m_sailCorners[1]).normalized;
      Vector3 sideB = (m_sailCorners[1] - m_sailCorners[2]).normalized;
      Vector3 sideC = (m_sailCorners[2] - m_sailCorners[3]).normalized;
      Vector3 sideD = (m_sailCorners[3] - m_sailCorners[0]).normalized;
      for (int j = 0; j < mesh.vertices.Length; j++)
      {
        if (m_lockedSailCorners.HasFlag(SailLockedSide.A) && mesh.vertices[j] == m_sailCorners[0])
        {
          coefficients[j].maxDistance = 0f;
        }

        if (m_lockedSailCorners.HasFlag(SailLockedSide.B) && mesh.vertices[j] == m_sailCorners[1])
        {
          coefficients[j].maxDistance = 0f;
        }

        if (m_lockedSailCorners.HasFlag(SailLockedSide.C) && mesh.vertices[j] == m_sailCorners[2])
        {
          coefficients[j].maxDistance = 0f;
        }

        if (m_lockedSailCorners.HasFlag(SailLockedSide.D) && mesh.vertices[j] == m_sailCorners[3])
        {
          coefficients[j].maxDistance = 0f;
        }

        if (m_lockedSailSides.HasFlag(SailLockedSide.A) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[0] - mesh.vertices[j]).normalized, sideA)) >=
            0.9999f)
        {
          coefficients[j].maxDistance = 0f;
        }

        if (m_lockedSailSides.HasFlag(SailLockedSide.B) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[1] - mesh.vertices[j]).normalized, sideB)) >=
            0.9999f)
        {
          coefficients[j].maxDistance = 0f;
        }

        if (m_lockedSailSides.HasFlag(SailLockedSide.C) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[2] - mesh.vertices[j]).normalized, sideC)) >=
            0.9999f)
        {
          coefficients[j].maxDistance = 0f;
        }

        if (m_lockedSailSides.HasFlag(SailLockedSide.D) &&
            Mathf.Abs(Vector3.Dot((m_sailCorners[3] - mesh.vertices[j]).normalized, sideD)) >=
            0.9999f)
        {
          coefficients[j].maxDistance = 0f;
        }
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
    if (m_patternHash == hash)
    {
      return;
    }

    m_patternHash = hash;
    CustomTextureGroup.CustomTexture customtexture =
      CustomTextureGroup.Get("Patterns").GetTextureByHash(hash);
    if (customtexture != null && (bool)customtexture.Texture && (bool)m_mesh)
    {
      m_mesh.material.SetTexture("_PatternTex", customtexture.Texture);
      if ((bool)customtexture.Normal)
      {
        m_mesh.material.SetTexture("_PatternNormal", customtexture.Normal);
      }
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
    if (m_mainHash == hash)
    {
      return;
    }

    m_mainHash = hash;
    CustomTextureGroup.CustomTexture customtexture =
      CustomTextureGroup.Get("Sails").GetTextureByHash(hash);
    if (customtexture != null && (bool)customtexture.Texture && (bool)m_mesh)
    {
      m_mesh.material.SetTexture("_MainTex", customtexture.Texture);
      if ((bool)customtexture.Normal)
      {
        m_mesh.material.SetTexture("_MainNormal", customtexture.Normal);
      }
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
    if (m_logoHash == hash)
    {
      return;
    }

    m_logoHash = hash;
    CustomTextureGroup.CustomTexture customtexture =
      CustomTextureGroup.Get("Logos").GetTextureByHash(hash);
    if (customtexture != null && (bool)customtexture.Texture && (bool)m_mesh)
    {
      m_mesh.material.SetTexture("_LogoTex", customtexture.Texture);
      if ((bool)customtexture.Normal)
      {
        m_mesh.material.SetTexture("_LogoNormal", customtexture.Normal);
      }
    }
  }

  private void OnDrawGizmos()
  {
    for (int i = 0; i < m_sailCorners.Count; i++)
    {
      Gizmos.DrawSphere(base.transform.position + m_sailCorners[i], 0.1f);
    }
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
    if (m_editPanel == null)
    {
      m_editPanel = new EditSailComponentPanel();
    }

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