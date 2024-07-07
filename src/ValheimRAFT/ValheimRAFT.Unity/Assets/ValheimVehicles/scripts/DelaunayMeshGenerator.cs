using UnityEngine;
using System.Collections.Generic;

public class DelaunayMeshGenerator : MonoBehaviour
{
    public int numberOfVertices = 10000;
    public Vector3 bounds = new Vector3(200, 50, 200);
    public Material meshMaterial;

    void Start()
    {
        // Generate random vertices
        List<Vector3> vertices = GenerateRandomVertices(numberOfVertices, bounds);

        // Perform Delaunay triangulation
        List<Triangle> triangles = PerformDelaunayTriangulation(vertices);

        // Create Unity mesh object
        Mesh delaunayMesh = GenerateUnityMesh(vertices, triangles);
        GameObject meshObject = new GameObject("DelaunayMesh", typeof(MeshFilter), typeof(MeshRenderer));
        meshObject.GetComponent<MeshFilter>().mesh = delaunayMesh;
        meshObject.GetComponent<MeshRenderer>().material = meshMaterial;
    }

    List<Vector3> GenerateRandomVertices(int count, Vector3 bounds)
    {
        List<Vector3> vertices = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(0, bounds.x);
            float y = Random.Range(0, bounds.y);
            float z = Random.Range(0, bounds.z);
            vertices.Add(new Vector3(x, y, z));
        }
        return vertices;
    }

    List<Triangle> PerformDelaunayTriangulation(List<Vector3> vertices)
    {
        // Create a super triangle
        float maxX = bounds.x * 10;
        float maxY = bounds.y * 10;
        float maxZ = bounds.z * 10;

        Vector3 v1 = new Vector3(-maxX, -maxY, -maxZ);
        Vector3 v2 = new Vector3(maxX, -maxY, -maxZ);
        Vector3 v3 = new Vector3(0, maxY, maxZ);

        List<Triangle> triangles = new List<Triangle> { new Triangle(v1, v2, v3) };

        // Add each vertex to the triangulation
        foreach (var vertex in vertices)
        {
            List<Triangle> badTriangles = new List<Triangle>();
            List<Edge> polygon = new List<Edge>();

            // Find all triangles that are no longer valid
            foreach (var triangle in triangles)
            {
                if (IsPointInCircumcircle(vertex, triangle))
                {
                    badTriangles.Add(triangle);
                    polygon.Add(new Edge(triangle.v1, triangle.v2));
                    polygon.Add(new Edge(triangle.v2, triangle.v3));
                    polygon.Add(new Edge(triangle.v3, triangle.v1));
                }
            }

            // Remove the bad triangles
            triangles.RemoveAll(t => badTriangles.Contains(t));

            // Remove duplicate edges
            polygon = RemoveDuplicateEdges(polygon);

            // Re-triangulate the polygonal hole
            foreach (var edge in polygon)
            {
                triangles.Add(new Triangle(edge.v1, edge.v2, vertex));
            }
        }

        // Remove triangles that share a vertex with the super triangle
        triangles.RemoveAll(t => t.ContainsVertex(v1) || t.ContainsVertex(v2) || t.ContainsVertex(v3));

        return triangles;
    }

    List<Edge> RemoveDuplicateEdges(List<Edge> edges)
    {
        List<Edge> uniqueEdges = new List<Edge>();
        foreach (var edge in edges)
        {
            if (!uniqueEdges.Contains(edge))
            {
                uniqueEdges.Add(edge);
            }
            else
            {
                uniqueEdges.Remove(edge);
            }
        }
        return uniqueEdges;
    }

    bool IsPointInCircumcircle(Vector3 point, Triangle triangle)
    {
        float ax = triangle.v1.x - point.x;
        float ay = triangle.v1.y - point.y;
        float az = triangle.v1.z - point.z;
        float bx = triangle.v2.x - point.x;
        float by = triangle.v2.y - point.y;
        float bz = triangle.v2.z - point.z;
        float cx = triangle.v3.x - point.x;
        float cy = triangle.v3.y - point.y;
        float cz = triangle.v3.z - point.z;

        float det = (ax * ax + ay * ay + az * az) * (bx * cy - by * cx)
                  - (bx * bx + by * by + bz * bz) * (ax * cy - ay * cx)
                  + (cx * cx + cy * cy + cz * cz) * (ax * by - ay * bx);

        return det > 0;
    }

    Mesh GenerateUnityMesh(List<Vector3> vertices, List<Triangle> triangles)
    {
        Mesh mesh = new Mesh();
        List<Vector3> meshVertices = new List<Vector3>();
        List<int> meshTriangles = new List<int>();

        Dictionary<Vector3, int> vertexIndexMap = new Dictionary<Vector3, int>();
        int index = 0;

        foreach (var vertex in vertices)
        {
            if (!vertexIndexMap.ContainsKey(vertex))
            {
                vertexIndexMap[vertex] = index;
                meshVertices.Add(vertex);
                index++;
            }
        }

        foreach (var triangle in triangles)
        {
            meshTriangles.Add(vertexIndexMap[triangle.v1]);
            meshTriangles.Add(vertexIndexMap[triangle.v2]);
            meshTriangles.Add(vertexIndexMap[triangle.v3]);
        }

        mesh.SetVertices(meshVertices);
        mesh.SetTriangles(meshTriangles, 0);
        mesh.RecalculateNormals();

        return mesh;
    }

    struct Triangle
    {
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 v3;

        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }

        public bool ContainsVertex(Vector3 vertex)
        {
            return v1 == vertex || v2 == vertex || v3 == vertex;
        }
    }

    struct Edge
    {
        public Vector3 v1;
        public Vector3 v2;

        public Edge(Vector3 v1, Vector3 v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge))
                return false;

            Edge other = (Edge)obj;
            return (v1 == other.v1 && v2 == other.v2) || (v1 == other.v2 && v2 == other.v1);
        }

        public override int GetHashCode()
        {
            return v1.GetHashCode() ^ v2.GetHashCode();
        }
    }
}
