// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.RPC;
using ValheimVehicles.SharedScripts.Helpers;
using ValheimVehicles.Structs;
using Random = UnityEngine.Random;

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
    [SerializeField] public List<CannonController> autoTargetCannonControllers = new();
    [SerializeField] public List<CannonController> manualFireControllers = new();

    private readonly Collider[] _enemyBuffer = new Collider[32];
    private readonly Dictionary<int, CoroutineHandle> _manualFireCannonsRoutines = new();

    private readonly Dictionary<Transform, DefenseAreaTrigger> _playerAreaTriggers = new();

    private readonly List<GameObject> _spawnedDefenseAreaTriggers = new();

    private CoroutineHandle _acquireTargetsRoutine;
    private CoroutineHandle _autoFireCannonsRoutine;

    public Action? OnCannonListUpdated;

    public AmmoController ammoController = null!;

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
    private readonly Dictionary<CannonDirectionGroup, float> _manualGroupTilt =
      new()
      {
        { CannonDirectionGroup.Forward, 0f },
        { CannonDirectionGroup.Right, 0f },
        { CannonDirectionGroup.Back, 0f },
        { CannonDirectionGroup.Left, 0f }
      };

    public IReadOnlyDictionary<CannonDirectionGroup, List<CannonController>> ManualCannonGroups => _manualCannonGroups;
    public IReadOnlyDictionary<CannonDirectionGroup, float> ManualGroupTilt => _manualGroupTilt;
    public CannonFiringHotkeys _cannonFiringHotkeys;

#if !UNITY_2022 && !UNITY_EDITOR
    private VehiclePiecesController _piecesController;
#endif
    public Transform _forwardTransform { get; set; }
    public ZNetView m_nview;

    private void Awake()
    {
      m_nview = GetComponentInParent<ZNetView>();

      _autoFireCannonsRoutine = new CoroutineHandle(this);
      _acquireTargetsRoutine = new CoroutineHandle(this);

      ammoController = gameObject.GetOrAddComponent<AmmoController>();

      SetupForwardTransform();

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

    public void SetupForwardTransform()
    {
#if !UNITY_2022 && !UNITY_EDITOR
      _piecesController = GetComponent<VehiclePiecesController>();
      _forwardTransform = _piecesController != null && _piecesController.MovementController != null ? _piecesController.MovementController.ShipDirection : transform;
#else
      ForwardTransform = transform
#endif
    }

    /// <summary>
    /// Ensures some components are not retained.
    /// </summary>
    protected internal virtual void OnDestroy()
    {
      if (ammoController != null)
      {
        Destroy(ammoController);
      }
      if (_cannonFiringHotkeys != null)
      {
        Destroy(_cannonFiringHotkeys);
      }
    }

    private void FixedUpdate()
    {
      if (autoTargetCannonControllers.Count > 0)
      {
        StartUpdatingAutoCannonTargets();
      }

#if !UNITY_2022 && !UNITY_EDITOR
      if (autoFire && m_nview && m_nview.IsOwner())
      {
        Request_AutoFireCannons();
      }
#else
      if (autoFire)
      {
        StartAutoFiring();
      }
#endif
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

  #region RPC NETWORKING

#if !UNITY_2022 && !UNITY_EDITOR
    public void Request_AutoFireCannons()
    {
      if (autoTargetCannonControllers.Count < 1) return;
      if (!m_nview)
      {
        m_nview = GetComponentInParent<ZNetView>();
      }
      if (!m_nview)
      {
        LoggerProvider.LogWarning("cannonController missing znetview!");
        return;
      }
      if (!m_nview.IsValid()) return;
      var zdo = m_nview.GetZDO();
      var cannonFireDataList = CannonFireData.CreateListOfCannonFireDataFromTargetController(this, autoTargetCannonControllers);
      if (cannonFireDataList.Count == 0) return;

      var pkg = new ZPackage();
      pkg.Write(zdo.m_uid);
      CannonFireData.WriteListToPackage(pkg, cannonFireDataList);

      AutoFireCannonGroup_RPC.Send(ZNetView.Everybody, pkg);
    }

    public void Request_FireManualCannons(CannonDirectionGroup[] cannonGroups)
    {
      foreach (var cannonGroup in cannonGroups)
      {
        Request_FireManualCannons(cannonGroup);
      }
    }

    public ZNetView? GetNetView()
    {
      if (!m_nview)
      {
        if (_piecesController)
        {
          m_nview = _piecesController.Manager.m_nview;
        }
        else
        {
          m_nview = GetComponentInParent<ZNetView>();
        }
      }
      if (!m_nview)
      {
        LoggerProvider.LogWarning("cannonController missing znetview!");
      }

      return m_nview;
    }

    public void Request_FireManualCannons(CannonDirectionGroup cannonGroupId)
    {
      if (ammoController.ExplosiveAmmo < 1 && ammoController.SolidAmmo < 1) return;
      var nv = GetNetView();
      if (nv == null || !nv.IsValid()) return;

      var zdo = m_nview.GetZDO();
      if (zdo == null) return;

      var cannonTilt = _manualGroupTilt[cannonGroupId];
      var firingCannonGroup = _manualCannonGroups[cannonGroupId];

      var cannonFireDataList = CannonFireData.CreateListOfCannonFireDataFromTargetController(this, firingCannonGroup);
      if (cannonFireDataList.Count == 0) return;

      var pkg = new ZPackage();

      pkg.Write(zdo.m_uid);
      pkg.Write((int)cannonGroupId);
      pkg.Write(cannonTilt);

      CannonFireData.WriteListToPackage(pkg, cannonFireDataList);

      FireCannonGroup_RPC.Send(ZNetView.Everybody, pkg);
    }

    public static RPCEntity FireCannonGroup_RPC = null!;
    public static RPCEntity AutoFireCannonGroup_RPC = null!;

    public static void RegisterCannonControllerRPCs()
    {
      FireCannonGroup_RPC = RPCManager.RegisterRPC(nameof(RPC_FireAllCannonsInGroup), RPC_FireAllCannonsInGroup);
      AutoFireCannonGroup_RPC = RPCManager.RegisterRPC(nameof(RPC_AutoFireAllCannons), RPC_AutoFireAllCannons);
    }

    public static IEnumerator RPC_AutoFireAllCannons(long senderId, ZPackage package)
    {
      package.SetPos(0);

      var targetControllerZDOID = package.ReadZDOID();

      GameObject? targetControllerObj = null;
      yield return FindInstanceAsync(targetControllerZDOID, x => targetControllerObj = x);
      if (targetControllerObj == null) yield break;

      // allocate/read the rest of the package after finding object.
      var cannonFireDataList = CannonFireData.ReadListFromPackage(package);

      // can be a child for the handheld version.
      var targetController = targetControllerObj.GetComponentInChildren<TargetController>();
      if (!targetController)
      {
        LoggerProvider.LogWarning($"targetController with zdoid: {targetControllerZDOID} not found on object {targetControllerObj.name}. CannonController should exist otherwise we cannot instantiate cannonball without collision issues");
        yield break;
      }

      // for updates the group tilt from the host that requested to sync it to a client.
      targetController.StartAutoFiring(cannonFireDataList);
    }

    public static TargetController? GetTargetControllerFromNetViewRoot(GameObject obj)
    {
      TargetController? targetController = null;
      // can be a child for the handheld version.
      if (PrefabNames.IsVehicle(obj.name))
      {
        var vehicle = obj.GetComponent<VehicleManager>();
        if (vehicle == null || vehicle.PiecesController == null) return null;
        targetController = vehicle.PiecesController.targetController;
      }
      else
      {
        targetController = obj.GetComponentInChildren<TargetController>();
      }

      return targetController;
    }

    public static IEnumerator FindInstanceAsync(ZDOID zdoid, Action<GameObject> callback)
    {
      var obj = ZNetScene.instance.FindInstance(zdoid);
      if (obj == null)
      {
        var stopwatch = Stopwatch.StartNew();
        var lastUpdateTime = 0;
        ZDOMan.instance.RequestZDO(zdoid);
        while (stopwatch.ElapsedMilliseconds < 1000 && obj == null)
        {
          obj = ZNetScene.instance.FindInstance(zdoid);
          if (stopwatch.ElapsedMilliseconds - lastUpdateTime > 10)
          {
            ZDOMan.instance.RequestZDO(zdoid);
          }
          yield return null;
        }

        if (obj == null)
        {
          LoggerProvider.LogError($"Could not find obj with ZDOID {zdoid}");
          yield break;
        }
      }

      callback.Invoke(obj);
    }

    public static IEnumerator RPC_FireAllCannonsInGroup(long senderId, ZPackage pkg)
    {
      pkg.SetPos(0);
      var zdoid = pkg.ReadZDOID();

      GameObject? targetControllerObj = null;
      yield return FindInstanceAsync(zdoid, x => targetControllerObj = x);
      if (targetControllerObj == null) yield break;

      // allocate the other data if the zdo exists.
      var cannonGroupId = (CannonDirectionGroup)pkg.ReadInt();
      var cannonTilt = pkg.ReadSingle();
      var firingDataList = CannonFireData.ReadListFromPackage(pkg);

      var targetController = GetTargetControllerFromNetViewRoot(targetControllerObj);
      if (targetController == null)
      {
        LoggerProvider.LogWarning($"targetController {zdoid} not found. CannonController should exist otherwise we cannot instantiate cannonball without collision issues");
        yield break;
      }

      // for updates the group tilt from the host that requested to sync it to a client.
      targetController.SetManualGroupTilt(cannonGroupId, cannonTilt);
      targetController.StartManualGroupFiring(firingDataList, cannonGroupId);
    }
#endif

  #endregion

    private IEnumerator FireCannonDelayed(CannonController cannon, CannonFireData data, float delay, bool isManualFire)
    {
      // only yield if the cannon actually fires
      if (cannon.Fire(data, ammoController.GetAmmoAmountFromCannonballVariant(data.ammoVariant), isManualFire))
      {
        yield return new WaitForSeconds(delay);
      }
    }

    private IEnumerator AutoFireCannons(List<CannonFireData> cannonFiringDataList)
    {
      var objToCannonControllerMap = autoTargetCannonControllers.ToDictionary(x => x.gameObject, x => x);
      var totalAmmoDeltaExplosive = 0;
      var totalAmmoDeltaSolid = 0;

      foreach (var cannonFireData in cannonFiringDataList)
      {
        var cannonControllerObj = ZNetScene.instance.FindInstance(cannonFireData.cannonControllerZDOID);
        if (cannonControllerObj == null) continue;
        // optimistic getter for cannonControllers. But possibly not valid.
        if (!objToCannonControllerMap.TryGetValue(cannonControllerObj, out var cannonController))
        {
          cannonController = cannonControllerObj.GetComponent<CannonController>();
          if (!cannonController)
          {
            continue;
          }
        }
        if (cannonController == null) continue;
        yield return FireCannonDelayed(cannonController, cannonFireData, FiringDelayPerCannon, false);
        AmmoController.SubtractAmmoByVariant(cannonFireData.ammoVariant, cannonFireData.allocatedAmmo, ref totalAmmoDeltaSolid, ref totalAmmoDeltaExplosive);
      }

      if (cannonFiringDataList.Count > 0 && m_nview.IsOwner())
      {
        ammoController.OnAmmoChanged(Math.Abs(totalAmmoDeltaSolid), Mathf.Abs(totalAmmoDeltaExplosive));
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
        autoTargetCannonControllers.RemoveAll(x => x == null);

        targets?.RemoveAll(x => x == null);

        if (targets != null && targets.Count > 0 && autoTargetCannonControllers.Count > 0)
        {
          AssignCannonsToTargets(autoTargetCannonControllers.ToList(), targets, maxCannonsPerEnemy);
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

    public void StartAutoFiring(List<CannonFireData> cannonFireDataList)
    {
      if (_autoFireCannonsRoutine.IsRunning || autoTargetCannonControllers.Count == 0) return;
      _autoFireCannonsRoutine.Start(AutoFireCannons(cannonFireDataList));
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

    public List<CannonController> GetCannonManualFiringGroup(CannonDirectionGroup group)
    {
      return _manualCannonGroups[group];
    }

    private void AddManualCannonToGroup(CannonController cannon)
    {
      var group = GetDirectionGroup(_forwardTransform, cannon.transform);

      // Use TryGetValue for perf, don’t double index.
      if (!_manualCannonGroups.TryGetValue(group, out var cannonGroup) || cannonGroup == null)
      {
        cannonGroup = new List<CannonController>();
        _manualCannonGroups[group] = cannonGroup;
      }

      cannonGroup.Add(cannon);

      // Filter out nulls, then sort.
      var sortedGroup = cannonGroup
        .Where(x => x != null)
        .OrderBy(c =>
          (transform.InverseTransformPoint(c.transform.position).x,
            transform.InverseTransformPoint(c.transform.position).z))
        .ToList();

      _manualCannonGroups[group] = sortedGroup;
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
    public void SetManualGroupTilt(CannonDirectionGroup group, float yaw)
    {
      _manualGroupTilt[group] = yaw;
      var cannons = _manualCannonGroups[group];
      foreach (var cannon in cannons)
      {
        if (cannon == null) continue;
        cannon.SetManualTilt(yaw);
      }
    }

    public float GetManualGroupTilt(CannonDirectionGroup group)
    {
      return _manualGroupTilt[group];
    }

    public void StartManualGroupFiring(List<CannonFireData> cannonFireDataList, CannonDirectionGroup group)
    {
      if (!_manualFireCannonsRoutines.TryGetValue((int)group, out var routine))
        routine = new CoroutineHandle(this);
      if (routine.IsRunning || _manualCannonGroups[group].Count == 0) return;
      routine.Start(ManualFireCannonsGroupCoroutine(cannonFireDataList, group));
    }

    /// <summary>
    /// Coroutine: fires all cannons in the group in staggered sequence, tracks ammo usage, applies OnAmmoChanged at the end.
    /// </summary>
    private IEnumerator ManualFireCannonsGroupCoroutine(List<CannonFireData> cannonFireDataList, CannonDirectionGroup group)
    {
      var totalAmmoDeltaSolid = 0;
      var totalAmmoDeltaExplosive = 0;
      var tilt = _manualGroupTilt[group];
      var cannons = _manualCannonGroups[group].ToList();

      for (var i = 0; i < cannonFireDataList.Count; i++)
      {
        var data = cannonFireDataList[i];
        var cannonControllerObj = ZNetScene.instance.FindInstance(data.cannonControllerZDOID);
        if (!cannonControllerObj) continue;
        var cannon = cannonControllerObj.GetComponent<CannonController>();
        if (!cannon) continue;

        cannon.SetManualTilt(tilt);

        if (cannon.Fire(data, ammoController.GetAmmoAmountFromCannonballVariant(data.ammoVariant), true)) // true = isManualFiring
        {
          AmmoController.SubtractAmmoByVariant(data.ammoVariant, data.allocatedAmmo, ref totalAmmoDeltaSolid, ref totalAmmoDeltaExplosive);
          // Ammo logic (optional, if ammo is spent inside CannonController now)
          yield return new WaitForSeconds(FiringDelayPerCannon);
        }
      }

      if (cannons.Count > 0 && m_nview.IsOwner())
      {
        ammoController.OnAmmoChanged(Math.Abs(totalAmmoDeltaSolid), Mathf.Abs(totalAmmoDeltaExplosive));
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
          autoTargetCannonControllers.Add(controller);
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
          autoTargetCannonControllers.Remove(controller);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      OnCannonListUpdated?.Invoke();
    }

    // --- GROUPING LOGIC ---

    private static CannonDirectionGroup GetDirectionGroup(Transform reference, Transform cannon)
    {
      // Compute the yaw difference between the reference (vehicle) and the cannon's forward
      // 0 = Forward, 90 = Right, 180/-180 = Back, -90 = Left

      // Get the local rotation of the cannon relative to the reference
      var localRotation = Quaternion.Inverse(reference.rotation) * cannon.rotation;
      var localYaw = localRotation.eulerAngles.y;

      // Convert yaw to -180..180 for easy grouping
      var yaw = Mathf.DeltaAngle(0, localYaw);

      // Group by quadrant (Forward, Right, Back, Left)
      if (yaw >= -45f && yaw < 45f)
        return CannonDirectionGroup.Forward;
      if (yaw >= 45f && yaw < 135f)
        return CannonDirectionGroup.Right;
      if (yaw >= -135f && yaw < -45f)
        return CannonDirectionGroup.Left;
      // All others (135..180, -180..-135) = Back
      return CannonDirectionGroup.Back;
    }


    public void RecalculateCannonGroups()
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
        var group = GetDirectionGroup(_forwardTransform, cannon.transform);
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