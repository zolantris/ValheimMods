// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    }

    public void RebuildPylonNetwork()
    {
      _pylons.Clear();
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

        foreach (var node in PowerNodeComponentBase.Instances)
        {
          node.SetNetworkId(networkId);

          if (_chain.Any(p => Vector3.Distance(node.Position, p.Position) <= p.MaxConnectionDistance))
          {
            RegisterNode(node);
            var closest = _chain.OrderBy(p => Vector3.Distance(p.Position, node.Position)).FirstOrDefault();
            if (closest != null)
            {
              GenerateNodeLinkLine(node, closest);
            }
          }
        }

        if (_chain.Count >= 2)
        {
          GenerateChainLine(_chain);
        }
      }
    }

    private void GenerateChainLine(List<PowerPylon> chain)
    {
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

      for (int i = 0; i < chain.Count - 1; i++)
      {
        var start = parent.InverseTransformPoint(chain[i].wireConnector.position);
        var end = parent.InverseTransformPoint(chain[i + 1].wireConnector.position);

        for (int j = 0; j < curvedLinePoints; j++)
        {
          float t = j / (float)(curvedLinePoints - 1);
          var point = Vector3.Lerp(start, end, t);
          point += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.2f;
          curvedPoints.Add(point);
        }
      }

      line.positionCount = curvedPoints.Count;
      line.SetPositions(curvedPoints.ToArray());
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
      float deltaTime = Time.fixedDeltaTime;
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

      float totalDemand = 0f;
      foreach (var c in _consumers)
        totalDemand += c.RequestedPower(deltaTime);

      float fromSources = 0f;
      foreach (var s in _sources)
        fromSources += s.RequestAvailablePower(deltaTime);

      float remaining = totalDemand - fromSources;

      float fromStorage = 0f;
      if (remaining > 0f)
      {
        float safeMargin = Mathf.Max(0.01f, totalDemand * 0.01f);
        foreach (var b in _storage)
          fromStorage += b.Discharge(remaining + safeMargin);
      }

      float totalAvailable = fromSources + fromStorage;

      foreach (var c in _consumers)
      {
        float required = c.RequestedPower(deltaTime);
        float granted = Mathf.Min(required, totalAvailable);
        totalAvailable -= granted;

        c.SetActive(granted >= required);
        c.ApplyPower(granted, deltaTime);
      }

      foreach (var b in _storage)
      {
        if (totalAvailable <= 0f) break;
        totalAvailable -= b.Charge(totalAvailable);
      }
    }
  }
}
