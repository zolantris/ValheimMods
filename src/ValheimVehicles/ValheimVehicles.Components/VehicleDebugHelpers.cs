using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Components;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Components;

public struct DrawTargetColliders
{
  public DrawTargetColliders()
  {
    collider = null;
    lineColor = default;
    parent = null;
    width = 1f;
    name = "";
  }

  public BoxCollider collider { get; set; }
  public Color lineColor { get; set; }
  public Transform parent { get; set; }
  public float width { get; set; }

  // used for inspecting
  public string name { get; set; }
}

public class VehicleDebugHelpers : MonoBehaviour
{
  private Dictionary<string, List<LineRenderer>> lines = new();
  private Dictionary<string, GameObject> colliderTextObjects = new();

  public bool autoUpdateColliders = false;
  private List<DrawTargetColliders> targetColliders = [];
  public GameObject VehicleObj;
  public VehicleManager vehicleManagerInstance;
  private Coroutine? _drawColliderCoroutine = null;
  private GameObject? worldCenterOfMassCube;
  private GameObject? vehicleMovementAutomaticCenterOfMassCube;
  private GameObject? vehiclePiecesCenterOfMassCube;
  private GameObject? vehiclePiecesCenterCube;
  private GameObject? vehiclePieceCenterPoint;
  private GameObject? vehicleMovementCenterCube;
  private GameObject? forwardCube;
  private GameObject? backwardCube;
  private GameObject? rightCube;
  private GameObject? leftCube;

  public static Color OrangeColor = new(1f, 0.647f, 0);

  // todo check if there is a conflicting textmesh and push the current one upwards. Recurse until finding a unique point of a specific increment in height.
  private Dictionary<GameObject, Vector3> localPositions = new();

  private void RenderDebugCubes()
  {
    if (vehicleManagerInstance == null ||
        vehicleManagerInstance.MovementController == null || vehicleManagerInstance.PiecesController == null) return;

    var shipFloatation = vehicleManagerInstance
      .MovementController.GetShipFloatation();

    // physics should be orange
    if (shipFloatation != null)
    {
      RenderDebugCube(ref forwardCube, shipFloatation.Value.ShipForward,
        "water_forward", OrangeColor, Vector3.zero);
      RenderDebugCube(ref backwardCube, shipFloatation.Value.ShipBack,
        "water_backward", OrangeColor, Vector3.zero);
      RenderDebugCube(ref rightCube, shipFloatation.Value.ShipRight,
        "water_right", OrangeColor, Vector3.zero);
      RenderDebugCube(ref leftCube, shipFloatation.Value.ShipLeft, "water_left", OrangeColor, Vector3.zero);
    }

    // center of mass debugging should be yellow
    RenderDebugCube(ref worldCenterOfMassCube, vehicleManagerInstance.MovementController.m_body.worldCenterOfMass, "center_of_mass", Color.yellow, Vector3.up * 1);
    RenderDebugCube(ref vehiclePiecesCenterOfMassCube, vehicleManagerInstance.PiecesController.m_localRigidbody.worldCenterOfMass, "vehicle_pieces_automatic_center_of_mass", Color.yellow, Vector3.up * 0.5f);
    RenderDebugCube(ref vehicleMovementAutomaticCenterOfMassCube, vehicleManagerInstance.MovementController.m_body.position + vehicleManagerInstance.MovementController.vehicleAutomaticCenterOfMassPoint, "vehicle_automatic_center_of_mass", Color.yellow, Vector3.up * 2);

    // vehicle center debugging should be green
    RenderDebugCube(ref vehiclePiecesCenterCube, vehicleManagerInstance.PiecesController.transform.position, "vehicle_piece_center", Color.green, Vector3.up * 3);
    RenderDebugCube(ref vehicleMovementCenterCube, vehicleManagerInstance.MovementController.transform.position, "vehicle_movement_center", Color.green, Vector3.up * 4);

    RenderDebugCube(ref vehiclePieceCenterPoint, vehicleManagerInstance.PiecesController.vehicleCenter.transform.position, "piece_vehicle_center_point", Color.red, Vector3.up * 5);
  }

  private void FixedUpdate()
  {
    if (!isActiveAndEnabled) return;
    if (autoUpdateColliders ||
        VehicleDebugConfig.AutoShowVehicleColliders.Value)
    {
      RenderDebugCubes();
      DrawAllColliders();
      Update3DTextLookAt();
    }
  }

  private void OnDestroy()
  {
    lines.Values.ToList()
      .ForEach(x => x.ForEach(Destroy));
    lines.Clear();

    Destroy(worldCenterOfMassCube);
    Destroy(vehicleMovementAutomaticCenterOfMassCube);
    Destroy(vehiclePiecesCenterOfMassCube);
    Destroy(vehiclePiecesCenterCube);
    Destroy(vehiclePieceCenterPoint);
    Destroy(vehicleMovementCenterCube);
    Destroy(forwardCube);
    Destroy(backwardCube);
    Destroy(rightCube);
    Destroy(leftCube);

    foreach (var obj in targetColliders)
    {
      if (obj.collider == null) continue;
      Remove3DTextForCollider(obj.collider.gameObject.name);
    }
  }

  public void StartRenderAllCollidersLoop()
  {
    autoUpdateColliders = !autoUpdateColliders;
    RenderDebugCubes();

    if (autoUpdateColliders) return;
    foreach (var keyValuePair in lines)
    {
      if (!lines.TryGetValue(keyValuePair.Key, out var data)) continue;
      if (data == null) continue;
      foreach (var lineRenderer in keyValuePair.Value.ToList()
                 .OfType<LineRenderer>())
      {
        Destroy(lineRenderer.gameObject);
        data.Remove(lineRenderer);
      }
    }
  }

  private void Remove3DTextForCollider(string colliderName)
  {
    if (colliderTextObjects.ContainsKey(colliderName))
    {
      var textObj = colliderTextObjects[colliderName];
      Destroy(textObj); // Destroy the text object
      colliderTextObjects.Remove(colliderName); // Remove from dictionary
    }
  }

  private void Update3DTextLookAt()
  {
    // Loop through each collider text object and update the LookAt to always face the camera
    foreach (var textObject in colliderTextObjects.Values)
      if (textObject != null)
      {
        textObject.transform.LookAt(GameCamera.instance.transform);
        textObject.transform.Rotate(0, 180f,
          0); // Correct the reversed orientation
      }
  }

  public static string SplitCamelCase(string input)
  {
    // Regex to find uppercase letters that are preceded by lowercase letters
    var pattern = @"([a-z])([A-Z])";

    // Replace with the first letter, followed by a space, then the second letter
    var result = Regex.Replace(input, pattern, "$1 $2");

    // Trim to remove any leading or trailing spaces
    return result.Trim();
  }

  public void RenderDebugCube(ref GameObject? cube, Vector3 position,
    string cubeTitle, Color color, Vector3 textOffset)
  {
    if (!autoUpdateColliders)
    {
      if (cube != null) Destroy(cube);
      return;
    }

    if (vehicleManagerInstance.MovementController == null) return;

    if (cube == null)
    {
      // Create the cube
      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.name = $"debug_cube_{cubeTitle}";
      cube.transform.localScale = Vector3.one * 0.3f;
      var collider = cube.GetComponent<BoxCollider>();
      if (collider) Destroy(collider);

      var meshRenderer = cube.GetComponent<MeshRenderer>();
      meshRenderer.material =
        new Material(LoadValheimVehicleAssets.DoubleSidedTransparentMat)
        {
          color = color
        };
      cube.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

      // Add the text element as a child
      var textObj = new GameObject("CubeText");
      textObj.transform.SetParent(cube.transform);
      textObj.transform.localPosition =
        new Vector3(0, 1.2f, 0) + textOffset; // Adjust height as needed
      var textMesh = textObj.AddComponent<TextMesh>();
      var text = SplitCamelCase(cubeTitle.Replace("_", " "));
      textMesh.text = text; // Set desired text
      textMesh.fontSize = 32;
      textMesh.characterSize = 0.1f; // Adjust size as needed
      textMesh.anchor = TextAnchor.MiddleCenter;
      textMesh.alignment = TextAlignment.Center;
      textMesh.color = Color.yellow;
    }

    // Update the cube's position and set its parent
    cube.transform.position = position;
    cube.transform.SetParent(
      vehicleManagerInstance.PiecesController.transform,
      true);

    // Ensure the text always faces the camera
    var textTransform = cube.transform.Find("CubeText");
    if (textTransform != null && Camera.main != null)
    {
      textTransform.LookAt(Camera.main.transform);
      textTransform.rotation =
        Quaternion.LookRotation(textTransform.forward *
                                -1); // Flip to face correctly
    }
  }

  public static VehicleDebugHelpers? GetOnboardVehicleDebugHelper()
  {
    if (!Player.m_localPlayer) return null;

    var vehicleInstance = VehicleCommands.GetNearestVehicleManager();

    if (vehicleInstance == null) return null;

    vehicleInstance.AddOrRemoveVehicleDebugger();
    var helper = vehicleInstance.Instance.VehicleDebugHelpersInstance;

    return helper;
  }

  public void FlipShip()
  {
    if (!(bool)vehicleManagerInstance?.MovementController?.m_body) return;
    if (vehicleManagerInstance == null) return;
    if (vehicleManagerInstance.MovementController == null) return;
    // flips the x and z axis which act as the boat depth and sides
    // y-axis is boat height. Flipping that would just rotate boat which is why it is omitted
    if (!vehicleManagerInstance.isCreative)
      vehicleManagerInstance.MovementController.m_body.isKinematic = true;

    // transform.rotation = Quaternion.Euler(0, VehicleObj.transform.eulerAngles.y,
    //   0);
    vehicleManagerInstance.MovementController.m_body.rotation = Quaternion.Euler(0,
      VehicleObj.transform.eulerAngles.y,
      0);

    if (!vehicleManagerInstance.isCreative)
      vehicleManagerInstance.MovementController.m_body.isKinematic = false;
  }

  private static void DrawLine(Vector3 start, Vector3 end, int index,
    DrawLineData data)
  {
    var (color, material,
      parent, collider, lineItems,
      width) = data;
    if (lineItems == null) throw new ArgumentNullException(nameof(lineItems));

    LineRenderer line;
    if (index < lineItems.Count)
    {
      line = lineItems[index];
    }
    else
    {
      line = new GameObject($"{collider.name}_Line_{index}")
        .AddComponent<LineRenderer>();
      line.transform.SetParent(parent);
    }

    line.material = material;
    line.startColor = color;
    line.endColor = color;
    line.startWidth = width;
    line.endWidth = width;
    line.positionCount = 2;
    line.useWorldSpace = true;
    line.SetPosition(0, start);
    line.SetPosition(1, end);

    if (index < lineItems.Count)
      lineItems[index] = line;
    else
      lineItems.Add(line);
  }


  private static readonly Color LineColorDefault = Color.green;

  public void AddColliderToRerender(DrawTargetColliders drawTargetColliders)
  {
    if (!drawTargetColliders.collider.enabled) return;

    targetColliders.Add(drawTargetColliders);
  }

  private IEnumerable DrawCollidersCoroutine()
  {
    Logger.LogDebug("called DrawCollidersCoroutine");
    while (autoUpdateColliders == true)
    {
      Logger.LogDebug("DrawCollidersCoroutine Update");
      DrawAllColliders();
      yield return null;
    }

    yield return null;
  }

  private bool DrawAllColliders()
  {
    foreach (var drawTargetColliders in targetColliders)
      DrawColliders(drawTargetColliders.collider,
        drawTargetColliders.collider.transform,
        drawTargetColliders.lineColor, drawTargetColliders.name);

    return true;
  }

  private struct DrawLineData
  {
    public Color color;
    public Material material;
    public Transform parent;
    public BoxCollider boxCollider;
    public List<LineRenderer>? lineItems;
    public float width;

    public void Deconstruct(out Color o_color, out Material o_material,
      out Transform o_parent,
      out BoxCollider o_boxCollider, out List<LineRenderer>? o_lineItems,
      out float o_width)
    {
      o_color = color;
      o_material = material;
      o_parent = parent;
      o_boxCollider = boxCollider;
      o_lineItems = lineItems;
      o_width = width;
    }
  }

  /*
   * Debug any boxCollider, graphically visualizes the bounds
   */
  public void DrawColliders(BoxCollider boxCollider, Transform parent,
    Color? lineColor, string name)
  {
    var color = lineColor ?? LineColorDefault;

    if (boxCollider == null) return;
    if (!boxCollider.gameObject.activeInHierarchy) return;

    var material =
      new Material(LoadValheimVehicleAssets.DoubleSidedTransparentMat)
      {
        color = color
      };
    const float width = 0.05f;
    var boxColliderTransform = boxCollider.transform;

    var rightDir = boxColliderTransform.right.normalized;
    var forwardDir = boxColliderTransform.forward.normalized;
    var upDir = boxColliderTransform.up.normalized;
    var center = boxColliderTransform.position + boxCollider.center;
    var size = boxCollider.size;

    var lossyScale = boxColliderTransform.lossyScale;

    size.x *= lossyScale.x;
    size.y *= lossyScale.y;
    size.z *= lossyScale.z;
    var extents = size / 2f;

    var topMostDir = upDir * extents.y;
    var rightMostDir = rightDir * extents.x;
    var forwardMostDir = forwardDir * extents.z;

    var forwardTopRight = center + topMostDir + rightMostDir +
                          forwardMostDir;
    var forwardBottomRight =
      center - topMostDir + rightMostDir + forwardMostDir;
    var forwardTopLeft = center + topMostDir - rightMostDir + forwardMostDir;
    var forwardBottomLeft = center - topMostDir - rightMostDir + forwardMostDir;

    var backwardTopRight = center + topMostDir + rightMostDir -
                           forwardMostDir;
    var backwardBottomRight =
      center - topMostDir + rightMostDir - forwardMostDir;
    var backwardTopLeft = center + topMostDir - rightMostDir - forwardMostDir;
    var backwardBottomLeft =
      center - topMostDir - rightMostDir - forwardMostDir;

    if (!lines.TryGetValue(boxCollider.name, out var colliderItems))
    {
      colliderItems = [];
      lines.Add(boxCollider.name, colliderItems);
    }

    var lineRendererData = new DrawLineData
    {
      color = color,
      boxCollider = boxCollider,
      parent = parent,
      material = material,
      lineItems = colliderItems,
      width = 0.1f
    };

    var index = 0;
    DrawLine(forwardTopRight,
      forwardTopLeft, index, lineRendererData);
    index++;
    DrawLine(forwardBottomRight,
      forwardBottomLeft, index, lineRendererData);
    index++;
    DrawLine(forwardTopRight,
      forwardBottomRight, index, lineRendererData);
    index++;
    DrawLine(forwardTopLeft,
      forwardBottomLeft, index, lineRendererData);
    index++;

    DrawLine(backwardTopRight,
      backwardTopLeft, index, lineRendererData);
    index++;
    DrawLine(backwardBottomRight,
      backwardBottomLeft, index, lineRendererData);
    index++;
    DrawLine(backwardTopRight,
      backwardBottomRight, index, lineRendererData);
    index++;
    DrawLine(backwardTopLeft,
      backwardBottomLeft, index, lineRendererData);
    index++;

    DrawLine(backwardTopLeft,
      forwardTopLeft, index, lineRendererData);
    index++;
    DrawLine(backwardBottomLeft,
      forwardBottomLeft, index, lineRendererData);
    index++;
    DrawLine(backwardTopRight,
      forwardTopRight, index, lineRendererData);
    index++;
    DrawLine(backwardBottomRight,
      forwardBottomRight, index, lineRendererData);

    lines[boxCollider.name] = colliderItems;

    Update3DTextForCollider(boxCollider, parent, name, color);
  }

  private void Update3DTextForCollider(BoxCollider boxCollider,
    Transform parent, string textTitle, Color color)

  {
    // If text already exists for this collider, update it
    if (colliderTextObjects.ContainsKey(boxCollider.gameObject.name))
    {
      var existingText = colliderTextObjects[boxCollider.gameObject.name];
      // Update text content and position if necessary
      existingText.transform.position = boxCollider.transform.position;
      existingText.transform.SetParent(parent);

      // Ensure the text always faces the camera
      existingText.transform.LookAt(GameCamera.instance.transform);
      existingText.transform.Rotate(0, 180f,
        0); // Correct the reversed orientation
    }
    else
    {
      // Create new 3D Text for this collider
      var textObj = new GameObject($"{boxCollider.gameObject.name}_label");
      var textMeshPro = textObj.AddComponent<TextMeshPro>();

      // Set text properties
      textMeshPro.text = textTitle;
      textMeshPro.fontSize = 10;
      textMeshPro.color = color;

      // Position the text next to the collider
      textObj.transform.position = boxCollider.transform.position;
      textObj.transform.SetParent(parent);

      // Make text face the camera
      textObj.transform.LookAt(GameCamera.instance.transform);
      textObj.transform.Rotate(0, 180f, 0); // Correct the reversed orientation

      // Add text to dictionary for future updates
      colliderTextObjects.Add(boxCollider.gameObject.name, textObj);
    }
  }
}