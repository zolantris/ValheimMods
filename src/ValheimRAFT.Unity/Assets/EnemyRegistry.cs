#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public static class EnemyRegistry
  {
    public static readonly HashSet<GameObject> ActiveEnemies = new();
  }
}