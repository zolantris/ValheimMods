using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
public class MeshBoundsVisualizer : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebugCubes = true;
    public float cubeSize = 0.2f;
    public Color cubeColor = Color.yellow;

    [Header("Text Settings")]
    public bool showDebugText = true;
    public Color textColor = Color.white;

    [Header("Raycast Settings")]
    public LayerMask layerMask;

    private List<GameObject> debugCubes = new List<GameObject>();
    private List<GameObject> debugTexts = new List<GameObject>();

    /// <summary>
    /// Calculates bounds with both relative corners and directional centers.
    /// </summary>
    public void CalculateAndVisualizeBounds(MeshCollider meshCollider, Transform forwardTransform)
    {
        if (meshCollider.sharedMesh == null)
        {
            Debug.LogError("Mesh is null!");
            return;
        }

        // Directions for corners (for bounds visualization)
        Vector3[] cornerDirections =
        {
            new(-1, 0, -1), // Bottom Left
            new(1, 0, -1),  // Bottom Right
            new(-1, 0, 1),  // Top Left
            new(1, 0, 1)    // Top Right
        };

        // Directions for centers (relative to forwardTransform)
        Vector3 forward = forwardTransform.forward;
        Vector3 right = forwardTransform.right;
        Vector3 back = -forward;
        Vector3 left = -right;

        Vector3[] centerDirections = { forward, left, right, back };
        string[] centerNames = { "Forward", "Left", "Right", "Backward" };

        // Calculate and visualize corner points
        List<Vector3> cornerPoints = CalculateRaycastPoints(meshCollider, cornerDirections, layerMask);
        DrawDebugLines(cornerPoints);

        // Calculate and visualize directional center points
        List<Vector3> centerPoints = CalculateRaycastPoints(meshCollider, centerDirections, layerMask);
        DrawDebugCubesAndLabels(centerPoints, centerNames);
    }

    /// <summary>
    /// Calculates raycast points for the given directions.
    /// </summary>
    private List<Vector3> CalculateRaycastPoints(MeshCollider meshCollider, Vector3[] directions, LayerMask layerMask)
    {
        List<Vector3> points = new List<Vector3>();
        foreach (var dir in directions)
        {
            Vector3 origin = meshCollider.bounds.center + Vector3.Scale(dir, meshCollider.bounds.extents + Vector3.one);
            Vector3 direction = Vector3.Scale(dir.normalized, Vector3.one * -1);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 30, layerMask))
            {
                if (hit.collider.name != meshCollider.name) continue;
                MeshCollider hitMeshCollider = hit.collider as MeshCollider;
                if (hitMeshCollider != null && hitMeshCollider.sharedMesh == meshCollider.sharedMesh)
                {
                    points.Add(hit.point);
                }
            }

            // Debug Ray
            Debug.DrawRay(origin, direction * 30f, Color.red, 1f);
        }
        return points;
    }

    /// <summary>
    /// Draws debug lines connecting the corner points.
    /// </summary>
    private void DrawDebugLines(List<Vector3> cornerPoints)
    {
        if (cornerPoints == null || cornerPoints.Count != 4) return;

        Debug.DrawLine(cornerPoints[0], cornerPoints[1], Color.green); // Bottom Edge
        Debug.DrawLine(cornerPoints[1], cornerPoints[3], Color.green); // Right Edge
        Debug.DrawLine(cornerPoints[3], cornerPoints[2], Color.green); // Top Edge
        Debug.DrawLine(cornerPoints[2], cornerPoints[0], Color.green); // Left Edge
    }

    /// <summary>
    /// Draws debug cubes and their directional labels.
    /// </summary>
    private void DrawDebugCubesAndLabels(List<Vector3> centerPoints, string[] names)
    {
        ClearDebugObjects();

        for (int i = 0; i < centerPoints.Count; i++)
        {
            // Debug Cube
            if (showDebugCubes)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = centerPoints[i];
                cube.transform.localScale = Vector3.one * cubeSize;
                cube.GetComponent<Renderer>().material.color = cubeColor;
                debugCubes.Add(cube);
            }

            // Debug Text
            if (showDebugText)
            {
                GameObject textObj = new GameObject($"Text_{names[i]}");
                TextMesh textMesh = textObj.AddComponent<TextMesh>();
                textMesh.text = names[i];
                textMesh.characterSize = 0.2f;
                textMesh.color = textColor;
                textObj.transform.position = centerPoints[i] + Vector3.up * 0.5f;
                textObj.transform.LookAt(Camera.main.transform);
                textObj.transform.Rotate(0, 180, 0); // Flip for readability
                debugTexts.Add(textObj);
            }
        }
    }

    /// <summary>
    /// Clears previously created debug objects (cubes and texts).
    /// </summary>
    private void ClearDebugObjects()
    {
        foreach (var cube in debugCubes)
        {
            if (cube != null) Destroy(cube);
        }
        debugCubes.Clear();

        foreach (var text in debugTexts)
        {
            if (text != null) Destroy(text);
        }
        debugTexts.Clear();
    }
}
}