using System;
using UnityEngine;
namespace Eldritch.Core;

[Serializable]
public class XenoHuntBehaviorConfig
{
  [Header("Behavior Toggles")]
  public bool enableCircling = true;
  public bool enableCreeping = true;
  public bool enableAttack = true;
  public bool enableLeaping = true;
  public bool enableRetreating = true;
  public bool enableRandomCamouflage = true;

  [Header("Behavior Overrides")]
  public bool FORCE_Circling;
  public bool FORCE_Creeping;

  [Header("Targeting")]
  public float maxTargetDistance = 80f;
  public float minHuntDistance = 5f;
  public float minCreepDistance = 2f; // really close

  [Header("Circle Movement")]
  public float circleMoveSpeed = 7.5f;
  public float creepingMoveSpeed = 5f;
  public float movingSpeedFlux = 2f;
  public float minCircleRadius = 5f;
  public float maxCircleRadius = 15f;
  public float circleRadiusFactor = 0.5f;

  [Header("Leaping Cooldown")]
  public float leapingCooldown = 10f;

  [Header("Weighted Random Substate Probabilities (sum should be ~1.0)")]
  [Range(0f, 1f)] public float probCircling = 0.25f;
  [Range(0f, 1f)] public float probMovingAway = 0.05f;
  [Range(0f, 1f)] public float probPausing = 0.15f;
  [Range(0f, 1f)] public float probCreeping = 0.55f;

  [Header("Weighted Random Ability properties during specific behaviors")]
  [Range(0f, 1f)] public float probCamouflage = 0.0f;
  [Range(0f, 1f)] public float probAttackInRange = 1f;
  [Range(0f, 1f)] public float probLeapRetreat = 0.0f; // after leaping in range. Instead of engaging in combat. Retreat.

  [Header("State Timer Durations (seconds)")]
  public Vector2 circlingTimeRange = new(1, 1); // min/max
  public Vector2 movingAwayTimeRange = new(1.0f, 1.0f);
  public Vector2 pausingTimeRange = new(0.8f, 1.4f);
  public Vector2 creepingTimeRange = new(1.1f, 2.0f);

  // Add more as needed (e.g., attack state durations, forced state logic, etc.)
}