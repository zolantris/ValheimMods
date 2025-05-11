// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class PowerNetworkManager : SingletonBehaviour<PowerNetworkManager>
  {
    private static readonly int MaxPoints = 50;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private static Material _materialInstance;

    [Header("Wire Configuration")]
    public Material wireMaterial;
    public float maxConnectionDistance = 50f;
    public bool debugRefreshEveryFixedUpdate;
    private readonly List<LineRenderer> _activeLines = new();
    private readonly WaitForSeconds _refreshDelay = new(0.05f);
    private Coroutine _delayedRefreshRoutine;
    private ElectricitySparkManager _sparkManager;

    private void FixedUpdate()
    {
      if (debugRefreshEveryFixedUpdate)
      {
        ScheduleRefresh();
      }
    }

    private void OnDestroy()
    {
      PowerPylonRegistry.OnPylonListChanged -= ScheduleRefresh;
    }

    public override void OnAwake()
    {
      _sparkManager = gameObject.AddComponent<ElectricitySparkManager>();
      PowerPylonRegistry.OnPylonListChanged += ScheduleRefresh;
      ScheduleRefresh();
    }

    public void ScheduleRefresh()
    {
      if (_delayedRefreshRoutine != null) return;
      if (!this || !gameObject || !Application.isPlaying) return;

      _delayedRefreshRoutine = StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
      yield return _refreshDelay;

      if (!this || !gameObject || !Application.isPlaying) yield break;

      _delayedRefreshRoutine = null;
      RefreshConnections();
    }

    private void RefreshConnections()
    {
      foreach (var line in _activeLines)
      {
        if (line != null) Destroy(line.gameObject);
      }
      _activeLines.Clear();

      var allPylons = PowerPylonRegistry.All;
      var unvisited = new HashSet<PowerPylon>(allPylons.Where(p => p && p.wireConnector));

      while (unvisited.Count > 0)
      {
        var chain = new List<PowerPylon>();
        var pending = new Queue<PowerPylon>();

        var start = unvisited.First();
        pending.Enqueue(start);
        unvisited.Remove(start);

        while (pending.Count > 0)
        {
          var current = pending.Dequeue();
          chain.Add(current);

          foreach (var neighbor in unvisited.ToList())
          {
            if (neighbor == null || neighbor.wireConnector == null) continue;

            var distance = Vector3.Distance(current.wireConnector.position, neighbor.wireConnector.position);
            if (distance <= maxConnectionDistance)
            {
              pending.Enqueue(neighbor);
              unvisited.Remove(neighbor);
            }
          }
        }

        if (chain.Count >= 2)
        {
          GenerateChainLine(chain);
        }
      }
    }

    private void GenerateChainLine(List<PowerPylon> chain)
    {
      // Use the first pylon's root (e.g., rigidbody parent) as anchor
      var parent = chain[0].transform.root;

      var obj = new GameObject("PylonChainConnector");
      obj.transform.SetParent(parent, false);
      obj.transform.localPosition = Vector3.zero;

      var line = obj.AddComponent<LineRenderer>();
      _activeLines.Add(line);

      line.material = GetWireMaterial();
      line.widthMultiplier = 0.02f;
      line.textureMode = LineTextureMode.Tile;
      line.useWorldSpace = false;

      var curvedPoints = new List<Vector3>();

      for (var i = 0; i < chain.Count - 1; i++)
      {
        var start = parent.InverseTransformPoint(chain[i].wireConnector.position);
        var end = parent.InverseTransformPoint(chain[i + 1].wireConnector.position);

        for (var j = 0; j < MaxPoints; j++)
        {
          var t = j / (float)(MaxPoints - 1);
          var point = Vector3.Lerp(start, end, t);
          point += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.2f;
          curvedPoints.Add(point);
        }
      }

      line.positionCount = curvedPoints.Count;
      line.SetPositions(curvedPoints.ToArray());
    }


    private Material GetWireMaterial()
    {
      if (wireMaterial != null) return wireMaterial;

      if (_materialInstance == null)
      {
        _materialInstance = new Material(Shader.Find("Sprites/Default"))
        {
          color = Color.black
        };
        _materialInstance.EnableKeyword("_EMISSION");
        _materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        _materialInstance.SetColor(EmissionColor, Color.black * 1.5f);
      }

      return _materialInstance;
    }
  }
}