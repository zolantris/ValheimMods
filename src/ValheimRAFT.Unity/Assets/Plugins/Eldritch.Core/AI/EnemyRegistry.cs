#region

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#endregion

namespace Eldritch.Core
{
  public static class EnemyRegistry
  {
    public static HashSet<GameObject> ActiveEnemies = new();

    public static bool TryGetClosestEnemy(Transform self, float maxDistance, [NotNullWhen(true)] out Transform? target)
    {
      var shouldPrune = false;
      target = null;
      var closestDist = maxDistance;
      foreach (var obj in ActiveEnemies)
      {
        if (obj == null)
        {
          shouldPrune = true;
          continue;
        }
        // skip self transform
        if (obj.transform == self) continue;

        var dist = Vector3.Distance(obj.transform.position, self.position);
        if (dist < closestDist)
        {
          target = obj.transform;
          closestDist = dist;
        }
      }
      if (shouldPrune)
      {
        ActiveEnemies.RemoveWhere(x => x == null);
      }

      if (closestDist >= maxDistance || target == null)
      {
        return false;
      }

      return true;
    }
  }
}