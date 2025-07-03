// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class PowderBarrel : MonoBehaviour
  {

    public const string BarrelExplosionColliderName = "barrel_explosion_collider";
    [SerializeField] private float destroyDeactivationDelayTimeInMs;
    [Tooltip("Hides the barrel mesh when the explosion hits the destroy deactivation timer.")]
    [SerializeField] public bool shouldHideBarrelMeshOnExplode = true;
    private readonly Collider[] allocatedColliders = new Collider[100];
    private CoroutineHandle _aoeRoutine;

    private CoroutineHandle _explosionRoutine;
    private AudioSource explosionAudio;
    private BoxCollider explosionCollider;
    private ParticleSystem explosionFx;
    private Transform explosionFxTransform;
    private Transform explosionTransform;
    private Transform meshesTransform;

    // meant for integration, we need to destroy the barrel.
    [CanBeNull] public Action onExplosionComplete = () =>
    {
    };

    public Action<Collider> OnHitCollider = c =>
    {
      LoggerProvider.LogDev($"Hit collider {c.name}");
    };

    private void Awake()
    {
      onExplosionComplete = () =>
      {
        Destroy(gameObject);
      };
      
      _explosionRoutine = new CoroutineHandle(this);
      _aoeRoutine = new CoroutineHandle(this);
      
      explosionTransform = transform.Find("explosion");
      meshesTransform = transform.Find("meshes");
      explosionFxTransform = explosionTransform.Find("explosion_effect");
      explosionCollider = transform.Find(BarrelExplosionColliderName).GetComponent<BoxCollider>();;
    
      explosionAudio = explosionTransform.GetComponent<AudioSource>();
      explosionFx = explosionFxTransform.GetComponent<ParticleSystem>();
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
      int count = Physics.OverlapSphereNonAlloc(explosionOrigin, 5f, allocatedColliders, LayerHelpers.PieceLayerMask);

      // Gather all barrels and distances.
      var barrels = new List<(PowderBarrel barrel, float distance)>(count);
      for (int i = 0; i < count; i++)
      {
        var col = allocatedColliders[i];
        if (col.name == BarrelExplosionColliderName)
        {
          var powderBarrel = col.GetComponentInParent<PowderBarrel>();
          if (powderBarrel != null)
          {
            float dist = Vector3.Distance(explosionOrigin, powderBarrel.transform.position);
            barrels.Add((powderBarrel, dist));
          }
          continue;
        }
        // Not a barrel, invoke fallback
        OnHitCollider?.Invoke(col);
      }

      // Sort by distance (ascending: nearest first)
      barrels.Sort((a, b) => a.distance.CompareTo(b.distance));

      // Fire explosions in order
      foreach (var (barrel, _) in barrels)
      {
        barrel.StartExplosion();
        LoggerProvider.LogDev($"Triggered powder barrel explosion at {barrel.transform.position}");
        yield return new WaitForSeconds(0.5f); // Adjust as needed
      }
    }

    private IEnumerator Explode()
    {
      LoggerProvider.LogDev("Exploding");
      explosionFx.Play();
      explosionAudio.Play();

      var timer = Stopwatch.StartNew();
      
      if (destroyDeactivationDelayTimeInMs > 0)
      {
        yield return new WaitUntil(() => timer.ElapsedMilliseconds > destroyDeactivationDelayTimeInMs);
      }
      if (shouldHideBarrelMeshOnExplode)
      {
        meshesTransform.gameObject.SetActive(false);
      }
      yield return new WaitUntil(() => timer.ElapsedMilliseconds > 10000f || explosionFx.isStopped && !explosionAudio.isPlaying);
      timer.Reset();
      onExplosionComplete?.Invoke();
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