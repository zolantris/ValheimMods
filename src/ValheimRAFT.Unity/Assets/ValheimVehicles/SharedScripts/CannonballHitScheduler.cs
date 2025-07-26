// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    private static CoroutineHandle _applyShieldUpdate;

    private static readonly Queue<DamageInfo> _queuedDamageInfo = new();
    public static bool UseCharacterHit = false;

    [CanBeNull] public static Cannonball CurrentImpactAudioCannonball;
    public static Queue<(Cannonball, float)> CannonballImpactAudioQueue = new();
    // damage types.
    public static float BaseDamageExplosiveCannonball = 50f;
    public static float BaseDamageSolidCannonball = 80f;
    public static float ExplosionShellRadius = 7.5f;

    public static Dictionary<ShieldGenerator, (Cannonball, float, ShieldGenerator)> m_scheduledShieldUpdates = new();
    public static Dictionary<ShieldGenerator, HashSet<(Vector3, Quaternion)>> m_scheduledShieldHitPoints = new();

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

    /// <summary>
    /// These are static but they must match the current instance so force updating these is required per instance update.
    /// </summary>
    public static void SetupCoroutines()
    {
      if (Instance == null) return;

      _applyDamageCoroutine ??= new CoroutineHandle(Instance);
      _applyAudioCoroutine ??= new CoroutineHandle(Instance);
      _applyShieldUpdate ??= new CoroutineHandle(Instance);

      if (!_applyDamageCoroutine.IsValid(Instance)) _applyDamageCoroutine = new CoroutineHandle(Instance);
      if (!_applyAudioCoroutine.IsValid(Instance)) _applyAudioCoroutine = new CoroutineHandle(Instance);
      if (!_applyShieldUpdate.IsValid(Instance)) _applyShieldUpdate = new CoroutineHandle(Instance);
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

    /// <summary>
    /// Only adds keys if they are not already scheduled.
    /// </summary>
    public static void AddShieldUpdate(Cannonball cannonball, float force, ShieldGenerator shieldGenerator)
    {
      if (!m_scheduledShieldUpdates.ContainsKey(shieldGenerator))
      {
        m_scheduledShieldUpdates.Add(shieldGenerator, (cannonball, force, shieldGenerator));
      }

      if (!m_scheduledShieldHitPoints.TryGetValue(shieldGenerator, out var hitPoints))
      {
        hitPoints = new HashSet<(Vector3, Quaternion)>();
        m_scheduledShieldHitPoints[shieldGenerator] = hitPoints;
      }
      var cannonballPosition = cannonball.transform.position;
      hitPoints.Add((cannonballPosition, Quaternion.LookRotation(shieldGenerator.transform.position.DirTo(cannonballPosition))));

      ScheduleUpdateShieldHit();
    }

    public static void ScheduleUpdateShieldHit()
    {
      if (!TryInit()) return;
      if (_applyShieldUpdate.IsRunning) return;
      _applyShieldUpdate.Start(UpdateShieldHitRoutine());
    }

    /// <summary>
    /// This will prevent spamming the clients with hits.
    /// </summary>
    public static IEnumerator UpdateShieldHitRoutine()
    {
      yield return new WaitForFixedUpdate();
      var queuedHits = m_scheduledShieldUpdates.Values.ToList();

      foreach (var (cannonball, force, shieldGenerator) in queuedHits)
      {
        if (shieldGenerator == null || cannonball == null) continue;
        if (!m_scheduledShieldHitPoints.TryGetValue(shieldGenerator, out var hits))
        {
          continue;
        }

        foreach (var (pos, dir) in hits)
        {
          shieldGenerator.m_shieldHitEffects.Create(pos, dir);
        }

        shieldGenerator.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_HitNow");
        shieldGenerator.UpdateShield();
        // only impact effects regardless if the shell is an explosive.
        cannonball.StartImpactEffectAudio(force, true);
        yield return null;
      }

      m_scheduledShieldHitPoints.Clear();
      m_scheduledShieldUpdates.Clear();
      yield return new WaitForSeconds(0.1f);
    }

    private static void CommitDamage(DamageInfo damageInfo)
    {
      if (!TryInit()) return;
      var damageCollider = damageInfo.collider;
      if (damageCollider == null) return;
#if VALHEIM
      // hit is invalid.
      var isPlayerNull = Player.m_localPlayer == null;
      var hitZdoid = isPlayerNull ? ZDOID.None : Player.m_localPlayer!.GetZDOID();
      var hitData = new HitData
      {
        m_hitCollider = damageCollider
      };
      hitData.m_damage.m_blunt = damageInfo.isExplosionHit ? damageInfo.damage * 0.5f : damageInfo.damage;
      hitData.m_damage.m_chop = damageInfo.damage * 0.5f;
      // full damage for pickaxe when hit with a solid (explosive is less but over an area) 
      hitData.m_damage.m_pickaxe = damageInfo.isExplosionHit ? damageInfo.damage * 0.25f : damageInfo.damage;
      hitData.m_damage.m_fire = damageInfo.isExplosionHit ? damageInfo.damage : 0f;
      hitData.m_toolTier = 100;
      hitData.m_point = damageInfo.hitPoint;
      hitData.m_dir = damageInfo.direction;
      hitData.m_attacker = hitZdoid;
      hitData.m_hitType = damageInfo.isSelfHit ? HitData.HitType.Self : HitData.HitType.EnemyHit;
      hitData.m_pushForce = 1f;
      hitData.m_staggerMultiplier = 12f;
      hitData.m_radius = 0; // do not use radius hits as these will do an additional raycast which is not necessary.
      hitData.m_ranged = true;
      hitData.m_dodgeable = true;
      hitData.m_blockable = true;

      // no chop/pickaxe damage for character hit.
      if (damageInfo.isCharacterHit)
      {
        hitData.m_damage.m_chop = 0;
        hitData.m_damage.m_pickaxe = 0;
      }

      // do not add additional damage types.
      if (damageInfo.isMineRock5Hit || damageInfo.isMineRockHit)
      {
        hitData.m_damage.m_blunt = 0;
        hitData.m_damage.m_chop = 0;
        hitData.m_damage.m_fire = 0;
      }

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
#if VALHEIM
      if (!Game.instance) return false;
#endif
      if (Instance != null) return true;
#if VALHEIM
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

    public static float GetBaseDamageForHit(bool isExplosionHit, float force)
    {
      var baseDamage = isExplosionHit ? BaseDamageExplosiveCannonball : BaseDamageSolidCannonball;
      // makes cannonball damage variable within specific bounds.
      var lerpedForceDamage = Mathf.Lerp(0.1f, 1.5f, force / 90f);
      var forceDamage = Mathf.Clamp(baseDamage * lerpedForceDamage, 10f, 300f);
      return forceDamage;
    }

    public static DamageInfo GetDamageInfoForHit(Collider otherCollider, Vector3 hitPoint, Vector3 dir, Vector3 velocity, float force, bool isExplosionHit)
    {
#if VALHEIM

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


      var damage = GetBaseDamageForHit(isExplosionHit, force);


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
        damage = damage
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

    public static void AddDamageToQueue(Collider otherCollider, Vector3 hitPoint, Vector3 dir, Vector3 velocity, float force, bool isExplosionHit)
    {
      if (!TryInit()) return;
#if VALHEIM
      var damageInfo = GetDamageInfoForHit(otherCollider, hitPoint, dir, velocity, force, isExplosionHit);
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
      var lastPauseCount = 0;
      while (_queuedDamageInfo.Count > 0 && count < maxLoops)
      {
        count++;
        if (timer.ElapsedMilliseconds > 10)
        {
          yield return null;
          timer.Restart();
        }

        // prevent damage from spamming network on single frame.
        if (count - lastPauseCount > 10)
        {
          lastPauseCount = count;
          yield return null;
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