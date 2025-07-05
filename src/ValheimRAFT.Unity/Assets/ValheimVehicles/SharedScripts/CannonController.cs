// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

#if !UNITY_EDITOR && !UNITY_2022
using ValheimVehicles.Controllers;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class CannonController : MonoBehaviour
  {
    [Header("Cannonball")]
    [Tooltip("Prefab asset for the cannonball projectile (assign in inspector or dynamically).")]
    // Will be set from asset and cached for instantiation
    public static Cannonball CannonballSolidPrefab;
    public static Cannonball CannonballExplosivePrefab;
    private static bool hasRunSetup;

    [Header("Cannon Animations")]
    public static float CannonAimSpeed = 0f;

    public static float CannonFireAudioVolume = 1f;
    public static float CannonReloadAudioVolume = 0.5f;
    public static bool HasFireAudio = true;
    public static bool HasReloadAudio = true;
    public static LayerMask SightBlockingMask = 0;

    public static float MaxFiringRotationYOverride = 0f;
    public static float MaxFiringPitchOverride = 0f;
    public static float MinFiringPitchOverride = 0f;
    [Tooltip("Optional: prefab asset for this instance (overrides static for this controller only).")]
    [SerializeField] private GameObject CannonballSolidPrefabAssetLocal;
    [SerializeField] private GameObject CannonballExplosivePrefabAssetLocal;

    [Header("Transforms")]
    [Tooltip("Transform to use for fire direction (usually barrel/shooter).")]
    [SerializeField] private Transform cannonShooterTransform;
    [SerializeField] private Transform cannonShooterAimPoint;
    [SerializeField] private Transform cannonRotationalTransform;
    [Tooltip("Speed (m/s) for cannonball launch.")]
    [SerializeField] private float cannonballSpeed = 90f; // 90m/s is standard.

    [Header("Ammunition")]
    [Tooltip("Maximum shells this cannon can hold.")]
    [SerializeField] public int maxAmmo = 12;
    [Tooltip("Current shells. This should be read-only, but exposed for testing")]
    [SerializeField] private int _currentAmmo;
    [Tooltip("How many shells are reloaded at once.")]
    [SerializeField] private int reloadQuantity = 1;
    [Tooltip("Time to reload (seconds).")]
    [SerializeField] private float reloadTime = 0.5f;
    [Tooltip("Automatically reload when fired?")]
    [SerializeField] private bool autoReload = true;
    [Tooltip("Ammunition type")]
    [SerializeField] private Cannonball.CannonballType ammoType = Cannonball.CannonballType.Solid;

    [Header("Effects & Sounds")]
    [Tooltip("Sound when firing.")]
    [SerializeField] public AudioClip fireClip;
    [Tooltip("Sound when reloading.")]
    [SerializeField] public AudioClip reloadClip;
    [SerializeField] public float fireClipPitch = 1f;
    [SerializeField] public float reloadClipPitch = 0.75f;
    [Tooltip("Speed at which the cannon aims to adjust to target position.")]
    [SerializeField] public float _aimingSpeed = 10f;

    [Header("Pooling")]
    [Tooltip("Minimum cannonballs kept in pool (typically 1 per cannon).")]
    [SerializeField] private int minPoolSizePerBarrel = 1;
    [SerializeField] public Transform cannonFireAudioSourceTransform;
    [SerializeField] public Transform cannonReloadAudioSourceTransform;


    [SerializeField] public bool hasLoaded = true;

    [Header("Cannon Targeting")]
    [Tooltip("Target To fire upon. Will aim for the position center.")]
    [SerializeField] public Transform firingTarget;
    [Tooltip("Generic firing coordinates")]
    [SerializeField] public Vector3 firingCoordinates;
    [Tooltip("Max Range Cannons can fire at.")]
    [SerializeField] public float maxFiringRange = 150f;
    [Tooltip("Max degrees a cannon can pivot to hit a object")]
    [SerializeField] public float maxFiringRotationY = 30f;
    [Tooltip("Max pitch is the degrees the barrel can aim upwards")]
    [SerializeField] public float maxFiringPitch = 25f;
    [Tooltip("Min pitch is the degrees the barrel can aim downwards")]
    [SerializeField] public float minFiringPitch = 10f;
    [SerializeField] public bool canRotateFiringRangeY = true;


    [Header("Barrel Ammo Logic")]
    [SerializeField] private bool hasNearbyPowderBarrel;
    [SerializeField] private float barrelSupplyRadius = 5f;
    // barrel check timers
    public float LastBarrelCheckTime;
    public float BarrelCheckInterval = 30f;

    public float lostTargetDeltaReturnTime = 5f;
    public float lastUpdateTargetTime = -999f;

    // --- State ---
    private readonly HashSet<Cannonball> _activeCannonballs = new();
    private readonly Dictionary<BarrelPart, Cannonball> _loadedCannonballs = new();
    private readonly List<Cannonball> _trackedLoadedCannonballs = new();

    private readonly List<BarrelPart> shootingParts = new();
    private bool _canFire;
    private Queue<Cannonball> _cannonballPool = new();
    private AudioSource _cannonFireAudioSource;
    private AudioSource _cannonReloadAudioSource;
    private CoroutineHandle _cleanupRoutine;

    private Collider[] _colliders;
    private Quaternion _defaultShooterLocalRotation;
    private float _lastAllowedYaw = float.NaN;

    private Quaternion _recoilRotation;
    private CoroutineHandle _recoilRoutine;
    private Quaternion _reloadRotation;
    private CoroutineHandle _reloadRoutine;
    private Quaternion _targetShooterLocalRotation;
    [NonSerialized] public Vector3? currentAimPoint; // Null if no current target assigned
    public Func<Vector3?> GetFiringTargetPosition = () => null;

    public int MinPoolSize => 1 * Mathf.Min(shootingParts.Count, 1);

    public float aimingSpeed => CannonAimSpeed > 0f ? CannonAimSpeed : _aimingSpeed;

    public int CurrentAmmo { get => _currentAmmo; set => _currentAmmo = value; }
    public bool IsReloading { get; private set; }
    public bool IsFiring { get; private set; }


    public bool IsLoaded => shootingParts.All(sp =>
      _loadedCannonballs.TryGetValue(sp, out var ball) && ball != null);

    public bool IsAnyBarrelLoaded => shootingParts.Any(sp =>
      _loadedCannonballs.TryGetValue(sp, out var ball) && ball != null);

    public float FiringRotationMaxY => MaxFiringRotationYOverride > 0f ? MaxFiringRotationYOverride : maxFiringRotationY;
    public float BarrelPitchMaxAngle => MaxFiringPitchOverride > 0f ? MaxFiringPitchOverride : maxFiringPitch;
    public float BarrelPitchMinAngle => MinFiringPitchOverride > 0f ? MinFiringPitchOverride : minFiringPitch;

    public Cannonball.CannonballType AmmoType
    {
      get => ammoType;
      set
      {
        ammoType = value;
        SetupCannonballPrefab();
      }
    }

    private void Awake()
    {
      SetupTransforms();

      _cannonFireAudioSource = cannonFireAudioSourceTransform.GetComponent<AudioSource>();
      _cannonReloadAudioSource = cannonReloadAudioSourceTransform.GetComponent<AudioSource>();

      _colliders = GetComponentsInChildren<Collider>(true);

      // routines
      _cleanupRoutine = new CoroutineHandle(this);
      _recoilRoutine = new CoroutineHandle(this);
      _reloadRoutine = new CoroutineHandle(this);

      if (cannonShooterTransform != null)
      {
        _defaultShooterLocalRotation = cannonShooterTransform.localRotation;
        _recoilRotation = Quaternion.Euler(-2f, 0f, 0f) * _defaultShooterLocalRotation;
        _reloadRotation = Quaternion.Euler(10f, 0f, 0f) * _defaultShooterLocalRotation;
        _targetShooterLocalRotation = _defaultShooterLocalRotation;
      }


#if UNITY_EDITOR
      // Must only be run in Unity Editor. We reset static values so it's like first to load for this object.
      CleanupCannonballPrefab();
#endif
      SetupCannonballPrefab();


      // we start loaded with this. TODO might move this logic into a network level check.
      if (!IsAnyBarrelLoaded && hasLoaded)
      {
        foreach (var shootingPart in shootingParts)
        {
          var loadedCannonball = GetPooledCannonball(shootingPart);
          loadedCannonball.Load(shootingPart.projectileLoader);
        }
      }

      SightBlockingMask = GetSightBlockingMask();

      InitializePool();

      GetFiringTargetPosition = HandleFiringTargetPositionDefault;
      // --- Setup shooter recoil rotation values ---
    }


    internal virtual void Start()
    {
      LoggerProvider.LogDev("Solver Iterations: " + Physics.defaultSolverIterations);
      LoggerProvider.LogDev("Solver Velocity Iterations: " + Physics.defaultSolverVelocityIterations);
      LoggerProvider.LogDev("Fixed Timestep: " + Time.fixedDeltaTime);
      LoggerProvider.LogDev("Physics Engine: PhysX");
      SetupCannonballPrefab();
      TryReload();
    }

    private void FixedUpdate()
    {
      if (LastBarrelCheckTime == 0 || LastBarrelCheckTime + BarrelCheckInterval < Time.fixedTime)
      {
        hasNearbyPowderBarrel = PowderBarrel.FindNearbyBarrels(transform.position, barrelSupplyRadius)?.Count > 0;
        LastBarrelCheckTime = Time.fixedTime;
      }

      SyncLoadedCannonballs();

      if (firingTarget != null || firingCoordinates != null)
      {
        AdjustFiringAngle();
      }
    }

    private void OnDisable()
    {
      CleanupPool();
    }

    public bool IsBarrelLoaded(BarrelPart part)
    {
      return _loadedCannonballs.TryGetValue(part, out var ball) && ball != null;
    }

    public void SyncLoadedCannonballFromList(IEnumerable<Cannonball> cannonballs)
    {
      foreach (var ball in cannonballs)
      {
        if (ball != null && !ball.IsInFlight)
        {
          var ballTransform = ball.transform;
          if (ball.m_body != null)
          {
            var t = ball.lastFireTransform != null ? ball.lastFireTransform : cannonShooterTransform;
            ball.m_body.Move(t.position, t.rotation);
          }
          else
          {
            ballTransform.position = cannonShooterTransform.position;
            ballTransform.rotation = cannonShooterTransform.rotation;
          }
        }
      }
    }

    // Sync "loaded" (not-in-flight) cannonballs to loader, never parented!
    public void SyncLoadedCannonballs()
    {
      SyncLoadedCannonballFromList(_trackedLoadedCannonballs);
      SyncLoadedCannonballFromList(_cannonballPool);
    }

    /// <summary>
    /// todo this needs to be masks that do not block as much but could obstruct a shot.
    ///
    /// without this layermask for terrain the cannons will keep firing and missing.
    /// </summary>
    /// <returns></returns>
    public LayerMask GetSightBlockingMask()
    {
      // return 0;
      // return LayerMask.GetMask("terrain");
      return LayerHelpers.CannonBlockingSiteHitLayers;
    }

    private Vector3? HandleFiringTargetPositionDefault()
    {
      if (currentAimPoint != null && firingTarget != null && firingTarget.gameObject.activeInHierarchy)
        return currentAimPoint;
      return null;
    }

    public event Action<int> OnAmmoChanged;
    public event Action OnFired;
    public event Action OnReloaded;

    private void SetupTransforms()
    {
      if (cannonRotationalTransform == null)
      {
        cannonRotationalTransform = transform.Find("rotational");
      }

      if (cannonFireAudioSourceTransform == null)
      {
        cannonFireAudioSourceTransform = transform.Find("rotational/cannon_shooter/cannon_shot_audio");
      }

      if (cannonReloadAudioSourceTransform == null)
      {
        cannonReloadAudioSourceTransform = transform.Find("rotational/cannon_shooter/cannon_reload_audio");
      }

      if (cannonShooterTransform == null)
        cannonShooterTransform = transform.Find("rotational/cannon_shooter");

      if (cannonShooterAimPoint == null)
        cannonShooterAimPoint = transform.Find("rotational/cannon_shooter/shooter_aim_point");

      for (var i = 0; i < cannonShooterTransform.childCount; i++)
      {
        var child = cannonShooterTransform.GetChild(i);
        if (child.name.StartsWith("cannon_shooter_part"))
        {
          var shootingPart = BarrelPart.Init(child);
          shootingParts.Add(shootingPart);
        }
      }

      if (shootingParts.Count == 0)
      {
        LoggerProvider.LogError("No shooting parts found in cannon shooter.");
      }
    }

    private static void CleanupCannonballPrefab()
    {
#if UNITY_EDITOR
      hasRunSetup = false;
      // Only clean up runtime-created prefabs, never destroy inspector assets
      if (CannonballSolidPrefab != null && Application.isPlaying)
      {
        var go = CannonballSolidPrefab.gameObject;
        if (go != null && go.scene.IsValid())
        {
          Destroy(go);
        }
        CannonballSolidPrefab = null;
      }
      
      if (CannonballExplosivePrefab != null && Application.isPlaying)
      {
        var go = CannonballExplosivePrefab.gameObject;
        if (go != null && go.scene.IsValid())
        {
          Destroy(go);
        }
        CannonballExplosivePrefab = null;
      }
#endif
    }

    private Cannonball SelectCannonballType()
    {
      if (!hasRunSetup)
      {
        SetupCannonballPrefab();
      }

      switch (ammoType)
      {
        case Cannonball.CannonballType.Solid:
          return CannonballSolidPrefab;
        case Cannonball.CannonballType.Explosive:
          return CannonballExplosivePrefab;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void InitCannonballPrefabAssets()
    {
      if (CannonballExplosivePrefab == null)
      {
        if (CannonballExplosivePrefabAssetLocal != null)
        {
          var go = Instantiate(CannonballExplosivePrefabAssetLocal);
          CannonballExplosivePrefab = go.GetComponent<Cannonball>();
        }
        else
        {
          LoggerProvider.LogWarning("No cannonball explosive prefab asset set. Please set one in CannonballExplosivePrefabAsset or CannonballExplosivePrefabAssetLocal.");
        }
      }

      if (CannonballSolidPrefab == null)
      {
        if (CannonballSolidPrefabAssetLocal != null)
        {
          var go = Instantiate(CannonballSolidPrefabAssetLocal);
          CannonballSolidPrefab = go.GetComponent<Cannonball>();
        }
        else
        {
          LoggerProvider.LogWarning("No cannonball explosive prefab asset set. Please set one in CannonballExplosivePrefabAsset or CannonballExplosivePrefabAssetLocal.");
        }
      }
    }

    private void CleanupPool()
    {
      if (_cannonballPool.Count > 0)
      {
        while (_cannonballPool.Count > 0)
        {
          var ball = _cannonballPool.Dequeue();
          if (ball == null) continue;
          Destroy(ball.gameObject);
        }
      }
      _trackedLoadedCannonballs.RemoveAll(x => x == null);
    }

    private void SetupCannonballPrefab()
    {
      hasRunSetup = true;

      // must clean the pool if shooting new cannonballs.
      CleanupPool();
      InitCannonballPrefabAssets();


      var selectedCannonball = SelectCannonballType();

      if (selectedCannonball == null)
      {
        LoggerProvider.LogError("Unexpected null cannonball prefab.");
      }

      // IgnoreVehicleColliders(selectedCannonball);
      // IgnoreLocalColliders(selectedCannonball);
      //
      // var go = selectedCannonball.gameObject;
      // var goTransform = go.transform;
      // goTransform.position = Vector3.zero;
      // goTransform.rotation = Quaternion.identity;
      // go.SetActive(false);
      //
      // var rb = go.GetComponent<Rigidbody>();
      // if (rb != null)
      // {
      //   rb.isKinematic = true;
      //   rb.useGravity = false;
      // }
    }

    private void InitializePool()
    {
      _cannonballPool = new Queue<Cannonball>(MinPoolSize);
      var selectedCannonball = SelectCannonballType();
      if (!selectedCannonball) return;
      for (var i = 0; i < MinPoolSize; i++)
      {
        foreach (var shootingPart in shootingParts)
        {
          var go = Instantiate(selectedCannonball.gameObject, shootingPart.projectileLoader.position, shootingPart.projectileLoader.rotation, null);
          go.name = $"cannonball_queue_{ammoType}_{i}";
          var obj = go.GetComponent<Cannonball>();
          IgnoreLocalColliders(obj);
          go.SetActive(false);
          _cannonballPool.Enqueue(obj);
        }
      }
    }

    // ReSharper disable once UseNullableReferenceTypesAnnotationSyntax
    private void IgnoreVehicleColliders(Cannonball selectedCannonball)
    {
#if !UNITY_2022 && !UNITY_EDITOR
      var vehiclePiecesController = GetComponentInParent<VehiclePiecesController>();
      if (vehiclePiecesController != null)
      {
        vehiclePiecesController.IgnoreAllVehicleCollidersForGameObjectChildren(selectedCannonball.gameObject);
      }
#endif
    }

    private void IgnoreLocalColliders(Cannonball cannonball)
    {
      if (cannonball == null || _colliders == null) return;
      foreach (var localCollider in _colliders)
      {
        if (localCollider == null) continue;
        foreach (var cannonballCollider in cannonball.Colliders)
        {
          if (cannonballCollider == null) continue;
          Physics.IgnoreCollision(localCollider, cannonballCollider, true);
        }
      }
    }

    private void IgnoreOtherCannonballColliders(Cannonball localCannonball)
    {
      return;
      foreach (var ball in _trackedLoadedCannonballs)
      {
        if (!ball) continue;
        foreach (var otherCollider in ball.Colliders)
        {
          if (otherCollider == null) continue;
          foreach (var localCollider in localCannonball.Colliders)
          {
            if (localCollider == null) continue;
            Physics.IgnoreCollision(otherCollider, localCollider, true);
          }
        }
      }
    }

    private Cannonball GetPooledCannonball(BarrelPart barrelPart)
    {
      _trackedLoadedCannonballs.RemoveAll(x => x == null);
      if (_cannonballPool.Count > 0)
      {
        var ball = _cannonballPool.Dequeue();

        while (ball == null && _cannonballPool.Count > 0)
        {
          ball = _cannonballPool.Dequeue();
        }

        if (ball)
        {
          ball.gameObject.name = $"cannonball_{ammoType}_active_{_cannonballPool.Count}";
          ball.gameObject.SetActive(true);
          _trackedLoadedCannonballs.Add(ball);
          return ball;
        }
      }
      var selectedCannonball = SelectCannonballType();
      if (!selectedCannonball)
      {
        LoggerProvider.LogWarning("No cannonball prefab set. Please set one in CannonballSolidPrefab or CannonballExplosivePrefab.");
        return null;
      }

      var go = Instantiate(selectedCannonball.gameObject, barrelPart.projectileLoader.position, barrelPart.projectileLoader.rotation, null);
      var localCannonball = go.GetComponent<Cannonball>();

      IgnoreLocalColliders(localCannonball);
      IgnoreOtherCannonballColliders(localCannonball);

      go.gameObject.SetActive(true);

      _trackedLoadedCannonballs.Add(localCannonball);
      go.name = $"cannonball_{ammoType}_active_{_trackedLoadedCannonballs.Count}";
      return localCannonball;
    }

    public void ReturnCannonballToPool(Cannonball ball)
    {
      if (!isActiveAndEnabled) return;
      if (!ball) return;
      _cannonballPool.Enqueue(ball);
      _activeCannonballs.Remove(ball);
      _trackedLoadedCannonballs.Remove(ball);

      ball.gameObject.SetActive(false);

      StartCleanupCoroutine();
    }

    /// <summary>
    /// Numerically finds the firing angle to hit the target, accounting for drag.
    /// </summary>
    public static bool CalculateBallisticAimWithDrag(
      Vector3 fireOrigin,
      Vector3 targetPosition,
      float launchSpeed,
      float drag,
      out Vector3 fireDirection,
      out float angleDegrees,
      int maxIterations = 100,
      float tolerance = 0.05f)
    {
      var delta = targetPosition - fireOrigin;
      var xzDist = new Vector2(delta.x, delta.z).magnitude;
      var y = delta.y;

      if (xzDist < 0.01f)
      {
        fireDirection = Vector3.zero;
        angleDegrees = 0;
        return false;
      }

      var dirXZ = new Vector3(delta.x, 0, delta.z).normalized;

      // ALLOW NEGATIVE ANGLES (downward)
      var low = -Mathf.PI / 4f; // -45°
      var high = Mathf.PI / 2f; // +90°
      var found = false;
      float angle = 0;

      for (var i = 0; i < maxIterations; i++)
      {
        angle = (low + high) * 0.5f;
        var yAtTarget = SimulateProjectileHeightAtXZ(launchSpeed, drag, xzDist, angle);

        var diff = yAtTarget - y;

        if (Mathf.Abs(diff) < tolerance)
        {
          found = true;
          break;
        }

        if (diff > 0)
          high = angle; // Too high
        else
          low = angle; // Too low
      }

      if (!found)
      {
        fireDirection = Vector3.zero;
        angleDegrees = 0;
        return false;
      }

      fireDirection = Quaternion.AngleAxis(Mathf.Rad2Deg * angle, Vector3.Cross(Vector3.up, dirXZ)) * dirXZ;
      angleDegrees = angle * Mathf.Rad2Deg;
      return true;
    }

    /// <summary>
    /// Simulates the Y (vertical) position after traveling horizontalDistance at a given launch angle, speed, and drag.
    /// </summary>
    private static float SimulateProjectileHeightAtXZ(
      float speed, float drag, float horizontalDistance, float angle)
    {
      // Simulate flight in XZ plane only
      var dt = 0.01f; // Smaller for more accuracy!
      var vx = speed * Mathf.Cos(angle);
      var vy = speed * Mathf.Sin(angle);

      float x = 0;
      float y = 0;

      while (x < horizontalDistance && y > -500 && x < 10000)
      {
        // Apply drag to velocity (Unity's model)
        vx *= Mathf.Exp(-drag * dt);
        vy = (vy + Physics.gravity.y * dt) * Mathf.Exp(-drag * dt);

        x += vx * dt;
        y += vy * dt;

        // Early out if we've dropped far below the target
        if (y < -50) break;
      }

      return y;
    }

    /// <summary>
    /// Tries to find an optimal aim position on the target's collider (filtered by LayerMask),
    /// returning true if the cannon can hit it (no blocking obstacle in the way).
    /// </summary>
    public bool CanHitTargetCollider(Transform target, out Vector3 aimPoint)
    {
      aimPoint = target.position;

      if (target == null)
        return false;

      // Get all colliders on the target and its children, filter by layer
      var colliders = target.GetComponentsInChildren<Collider>(false)
        .Where(c => ((1 << c.gameObject.layer) & LayerHelpers.CharacterLayerMask) != 0 && c.enabled)
        .ToList();
      

      if (colliders.Count == 0)
        return false;

      // Pick the collider closest to our muzzle or cannon position
      var fireOrigin = cannonShooterAimPoint ? cannonShooterAimPoint.position : transform.position;
      Collider bestCollider = null;
      float bestDist = float.MaxValue;
      Vector3 bestPoint = target.position;
      var muzzle = cannonShooterAimPoint ? cannonShooterAimPoint.position : transform.position;

      foreach (var col in colliders)
      {
        var point = col.ClosestPoint(fireOrigin); // Closest point on collider to our muzzle
        var dist = Vector3.SqrMagnitude(point - fireOrigin);

        #if DEBUG
        // LoggerProvider.LogDebugDebounced($"[CannonTargeting] Checking collider '{col.name}' on '{col.gameObject.name}' at layer {col.gameObject.layer} | ClosestPoint: {point} | Dist: {Mathf.Sqrt(dist)}");
        #endif
        // RuntimeDebugLineDrawer.DrawLine(muzzle, point, RuntimeDebugLineDrawer.TYellow, 0.05f, 0.05f);

        if (dist < bestDist)
        {
          bestDist = dist;
          bestPoint = point;
          bestCollider = col;
        }
      }

      if (bestCollider == null)
        return false;

      // Final validation: can we actually hit this point?
      if (!CanAimAt(bestPoint))
        return false;

      aimPoint = bestPoint;
      return true;
    }

    public bool CanAimAt(Vector3 targetPosition)
    {
      // Do not fire on out of sight target.
      if (!HasLineOfSightToTarget(targetPosition, SightBlockingMask, true))
      {
        return false;
      }
      // --- YAW CHECK ---
      var toTargetXZ = targetPosition - cannonShooterAimPoint.position;
      toTargetXZ.y = 0f;
      if (toTargetXZ.sqrMagnitude < 0.01f) return false;

      var currentYaw = cannonRotationalTransform.eulerAngles.y;
      var desiredYaw = Quaternion.LookRotation(toTargetXZ, Vector3.up).eulerAngles.y;
      var deltaYaw = Mathf.DeltaAngle(currentYaw, desiredYaw);
      if (Mathf.Abs(deltaYaw) > FiringRotationMaxY) return false;

      // --- PITCH CHECK ---
      var fireOrigin = cannonShooterAimPoint.position;
      var delta = targetPosition - fireOrigin;
      var xzDist = new Vector2(delta.x, delta.z).magnitude;
      if (xzDist < 0.01f) return false;

      var dirXZ = new Vector3(delta.x, 0, delta.z).normalized;
      if (!CalculateBallisticAimWithDrag(fireOrigin, targetPosition, cannonballSpeed, CannonballSolidPrefab.cannonBallDrag, out _, out var angle)) return false;

      var pitch = -angle;
      if (pitch < -BarrelPitchMaxAngle || pitch > BarrelPitchMinAngle) return false;

      return true;
    }

    public void RotateTowardsOrigin()
    {
      if (!float.IsNaN(_lastAllowedYaw))
      {
        // Stay at last allowed yaw, do not follow out-of-range targets
        var localRotation = cannonRotationalTransform.localRotation;
        var rotation = Quaternion.Euler(localRotation.eulerAngles.x, _lastAllowedYaw, localRotation.z);
        localRotation = Quaternion.Lerp(localRotation, rotation, Time.fixedDeltaTime);
        cannonRotationalTransform.localRotation = localRotation;
      }
      else
      {
        var rotation = Quaternion.Euler(0f, _lastAllowedYaw, cannonRotationalTransform.localRotation.z);
        cannonRotationalTransform.localRotation = rotation;
      }
    }

    public void AdjustFiringAngle()
    {
      if (IsReloading) return;

      var fireOrigin = cannonShooterTransform.position;
      var targetPosition = GetFiringTargetPosition();
      if (!targetPosition.HasValue)
      {
        RotateTowardsOrigin();
        _canFire = false;
        return;
      }

      // --- YAW ---
      var toTarget = targetPosition.Value - cannonRotationalTransform.position;
      toTarget.y = 0f; // flatten to XZ
      if (toTarget.sqrMagnitude < 0.01f)
      {
        _canFire = false;
        return;
      }

      var desiredYawWorld = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
      var currentYaw = cannonRotationalTransform.eulerAngles.y;
      var targetYaw = desiredYawWorld.eulerAngles.y;
      var deltaYaw = Mathf.DeltaAngle(currentYaw, targetYaw);

      var isWithinYawRange = Mathf.Abs(deltaYaw) <= maxFiringRotationY;

      if (canRotateFiringRangeY && isWithinYawRange)
      {
        _lastAllowedYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.fixedDeltaTime * aimingSpeed);
        var prevRotation = cannonRotationalTransform.rotation;
        cannonRotationalTransform.rotation = Quaternion.Euler(prevRotation.eulerAngles.x, _lastAllowedYaw, prevRotation.eulerAngles.z);
      }
      else
      {
        if (lastUpdateTargetTime + lostTargetDeltaReturnTime < Time.fixedTime)
        {
          RotateTowardsOrigin();
          lastUpdateTargetTime = Time.fixedTime;
        }
        lastUpdateTargetTime += Time.fixedDeltaTime;
        _canFire = false;
        return;
      }
      // --- PITCH (X/Elevation) ---
      var hasPitch = false;
        var prevLocalRotation = cannonRotationalTransform.localRotation;
      if (isWithinYawRange && CalculateBallisticAimWithDrag(fireOrigin, targetPosition.Value, cannonballSpeed, CannonballSolidPrefab.cannonBallDrag, out var fireDir, out var angle))
      {
        // Clamp pitch
        // this can already be mutated by rotational in Y
        _targetShooterLocalRotation = Quaternion.Euler(Mathf.Clamp(-angle, -BarrelPitchMaxAngle, BarrelPitchMinAngle), prevLocalRotation.eulerAngles.y, prevLocalRotation.eulerAngles.z);
        hasPitch = true;
      }
      else
      {
        hasPitch = false;
        _targetShooterLocalRotation = Quaternion.Euler(0, prevLocalRotation.eulerAngles.y, prevLocalRotation.eulerAngles.z);
      }

      var localRotation = cannonRotationalTransform.localRotation;

      localRotation = Quaternion.Lerp(
        localRotation,
        _targetShooterLocalRotation,
        Time.fixedDeltaTime * aimingSpeed
      );
      cannonRotationalTransform.localRotation = localRotation;

      // --- Firing check (with angle threshold) ---
      var alignedYaw = Mathf.Abs(Mathf.DeltaAngle(targetYaw, cannonRotationalTransform.rotation.eulerAngles.y)) < 0.5f;
      var alignedPitch = Quaternion.Angle(localRotation, _targetShooterLocalRotation) < 0.5f;

      _canFire = isWithinYawRange && hasPitch && alignedYaw && alignedPitch;
    }

    public void Fire()
    {
      if (!isActiveAndEnabled) return;
      if (IsReloading || IsFiring || CurrentAmmo <= 0 || !hasNearbyPowderBarrel || !_canFire) return;
      IsFiring = true;

      var hasFired = false;
      for (var index = 0; index < shootingParts.Count; index++)
      {
        var shootingPart = shootingParts[index];
        if (!FireSingle(shootingPart,index)) break;
        hasFired = true;
      }

      if (!hasFired)
      {
        IsFiring = false;
        return;
      }

      CurrentAmmo = Math.Max(0, CurrentAmmo - shootingParts.Count);

      // use a single audio clip for now. Using multiple is not worth it for perf.
      PlayFireClip();

      OnAmmoChanged?.Invoke(CurrentAmmo);
      OnFired?.Invoke();

      _recoilRoutine.Start(RecoilCoroutine());
    }

    private bool FireSingle(BarrelPart barrel, int barrelCount)
    {
      if (!_loadedCannonballs.TryGetValue(barrel, out var loadedCannonball) || loadedCannonball == null)
      {
        loadedCannonball = GetPooledCannonball(barrel);
        if (loadedCannonball == null)
        {
          LoggerProvider.LogWarning("No cannonball available. Please increase the pool size.");
          return false;
        }
        _loadedCannonballs[barrel] = loadedCannonball;
        loadedCannonball.Load(barrel.projectileLoader);
      }

      var targetPosition = GetFiringTargetPosition();
      if (!targetPosition.HasValue) return false;
      var barrelPosition = barrel.projectileLoader.position;
      if (Vector3.Distance(targetPosition.Value, barrelPosition) > maxFiringRange) return false;

      IgnoreLocalColliders(loadedCannonball);
      loadedCannonball.transform.position = barrelPosition;

      var randomVelocityMultiplier = (Random.value - 0.5f) * 2f;
      var localSpeed = cannonballSpeed + randomVelocityMultiplier;
      
      RuntimeDebugLineDrawer.DrawLine(barrelPosition, targetPosition.Value, RuntimeDebugLineDrawer.TGreen, 0.1f, 0.05f);

      
      loadedCannonball.Fire(
        cannonShooterTransform.forward * localSpeed,
        barrel.projectileLoader, this, barrelCount);

      PlayMuzzleFlash(barrel);

      _activeCannonballs.Add(loadedCannonball);
      _trackedLoadedCannonballs.Remove(loadedCannonball);

      // Clean up loaded ball for this barrel
      _loadedCannonballs[barrel] = null;

      return true;
    }

    [ContextMenu("Reload Cannon")]
    public void TryReload()
    {
      if (IsReloading || CurrentAmmo <= 0 || IsAnyBarrelLoaded)
        return;
      _reloadRoutine.Start(ReloadCoroutine());
    }

    /// <summary>
    /// Checks if the cannon has clear line of sight from the muzzle to the specified target position,
    /// ignoring any colliders belonging to itself. Returns true if nothing blocks the shot.
    /// </summary>
    public bool HasLineOfSightToTarget(Vector3 targetPosition, LayerMask obstacleMask, bool raycastDebugDraw = false)
    {
      // Use a small offset forward to avoid muzzle collisions
      var fireOrigin = cannonShooterAimPoint.position;

      var dir = targetPosition - fireOrigin;
      var distance = dir.magnitude;
      dir.Normalize();

      // Allow for multiple skips if we hit our own ship/vehicle/cannon parts
      var currOrigin = fireOrigin;
      var remainingDist = distance;
      const int maxSelfSkips = 8;
      for (var i = 0; i < maxSelfSkips; i++)
      {
        // do not hit triggers...if we do this we have to ignore triggers that listen for character (which we cannot do).
        if (Physics.Raycast(currOrigin, dir, out var hit, remainingDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
          if (hit.transform.gameObject.layer == LayerHelpers.CharacterLayer && !TargetController.IsHostileCharacter(hit.transform))
          {
            RuntimeDebugLineDrawer.DrawLine(fireOrigin, hit.point, RuntimeDebugLineDrawer.TRed, 0.1f, 0.05f);
            return false;
          }
          // If we hit ourselves, skip past and keep going
          // try again if we are hostile and are a character
          // bail if we are a friendly character.
          // try again if we hit a triggerArea for some reason.
          if (hit.transform == transform || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform) || hit.transform.gameObject.layer == LayerHelpers.CharacterTriggerLayer || hit.transform.gameObject.layer == LayerHelpers.CharacterLayer && TargetController.IsHostileCharacter(hit.transform))
          {
            currOrigin = hit.collider.ClosestPoint(hit.point + dir * 10 ) + dir * 0.1f; // Move just past this collider
            remainingDist = distance - (currOrigin - fireOrigin).magnitude;
            continue;
          }
          
          if (raycastDebugDraw)
            RuntimeDebugLineDrawer.DrawLine(fireOrigin, hit.point, RuntimeDebugLineDrawer.TRed, 0.1f, 0.05f);

          // Any other collider is considered blocking
          return false;
        }
        return true;
      }
      // If we exceeded skips, treat as blocked (fail-safe)
      return false;
    }

    private IEnumerator RecoilCoroutine()
    {
      var elapsed = 0f;
      var recoilUpwardAnimationDuration = 0.15f;
      var recoilReturnAnimationDuration = 0.15f;
      var recoilRotation = Quaternion.Euler(-2f, 0f, 0f) * _targetShooterLocalRotation;
      while (elapsed < recoilReturnAnimationDuration + recoilUpwardAnimationDuration)
      {
        var t = Mathf.Clamp01(elapsed / 2f / 0.15f); // 0..1
        elapsed += Time.deltaTime;

        // move towards recoil.
        if (elapsed < 0.1f)
        {
          cannonRotationalTransform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.forward * -0.1f, t);
          if (cannonShooterTransform != null)
          {
            cannonShooterTransform.localRotation = Quaternion.Lerp(_targetShooterLocalRotation, recoilRotation, Mathf.Clamp01(t));
          }
        }
        else
        {
          // move back to target position. (and then will reload afterwards) 
          if (cannonShooterTransform != null)
          {
            cannonShooterTransform.localRotation = Quaternion.Lerp(_recoilRotation, _targetShooterLocalRotation, Mathf.Clamp01(t));
          }
          cannonRotationalTransform.localPosition = Vector3.Lerp(Vector3.forward * -0.1f, Vector3.zero, t);
        }
        yield return null;
      }
      IsFiring = false;

      if (autoReload && CurrentAmmo > 0)
      {
        _reloadRoutine.Start(ReloadCoroutine());
      }
      else
      {
        LoggerProvider.LogDev("No ammo left, not reloading.");
      }
    }

    private IEnumerator ReloadCoroutine()
    {
      if (!hasNearbyPowderBarrel)
        yield break;

      IsReloading = true;
      var elapsed = 0f;

      var startRotation = _targetShooterLocalRotation;
      var localEulerRotation = cannonShooterTransform.localRotation;
      var endRotation = Quaternion.Euler(_targetShooterLocalRotation.x + _recoilRotation.x, localEulerRotation.eulerAngles.y, localEulerRotation.eulerAngles.z);

      var safeReloadTime = Mathf.Clamp(reloadTime, 0.1f, 5f);
      while (elapsed < reloadTime)
      {
        var t = Mathf.Clamp01(elapsed * 2f / safeReloadTime); // 0..1
        elapsed += Time.deltaTime;
        if (cannonShooterTransform != null)
          cannonShooterTransform.localRotation = Quaternion.Lerp(startRotation, endRotation, Mathf.Clamp01(t));
        yield return null; // Wait one frame
      }

      PlayReloadClip();

      var shotsToReload = Math.Min(reloadQuantity, shootingParts.Count);
      for (var i = 0; i < shotsToReload && CurrentAmmo - i > 0; i++)
      {
        var shootingPart = shootingParts[i];
        if (shootingPart == null) break;
        var loaded = GetPooledCannonball(shootingPart);
        loaded.Load(shootingPart.projectileLoader);
        _loadedCannonballs[shootingPart] = loaded;
      }
      IsReloading = false;
      OnReloaded?.Invoke();
    }

    private void PlayMuzzleFlash(BarrelPart barrelPart)
    {
      if (barrelPart?.muzzleFlashEffect)
      {
        barrelPart.muzzleFlashEffect.Play();
      }
    }

    private void PlayReloadClip()
    {
      if (!HasReloadAudio) return;
      if (!_cannonReloadAudioSource) return;
      _cannonReloadAudioSource.volume = CannonReloadAudioVolume;

      // do nothing on reload if it's already playing.
      if (_cannonReloadAudioSource.isPlaying)
      {
        return;
      }

      _cannonReloadAudioSource.pitch = reloadClipPitch;
      _cannonReloadAudioSource.Play();
    }

    private void PlayFireClip()
    {
      if (!HasFireAudio || !_cannonFireAudioSource) return;
      _cannonFireAudioSource.volume = CannonFireAudioVolume;

      if (_cannonFireAudioSource.isPlaying)
      {
        _cannonFireAudioSource.Stop();
      }

      _cannonFireAudioSource.pitch = fireClipPitch + Random.Range(-0.2f, 0.2f);
      _cannonFireAudioSource.Play();
    }

    private void StartCleanupCoroutine()
    {
      if (!isActiveAndEnabled) return;
      _cleanupRoutine.Start(CleanupExtraCannonballsAfterDelay());
    }

    private IEnumerator CleanupExtraCannonballsAfterDelay()
    {
      yield return new WaitForSeconds(10f);
      while (_cannonballPool.Count > MinPoolSize)
      {
        var ball = _cannonballPool.Dequeue();
        if (ball != null)
        {
          Destroy(ball.gameObject);
        }
      }

      _trackedLoadedCannonballs.RemoveAll(x => x == null);
    }

    public class BarrelPart
    {
      public Transform muzzleExitPoint;
      public ParticleSystem muzzleFlashEffect;
      public Transform projectileLoader;

      public static BarrelPart Init(Transform shootingPartTransform)
      {
        var shootingPart = new BarrelPart
        {
          muzzleExitPoint = shootingPartTransform.Find("points/muzzle_exit_point"),
          muzzleFlashEffect = shootingPartTransform.Find("muzzle_flash_effect").GetComponent<ParticleSystem>(),
          projectileLoader = shootingPartTransform.Find("points/projectile_loader")
        };
        return shootingPart;
      }
    }
  }
}