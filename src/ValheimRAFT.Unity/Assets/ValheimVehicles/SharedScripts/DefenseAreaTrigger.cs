// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
// ReSharper disable UseNullableReferenceTypesAnnotationSyntax

#endregion

namespace ValheimVehicles.SharedScripts
{

  public class DefenseAreaTrigger : MonoBehaviour
  {
    public Collider triggerCollider;
    private readonly HashSet<Transform> _currentEnemies = new();
    public IEnumerable<Transform> CurrentEnemies => _currentEnemies;
    public void Awake()
    {
      triggerCollider = GetComponent<Collider>();
      triggerCollider.gameObject.layer = LayerHelpers.CharacterTriggerLayer;
    }

    private void OnTriggerEnter(Collider other)
    {
      // Find the root (or use a unique component, e.g. EnemyController)
      var enemyRoot = GetEnemyRootTransform(other);
      if (enemyRoot != null)
        _currentEnemies.Add(enemyRoot);
    }

    private void OnTriggerExit(Collider other)
    {
      var enemyRoot = GetEnemyRootTransform(other);
      if (enemyRoot != null)
        _currentEnemies.Remove(enemyRoot);
    }

    [CanBeNull] private Transform GetEnemyRootTransform(Collider col)
    {
#if !UNITY_EDITOR && !UNITY_2022
      // For Valheim: Prefer Character (Valheim's NPC/monster/player class)
      var character = col.GetComponentInParent<Character>();
      if (character != null && !character.IsPlayer() && !character.IsTamed(5f) && !character.IsDead())
      {
        return character.transform;
      }
      else
      {
        return null;
      }
#endif
      return FindParent(col.transform, t => t.name.StartsWith("Enemy"));
    }

    public static Transform FindParent(Transform t, Func<Transform, bool> predicate)
    {
      while (t != null)
      {
        if (predicate(t))
          return t;
        t = t.parent;
      }
      return null;
    }

    // Optional: Clean up dead/invalids periodically or on demand
    public void Prune()
    {
      _currentEnemies.RemoveWhere(t =>
        {
#if !UNITY_EDITOR && !UNITY_2022
          if (t != null)
          {
            var character = t.GetComponent<Character>();
            if (character != null && character.IsDead())
            {
              return true;
            }
          }
#endif
          if (t == null) return true;
          if (t.gameObject != null && !t.gameObject.activeInHierarchy)
          {
            return true;
          }
          return false;
        }
        /* || IsDead(t) */);
    }
  }

}