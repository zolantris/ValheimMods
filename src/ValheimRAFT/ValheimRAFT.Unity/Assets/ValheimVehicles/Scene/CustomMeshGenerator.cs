
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ValheimRAFT.Unity.Assets.ValheimVehicles.Scene
{
    
public class ConvexMeshGenerator : MonoBehaviour
{
    public List<Transform> points; // Assign points in the inspector
    public MeshFilter meshFilter;

    void Awake()
    {
        GenerateConvexMesh();
    }

    public void GenerateConvexMesh()
    {
        Vector3[] outerPoints = GetOuterPoints(points);
        if (outerPoints.Length < 3)
        {
            Debug.LogWarning("Not enough outer points to form a mesh. Returning null.");
            return;
        }

        Mesh mesh = CreateConvexHullMesh(outerPoints);
        if (mesh != null && ValidateMesh(mesh))
        {
            meshFilter.mesh = mesh;
        }
        else
        {
            Debug.LogWarning("Mesh generation failed or is invalid.");
        }
    }

    Vector3[] GetOuterPoints(List<Transform> inputPoints)
    {
        // Use a HashSet to filter out duplicates
        HashSet<Vector3> uniquePoints = new HashSet<Vector3>();

        foreach (var point in inputPoints)
        {
            uniquePoints.Add(point.position);
        }

        // Convert to array for processing
        Vector3[] pointsArray = new Vector3[uniquePoints.Count];
        uniquePoints.CopyTo(pointsArray);

        // Add logic to remove inner points and outliers as needed
        return RemoveOutliers(pointsArray);
    }

    Vector3[] RemoveOutliers(Vector3[] points)
    {
        // Implement logic to remove outlier points and return a filtered list
        // This can be done using clustering algorithms, distance thresholds, etc.

        // Placeholder: return all points for now
        return points;
    }

    Mesh CreateConvexHullMesh(Vector3[] outerPoints)
    {
        // Use a library or implement a convex hull algorithm here
        // Placeholder: Just return null for now

        return null;
    }

    bool ValidateMesh(Mesh mesh)
    {
        // Check for nested vertices or other invalid conditions
        // Placeholder: simple validation returning true for now
        return true;
    }
}

// [CustomEditor(typeof(ConvexMeshGenerator))]
// public class ConvexMeshGeneratorEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         DrawDefaultInspector();
//
//         if (GUILayout.Button("Generate Convex Mesh"))
//         {
//             ((ConvexMeshGenerator)target).GenerateConvexMesh();
//         }
//     }
// }
}
