using System.IO;
using UnityEngine;

namespace ValheimVehicles.Helpers;

public static class BinaryIOUtil
{
	public static void Write(this BinaryWriter writer, Vector3 vector)
	{
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}

	public static Vector3 ReadVector3(this BinaryReader reader)
	{
		return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	public static void Write(this BinaryWriter writer, Color color)
	{
		writer.Write((byte)(color.r * 255f));
		writer.Write((byte)(color.g * 255f));
		writer.Write((byte)(color.b * 255f));
		writer.Write((byte)(color.a * 255f));
	}

	public static Color ReadColor(this BinaryReader reader)
	{
		return new Color((float)(int)reader.ReadByte() / 255f, (float)(int)reader.ReadByte() / 255f, (float)(int)reader.ReadByte() / 255f, (float)(int)reader.ReadByte() / 255f);
	}

	public static void Write(this BinaryWriter writer, Vector2 vector)
	{
		writer.Write(vector.x);
		writer.Write(vector.y);
	}

	public static Vector2 ReadVector2(this BinaryReader reader)
	{
		return new Vector2(reader.ReadSingle(), reader.ReadSingle());
	}
}
