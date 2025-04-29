using System;
using UnityEngine;
namespace ValheimVehicles.Storage.Serialization;

[Serializable]
public struct SerializableVector3
{
  public float x;
  public float y;
  public float z;

  public SerializableVector3(float x, float y, float z)
  {
    this.x = x;
    this.y = y;
    this.z = z;
  }

  public SerializableVector3(Vector3 v)
  {
    x = v.x;
    y = v.y;
    z = v.z;
  }

  public readonly Vector3 ToVector3()
  {
    return new Vector3(x, y, z);
  }
}