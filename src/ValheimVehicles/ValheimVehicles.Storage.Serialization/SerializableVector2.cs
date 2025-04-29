using System;
using UnityEngine;
namespace ValheimVehicles.Storage.Serialization;

[Serializable]
public struct SerializableVector2
{
  public float x;
  public float y;

  public SerializableVector2(float x, float y)
  {
    this.x = x;
    this.y = y;
  }

  public SerializableVector2(Vector2 v)
  {
    x = v.x;
    y = v.y;
  }

  public readonly Vector2 ToVector2()
  {
    return new Vector2(x, y);
  }
}