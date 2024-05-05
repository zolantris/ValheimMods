using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
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

    public float arc = 360f;

    public LineRenderer LineRendererInstance;

    private GameObject m_circleInstance;
    private void Awake()
    {
        DestroyPreviousComponents();
        CreateCircleLine();
    }
    
    private void CreateCircleLine()
    {
        DestroyPreviousComponents();
        var shader = Shader.Find("Unlit/Lighting");
        lineRendererMaterial = new Material(shader)
        {
            color = MaterialColor
        };
        m_circleInstance = new GameObject("CircleLine")
        {
            transform = { parent = transform }
        };
        LineRendererInstance = m_circleInstance.AddComponent<LineRenderer>();
        LineRendererInstance.material = lineRendererMaterial;
        LineRendererInstance.endWidth = 0.1f;
        LineRendererInstance.startWidth = 0.1f;
    }

    public void Start()
    {
        CheckForUpdates();
        Draw();
    }

    private void OnEnable()
    {
        CheckForUpdates();
        
        if (LineRendererInstance && m_circleInstance)
        {
            Draw();
            return;
        }

        DestroyPreviousComponents();
        CreateCircleLine();
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

        if (m_circleInstance)
        {
            Destroy(m_circleInstance.gameObject);
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
        Debug.Log(points);

        LineRendererInstance.positionCount = seg;
        LineRendererInstance.SetPositions(points);
    }

    public void Draw()
    {
        // rotate the whole object to align with middle of arc
        if (arc < 360)
        {
            transform.Rotate(0, 0, 180);
        }
        
        
        var seg = segments;
        if (!LineRendererInstance.loop)
        {
            seg += 1;
        }
        
        var halfArcRange = arc / 2;
        var startPoint = -halfArcRange;
        var endPoint = halfArcRange;
        var increment = arc / seg;
        // -180 0 180
        //  
        var points = new List<Vector3>();

        var currentPoint = startPoint;
        while(points.Count < seg)
        {
            var rad = Mathf.Deg2Rad * currentPoint;
            points.Add(new Vector3( Mathf.Sin(rad) - Radius,Mathf.Cos(rad) * Radius, transform.localPosition.z));
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
        // transform.InverseTransformPoints(points);
        // transform.TransformPoints(pointsArray);
        LineRendererInstance.SetPositions(pointsArray);
    }
}
