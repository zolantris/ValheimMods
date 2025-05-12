// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerNetworkController : SingletonBehaviour<PowerNetworkController>
  {
    private static readonly List<PowerSourceComponent> _sources = new();
    private static readonly List<PowerStorageComponent> _storage = new();
    private static readonly List<PowerConsumerComponent> _consumers = new();
    private static readonly List<PowerPylon> _pylons = new();
    private static readonly Queue<PowerPylon> _pending = new();
    private static readonly List<PowerPylon> _chain = new();
    private static readonly HashSet<PowerPylon> _unvisited = new();

    [SerializeField] private int curvedLinePoints = 50;
    [SerializeField] private Material fallbackWireMaterial;
    private readonly List<LineRenderer> _activeLines = new();
    private readonly Dictionary<string, List<IPowerNode>> _networks = new();

    private readonly float _updateInterval = 0.25f;
    private float _nextUpdate;

    public Coroutine? _rebuildPylonNetworkRoutine;
    public static Material WireMaterial { get; set; }


    public override void Awake()
    {
#if UNITY_EDITOR
      if (WireMaterial == null)
      {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader)
        {
          WireMaterial = new Material(shader)
          {
            color = Color.black
          };
          WireMaterial.EnableKeyword("_EMISSION");
          WireMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
          WireMaterial.SetColor("_EmissionColor", Color.black * 1.5f);
        }
        else
        {
          Debug.LogWarning("Default wire shader not found. WireMaterial will be pink.");
        }
      }
      if (WireMaterial == null && fallbackWireMaterial != null)
        WireMaterial = fallbackWireMaterial;
#endif
      base.Awake();
    }

    public void Start()
    {
      RequestRebuildPylonNetwork();
    }

    private void FixedUpdate()
    {
      if (Time.time < _nextUpdate) return;
      _nextUpdate = Time.time + _updateInterval;

      foreach (var pair in _networks)
      {
        SimulateNetwork(pair.Value);
      }
    }

    public void RegisterNode(IPowerNode node)
    {
      if (!_networks.TryGetValue(node.NetworkId, out var list))
      {
        list = new List<IPowerNode>();
        _networks[node.NetworkId] = list;
      }

      if (!list.Contains(node))
        list.Add(node);

      RequestRebuildPylonNetwork();
    }
    public Stopwatch rebuildTimer = new();
    public IEnumerator RebuildPylonNetworkCoroutine()
    {
      yield return new WaitForSeconds(1f);
      _pylons.Clear();
      rebuildTimer.Restart();
      foreach (var p in PowerPylonRegistry.All)
      {
        if (p != null) _pylons.Add(p);
      }

      foreach (var line in _activeLines)
      {
        if (line != null) Destroy(line.gameObject);
      }
      _activeLines.Clear();
      _networks.Clear();

      _unvisited.Clear();
      foreach (var pylon in _pylons)
        _unvisited.Add(pylon);

      while (_unvisited.Count > 0)
      {
        if (rebuildTimer.ElapsedMilliseconds > 10)
        {
          rebuildTimer.Restart();
          yield return null;
        }

        _pending.Clear();
        _chain.Clear();

        var start = default(PowerPylon);
        foreach (var item in _unvisited)
        {
          start = item;
          break;
        }
        if (start == null) break;

        _pending.Enqueue(start);
        _unvisited.Remove(start);

        while (_pending.Count > 0)
        {
          if (rebuildTimer.ElapsedMilliseconds > 10)
          {
            rebuildTimer.Restart();
            yield return null;
          }
          var current = _pending.Dequeue();
          _chain.Add(current);

          foreach (var neighbor in _unvisited)
          {
            if (Vector3.Distance(current.wireConnector.position, neighbor.wireConnector.position) <= current.MaxConnectionDistance)
            {
              _pending.Enqueue(neighbor);
              _unvisited.Remove(neighbor);
              break;
            }
          }
        }

        var networkId = Guid.NewGuid().ToString();
        foreach (var pylon in _chain)
        {
          pylon.SetNetworkId(networkId);
          RegisterNode(pylon);
        }

        var nodeLinkMap = new Dictionary<PowerPylon, List<IPowerNode>>();

        foreach (var node in PowerNodeComponentBase.Instances)
        {
          node.SetNetworkId(networkId);

          var closest = _chain.OrderBy(p => Vector3.Distance(p.Position, node.Position)).FirstOrDefault();
          if (closest != null && Vector3.Distance(node.Position, closest.Position) <= closest.MaxConnectionDistance)
          {
            RegisterNode(node);

            if (!nodeLinkMap.TryGetValue(closest, out var list))
              nodeLinkMap[closest] = list = new List<IPowerNode>();

            list.Add(node);
          }
        }

        if (_chain.Count >= 2)
        {
          GenerateChainLine(_chain, nodeLinkMap);
        }
      }
      rebuildTimer.Reset();
      yield return new WaitForSeconds(1f);
      _rebuildPylonNetworkRoutine = null;
    }

    public void RequestRebuildPylonNetwork()
    {
      if (_rebuildPylonNetworkRoutine != null) { return; }
      _rebuildPylonNetworkRoutine = StartCoroutine(RebuildPylonNetworkCoroutine());
    }

    // private void GenerateChainLine(List<PowerPylon> chain)
    // {
    //   var parent = chain[0].transform.root;
    //   var obj = new GameObject("PylonChainConnector");
    //   obj.transform.SetParent(parent, false);
    //
    //   var line = obj.AddComponent<LineRenderer>();
    //   _activeLines.Add(line);
    //
    //   line.material = WireMaterial;
    //   line.widthMultiplier = 0.02f;
    //   line.textureMode = LineTextureMode.Tile;
    //   line.useWorldSpace = false;
    //
    //   var curvedPoints = new List<Vector3>();
    //
    //   for (var i = 0; i < chain.Count - 1; i++)
    //   {
    //     var start = parent.InverseTransformPoint(chain[i].wireConnector.position);
    //     var end = parent.InverseTransformPoint(chain[i + 1].wireConnector.position);
    //
    //     for (var j = 0; j < curvedLinePoints; j++)
    //     {
    //       var t = j / (float)(curvedLinePoints - 1);
    //       var point = Vector3.Lerp(start, end, t);
    //       point += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.2f;
    //       curvedPoints.Add(point);
    //     }
    //   }
    //
    //   line.positionCount = curvedPoints.Count;
    //   line.SetPositions(curvedPoints.ToArray());
    // }
    private void GenerateChainLine(List<PowerPylon> chain, Dictionary<PowerPylon, List<IPowerNode>> nodeLinks)
    {
      if (chain == null || chain.Count < 2) return;

      var parent = chain[0].transform.root;
      var obj = new GameObject("PylonChainConnector");
      obj.transform.SetParent(parent, false);

      var line = obj.AddComponent<LineRenderer>();
      _activeLines.Add(line);

      line.material = WireMaterial;
      line.widthMultiplier = 0.02f;
      line.textureMode = LineTextureMode.Tile;
      line.useWorldSpace = false;

      var curvedPoints = new List<Vector3>();

      // === Chain: pylon-to-pylon segments ===
      for (var i = 0; i < chain.Count - 1; i++)
      {
        var start = parent.InverseTransformPoint(chain[i].wireConnector.position);
        var end = parent.InverseTransformPoint(chain[i + 1].wireConnector.position);

        AddCurvedPointsBetween(start, end, curvedPoints);
      }

      // === Chain: node-to-pylon links ===
      foreach (var kvp in nodeLinks)
      {
        var pylon = kvp.Key;
        if (pylon == null || !nodeLinks.TryGetValue(pylon, out var linkedNodes)) continue;

        foreach (var node in linkedNodes)
        {
          if (node == null) continue;

          var start = parent.InverseTransformPoint(pylon.wireConnector.position);
          var end = parent.InverseTransformPoint(
            node.ConnectorPoint != null ? node.ConnectorPoint.position : node.Position
          );

          AddCurvedPointsBetween(start, end, curvedPoints);
        }
      }

      line.positionCount = curvedPoints.Count;
      line.SetPositions(curvedPoints.ToArray());
    }

    private void AddCurvedPointsBetween(Vector3 start, Vector3 end, List<Vector3> curvedPoints)
    {
      for (var j = 0; j < curvedLinePoints; j++)
      {
        var t = j / (float)(curvedLinePoints - 1);
        var point = Vector3.Lerp(start, end, t);
        point += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.2f;
        curvedPoints.Add(point);
      }
    }

    private void GenerateNodeLinkLine(IPowerNode from, PowerPylon to)
    {
      var obj = new GameObject($"NodeToPylon_{from.GetHashCode()}");
      obj.transform.SetParent(transform, false);

      var line = obj.AddComponent<LineRenderer>();
      _activeLines.Add(line);

      line.material = WireMaterial;
      line.widthMultiplier = 0.015f;
      line.textureMode = LineTextureMode.Tile;
      line.useWorldSpace = true;

      line.positionCount = 2;
      var fromPoint = from.ConnectorPoint;
      line.SetPosition(0, fromPoint ? fromPoint.position : from.Position);
      line.SetPosition(1, to.wireConnector ? to.wireConnector.position : to.Position);
    }

    private void SimulateNetwork(List<IPowerNode> nodes)
    {
      var deltaTime = Time.fixedDeltaTime;
      _sources.Clear();
      _storage.Clear();
      _consumers.Clear();

      foreach (var node in nodes)
      {
        switch (node)
        {
          case PowerSourceComponent s:
            _sources.Add(s);
            break;
          case PowerStorageComponent b:
            _storage.Add(b);
            break;
          case PowerConsumerComponent c:
            if (c.IsDemanding)
              _consumers.Add(c);
            break;
        }
      }

      var totalDemand = 0f;
      foreach (var c in _consumers)
        totalDemand += c.RequestedPower(deltaTime);

      var networkIsDemanding = _consumers.Any(c => c.IsDemanding) || _storage.Any(s => s.CapacityRemaining > 0f);

      // TODO when all pylons are on all PowerNodes it will look much more realistic. We might be able to remove power lines.
      if (lightningBurstCoroutine != null)
      {
        StopCoroutine(lightningBurstCoroutine);
      }

      lightningBurstCoroutine = StartCoroutine(ActivateLightningBursts());

      var fromSources = 0f;
      foreach (var s in _sources)
        fromSources += s.RequestAvailablePower(deltaTime, networkIsDemanding);

      var remaining = totalDemand - fromSources;

      var fromStorage = 0f;
      if (remaining > 0f)
      {
        var safeMargin = Mathf.Max(0.01f, totalDemand * 0.01f);
        foreach (var b in _storage)
          fromStorage += b.Discharge(remaining + safeMargin);
      }

      var totalAvailable = fromSources + fromStorage;

      if (totalAvailable <= 0f)
      {
        foreach (var c in _consumers)
        {
          c.SetActive(false);
          c.ApplyPower(0f, deltaTime);
        }
        return;
      }

      foreach (var c in _consumers)
      {
        var required = c.RequestedPower(deltaTime);
        var granted = Mathf.Min(required, totalAvailable);
        totalAvailable -= granted;

        c.SetActive(granted > 0f);
        c.ApplyPower(granted, deltaTime);
      }

      foreach (var b in _storage)
      {
        if (totalAvailable <= 0f) break;
        totalAvailable -= b.Charge(totalAvailable);
      }
    }


  #region Lightning Spark Management

    [SerializeField] private float lightningCycleTime = 10f;
    [SerializeField] private float lightningDuration = 3f;

    private float _lightningTimer;
    private bool _lightningActive;
    private Coroutine? lightningBurstCoroutine = null;

    private void Update()
    {
      if (_lightningActive) return;

      _lightningTimer += Time.deltaTime;

      if (_lightningTimer >= lightningCycleTime && lightningBurstCoroutine == null && HasPoweredNetworks())
      {
        _lightningTimer = 0f;
        lightningBurstCoroutine = StartCoroutine(ActivateLightningBursts());
      }
    }

    private bool HasPoweredNetworks()
    {
      return _networks.Values.Any(group => group.OfType<PowerConsumerComponent>().Any(c => c.IsActive));
    }

    private IEnumerator ActivateLightningBursts()
    {
      _lightningActive = true;

      foreach (var kvp in _networks)
      {
        var network = kvp.Value;
        var pylons = network.OfType<PowerPylon>().ToList();
        var consumers = network.OfType<PowerConsumerComponent>().ToList();

        // Skip if no pylons or if no consumers are active
        if (pylons.Count < 2 || consumers.All(c => !c.IsActive)) continue;

        foreach (var origin in pylons)
        {
          if (origin == null || origin.lightningBolt == null || origin.coilTop == null) continue;

          var target = GetClosestPylonWire(origin, pylons);
          if (target != null)
          {
            origin.UpdateCoilPosition(origin.coilTop.gameObject, target.gameObject);
          }
        }
      }

      yield return new WaitForSeconds(lightningDuration);

      foreach (var pylon in PowerPylonRegistry.All)
      {
        if (pylon == null || pylon.lightningBolt == null) continue;
        pylon.UpdateCoilPosition(pylon.coilTop.gameObject, pylon.coilBottom.gameObject);
      }

      _lightningActive = false;
      lightningBurstCoroutine = null;
    }


    private Transform? GetClosestPylonWire(PowerPylon origin, List<PowerPylon> networkPylons)
    {
      Transform? closest = null;
      var closestDist = float.MaxValue;

      foreach (var other in networkPylons)
      {
        if (other == null || other == origin || other.wireConnector == null) continue;

        var dist = Vector3.Distance(origin.wireConnector.position, other.wireConnector.position);
        if (dist < closestDist)
        {
          closestDist = dist;
          closest = other.wireConnector;
        }
      }

      return closest;
    }

  #endregion

  }
}