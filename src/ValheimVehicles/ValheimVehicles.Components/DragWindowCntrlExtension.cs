using System;
using Jotunn.GUI;
using UnityEngine;
using UnityEngine.EventSystems;
namespace ValheimVehicles.Vehicles;

/// <summary>
/// Copied from Jotunn.DragWindowCntrl
/// </summary>
public class DragWindowCntrlExtension : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler
{
  private RectTransform window;
  private Vector2 delta;
  public Action? OnDragCalled = null;

  private void Awake()
  {
    window = (RectTransform)transform;
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    delta = Input.mousePosition - window.position;
  }

  public void OnDrag(PointerEventData eventData)
  {
    var vector2 = eventData.position - delta;
    var rect = window.rect;
    var lossyScale = (Vector2)window.lossyScale;
    var min1 = rect.width / 2f * lossyScale.x;
    var max1 = Screen.width - min1;
    var min2 = rect.height / 2f * lossyScale.y;
    var max2 = Screen.height - min2;
    vector2.x = Mathf.Clamp(vector2.x, min1, max1);
    vector2.y = Mathf.Clamp(vector2.y, min2, max2);
    transform.position = vector2;
    OnDragCalled?.Invoke();
  }
}