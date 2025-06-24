using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Components;
namespace ValheimVehicles.Storage.Serialization;

/// <summary>
/// ZDO helpers only.
/// </summary>
public static class StoredSailDataExtensions
{
  public static StoredSailData GetSerializableData(ZDO zdo, SailComponent sail)
  {
    var data = new StoredSailData
    {
      SailCorners = new List<SerializableVector3>(),
      LockedSides = zdo.GetInt(SailComponent.m_lockedSailSidesHash),
      LockedCorners = zdo.GetInt(SailComponent.m_lockedSailCornersHash),
      MaterialVariant = zdo.GetInt(SailComponent.m_sailMaterialVariantHash),
      MainHash = zdo.GetInt(SailComponent.m_mainHashRefHash),
      MainScale = new SerializableVector2(zdo.GetVec3(SailComponent.m_mainScaleHash, Vector3.zero)),
      MainOffset = new SerializableVector2(zdo.GetVec3(SailComponent.m_mainOffsetHash, Vector3.zero)),
      MainColor = new SerializableColor(sail.GetColorFromByteStream(zdo.GetByteArray(SailComponent.m_mainColorHash))),

      PatternScale = new SerializableVector2(zdo.GetVec3(SailComponent.m_patternScaleHash, Vector3.zero)),
      PatternOffset = new SerializableVector2(zdo.GetVec3(SailComponent.m_patternOffsetHash, Vector3.zero)),
      PatternColor = new SerializableColor(sail.GetColorFromByteStream(zdo.GetByteArray(SailComponent.m_patternColorHash))),
      PatternHash = zdo.GetInt(SailComponent.m_patternZDOHash),
      PatternRotation = zdo.GetFloat(SailComponent.m_patternRotationHash),

      LogoHash = zdo.GetInt(SailComponent.m_logoZdoHash),
      LogoScale = new SerializableVector2(zdo.GetVec3(SailComponent.m_logoScaleHash, Vector3.zero)),
      LogoOffset = new SerializableVector2(zdo.GetVec3(SailComponent.m_logoOffsetHash, Vector3.zero)),
      LogoColor = new SerializableColor(sail.GetColorFromByteStream(zdo.GetByteArray(SailComponent.m_logoColorHash))),
      LogoRotation = zdo.GetFloat(SailComponent.m_logoRotationHash),

      SailFlags = zdo.GetInt(SailComponent.m_sailFlagsHash)
    };

    var count = zdo.GetInt(SailComponent.m_sailCornersCountHash);
    for (var i = 0; i < count && i < 4; i++)
    {
      var corner = zdo.GetVec3(i switch
      {
        0 => SailComponent.m_sailCorner1Hash,
        1 => SailComponent.m_sailCorner2Hash,
        2 => SailComponent.m_sailCorner3Hash,
        3 => SailComponent.m_sailCorner4Hash,
        _ => 0
      }, Vector3.zero);
      data.SailCorners.Add(new SerializableVector3(corner));
    }

    return data;
  }

  public static void ApplySerializableData(this StoredSailData data, ZDO zdo, SailComponent component)
  {
    zdo.Set(SailComponent.m_sailCornersCountHash, data.SailCorners.Count);
    for (var i = 0; i < data.SailCorners.Count && i < 4; i++)
    {
      var corner = data.SailCorners[i].ToVector3();
      zdo.Set(i switch
      {
        0 => SailComponent.m_sailCorner1Hash,
        1 => SailComponent.m_sailCorner2Hash,
        2 => SailComponent.m_sailCorner3Hash,
        3 => SailComponent.m_sailCorner4Hash,
        _ => 0
      }, corner);
    }

    zdo.Set(SailComponent.m_lockedSailSidesHash, data.LockedSides);
    zdo.Set(SailComponent.m_lockedSailCornersHash, data.LockedCorners);

    // for full overrides of materials using vanilla sails.
    zdo.Set(SailComponent.m_sailMaterialVariantHash, data.MaterialVariant);

    zdo.Set(SailComponent.m_mainHashRefHash, data.MainHash);
    zdo.Set(SailComponent.m_mainScaleHash, data.MainScale.ToVector2());
    zdo.Set(SailComponent.m_mainOffsetHash, data.MainOffset.ToVector2());
    zdo.Set(SailComponent.m_mainColorHash, component.ConvertColorToByteStream(data.MainColor.ToColor()));

    zdo.Set(SailComponent.m_patternScaleHash, data.PatternScale.ToVector2());
    zdo.Set(SailComponent.m_patternOffsetHash, data.PatternOffset.ToVector2());
    zdo.Set(SailComponent.m_patternColorHash, component.ConvertColorToByteStream(data.PatternColor.ToColor()));
    zdo.Set(SailComponent.m_patternZDOHash, data.PatternHash);
    zdo.Set(SailComponent.m_patternRotationHash, data.PatternRotation);

    zdo.Set(SailComponent.m_logoZdoHash, data.LogoHash);
    zdo.Set(SailComponent.m_logoScaleHash, data.LogoScale.ToVector2());
    zdo.Set(SailComponent.m_logoOffsetHash, data.LogoOffset.ToVector2());
    zdo.Set(SailComponent.m_logoColorHash, component.ConvertColorToByteStream(data.LogoColor.ToColor()));
    zdo.Set(SailComponent.m_logoRotationHash, data.LogoRotation);

    zdo.Set(SailComponent.m_sailFlagsHash, data.SailFlags);
    zdo.Set(SailComponent.HasInitializedHash, true);
  }

}