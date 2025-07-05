// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class TargetController : MonoBehaviour
  {
    public enum TargetingMode
    {
      None,
      DefendArea,
      DefendPlayer
      // Future: AttackArea, AttackPlayer, Patrol, etc
    }

    public static float DEFEND_PLAYER_SAFE_RADIUS = 3f;
    public static float MAX_DEFEND_SEARCH_RADIUS = 30f;
    public static Vector3 MAX_DEFEND_AREA_SEARCH = new(30f, 40f, 30f);

    public static Func<Transform, bool> IsHostileCharacter = transform1 => true;

    public static bool canShootPlayer = true;

    [Header("Defense boundaries")]
    [SerializeField] public float MaxDefendSearchRadius = MAX_DEFEND_SEARCH_RADIUS;
    [SerializeField] public float DefendPlayerSafeRadius = DEFEND_PLAYER_SAFE_RADIUS;

    [Header("Defense Areas")]
    [SerializeField] public List<DefenseArea> defendAreas = new();

// Player defense: player transforms
    [Tooltip("Player defense")]
    [SerializeField] public List<Transform> defendPlayers = new();

    public TargetingMode targetingMode = TargetingMode.None;

    [SerializeField] public int maxCannonsPerEnemy = 2;
    [SerializeField] public bool autoFire;

    public float FiringDelay = 0.1f;
    public bool CanFire;
    [SerializeField] public List<CannonController> cannonControllers = new();

    private readonly Collider[] _enemyBuffer = new Collider[32];

    private readonly Dictionary<Transform, DefenseAreaTrigger> _playerAreaTriggers = new();

    private readonly List<GameObject> _spawnedDefenseAreaTriggers = new();

    private CoroutineHandle _fireCannonsRoutine;

    private void Awake()
    {
      _fireCannonsRoutine = new CoroutineHandle(this);

      IsHostileCharacter = t =>
      {
#if !UNITY_EDITOR && !UNITY_2022
        var character = t.GetComponent<Character>();
        if (character == null) return false;
        if (canShootPlayer && character.IsPlayer() && !character.IsDead()) return true;
        return !character.IsPlayer() && !character.IsTamed(5f) && !character.IsDead();
        #else
        if (!t) return false;
        var tName = t.name;
        var rootName = t.root.name;
        if (rootName.StartsWith("player") || rootName.Contains("friendly") || tName.StartsWith("player_collider")) return false;
        return true;
#endif
        return true;
      };

      GetComponentsInChildren(false, cannonControllers);

      UpdateCannonTarget();

      RefreshAllDefenseAreaTriggers();
      RefreshPlayerDefenseTriggers();
    }

    private void FixedUpdate()
    {
      UpdateCannonTarget();

      if (autoFire)
      {
        StartFiring();
      }
    }

    private void OnDrawGizmos()
    {
      OnDrawGizmosSelected();
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;

      foreach (var player in defendPlayers)
      {
        Gizmos.DrawWireSphere(player.transform.position, MaxDefendSearchRadius);
      }

      foreach (var area in defendAreas)
        Gizmos.DrawWireCube(area.center, area.size);
    }

    public void AddDefenseArea(Vector3 center, Vector3 size)
    {
      defendAreas.Add(new DefenseArea { center = center, size = size });
      RefreshAllDefenseAreaTriggers();
    }

    public void RemoveDefenseArea(int index)
    {
      if (index < 0 || index >= defendAreas.Count) return;
      defendAreas.RemoveAt(index);
      RefreshAllDefenseAreaTriggers();
    }

    public void RefreshPlayerDefenseTriggers()
    {
      // Cleanup first
      foreach (var pair in _playerAreaTriggers)
      {
        if (pair.Value && pair.Value.gameObject)
          Destroy(pair.Value.gameObject);
      }
      _playerAreaTriggers.Clear();

      // Create for each player
      foreach (var player in defendPlayers)
      {
        if (player == null) continue;
        var go = new GameObject($"PlayerDefenseAreaTrigger_{player.name}")
        {
          transform = { position = player.position, parent = player.transform },
          layer = LayerHelpers.CharacterTriggerLayer
        };

        var sphere = go.AddComponent<SphereCollider>();
        var playerColliders = player.GetComponentsInChildren<Collider>(true);
        foreach (var playerCollider in playerColliders)
        {
          Physics.IgnoreCollision(sphere, playerCollider, true);
        }

        sphere.includeLayers = LayerHelpers.CharacterLayerMask;
        sphere.radius = MaxDefendSearchRadius;
        sphere.isTrigger = true;

        var trigger = go.AddComponent<DefenseAreaTrigger>();

        _playerAreaTriggers[player] = trigger;
      }
    }

    public void RefreshAllDefenseAreaTriggers()
    {
      // Clean up previous
      foreach (var go in _spawnedDefenseAreaTriggers)
      {
        if (go) Destroy(go);
      }
      _spawnedDefenseAreaTriggers.Clear();

      // Recreate for each area
      for (var i = 0; i < defendAreas.Count; i++)
      {
        var area = defendAreas[i];

        // Spawn
        var go = new GameObject($"DefenseAreaTrigger_{i}");
        go.transform.SetParent(null);
        go.transform.position = area.center;
        go.transform.localScale = Vector3.one;

        var box = go.AddComponent<BoxCollider>();
        box.includeLayers = LayerHelpers.CharacterLayerMask;
        box.size = area.size;
        box.isTrigger = true;

        var trigger = go.AddComponent<DefenseAreaTrigger>();

        // Store reference (optional, for tracking)
        area.trigger = trigger;
        defendAreas[i] = area;

        _spawnedDefenseAreaTriggers.Add(go);
      }
    }

    private IEnumerator FireCannonDelayed(CannonController cannon, float delay)
    {
      yield return new WaitForSeconds(delay);
      cannon.Fire();
    }

    private IEnumerator FireCannons()
    {
      CanFire = false;
      foreach (var cannonsController in cannonControllers.ToList())
      {
        if (!cannonsController) continue;
        yield return FireCannonDelayed(cannonsController, 0.1f);
      }

      yield return new WaitForSeconds(0.1f);

      CanFire = true;
    }

    private List<Transform> AcquireAllTargets_DefendArea()
    {
      var found = new List<Transform>();
      var copy = defendAreas.ToList();
      for (var index = 0; index < copy.Count; index++)
      {
        var area = copy[index];
        if (area.trigger == null)
        {
          defendAreas.RemoveAt(index);
          continue;
        }
        // You can prune here if you want to clean up destroyed/invalid enemies
        area.trigger.Prune();
        foreach (var t in area.trigger.CurrentEnemies)
        {
          if (IsValidHostile(t) && !found.Contains(t))
            found.Add(t);
        }
      }
      return found;
    }

    private List<Transform> AcquireAllTargets_DefendPlayer()
    {
      var found = new HashSet<Transform>();
      foreach (var player in defendPlayers.ToList())
      {
        if (player == null) continue;
        if (!_playerAreaTriggers.TryGetValue(player, out var trigger) || trigger == null) continue;

        trigger.Prune(); // Clean out null/dead refs

        foreach (var t in trigger.CurrentEnemies.ToList())
        {
          if (IsValidHostile(t) && Vector3.Distance(player.position, t.position) > DefendPlayerSafeRadius)
            found.Add(t);
        }
      }
      return found.ToList();
    }

    private bool IsNearAnyPlayer(Vector3 pos, float minDist)
    {
      foreach (var player in defendPlayers.ToList())
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

    private void AssignCannonsToTargets(
    List<CannonController> cannons,
    List<Transform> targets,
    int maxCannonsPerTarget = 1)
    {
        // 1. Map to keep track of cannons per target.
        var assignedCounts = new Dictionary<Transform, int>(targets.Count);
        foreach (var t in targets)
            assignedCounts[t] = 0;

        var unassignedCannons = new List<CannonController>(cannons.Count);

        // 2. Retain existing assignments if possible (O(N))
        foreach (var cannon in cannons)
        {
            var assigned = cannon.firingTarget;
            if (
                assigned != null &&
                assignedCounts.TryGetValue(assigned, out var count) &&
                count < maxCannonsPerTarget &&
                cannon.CanHitTargetCollider(assigned, out var aimPoint) &&
                Vector3.Distance(cannon.transform.position, aimPoint) <= cannon.maxFiringRange)
            {
                cannon.currentAimPoint = aimPoint;
                assignedCounts[assigned]++;
                continue;
            }
            cannon.firingTarget = null;
            cannon.currentAimPoint = null;
            unassignedCannons.Add(cannon);
        }

        // 3. Assign unassigned cannons to best targets (O(N*M))
        foreach (var cannon in unassignedCannons)
        {
            Transform bestTarget = null;
            Vector3 bestAimPoint = default;
            float bestDist = float.MaxValue;

            foreach (var t in targets)
            {
                if (assignedCounts[t] >= maxCannonsPerTarget) continue;
                if (!cannon.CanHitTargetCollider(t, out var aimPoint)) continue;
                
                float dist = Vector3.SqrMagnitude(cannon.transform.position - aimPoint);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = t;
                    bestAimPoint = aimPoint;
                }
            }

            if (bestTarget != null)
            {
                cannon.firingTarget = bestTarget;
                cannon.currentAimPoint = bestAimPoint;
                assignedCounts[bestTarget]++;
            }
            // else
            // {
            //     cannon.firingTarget = null;
            //     cannon.currentAimPoint = null;
            // }
        }
    }


    public void UpdateCannonTarget()
    {
      var targets = targetingMode switch
      {
        TargetingMode.DefendPlayer => AcquireAllTargets_DefendPlayer(),
        TargetingMode.DefendArea => AcquireAllTargets_DefendArea(),
        _ => null
      };

      cannonControllers.RemoveAll(x => x == null);
      targets?.RemoveAll(x => x == null);

      if (targets != null && targets.Count > 0 && cannonControllers.Count > 0)
      {
        AssignCannonsToTargets(cannonControllers.ToList(), targets, maxCannonsPerEnemy);
      }
      else
      {
        foreach (var cannon in cannonControllers.ToList())
        {
          if (!cannon) continue;
          cannon.firingTarget = null;
        }
      }
    }

    public void StartFiring()
    {
      if (_fireCannonsRoutine.IsRunning) return;
      _fireCannonsRoutine.Start(FireCannons());
    }

    public void AddPlayer(Transform player)
    {
      if (player == null || defendPlayers.Contains(player)) return;
      defendPlayers.Add(player);
      RefreshPlayerDefenseTriggers();
    }
    public void RemovePlayer(Transform player)
    {
      defendPlayers.Remove(player);
      RefreshPlayerDefenseTriggers();
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

      [NonSerialized] public DefenseAreaTrigger trigger; // Runtime-only
      public Bounds GetBounds()
      {
        return new Bounds(center, size);
      }
    }
  }
}