using System;
using UnityEngine;
namespace Eldritch.Core
{
  [Serializable]
  public class XenoHuntBehaviorConfig
  {
    [Header("Behavior Toggles")]
    public bool enableCircling = true;
    public bool FORCE_Circling;
    public bool enableCreeping = true;
    public bool enableAttack = true;
    public bool enableLeaping = true;
    public bool enableRetreating = true;
    public bool enableRandomCamouflage = true;

    [Header("Targeting")]
    public float maxTargetDistance = 50f;
    public float minHuntDistance = 10f;

    [Header("Circle Movement")]
    public float circleMoveSpeed = 7.5f;
    public float creepingMoveSpeed = 5f;
    public float movingSpeedFlux = 2f;
    public float circleRadius = 15f;

    [Header("Leaping Cooldown")]
    public float leapingCooldown = 10f;

    [Header("Weighted Random Substate Probabilities (sum should be ~1.0)")]
    [Range(0f, 1f)] public float probCircling = 0.55f;
    [Range(0f, 1f)] public float probMovingAway = 0.10f;
    [Range(0f, 1f)] public float probPausingToTurn = 0.22f;
    [Range(0f, 1f)] public float probCreeping = 0.13f;

    [Header("Weighted Random Ability properties during specific behaviors")]
    [Range(0f, 1f)] public float probCamouflage = 0.15f;
    [Range(0f, 1f)] public float probLeapRetreat = 0.5f; // after leaping in range. Instead of engaging in combat. Retreat.

    [Header("State Timer Durations (seconds)")]
    public Vector2 circlingTimeRange = new(2.2f, 3.4f); // min/max
    public Vector2 movingAwayTimeRange = new(1.0f, 1.8f);
    public Vector2 pausingToTurnTimeRange = new(0.8f, 1.4f);
    public Vector2 creepingTimeRange = new(1.1f, 2.0f);

    // Add more as needed (e.g., attack state durations, forced state logic, etc.)
  }
}