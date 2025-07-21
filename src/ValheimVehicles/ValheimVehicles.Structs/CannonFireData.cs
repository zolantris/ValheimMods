using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ValheimVehicles.SharedScripts.Structs;

public partial struct CannonFireData
{
  public ZDOID cannonControllerZDOID;

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