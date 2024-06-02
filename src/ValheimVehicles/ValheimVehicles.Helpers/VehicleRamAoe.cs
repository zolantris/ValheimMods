using System;
using System.Security.Policy;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Helpers;

public class VehicleRamAoe : Aoe
{
  public HitData.DamageTypes baseDamage;
  public float velocityMultiplier = 0;
  public float massMultiplier = 3;
  public float velocityThreshold = 0.5f;
  public VehicleShip? vehicle;
  public static bool MultiplicativeCollisionVelocity = true;
  public const float MaxVelocityMultiplier = 20f;
  public bool isReadyForCollisions = false;

  public void UpdateReadyForCollisions()
  {
    CancelInvoke(nameof(UpdateReadyForCollisions));
    if (!m_nview)
    {
      isReadyForCollisions = false;
      Invoke(nameof(UpdateReadyForCollisions), 1);
      return;
    }

    var isVehicleChild = m_nview.GetZDO().GetInt(VehicleZdoVars.MBParentIdHash);
    if (isVehicleChild == 0)
    {
      isReadyForCollisions = true;
      return;
    }

    // Must be within the BaseVehicleController otherwise this AOE could attempt to damage items within the raft ball
    var isChildOfBaseVehicle = transform.root.GetComponent<BaseVehicleController>();
    if (!(bool)isChildOfBaseVehicle)
    {
      isReadyForCollisions = false;
      return;
    }

    isReadyForCollisions = true;
  }

  public void Start()
  {
    Invoke(nameof(UpdateReadyForCollisions), 1f);
  }

  public override void OnEnable()
  {
    Invoke(nameof(UpdateReadyForCollisions), 1f);
    base.OnEnable();
  }

  public void UpdateDamageFromVelocityCollider(Collider collider)
  {
    if (!collider) return;
    // reset damage to base damage if one of these is not available, will still recalculate later
    if (!vehicle?.m_body || !collider.attachedRigidbody)
    {
      m_damage = baseDamage;
    }

    // early exit if both are not valid
    if (!vehicle?.m_body && !collider.attachedRigidbody) return;

    // Velocity will significantly increase if the object is moving towards the other object IE collision
    float relativeVelocity;
    if (!vehicle?.m_body)
    {
      relativeVelocity = collider.attachedRigidbody.velocity.magnitude;
    }
    else
    {
      relativeVelocity =
        Vector3.Magnitude(collider?.attachedRigidbody?.velocity ??
                          Vector3.zero - vehicle?.m_body?.velocity ??
                          Vector3.zero);
    }

    UpdateDamageFromVelocity(relativeVelocity);
  }

  public void UpdateDamageFromVelocity(float relativeVelocityMagnitude)
  {
    var multiplier = Mathf.Min(relativeVelocityMagnitude * 0.5f, MaxVelocityMultiplier);

    if (Mathf.Approximately(multiplier, 0))
    {
      multiplier = 0;
    }

    m_damage = new HitData.DamageTypes()
    {
      m_damage = baseDamage.m_damage * multiplier,
      m_blunt = baseDamage.m_blunt * multiplier,
      m_slash = baseDamage.m_slash * multiplier,
      m_pierce = baseDamage.m_pierce * multiplier,
      m_chop = baseDamage.m_chop * multiplier,
      m_pickaxe = baseDamage.m_pickaxe * multiplier,
      m_fire = baseDamage.m_fire * multiplier,
      m_frost = baseDamage.m_frost * multiplier,
      m_lightning = baseDamage.m_lightning * multiplier,
      m_poison = baseDamage.m_poison * multiplier,
      m_spirit = baseDamage.m_spirit * multiplier,
    };
  }

  private void ShouldIgnore(Collider collider)
  {
    if (!collider) return;
    if (collider.transform.root.name.StartsWith(PrefabNames.WaterVehicleShip) &&
        collider.transform.root == transform.root)
    {
      var childColliders = GetComponentsInChildren<Collider>();
      foreach (var childCollider in childColliders)
      {
        Physics.IgnoreCollision(childCollider, collider, true);
      }
    }
  }

  public new void OnCollisionEnter(Collision collision)
  {
    if (!isReadyForCollisions) return;
    ShouldIgnore(collision.collider);
    UpdateDamageFromVelocity(Vector3.Magnitude(collision.relativeVelocity));
    base.OnCollisionEnter(collision);
  }

  public new void OnCollisionStay(Collision collision)
  {
    if (!isReadyForCollisions) return;
    UpdateDamageFromVelocity(Vector3.Magnitude(collision.relativeVelocity));
    base.OnCollisionStay(collision);
  }

  public new void OnTriggerEnter(Collider collider)
  {
    if (!isReadyForCollisions) return;
    ShouldIgnore(collider);
    UpdateDamageFromVelocityCollider(collider);
    base.OnTriggerEnter(collider);
  }

  public new void OnTriggerStay(Collider collider)
  {
    if (!isReadyForCollisions) return;
    UpdateDamageFromVelocityCollider(collider);
    base.OnTriggerStay(collider);
  }

  public void SetBaseDamage(HitData.DamageTypes hitData)
  {
    baseDamage = hitData;
  }
}