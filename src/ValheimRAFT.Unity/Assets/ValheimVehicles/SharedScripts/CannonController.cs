// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

#if VALHEIM
using ValheimVehicles.Controllers;
using ValheimVehicles.Structs;
using ValheimVehicles.RPC;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using ValheimVehicles.SharedScripts.Structs;
using ValheimVehicles.SharedScripts.UI;

// assignments
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class CannonController : MonoBehaviour, ICannonPersistentConfig
  {
    [Header("Cannonball")]
    [Tooltip("Prefab asset for the cannonball projectile (assign in inspector or dynamically).")]
    // Will be set from asset and cached for instantiation
    public static GameObject CannonballSolidPrefab;
    public static GameObject CannonballExplosivePrefab;
    public static float BarrelSupplyDistance = 5f;
    private static bool hasRunSetup;

    [Header("Cannon Animations")]
    public static float CannonAimSpeed = 0f;

    public static float CannonFireAudioVolume = 1f;
    public static float CannonReloadAudioVolume = 0.5f;
    public static bool HasFireAudio = true;
    public static bool HasReloadAudio = false;
    public static float CannonAimingCenterOffsetY = 0f;
    public static LayerMask SightBlockingMask = 0;

    public static float MaxFiringRotationYOverride = 0f;
    public static float MaxFiringPitchOverride = 0f;
    public static float MinFiringPitchOverride = 0f;

    public static float maxSidewaysArcDegrees = 0.15f; // Tune as desired
    public static float maxVerticalArcDegrees = 0.15f; // Small up/down variation
    [Tooltip("Optional: prefab asset for this instance (overrides static for this controller only).")]
    [SerializeField] private GameObject CannonballSolidPrefabAssetLocal;
    [SerializeField] private GameObject CannonballExplosivePrefabAssetLocal;

    [Header("Transforms")]
    [Tooltip("Transform to use for fire direction (usually barrel/shooter).")]
    [SerializeField] public Transform cannonShooterTransform;
    [SerializeField] public Transform cannonShooterAimPoint;
    [SerializeField] public Transform cannonRotationalTransform;
    [Tooltip("Speed (m/s) for cannonball launch.")]
    [SerializeField] private float cannonballSpeed = 90f; // 90m/s is standard.

    [Header("Ammunition")]
    [Tooltip("Maximum shells this cannon can hold.")]
    [SerializeField] public int maxAmmo = 12;
    [Tooltip("Current shells. This should be read-only, but exposed for testing")]
    [SerializeField] private int _ammoCount;
    [Tooltip("How many shells are reloaded at once.")]
    [SerializeField] private int reloadQuantity = 1;
    [Tooltip("Time to reload (seconds).")]
    [SerializeField] private float _reloadTime = 0.5f;
    [Tooltip("Automatically reload when fired?")]
    [SerializeField] private bool autoReload = true;
    [Tooltip("Ammunition type")]
    [SerializeField] private CannonballVariant _ammoVariant = CannonballVariant.Solid;

    [Header("Effects & Sounds")]
    [Tooltip("Sound when firing.")]
    [SerializeField] public AudioClip fireClip;
    [Tooltip("Sound when reloading.")]
    [SerializeField] public AudioClip reloadClip;
    [SerializeField] public float fireClipPitch = 1f;
    [SerializeField] public float reloadClipPitch = 1f;
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
    [SerializeField]
    [CanBeNull]
    public Transform firingTarget;
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
    [SerializeField] public bool hasNearbyPowderBarrel;
    [SerializeField] private float barrelSupplyRadius = BarrelSupplyDistance;
    [SerializeField] public int ManualFiringGroupId;
    // barrel check timers
    public float LastBarrelCheckTime;
    public float BarrelCheckInterval = 5f;

    [Header("Firing Modes")]
    [SerializeField] public CannonFiringMode cannonFiringMode = CannonFiringMode.Auto;

    public CannonVariant cannonVariant = CannonVariant.Unknown;

    private readonly List<Collider> _colliders = new();

    // --- State ---
    private readonly Dictionary<BarrelPart, Cannonball> _loadedCannonballs = new();
    internal readonly List<Cannonball> _trackedLoadedCannonballs = new();
    private Queue<Cannonball> _cannonballPool = new();

    public readonly List<BarrelPart> shootingBarrelParts = new();
    private bool _canAutoFire;
    private AudioSource _cannonFireAudioSource;
    public static float CannonHandheld_FireAudioStartTime = 0.5f;
    private AudioSource _cannonReloadAudioSource;
    private CoroutineHandle _cleanupRoutine;
    private Quaternion _defaultShooterLocalRotation;
    private float _lastAllowedYaw = float.NaN;

    private Quaternion _recoilRotation;
    private CoroutineHandle _recoilRoutine;
    private Quaternion _reloadRotation;
    private CoroutineHandle _reloadRoutine;
    private Quaternion _targetShooterLocalRotation;
    [NonSerialized] public Vector3? currentAimPoint; // Null if no current target assigned
    public Func<Vector3?> GetFiringTargetCurrentAimPoint = () => null;

    public HashSet<Transform> IgnoredTransformRoots = new();

    public int MinPoolSize => 1 * Mathf.Min(shootingBarrelParts.Count, 1);

    public float aimingSpeed => CannonAimSpeed > 0f ? CannonAimSpeed : _aimingSpeed;

    public bool IsReloading { get; private set; }
    public bool IsFiring { get; private set; }
    public CannonDirectionGroup? CurrentManualDirectionGroup { get; set; }

    public static float ReloadTimeOverride = 0f;
    public static float CannonHandHeld_ReloadTime = 5f;

    public static HashSet<CannonController> Instances = new();

#if VALHEIM
    public VehiclePiecesController PiecesController;
#endif

    public float ReloadTime
    {
      get
      {
        if (IsHandHeldCannon)
          return CannonHandHeld_ReloadTime;
        return ReloadTimeOverride > 0f
          ? ReloadTimeOverride
          : _reloadTime;
      }
    }

    public bool IsLoaded => shootingBarrelParts.All(sp =>
      _loadedCannonballs.TryGetValue(sp, out var ball) && ball != null);

    public bool IsAnyBarrelLoaded => shootingBarrelParts.Any(sp =>
      _loadedCannonballs.TryGetValue(sp, out var ball) && ball != null);

    public float FiringRotationMaxY => MaxFiringRotationYOverride > 0f ? MaxFiringRotationYOverride : maxFiringRotationY;
    public float BarrelPitchMaxAngle => MaxFiringPitchOverride > 0f ? MaxFiringPitchOverride : maxFiringPitch;
    public float BarrelPitchMinAngle => MinFiringPitchOverride > 0f ? MinFiringPitchOverride : minFiringPitch;

  #region Valheim Integrations

#if VALHEIM
    public ZNetView m_nview;
#endif

  #endregion


    protected internal virtual void Awake()
    {
      SetupTransforms();
      InitCoroutines();
      UpdateTurretTypeFromPrefabName();

      _cannonFireAudioSource = cannonFireAudioSourceTransform.GetComponent<AudioSource>();
      _cannonReloadAudioSource = cannonReloadAudioSourceTransform.GetComponent<AudioSource>();

      GetComponentsInChildren(true, _colliders);

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

#if VALHEIM
      m_nview = GetComponent<ZNetView>();
      if (!m_nview)
      {
        m_nview = GetComponentInParent<ZNetView>();
      }
#else
      // Must only be run in Unity Editor. We reset static values so it's like first to load for this object.
      CleanupCannonballPrefab();
#endif
      SetupCannonballPrefab();


      // we start loaded with this. TODO might move this logic into a network level check.
      if (!IsAnyBarrelLoaded && hasLoaded)
      {
        foreach (var shootingPart in shootingBarrelParts)
        {
          var loadedCannonball = GetPooledCannonball(shootingPart);
          loadedCannonball.Load(shootingPart.projectileLoader);
        }
      }

      SightBlockingMask = GetSightBlockingMask();

      InitializePool();

      GetFiringTargetCurrentAimPoint = HandleFiringTargetPositionDefault;
      // --- Setup shooter recoil rotation values ---
    }


    protected internal virtual void Start()
    {
      SetupCannonballPrefab();
    }

    protected internal virtual void FixedUpdate()
    {
      UpdateNearbyBarrels();
      SyncLoadedCannonballs();
      AdjustFiringAngle();
    }

    protected internal virtual void OnEnable()
    {
      Instances.Add(this);
#if DEBUG
      Application.logMessageReceived += OnLogMessageReceived;
#endif
      InitCoroutines();
    }

    protected internal virtual void OnDisable()
    {
      Instances.Remove(this);
#if DEBUG
      Application.logMessageReceived -= OnLogMessageReceived;
#endif
      CleanupPool();
    }

    protected internal virtual void OnDestroy()
    {
      StopAllCoroutines();
      foreach (var cannonball in _cannonballPool)
      {
        if (cannonball)
        {
          if (cannonball)
            Destroy(cannonball);
        }
      }

      foreach (var cannonball in _loadedCannonballs.Values)
      {
        if (cannonball)
        {
          Destroy(cannonball);
        }
      }
    }

    public virtual CannonballVariant AmmoVariant
    {
      get => _ammoVariant;
      set
      {
        _ammoVariant = value;
        SetupCannonballPrefab();
      }
    }


    public CannonFiringMode CannonFiringMode
    {
      get;
      set;
    }

    public Transform GetTiltTransformForPrefab()
    {
      return transform.name.Contains("auto") ? cannonRotationalTransform : cannonShooterTransform;
    }

    // --- Add below your other methods ---
    public void SetManualTilt(float yaw)
    {
      if (cannonRotationalTransform != null)
      {
        var selectedTransform = GetTiltTransformForPrefab();
        var euler = selectedTransform.localEulerAngles;
        euler.x = yaw;
        selectedTransform.localEulerAngles = euler;
      }
    }
    public void SetFiringMode(CannonFiringMode mode)
    {
      cannonFiringMode = mode;
    }

    public void SetAmmoVariant(CannonballVariant val)
    {
      AmmoVariant = val;
    }

    public void SetAmmoVariantFromToken(string tokenId)
    {
      var equippedVariant = AmmoController.GetAmmoVariantFromToken(tokenId);
      AmmoVariant = equippedVariant;
    }

    public void InitCoroutines()
    {
      _cleanupRoutine ??= new CoroutineHandle(this);
      _recoilRoutine ??= new CoroutineHandle(this);
      _reloadRoutine ??= new CoroutineHandle(this);
    }
    public CannonFiringMode GetFiringMode()
    {
      return cannonFiringMode;
    }

    public void UpdateNearbyBarrels()
    {
      if (LastBarrelCheckTime == 0 || PowderBarrel.LastBarrelPlaceTime + Time.deltaTime >= Time.fixedTime || LastBarrelCheckTime + BarrelCheckInterval < Time.fixedTime)
      {
        hasNearbyPowderBarrel = PowderBarrel.FindNearbyBarrels(transform.position, barrelSupplyRadius)?.Count > 0;
        LastBarrelCheckTime = Time.fixedTime;
      }
    }

    public bool IsBarrelLoaded(BarrelPart part)
    {
      return _loadedCannonballs.TryGetValue(part, out var ball) && ball != null;
    }

    public int GetBarrelCount()
    {
      return shootingBarrelParts.Count;
    }

    public void SyncLoadedCannonballFromList(IEnumerable<Cannonball> cannonballs)
    {
      foreach (var ball in cannonballs)
      {
        if (ball != null && !ball.IsInFlight)
        {
          if (ball.m_body != null && ball.m_body.isKinematic)
          {
            var t = ball.lastFireTransform != null ? ball.lastFireTransform : cannonShooterTransform;
            ball.m_body.MovePosition(t.position);
            // ball.m_body.Move(t.position, t.rotation);
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
      return LayerMask.GetMask("terrain");
      // return LayerHelpers.CannonBlockingSiteHitLayers;
    }

    private Vector3? HandleFiringTargetPositionDefault()
    {
      if (currentAimPoint != null && IsHandHeldCannon || firingTarget != null) return currentAimPoint;
      return null;
    }

    public event Action<int> OnAmmoChanged;
    public event Action OnFired;
    public event Action OnReloaded;

    /// <summary>
    /// Matches valheim runtime or unity editor variants of cannons and sets their type.
    /// </summary>
    private void UpdateTurretTypeFromPrefabName()
    {
      var prefabName = transform.name;

      if (prefabName.StartsWith(PrefabNames.CannonFixedTier1) || prefabName.Contains("cannon_fixed"))
      {
        cannonVariant = CannonVariant.Fixed;
        return;
      }

      if (prefabName.StartsWith(PrefabNames.CannonTurretTier1) || prefabName.Contains("cannon_turret"))
      {
        cannonVariant = CannonVariant.Turret;
        return;
      }

      if (prefabName.StartsWith(PrefabNames.CannonHandHeldItem) || prefabName.Contains("handheld"))
      {
        cannonVariant = CannonVariant.HandHeld;
        return;
      }

      LoggerProvider.LogError($"Unknown cannon variant for prefab {prefabName}. Using serialized value {cannonVariant}");
    }

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
          shootingBarrelParts.Add(shootingPart);
        }
      }

      if (shootingBarrelParts.Count == 0)
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

    public static GameObject? SelectCannonballType(CannonballVariant cannonballVariant)
    {
      switch (cannonballVariant)
      {
        case CannonballVariant.Solid:
          return CannonballSolidPrefab;
        case CannonballVariant.Explosive:
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
          CannonballExplosivePrefab = go;
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
          CannonballSolidPrefab = go;
        }
        else
        {
          LoggerProvider.LogWarning("No cannonball explosive prefab asset set. Please set one in CannonballExplosivePrefabAsset or CannonballExplosivePrefabAssetLocal.");
        }
      }
    }

    private void CleanupPool()
    {
      _trackedLoadedCannonballs.RemoveAll(x => x == null);
      foreach (var cannonball in _loadedCannonballs.Values)
      {
        if (cannonball == null) continue;
        Destroy(cannonball);
      }
      _loadedCannonballs.Clear();
      if (_cannonballPool.Count > 0)
      {
        while (_cannonballPool.Count > 0)
        {
          var ball = _cannonballPool.Dequeue();
          if (ball == null) continue;
          Destroy(ball.gameObject);
        }
      }
    }

    public virtual void OnAmmoVariantUpdate(CannonballVariant variant)
    {
      // ensures syncing in case override of AmmoVariant is not using correct keys.
      _ammoVariant = variant;
      CleanupPool();
    }

    internal void SetupCannonballPrefab()
    {
      hasRunSetup = true;

      // must clean the pool if shooting new cannonballs.
      CleanupPool();
      InitCannonballPrefabAssets();
    }

    private void InitializePool()
    {
      _cannonballPool = new Queue<Cannonball>(MinPoolSize);
      var selectedCannonball = SelectCannonballType(AmmoVariant);
      if (!selectedCannonball) return;
      for (var i = 0; i < MinPoolSize; i++)
      {
        foreach (var shootingPart in shootingBarrelParts)
        {
          var go = Instantiate(selectedCannonball, shootingPart.projectileLoader.position, shootingPart.projectileLoader.rotation, null);
          go.name = $"cannonball_queue_{AmmoVariant}_{i}";
          var obj = go.GetComponent<Cannonball>();
          IgnoreVehicleColliders(obj);
          IgnoreLocalColliders(obj);
          go.SetActive(false);
          _cannonballPool.Enqueue(obj);
        }
      }
    }

    // ReSharper disable once UseNullableReferenceTypesAnnotationSyntax
    private void IgnoreVehicleColliders(Cannonball selectedCannonball)
    {
#if VALHEIM
      if (!PiecesController)
      {
        PiecesController = GetComponentInParent<VehiclePiecesController>();
      }
      if (PiecesController != null)
      {
        PiecesController.IgnoreAllVehicleCollidersForGameObjectChildren(selectedCannonball.gameObject);
      }
#endif
    }

    private void IgnoreAllCollidersFromPlayerRoot(Cannonball selectedCannonball)
    {
#if VALHEIM
      // var allColliders = transform.root.GetComponentsInChildren<Collider>();
      var player = transform.GetComponentInParent<Player>();
      if (!player) return;
      var allColliders = player.GetComponentsInChildren<Collider>(true);
      foreach (var allCollider in allColliders)
      {
        if (allCollider == null) continue;
        Physics.IgnoreCollision(allCollider, selectedCannonball.sphereCollisionCollider, true);
      }
#endif
    }

    private void IgnoreLocalColliders(Cannonball cannonball)
    {
      if (cannonball == null || _colliders.Count == 0) return;
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

    public Cannonball GetPooledCannonball(BarrelPart barrelPart)
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
          ball.gameObject.name = $"cannonball_{AmmoVariant}_active_{_cannonballPool.Count}";
          ball.gameObject.SetActive(true);
          _trackedLoadedCannonballs.Add(ball);
          return ball;
        }
      }
      var selectedCannonball = SelectCannonballType(AmmoVariant);
      if (!selectedCannonball)
      {
        LoggerProvider.LogWarning("No cannonball prefab set. Please set one in CannonballSolidPrefab or CannonballExplosivePrefab.");
        return null;
      }

      var go = Instantiate(selectedCannonball, barrelPart.projectileLoader.position, barrelPart.projectileLoader.rotation, null);
      var localCannonball = go.GetComponent<Cannonball>();

      if (cannonVariant == CannonVariant.HandHeld)
      {
        IgnoreAllCollidersFromPlayerRoot(localCannonball);
      }
      else
      {
        IgnoreVehicleColliders(localCannonball);
      }

      IgnoreLocalColliders(localCannonball);
      IgnoreOtherCannonballColliders(localCannonball);
      localCannonball.IgnoredTransformRoots = IgnoredTransformRoots;

      go.gameObject.SetActive(true);

      _trackedLoadedCannonballs.Add(localCannonball);
      go.name = $"cannonball_{AmmoVariant}_active_{_trackedLoadedCannonballs.Count}";
      return localCannonball;
    }

    public void ReturnCannonballToPool(Cannonball ball)
    {
      if (!isActiveAndEnabled) return;
      if (!ball) return;
      _cannonballPool.Enqueue(ball);
      _trackedLoadedCannonballs.Remove(ball);

      ball.gameObject.SetActive(false);

      StartCleanupCoroutine();
    }

    /// <summary>
    /// Numerically finds the firing angle to hit the target, accounting for drag.
    /// </summary>
    public static bool CalculateBallisticAim(
      Vector3 fireOrigin,
      Vector3 targetPosition,
      float launchSpeed,
      float drag,
      out Vector3 fireDirection,
      out float angleDegrees,
      int maxIterations = 1000,
      float tolerance = 0.05f)
    {
      fireDirection = Vector3.zero;
      angleDegrees = 0f;

      var delta = targetPosition - fireOrigin;
      var xzDist = new Vector2(delta.x, delta.z).magnitude;
      var y = delta.y;

      // 1. If target is very close, just aim at it directly
      if (xzDist < 0.01f || delta.magnitude < 0.5f)
      {
        fireDirection = delta.normalized;
        angleDegrees = 0;
        return true;
      }

      // 2. If target is almost straight up or down (vertical shot), shoot straight
      if (xzDist < 1.0f)
      {
        fireDirection = delta.normalized;
        angleDegrees = Mathf.Sign(y) * 90f;
        return true;
      }

      // 3. Standard bisection solve for angle
      var dirXZ = new Vector3(delta.x, 0, delta.z).normalized;
      var low = -Mathf.PI / 2f;
      var high = Mathf.PI / 2f;
      float angle = 0;
      var found = false;

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
          high = angle;
        else
          low = angle;
      }

      if (!found)
      {
        // Fallback: aim straight at the target (not a perfect arc, but at least aim *at* the target)
        fireDirection = delta.normalized;
        angleDegrees = Vector3.Angle(Vector3.ProjectOnPlane(delta, Vector3.up), delta) * Mathf.Sign(y);
        return true; // Don't break contract!
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
        .Where(c => (1 << c.gameObject.layer & LayerHelpers.CharacterLayerMask) != 0 && c.enabled)
        .ToList();


      if (colliders.Count == 0)
        return false;

      // Pick the collider closest to our muzzle or cannon position
      var fireOrigin = cannonShooterAimPoint ? cannonShooterAimPoint.position : transform.position;
      Collider bestCollider = null;
      var bestDist = maxFiringRange;
      var bestPoint = target.position;
      var largestSize = 0f;
      var muzzle = cannonShooterAimPoint ? cannonShooterAimPoint.position : transform.position;

      foreach (var col in colliders)
      {
        var point = col.ClosestPoint(fireOrigin); // Closest point on collider to our muzzle
        var dist = Vector3.Distance(point, fireOrigin);
        var bounds = col.bounds;
        var size = bounds.size.sqrMagnitude;
        var colliderCenter = bounds.center;

        // LoggerProvider.LogDebugDebounced($"[CannonTargeting] Checking collider '{col.name}' on '{col.gameObject.name}' at layer {col.gameObject.layer} | ClosestPoint: {point} | Dist: {Mathf.Sqrt(dist)}");
        if (size == 0) continue;

        if (largestSize < size || largestSize <= size && dist < bestDist)
        {
          var bestCenterPoint = new Vector3(colliderCenter.x, Mathf.Max(bounds.center.y + bounds.extents.y * CannonAimingCenterOffsetY, bounds.center.y), colliderCenter.z);

          largestSize = size;
          bestDist = dist;
          bestPoint = bestCenterPoint;
          bestCollider = col;
          RuntimeDebugLineDrawer.DrawLine(muzzle, bestCenterPoint, Color.green, 3f);
        }
        else
        {
          RuntimeDebugLineDrawer.DrawLine(muzzle, colliderCenter, Color.yellow, 3f);
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
      if (toTargetXZ.sqrMagnitude < 0.01f) return false;

      var currentYaw = cannonRotationalTransform.eulerAngles.y;
      var desiredYaw = QuaternionExtensions.LookRotationSafe(toTargetXZ, Vector3.up).eulerAngles.y;
      var deltaYaw = Mathf.DeltaAngle(currentYaw, desiredYaw);
      if (Mathf.Abs(deltaYaw) > FiringRotationMaxY) return false;

      // --- PITCH CHECK ---
      var fireOrigin = cannonShooterAimPoint.position;
      var delta = targetPosition - fireOrigin;
      var xzDist = new Vector2(delta.x, delta.z).magnitude;
      if (xzDist < 0.01f) return false;

      if (!CalculateBallisticAim(fireOrigin, targetPosition, cannonballSpeed, Cannonball.CannonBallDrag, out _, out var angle))
      {
        // fails alot.
        return false;
      }

      var pitch = -angle;
      return pitch < BarrelPitchMaxAngle || pitch > BarrelPitchMinAngle;
    }

    public void RotateTowardsOrigin()
    {
      if (!float.IsNaN(_lastAllowedYaw))
      {
        // Stay at last allowed yaw, do not follow out-of-range targets
        cannonRotationalTransform.localRotation = Quaternion.Lerp(cannonRotationalTransform.localRotation, Quaternion.identity, Time.fixedDeltaTime);
      }
      else
      {
        cannonRotationalTransform.localRotation = Quaternion.identity;
      }
    }


    public void AdjustFiringAngle()
    {
      if (cannonFiringMode == CannonFiringMode.Manual || IsHandHeldCannon) return;
      if (IsReloading) return;

      var fireOrigin = cannonShooterTransform.position;
      var targetPosition = GetFiringTargetCurrentAimPoint();
      if (!targetPosition.HasValue)
      {
        // RotateTowardsOrigin();
        _canAutoFire = false;
        return;
      }

      // --- YAW ---
      var toTarget = targetPosition.Value - cannonRotationalTransform.position;
      toTarget.y = 0f; // flatten to XZ
      if (toTarget.sqrMagnitude < 0.01f)
      {
        _canAutoFire = false;
        return;
      }

      var desiredYawWorld = QuaternionExtensions.LookRotationSafe(toTarget.normalized, Vector3.up);
      var currentYaw = cannonRotationalTransform.eulerAngles.y;
      var targetYaw = desiredYawWorld.eulerAngles.y;
      var deltaYaw = Mathf.DeltaAngle(currentYaw, targetYaw);

      var isWithinYawRange = Mathf.Abs(deltaYaw) <= maxFiringRotationY;

      if (canRotateFiringRangeY && isWithinYawRange)
      {
        _lastAllowedYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.fixedDeltaTime * aimingSpeed);
        var prevRotation = cannonRotationalTransform.rotation;
        cannonRotationalTransform.rotation = Quaternion.Euler(prevRotation.x, _lastAllowedYaw, prevRotation.z);
      }
      else
      {
        // RotateTowardsOrigin();
        _canAutoFire = false;
        return;
      }
      // --- PITCH (X/Elevation) ---
      var hasPitch = false;
      if (isWithinYawRange && CalculateBallisticAim(fireOrigin, targetPosition.Value, cannonballSpeed, Cannonball.CannonBallDrag, out var fireDir, out var angle))
      {
        // Clamp pitch
        // this can already be mutated by rotational in Y
        var prevRotation = cannonRotationalTransform.localRotation;
        _targetShooterLocalRotation = Quaternion.Euler(Mathf.Clamp(-angle, BarrelPitchMinAngle, BarrelPitchMaxAngle), prevRotation.y, prevRotation.z);
        hasPitch = true;
      }
      else
      {
        hasPitch = false;
        _targetShooterLocalRotation = Quaternion.identity;
      }

      var localRotation = cannonShooterTransform.localRotation;

      localRotation = Quaternion.Lerp(
        localRotation,
        _targetShooterLocalRotation,
        Time.fixedDeltaTime * aimingSpeed
      );
      cannonShooterTransform.localRotation = localRotation;

      // --- Firing check (with angle threshold) ---
      var alignedYaw = Mathf.Abs(Mathf.DeltaAngle(targetYaw, cannonRotationalTransform.rotation.eulerAngles.y)) < 0.5f;
      var alignedPitch = Quaternion.Angle(localRotation, _targetShooterLocalRotation) < 0.5f;

      _canAutoFire = isWithinYawRange && hasPitch && alignedYaw && alignedPitch;
    }

    public bool IsHandHeldCannon => cannonVariant == CannonVariant.HandHeld;

    public bool CanFire(bool isManualFiring, int remainingAmmo)
    {
      if (!isActiveAndEnabled) return false;
      if (remainingAmmo <= 0) return false;
      if (IsReloading || IsFiring || !IsHandHeldCannon && !hasNearbyPowderBarrel) return false;
      // auto fire logic prevents firing while cannon is misaligned with the target.
      if (!isManualFiring && !_canAutoFire) return false;

      return true;
    }

    public static float GetRandomCannonVelocity => Random.value;
    public static float GetRandomCannonArc => Random.Range(-maxSidewaysArcDegrees, maxSidewaysArcDegrees);

    public bool Fire(CannonFireData data, int remainingAmmo, bool isManualFiring, bool isHost = true)
    {
      if (!CanFire(isManualFiring, data.allocatedAmmo))
      {
        return false;
      }
      IsFiring = true;

      // force updates the ammo variant.
      AmmoVariant = data.ammoVariant;

      var currentAmmo = data.allocatedAmmo;
      var hasFired = false;
      for (var index = 0; index < data.cannonShootingPositions.Count; index++)
      {
        if (currentAmmo <= 0) break;
        if (!FireSingle(data, isManualFiring, index)) break;
        currentAmmo--;
        hasFired = true;
      }

      if (!hasFired)
      {
        IsFiring = false;
        return false;
      }

      // use a single audio clip for now. Using multiple is not worth it for perf.
      PlayFireClip();

      OnFired?.Invoke();

      _recoilRoutine.Start(RecoilCoroutine());
      var canReload = !isHost || remainingAmmo > 0;

      if (autoReload && canReload)
      {
        _reloadRoutine.Start(ReloadCoroutine(remainingAmmo));
      }
      return true;
    }

    private bool FireSingle(CannonFireData data, bool isManualFiring, int barrelIndex)
    {
      if (data.cannonShootingPositions == null || data.cannonShootingPositions.Count == 0)
        return false;
      if (barrelIndex < 0 || barrelIndex >= data.cannonShootingPositions.Count)
        return false;

      // Find the matching barrel part by comparing position
      var firePosition = data.cannonShootingPositions[barrelIndex];
      var barrel = shootingBarrelParts.FirstOrDefault(bp =>
        Vector3.Distance(bp.projectileLoader.position, firePosition) < 0.01f);
      if (barrel == null)
        barrel = shootingBarrelParts.ElementAtOrDefault(barrelIndex);
      if (barrel == null)
        return false;

      return FireSingle(data, barrel, data.allocatedAmmo, isManualFiring);
    }

    private bool FireSingle(CannonFireData data, BarrelPart barrel, int barrelCount, bool isManualFiring)
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

      if (!isManualFiring)
      {
        var targetPosition = GetFiringTargetCurrentAimPoint();
        if (!targetPosition.HasValue) return false;
        if (Vector3.Distance(targetPosition.Value, barrel.projectileLoader.position) > maxFiringRange) return false;
      }

      IgnoreLocalColliders(loadedCannonball);
      loadedCannonball.transform.position = barrel.projectileLoader.position;

      var randomVelocityMultiplier = data.randomVelocityValue;
      var localSpeed = cannonballSpeed + randomVelocityMultiplier;

      // var sideArc = Random.Range(-maxSidewaysArcDegrees, maxSidewaysArcDegrees);

      var baseForward = data.shootingDirection;

// Defensive: If forward is degenerate, fallback to transform.forward or world forward.
      if (baseForward.sqrMagnitude < 1e-5f)
      {
        // You can log this once or just silently fallback for performance.
        baseForward = transform.forward.sqrMagnitude > 1e-5f ? transform.forward : Vector3.forward;
#if UNITY_EDITOR
    Debug.LogWarning("CannonController: cannonShooterAimPoint.forward was zero, using fallback for firing direction.");
#endif
      }

      var arcRot = Quaternion.Euler(0f, data.randomArcValue, 0f);
      var arcedForward = arcRot * baseForward;

// Defensive: If arcRot or math yields a bad direction, fallback again.
      if (arcedForward.sqrMagnitude < 1e-5f)
      {
        arcedForward = baseForward.sqrMagnitude > 1e-5f ? baseForward : Vector3.forward;
#if UNITY_EDITOR
    Debug.LogWarning("CannonController: arcedForward was zero, using fallback.");
#endif
      }
      loadedCannonball.CanApplyDamage = data.canApplyDamage;
      var barrelIndex = barrelCount - 1;
      loadedCannonball.Fire(
        data, this, arcedForward.normalized * localSpeed, barrelIndex);

      PlayMuzzleFlash(barrel);

      _trackedLoadedCannonballs.Remove(loadedCannonball);

      _loadedCannonballs[barrel] = null;

      return true;
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
        if (Physics.Raycast(currOrigin, dir, out var hit, remainingDist, obstacleMask))
        {
          // If we hit ourselves, skip past and keep going
          if (hit.transform == transform || hit.transform.IsChildOf(transform))
          {
            currOrigin = hit.point + dir * hit.collider.bounds.size.magnitude; // Move just past this collider
            remainingDist = distance - (currOrigin - fireOrigin).magnitude;
            continue;
          }

          if (raycastDebugDraw)
            RuntimeDebugLineDrawer.DrawLine(fireOrigin, hit.point, RuntimeDebugLineDrawer.TOrange, 0.3f);

          // Any other collider is considered blocking
          return false;
        }
        // No hit = clear shot
        return true;
      }
      // If we exceeded skips, treat as blocked (fail-safe)
      return false;
    }

    public static float recoilUpwardAnimationDuration = 0.5f;
    public static float recoilReturnAnimationDuration = 0.5f;

    private IEnumerator RecoilCoroutine()
    {
      var elapsed = 0f;
      while (elapsed < recoilReturnAnimationDuration + recoilUpwardAnimationDuration)
      {
        var t = Mathf.Clamp01(elapsed / 2f / 0.15f); // 0..1
        elapsed += Time.deltaTime;

        // move towards recoil.
        if (elapsed < 0.1f)
        {
          cannonRotationalTransform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.forward * -0.1f, t);
        }
        else
        {
          cannonRotationalTransform.localPosition = Vector3.Lerp(Vector3.forward * -0.1f, Vector3.zero, t);
        }
        yield return new WaitForFixedUpdate();
      }
      IsFiring = false;
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
      if (condition != null && condition.Contains("Look rotation viewing vector is zero"))
      {
        Debug.LogError("Zero LookRotation: \n" + stackTrace);
      }
    }

    private IEnumerator ReloadCoroutine(int remainingAmmo)
    {
      if (!hasNearbyPowderBarrel && !IsHandHeldCannon)
        yield break;

      IsReloading = true;
      var elapsed = 0f;

      var safeReloadTime = Mathf.Clamp(ReloadTime, 0.1f, 15f);
      PlayReloadClip();
      yield return new WaitForSeconds(safeReloadTime);
      if (_cannonReloadAudioSource.isPlaying)
      {
        yield return new WaitUntil(() => !_cannonReloadAudioSource.isPlaying);
      }

      var shotsToReload = Math.Min(reloadQuantity, shootingBarrelParts.Count);
      for (var i = 0; i < shotsToReload && remainingAmmo - i > 0; i++)
      {
        var shootingPart = shootingBarrelParts[i];
        if (shootingPart == null) break;
        var loaded = GetPooledCannonball(shootingPart);
        loaded.Load(shootingPart.projectileLoader);
        _loadedCannonballs[shootingPart] = loaded;
      }
      yield return new WaitForFixedUpdate();
      IsReloading = false;
      OnReloaded?.Invoke();
    }

    private void PlayMuzzleFlash(BarrelPart? barrelPart)
    {
      if (barrelPart != null && barrelPart.muzzleFlashEffect != null)
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

      if (IsHandHeldCannon)
      {
        _cannonFireAudioSource.time = CannonHandheld_FireAudioStartTime;
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

    public void AddIgnoredTransforms(IEnumerable<Transform> transforms)
    {
      IgnoredTransformRoots.RemoveWhere(x => x == null);
      foreach (var transform1 in transforms)
      {
        IgnoredTransformRoots.Add(transform1);
      }
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