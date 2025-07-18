// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public struct DamageInfo
  {
    public Collider collider;
    public float force;
    public Vector3 velocity;
    public Vector3 direction;
    public Vector3 hitPoint;
    public float explosionRadius;
    public bool isExplosionHit;
    public bool isCharacterHit;
    public bool isMineRockHit;
    public bool isMineRock5Hit;
    public bool isDestructibleHit;
    public bool isSelfHit;
    public float damage;
    public CannonballVariant cannonballVariant;
  }


  public class CannonballHitScheduler : SingletonBehaviour<CannonballHitScheduler>
  {
    private static CoroutineHandle _applyDamageCoroutine;
    private static CoroutineHandle _applyAudioCoroutine;
    private static readonly Queue<DamageInfo> _queuedDamageInfo = new();
    public static bool UseCharacterHit = false;

    [CanBeNull] public static Cannonball CurrentImpactAudioCannonball;
    public static Queue<(Cannonball, float)> CannonballImpactAudioQueue = new();
    // damage types.
    public static float BaseDamageExplosiveCannonball = 50f;
    public static float BaseDamageSolidCannonball = 80f;
    public static float ExplosionShellRadius = 7.5f;

    public void OnEnable()
    {
      SetupCoroutines();
    }
    public void OnDisable()
    {
      SetupCoroutines();
    }

    public override void OnAwake()
    {
      SetupCoroutines();
    }

    public static void SetupCoroutines()
    {
      _applyDamageCoroutine ??= new CoroutineHandle(Instance);
      _applyAudioCoroutine ??= new CoroutineHandle(Instance);
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EditorDomainReloadInit()
    {
      if (Instance == null)
      {
        var go = new GameObject("CannonballHitScheduler");
        Instance = go.AddComponent<CannonballHitScheduler>();
        DontDestroyOnLoad(go);
        SetupCoroutines();
      }
    }
#endif

    private static void CommitDamage(DamageInfo damageInfo)
    {
      if (!TryInit()) return;
      var damageCollider = damageInfo.collider;
      if (damageCollider == null) return;
#if !UNITY_2022 && !UNITY_EDITOR
      // hit is invalid.
      var hitData = new HitData
      {
        m_hitCollider = damageCollider
      };
      hitData.m_damage.m_damage = damageInfo.damage * 0.5f;
      hitData.m_damage.m_blunt = damageInfo.isExplosionHit ? 0f : damageInfo.damage * 0.5f;
      hitData.m_damage.m_pickaxe = damageInfo.isExplosionHit ? 0f : damageInfo.damage;
      hitData.m_damage.m_fire = damageInfo.isExplosionHit ? damageInfo.damage : 0f;
      hitData.m_toolTier = 100;
      hitData.m_point = damageInfo.hitPoint;
      hitData.m_dir = damageInfo.direction;
      hitData.m_attacker = Player.m_localPlayer.GetZDOID();
      hitData.m_hitType = HitData.HitType.Impact;
      hitData.m_pushForce = 2f;
      hitData.m_staggerMultiplier = 12f;
      hitData.m_radius = damageInfo.explosionRadius;
      hitData.m_ranged = true;
      hitData.m_dodgeable = false;
      hitData.m_blockable = false;

      if (UseCharacterHit && damageCollider != null)
      {
        var character = damageCollider.GetComponentInParent<Character>();
        if (character != null)
        {
          var player = character as Player;
          var isSelfHit = player != null && player == Player.m_localPlayer;
          if (isSelfHit)
          {
            hitData.m_hitType = HitData.HitType.EnemyHit;
          }
          else
          {
            hitData.m_hitType = HitData.HitType.PlayerHit;
          }
        }
      }

      if (damageInfo.isMineRock5Hit)
      {
        var mineRock5 = damageCollider.GetComponentInParent<MineRock5>();
        if (mineRock5 != null)
        {
          mineRock5.Damage(hitData);
        }
      }
      else if (damageInfo.isMineRockHit)
      {
        var mineRock = damageCollider.GetComponentInParent<MineRock>();
        if (mineRock != null)
        {
          mineRock.Damage(hitData);
        }
      }
      else if (damageInfo.isDestructibleHit)
      {
        var destructible = damageCollider.GetComponentInParent<IDestructible>();
        if (destructible != null)
        {
          destructible.Damage(hitData);
        }
      }
#else
        LoggerProvider.LogWarning("Cannonball damage not implemented!");
#endif
    }

    private static bool TryInit()
    {
#if !UNITY_EDITOR && !UNITY_2022
      if (!Game.instance) return false;
#endif
      if (Instance != null) return true;
#if !UNITY_EDITOR && !UNITY_2022
      Instance = Game.instance.gameObject.AddComponent<CannonballHitScheduler>();
#else
      var go = new GameObject("CannonballHitScheduler");
      Instance = go.AddComponent<CannonballHitScheduler>();
      DontDestroyOnLoad(go);
#endif
      if (Instance)
      {
        SetupCoroutines();
      }
      return Instance != null;
    }

    /// <summary>
    /// For handling powderbarrels too
    /// </summary>
    ///  Todo make this a bit less focused on cannonballs and more on types of hits
    public static DamageInfo GetDamageInfoForHit(PowderBarrel powderBarrel, Collider otherCollider, Vector3 hitPoint, Vector3 dir, Vector3 velocity, float force, bool isExplosionHit)
    {
#if !UNITY_EDITOR && !UNITY_2022

      // The main types of damage that can be done.
      // current order should be MineRock5 -> MineRock -> IDestructible
      // todo only get components needed based on order so either components are not fetched if one of them is truthy.
      var character = otherCollider.GetComponentInParent<Character>();
      var mineRock5 = otherCollider.GetComponentInParent<MineRock5>();
      var mineRock = otherCollider.GetComponentInParent<MineRock>();
      var destructible = otherCollider.GetComponentInParent<IDestructible>();

      var isMineRockHit = mineRock != null;
      var isMineRock5Hit = mineRock5 != null;
      var isDestructibleHit = destructible != null;

      if (isDestructibleHit)
      {
        if (isMineRockHit)
        {
          LoggerProvider.LogDebug($"IsMineRock equal to IDestructible {destructible == mineRock as IDestructible}");
        }

        if (isMineRock5Hit)
        {
          LoggerProvider.LogDebug($"IsMineRock5 equal to IDestructible {destructible == mineRock5 as IDestructible}");
        }
      }

      var isCharacterHit = character != null;
      var isSelfHit = isCharacterHit && character as Player == Player.m_localPlayer;

      var baseDamage = BaseDamageExplosiveCannonball;
      // makes cannonball damage variable within specific bounds.
      var lerpedForceDamage = Mathf.Lerp(0.1f, 1.5f, force / 90f);
      var forceDamage = Mathf.Clamp(baseDamage * lerpedForceDamage, 10f, 300f);

      var damageInfo = new DamageInfo
      {
        collider = otherCollider,
        velocity = velocity,
        direction = dir,
        hitPoint = hitPoint,
        force = force,
        isExplosionHit = isExplosionHit,
        isCharacterHit = isCharacterHit,
        isMineRockHit = isMineRockHit,
        isMineRock5Hit = isMineRock5Hit,
        isDestructibleHit = isDestructibleHit,
        isSelfHit = isSelfHit,
        explosionRadius = (isMineRock5Hit || isMineRockHit) && isExplosionHit ? ExplosionShellRadius : 0f,
        cannonballVariant = CannonballVariant.Explosive,
        damage = forceDamage
      };
#else
      var damageInfo = new DamageInfo();
#endif

      return damageInfo;
    }

    public static DamageInfo GetDamageInfoForHit(Cannonball cannonball, Collider otherCollider, Vector3 hitPoint, Vector3 dir, Vector3 velocity, float force, bool isExplosionHit)
    {
#if !UNITY_EDITOR && !UNITY_2022

      // The main types of damage that can be done.
      // current order should be MineRock5 -> MineRock -> IDestructible
      // todo only get components needed based on order so either components are not fetched if one of them is truthy.
      var character = otherCollider.GetComponentInParent<Character>();
      var mineRock5 = otherCollider.GetComponentInParent<MineRock5>();
      var mineRock = otherCollider.GetComponentInParent<MineRock>();
      var destructible = otherCollider.GetComponentInParent<IDestructible>();

      var isMineRockHit = mineRock != null;
      var isMineRock5Hit = mineRock5 != null;
      var isDestructibleHit = destructible != null;

      if (isDestructibleHit)
      {
        if (isMineRockHit)
        {
          LoggerProvider.LogDebug($"IsMineRock equal to IDestructible {destructible == mineRock as IDestructible}");
        }

        if (isMineRock5Hit)
        {
          LoggerProvider.LogDebug($"IsMineRock5 equal to IDestructible {destructible == mineRock5 as IDestructible}");
        }
      }

      var isCharacterHit = character != null;
      var isSelfHit = isCharacterHit && character as Player == Player.m_localPlayer;

      var cannonballVariant = cannonball.cannonballVariant;
      var isSolidCannonball = cannonballVariant == CannonballVariant.Solid;

      var baseDamage = isSolidCannonball ? BaseDamageSolidCannonball : BaseDamageExplosiveCannonball;
      // makes cannonball damage variable within specific bounds.
      var lerpedForceDamage = Mathf.Lerp(0.1f, 1.5f, force / 90f);
      var forceDamage = Mathf.Clamp(baseDamage * lerpedForceDamage, 10f, 300f);

      var damageInfo = new DamageInfo
      {
        collider = otherCollider,
        velocity = velocity,
        direction = dir,
        hitPoint = hitPoint,
        force = force,
        isExplosionHit = isExplosionHit,
        isCharacterHit = isCharacterHit,
        isMineRockHit = isMineRockHit,
        isMineRock5Hit = isMineRock5Hit,
        isDestructibleHit = isDestructibleHit,
        isSelfHit = isSelfHit,
        explosionRadius = (isMineRock5Hit || isMineRockHit) && isExplosionHit ? ExplosionShellRadius : 0f,
        cannonballVariant = cannonballVariant,
        damage = forceDamage
      };
#else
      var damageInfo = new DamageInfo();
#endif

      return damageInfo;
    }

    public static void AddDamageToQueue(DamageInfo damageInfo)
    {
      if (!TryInit()) return;
      _queuedDamageInfo.Enqueue(damageInfo);
      ScheduleCommitDamage();
    }

    public static void AddDamageToQueue(PowderBarrel powderBarrel, Collider otherCollider, Vector3 hitPoint, Vector3 dir, Vector3 velocity, float force, bool isExplosionHit)
    {
      if (!TryInit()) return;
#if !UNITY_EDITOR && !UNITY_2022
      var damageInfo = GetDamageInfoForHit(powderBarrel, otherCollider, hitPoint, dir, velocity, force, isExplosionHit);
      _queuedDamageInfo.Enqueue(damageInfo);
#endif
      ScheduleCommitDamage();
    }

    public static void AddDamageToQueue(Cannonball cannonball, Collider otherCollider, Vector3 hitPoint, Vector3 dir, Vector3 velocity, float force, bool isExplosionHit)
    {
      if (!TryInit()) return;
#if !UNITY_EDITOR && !UNITY_2022
      var damageInfo = GetDamageInfoForHit(cannonball, otherCollider, hitPoint, dir, velocity, force, isExplosionHit);
      _queuedDamageInfo.Enqueue(damageInfo);
#endif
      ScheduleCommitDamage();
    }

    public static void ScheduleCommitDamage()
    {
      if (!TryInit()) return;
      if (_applyDamageCoroutine.IsRunning) return;
      _applyDamageCoroutine.Start(CommitDamageCoroutine());
    }

    public static void ScheduleImpactSound(Cannonball cannonball, float impactVelocity)
    {
      if (!TryInit()) return;
      CannonballImpactAudioQueue.Enqueue((cannonball, impactVelocity));
      if (_applyAudioCoroutine.IsRunning) return;
      _applyAudioCoroutine.Start(ImpactEffectSchedulerCoroutine());
    }

    public static IEnumerator ImpactEffectSchedulerCoroutine()
    {
      while (CannonballImpactAudioQueue.Count > 0)
      {
        var (cannonball, impactVelocity) = CannonballImpactAudioQueue.Dequeue();
        CurrentImpactAudioCannonball = cannonball;
        if (cannonball != null && cannonball.isActiveAndEnabled && cannonball.gameObject.activeInHierarchy)
        {
          cannonball.StartImpactEffectAudio(impactVelocity);
        }
        yield return new WaitForSeconds(Random.value * 0.1f);
      }
    }

    /// <summary>
    /// Damage Coroutine that will wait for next frame if there was >10ms of timer passing when running methods.
    /// </summary>
    /// <returns></returns>
    public static IEnumerator CommitDamageCoroutine()
    {
      if (!TryInit()) yield break;
      yield return new WaitForFixedUpdate();
      var timer = Stopwatch.StartNew();
      const int maxLoops = 1000;
      var count = 0;
      while (_queuedDamageInfo.Count > 0 && count < maxLoops)
      {
        count++;
        if (timer.ElapsedMilliseconds > 10)
        {
          yield return null;
          timer.Restart();
        }

        var damageInfo = _queuedDamageInfo.Dequeue();
        if (damageInfo.collider == null)
        {
          continue;
        }
        CommitDamage(damageInfo);
      }
      timer.Stop();
    }
  }
}