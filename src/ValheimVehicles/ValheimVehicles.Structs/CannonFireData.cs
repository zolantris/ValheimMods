using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Structs;

public struct CannonFireData
{
  public ZDOID cannonControllerZDOID;
  public float randomVelocityValue;
  public float randomArcValue;
  public List<Vector3> cannonShootingPositions;
  public int allocatedAmmo;
  public CannonballVariant ammoVariant;
  public bool canApplyDamage;
  public Vector3 shootingDirection;
  public Quaternion cannonLocalRotation;

  public static List<CannonFireData> CreateListOfCannonFireDataFromTargetController(TargetController targetController, List<CannonController> firingGroup)
  {
    if (!targetController.m_nview) return new List<CannonFireData>();

    var cannonFireDataList = new List<CannonFireData>();

    var ammoSolid = targetController.ammoController.SolidAmmo;
    var ammoExplosive = targetController.ammoController.ExplosiveAmmo;
    var canApplyDamage = targetController.m_nview.IsOwner();

    foreach (var cannonController in firingGroup)
    {
      if (cannonController == null) continue;
      var barrelCount = cannonController.GetBarrelCount();
      var ammoToUse = AmmoController.SubtractAmmoByVariant(cannonController.AmmoVariant, barrelCount, ref ammoSolid, ref ammoExplosive);

      if (canApplyDamage)
      {
        cannonController.m_nview.ClaimOwnership();
      }
      var data = CreateCannonFireData(cannonController, ammoToUse);
      if (data.HasValue)
      {
        cannonFireDataList.Add(data.Value);
      }
    }

    return cannonFireDataList;
  }

  public static CannonFireData? CreateCannonFireDataFromHandHeld(CannonHandHeldController cannonHandHeld)
  {
    var ammoSolid = cannonHandHeld.ammoController.SolidAmmo;
    var ammoExplosive = cannonHandHeld.ammoController.ExplosiveAmmo;
    var ammoToUse = AmmoController.SubtractAmmoByVariant(cannonHandHeld.AmmoVariant, cannonHandHeld.GetBarrelCount(), ref ammoSolid, ref ammoExplosive);

    var fireData = CreateCannonFireData(cannonHandHeld, ammoToUse);
    return fireData;
  }

  public static CannonFireData? CreateCannonFireData(CannonController cannonController, int remainingAmmo)
  {
    if (remainingAmmo == 0) return null;
    if (cannonController == null || cannonController.m_nview == null || !cannonController.m_nview.IsValid()) return null;

    var cannonShootingPositions = cannonController.shootingBarrelParts.Where(x => x != null).Select(x => x.projectileLoader.position).ToList();

    if (cannonShootingPositions.Count == 0) return null;

    while (remainingAmmo < cannonShootingPositions.Count && cannonShootingPositions.Count > 0)
    {
      cannonShootingPositions.RemoveAt(remainingAmmo - 1);
    }
    if (cannonShootingPositions.Count == 0) return null;

    var zdoid = cannonController.m_nview.GetZDO().m_uid;
    var canApplyDamage = cannonController.m_nview.IsOwner();

    var data = new CannonFireData
    {
      cannonControllerZDOID = zdoid,
      randomVelocityValue = CannonController.GetRandomCannonVelocity,
      randomArcValue = CannonController.GetRandomCannonArc,
      ammoVariant = cannonController.AmmoVariant,
      allocatedAmmo = remainingAmmo,
      canApplyDamage = canApplyDamage,
      shootingDirection = cannonController.cannonShooterAimPoint.forward,
      cannonShootingPositions = cannonShootingPositions,
      cannonLocalRotation = cannonController.cannonRotationalTransform.localRotation
    };

    return data;
  }

  public static ZPackage WriteListToPackage(ZPackage pkg, List<CannonFireData> dataList)
  {
    // Write count
    pkg.Write(dataList.Count);

    // Write each CannonFireData
    foreach (var data in dataList)
    {
      WriteIntoPackage(pkg, data);
    }

    return pkg;
  }

  public static List<CannonFireData> ReadListFromPackage(ZPackage pkg)
  {
    // Read and validate TargetControllerZDOID
    var count = pkg.ReadInt();
    var firingDataList = new List<CannonFireData>(count);

    for (var i = 0; i < count; i++)
    {
      var data = ReadFromPackage(pkg);
      firingDataList.Add(data);
    }

    return firingDataList;
  }

  // --- Write ---
  public static ZPackage WriteToPackage(CannonFireData data)
  {
    var pkg = new ZPackage();
    WriteIntoPackage(pkg, data);
    return pkg;
  }

  public static void WriteIntoPackage(ZPackage pkg, CannonFireData data)
  {
    pkg.Write(data.cannonControllerZDOID);
    pkg.Write(data.randomVelocityValue);
    pkg.Write(data.randomArcValue);
    pkg.Write((int)data.ammoVariant);
    pkg.Write(data.allocatedAmmo);
    pkg.Write(data.canApplyDamage);
    pkg.Write(data.shootingDirection);
    pkg.Write(data.cannonLocalRotation);

    // Write barrel positions
    pkg.Write(data.cannonShootingPositions.Count);
    foreach (var barrelPosition in data.cannonShootingPositions)
    {
      pkg.Write(barrelPosition);
    }
  }


  // --- Read ---
  public static CannonFireData ReadFromPackage(ZPackage pkg)
  {
    var data = new CannonFireData
    {
      cannonControllerZDOID = pkg.ReadZDOID(),
      randomVelocityValue = pkg.ReadSingle(),
      randomArcValue = pkg.ReadSingle(),
      ammoVariant = (CannonballVariant)pkg.ReadInt(),
      allocatedAmmo = pkg.ReadInt(),
      canApplyDamage = pkg.ReadBool(),
      shootingDirection = pkg.ReadVector3(),
      cannonLocalRotation = pkg.ReadQuaternion()
    };

    // Read barrel positions
    var barrelCount = pkg.ReadInt();
    data.cannonShootingPositions = new List<Vector3>(barrelCount);
    for (var i = 0; i < barrelCount; ++i)
    {
      data.cannonShootingPositions.Add(pkg.ReadVector3());
    }

    return data;
  }
}