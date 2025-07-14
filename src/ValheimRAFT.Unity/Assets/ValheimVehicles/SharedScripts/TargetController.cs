// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.SharedScripts.Helpers;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public enum CannonDirectionGroup
  {
    Forward,
    Right,
    Back,
    Left
  }

  [RequireComponent(typeof(AmmoController))]
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

    public static float FiringCooldown = 0.2f;
    public static float FiringDelayPerCannon = 0.05f;

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
    [SerializeField] public List<CannonController> allCannonControllers = new();
    [SerializeField] public List<CannonController> autoTargetControllers = new();
    [SerializeField] public List<CannonController> manualFireControllers = new();

    private readonly Collider[] _enemyBuffer = new Collider[32];
    private readonly Dictionary<int, CoroutineHandle> _manualFireCannonsRoutines = new();

    private readonly Dictionary<Transform, DefenseAreaTrigger> _playerAreaTriggers = new();

    private readonly List<GameObject> _spawnedDefenseAreaTriggers = new();

    private CoroutineHandle _acquireTargetsRoutine;
    private CoroutineHandle _autoFireCannonsRoutine;

    public Action? OnCannonListUpdated;

    private AmmoController _ammoController = null!;

    // --- Direction Group Cache ---
    private Dictionary<CannonDirectionGroup, List<CannonController>> _groupedCannons = null!;
    public IReadOnlyDictionary<CannonDirectionGroup, List<CannonController>> GroupedCannons => _groupedCannons;

    private readonly Dictionary<CannonDirectionGroup, List<CannonController>> _manualCannonGroups =
      new()
      {
        { CannonDirectionGroup.Forward, new List<CannonController>() },
        { CannonDirectionGroup.Right, new List<CannonController>() },
        { CannonDirectionGroup.Back, new List<CannonController>() },
        { CannonDirectionGroup.Left, new List<CannonController>() }
      };
    private readonly Dictionary<CannonDirectionGroup, float> _manualGroupYaw =
      new()
      {
        { CannonDirectionGroup.Forward, 0f },
        { CannonDirectionGroup.Right, 0f },
        { CannonDirectionGroup.Back, 0f },
        { CannonDirectionGroup.Left, 0f }
      };

    public IReadOnlyDictionary<CannonDirectionGroup, List<CannonController>> ManualCannonGroups => _manualCannonGroups;
    public IReadOnlyDictionary<CannonDirectionGroup, float> ManualGroupYaw => _manualGroupYaw;

    private void Awake()
    {
      _autoFireCannonsRoutine = new CoroutineHandle(this);
      _acquireTargetsRoutine = new CoroutineHandle(this);

      _ammoController = gameObject.GetOrAddComponent<AmmoController>();

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

#if UNITY_EDITOR
      // mostly for local testing.
      GetComponentsInChildren(false, allCannonControllers);
      foreach (var allCannonController in allCannonControllers)
      {
        switch (allCannonController.GetFiringMode())
        {
          case CannonFiringMode.Manual:
            manualFireControllers.Add(allCannonController);
            break;
          case CannonFiringMode.Auto:
            autoTargetControllers.Add(allCannonController);
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
#endif

      RefreshAllDefenseAreaTriggers();
      RefreshPlayerDefenseTriggers();

      RecalculateCannonGroups(); // New: Build initial grouping
    }

    private void FixedUpdate()
    {
      if (autoTargetControllers.Count > 0)
      {
        StartUpdatingAutoCannonTargets();
      }

      if (autoFire)
      {
        StartAutoFiring();
      }
    }

    public void OnEnable()
    {
      _autoFireCannonsRoutine ??= new CoroutineHandle(this);
      _acquireTargetsRoutine ??= new CoroutineHandle(this);

#if UNITY_EDITOR
      // mostly for local testing.
      GetComponentsInChildren(false, allCannonControllers);
      foreach (var allCannonController in allCannonControllers)
      {
        switch (allCannonController.GetFiringMode())
        {
          case CannonFiringMode.Manual:
            manualFireControllers.Add(allCannonController);
            break;
          case CannonFiringMode.Auto:
            autoTargetControllers.Add(allCannonController);
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
#endif
      RecalculateCannonGroups();
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
        var defenseTrigger = player.GetComponent<DefenseAreaTrigger>();
        if (defenseTrigger)
        {
          _playerAreaTriggers[player] = defenseTrigger;
          continue;
        }

        var go = new GameObject($"{PrefabNames.ValheimVehiclesPrefix}_PlayerDefenseAreaTrigger_{player.name}")
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
        var go = new GameObject($"{PrefabNames.ValheimVehiclesPrefix}_DefenseAreaTrigger_{i}");
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

    private IEnumerator FireCannonDelayed(CannonController cannon, float delay, bool isManualFire, Action<int> OnAmmoUpdate)
    {
      var ammoVariant = cannon.AmmoVariant;
      var remainingAmmo = _ammoController.GetAmmoAmountFromCannonballVariant(ammoVariant);

      // only yield if the cannon actually fires
      if (cannon.Fire(isManualFire, remainingAmmo, out var deltaAmmo))
      {
        OnAmmoUpdate.Invoke(deltaAmmo);
        yield return new WaitForSeconds(delay);
      }
    }

    private IEnumerator ManualFireCannons(int groupId)
    {
      var totalAmmoDeltaExplosive = 0;
      var totalAmmoDeltaSolid = 0;

      var list = manualFireControllers.ToList();
      foreach (var cannonsController in list)
      {
        if (!cannonsController || cannonsController.ManualFiringGroupId != groupId) continue;
        yield return FireCannonDelayed(cannonsController, FiringDelayPerCannon, true, (deltaAmmo) =>
        {
          if (cannonsController.AmmoVariant == CannonballVariant.Solid)
          {
            totalAmmoDeltaSolid += deltaAmmo;
          }
          if (cannonsController.AmmoVariant == CannonballVariant.Explosive)
          {
            totalAmmoDeltaExplosive += deltaAmmo;
          }
        });
      }

      if (list.Count > 0 && (totalAmmoDeltaExplosive > 0 || totalAmmoDeltaSolid > 0))
      {
        _ammoController.OnAmmoChanged(totalAmmoDeltaSolid, totalAmmoDeltaExplosive);
      }

      yield return new WaitForSeconds(FiringCooldown);
    }

    private IEnumerator AutoFireCannons()
    {
      var totalAmmoDeltaExplosive = 0;
      var totalAmmoDeltaSolid = 0;
      var list = autoTargetControllers.ToList();
      foreach (var cannonsController in list)
      {
        if (!cannonsController) continue;
        yield return FireCannonDelayed(cannonsController, FiringDelayPerCannon, false, (deltaAmmo) =>
        {
          if (cannonsController.AmmoVariant == CannonballVariant.Solid)
          {
            totalAmmoDeltaSolid += deltaAmmo;
          }
          if (cannonsController.AmmoVariant == CannonballVariant.Explosive)
          {
            totalAmmoDeltaExplosive += deltaAmmo;
          }
        });
      }

      if (list.Count > 0 && (totalAmmoDeltaExplosive > 0 || totalAmmoDeltaSolid > 0))
      {
        _ammoController.OnAmmoChanged(totalAmmoDeltaSolid, totalAmmoDeltaExplosive);
      }

      yield return new WaitForSeconds(FiringCooldown);
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
      return t.name.StartsWith("enemy");
    }

    private void AssignCannonsToTargets(
      List<CannonController> cannons,
      List<Transform> targets,
      int maxCannonsPerTarget = 1)
    {
      var assignedCounts = new Dictionary<Transform, int>(targets.Count);
      foreach (var t in targets)
        assignedCounts[t] = 0;

      var unassignedCannons = new List<CannonController>(cannons.Count);

      foreach (var cannon in cannons)
      {
        var assigned = cannon.firingTarget;
        if (assigned == null)
        {
          cannon.firingTarget = null;
          cannon.currentAimPoint = null;
          unassignedCannons.Add(cannon);
          continue;
        }

        var hasCounts = assignedCounts.TryGetValue(assigned, out var count);
        var canHitTarget = cannon.CanHitTargetCollider(assigned, out var aimPoint);
        var isNearTarget = Vector3.Distance(cannon.cannonShooterAimPoint.position, aimPoint) <= cannon.maxFiringRange;

        if (
          hasCounts &&
          count < maxCannonsPerTarget &&
          canHitTarget && isNearTarget)
        {
          cannon.currentAimPoint = aimPoint;
          assignedCounts[assigned]++;
          continue;
        }
        cannon.firingTarget = null;
        cannon.currentAimPoint = null;
        unassignedCannons.Add(cannon);
      }

      foreach (var cannon in unassignedCannons)
      {
        Transform bestTarget = null;
        Vector3? bestAimPoint = null;
        var bestDist = cannon.maxFiringRange + 1f;

        foreach (var t in targets)
        {
          if (assignedCounts[t] >= maxCannonsPerTarget) continue;
          if (!cannon.CanHitTargetCollider(t, out var aimPoint)) continue;

          var dist = Vector3.Distance(cannon.cannonShooterAimPoint.position, aimPoint);
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
      }
    }

    private IEnumerator UpdateCannonTargetsRoutine()
    {
      UpdateAutoCannonTargets();
      yield return new WaitForSeconds(0.2f);
    }

    private void StartUpdatingAutoCannonTargets()
    {
      if (_acquireTargetsRoutine.IsRunning) return;
      _acquireTargetsRoutine.Start(UpdateCannonTargetsRoutine());
    }

    private void UpdateAutoCannonTargets()
    {
      try
      {
        var targets = targetingMode switch
        {
          TargetingMode.DefendPlayer => AcquireAllTargets_DefendPlayer(),
          TargetingMode.DefendArea => AcquireAllTargets_DefendArea(),
          _ => null
        };

        allCannonControllers.RemoveAll(x => x == null);
        autoTargetControllers.RemoveAll(x => x == null);

        targets?.RemoveAll(x => x == null);

        if (targets != null && targets.Count > 0 && autoTargetControllers.Count > 0)
        {
          AssignCannonsToTargets(autoTargetControllers.ToList(), targets, maxCannonsPerEnemy);
        }
        else
        {
          foreach (var cannon in allCannonControllers.ToList())
          {
            if (!cannon) continue;
            cannon.firingTarget = null;
          }
        }
      }
      catch (Exception e)
      {
        LoggerProvider.LogDebugDebounced($"Error while updating auto cannon targets\n{e}");
      }
    }

    public void StartManualFiring(int groupId = 0)
    {
      if (!_manualFireCannonsRoutines.TryGetValue(groupId, out var routine))
      {
        routine = new CoroutineHandle(this);
      }
      if (routine.IsRunning || manualFireControllers.Count == 0) return;
      routine.Start(ManualFireCannons(groupId));
    }

    public void StartAutoFiring()
    {
      if (_autoFireCannonsRoutine.IsRunning || autoTargetControllers.Count == 0) return;
      _autoFireCannonsRoutine.Start(AutoFireCannons());
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

    private void AddManualCannonToGroup(CannonController cannon)
    {
      var group = GetDirectionGroup(transform, cannon.transform);
      _manualCannonGroups[group].Add(cannon);
      cannon.CurrentManualDirectionGroup = group;
    }

    private void RemoveManualCannonFromGroup(CannonController cannon)
    {
      if (cannon.CurrentManualDirectionGroup.HasValue)
      {
        var group = cannon.CurrentManualDirectionGroup.Value;
        _manualCannonGroups[group].Remove(cannon);
        cannon.CurrentManualDirectionGroup = null;
      }
      else
      {
        // Fallback: rare
        foreach (var kv in _manualCannonGroups)
          kv.Value.Remove(cannon);
      }
    }

    /// <summary>
    /// Call if you move/re-parent a manual cannon at runtime and need to regroup.
    /// </summary>
    public void RefreshManualCannonGroup(CannonController cannon)
    {
      RemoveManualCannonFromGroup(cannon);
      AddManualCannonToGroup(cannon);
    }

    // Yaw per group
    public void SetManualGroupYaw(CannonDirectionGroup group, float yaw)
    {
      _manualGroupYaw[group] = yaw;
    }
    public float GetManualGroupYaw(CannonDirectionGroup group)
    {
      return _manualGroupYaw[group];
    }

    public void StartManualGroupFiring(CannonDirectionGroup group)
    {
      if (!_manualFireCannonsRoutines.TryGetValue((int)group, out var routine))
        routine = new CoroutineHandle(this);
      if (routine.IsRunning || _manualCannonGroups[group].Count == 0) return;
      routine.Start(ManualFireCannonsGroupCoroutine(group));
    }

    /// <summary>
    /// Coroutine: fires all cannons in the group in staggered sequence, tracks ammo usage, applies OnAmmoChanged at the end.
    /// </summary>
    private IEnumerator ManualFireCannonsGroupCoroutine(CannonDirectionGroup group)
    {
      var totalAmmoDeltaSolid = 0;
      var totalAmmoDeltaExplosive = 0;
      var yaw = _manualGroupYaw[group];
      var cannons = _manualCannonGroups[group].ToList();

      foreach (var cannon in cannons)
      {
        if (!cannon) continue;

        cannon.SetManualYaw(yaw);

        if (cannon.Fire(
              true,
              _ammoController.GetAmmoAmountFromCannonballVariant(cannon.AmmoVariant),
              out var deltaAmmo))
        {
          if (cannon.AmmoVariant == CannonballVariant.Solid)
            totalAmmoDeltaSolid += deltaAmmo;
          else if (cannon.AmmoVariant == CannonballVariant.Explosive)
            totalAmmoDeltaExplosive += deltaAmmo;

          // Stagger delay only if fired
          yield return new WaitForSeconds(FiringDelayPerCannon);
        }
      }

      if (cannons.Count > 0 && (totalAmmoDeltaSolid != 0 || totalAmmoDeltaExplosive != 0))
      {
        _ammoController.OnAmmoChanged(totalAmmoDeltaSolid, totalAmmoDeltaExplosive);
      }

      yield return new WaitForSeconds(FiringCooldown);
    }

    public void AddCannon(CannonController controller)
    {
      allCannonControllers.Add(controller);
      switch (controller.GetFiringMode())
      {
        case CannonFiringMode.Manual:
          manualFireControllers.Add(controller);
          AddManualCannonToGroup(controller);
          break;
        case CannonFiringMode.Auto:
          autoTargetControllers.Add(controller);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      OnCannonListUpdated?.Invoke();
    }

    public void RemoveCannon(CannonController controller)
    {
      allCannonControllers.Remove(controller);
      switch (controller.GetFiringMode())
      {
        case CannonFiringMode.Manual:
          manualFireControllers.Remove(controller);
          RemoveManualCannonFromGroup(controller);
          break;
        case CannonFiringMode.Auto:
          autoTargetControllers.Remove(controller);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      OnCannonListUpdated?.Invoke();
    }

    // --- GROUPING LOGIC ---

    private static CannonDirectionGroup GetDirectionGroup(Transform reference, Transform cannon)
    {
      var toCannon = cannon.position - reference.position;
      toCannon.y = 0f;
      if (toCannon.sqrMagnitude < 1e-4f)
        return CannonDirectionGroup.Forward;

      var local = reference.InverseTransformDirection(toCannon.normalized);
      var angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
      angle = (angle + 360f) % 360f;

      if (angle >= 315f || angle < 45f)
        return CannonDirectionGroup.Forward;
      if (angle >= 45f && angle < 135f)
        return CannonDirectionGroup.Right;
      if (angle >= 135f && angle < 225f)
        return CannonDirectionGroup.Back;
      return CannonDirectionGroup.Left;
    }

    private void RecalculateCannonGroups()
    {
      // O(N) - Efficient, no allocations except per-list
      _groupedCannons = new Dictionary<CannonDirectionGroup, List<CannonController>>
      {
        { CannonDirectionGroup.Forward, new List<CannonController>() },
        { CannonDirectionGroup.Right, new List<CannonController>() },
        { CannonDirectionGroup.Back, new List<CannonController>() },
        { CannonDirectionGroup.Left, new List<CannonController>() }
      };
      foreach (var cannon in allCannonControllers)
      {
        if (cannon == null) continue;
        var group = GetDirectionGroup(transform, cannon.transform);
        _groupedCannons[group].Add(cannon);
      }
    }

    // Example public API for outside use (can expand to support N directions easily)
    public IReadOnlyList<CannonController> GetCannonsByDirection(CannonDirectionGroup group)
    {
      return _groupedCannons.TryGetValue(group, out var list) ? list : Array.Empty<CannonController>();
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