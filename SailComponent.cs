// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.SailComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ValheimRAFT.UI;
using ValheimRAFT.Util;

namespace ValheimRAFT
{
  public class SailComponent : MonoBehaviour, Interactable, Hoverable
  {
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
    private static EditSailComponentPanel m_editPanel = (EditSailComponentPanel)null;
    internal static bool m_sailInit = true;
    public SailComponent.SailFlags m_sailFlags;
    public float m_windMultiplier = 10f;
    public float m_clothRandomAccelerationFactor = 0.5f;
    public SailComponent.SailLockedSide m_lockedSailSides;
    public SailComponent.SailLockedSide m_lockedSailCorners;
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

    public void Awake()
    {
      SailComponent.m_sailComponents.Add(this);
      this.m_mastComponent = ((Component)this).GetComponent<MastComponent>();
      this.m_sailObject = ((Component)((Component)this).transform.Find("Sail")).gameObject;
      this.m_sailCloth = this.m_sailObject.GetComponent<Cloth>();
      if (m_sailCloth)
      {
        this.m_sailCloth.useTethers = false;
        this.m_sailCloth.useGravity = true;
        this.m_sailCloth.bendingStiffness = 1f;
      }

      this.m_mesh = this.m_sailObject.GetComponent<SkinnedMeshRenderer>();
      this.m_meshCollider = this.m_sailObject.GetComponent<MeshCollider>();
      this.m_nview = ((Component)this).GetComponent<ZNetView>();
      if (SailComponent.m_sailInit)
        this.LoadZDO();
      else if (!ZNetView.m_forceDisableInit)
        this.InvokeRepeating("LoadZDO", 5f, 5f);
    }

    public void Update()
    {
      Vector3 windForce = EnvMan.instance.GetWindForce();
      this.m_sailCloth.externalAcceleration = windForce * m_windMultiplier;
      this.m_sailCloth.randomAcceleration =
        windForce * m_windMultiplier * m_clothRandomAccelerationFactor;
    }

    public void OnDestroy() => SailComponent.m_sailComponents.Remove(this);

    public void SetAllowSailShrinking(bool allow)
    {
      if (!m_mastComponent)
        return;
      this.m_sailFlags =
        allow
          ? this.m_sailFlags | SailComponent.SailFlags.AllowSailShrinking
          : this.m_sailFlags & ~SailComponent.SailFlags.AllowSailShrinking;
      this.m_mastComponent.m_allowSailShrinking = allow;
    }

    public void SetDisableCloth(bool allow)
    {
      if (!m_mastComponent)
        return;
      this.m_sailFlags =
        allow
          ? this.m_sailFlags | SailComponent.SailFlags.DisableCloth
          : this.m_sailFlags & ~SailComponent.SailFlags.DisableCloth;
      this.m_mastComponent.m_disableCloth = allow;
      if (m_sailCloth && this.m_sailCloth.enabled != !allow)
        this.m_sailCloth.enabled = !allow;
    }

    public void LoadFromMaterial()
    {
      Material sailMaterial = this.GetSailMaterial();
      Texture texture1 = sailMaterial.GetTexture("_MainTex");
      if (CustomTextureGroup.Get("Sails")
            .GetTextureByHash(StringExtensionMethods.GetStableHashCode((texture1).name)) !=
          null)
        this.m_mainHash = StringExtensionMethods.GetStableHashCode((texture1).name);
      this.m_mainScale = sailMaterial.GetTextureScale("_MainTex");
      this.m_mainOffset = sailMaterial.GetTextureOffset("_MainTex");
      this.m_mainColor = sailMaterial.GetColor("_MainColor");
      Texture texture2 = sailMaterial.GetTexture("_PatternTex");
      if (CustomTextureGroup.Get("Patterns")
            .GetTextureByHash(StringExtensionMethods.GetStableHashCode((texture2).name)) !=
          null)
        this.m_patternHash = StringExtensionMethods.GetStableHashCode((texture2).name);
      this.m_patternScale = sailMaterial.GetTextureScale("_PatternTex");
      this.m_patternOffset = sailMaterial.GetTextureOffset("_PatternTex");
      this.m_patternColor = sailMaterial.GetColor("_PatternColor");
      this.m_patternRotation = sailMaterial.GetFloat("_PatternRotation");
      Texture texture3 = sailMaterial.GetTexture("_LogoTex");
      if (CustomTextureGroup.Get("Logos")
            .GetTextureByHash(StringExtensionMethods.GetStableHashCode(texture3.name)) !=
          null)
        this.m_logoHash = StringExtensionMethods.GetStableHashCode(texture3.name);
      this.m_logoScale = sailMaterial.GetTextureScale("_LogoTex");
      this.m_logoOffset = sailMaterial.GetTextureOffset("_LogoTex");
      this.m_logoColor = sailMaterial.GetColor("_LogoColor");
      this.m_logoRotation = sailMaterial.GetFloat("_LogoRotation");
    }

    public Material GetSailMaterial() => ((Renderer)this.m_mesh).material;

    public void LoadZDO()
    {
      if (!m_nview || this.m_nview.m_zdo == null)
        return;
      byte[] byteArray = this.m_nview.m_zdo.GetByteArray("MB_sailConfig", (byte[])null);
      if (byteArray == null)
        return;
      BinaryReader reader = new BinaryReader((Stream)new MemoryStream(byteArray));
      if (reader.ReadByte() != (byte)1)
        return;
      bool flag1 = false;
      bool flag2 = false;
      int num = (int)reader.ReadByte();
      if (this.m_sailCorners.Count != num)
      {
        flag1 = true;
        this.m_sailCorners.Clear();
        for (int index = 0; index < num; ++index)
          this.m_sailCorners.Add(Utils.ReadVector3(reader));
      }
      else
      {
        for (int index = 0; index < num; ++index)
        {
          Vector3 vector3 = Utils.ReadVector3(reader);
          if ((this.m_sailCorners[index] != vector3))
          {
            this.m_sailCorners[index] = vector3;
            flag1 = true;
          }
        }
      }

      SailComponent.SailLockedSide
        sailLockedSide1 = (SailComponent.SailLockedSide)reader.ReadByte();
      SailComponent.SailLockedSide
        sailLockedSide2 = (SailComponent.SailLockedSide)reader.ReadByte();
      Vector2 vector2_1 = reader.ReadVector2();
      Vector2 vector2_2 = reader.ReadVector2();
      Color color1 = reader.ReadColor();
      int hash1 = reader.ReadInt32();
      Vector2 vector2_3 = reader.ReadVector2();
      Vector2 vector2_4 = reader.ReadVector2();
      Color color2 = reader.ReadColor();
      int hash2 = reader.ReadInt32();
      float rotation1 = reader.ReadSingle();
      Vector2 vector2_5 = reader.ReadVector2();
      Vector2 vector2_6 = reader.ReadVector2();
      Color color3 = reader.ReadColor();
      int hash3 = reader.ReadInt32();
      float rotation2 = reader.ReadSingle();
      SailComponent.SailFlags sailFlags = (SailComponent.SailFlags)reader.ReadByte();
      if (sailLockedSide1 != this.m_lockedSailSides)
      {
        flag2 = true;
        this.m_lockedSailSides = sailLockedSide1;
      }

      if (sailLockedSide2 != this.m_lockedSailCorners)
      {
        flag2 = true;
        this.m_lockedSailCorners = sailLockedSide2;
      }

      this.SetMain(hash1);
      this.SetMainColor(color1);
      this.SetMainOffset(vector2_2);
      this.SetMainScale(vector2_1);
      this.SetPattern(hash2);
      this.SetPatternColor(color2);
      this.SetPatternOffset(vector2_4);
      this.SetPatternScale(vector2_3);
      this.SetPatternRotation(rotation1);
      this.SetLogo(hash3);
      this.SetLogoColor(color3);
      this.SetLogoOffset(vector2_6);
      this.SetLogoScale(vector2_5);
      this.SetLogoRotation(rotation2);
      this.SetAllowSailShrinking(
        sailFlags.HasFlag((Enum)SailComponent.SailFlags.AllowSailShrinking));
      this.SetDisableCloth(sailFlags.HasFlag((Enum)SailComponent.SailFlags.DisableCloth));
      if (flag1)
      {
        this.CreateSailMesh();
      }
      else
      {
        if (!flag2)
          return;
        this.UpdateCoefficients();
      }
    }

    public void SaveZDO()
    {
      if (!this.m_nview || this.m_nview.m_zdo == null)
        return;
      MemoryStream output = new MemoryStream();
      BinaryWriter writer = new BinaryWriter((Stream)output);
      writer.Write((byte)1);
      writer.Write((byte)this.m_sailCorners.Count);
      for (int index = 0; index < this.m_sailCorners.Count; ++index)
        Utils.Write(writer, this.m_sailCorners[index]);
      writer.Write((byte)this.m_lockedSailSides);
      writer.Write((byte)this.m_lockedSailCorners);
      Utils.Write(writer, this.m_mainScale);
      Utils.Write(writer, this.m_mainOffset);
      writer.Write(this.m_mainColor);
      writer.Write(this.m_mainHash);
      Utils.Write(writer, this.m_patternScale);
      Utils.Write(writer, this.m_patternOffset);
      writer.Write(this.m_patternColor);
      writer.Write(this.m_patternHash);
      writer.Write(this.m_patternRotation);
      Utils.Write(writer, this.m_logoScale);
      Utils.Write(writer, this.m_logoOffset);
      writer.Write(this.m_logoColor);
      writer.Write(this.m_logoHash);
      writer.Write(this.m_logoRotation);
      writer.Write((byte)this.m_sailFlags);
      this.m_nview.m_zdo.Set("MB_sailConfig", output.ToArray());
    }

    public void CreateSailMesh()
    {
      ZLog.Log((object)string.Format(
        "CreateSailMesh(): {0} m_lockedSailCorners: {1} ({2}) m_lockedSailSides: {3} ({4})",
        (object)this.m_sailCorners.Count, (object)this.m_lockedSailCorners,
        (object)(int)this.m_lockedSailCorners, (object)this.m_lockedSailSides,
        (object)(int)this.m_lockedSailSides));
      this.m_sailCloth.enabled = false;
      if (this.m_sailCorners.Count < 3)
        return;
      List<Vector3> vector3List = new List<Vector3>();
      List<Vector2> vector2List1 = new List<Vector2>();
      List<int> intList = new List<int>();
      Mesh mesh1 = new Mesh();
      Vector3 vector3_1;
      if (this.m_sailCorners.Count == 3)
      {
        mesh1.SetVertices(new Vector3[3]
        {
          this.m_sailCorners[0],
          this.m_sailCorners[1],
          this.m_sailCorners[2]
        });
        mesh1.SetTriangles(new int[6] { 0, 1, 2, 0, 2, 1 }, 0);
        mesh1.Optimize();
        vector3List.Add(this.m_sailCorners[0]);
        vector3List.Add(this.m_sailCorners[1]);
        vector3List.Add(this.m_sailCorners[2]);
        intList.Add(0);
        intList.Add(1);
        intList.Add(2);
        List<Vector2> vector2List2 = vector2List1;
        Vector2 vector2_1 = new Vector2();
        vector2_1.x = 0.0f;
        vector2_1.y = 0.0f;
        Vector2 vector2_2 = vector2_1;
        vector2List2.Add(vector2_2);
        List<Vector2> vector2List3 = vector2List1;
        vector2_1 = new Vector2();
        vector2_1.x = 1f;
        vector2_1.y = 0.0f;
        Vector2 vector2_3 = vector2_1;
        vector2List3.Add(vector2_3);
        vector2List1.Add(new Vector2() { x = 1f, y = 1f });
      }
      else if (this.m_sailCorners.Count == 4)
      {
        mesh1.SetVertices(new Vector3[4]
        {
          this.m_sailCorners[0],
          this.m_sailCorners[1],
          this.m_sailCorners[2],
          this.m_sailCorners[3]
        });
        mesh1.SetTriangles(new int[12]
        {
          0,
          1,
          2,
          1,
          0,
          2,
          1,
          2,
          3,
          2,
          1,
          3
        }, 0);
        mesh1.Optimize();
        Vector3 vector3_2 = (this.m_sailCorners[1] - this.m_sailCorners[0]);
        float magnitude1 = ((Vector3)vector3_2).magnitude;
        vector3_1 = (this.m_sailCorners[2] - this.m_sailCorners[0]);
        float magnitude2 = ((Vector3)vector3_1).magnitude;
        float num1 = Mathf.Round(magnitude1 / this.m_sailSubdivision);
        float num2 = Mathf.Round(magnitude2 / this.m_sailSubdivision);
        for (int index1 = 0; (double)index1 <= (double)num1; ++index1)
        {
          for (int index2 = 0; (double)index2 <= (double)num2; ++index2)
          {
            Vector3 vector3_3 = Vector3.Lerp(
              Vector3.Lerp(this.m_sailCorners[0], this.m_sailCorners[1], (float)index1 / num1),
              Vector3.Lerp(this.m_sailCorners[3], this.m_sailCorners[2], (float)index1 / num1),
              (float)index2 / num2);
            vector3List.Add(vector3_3);
            vector2List1.Add(new Vector2()
            {
              x = (float)index1 / num1,
              y = (float)index2 / num2
            });
          }
        }

        float num3 = num1 + 1f;
        float num4 = num2 + 1f;
        for (int index3 = 0; (double)index3 < (double)num3 - 1.0; ++index3)
        {
          for (int index4 = 0; (double)index4 < (double)num4 - 1.0; ++index4)
          {
            intList.Add((int)((double)num4 * (double)index3 + (double)index4) + 1);
            intList.Add((int)((double)num4 * (double)index3 + (double)index4));
            intList.Add((int)((double)num4 * (double)index3 + (double)index4) + (int)num4);
            intList.Add((int)((double)num4 * (double)index3 + (double)index4) + 1);
            intList.Add((int)((double)num4 * (double)index3 + (double)index4) + (int)num4);
            intList.Add((int)((double)num4 * (double)index3 + (double)index4) + (int)num4 + 1);
          }
        }
      }

      Mesh mesh2 = new Mesh();
      mesh2.SetVertices(vector3List);
      mesh2.SetTriangles(intList, 0);
      mesh2.SetUVs(0, vector2List1);
      if (this.m_sailCorners.Count == 3)
      {
        float num = this.m_sailSubdivision * this.m_sailSubdivision;
        while (true)
        {
          vector3_1 = (mesh2.vertices[mesh2.triangles[0]] -
                       mesh2.vertices[mesh2.triangles[1]]);
          if ((double)((Vector3)vector3_1).sqrMagnitude >= (double)num)
            MeshHelper.Subdivide(mesh2);
          else
            break;
        }
      }

      mesh2.Optimize();
      mesh2.RecalculateNormals();
      this.m_mesh.sharedMesh = mesh2;
      this.m_meshCollider.sharedMesh = mesh1;
      this.UpdateCoefficients();
    }

    public void UpdateCoefficients()
    {
      this.m_sailCloth.enabled =
        !this.m_sailFlags.HasFlag((Enum)SailComponent.SailFlags.DisableCloth);
      Mesh sharedMesh = this.m_mesh.sharedMesh;
      ClothSkinningCoefficient[] skinningCoefficientArray =
        sharedMesh.vertexCount == this.m_sailCloth.coefficients.Length
          ? this.m_sailCloth.coefficients
          : new ClothSkinningCoefficient[sharedMesh.vertexCount];
      for (int index = 0; index < skinningCoefficientArray.Length; ++index)
      {
        skinningCoefficientArray[index].maxDistance = float.MaxValue;
        skinningCoefficientArray[index].collisionSphereDistance = float.MaxValue;
      }

      if (this.m_sailCorners.Count == 3)
      {
        this.m_lockedSailCorners &= ~SailComponent.SailLockedSide.D;
        this.m_lockedSailSides &= ~SailComponent.SailLockedSide.D;
        if (this.m_lockedSailCorners == SailComponent.SailLockedSide.None &&
            this.m_lockedSailSides == SailComponent.SailLockedSide.None)
        {
          this.m_lockedSailCorners = SailComponent.SailLockedSide.Everything;
          this.m_lockedSailSides = SailComponent.SailLockedSide.Everything;
        }

        Vector3 vector3_1 = this.m_sailCorners[0] - this.m_sailCorners[1];
        Vector3 normalized1 = ((Vector3)vector3_1).normalized;
        Vector3 vector3_2 = this.m_sailCorners[1] - this.m_sailCorners[2];
        Vector3 normalized2 = ((Vector3)vector3_2).normalized;
        Vector3 vector3_3 = this.m_sailCorners[2] - this.m_sailCorners[0];
        Vector3 normalized3 = ((Vector3)vector3_3).normalized;
        for (int index = 0; index < sharedMesh.vertices.Length; ++index)
        {
          if (this.m_lockedSailCorners.HasFlag((Enum)SailComponent.SailLockedSide.A) &&
              sharedMesh.vertices[index] == this.m_sailCorners[0])
            skinningCoefficientArray[index].maxDistance = 0.0f;
          if (this.m_lockedSailCorners.HasFlag((Enum)SailComponent.SailLockedSide.B) &&
              sharedMesh.vertices[index] == this.m_sailCorners[1])
            skinningCoefficientArray[index].maxDistance = 0.0f;
          if (this.m_lockedSailCorners.HasFlag((Enum)SailComponent.SailLockedSide.C) &&
              sharedMesh.vertices[index] == this.m_sailCorners[2])
            skinningCoefficientArray[index].maxDistance = 0.0f;
          int num1;
          if (this.m_lockedSailSides.HasFlag((Enum)SailComponent.SailLockedSide.A))
          {
            vector3_3 = (this.m_sailCorners[0] - sharedMesh.vertices[index]);
            num1 = (double)Mathf.Abs(Vector3.Dot(((Vector3)vector3_3).normalized, normalized1)) >=
                   0.99989998340606689
              ? 1
              : 0;
          }
          else
            num1 = 0;

          if (num1 != 0)
            skinningCoefficientArray[index].maxDistance = 0.0f;
          int num2;
          if (this.m_lockedSailSides.HasFlag((Enum)SailComponent.SailLockedSide.B))
          {
            vector3_3 = (this.m_sailCorners[1] - sharedMesh.vertices[index]);
            num2 = (double)Mathf.Abs(Vector3.Dot(((Vector3)vector3_3).normalized, normalized2)) >=
                   0.99989998340606689
              ? 1
              : 0;
          }
          else
            num2 = 0;

          if (num2 != 0)
            skinningCoefficientArray[index].maxDistance = 0.0f;
          int num3;
          if (this.m_lockedSailSides.HasFlag((Enum)SailComponent.SailLockedSide.C))
          {
            vector3_3 = (this.m_sailCorners[2] - sharedMesh.vertices[index]);
            num3 = (double)Mathf.Abs(Vector3.Dot(((Vector3)vector3_3).normalized, normalized3)) >=
                   0.99989998340606689
              ? 1
              : 0;
          }
          else
            num3 = 0;

          if (num3 != 0)
            skinningCoefficientArray[index].maxDistance = 0.0f;
        }
      }
      else if (this.m_sailCorners.Count == 4)
      {
        if (this.m_lockedSailCorners == SailComponent.SailLockedSide.None &&
            this.m_lockedSailSides == SailComponent.SailLockedSide.None)
        {
          this.m_lockedSailCorners = SailComponent.SailLockedSide.Everything;
          this.m_lockedSailSides = SailComponent.SailLockedSide.Everything;
        }

        Vector3 vector3_4 = (this.m_sailCorners[0] - this.m_sailCorners[1]);
        Vector3 normalized4 = ((Vector3)vector3_4).normalized;
        Vector3 vector3_5 = (this.m_sailCorners[1] - this.m_sailCorners[2]);
        Vector3 normalized5 = ((Vector3)vector3_5).normalized;
        Vector3 vector3_6 = (this.m_sailCorners[2] - this.m_sailCorners[3]);
        Vector3 normalized6 = ((Vector3)vector3_6).normalized;
        vector3_6 = (this.m_sailCorners[3] - this.m_sailCorners[0]);
        Vector3 normalized7 = ((Vector3)vector3_6).normalized;
        for (int index = 0; index < sharedMesh.vertices.Length; ++index)
        {
          if (this.m_lockedSailCorners.HasFlag((Enum)SailComponent.SailLockedSide.A) &&
              sharedMesh.vertices[index] == this.m_sailCorners[0])
            skinningCoefficientArray[index].maxDistance = 0.0f;
          if (this.m_lockedSailCorners.HasFlag((Enum)SailComponent.SailLockedSide.B) &&
              (sharedMesh.vertices[index] == this.m_sailCorners[1]))
            skinningCoefficientArray[index].maxDistance = 0.0f;
          if (this.m_lockedSailCorners.HasFlag((Enum)SailComponent.SailLockedSide.C) &&
              (sharedMesh.vertices[index] == this.m_sailCorners[2]))
            skinningCoefficientArray[index].maxDistance = 0.0f;
          if (this.m_lockedSailCorners.HasFlag((Enum)SailComponent.SailLockedSide.D) &&
              (sharedMesh.vertices[index] == this.m_sailCorners[3]))
            skinningCoefficientArray[index].maxDistance = 0.0f;
          int num4;
          if (this.m_lockedSailSides.HasFlag((Enum)SailComponent.SailLockedSide.A))
          {
            vector3_6 = (this.m_sailCorners[0] - sharedMesh.vertices[index]);
            num4 = (double)Mathf.Abs(Vector3.Dot(((Vector3)vector3_6).normalized, normalized4)) >=
                   0.99989998340606689
              ? 1
              : 0;
          }
          else
            num4 = 0;

          if (num4 != 0)
            skinningCoefficientArray[index].maxDistance = 0.0f;
          int num5;
          if (this.m_lockedSailSides.HasFlag((Enum)SailComponent.SailLockedSide.B))
          {
            vector3_6 = (this.m_sailCorners[1] - sharedMesh.vertices[index]);
            num5 = (double)Mathf.Abs(Vector3.Dot(((Vector3)vector3_6).normalized, normalized5)) >=
                   0.99989998340606689
              ? 1
              : 0;
          }
          else
            num5 = 0;

          if (num5 != 0)
            skinningCoefficientArray[index].maxDistance = 0.0f;
          int num6;
          if (this.m_lockedSailSides.HasFlag((Enum)SailComponent.SailLockedSide.C))
          {
            vector3_6 = (this.m_sailCorners[2] - sharedMesh.vertices[index]);
            num6 = (double)Mathf.Abs(Vector3.Dot(((Vector3)vector3_6).normalized, normalized6)) >=
                   0.99989998340606689
              ? 1
              : 0;
          }
          else
            num6 = 0;

          if (num6 != 0)
            skinningCoefficientArray[index].maxDistance = 0.0f;
          int num7;
          if (this.m_lockedSailSides.HasFlag((Enum)SailComponent.SailLockedSide.D))
          {
            vector3_6 = (this.m_sailCorners[3] - sharedMesh.vertices[index]);
            num7 = (double)Mathf.Abs(Vector3.Dot(((Vector3)vector3_6).normalized, normalized7)) >=
                   0.99989998340606689
              ? 1
              : 0;
          }
          else
            num7 = 0;

          if (num7 != 0)
            skinningCoefficientArray[index].maxDistance = 0.0f;
        }
      }

      this.m_sailCloth.coefficients = skinningCoefficientArray;
    }

    public void SetPatternScale(Vector2 vector2)
    {
      if (this.m_patternScale == vector2)
        return;
      this.m_patternScale = vector2;
      ((Renderer)this.m_mesh).material.SetTextureScale("_PatternTex", this.m_patternScale);
    }

    public void SetPatternOffset(Vector2 vector2)
    {
      if ((this.m_patternOffset == vector2))
        return;
      this.m_patternOffset = vector2;
      ((Renderer)this.m_mesh).material.SetTextureOffset("_PatternTex", this.m_patternOffset);
    }

    public void SetPatternColor(Color color)
    {
      if ((this.m_patternColor == color))
        return;
      this.m_patternColor = color;
      ((Renderer)this.m_mesh).material.SetColor("_PatternColor", color);
    }

    public void SetPatternRotation(float rotation)
    {
      if ((double)this.m_patternRotation == (double)rotation)
        return;
      this.m_patternRotation = rotation;
      ((Renderer)this.m_mesh).material.SetFloat("_PatternRotation", rotation);
    }

    public void SetPattern(int hash)
    {
      if (this.m_patternHash == hash)
        return;
      this.m_patternHash = hash;
      CustomTextureGroup.CustomTexture textureByHash =
        CustomTextureGroup.Get("Patterns").GetTextureByHash(hash);
      if (textureByHash == null || !textureByHash.Texture ||
          !m_mesh)
        return;
      ((Renderer)this.m_mesh).material.SetTexture("_PatternTex", textureByHash.Texture);
      if (!textureByHash.Normal)
        return;
      ((Renderer)this.m_mesh).material.SetTexture("_PatternNormal", textureByHash.Normal);
    }

    public void SetMainScale(Vector2 vector2)
    {
      if (this.m_mainScale == vector2)
        return;
      this.m_mainScale = vector2;
      ((Renderer)this.m_mesh).material.SetTextureScale("_MainTex", this.m_mainScale);
    }

    public void SetMainOffset(Vector2 vector2)
    {
      if (this.m_mainOffset == vector2)
        return;
      this.m_mainOffset = vector2;
      ((Renderer)this.m_mesh).material.SetTextureOffset("_MainTex", this.m_mainOffset);
    }

    public void SetMainColor(Color color)
    {
      if (this.m_mainColor == color)
        return;
      this.m_mainColor = color;
      ((Renderer)this.m_mesh).material.SetColor("_MainColor", color);
    }

    public void SetMain(int hash)
    {
      if (this.m_mainHash == hash)
        return;
      this.m_mainHash = hash;
      CustomTextureGroup.CustomTexture textureByHash =
        CustomTextureGroup.Get("Sails").GetTextureByHash(hash);
      if (textureByHash == null || !textureByHash.Texture ||
          !m_mesh)
        return;
      ((Renderer)this.m_mesh).material.SetTexture("_MainTex", textureByHash.Texture);
      if (!textureByHash.Normal)
        return;
      ((Renderer)this.m_mesh).material.SetTexture("_MainNormal", textureByHash.Normal);
    }

    public void SetLogoScale(Vector2 vector2)
    {
      if (this.m_logoScale == vector2)
        return;
      this.m_logoScale = vector2;
      ((Renderer)this.m_mesh).material.SetTextureScale("_LogoTex", this.m_logoScale);
    }

    public void SetLogoOffset(Vector2 vector2)
    {
      if (this.m_logoOffset == vector2)
        return;
      this.m_logoOffset = vector2;
      ((Renderer)this.m_mesh).material.SetTextureOffset("_LogoTex", this.m_logoOffset);
    }

    public void SetLogoColor(Color color)
    {
      if (this.m_logoColor == color)
        return;
      this.m_logoColor = color;
      ((Renderer)this.m_mesh).material.SetColor("_LogoColor", color);
    }

    public void SetLogoRotation(float rotation)
    {
      if ((double)this.m_logoRotation == (double)rotation)
        return;
      this.m_logoRotation = rotation;
      ((Renderer)this.m_mesh).material.SetFloat("_LogoRotation", rotation);
    }

    public void SetLogo(int hash)
    {
      if (this.m_logoHash == hash)
        return;
      this.m_logoHash = hash;
      CustomTextureGroup.CustomTexture textureByHash =
        CustomTextureGroup.Get("Logos").GetTextureByHash(hash);
      if (textureByHash == null || !textureByHash.Texture ||
          !this.m_mesh)
        return;
      ((Renderer)this.m_mesh).material.SetTexture("_LogoTex", textureByHash.Texture);
      if (!textureByHash.Normal)
        return;
      ((Renderer)this.m_mesh).material.SetTexture("_LogoNormal", textureByHash.Normal);
    }

    private void OnDrawGizmos()
    {
      for (int index = 0; index < this.m_sailCorners.Count; ++index)
        Gizmos.DrawSphere(
          (((Component)this).transform.position + this.m_sailCorners[index]),
          0.1f);
    }

    public string GetHoverName() => "";

    public string GetHoverText() =>
      Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $mb_sail_edit");

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
      if (SailComponent.m_editPanel == null)
        SailComponent.m_editPanel = new EditSailComponentPanel();
      this.TryEdit();
      return true;
    }

    public void TryEdit()
    {
      if (!this.m_nview.IsOwner())
      {
        if (this.IsInvoking(nameof(TryEdit)))
          return;
        this.m_nview.ClaimOwnership();
        this.InvokeRepeating(nameof(TryEdit), 0.5f, 0.5f);
      }
      else
      {
        this.CancelInvoke(nameof(TryEdit));
        SailComponent.m_editPanel.ShowPanel(this);
      }
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    internal void StartEdit() => this.CancelInvoke("LoadZDO");

    internal void EndEdit() => this.InvokeRepeating("LoadZDO", 5f, 5f);

    [Flags]
    public enum SailFlags
    {
      None = 0,
      AllowSailShrinking = 1,
      DisableCloth = 2,
    }

    [Flags]
    public enum SailLockedSide
    {
      None = 0,
      A = 1,
      B = 2,
      C = 4,
      D = 8,
      Everything = D | C | B | A, // 0x0000000F
    }
  }
}