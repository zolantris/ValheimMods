// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using Zolantris.Shared;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class PowderBarrel : MonoBehaviour
  {

    public const string BarrelExplosionColliderName = "barrel_explosion_collider";
    [SerializeField] private float destroyDeactivationDelayTimeInMs = 0.25f;
    [Tooltip("Hides the barrel mesh when the explosion hits the destroy deactivation timer.")]
    [SerializeField] public bool shouldHideBarrelMeshImmediatelyOnExplode = false;
    [SerializeField] public bool CanDestroyOnExplode = true;
    [SerializeField] public bool CanExplodeMultipleTimes = false;
    private readonly Collider[] allocatedColliders = new Collider[100];
    public static float BarrelExplosionChainDelay = 0.25f;
    private CoroutineHandle _aoeRoutine;

    private CoroutineHandle _explosionRoutine;
    private AudioSource explosionAudio;
    private BoxCollider explosionCollider;
    private ParticleSystem explosionFx;
    private Transform explosionFxTransform;
    private Transform explosionTransform;
    private Transform meshesTransform;

    public static HashSet<PowderBarrel> ExplodingBarrels = new();

    public static float LastBarrelPlaceTime;
#if VALHEIM
    public WearNTear wearNTear;
#endif

    public Action<Collider> OnHitCollider = c =>
    {
      LoggerProvider.LogDev($"Hit collider {c.name}");
    };

    private void Awake()
    {
      _explosionRoutine = new CoroutineHandle(this);
      _aoeRoutine = new CoroutineHandle(this);
      explosionTransform = transform.Find("explosion");
      meshesTransform = transform.Find("meshes");
      explosionFxTransform = explosionTransform.Find("explosion_effect");
      explosionCollider = transform.Find(BarrelExplosionColliderName).GetComponent<BoxCollider>();

      explosionAudio = explosionTransform.GetComponent<AudioSource>();
      explosionFx = explosionFxTransform.GetComponent<ParticleSystem>();
#if VALHEIM
      wearNTear = GetComponent<WearNTear>();
#endif
    }

    private void Start()
    {
      LastBarrelPlaceTime = Time.fixedTime;
    }

    public void OnEnable()
    {
#if VALHEIM
      wearNTear = GetComponent<WearNTear>();
      if (wearNTear != null)
      {
        wearNTear.m_onDamaged += OnWearNTearDamage;
      }
#endif
    }

    public void OnDisable()
    {
#if VALHEIM
      wearNTear = GetComponent<WearNTear>();
      if (wearNTear != null)
      {
        wearNTear.m_onDamaged -= OnWearNTearDamage;
      }
#endif
    }

    public void OnExplodeDestroy()
    {
#if VALHEIM
      if (wearNTear == null) return;
      wearNTear.Destroy(null, true);
#else
          Destroy(gameObject);
#endif
    }

    /// <summary>
    /// Destroys the prefab on wearntear damage.
    /// </summary>
    public void OnWearNTearDamage()
    {
#if VALHEIM
      if (wearNTear == null) return;
      if (_explosionRoutine.IsRunning) return;
      if (wearNTear.m_healthPercentage <= 0.75f)
      {
        StartExplosion();
      }
#endif
    }

    /// <summary>
    /// update cannons nearby with slight delay.
    /// </summary>
    public void StartUpdateNearbyCannonsOnPlace()
    {
      Invoke(nameof(UpdateNearbyCannonsOnPlace), 0.5f);
    }

    [CanBeNull]
    public static List<PowderBarrel> UpdateNearbyCannonsOnPlace(Vector3 position, float radius)
    {
      // ReSharper disable once Unity.PreferNonAllocApi
      var colliders = Physics.OverlapSphere(position, CannonController.BarrelSupplyDistance, LayerHelpers.PieceLayerMask);
      var barrels = new List<PowderBarrel>();

      for (var i = 0; i < colliders.Length; i++)
      {
        var col = colliders[i];
        if (col.name != BarrelExplosionColliderName) continue;
        var controller = col.GetComponentInParent<CannonController>();
        if (controller != null)
        {
          controller.hasNearbyPowderBarrel = true;
        }
      }

      return barrels;
    }

    [CanBeNull]
    public static List<PowderBarrel> FindNearbyBarrels(Vector3 position, float radius)
    {
      // ReSharper disable once Unity.PreferNonAllocApi
      var colliders = Physics.OverlapSphere(position, radius, LayerHelpers.PieceLayerMask);
      var barrels = new List<PowderBarrel>();
      for (var i = 0; i < colliders.Length; i++)
      {
        var col = colliders[i];
        if (col.name != BarrelExplosionColliderName) continue;
        var powderBarrel = col.GetComponentInParent<PowderBarrel>();
        if (powderBarrel != null)
        {
          barrels.Add(powderBarrel);
        }
      }

      return barrels;
    }

    private IEnumerator FindNearbyBarrelsToExplode()
    {
      if (!isActiveAndEnabled) yield break;
      yield return new WaitForFixedUpdate();

      var explosionOrigin = explosionCollider.bounds.center;
      var count = Physics.OverlapSphereNonAlloc(explosionOrigin, 5f, allocatedColliders, LayerHelpers.CannonHitLayers);

      // Gather all barrels and distances.
      var barrels = new List<(PowderBarrel barrel, float distance)>(count);
      for (var i = 0; i < count; i++)
      {
        var col = allocatedColliders[i];
        var hitPoint = col.ClosestPointOnBounds(explosionOrigin);
        var dir = (hitPoint - explosionOrigin).normalized;
        var dist = Vector3.Distance(explosionOrigin, hitPoint);

        if (col.name == BarrelExplosionColliderName)
        {
          var powderBarrel = col.GetComponentInParent<PowderBarrel>();
          if (powderBarrel != null)
          {
            if (ExplodingBarrels.Add(powderBarrel))
            {
              barrels.Add((powderBarrel, dist));
            }
          }
          continue;
        }

        CannonballHitScheduler.AddDamageToQueue(col, hitPoint, dir, Vector3.zero, 90f, true);

        // Not a barrel, invoke fallback
        OnHitCollider?.Invoke(col);
      }

      // Sort by distance (ascending: nearest first)
      barrels.Sort((a, b) => a.distance.CompareTo(b.distance));

      // Fire explosions in order
      foreach (var (barrel, _) in barrels)
      {
        barrel.StartExplosion();
#if DEBUG
        LoggerProvider.LogDev($"Triggered powder barrel explosion at {barrel.transform.position}");
#endif
        yield return new WaitForSeconds(BarrelExplosionChainDelay); // Adjust as needed
      }

      foreach (var (barrel, _) in barrels)
      {
        ExplodingBarrels.Remove(barrel);
      }
    }

    private IEnumerator Explode()
    {
      explosionFx.Play();
      explosionAudio.Play();

      var timer = Stopwatch.StartNew();

      if (destroyDeactivationDelayTimeInMs > 0)
      {
        yield return new WaitUntil(() => timer.ElapsedMilliseconds > destroyDeactivationDelayTimeInMs);
      }
      if (shouldHideBarrelMeshImmediatelyOnExplode && !CanExplodeMultipleTimes)
      {
        meshesTransform.gameObject.SetActive(false);
      }
      else
      {
        meshesTransform.localScale = new Vector3(1f, 0.1f, 1f);
      }

      var maxEffectsTimeInMs = Mathf.Max(explosionFx.totalTime, explosionAudio.clip.length) * 1000;

      // hide mesh transform (halfway through explosion.
      if (!shouldHideBarrelMeshImmediatelyOnExplode && !CanExplodeMultipleTimes)
      {
        yield return new WaitUntil(() => timer.ElapsedMilliseconds > maxEffectsTimeInMs / 2f);
        meshesTransform.gameObject.SetActive(false);
      }

      yield return new WaitUntil(() => timer.ElapsedMilliseconds > maxEffectsTimeInMs || !explosionFx.isPlaying && !explosionAudio.isPlaying);
      timer.Reset();
      OnExplodeDestroy();
      yield return null;
    }

    public void StartExplosion()
    {
      if (_explosionRoutine.IsRunning) return;
      _explosionRoutine.Start(Explode());
      _aoeRoutine.Start(FindNearbyBarrelsToExplode());
    }
  }
}