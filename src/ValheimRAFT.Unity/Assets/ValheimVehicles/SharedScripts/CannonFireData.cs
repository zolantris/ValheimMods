// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.Structs
{
  // required for integration lib.
  // ReSharper disable once PartialTypeWithSinglePart
  public partial struct CannonFireData : IEquatable<CannonFireData>
  {
    public float randomVelocityValue;
    public float randomArcValue;
    public List<Vector3> cannonShootingPositions;
    public int allocatedAmmo;
    public CannonballVariant ammoVariant;
    public bool canApplyDamage;
    public Vector3 shootingDirection;
    public Quaternion cannonLocalRotation;

    public static Dictionary<CannonFireData, CannonController> CannonControllerMap = new();

    // ReSharper disable once UseNullableReferenceTypesAnnotationSyntax
    [CanBeNull]
    public GameObject GetCannonControllerObj()
    {
#if VALHEIM
      var obj = ZNetScene.instance.FindInstance(cannonControllerZDOID);
      return obj;
#else
      if (!CannonControllerMap.TryGetValue(this, out var cannonController)) return null;
      return cannonController.gameObject;
#endif
    }

    public static List<CannonFireData> CreateListOfCannonFireDataFromTargetController(TargetController targetController, List<CannonController> firingGroup)
    {
#if VALHEIM
      if (!targetController.m_nview) return new List<CannonFireData>();
#endif

      var cannonFireDataList = new List<CannonFireData>();

      var ammoSolid = targetController.ammoController.SolidAmmo;
      var ammoExplosive = targetController.ammoController.ExplosiveAmmo;
#if VALHEIM
      var canApplyDamage = targetController.m_nview.IsOwner();
#else
      var canApplyDamage = true;
#endif
      foreach (var cannonController in firingGroup)
      {
        if (cannonController == null) continue;
        var barrelCount = cannonController.GetBarrelCount();
        var ammoToUse = AmmoController.SubtractAmmoByVariant(cannonController.AmmoVariant, barrelCount, ref ammoSolid, ref ammoExplosive);

#if VALHEIM
        if (canApplyDamage)
        {
          cannonController.m_nview.ClaimOwnership();
        }
#endif

        var data = CreateCannonFireData(cannonController, ammoToUse);
        if (data.HasValue)
        {
          cannonFireDataList.Add(data.Value);
        }
      }

      return cannonFireDataList;
    }

#if VALHEIM
    public static CannonFireData? CreateCannonFireDataFromHandHeld(CannonHandHeldController cannonHandHeld)
    {
      var ammoSolid = cannonHandHeld.ammoController.SolidAmmo;
      var ammoExplosive = cannonHandHeld.ammoController.ExplosiveAmmo;
      var ammoToUse = AmmoController.SubtractAmmoByVariant(cannonHandHeld.AmmoVariant, cannonHandHeld.GetBarrelCount(), ref ammoSolid, ref ammoExplosive);

      var fireData = CreateCannonFireData(cannonHandHeld, ammoToUse);
      return fireData;
    }
#endif

    public static CannonFireData? CreateCannonFireData(CannonController cannonController, int remainingAmmo)
    {
      if (remainingAmmo == 0) return null;
      if (cannonController == null) return null;
#if VALHEIM
      if (cannonController.m_nview == null || !cannonController.m_nview.IsValid()) return null;
#endif

      var cannonShootingPositions = cannonController.shootingBarrelParts.Where(x => x != null).Select(x => x.projectileLoader.position).ToList();

      if (cannonShootingPositions.Count == 0) return null;

      while (remainingAmmo < cannonShootingPositions.Count && cannonShootingPositions.Count > 0)
      {
        cannonShootingPositions.RemoveAt(remainingAmmo - 1);
      }
      if (cannonShootingPositions.Count == 0) return null;

#if VALHEIM
      var zdoid = cannonController.m_nview.GetZDO().m_uid;
      var canApplyDamage = cannonController.m_nview.IsOwner();
#else
      var canApplyDamage = true;
#endif

      var data = new CannonFireData
      {
#if VALHEIM
        cannonControllerZDOID = zdoid,
#endif
        randomVelocityValue = CannonController.GetRandomCannonVelocity,
        randomArcValue = CannonController.GetRandomCannonArc,
        ammoVariant = cannonController.AmmoVariant,
        allocatedAmmo = remainingAmmo,
        canApplyDamage = canApplyDamage,
        shootingDirection = cannonController.cannonShooterAimPoint.forward,
        cannonShootingPositions = cannonShootingPositions,
        cannonLocalRotation = cannonController.cannonRotationalTransform.localRotation
      };

#if UNITY_EDITOR
      CannonControllerMap[data] = cannonController;
#endif

      return data;
    }

    public bool Equals(CannonFireData other)
    {
      return randomVelocityValue.Equals(other.randomVelocityValue) && randomArcValue.Equals(other.randomArcValue) && Equals(cannonShootingPositions, other.cannonShootingPositions) && allocatedAmmo == other.allocatedAmmo && ammoVariant == other.ammoVariant && canApplyDamage == other.canApplyDamage && shootingDirection.Equals(other.shootingDirection) && cannonLocalRotation.Equals(other.cannonLocalRotation);
    }

#if UNITY_EDITOR
    public override bool Equals(object obj)
    {
      return obj is CannonFireData other && Equals(other);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(randomVelocityValue, randomArcValue, cannonShootingPositions, allocatedAmmo, (int)ammoVariant, canApplyDamage, shootingDirection, cannonLocalRotation);
    }
#endif
  }
}