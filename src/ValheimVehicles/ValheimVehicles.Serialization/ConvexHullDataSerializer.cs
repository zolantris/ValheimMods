using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValheimVehicles.Serialization;

/// <summary>
/// Serializes convex collider geometry into a compact, versioned binary format.
/// Vertices are stored in vehicle-root local space.
/// </summary>
public static class ConvexHullDataSerializer
{
  private const byte FormatVersion = 1;

  public const int MaxColliders = 3;
  public const int MaxTrianglesPerCollider = 255;
  public const int MaxVerticesPerCollider = 1024;

  // An application-level budget, not a Valheim engine limit.
  public const int MaxPayloadBytes = 8 * 1024;

  private const int BytesPerVertex = 12;
  private const int BytesPerTriangle = 6;

  public sealed class ColliderData
  {
    public Vector3[] Vertices { get; }
    public int[] Triangles { get; }
    public bool IsTrigger { get; }

    public ColliderData(
      Vector3[] vertices,
      int[] triangles,
      bool isTrigger)
    {
      Vertices = vertices;
      Triangles = triangles;
      IsTrigger = isTrigger;
    }
  }

  /// <summary>
  /// Serializes source colliders relative to the vehicle root.
  /// The source colliders must be valid convex hull meshes.
  /// </summary>
  public static byte[] Serialize(
    Transform vehicleRoot,
    IReadOnlyList<MeshCollider> colliders)
  {
    if (vehicleRoot == null)
      throw new ArgumentNullException(nameof(vehicleRoot));

    if (colliders == null)
      throw new ArgumentNullException(nameof(colliders));

    if (colliders.Count == 0 || colliders.Count > MaxColliders)
    {
      throw new InvalidDataException(
        $"Collider count must be between 1 and {MaxColliders}.");
    }

    using (var stream = new MemoryStream())
    using (var writer = new BinaryWriter(stream))
    {
      writer.Write(FormatVersion);
      writer.Write((byte)colliders.Count);

      foreach (var collider in colliders)
      {
        WriteCollider(writer, vehicleRoot, collider);

        if (stream.Length > MaxPayloadBytes)
        {
          throw new InvalidDataException(
            $"Convex hull payload exceeds {MaxPayloadBytes} bytes.");
        }
      }

      writer.Flush();

      if (stream.Length > MaxPayloadBytes)
      {
        throw new InvalidDataException(
          $"Convex hull payload exceeds {MaxPayloadBytes} bytes.");
      }

      return stream.ToArray();
    }
  }

  private static void WriteCollider(
    BinaryWriter writer,
    Transform vehicleRoot,
    MeshCollider collider)
  {
    if (collider == null)
      throw new InvalidDataException("Collider reference is null.");

    var mesh = collider.sharedMesh;

    if (mesh == null)
      throw new InvalidDataException("Collider has no shared mesh.");

    if (!collider.convex)
      throw new InvalidDataException(
        "Only convex MeshColliders are supported.");

    var sourceVertices = mesh.vertices;
    var triangles = mesh.triangles;

    if (sourceVertices.Length == 0 ||
        sourceVertices.Length > MaxVerticesPerCollider)
    {
      throw new InvalidDataException(
        $"Invalid vertex count: {sourceVertices.Length}.");
    }

    if (triangles.Length == 0 || triangles.Length % 3 != 0)
    {
      throw new InvalidDataException(
        "Mesh triangle index count is invalid.");
    }

    var triangleCount = triangles.Length / 3;

    if (triangleCount > MaxTrianglesPerCollider)
    {
      throw new InvalidDataException(
        $"Convex mesh has {triangleCount} triangles; " +
        $"maximum is {MaxTrianglesPerCollider}.");
    }

    // Two-byte indices are sufficient for our bounded vertex count.
    if (sourceVertices.Length > ushort.MaxValue)
    {
      throw new InvalidDataException("Too many vertices for 16-bit indices.");
    }

    // Validate indices before writing anything for this collider.
    for (var i = 0; i < triangles.Length; i++)
    {
      if (triangles[i] < 0 ||
          triangles[i] >= sourceVertices.Length ||
          triangles[i] > ushort.MaxValue)
      {
        throw new InvalidDataException(
          $"Triangle index {triangles[i]} is out of range.");
      }
    }

    writer.Write(collider.isTrigger);
    writer.Write((ushort)sourceVertices.Length);
    writer.Write((ushort)triangleCount);

    // Bake all transforms into vehicle-root local coordinates.
    foreach (var vertex in sourceVertices)
    {
      var worldVertex = collider.transform.TransformPoint(vertex);
      var localVertex = vehicleRoot.InverseTransformPoint(worldVertex);

      if (!IsFinite(localVertex))
      {
        throw new InvalidDataException(
          "Mesh contains a non-finite vertex.");
      }

      writer.Write(localVertex.x);
      writer.Write(localVertex.y);
      writer.Write(localVertex.z);
    }

    foreach (var index in triangles)
    {
      writer.Write((ushort)index);
    }
  }

  /// <summary>
  /// Parses and validates the entire payload before returning geometry.
  /// </summary>
  public static List<ColliderData> Deserialize(byte[] payload)
  {
    if (payload == null)
      throw new ArgumentNullException(nameof(payload));

    if (payload.Length < 2 || payload.Length > MaxPayloadBytes)
    {
      throw new InvalidDataException(
        $"Invalid convex hull payload size: {payload.Length}.");
    }

    var result = new List<ColliderData>();

    using var stream = new MemoryStream(payload, false);
    using var reader = new BinaryReader(stream);
    var version = reader.ReadByte();

    if (version != FormatVersion)
    {
      throw new InvalidDataException(
        $"Unsupported convex hull format version: {version}.");
    }

    int colliderCount = reader.ReadByte();

    if (colliderCount == 0 || colliderCount > MaxColliders)
    {
      throw new InvalidDataException(
        $"Invalid collider count: {colliderCount}.");
    }

    for (var i = 0; i < colliderCount; i++)
    {
      var isTrigger = reader.ReadBoolean();
      int vertexCount = reader.ReadUInt16();
      int triangleCount = reader.ReadUInt16();

      if (vertexCount == 0 ||
          vertexCount > MaxVerticesPerCollider)
      {
        throw new InvalidDataException(
          $"Invalid vertex count: {vertexCount}.");
      }

      if (triangleCount == 0 ||
          triangleCount > MaxTrianglesPerCollider)
      {
        throw new InvalidDataException(
          $"Invalid triangle count: {triangleCount}.");
      }

      var requiredBytes =
        (long)vertexCount * BytesPerVertex +
        (long)triangleCount * BytesPerTriangle;

      if (requiredBytes > stream.Length - stream.Position)
      {
        throw new InvalidDataException(
          "Convex hull payload ended unexpectedly.");
      }

      var vertices = new Vector3[vertexCount];

      for (var v = 0; v < vertexCount; v++)
      {
        var vertex = new Vector3(
          reader.ReadSingle(),
          reader.ReadSingle(),
          reader.ReadSingle());

        if (!IsFinite(vertex))
        {
          throw new InvalidDataException(
            "Payload contains a non-finite vertex.");
        }

        vertices[v] = vertex;
      }

      var triangles = new int[triangleCount * 3];

      for (var t = 0; t < triangles.Length; t++)
      {
        int index = reader.ReadUInt16();

        if (index >= vertexCount)
        {
          throw new InvalidDataException(
            $"Triangle index {index} exceeds vertex count.");
        }

        triangles[t] = index;
      }

      result.Add(new ColliderData(
        vertices,
        triangles,
        isTrigger));
    }

    if (stream.Position != stream.Length)
    {
      throw new InvalidDataException(
        "Unexpected trailing bytes in convex hull payload.");
    }

    return result;
  }

  /// <summary>
  /// Reconstructs a Unity mesh from validated geometry.
  /// Caller owns the returned mesh and must destroy it when no longer used.
  /// </summary>
  public static Mesh CreateMesh(ColliderData data)
  {
    if (data == null)
      throw new ArgumentNullException(nameof(data));

    var mesh = new Mesh
    {
      name = "ValheimRAFT_ConvexHull"
    };

    try
    {
      mesh.vertices = data.Vertices;
      mesh.triangles = data.Triangles;
      mesh.RecalculateBounds();

      return mesh;
    }
    catch
    {
      UnityEngine.Object.Destroy(mesh);
      throw;
    }
  }

  private static bool IsFinite(Vector3 value)
  {
    return IsFinite(value.x) &&
           IsFinite(value.y) &&
           IsFinite(value.z);
  }

  private static bool IsFinite(float value)
  {
    return !float.IsNaN(value) && !float.IsInfinity(value);
  }
}