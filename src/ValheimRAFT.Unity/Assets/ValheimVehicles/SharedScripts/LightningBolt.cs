//
// Lightning Bolt for Unity
// (c) 2016 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{

  /// <summary>
  /// Types of animations for lightning bolts
  /// </summary>
  public enum LightningBoltAnimationMode
  {
    /// <summary>
    /// No animation
    /// </summary>
    None,

    /// <summary>
    /// Pick a random frame
    /// </summary>
    Random,

    /// <summary>
    /// Loop through each frame and restart at the beginning
    /// </summary>
    Loop,

    /// <summary>
    /// Loop through each frame then go backwards to the beginning then forward, etc.
    /// </summary>
    PingPong
  }

  /// <summary>
  /// Allows creation of simple lightning bolts
  /// </summary>
  [RequireComponent(typeof(LineRenderer))]
  public class LightningBolt : MonoBehaviour
  {
    [Tooltip("The game object where the lightning will emit from. If null, StartPosition is used.")]
    public GameObject StartObject;

    [Tooltip("The start position where the lightning will emit from. This is in world space if StartObject is null, otherwise this is offset from StartObject position.")]
    public Vector3 StartPosition;

    [Tooltip("The game object where the lightning will end at. If null, EndPosition is used.")]
    public GameObject EndObject;

    [Tooltip("The end position where the lightning will end at. This is in world space if EndObject is null, otherwise this is offset from EndObject position.")]
    public Vector3 EndPosition;

    [Range(0, 8)]
    [Tooltip("How manu generations? Higher numbers create more line segments.")]
    public int Generations = 6;

    [Range(0.01f, 1.0f)]
    [Tooltip("How long each bolt should last before creating a new bolt. In ManualMode, the bolt will simply disappear after this amount of seconds.")]
    public float Duration = 0.05f;

    [Range(0.0f, 1.0f)]
    [Tooltip("How chaotic should the lightning be? (0-1)")]
    public float ChaosFactor = 0.15f;

    [Tooltip("In manual mode, the trigger method must be called to create a bolt")]
    public bool ManualMode;

    [FormerlySerializedAs("Rows")]
    [Range(1, 64)]
    [Tooltip("The number of rows in the texture. Used for animation.")]
    public int m_rows = 1;

    public int Rows
    {
      get => m_rows;
      set
      {
        m_rows = value;
        UpdateFromMaterialChange();
      }
    }


    [Range(1, 64)]
    [Tooltip("The number of columns in the texture. Used for animation.")]
    public int m_columns = 1;

    public int Columns
    {
      get => m_columns;
      set
      {
        m_columns = value;
        UpdateFromMaterialChange();
      }
    }

    [Tooltip("The animation mode for the lightning")]
    public LightningBoltAnimationMode AnimationMode = LightningBoltAnimationMode.PingPong;
    private readonly List<KeyValuePair<Vector3, Vector3>> segments = new();
    private int animationOffsetIndex;
    private int animationPingPongDirection = 1;

    private LineRenderer lineRenderer;
    private Vector2[] offsets;
    private bool orthographic;

    /// <summary>
    /// Assign your own random if you want to have the same lightning appearance
    /// </summary>
    [NonSerialized]
    public Random RandomGenerator = new();
    private Vector2 size;
    private int startIndex;
    private float timer;

    // distribute or Stretch look best.
    public LineTextureMode textureMode = LineTextureMode.Stretch;

    public void Awake()
    {
      lineRenderer = GetComponent<LineRenderer>();
      if (lineRenderer == null)
      {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
      }
    }

    private void Start()
    {
      orthographic = Camera.main != null && Camera.main.orthographic;
      lineRenderer.textureMode = textureMode;
      lineRenderer.positionCount = 0;
      UpdateFromMaterialChange();
    }

    private void Update()
    {
      var main = Camera.main;
      orthographic = main != null && main.orthographic;
      if (timer <= 0.0f)
      {
        if (ManualMode)
        {
          timer = Duration;
          lineRenderer.positionCount = 0;
        }
        else
        {
          Trigger();
        }
      }
      timer -= Time.deltaTime;
    }

    private void GetPerpendicularVector(ref Vector3 directionNormalized, out Vector3 side)
    {
      if (directionNormalized == Vector3.zero)
      {
        side = Vector3.right;
      }
      else
      {
        // use cross product to find any perpendicular vector around directionNormalized:
        // 0 = x * px + y * py + z * pz
        // => pz = -(x * px + y * py) / z
        // for computational stability use the component farthest from 0 to divide by
        var x = directionNormalized.x;
        var y = directionNormalized.y;
        var z = directionNormalized.z;
        float px, py, pz;
        float ax = Mathf.Abs(x), ay = Mathf.Abs(y), az = Mathf.Abs(z);
        if (ax >= ay && ay >= az)
        {
          // x is the max, so we can pick (py, pz) arbitrarily at (1, 1):
          py = 1.0f;
          pz = 1.0f;
          px = -(y * py + z * pz) / x;
        }
        else if (ay >= az)
        {
          // y is the max, so we can pick (px, pz) arbitrarily at (1, 1):
          px = 1.0f;
          pz = 1.0f;
          py = -(x * px + z * pz) / y;
        }
        else
        {
          // z is the max, so we can pick (px, py) arbitrarily at (1, 1):
          px = 1.0f;
          py = 1.0f;
          pz = -(x * px + y * py) / z;
        }
        side = new Vector3(px, py, pz).normalized;
      }
    }

    private void GenerateLightningBolt(Vector3 start, Vector3 end, int generation, int totalGenerations, float offsetAmount)
    {
      if (generation < 0 || generation > 8)
      {
        return;
      }
      if (orthographic)
      {
        start.z = end.z = Mathf.Min(start.z, end.z);
      }

      segments.Add(new KeyValuePair<Vector3, Vector3>(start, end));
      if (generation == 0)
      {
        return;
      }

      Vector3 randomVector;
      if (offsetAmount <= 0.0f)
      {
        offsetAmount = (end - start).magnitude * ChaosFactor;
      }

      while (generation-- > 0)
      {
        var previousStartIndex = startIndex;
        startIndex = segments.Count;
        for (var i = previousStartIndex; i < startIndex; i++)
        {
          start = segments[i].Key;
          end = segments[i].Value;

          // determine a new direction for the split
          var midPoint = (start + end) * 0.5f;

          // adjust the mid point to be the new location
          RandomVector(ref start, ref end, offsetAmount, out randomVector);
          midPoint += randomVector;

          // add two new segments
          segments.Add(new KeyValuePair<Vector3, Vector3>(start, midPoint));
          segments.Add(new KeyValuePair<Vector3, Vector3>(midPoint, end));
        }

        // halve the distance the lightning can deviate for each generation down
        offsetAmount *= 0.5f;
      }
    }

    public void RandomVector(ref Vector3 start, ref Vector3 end, float offsetAmount, out Vector3 result)
    {
      if (orthographic)
      {
        var directionNormalized = (end - start).normalized;
        var side = new Vector3(-directionNormalized.y, directionNormalized.x, directionNormalized.z);
        var distance = (float)RandomGenerator.NextDouble() * offsetAmount * 2.0f - offsetAmount;
        result = side * distance;
      }
      else
      {
        var directionNormalized = (end - start).normalized;
        Vector3 side;
        GetPerpendicularVector(ref directionNormalized, out side);

        // generate random distance
        var distance = ((float)RandomGenerator.NextDouble() + 0.1f) * offsetAmount;

        // get random rotation angle to rotate around the current direction
        var rotationAngle = (float)RandomGenerator.NextDouble() * 360.0f;

        // rotate around the direction and then offset by the perpendicular vector
        result = Quaternion.AngleAxis(rotationAngle, directionNormalized) * side * distance;
      }
    }

    private void SelectOffsetFromAnimationMode()
    {
      int index;

      if (AnimationMode == LightningBoltAnimationMode.None)
      {
        lineRenderer.material.mainTextureOffset = offsets[0];
        return;
      }
      if (AnimationMode == LightningBoltAnimationMode.PingPong)
      {
        index = animationOffsetIndex;
        animationOffsetIndex += animationPingPongDirection;
        if (animationOffsetIndex >= offsets.Length)
        {
          animationOffsetIndex = offsets.Length - 2;
          animationPingPongDirection = -1;
        }
        else if (animationOffsetIndex < 0)
        {
          animationOffsetIndex = 1;
          animationPingPongDirection = 1;
        }
      }
      else if (AnimationMode == LightningBoltAnimationMode.Loop)
      {
        index = animationOffsetIndex++;
        if (animationOffsetIndex >= offsets.Length)
        {
          animationOffsetIndex = 0;
        }
      }
      else
      {
        index = RandomGenerator.Next(0, offsets.Length);
      }

      if (index >= 0 && index < offsets.Length)
      {
        lineRenderer.material.mainTextureOffset = offsets[index];
      }
      else
      {
        lineRenderer.material.mainTextureOffset = offsets[0];
      }
    }

    public void UpdateLightningSize(float val)
    {
      if (lineRenderer == null) return;
      lineRenderer.startWidth = val * RandomGenerator.Next(1, 10) / 5;
      lineRenderer.endWidth = val * RandomGenerator.Next(1, 10) / 5;
    }

    private void UpdateLineRenderer()
    {
      var segmentCount = segments.Count - startIndex + 1;
      lineRenderer.positionCount = segmentCount;

      if (segmentCount < 1 || segments.Count == 0)
      {
        return;
      }

      var index = 0;
      lineRenderer.SetPosition(index++, segments[startIndex].Key);

      for (var i = startIndex; i < segments.Count; i++)
      {
        lineRenderer.SetPosition(index++, segments[i].Value);
      }

      segments.Clear();

      SelectOffsetFromAnimationMode();
    }

    public void HideLightning()
    {
      if (lineRenderer == null) return;
      lineRenderer.positionCount = 0;
      enabled = false;
    }

    public void ShowLightning()
    {
      enabled = true;
    }

    public void ToggleLightning(bool val)
    {
      if (!val)
      {
        lineRenderer.positionCount = 0;
      }
      enabled = val;
    }

    /// <summary>
    /// Trigger a lightning bolt. Use this if ManualMode is true.
    /// </summary>
    public void Trigger()
    {
      Vector3 start, end;
      timer = Duration + Mathf.Min(0.0f, timer);
      if (StartObject == null)
      {
        start = StartPosition;
      }
      else
      {
        start = StartObject.transform.position + StartPosition;
      }
      if (EndObject == null)
      {
        end = EndPosition;
      }
      else
      {
        end = EndObject.transform.position + EndPosition;
      }
      startIndex = 0;
      GenerateLightningBolt(start, end, Generations, Generations, 0.0f);
      UpdateLineRenderer();
    }

    /// <summary>
    /// Call this method if you change the material on the line renderer
    /// </summary>
    public void UpdateFromMaterialChange()
    {
      if (lineRenderer == null) return;
      if (lineRenderer.material == null) return;

      lineRenderer.textureMode = textureMode;

      size = new Vector2(1.0f / Columns, 1.0f / m_rows);
      lineRenderer.material.mainTextureScale = size;
      offsets = new Vector2[m_rows * Columns];
      for (var y = 0; y < m_rows; y++)
      {
        for (var x = 0; x < Columns; x++)
        {
          offsets[x + y * Columns] = new Vector2((float)x / Columns, (float)y / m_rows);
        }
      }
    }
  }
}