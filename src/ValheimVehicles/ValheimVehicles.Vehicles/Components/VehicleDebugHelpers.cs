using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Vehicles;

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
  public VehicleShip VehicleShipInstance;
  private Coroutine? _drawColliderCoroutine = null;
  private GameObject? worldCenterOfMassCube;
  private GameObject? forwardCube;
  private GameObject? backwardCube;
  private GameObject? rightCube;
  private GameObject? leftCube;

  private void RenderForcePointCubes()
  {
    if (VehicleShipInstance == null ||
        VehicleShipInstance.MovementController == null) return;

    var shipFloatation = VehicleShipInstance
      .MovementController.GetShipFloatation();

    if (shipFloatation == null) return;

    RenderWaterForceCube(ref forwardCube, shipFloatation.Value.ShipForward,
      "forward");
    RenderWaterForceCube(ref backwardCube, shipFloatation.Value.ShipBack,
      "backward");
    RenderWaterForceCube(ref rightCube, shipFloatation.Value.ShipRight,
      "right");
    RenderWaterForceCube(ref leftCube, shipFloatation.Value.ShipLeft, "left");
  }

  private void FixedUpdate()
  {
    if (!isActiveAndEnabled) return;
    if (autoUpdateColliders ||
        VehicleDebugConfig.AutoShowVehicleColliders.Value)
    {
      RenderForcePointCubes();
      RenderWorldCenterOfMassAsCube();
      DrawAllColliders();
      Update3DTextLookAt();
    }
  }

  private void OnDestroy()
  {
    if (worldCenterOfMassCube != null) Destroy(worldCenterOfMassCube);
    lines.Values.ToList()
      .ForEach(x => x.ForEach(Destroy));
    lines.Clear();

    foreach (var obj in targetColliders)
    {
      if (obj.collider == null) continue;
      Remove3DTextForCollider(obj.collider.gameObject.name);
    }
  }

  public void StartRenderAllCollidersLoop()
  {
    autoUpdateColliders = !autoUpdateColliders;
    RenderWorldCenterOfMassAsCube();
    RenderForcePointCubes();

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


  public void RenderWaterForceCube(ref GameObject? cube, Vector3 position,
    string cubeTitle)
  {
    if (!autoUpdateColliders)
    {
      if (cube != null) Destroy(cube);
      return;
    }

    if (VehicleShipInstance.MovementController == null) return;

    if (cube == null)
    {
      // Create the cube
      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.name = $"force_cube_{cubeTitle}";
      var collider = cube.GetComponent<BoxCollider>();
      if (collider) Destroy(collider);

      var meshRenderer = cube.GetComponent<MeshRenderer>();
      meshRenderer.material =
        new Material(LoadValheimVehicleAssets.DoubleSidedTransparentMat)
        {
          color = Color.green
        };
      cube.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

      // Add the text element as a child
      var textObj = new GameObject("CubeText");
      textObj.transform.SetParent(cube.transform);
      textObj.transform.localPosition =
        new Vector3(0, 1.2f, 0); // Adjust height as needed

      var textMesh = textObj.AddComponent<TextMesh>();
      textMesh.text = $"Force Cube {cubeTitle}"; // Set desired text
      textMesh.fontSize = 32;
      textMesh.characterSize = 0.1f; // Adjust size as needed
      textMesh.anchor = TextAnchor.MiddleCenter;
      textMesh.alignment = TextAlignment.Center;
      textMesh.color = Color.yellow;
    }

    // Update the cube's position and set its parent
    cube.transform.position = position;
    cube.transform.SetParent(
      VehicleShipInstance.PiecesController.transform,
      false);

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


  public void RenderWorldCenterOfMassAsCube()
  {
    if (!autoUpdateColliders)
    {
      if (worldCenterOfMassCube != null) Destroy(worldCenterOfMassCube);
      return;
    }

    if (VehicleShipInstance.MovementController == null) return;
    if (worldCenterOfMassCube == null)
    {
      worldCenterOfMassCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      var collider = worldCenterOfMassCube.GetComponent<BoxCollider>();
      if (collider) Destroy(collider);
      var meshRenderer = worldCenterOfMassCube.GetComponent<MeshRenderer>();
      meshRenderer.material =
        new Material(LoadValheimVehicleAssets.DoubleSidedTransparentMat)
        {
          color = Color.yellow
        };
      worldCenterOfMassCube.gameObject.layer =
        LayerMask.NameToLayer("Ignore Raycast");
    }

    worldCenterOfMassCube.transform.position = VehicleShipInstance
      .MovementController.m_body
      .worldCenterOfMass;
    worldCenterOfMassCube.transform.SetParent(
      VehicleShipInstance.PiecesController.transform,
      false);
  }

  private static RaycastHit? RaycastToPiecesUnderPlayerCamera()
  {
    var player = Player.m_localPlayer;
    if (player == null) return null;

    if (!Physics.Raycast(
          GameCamera.instance.transform.position,
          GameCamera.instance.transform.forward,
          out var rayCastHitInfo, 50f, LayerMask.GetMask("piece"))) return null;
    return rayCastHitInfo;
  }

  public static VehicleDebugHelpers GetOnboardVehicleDebugHelper()
  {
    var helper = GetVehiclePiecesController()?.VehicleInstance?.Instance
      ?.VehicleDebugHelpersInstance;
    return helper;
  }

  public static VehicleDebugHelpers? GetOnboardMBRaftDebugHelper()
  {
    return GetMBRaftController()?.shipController?.VehicleDebugHelpersInstance;
  }

  public static MoveableBaseRootComponent? GetMBRaftController()
  {
    var rayCastHitInfo = RaycastToPiecesUnderPlayerCamera();
    return rayCastHitInfo?.collider
      .GetComponentInParent<MoveableBaseRootComponent>();
  }

  public static VehiclePiecesController? GetVehiclePiecesController()
  {
    var rayCastHitInfo = RaycastToPiecesUnderPlayerCamera();
    return rayCastHitInfo?.collider.transform.root
      .GetComponent<VehiclePiecesController>();
  }

  public void FlipShip()
  {
    if (!(bool)VehicleShipInstance?.MovementController?.m_body) return;
    if (VehicleShipInstance == null) return;
    if (VehicleShipInstance.MovementController == null) return;
    // flips the x and z axis which act as the boat depth and sides
    // y-axis is boat height. Flipping that would just rotate boat which is why it is omitted
    if (!VehicleShipInstance.isCreative)
      VehicleShipInstance.MovementController.m_body.isKinematic = true;

    // transform.rotation = Quaternion.Euler(0, VehicleObj.transform.eulerAngles.y,
    //   0);
    VehicleShipInstance.MovementController.m_body.rotation = Quaternion.Euler(0,
      VehicleObj.transform.eulerAngles.y,
      0);

    if (!VehicleShipInstance.isCreative)
      VehicleShipInstance.MovementController.m_body.isKinematic = false;
  }

  public void MoveShip(Vector3 vector)
  {
    if (!(bool)VehicleShipInstance.MovementController.m_body) return;
    // flips the x and z axis which act as the boat depth and sides
    // y axis is boat height. Flipping that would just rotate boat which is why it is omitted
    VehicleShipInstance.MovementController.m_body.isKinematic = true;
    transform.rotation = Quaternion.Euler(0f,
      VehicleObj.transform.eulerAngles.y,
      0f);
    transform.position += vector;
    VehicleShipInstance.MovementController.m_body.isKinematic = false;
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

    var lineRendererData = new DrawLineData()
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
      var offset = new Vector3(0, boxCollider.size.y / 2f + 1f, 0);
      existingText.transform.position = boxCollider.transform.position + offset;
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
      var offset = new Vector3(0, boxCollider.size.y / 2f + 1f, 0);
      textObj.transform.position = boxCollider.transform.position + offset;
      textObj.transform.SetParent(parent);

      // Make text face the camera
      textObj.transform.LookAt(GameCamera.instance.transform);
      textObj.transform.Rotate(0, 180f, 0); // Correct the reversed orientation

      // Add text to dictionary for future updates
      colliderTextObjects.Add(boxCollider.gameObject.name, textObj);
    }
  }
}