using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Prefabs;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Vehicles;

public interface IDrawTargetColliders
{
  public BoxCollider collider { get; set; }
  public Color lineColor { get; set; }
  public GameObject parent { get; set; }
}

public class DrawTargetColliders : IDrawTargetColliders
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
  GUIStyle myButtonStyle;
  public GameObject VehicleObj;
  public VehicleShip VehicleShipInstance;
  private Coroutine? _drawColliderCoroutine = null;

  public void Awake()
  {
    if (!autoUpdateColliders)
    {
      return;
    }

    _drawColliderCoroutine = StartCoroutine(nameof(DrawCollidersCoroutine));
  }

  private InvokeBinder repeatInvoke;

  private void OnGUI()
  {
    if (myButtonStyle == null)
    {
      myButtonStyle = new GUIStyle(GUI.skin.button);
      myButtonStyle.fontSize = 50;
    }

    GUILayout.BeginArea(new Rect(500, 10, 150, 150), myButtonStyle);
    if (GUILayout.Button("toggle collider visualization"))
    {
      autoUpdateColliders = !autoUpdateColliders;

      CancelInvoke(nameof(DrawAllColliders));
      if (autoUpdateColliders)
      {
        InvokeRepeating(nameof(DrawAllColliders), 0f, 0.1f);
      }

      // if (_drawColliderCoroutine != null)
      // {
      //   StopCoroutine(_drawColliderCoroutine);
      //   _drawColliderCoroutine = null;
      // }
      //
      // if (autoUpdateColliders)
      // {
      //   _drawColliderCoroutine = StartCoroutine(nameof(DrawCollidersCoroutine));
      // }
    }

    // if (GUILayout.Button("run collider visual"))
    // {
    //   autoUpdateColliders = false;
    //   StopCoroutine(nameof(DrawCollidersCoroutine));
    //   DrawAllColliders();
    // }

    if (GUILayout.Button("Flip Ship"))
    {
      FlipShip();
    }

    GUILayout.EndArea();
  }

  private void FlipShip()
  {
    if (VehicleShipInstance.m_body == null) return;
    // flips the x and z axis which act as the boat depth and sides
    // y axis is boat height. Flipping that would just rotate boat which is why it is omitted
    VehicleShipInstance.m_body.isKinematic = true;
    transform.rotation = Quaternion.Euler(0f, VehicleObj.transform.eulerAngles.y,
      0f);
    VehicleShipInstance.m_body.isKinematic = false;
  }

  private void DrawLine(Vector3 start, Vector3 end, Color color, Material material,
    GameObject parent, List<LineRenderer> lineItems, int index,
    float width = 0.01f)
  {
    if (lineItems == null) throw new ArgumentNullException(nameof(lineItems));

    LineRenderer line;
    if (index < lineItems.Count)
    {
      line = lineItems[index];
    }
    else
    {
      line = new GameObject($"{parent.name}_Line_{index}")
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
    while (autoUpdateColliders)
    {
      Logger.LogDebug("DrawCollidersCoroutine Update");
      DrawAllColliders();
      yield return new WaitForEndOfFrame();
    }
  }

  private bool DrawAllColliders()
  {
    Logger.LogDebug("DrawAllColliders called");
    foreach (var drawTargetColliders in targetColliders)
    {
      DrawColliders(drawTargetColliders.collider, drawTargetColliders.parent,
        drawTargetColliders.lineColor);
    }

    return true;
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
    const float width = 0.01f;
    var rightDir = boxCollider.transform.right.normalized;
    var forwardDir = boxCollider.transform.forward.normalized;
    var upDir = boxCollider.transform.up.normalized;
    var center = boxCollider.transform.position + boxCollider.center;
    var size = boxCollider.size;
    size.x *= boxCollider.transform.lossyScale.x;
    size.y *= boxCollider.transform.lossyScale.y;
    size.z *= boxCollider.transform.lossyScale.z;

    if (!lines.TryGetValue(boxCollider.name, out var colliderItems))
    {
      colliderItems = [];
      lines.Add(boxCollider.name, colliderItems);
    }

    var index = 0;
    DrawLine(center + upDir * size.y / 2f + rightDir * size.x / 2f + forwardDir * size.z / 2f,
      center + upDir * size.y / 2f - rightDir * size.x / 2f + forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center - upDir * size.y / 2f + rightDir * size.x / 2f + forwardDir * size.z / 2f,
      center - upDir * size.y / 2f - rightDir * size.x / 2f + forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center + upDir * size.y / 2f + rightDir * size.x / 2f + forwardDir * size.z / 2f,
      center - upDir * size.y / 2f + rightDir * size.x / 2f + forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center + upDir * size.y / 2f - rightDir * size.x / 2f + forwardDir * size.z / 2f,
      center - upDir * size.y / 2f - rightDir * size.x / 2f + forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center + upDir * size.y / 2f + rightDir * size.x / 2f - forwardDir * size.z / 2f,
      center + upDir * size.y / 2f - rightDir * size.x / 2f - forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center - upDir * size.y / 2f + rightDir * size.x / 2f - forwardDir * size.z / 2f,
      center - upDir * size.y / 2f - rightDir * size.x / 2f - forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center + upDir * size.y / 2f + rightDir * size.x / 2f - forwardDir * size.z / 2f,
      center - upDir * size.y / 2f + rightDir * size.x / 2f - forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center + upDir * size.y / 2f - rightDir * size.x / 2f - forwardDir * size.z / 2f,
      center - upDir * size.y / 2f - rightDir * size.x / 2f - forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center + upDir * size.y / 2f + rightDir * size.x / 2f + forwardDir * size.z / 2f,
      center + upDir * size.y / 2f + rightDir * size.x / 2f - forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center - upDir * size.y / 2f + rightDir * size.x / 2f + forwardDir * size.z / 2f,
      center - upDir * size.y / 2f + rightDir * size.x / 2f - forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center + upDir * size.y / 2f - rightDir * size.x / 2f + forwardDir * size.z / 2f,
      center + upDir * size.y / 2f - rightDir * size.x / 2f - forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);
    index++;
    DrawLine(center - upDir * size.y / 2f - rightDir * size.x / 2f + forwardDir * size.z / 2f,
      center - upDir * size.y / 2f - rightDir * size.x / 2f - forwardDir * size.z / 2f, color,
      material, parent, colliderItems, index, width);

    lines[boxCollider.name] = colliderItems;
  }
}