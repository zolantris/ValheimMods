// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.Util.BinaryIOUtil
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D


using System.IO;
using UnityEngine;

namespace ValheimRAFT.Util
{
  public static class BinaryIOUtil
  {
    public static void Write(this BinaryWriter writer, Vector3 vector)
    {
      writer.Write(vector.x);
      writer.Write(vector.y);
      writer.Write(vector.z);
    }

    public static Vector3 ReadVector3(this BinaryReader reader) => new Vector3(reader.ReadSingle(),
      reader.ReadSingle(), reader.ReadSingle());

    public static void Write(this BinaryWriter writer, Color color)
    {
      writer.Write((byte)((double)color.r * (double)byte.MaxValue));
      writer.Write((byte)((double)color.g * (double)byte.MaxValue));
      writer.Write((byte)((double)color.b * (double)byte.MaxValue));
      writer.Write((byte)((double)color.a * (double)byte.MaxValue));
    }

    public static Color ReadColor(this BinaryReader reader) => new Color(
      (float)reader.ReadByte() / (float)byte.MaxValue,
      (float)reader.ReadByte() / (float)byte.MaxValue,
      (float)reader.ReadByte() / (float)byte.MaxValue,
      (float)reader.ReadByte() / (float)byte.MaxValue);

    public static void Write(this BinaryWriter writer, Vector2 vector)
    {
      writer.Write(vector.x);
      writer.Write(vector.y);
    }

    public static Vector2 ReadVector2(this BinaryReader reader) =>
      new Vector2(reader.ReadSingle(), reader.ReadSingle());
  }
}