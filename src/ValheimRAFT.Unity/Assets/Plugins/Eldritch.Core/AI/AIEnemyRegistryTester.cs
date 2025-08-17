using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Eldritch.Core
{
  public class AIEnemyRegistryTester : MonoBehaviour
  {
    public List<GameObject> enemiesTracker = new();
    private void Awake()
    {
      GetAllEnemies();
    }

    private List<GameObject> GetChildGameObjects(Transform parent)
    {
      var children = new List<GameObject>();
      for (var i = 0; i < parent.childCount; i++)
        children.Add(parent.GetChild(i).gameObject);
      return children;
    }

    [ContextMenu("Update all enemies")]
    public void GetAllEnemies()
    {
      enemiesTracker = GetChildGameObjects(transform);
      EnemyRegistry.ActiveEnemies = enemiesTracker.ToHashSet();
    }
  }
}