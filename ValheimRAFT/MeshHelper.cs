using System.Collections.Generic;
using UnityEngine;

namespace ValheimRAFT;

public class MeshHelper
{
	private static List<Vector3> vertices;

	private static List<Vector3> normals;

	private static List<Vector2> uv;

	private static List<int> indices;

	private static Dictionary<uint, int> newVectices;

	private static int GetNewVertex(int i1, int i2)
	{
		uint t1 = (uint)((i1 << 16) | i2);
		uint t2 = (uint)((i2 << 16) | i1);
		if (newVectices.ContainsKey(t2))
		{
			return newVectices[t2];
		}
		if (newVectices.ContainsKey(t1))
		{
			return newVectices[t1];
		}
		int newIndex = vertices.Count;
		newVectices.Add(t1, newIndex);
		vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
		if (normals != null)
		{
			normals.Add((normals[i1] + normals[i2]).normalized);
		}
		if (uv != null)
		{
			uv.Add((uv[i1] + uv[i2]) * 0.5f);
		}
		return newIndex;
	}

	public static void Subdivide(Mesh mesh)
	{
		newVectices = new Dictionary<uint, int>();
		vertices = new List<Vector3>(mesh.vertices);
		normals = ((mesh.normals != null && mesh.normals.Length == mesh.vertices.Length) ? new List<Vector3>(mesh.normals) : null);
		uv = ((mesh.uv != null && mesh.uv.Length == mesh.vertices.Length) ? new List<Vector2>(mesh.uv) : null);
		indices = new List<int>();
		int[] triangles = mesh.triangles;
		for (int i = 0; i < triangles.Length; i += 3)
		{
			int i2 = triangles[i];
			int i3 = triangles[i + 1];
			int i4 = triangles[i + 2];
			int a = GetNewVertex(i2, i3);
			int b = GetNewVertex(i3, i4);
			int c = GetNewVertex(i4, i2);
			indices.Add(i2);
			indices.Add(a);
			indices.Add(c);
			indices.Add(i3);
			indices.Add(b);
			indices.Add(a);
			indices.Add(i4);
			indices.Add(c);
			indices.Add(b);
			indices.Add(a);
			indices.Add(b);
			indices.Add(c);
		}
		mesh.vertices = vertices.ToArray();
		if (normals != null)
		{
			mesh.normals = normals.ToArray();
		}
		if (uv != null)
		{
			mesh.uv = uv.ToArray();
		}
		mesh.triangles = indices.ToArray();
		newVectices = null;
		vertices = null;
		normals = null;
		uv = null;
		indices = null;
	}
}
