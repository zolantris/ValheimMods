// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using Unity.Collections;
namespace ValheimVehicles.SharedScripts
{
  using System.Collections.Generic;
  using UnityEngine;

  public struct PrefabPieceData
  {
    public GameObject Prefab;
    public HashSet<Collider> Colliders;
    public HashSet<Collider> HullColliders; // ✅ Only colliders used for convex hulls
    public List<PrefabMeshData> MeshData;

    public PrefabPieceData(GameObject prefab)
    {
      Prefab = prefab;
      Colliders = new HashSet<Collider>();
      HullColliders = new HashSet<Collider>();
      MeshData = new List<PrefabMeshData>();
    }

    public void InitializeColliders(Transform root)
    {
      var allColliders = Prefab.GetComponentsInChildren<Collider>(includeInactive: false);

      foreach (var collider in allColliders)
      {
        Colliders.Add(collider);

        // ✅ Avoids unnecessary allocations
        if (LayerHelpers.IsContainedWithinMask(collider.gameObject.layer, LayerHelpers.PhysicalLayers))
        {
          HullColliders.Add(collider);

          if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
          {
            MeshData.Add(GetMeshData(meshCollider, root));
          }
        }
      }
    }

    private static PrefabMeshData GetMeshData(MeshCollider meshCollider, Transform root)
    {
      var mesh = meshCollider.sharedMesh;
      if (mesh == null) return new PrefabMeshData();

      var localVertices = new Vector3[mesh.vertexCount];
      for (var i = 0; i < mesh.vertexCount; i++)
      {
        localVertices[i] = meshCollider.transform.InverseTransformPoint(mesh.vertices[i]);
      }

      var localPos = meshCollider.transform.position - root.position;
      return new PrefabMeshData(localPos, localVertices, mesh.triangles, Allocator.Persistent);
    }
  }
}