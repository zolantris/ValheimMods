// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class PylonTestSpawner : MonoBehaviour
  {
    public GameObject PylonPrefab;
    public bool spawnFive = true;
    public bool spawnFifty;

    private void Awake()
    {
      if (PylonPrefab == null)
      {
        Debug.LogError("PylonPrefab not set on PylonTestSpawner.");
        return;
      }

      if (spawnFive)
        SpawnPylons(GeneratePositions(5, spacing: 10f));
      if (spawnFifty)
        SpawnPylons(GeneratePositions(50, spacing: 5f));

    }

    private void Start()
    {
      if (PowerNetworkController.Instance)
      {
       PowerNetworkController.Instance.RebuildPylonNetwork();
      }
    }

    private List<Vector3> GeneratePositions(int count, float spacing)
    {
      var positions = new List<Vector3>();
      var origin = transform.position;

      for (int i = 0; i < count; i++)
      {
        float x = origin.x + Random.Range(-spacing, spacing) + i * 1.25f;
        float z = origin.z + Random.Range(-spacing, spacing) + (i % 5) * 0.5f;
        float y = origin.y + Random.Range(-spacing, spacing) + i * 1.25f;
        positions.Add(new Vector3(x, origin.y, z));
      }

      return positions;
    }

    private void SpawnPylons(List<Vector3> positions)
    {
      foreach (var pos in positions)
      {
        var obj = Instantiate(PylonPrefab, pos, Quaternion.identity);
        obj.name = $"Pylon_{pos.x:F1}_{pos.z:F1}";
      }

      if (PowerNetworkController.Instance)
        PowerNetworkController.Instance.RebuildPylonNetwork();
    }
  }
}