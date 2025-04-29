using System;
using UnityEngine;
namespace ValheimVehicles.Storage.Serialization;

[Serializable]
public struct SerializableQuaternion
{
  public float x;
  public float y;
  public float z;
  public float w;

  public SerializableQuaternion(float x, float y, float z, float w)
  {
    this.x = x;
    this.y = y;
    this.z = z;
    this.w = w;
  }

  public SerializableQuaternion(Quaternion q)
  {
    x = q.x;
    y = q.y;
    z = q.z;
    w = q.w;
  }

  public readonly Quaternion ToQuaternion()
  {
    return new Quaternion(x, y, z, w);
  }
}