using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace ValheimVehicles.SharedScripts;

public class MeshBoundsVisualizer : MonoBehaviour
{
  public enum Direction
  {
    Forward,
    Left,
    Right,
    Backward
  }

  public static string[] centerNames =
    { "Forward", "Left", "Right", "Backward" };

  public static Direction[] centerDirections =
  {
    Direction.Forward, Direction.Left, Direction.Right, Direction.Backward
  };

  [Header("Debug Settings")] public bool showDebugCubes = true;

  public float cubeSize = 0.2f;
  public Color cubeColor = Color.yellow;

  [Header("Text Settings")] public bool showDebugText = true;

  public Color textColor = Color.white;

  [Header("Raycast Settings")] public LayerMask layerMask;

  public bool shouldDrawRaycast = false;

  public bool shouldDrawCubes = true;

  private readonly List<GameObject> debugCubeWithLabel = new();

  private List<Vector3> _cachedCenterPoints = new();

  private List<GameObject> meshObjects = new();

  private void Start()
  {
    layerMask = LayerHelpers.CustomRaftLayerMask;
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
  [UsedImplicitly]
  [CanBeNull]
  public List<PointWithDirection> CalculateAndVisualizeBounds(
    MeshCollider meshCollider,
    Transform forwardTransform)
  {
    List<PointWithDirection> pointsWithDirections = new();
    if (meshCollider.sharedMesh == null)
    {
      Debug.LogError("Mesh is null!");
      return pointsWithDirections;
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

    Vector3[] centerPoints = { forward, left, right, back };

    // Calculate and visualize corner points
    var cornerPoints =
      CalculateRaycastPoints(meshCollider, cornerDirections, layerMask);

    if (shouldDrawRaycast) DrawDebugLines(cornerPoints);

    // Calculate and visualize directional center points
    centerPoints =
      CalculateRaycastPoints(meshCollider, centerPoints, layerMask).ToArray();

    if (shouldDrawCubes) DrawDebugCubesAndLabels(centerPoints, centerNames);

    for (var index = 0; index < centerPoints.Length; index++)
      pointsWithDirections.Add(new PointWithDirection
      {
        point = centerPoints[index],
        direction = centerDirections[index]
      });
    return pointsWithDirections;
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
      var direction = Vector3.Scale(dir.normalized, Vector3.one * -1);
      var origin = meshCollider.bounds.center +
                   Vector3.Scale(direction,
                     meshCollider.bounds.extents + Vector3.one);


      if (Physics.Raycast(origin, direction, out var hit, 30, layerMask))
      {
        if (hit.collider.name != meshCollider.name) continue;
        var hitMeshCollider = hit.collider as MeshCollider;
        if (hitMeshCollider != null &&
            hitMeshCollider.sharedMesh == meshCollider.sharedMesh)
          points.Add(hit.point);
      }
      else
      {
        Debug.Log($"Raycast missed. Origin: {origin}, Direction: {direction}");
        points.Add(meshCollider.bounds.center +
                   Vector3.Scale(dir, meshCollider.bounds.extents));
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
  private void DrawDebugCubesAndLabels(Vector3[] centerPoints,
    string[] names)
  {
    // we do not want to run this unless in playmode for inspections as this is pretty perf heavy
    if (Application.isEditor && !Application.isPlaying) return;
    var isEqual = debugCubeWithLabel.Count == centerPoints.Length &&
                  centerPoints.Length == _cachedCenterPoints.Count;

    if (isEqual &&
        DebugUnityHelpers.Vector3ArrayEqualWithTolerance(
          centerPoints, _cachedCenterPoints.ToArray())) return;
    ClearDebugObjects();
    _cachedCenterPoints = centerPoints.ToList();

    for (var i = 0; i < centerPoints.Length; i++)
    {
      var cubeAndLabelObject = new GameObject
      {
        name = "Directional Cube",
        transform =
        {
          position = centerPoints[i]
        }
      };
      cubeAndLabelObject.transform.SetParent(transform);

      // Debug Cube
      if (showDebugCubes)
      {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var collider = cube.GetComponent<BoxCollider>();
        if (collider != null) Destroy(collider);
        cube.layer = LayerHelpers.IgnoreRaycastLayer;
        cube.transform.position = centerPoints[i];
        cube.transform.SetParent(cubeAndLabelObject.transform);
        cube.transform.localScale = Vector3.one * cubeSize;
        cube.GetComponent<Renderer>().material.color = cubeColor;
      }

      // Debug Text
      if (showDebugText)
      {
        var textObj = new GameObject($"Text_{names[i]}")
        {
          transform = { position = centerPoints[i] }
        };
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

  public struct PointWithDirection
  {
    public Vector3 point;

    public Direction direction;
  }
}