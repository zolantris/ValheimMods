// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts {
  public class TargetController : MonoBehaviour
  {

    public enum TargetingMode
    {
      None,
      DefendArea,
      DefendPlayer,
      // Future: AttackArea, AttackPlayer, Patrol, etc
    }

    public static readonly float DEFEND_PLAYER_SAFE_RADIUS = 1f;
    public static readonly float MAX_DEFEND_SEARCH_RADIUS = 30f;

    public static Func<Transform, bool> IsHostileCharacter = (transform1 => true);

    [Header("Defense boundaries")]
    [SerializeField] public float MaxDefendSearchRadius = MAX_DEFEND_SEARCH_RADIUS;
    [SerializeField] public float DefendPlayerSafeRadius = DEFEND_PLAYER_SAFE_RADIUS;

    [Header("Defense Areas")]
    [SerializeField] public List<DefenseArea> defendAreas = new();
    // Area defense: arbitrary points or transforms
    [SerializeField] public List<Transform> defendAreaTransforms = new();
    [SerializeField] public List<Vector3> defendAreaPoints = new();

// Player defense: player transforms
    [SerializeField] public List<Transform> defendPlayers = new();

    public TargetingMode targetingMode = TargetingMode.None;

    [SerializeField] private int maxCannonsPerEnemy = 2;

    private readonly Collider[] _enemyBuffer = new Collider[32];

    private CoroutineHandle _fireCannonsRoutine;
    public HashSet<CannonController> cannonControllers;

    private void Awake()
    {
      _fireCannonsRoutine = new CoroutineHandle(this);

      var list = GetComponentsInChildren<CannonController>(true);
      cannonControllers = list.ToHashSet();

      UpdateCannonTarget();
      
      InvokeRepeating(nameof(UpdateCannonTarget), 1f,5f);
    }

    private void OnDrawGizmos()
    {
      OnDrawGizmosSelected();
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      foreach (var area in defendAreas)
      {
        var bounds = area.GetBounds();
        Gizmos.DrawWireCube(bounds.center, bounds.size);
      }
      
      foreach (var player in defendPlayers)
      {
        Gizmos.DrawWireSphere(player.transform.position, MaxDefendSearchRadius);
      }
    }

    private IEnumerator FireCannonDelayed(CannonController cannon, float delay)
    {
      yield return new WaitForSeconds(delay);
      cannon.Fire();
    }

    private IEnumerator FireCannons()
    {
      foreach (var cannonsController in cannonControllers)
      {
        yield return FireCannonDelayed(cannonsController, 0.1f);
      }
    }

    private List<Transform> AcquireAllTargets_DefendArea()
    {
      var found = new List<Transform>();
      foreach (var area in defendAreas)
      {
        var bounds = area.GetBounds();
        int count = Physics.OverlapBoxNonAlloc(
          bounds.center,
          bounds.extents,
          _enemyBuffer,
          Quaternion.identity,
          LayerHelpers.CharacterLayerMask);

        for (int i = 0; i < count; i++)
        {
          var enemy = _enemyBuffer[i];
          if (IsValidHostile(enemy.transform) && bounds.Contains(enemy.transform.position))
          {
            if (!found.Contains(enemy.transform)) // Prevent dups
              found.Add(enemy.transform);
          }
        }
      }
      return found;
    }

    private List<Transform> AcquireAllTargets_DefendPlayer()
    {
      var targets = new List<Transform>();
      foreach (var player in defendPlayers.Where(p => p != null))
      {
        int hitCount = Physics.OverlapSphereNonAlloc(player.position, MAX_DEFEND_SEARCH_RADIUS, _enemyBuffer, LayerHelpers.CharacterLayerMask);
        for (int i = 0; i < hitCount; i++)
        {
          var hostile = _enemyBuffer[i].transform;
          if (IsValidHostile(hostile) && !IsNearAnyPlayer(hostile.position, DEFEND_PLAYER_SAFE_RADIUS))
            if (!targets.Contains(hostile)) // Prevent dups
              targets.Add(hostile);
        }
      }
      return targets;
    }

    private bool IsNearAnyPlayer(Vector3 pos, float minDist)
    {
      foreach (var player in defendPlayers)
        if (player && Vector3.Distance(player.position, pos) < minDist)
          return true;
      return false;
    }

    private bool IsValidHostile(Transform t)
    {
      if (IsHostileCharacter(t)) return true;
      
      // fallback
      // Replace this with your actual hostile-check logic
      return t.name.StartsWith("enemy");
    }

    private void AssignCannonsToTargets(List<CannonController> cannons, List<Transform> targets, int maxCannonsPerTarget = 1)
    {
      // Track how many cannons are assigned to each target
      var assignedCounts = new Dictionary<Transform, int>();
      foreach (var t in targets) assignedCounts[t] = 0;

      var unassignedCannons = new HashSet<CannonController>(cannons);

      while (unassignedCannons.Count > 0)
      {
        CannonController bestCannon = null;
        Transform bestTarget = null;
        float bestDist = float.MaxValue;

        foreach (var cannon in unassignedCannons)
        {
          foreach (var target in targets)
          {
            if (assignedCounts[target] >= maxCannonsPerTarget) continue;

            float dist = Vector3.Distance(cannon.transform.position, target.position);
            if (dist < bestDist)
            {
              bestDist = dist;
              bestCannon = cannon;
              bestTarget = target;
            }
          }
        }

        if (bestCannon != null && bestTarget != null)
        {
          bestCannon.firingTarget = bestTarget;
          assignedCounts[bestTarget]++;
          unassignedCannons.Remove(bestCannon);
        }
        else
        {
          // No more assignable cannons or targets
          break;
        }
      }

      // Optionally: for any unassigned cannons, clear their targets
      foreach (var cannon in unassignedCannons)
      {
        cannon.firingTarget = null;
      }
    }

    public void UpdateCannonTarget()
    {
      List<Transform> targets = targetingMode switch
      {
        TargetingMode.DefendPlayer => AcquireAllTargets_DefendPlayer(),
        TargetingMode.DefendArea   => AcquireAllTargets_DefendArea(),
        _                         => null
      };

      if (targets != null && targets.Count > 0)
      {
        AssignCannonsToTargets(cannonControllers.ToList(), targets, maxCannonsPerTarget: 1);
      }
      else
      {
        foreach (var cannon in cannonControllers)
          cannon.firingTarget = null;
      }
    }

    public void StartFiring()
    {
      if (_fireCannonsRoutine.IsRunning) return;
      _fireCannonsRoutine.Start(FireCannons());
    }

    public void AddCannon(CannonController controller)
    {
      cannonControllers.Add(controller);
    }

    public void RemoveCannon(CannonController controller)
    {
      cannonControllers.Remove(controller);
    }

    [Serializable]
    public struct DefenseArea
    {
      public Vector3 center;
      public Vector3 size;

      public Bounds GetBounds() => new Bounds(center, size);
    }
  }
}