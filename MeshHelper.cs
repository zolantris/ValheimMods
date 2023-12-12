// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.MeshHelper
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT
{
  public class MeshHelper
  {
    private static List<Vector3> vertices;
    private static List<Vector3> normals;
    private static List<Vector2> uv;
    private static List<int> indices;
    private static Dictionary<uint, int> newVectices;

    private static int GetNewVertex(int i1, int i2)
    {
      uint key1 = (uint) (i1 << 16 | i2);
      uint key2 = (uint) (i2 << 16 | i1);
      if (MeshHelper.newVectices.ContainsKey(key2))
        return MeshHelper.newVectices[key2];
      if (MeshHelper.newVectices.ContainsKey(key1))
        return MeshHelper.newVectices[key1];
      int count = MeshHelper.vertices.Count;
      MeshHelper.newVectices.Add(key1, count);
      MeshHelper.vertices.Add(Vector3.op_Multiply(Vector3.op_Addition(MeshHelper.vertices[i1], MeshHelper.vertices[i2]), 0.5f));
      if (MeshHelper.normals != null)
      {
        List<Vector3> normals = MeshHelper.normals;
        Vector3 vector3 = Vector3.op_Addition(MeshHelper.normals[i1], MeshHelper.normals[i2]);
        Vector3 normalized = ((Vector3) ref vector3).normalized;
        normals.Add(normalized);
      }
      if (MeshHelper.uv != null)
        MeshHelper.uv.Add(Vector2.op_Multiply(Vector2.op_Addition(MeshHelper.uv[i1], MeshHelper.uv[i2]), 0.5f));
      return count;
    }

    public static void Subdivide(Mesh mesh)
    {
      MeshHelper.newVectices = new Dictionary<uint, int>();
      MeshHelper.vertices = new List<Vector3>((IEnumerable<Vector3>) mesh.vertices);
      MeshHelper.normals = mesh.normals == null || mesh.normals.Length != mesh.vertices.Length ? (List<Vector3>) null : new List<Vector3>((IEnumerable<Vector3>) mesh.normals);
      MeshHelper.uv = mesh.uv == null || mesh.uv.Length != mesh.vertices.Length ? (List<Vector2>) null : new List<Vector2>((IEnumerable<Vector2>) mesh.uv);
      MeshHelper.indices = new List<int>();
      int[] triangles = mesh.triangles;
      for (int index = 0; index < triangles.Length; index += 3)
      {
        int num1 = triangles[index];
        int num2 = triangles[index + 1];
        int num3 = triangles[index + 2];
        int newVertex1 = MeshHelper.GetNewVertex(num1, num2);
        int newVertex2 = MeshHelper.GetNewVertex(num2, num3);
        int newVertex3 = MeshHelper.GetNewVertex(num3, num1);
        MeshHelper.indices.Add(num1);
        MeshHelper.indices.Add(newVertex1);
        MeshHelper.indices.Add(newVertex3);
        MeshHelper.indices.Add(num2);
        MeshHelper.indices.Add(newVertex2);
        MeshHelper.indices.Add(newVertex1);
        MeshHelper.indices.Add(num3);
        MeshHelper.indices.Add(newVertex3);
        MeshHelper.indices.Add(newVertex2);
        MeshHelper.indices.Add(newVertex1);
        MeshHelper.indices.Add(newVertex2);
        MeshHelper.indices.Add(newVertex3);
      }
      mesh.vertices = MeshHelper.vertices.ToArray();
      if (MeshHelper.normals != null)
        mesh.normals = MeshHelper.normals.ToArray();
      if (MeshHelper.uv != null)
        mesh.uv = MeshHelper.uv.ToArray();
      mesh.triangles = MeshHelper.indices.ToArray();
      MeshHelper.newVectices = (Dictionary<uint, int>) null;
      MeshHelper.vertices = (List<Vector3>) null;
      MeshHelper.normals = (List<Vector3>) null;
      MeshHelper.uv = (List<Vector2>) null;
      MeshHelper.indices = (List<int>) null;
    }
  }
}
