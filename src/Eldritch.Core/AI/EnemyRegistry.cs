#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Eldritch.Core
{
  public static class EnemyRegistry
  {
    public static readonly HashSet<GameObject> ActiveEnemies = new();
  }
}