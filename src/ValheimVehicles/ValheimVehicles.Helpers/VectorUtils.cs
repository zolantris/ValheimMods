using UnityEngine;

namespace ValheimVehicles.Helpers;

public static class VectorUtils
{
  public static Vector3 MergeVectors(Vector3 a, Vector3 b)
  {
    return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
  }

  public static Vector3 MultiplyVectors(Vector3 a, Vector3 b)
  {
    return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
  }

  /// <summary>
  /// Clamps the components of a vector between specified minimum and maximum values.
  /// </summary>
  /// <param name="vector">The vector to clamp.</param>
  /// <param name="min">The minimum value for each component.</param>
  /// <param name="max">The maximum value for each component.</param>
  /// <returns>A new vector with clamped components.</returns>
  public static Vector3 ClampVector(Vector3 vector, float min, float max)
  {
    return new Vector3(
      Mathf.Clamp(vector.x, min, max),
      Mathf.Clamp(vector.y, min, max),
      Mathf.Clamp(vector.z, min, max)
    );
  }
}