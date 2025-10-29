using System;
using System.Collections;
using UnityEngine;
using Zolantris.Shared;
using Random = UnityEngine.Random;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core
{
  public enum HuntBehaviorState
  {
    Circling,
    Chasing,
    MovingAway,
    Pausing,
    Creeping
  }

  [Serializable]
  public class XenoHuntBehaviorState : IBehaviorState, IBehaviorSharedState
  {
    public int creepDirection = 1;
    public int circleDirection = 1;
    [SerializeField] private float _decisionTimer;
    [SerializeField] public bool FORCE_CreepRetreat = false;

    public float creepSpeedMultiplier => creepDirection == 1 ? 1f : 0.5f;
    public XenoHuntBehaviorConfig behaviorConfig => XenoDroneConfig.xenoHuntBehaviorConfig; // shared ref with integrated state;
    public HuntBehaviorState State = HuntBehaviorState.Circling;
    private MonoBehaviour monoBehaviour;
    private CoroutineHandle timerHandler;


    public void Setup(MonoBehaviour mb)
    {
      monoBehaviour = mb;
      timerHandler ??= new CoroutineHandle(mb);
    }

    public IEnumerator BehaviorTimerRoutine()
    {
      while (monoBehaviour != null && monoBehaviour.isActiveAndEnabled && DecisionTimer > 0)
      {
        yield return new WaitForFixedUpdate();
        DecisionTimer -= Time.fixedDeltaTime;
      }

      DecisionTimer = -1f;
    }

    private bool IsOutOfHuntRange()
    {
      return DeltaPrimaryTarget > behaviorConfig.maxTargetDistance;
    }

    private bool IsWithinHuntingRange()
    {
      if (!PrimaryTarget) return false;
      return DeltaPrimaryTarget >= behaviorConfig.minHuntDistance && DeltaPrimaryTarget <= behaviorConfig.maxTargetDistance;
    }

    /// <summary>
    /// Main Sync Method (must be called during update to be accurate)
    /// </summary>
    /// <param name="sharedState"></param>
    public void SyncSharedData(IBehaviorSharedState sharedState)
    {
      PrimaryTarget = sharedState.PrimaryTarget;
      DeltaPrimaryTarget = sharedState.DeltaPrimaryTarget;
      Self = sharedState.Self;
    }

    /// <summary>
    /// Main updater method.
    /// </summary>
    /// <returns></returns>
    public bool TryUpdateBehavior()
    {
      var isWithinHuntRange = IsWithinHuntingRange();
      if (!isWithinHuntRange)
      {
        return false;
      }
      if (timerHandler == null)
      {
        LoggerProvider.LogError("BehaviorTimerRoutine: Timer handler is null. You must run Setup");
        return false;
      }

      if (timerHandler.IsRunning)
      {
        return true;
      }

      GetNextHuntCircleBehavior();

      timerHandler.Start(BehaviorTimerRoutine());

      return true;
    }

    private void UpdateCirclingBehavior(float rand)
    {
      State = HuntBehaviorState.Circling;
      circleDirection = rand < 0.10f ? -1 : 1;
      DecisionTimer = Random.Range(behaviorConfig.circlingTimeRange.x, behaviorConfig.circlingTimeRange.y);
    }

    private void UpdateMoveAwayBehavior(float rand)
    {
      State = HuntBehaviorState.MovingAway;
      DecisionTimer = Random.Range(behaviorConfig.movingAwayTimeRange.x, behaviorConfig.movingAwayTimeRange.y);
    }

    private void UpdateChaseBehavior()
    {
      State = HuntBehaviorState.Chasing;
      DecisionTimer = Random.Range(behaviorConfig.chasingTimeRange.x, behaviorConfig.chasingTimeRange.y);
    }

    private void UpdatePauseBehavior(float rand)
    {
      State = HuntBehaviorState.Pausing;
      circleDirection = rand < 0.6f ? -1 : 1;
      DecisionTimer = Random.Range(behaviorConfig.pausingTimeRange.x, behaviorConfig.pausingTimeRange.y);
    }

    private void UpdateCreepBehavior(float rand)
    {
      State = HuntBehaviorState.Creeping;

      var targetLookingAtMe = TargetingUtil.IsTargetLookingAtMe(PrimaryTarget, Self);

      var isNearCloseRange = DeltaPrimaryTarget < behaviorConfig.minCreepDistance;

      // do not retreat if nearby otherwise use 50% chance to retreat or advance
      // todo add courage (based on health) and enraged status when health lower than 10% and greater than 25%.
      var shouldRetreat = targetLookingAtMe && rand > 0.5f || isNearCloseRange && rand > 0.9f;

      DecisionTimer = Random.Range(behaviorConfig.creepingTimeRange.x, behaviorConfig.creepingTimeRange.y);

      if (FORCE_CreepRetreat)
      {
        creepDirection = -1;
      }
      else
      {
        creepDirection = shouldRetreat ? -1 : 1;
      }
    }

    private void GetNextHuntCircleBehavior()
    {
      var rand = Random.value;


      if (behaviorConfig.FORCE_Circling)
      {
        UpdateCirclingBehavior(rand);
        return;
      }
      if (behaviorConfig.FORCE_Creeping)
      {
        UpdateCreepBehavior(rand);
        return;
      }

      // If not chasing and able to start chasing, do so.
      // If already chasing and not out of range, continue chasing.
      if (State != HuntBehaviorState.Chasing && DeltaPrimaryTarget < behaviorConfig.attackStartChaseDistance || State == HuntBehaviorState.Chasing && DeltaPrimaryTarget < behaviorConfig.attackExitChaseDistance)
      {
        UpdateChaseBehavior();
        return;
      }

      var cursor = 0f;

      if (rand < (cursor += behaviorConfig.probMovingAway))
      {
        UpdateMoveAwayBehavior(rand);
        return;
      }
      if (rand < (cursor += behaviorConfig.probPausing))
      {
        UpdatePauseBehavior(rand);
        return;
      }
      if (rand < (cursor += behaviorConfig.probCreeping))
      {
        UpdateCreepBehavior(rand);
      }
      else
      {
        // circling is a fallthrough state.
        UpdateCirclingBehavior(rand);
      }

      if (cursor >= 1f)
      {
        LoggerProvider.LogDebugDebounced("Error state above 100% probability states below this will not fire.");
      }
      LoggerProvider.LogDebug($"Selected <{State}> with prob {cursor}");
    }

    public float DecisionTimer
    {
      get => _decisionTimer;
      set => _decisionTimer = value;
    }

    public Transform PrimaryTarget
    {
      get;
      set;
    }
    public Transform Self
    {
      get;
      set;
    }
    public float DeltaPrimaryTarget
    {
      get;
      set;
    }
  }
}