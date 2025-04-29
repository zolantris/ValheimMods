using System;
using System.Collections.Generic;

namespace ValheimVehicles.Storage.Serialization;

/// <summary>
/// Must be serializable data only. No helpers no Game references or Unity APIS
/// </summary>
[Serializable]
public class StoredSailData
{
  public List<SerializableVector3> SailCorners = new();

  public int LockedSides;
  public int LockedCorners;

  public int MainHash;
  public SerializableVector2 MainScale;
  public SerializableVector2 MainOffset;
  public SerializableColor MainColor;

  public int PatternHash;
  public SerializableVector2 PatternScale;
  public SerializableVector2 PatternOffset;
  public SerializableColor PatternColor;
  public float PatternRotation;

  public int LogoHash;
  public SerializableVector2 LogoScale;
  public SerializableVector2 LogoOffset;
  public SerializableColor LogoColor;
  public float LogoRotation;

  public int SailFlags;
}