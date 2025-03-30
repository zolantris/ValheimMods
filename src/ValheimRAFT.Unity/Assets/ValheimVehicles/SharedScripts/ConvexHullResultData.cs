#region

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public struct ConvexHullResultData
  {
    public int VertexStart;
    public int VertexCount;
    public int TriangleStart;
    public int TriangleCount;
    public int NormalStart;
    public int NormalCount;

    public ConvexHullResultData(int vertexStart, int vertexCount, int triangleStart, int triangleCount, int normalStart, int normalCount)
    {
      VertexStart = vertexStart;
      VertexCount = vertexCount;
      TriangleStart = triangleStart;
      TriangleCount = triangleCount;
      NormalStart = normalStart;
      NormalCount = normalCount;
    }
  }
}