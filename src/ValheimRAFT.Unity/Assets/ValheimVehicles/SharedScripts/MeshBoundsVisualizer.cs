using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
  public class MeshBoundsVisualizer : MonoBehaviour
  {
    [Header("Debug Settings")] public bool showDebugCubes = true;

    public float cubeSize = 0.2f;
    public Color cubeColor = Color.yellow;

    [Header("Text Settings")] public bool showDebugText = true;

    public Color textColor = Color.white;

    [Header("Raycast Settings")] public LayerMask layerMask;

    public bool shouldDrawRaycast;

    private readonly List<GameObject> debugCubeWithLabel = new();

    private List<Vector3> _cachedCenterPoints = new();

    private List<GameObject> meshObjects = new();

    private void Start()
    {
      ClearDebugObjects();
    }

    private void OnDisable()
    {
      ClearDebugObjects();
    }

    private void OnDestroy()
    {
      ClearDebugObjects();
    }

    /// <summary>
    ///   Calculates bounds with both relative corners and directional centers.
    /// </summary>
    public void CalculateAndVisualizeBounds(MeshCollider meshCollider,
      Transform forwardTransform)
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
        new(1, 0, -1), // Bottom Right
        new(-1, 0, 1), // Top Left
        new(1, 0, 1) // Top Right
      };

      // Directions for centers (relative to forwardTransform)
      var forward = forwardTransform.forward;
      var right = forwardTransform.right;
      var back = -forward;
      var left = -right;

      Vector3[] centerDirections = { forward, left, right, back };
      string[] centerNames = { "Forward", "Left", "Right", "Backward" };

      // Calculate and visualize corner points
      var cornerPoints =
        CalculateRaycastPoints(meshCollider, cornerDirections, layerMask);
      DrawDebugLines(cornerPoints);

      // Calculate and visualize directional center points
      var centerPoints =
        CalculateRaycastPoints(meshCollider, centerDirections, layerMask);
      DrawDebugCubesAndLabels(centerPoints, centerNames);
    }

    /// <summary>
    ///   Calculates raycast points for the given directions.
    /// </summary>
    private List<Vector3> CalculateRaycastPoints(MeshCollider meshCollider,
      Vector3[] directions, LayerMask layerMask)
    {
      var points = new List<Vector3>();
      foreach (var dir in directions)
      {
        var origin = meshCollider.bounds.center +
                     Vector3.Scale(dir,
                       meshCollider.bounds.extents + Vector3.one);
        var direction = Vector3.Scale(dir.normalized, Vector3.one * -1);

        if (Physics.Raycast(origin, direction, out var hit, 30, layerMask))
        {
          if (hit.collider.name != meshCollider.name) continue;
          var hitMeshCollider = hit.collider as MeshCollider;
          if (hitMeshCollider != null &&
              hitMeshCollider.sharedMesh == meshCollider.sharedMesh)
            points.Add(hit.point);
        }

        if (shouldDrawRaycast)
          // Debug Ray
          Debug.DrawRay(origin,
            Vector3.Scale(direction, meshCollider.bounds.extents), Color.red,
            1f);
      }

      return points;
    }

    /// <summary>
    ///   Draws debug lines connecting the corner points.
    /// </summary>
    private void DrawDebugLines(List<Vector3> cornerPoints)
    {
      if (cornerPoints == null || cornerPoints.Count != 4) return;

      Debug.DrawLine(cornerPoints[0], cornerPoints[1],
        Color.green); // Bottom Edge
      Debug.DrawLine(cornerPoints[1], cornerPoints[3],
        Color.green); // Right Edge
      Debug.DrawLine(cornerPoints[3], cornerPoints[2], Color.green); // Top Edge
      Debug.DrawLine(cornerPoints[2], cornerPoints[0],
        Color.green); // Left Edge
    }

    /// <summary>
    ///   Draws debug cubes and their directional labels.
    /// </summary>
    private void DrawDebugCubesAndLabels(List<Vector3> centerPoints,
      string[] names)
    {
      // we do not want to run this unless in playmode for inspections as this is pretty perf heavy
      if (Application.isEditor && !Application.isPlaying) return;
      var isEqual = debugCubeWithLabel.Count == centerPoints.Count &&
                    centerPoints.Count == _cachedCenterPoints.Count;

      if (isEqual &&
          DebugUnityHelpers.Vector3ArrayEqualWithTolerance(
            centerPoints.ToArray(), _cachedCenterPoints.ToArray())) return;
      ClearDebugObjects();
      _cachedCenterPoints = centerPoints;

      for (var i = 0; i < centerPoints.Count; i++)
      {
        var cubeAndLabelObject = new GameObject
        {
          name = "Directional Cube",
          transform =
          {
            position = centerPoints[i]
          }
        };

        // Debug Cube
        if (showDebugCubes)
        {
          var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
          cube.transform.SetParent(cubeAndLabelObject.transform);
          cube.transform.localScale = Vector3.one * cubeSize;
          cube.GetComponent<Renderer>().material.color = cubeColor;
        }

        // Debug Text
        if (showDebugText)
        {
          var textObj = new GameObject($"Text_{names[i]}");
          textObj.transform.SetParent(cubeAndLabelObject.transform);
          var textMesh = textObj.AddComponent<TextMesh>();
          textMesh.text = names[i];
          textMesh.characterSize = 0.2f;
          textMesh.color = textColor;
          textObj.transform.localPosition = Vector3.up * 0.5f;
          textObj.transform.LookAt(Camera.main.transform);
          textObj.transform.Rotate(0, 180, 0); // Flip for readability
        }

        debugCubeWithLabel.Add(cubeAndLabelObject);
      }
    }

    /// <summary>
    ///   Clears previously created debug objects (cubes and texts).
    /// </summary>
    private void ClearDebugObjects()
    {
      foreach (var cube in debugCubeWithLabel)
        if (cube != null)
          DebugUnityHelpers.AdaptiveDestroy(cube);
      debugCubeWithLabel.Clear();
    }
  }
}