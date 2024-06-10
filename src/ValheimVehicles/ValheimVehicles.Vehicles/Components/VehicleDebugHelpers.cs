using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Vehicles;

public interface IDrawTargetColliders
{
  public BoxCollider collider { get; set; }
  public Color lineColor { get; set; }
  public GameObject parent { get; set; }
}

public struct DrawTargetColliders : IDrawTargetColliders
{
  public BoxCollider collider { get; set; }
  public Color lineColor { get; set; }
  public GameObject parent { get; set; }
}

public class VehicleDebugHelpers : MonoBehaviour
{
  private Dictionary<string, List<LineRenderer>> lines = new();

  public bool autoUpdateColliders = false;
  private List<IDrawTargetColliders> targetColliders = [];
  public GameObject VehicleObj;
  public VehicleShip VehicleShipInstance;
  private Coroutine? _drawColliderCoroutine = null;

  private InvokeBinder repeatInvoke;

  private void FixedUpdate()
  {
    if (!isActiveAndEnabled) return;
    if (autoUpdateColliders)
    {
      DrawAllColliders();
    }
  }

  public void StartRenderAllCollidersLoop()
  {
    autoUpdateColliders = !autoUpdateColliders;
    if (autoUpdateColliders) return;
    foreach (var keyValuePair in lines)
    {
      if (!lines.TryGetValue(keyValuePair.Key, out var data)) continue;
      if (data == null) continue;
      foreach (var lineRenderer in keyValuePair.Value.ToList().OfType<LineRenderer>())
      {
        Destroy(lineRenderer.gameObject);
        data.Remove(lineRenderer);
      }
    }
  }

  private static RaycastHit? RaycastToPiecesUnderPlayerCamera()
  {
    var player = Player.m_localPlayer;
    if (player == null)
    {
      return null;
    }

    if (!Physics.Raycast(
          GameCamera.instance.transform.position, GameCamera.instance.transform.forward,
          out var rayCastHitInfo, 50f, LayerMask.GetMask("piece"))) return null;
    return rayCastHitInfo;
  }

  public static VehicleDebugHelpers GetOnboardVehicleDebugHelper()
  {
    return GetVehicleController()?.VehicleInstance.Instance.VehicleDebugHelpersInstance;
  }

  public static VehicleDebugHelpers? GetOnboardMBRaftDebugHelper()
  {
    return GetMBRaftController()?.shipController?.VehicleDebugHelpersInstance;
  }

  public static MoveableBaseRootComponent? GetMBRaftController()
  {
    var rayCastHitInfo = RaycastToPiecesUnderPlayerCamera();
    return rayCastHitInfo?.collider.GetComponentInParent<MoveableBaseRootComponent>();
  }

  public static BaseVehicleController? GetVehicleController()
  {
    var rayCastHitInfo = RaycastToPiecesUnderPlayerCamera();
    return rayCastHitInfo?.collider.transform.root.GetComponent<BaseVehicleController>();
  }

  public void FlipShip()
  {
    if (!(bool)VehicleShipInstance.MovementController.m_body) return;
    // flips the x and z axis which act as the boat depth and sides
    // y-axis is boat height. Flipping that would just rotate boat which is why it is omitted
    if (!VehicleShipInstance.isCreative)
    {
      VehicleShipInstance.MovementController.m_body.isKinematic = true;
    }

    transform.rotation = Quaternion.Euler(0, VehicleObj.transform.eulerAngles.y,
      0);

    if (!VehicleShipInstance.isCreative)
    {
      VehicleShipInstance.MovementController.m_body.isKinematic = false;
    }
  }

  /// <summary>
  /// Eventually will be used to visualize colliders on ship.
  /// </summary>
  ///
  /// todo fix logic to be accurate
  /// <param name="controller"></param>
  public void RenderAllVehicleBoxColliders(BaseVehicleController controller)
  {
    // foreach (var instanceMPiece in controller.m_pieces)
    // {
    //   var boxColliders = instanceMPiece.GetComponentsInChildren<BoxCollider>();
    //   foreach (var boxCollider in boxColliders)
    //   {
    //     AddColliderToRerender(new DrawTargetColliders()
    //     {
    //       collider = boxCollider,
    //       lineColor = Color.yellow,
    //       parent = controller.gameObject
    //     });
    //   }
    // }
  }

  public void MoveShip(Vector3 vector)
  {
    if (!(bool)VehicleShipInstance.MovementController.m_body) return;
    // flips the x and z axis which act as the boat depth and sides
    // y axis is boat height. Flipping that would just rotate boat which is why it is omitted
    VehicleShipInstance.MovementController.m_body.isKinematic = true;
    transform.rotation = Quaternion.Euler(0f, VehicleObj.transform.eulerAngles.y,
      0f);
    transform.position += vector;
    VehicleShipInstance.MovementController.m_body.isKinematic = false;
  }

  private static void DrawLine(Vector3 start, Vector3 end, int index, DrawLineData data)
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
      line.transform.SetParent(parent.transform);
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
    {
      lineItems[index] = line;
    }
    else
    {
      lineItems.Add(line);
    }
  }


  private static readonly Color LineColorDefault = Color.green;

  public void AddColliderToRerender(IDrawTargetColliders drawTargetColliders)
  {
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
    {
      DrawColliders(drawTargetColliders.collider, drawTargetColliders.parent,
        drawTargetColliders.lineColor);
    }

    return true;
  }

  private struct DrawLineData
  {
    public Color color;
    public Material material;
    public GameObject parent;
    public BoxCollider boxCollider;
    public List<LineRenderer>? lineItems;
    public float width;

    public void Deconstruct(out Color o_color, out Material o_material, out GameObject o_parent,
      out BoxCollider o_boxCollider, out List<LineRenderer>? o_lineItems, out float o_width)
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
  public void DrawColliders(BoxCollider boxCollider, GameObject parent,
    Color? lineColor)
  {
    var color = lineColor ?? LineColorDefault;

    if (boxCollider == null) return;
    var unlitColor = LoadValheimVehicleAssets.PieceShader;
    var material = new Material(unlitColor)
    {
      color = color
    };
    const float width = 0.05f;
    var rightDir = boxCollider.transform.right.normalized;
    var forwardDir = boxCollider.transform.forward.normalized;
    var upDir = boxCollider.transform.up.normalized;
    var center = boxCollider.transform.position + boxCollider.center;
    var size = boxCollider.size;
    size.x *= boxCollider.transform.lossyScale.x;
    size.y *= boxCollider.transform.lossyScale.y;
    size.z *= boxCollider.transform.lossyScale.z;
    var extents = size / 2f;

    var topMostDir = upDir * extents.y;
    var rightMostDir = rightDir * extents.x;
    var forwardMostDir = forwardDir * extents.z;

    var forwardTopRight = center + topMostDir + rightMostDir +
                          forwardMostDir;
    var forwardBottomRight = center - topMostDir + rightMostDir + forwardMostDir;
    var forwardTopLeft = center + topMostDir - rightMostDir + forwardMostDir;
    var forwardBottomLeft = center - topMostDir - rightMostDir + forwardMostDir;

    var backwardTopRight = center + topMostDir + rightMostDir -
                           forwardMostDir;
    var backwardBottomRight = center - topMostDir + rightMostDir - forwardMostDir;
    var backwardTopLeft = center + topMostDir - rightMostDir - forwardMostDir;
    var backwardBottomLeft = center - topMostDir - rightMostDir - forwardMostDir;

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
  }
}