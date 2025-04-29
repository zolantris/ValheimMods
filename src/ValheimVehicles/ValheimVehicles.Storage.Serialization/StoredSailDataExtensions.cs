using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Components;
namespace ValheimVehicles.Storage.Serialization;

/// <summary>
/// ZDO helpers only.
/// </summary>
public static class StoredSailDataExtensions
{
  public static StoredSailData LoadFromZDO(ZDO zdo, SailComponent sail)
  {
    var data = new StoredSailData
    {
      SailCorners = new List<SerializableVector3>(),
      LockedSides = zdo.GetInt(sail.m_lockedSailSidesHash),
      LockedCorners = zdo.GetInt(sail.m_lockedSailCornersHash),
      MainHash = zdo.GetInt(sail.m_mainHashRefHash),
      MainScale = new SerializableVector2(zdo.GetVec3(sail.m_mainScaleHash, Vector3.zero)),
      MainOffset = new SerializableVector2(zdo.GetVec3(sail.m_mainOffsetHash, Vector3.zero)),
      MainColor = new SerializableColor(sail.GetColorFromByteStream(zdo.GetByteArray(sail.m_mainColorHash))),

      PatternScale = new SerializableVector2(zdo.GetVec3(sail.m_patternScaleHash, Vector3.zero)),
      PatternOffset = new SerializableVector2(zdo.GetVec3(sail.m_patternOffsetHash, Vector3.zero)),
      PatternColor = new SerializableColor(sail.GetColorFromByteStream(zdo.GetByteArray(sail.m_patternColorHash))),
      PatternHash = zdo.GetInt(sail.m_patternZDOHash),
      PatternRotation = zdo.GetFloat(sail.m_patternRotationHash),

      LogoHash = zdo.GetInt(sail.m_logoZdoHash),
      LogoScale = new SerializableVector2(zdo.GetVec3(sail.m_logoScaleHash, Vector3.zero)),
      LogoOffset = new SerializableVector2(zdo.GetVec3(sail.m_logoOffsetHash, Vector3.zero)),
      LogoColor = new SerializableColor(sail.GetColorFromByteStream(zdo.GetByteArray(sail.m_logoColorHash))),
      LogoRotation = zdo.GetFloat(sail.m_logoRotationHash),

      SailFlags = zdo.GetInt(sail.m_sailFlagsHash)
    };

    var count = zdo.GetInt(sail.m_sailCornersCountHash);
    for (var i = 0; i < count && i < 4; i++)
    {
      var corner = zdo.GetVec3(i switch
      {
        0 => sail.m_sailCorner1Hash,
        1 => sail.m_sailCorner2Hash,
        2 => sail.m_sailCorner3Hash,
        3 => sail.m_sailCorner4Hash,
        _ => 0
      }, Vector3.zero);
      data.SailCorners.Add(new SerializableVector3(corner));
    }

    return data;
  }

  public static void ApplyToZDO(this StoredSailData data, ZDO zdo, SailComponent sail)
  {
    zdo.Set(sail.m_sailCornersCountHash, data.SailCorners.Count);
    for (var i = 0; i < data.SailCorners.Count && i < 4; i++)
    {
      var corner = data.SailCorners[i].ToVector3();
      zdo.Set(i switch
      {
        0 => sail.m_sailCorner1Hash,
        1 => sail.m_sailCorner2Hash,
        2 => sail.m_sailCorner3Hash,
        3 => sail.m_sailCorner4Hash,
        _ => 0
      }, corner);
    }

    zdo.Set(sail.m_lockedSailSidesHash, data.LockedSides);
    zdo.Set(sail.m_lockedSailCornersHash, data.LockedCorners);
    zdo.Set(sail.m_mainHashRefHash, data.MainHash);
    zdo.Set(sail.m_mainScaleHash, data.MainScale.ToVector2());
    zdo.Set(sail.m_mainOffsetHash, data.MainOffset.ToVector2());
    zdo.Set(sail.m_mainColorHash, sail.ConvertColorToByteStream(data.MainColor.ToColor()));

    zdo.Set(sail.m_patternScaleHash, data.PatternScale.ToVector2());
    zdo.Set(sail.m_patternOffsetHash, data.PatternOffset.ToVector2());
    zdo.Set(sail.m_patternColorHash, sail.ConvertColorToByteStream(data.PatternColor.ToColor()));
    zdo.Set(sail.m_patternZDOHash, data.PatternHash);
    zdo.Set(sail.m_patternRotationHash, data.PatternRotation);

    zdo.Set(sail.m_logoZdoHash, data.LogoHash);
    zdo.Set(sail.m_logoScaleHash, data.LogoScale.ToVector2());
    zdo.Set(sail.m_logoOffsetHash, data.LogoOffset.ToVector2());
    zdo.Set(sail.m_logoColorHash, sail.ConvertColorToByteStream(data.LogoColor.ToColor()));
    zdo.Set(sail.m_logoRotationHash, data.LogoRotation);

    zdo.Set(sail.m_sailFlagsHash, data.SailFlags);
    zdo.Set(sail.HasInitialized, true);
  }

}