using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Patches;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using Random = System.Random;
namespace ValheimVehicles.Controllers;

public class NearestTargetListener : MonoBehaviour, IAnimatorHandler
{
  [SerializeField] private Character selfCharacter;
  [SerializeField] private float updateInterval = 0.2f;

  public Character? NearestHostile { get; private set; }
  public Transform? NearestHostileCenterOfMass { get; private set; }
  private float _lastUpdateTime;

  private LightningBolt m_boltRight;
  private LightningBolt m_boltLeft;

  private bool _lightningActive;
  private Transform rightHandObj;
  private Transform leftHandObj;
  public float lightningRange = 15f;
  public bool IsToggled = true;
  public static bool CanToggle = true;
  public float lerpedBoltSizeMin = 0.02f;
  public float lerpedBoltSizeMax = 0.6f;
  public float lerpedBoltSize = 0.02f;

  public bool HasHandOffset = false;
  public static float damageInterval = 0.1f;
  public float lastUpdate = 0f;
  public float m_lightningDamage = 20f;
  public Random random = new();

  // todo make this higher.
  public float eitrCost = 0f;
  private void Awake()
  {
    selfCharacter = GetComponent<Character>();
  }

  private void Start()
  {
    if (Player.m_localPlayer == null || selfCharacter != Player.m_localPlayer)
    {
      Destroy(this);
      return;
    }
    CreateAndBindLightningBoltToPlayer();
  }


  public Vector3 GetHandOffset()
  {
    var offset = Player.m_localPlayer.transform.up * 0.1f;
    return offset;
  }


  public void RemoveCharacterFromAnimators()
  {
    CharacterAnimEvent_Patch.m_animatedHumanoids.Remove(selfCharacter.m_animator);
  }

  public void AddCharacterToAnimators()
  {
    if (!CharacterAnimEvent_Patch.m_animatedHumanoids.ContainsKey(selfCharacter.m_animator))
    {
      CharacterAnimEvent_Patch.m_animatedHumanoids.Add(selfCharacter.m_animator, this);
    }
  }

  public void ShowLightning()
  {
    if (m_boltRight.enabled && m_boltLeft.enabled) return;
    m_boltLeft.ShowLightning();
    m_boltRight.ShowLightning();
  }
  public void HideLightning()
  {
    if (!m_boltRight.enabled && !m_boltLeft.enabled) return;
    m_boltLeft.HideLightning();
    m_boltRight.HideLightning();
  }

  public void ToggleLightning()
  {
    if (IsToggled)
    {
      ShowLightning();
    }
    else
    {
      HideLightning();
    }
  }

  private void Update()
  {
    var alt = ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() ? ZInput.GetButton("JoyAltKeys") : ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
    if (Time.time - _lastUpdateTime < updateInterval) return;
    if (m_boltRight == null || m_boltLeft == null) return;
    if (CanToggle && IsToggled && alt != true)
    {
      ToggleLightning();
      return;
    }

    IsToggled = alt && !IsToggled;
    ToggleLightning();
  }

  public void ApplyLightningDamage()
  {
    if (NearestHostile == null)
    {
      lastUpdate = 0f;
      return;
    }
    lastUpdate += Time.fixedDeltaTime;
    if (lastUpdate < damageInterval)
    {
      return;
    }
    lastUpdate = 0f;
    if (Player.m_localPlayer.GetEitr() < eitrCost)
    {
      return;
    }

    var hit = new HitData
    {
      m_point = NearestHostileCenterOfMass != null ? NearestHostileCenterOfMass.position : NearestHostile.transform.position,
      m_attacker = Player.m_localPlayer.m_nview.GetZDO().m_uid,
      m_staggerMultiplier = 50,
      m_toolTier = 999,
      m_ranged = true,
      m_blockable = false,
      m_dodgeable = false,
      m_dir = Player.m_localPlayer.transform.forward,
      m_hitType = HitData.HitType.PlayerHit,
      m_damage = new HitData.DamageTypes
      {
        m_lightning = m_lightningDamage * random.Next(1, 10) / 5f
      }
    };
    NearestHostile.AddLightningDamage(m_lightningDamage);
    NearestHostile.Damage(hit);
    Player.m_localPlayer.UseEitr(eitrCost);
  }

  public float fixedUpdateBoltSizeMultiplier = 2f;

  private void UpdateBoltSize(bool alt)
  {
    if (!alt)
    {
      lerpedBoltSize = lerpedBoltSizeMin;
      return;
    }
    lerpedBoltSize = Mathf.Clamp(lerpedBoltSize + fixedUpdateBoltSizeMultiplier * Time.fixedDeltaTime, lerpedBoltSizeMin, lerpedBoltSizeMax);
    m_boltRight.UpdateLightningSize(lerpedBoltSize * random.Next(1, 20) / 10);
    m_boltLeft.UpdateLightningSize(lerpedBoltSize);
  }

  private void CreateAndBindLightningBoltToPlayer()
  {
    leftHandObj = selfCharacter.m_animator.GetBoneTransform(HumanBodyBones.LeftHand);
    rightHandObj = selfCharacter.m_animator.GetBoneTransform(HumanBodyBones.RightHand);

    SetupLightningBolt(out var boltLeft, leftHandObj);
    SetupLightningBolt(out var boltRight, rightHandObj);

    m_boltRight = boltRight;
    m_boltLeft = boltLeft;
  }
  private void SetupLightningBolt(out LightningBolt lightningBolt, Transform parentTransform)
  {
    var lightningBoltParent = new GameObject("LightningBoltParent")
    {
      transform =
      {
        parent = parentTransform
      }
    };

    lightningBoltParent.transform.position = leftHandObj.position;

    var lineRenderer = lightningBoltParent.GetComponent<LineRenderer>() ? lightningBoltParent.GetComponent<LineRenderer>() : lightningBoltParent.gameObject.AddComponent<LineRenderer>();
    lineRenderer.material = LoadValheimVehicleAssets.LightningMaterial;

    lightningBolt = lightningBoltParent.gameObject.AddComponent<LightningBolt>();
    lightningBolt.Duration = 0.1f;
    lightningBolt.ChaosFactor = 0.3f;
    lightningBolt.m_rows = 16;
    lightningBolt.Columns = 2;
    lightningBolt.AnimationMode = LightningBoltAnimationMode.PingPong;

    lineRenderer.startWidth = 0.02f;
    lineRenderer.endWidth = 0.01f;

    lightningBolt.StartObject = lightningBoltParent;

  }

  public static bool hasEndPosition = false;
  private void UpdateLightningTarget()
  {
    if (NearestHostile == null || !m_boltRight || !m_boltLeft) return;
    var position = NearestHostile.transform.position;

    if (hasEndPosition)
    {
      m_boltRight.EndPosition = position;
      m_boltLeft.EndPosition = position;
    }
    else
    {
      if (NearestHostileCenterOfMass != null)
      {
        var o = NearestHostileCenterOfMass.gameObject;
        m_boltRight.EndObject = o;
        m_boltLeft.EndObject = o;
      }
      else
      {
        var o = NearestHostile.gameObject;
        m_boltRight.EndObject = o;
        m_boltLeft.EndObject = o;
      }
    }
  }


  private bool IsInRange()
  {
    return NearestHostile != null && Vector3.Distance(selfCharacter.transform.position, NearestHostile.transform.position) > lightningRange;
  }

  private void FixedUpdate()
  {
    if (ZNet.instance == null || ZNetScene.instance == null || Game.instance == null) return;

    var canDoHeavyUpdate = Time.time - _lastUpdateTime > updateInterval;
    if (canDoHeavyUpdate)
    {
      _lastUpdateTime = Time.time;
      UpdateNearestHostile();
    }

    if (NearestHostile == null || !IsInRange())
    {
      m_boltLeft.EndObject = null;
      m_boltRight.EndObject = null;
      HideLightning();
      RemoveCharacterFromAnimators();
      return;
    }

    AddCharacterToAnimators();
    UpdateBoltSize(IsToggled);
    ShowLightning();

    ApplyLightningDamage();
  }

  private void UpdateNearestHostile()
  {
    if (NearestTargetScanManager.Instance == null) return;
    var candidates = NearestTargetScanManager.Instance.CachedCharacters;
    if (selfCharacter == null)
    {
      NearestHostile = null;
      return;
    }

    Character closest = null;

    // todo this likely needs to be much smaller.
    var minDistSq = float.MaxValue;
    // var minDistSq = 400f;
    var selfPos = selfCharacter.transform.position;

    foreach (var other in candidates)
    {
      if (other == null || other == selfCharacter) continue;
      if (!other.m_baseAI || !other.m_baseAI.IsEnemy(selfCharacter)) continue;

      var distSq = (other.transform.position - selfPos).sqrMagnitude;
      if (distSq < minDistSq)
      {
        minDistSq = distSq;
        closest = other;
      }
    }

    // clear damage intervals if the hostile is new
    if (NearestHostile != closest)
    {
      lastUpdate = 50;
    }

    NearestHostile = closest;
    NearestHostileCenterOfMass = null;
    if (NearestHostile != null)
    {
      NearestHostileCenterOfMass = GetCenterChestBone(NearestHostile);
      UpdateLightningTarget();
    }
  }

  public static Transform? GetCenterChestBone(Character character)
  {
    var animator = character.m_animator;
    if (animator == null) return null;

    // Try humanoid bone
    if (animator.isHuman)
    {
      var chest = animator.GetBoneTransform(HumanBodyBones.Chest)
                  ?? animator.GetBoneTransform(HumanBodyBones.Spine);

      if (chest != null) return chest;
    }

    // Fallback to name-based search
    return FindCenterBoneFallback(animator);
  }

  private static Transform? FindCenterBoneFallback(Animator animator)
  {
    var candidates = animator.GetComponentsInChildren<Transform>();

    // Common bone name patterns for chests/spines across Valheim creatures
    string[] chestKeywords = { "chest", "spine", "torso", "body" };

    return candidates.FirstOrDefault(t =>
      chestKeywords.Any(k => t.name.ToLowerInvariant().Contains(k)));
  }

  public void UpdateIK(Animator animator)
  {
    if (NearestHostile == null) return;
    if (animator == null) return;
    if (!IsInRange())
    {
      selfCharacter.m_animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
      selfCharacter.m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
      return;
    }
    var position = NearestHostile.transform.position;
    animator.SetIKPosition(AvatarIKGoal.LeftHand, position);
    animator.SetIKPosition(AvatarIKGoal.RightHand, position);

    selfCharacter.m_animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
    selfCharacter.m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
  }
}