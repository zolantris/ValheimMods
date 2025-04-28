using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Prefabs;

namespace ValheimVehicles.Components;

public class CircleLine : MonoBehaviour
{
  public int segments = 36;
  public Color MaterialColor = CircleWindColors.ValheimWindGray;
  private Material lineRendererMaterial;
  private float _radius = 1f;

  public float Radius
  {
    get => _radius;
    set
    {
      _radius = value;
      Draw();
    }
  }

  // 0-360f
  public float arc = 360f;

  public LineRenderer? LineRendererInstance;

  private GameObject? _circleInstance;

  private void Awake()
  {
    CreateCircleLine();
  }

  private void CreateCircleLine()
  {
    lineRendererMaterial = new Material(LoadValheimAssets.CustomPieceShader)
    {
      color = MaterialColor
    };
    _circleInstance = new GameObject("CircleLine")
    {
      layer = LayerMask.NameToLayer("UI"),
      transform = { parent = transform }
    };
    LineRendererInstance = _circleInstance.AddComponent<LineRenderer>();
    LineRendererInstance.material = lineRendererMaterial;
    LineRendererInstance.endWidth = 10f;
    LineRendererInstance.startWidth = 10f;
  }

  public void Start()
  {
    CheckForUpdates();
    Draw();
  }

  private void OnEnable()
  {
    CheckForUpdates();

    if (LineRendererInstance != null && _circleInstance != null)
    {
      Draw();
      return;
    }


    // DestroyPreviousComponents();
    CreateCircleLine();
    Draw();
  }

  private void CheckForUpdates()
  {
    if (MaterialColor != lineRendererMaterial.color)
    {
      lineRendererMaterial.color = MaterialColor;
    }
  }

  private void DestroyPreviousComponents()
  {
    if (LineRendererInstance)
    {
      Destroy(LineRendererInstance.gameObject);
    }

    if (_circleInstance)
    {
      Destroy(_circleInstance.gameObject);
    }
  }

  private void OnDestroy()
  {
    DestroyPreviousComponents();
  }

  public void DrawPartialCircle()
  {
    var seg = segments;
    if (!LineRendererInstance.loop)
    {
      seg += 1;
    }

    var points = new Vector3[seg];
    for (var i = 0; i < seg; i += 1)
    {
      var rad = Mathf.Deg2Rad * (i * arc / segments);
      points[i] = new Vector3(Mathf.Cos(rad) * Radius, Mathf.Sin(rad) * -Radius, 0);
    }

    LineRendererInstance.positionCount = seg;
    LineRendererInstance.SetPositions(points);
  }

  public void Draw()
  {
    var seg = segments;
    if (!LineRendererInstance.loop)
    {
      seg += 1;
    }

    var halfArcRange = arc / 2;
    var startPoint = -halfArcRange;
    var increment = arc / seg;
    var points = new List<Vector3>();

    var currentPoint = startPoint;
    while (points.Count < seg)
    {
      var rad = Mathf.Deg2Rad * currentPoint;
      points.Add(new Vector3(Mathf.Sin(rad) - Radius, Mathf.Cos(rad) * Radius,
        transform.localPosition.z));
      currentPoint += increment;
    }

    if (points.Count > 2 && Math.Abs(arc - 360) < 0.5)
    {
      var firstPoint = points.First();
      var lastPoint = points.Last();

      if (lastPoint != firstPoint)
      {
        points.Remove(lastPoint);
        points.Add(firstPoint);
      }
    }

    var pointsArray = points.ToArray();
    LineRendererInstance.positionCount = seg;
    LineRendererInstance.SetPositions(pointsArray);
  }
}