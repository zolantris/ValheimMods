// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  public struct ConvexHullResultData
  {
    public int VertexStartIndex;
    public int VertexCount;

    public int TriangleStartIndex;
    public int TriangleCount;

    public int NormalStartIndex;
    public int NormalCount;

    public int ValidClusterCount;

    public ConvexHullResultData(int vertexStart, int vertexCount, int triStart, int triCount, int normalStart, int normalCount, int validClusters)
    {
      VertexStartIndex = vertexStart;
      VertexCount = vertexCount;

      TriangleStartIndex = triStart;
      TriangleCount = triCount;

      NormalStartIndex = normalStart;
      NormalCount = normalCount;

      ValidClusterCount = validClusters;
    }
  }
}