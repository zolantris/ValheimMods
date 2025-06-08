// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using ValheimVehicles.SharedScripts;

#endregion

namespace ValheimVehicles.Storage.Serialization;

// ReSharper disable once PartialTypeWithSinglePart
[Serializable]
public class StoredSwivelCustomConfig
{
  public SerializableVector3 MaxEuler
  {
    get;
    set;
  }

  public SerializableVector3 MovementOffset
  {
    get;
    set;
  }

  public SwivelMode Mode
  {
    get;
    set;
  } = SwivelMode.Move;

  public float InterpolationSpeed
  {
    get;
    set;
  } = 10f;
  public HingeAxis HingeAxes
  {
    get;
    set;
  }
}