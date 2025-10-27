using Eldritch.Core;
using UnityEngine;
namespace Eldritch.Valheim;

public class XenoDrone_MonsterAI : MonsterAI
{
  public XenoDroneAI DroneAI;
  public XenoAIMovementController MovementController;

  public static bool ShouldSkipAiMovement = false;
  public static bool ShouldSkipUpdateAI = false;

  public override void Awake()
  {
    DroneAI = GetComponent<XenoDroneAI>();
    MovementController = GetComponent<XenoAIMovementController>();
    base.Awake();
  }

  public bool MonsterUpdateAIMethod(float dt)
  {
    if (DroneAI.abilityManager.IsDodging) return true;
    if (!ShouldSkipUpdateAI && !base.UpdateAI(dt))
      return false;

    if (DroneAI.CurrentState == XenoDroneAI.XenoAIState.Idle) return true;
    if (DroneAI.CurrentState == XenoDroneAI.XenoAIState.Dead) return true;
    if (DroneAI.CurrentState == XenoDroneAI.XenoAIState.Hunt) return true;

    if (IsSleeping())
    {
      UpdateSleep(dt);
      return true;
    }
    var character = m_character as Humanoid;
    if (HuntPlayer())
      SetAlerted(true);
    bool canHearTarget;
    bool canSeeTarget;
    UpdateTarget(character, dt, out canHearTarget, out canSeeTarget);
    if ((bool)(Object)m_tamable && (bool)(Object)m_tamable.m_saddle && m_tamable.m_saddle.UpdateRiding(dt))
      return true;

    // TODO Add support for setting target here from XenoDroneAI (based on canHearTarget and canSeeTarget)

    // Early out for skipping AI movement
    if (ShouldSkipAiMovement) return true;

    if (m_avoidLand && !m_character.IsSwimming())
    {
      MoveToWater(dt, 20f);
      return true;
    }
    if (DespawnInDay() && EnvMan.IsDay() && ((Object)m_targetCreature == (Object)null || !canSeeTarget))
    {
      MoveAwayAndDespawn(dt, true);
      return true;
    }
    if (IsEventCreature() && !RandEventSystem.HaveActiveEvent())
    {
      SetHuntPlayer(false);
      if ((Object)m_targetCreature == (Object)null && !IsAlerted())
      {
        MoveAwayAndDespawn(dt, false);
        return true;
      }
    }
    if (m_fleeIfNotAlerted && !HuntPlayer() && (bool)(Object)m_targetCreature && !IsAlerted() && (double)Vector3.Distance(m_targetCreature.transform.position, transform.position) - (double)m_targetCreature.GetRadius() > (double)m_alertRange)
    {
      Flee(dt, m_targetCreature.transform.position);
      return true;
    }
    if ((double)m_fleeIfLowHealth > 0.0 && (double)m_timeSinceHurt < (double)m_fleeTimeSinceHurt && (Object)m_targetCreature != (Object)null && (double)m_character.GetHealthPercentage() < (double)m_fleeIfLowHealth)
    {
      Flee(dt, m_targetCreature.transform.position);
      return true;
    }
    if (m_fleeInLava && m_character.InLava() && ((Object)m_targetCreature == (Object)null || m_targetCreature.AboveOrInLava()))
    {
      Flee(dt, m_character.transform.position - m_character.transform.forward);
      return true;
    }
    if ((m_afraidOfFire || m_avoidFire) && AvoidFire(dt, m_targetCreature, m_afraidOfFire))
    {
      if (m_afraidOfFire)
      {
        m_targetStatic = (StaticTarget)null;
        m_targetCreature = (Character)null;
      }
      return true;
    }
    if (!m_character.IsTamed())
    {
      if ((Object)m_targetCreature != (Object)null)
      {
        if ((bool)(Object)EffectArea.IsPointInsideNoMonsterArea(m_targetCreature.transform.position))
        {
          Flee(dt, m_targetCreature.transform.position);
          return true;
        }
      }
      else
      {
        var noMonsterArea = EffectArea.IsPointCloseToNoMonsterArea(transform.position);
        if ((Object)noMonsterArea != (Object)null)
        {
          Flee(dt, noMonsterArea.transform.position);
          return true;
        }
      }
    }
    if (m_fleeIfHurtWhenTargetCantBeReached && (Object)m_targetCreature != (Object)null && (double)m_timeSinceAttacking > 30.0 && (double)m_timeSinceHurt < 20.0)
    {
      Flee(dt, m_targetCreature.transform.position);
      m_lastKnownTargetPos = transform.position;
      m_updateTargetTimer = 1f;
      return true;
    }
    if ((!IsAlerted() || (Object)m_targetStatic == (Object)null && (Object)m_targetCreature == (Object)null) && UpdateConsumeItem(character, dt))
      return true;
    if ((double)m_circleTargetInterval > 0.0 && (bool)(Object)m_targetCreature)
    {
      m_pauseTimer += dt;
      if ((double)m_pauseTimer > (double)m_circleTargetInterval)
      {
        if ((double)m_pauseTimer > (double)m_circleTargetInterval + (double)m_circleTargetDuration)
          m_pauseTimer = Random.Range(0.0f, m_circleTargetInterval / 10f);
        RandomMovementArroundPoint(dt, m_targetCreature.transform.position, m_circleTargetDistance, IsAlerted());
        return true;
      }
    }
    var itemData = SelectBestAttack(character, dt);
    var flag = itemData != null && (double)Time.time - (double)itemData.m_lastAttackTime > (double)itemData.m_shared.m_aiAttackInterval && (double)m_character.GetTimeSinceLastAttack() >= (double)m_minAttackInterval && !IsTakingOff();
    if (((IsCharging() || !((Object)m_targetStatic != (Object)null) && !((Object)m_targetCreature != (Object)null)
          ? 0
          : itemData != null
            ? 1
            : 0) & (flag ? 1 : 0)) != 0 && !m_character.InAttack() && itemData.m_shared.m_attack != null && !itemData.m_shared.m_attack.IsDone() && !string.IsNullOrEmpty(itemData.m_shared.m_attack.m_chargeAnimationBool))
      ChargeStart(itemData.m_shared.m_attack.m_chargeAnimationBool);
    if ((m_character.IsFlying()
          ? m_circulateWhileChargingFlying ? 1 : 0
          : m_circulateWhileCharging
            ? 1
            : 0) != 0 && ((Object)m_targetStatic != (Object)null || (Object)m_targetCreature != (Object)null) && itemData != null && !flag && !m_character.InAttack())
    {
      var point = (bool)(Object)m_targetCreature ? m_targetCreature.transform.position : m_targetStatic.transform.position;
      RandomMovementArroundPoint(dt, point, m_randomMoveRange, IsAlerted());
      return true;
    }
    if ((Object)m_targetStatic == (Object)null && (Object)m_targetCreature == (Object)null || itemData == null)
    {
      if ((bool)(Object)m_follow)
        Follow(m_follow, dt);
      else
        IdleMovement(dt);
      ChargeStop();
      return true;
    }
    if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
    {
      if ((bool)(Object)m_targetStatic)
      {
        var closestPoint = m_targetStatic.FindClosestPoint(transform.position);
        if ((double)Vector3.Distance(closestPoint, transform.position) < (double)itemData.m_shared.m_aiAttackRange && CanSeeTarget(m_targetStatic))
        {
          LookAt(m_targetStatic.GetCenter());
          if ((double)itemData.m_shared.m_aiAttackMaxAngle == 0.0)
            ZLog.LogError((object)$"AI Attack Max Angle for {itemData.m_shared.m_name} is 0!");
          if (IsLookingAt(m_targetStatic.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle, itemData.m_shared.m_aiInvertAngleCheck) & flag)
            DoAttack((Character)null, false);
          else
            StopMoving();
        }
        else
        {
          MoveTo(dt, closestPoint, 0.0f, IsAlerted());
          ChargeStop();
        }
      }
      else if ((bool)(Object)m_targetCreature)
      {
        if (canHearTarget | canSeeTarget || HuntPlayer() && m_targetCreature.IsPlayer())
        {
          m_beenAtLastPos = false;
          m_lastKnownTargetPos = m_targetCreature.transform.position;
          var num1 = Vector3.Distance(m_lastKnownTargetPos, transform.position) - m_targetCreature.GetRadius();
          var num2 = m_alertRange * m_targetCreature.GetStealthFactor();
          if (canSeeTarget && (double)num1 < (double)num2)
            SetAlerted(true);
          var num3 = (double)num1 < (double)itemData.m_shared.m_aiAttackRange ? 1 : 0;
          if (num3 == 0 || !canSeeTarget || (double)itemData.m_shared.m_aiAttackRangeMin < 0.0 || !IsAlerted())
          {
            var velocity = m_targetCreature.GetVelocity();
            var vector3 = velocity * m_interceptTime;
            var lastKnownTargetPos = m_lastKnownTargetPos;
            if ((double)num1 > (double)vector3.magnitude / 4.0)
              lastKnownTargetPos += velocity * m_interceptTime;
            MoveTo(dt, lastKnownTargetPos, 0.0f, IsAlerted());
            if ((double)m_timeSinceAttacking > 15.0)
              m_unableToAttackTargetTimer = 15f;
          }
          else
            StopMoving();
          if ((num3 & (canSeeTarget ? 1 : 0)) != 0 && IsAlerted())
          {
            if (PheromoneFleeCheck(m_targetCreature))
            {
              Flee(dt, m_targetCreature.transform.position);
              m_updateTargetTimer = Random.Range(m_fleePheromoneMin, m_fleePheromoneMax);
              m_targetCreature = (Character)null;
            }
            else
            {
              LookAt(m_targetCreature.GetTopPoint());
              if (flag && IsLookingAt(m_lastKnownTargetPos, itemData.m_shared.m_aiAttackMaxAngle, itemData.m_shared.m_aiInvertAngleCheck))
                DoAttack(m_targetCreature, false);
            }
          }
        }
        else
        {
          ChargeStop();
          if (m_beenAtLastPos)
          {
            RandomMovement(dt, m_lastKnownTargetPos);
            if ((double)m_timeSinceAttacking > 15.0)
              m_unableToAttackTargetTimer = 15f;
          }
          else if (MoveTo(dt, m_lastKnownTargetPos, 0.0f, IsAlerted()))
            m_beenAtLastPos = true;
        }
      }
    }
    else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt || itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend)
    {
      var target = itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt ? HaveHurtFriendInRange(m_viewRange) : HaveFriendInRange(m_viewRange);
      if ((bool)(Object)target)
      {
        if ((double)Vector3.Distance(target.transform.position, transform.position) < (double)itemData.m_shared.m_aiAttackRange)
        {
          if (flag)
          {
            StopMoving();
            LookAt(target.transform.position);
            DoAttack(target, true);
          }
          else
            RandomMovement(dt, target.transform.position);
        }
        else
          MoveTo(dt, target.transform.position, 0.0f, IsAlerted());
      }
      else
        RandomMovement(dt, transform.position, true);
    }
    return true;
  }

  // todo use XenoAI for most of the delegation but call this monster ai for supplement things.
  public override bool UpdateAI(float dt)
  {
    return MonsterUpdateAIMethod(dt);
    // return base.UpdateAI(dt);
  }
}