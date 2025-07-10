// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class RuntimeDebugLineDrawer : SingletonBehaviour<RuntimeDebugLineDrawer>
  {
    private static GameObject _singletonObject;
    public static Material DebugRayMaterial;
#if UNITY_EDITOR
    public static bool IsEnabled = true;
#else
    public static bool IsEnabled = false;
#endif
    public static bool HasMaterialColorOverride = true;
    public static Color TRed = new(1f, 0, 0, 0.75f);
    public static Color TGreen = new(0, 1, 0, 0.75f);
    public static Color TBlue = new(0, 0.5f, 1, 0.75f);
    public static Color TOrange = new(1, 0.5f, 0, 0.75f);
    public static Color TYellow = new(1, 1f, 0, 0.75f);
    private readonly Queue<LineInstance> _linePool = new();
    private readonly List<LineInstance> _lines = new();
    private Material _defaultMat;

    private void Update()
    {
      if (!IsEnabled) return;
      var now = Time.realtimeSinceStartup;
      for (var i = 0; i < _lines.Count; ++i)
      {
        var line = _lines[i];
        if (line.active && now > line.expiry)
        {
          line.renderer.enabled = false;
          line.active = false;
          _linePool.Enqueue(line);
        }
      }
    }

    public override void OnAwake()
    {
      if (_defaultMat == null)
      {
        if (DebugRayMaterial == null)
        {
          var shader = Shader.Find("Sprites/Default");
          _defaultMat = new Material(shader)
          {
            color = Color.white,
            renderQueue = 3000 // Optional: transparent queue
          };
        }
        else
        {
          _defaultMat = DebugRayMaterial;
          _defaultMat.renderQueue = 3000; // Optional: transparent queue
        }
      }
    }

    private static void Init()
    {
      if (!IsEnabled) return;
      _singletonObject = new GameObject("RuntimeDebugLineDrawer_Singleton", typeof(RuntimeDebugLineDrawer));
      Instance = _singletonObject.GetComponent<RuntimeDebugLineDrawer>();
      DontDestroyOnLoad(_singletonObject);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.1f, float width = 0.04f)
    {
      if (!IsEnabled) return;
      if (Instance == null) Init();
      if (Instance == null || !Instance.isActiveAndEnabled) return;

      // Reuse a pooled line or create new
      LineInstance inst = null;
      while (Instance._linePool.Count > 0 && inst == null)
      {
        var candidate = Instance._linePool.Dequeue();
        if (candidate.renderer) inst = candidate;
      }
      if (inst == null)
      {
        var go = new GameObject("[DebugLine]");
        go.transform.SetParent(Instance.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = Instance._defaultMat;
        lr.positionCount = 2;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.useWorldSpace = true;
        inst = new LineInstance { renderer = lr };
        Instance._lines.Add(inst);
      }

      inst.active = true;
      inst.expiry = Time.realtimeSinceStartup + Mathf.Max(0.016f, duration);

      inst.renderer.enabled = true;
      inst.renderer.SetPosition(0, start);
      inst.renderer.SetPosition(1, end);

      if (HasMaterialColorOverride)
      {
        inst.renderer.material.color = color;
      }

      inst.renderer.startColor = color;
      inst.renderer.endColor = color;
      inst.renderer.startWidth = width;
      inst.renderer.endWidth = width;
    }

    public static void ClearAll()
    {
      if (Instance == null) return;
      if (Instance._lines.Count == 0) return;
      foreach (var l in Instance._lines)
      {
        if (l.renderer)
          l.renderer.enabled = false;
        l.active = false;
        Instance._linePool.Enqueue(l);
      }
    }

    private class LineInstance
    {
      public bool active;
      public float expiry;
      public LineRenderer renderer;
    }
  }
}