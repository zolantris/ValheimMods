using System;
using UnityEngine;
namespace Eldritch.Core
{
  /// <summary>
  ///   Todo integrate this into roam logic.
  /// </summary>
  [Serializable]
  public class XenoRoamBehaviorConfig
  {
    [Header("Behavior Toggles")]
    public bool enableRoaming = true;
    public bool enableDigging = true;
    public bool enableTreeClimbing = true;
    public bool enableSleeping = true;

    [Header("Behavior Overrides")]
    public bool FORCE_Roaming;
    public bool FORCE_Sleeping;

    [Header("Sleeping")]
    public float maxInitialSleepTime = 100f;
    public float sleepSpawnHeight = -1.5f;
    public Quaternion sleepSpawnRotation = Quaternion.identity;
    public float sleepAwakeDetectionRadius = 25f; // wakes up a grumpy alien

    [Header("Tree Climbing/Lurking")]
    public float maxTreeHeight = 50f;
    public float targetClimbingHeightPercentage = 0.75f;
    public Vector2 treeClimbingHeightRange = new(-0.25f, 0.25f); // must be range within targetClimbingHeightPercentage as center. 

    [Header("Weighted Random Substate Probabilities (sum should be ~1.0)")]
    [Range(0f, 1f)] public float probSleep = 0.05f; // the probability during roam behavior to go to sleep
    [Range(0f, 1f)] public float probPause = 0.25f;
    [Range(0f, 1f)] public float probClimbing = 0.25f;
    [Range(0f, 1f)] public float probWander = 0.35f;

    [Header("State Timer Durations (seconds)")]
    public Vector2 pausingDuration = new(3, 5f);
    public Vector2 pausingAfterClimbingDuration = new(15, 30f);
    public Vector2 sleepTimeRange = new(2.2f, 3.4f); // min/max
    public Vector2 treeLurkTimeRange = new(1.0f, 1.8f);
  }
}