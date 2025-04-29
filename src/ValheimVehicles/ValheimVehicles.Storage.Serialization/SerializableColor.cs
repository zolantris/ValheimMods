using System;
using UnityEngine;
namespace ValheimVehicles.Storage.Serialization;

[Serializable]
public struct SerializableColor
{
  public float r;
  public float g;
  public float b;
  public float a;

  public SerializableColor(float r, float g, float b, float a)
  {
    this.r = r;
    this.g = g;
    this.b = b;
    this.a = a;
  }

  public SerializableColor(Color color)
  {
    r = color.r;
    g = color.g;
    b = color.b;
    a = color.a;
  }

  public readonly Color ToColor()
  {
    return new Color(r, g, b, a);
  }
}