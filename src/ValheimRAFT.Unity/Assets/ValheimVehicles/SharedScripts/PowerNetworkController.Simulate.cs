// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController
  {
    private readonly List<IPowerSource> _sources = new();
    private readonly List<IPowerStorage> _storage = new();
    private readonly List<IPowerConsumer> _consumers = new();
    private readonly List<IPowerConduit> _conduits = new();

    public void SimulateNetwork(List<IPowerNode> nodes)
    {
      var deltaTime = Time.fixedDeltaTime;
      _sources.Clear();
      _storage.Clear();
      _consumers.Clear();
      _conduits.Clear();

      foreach (var node in nodes)
      {
        switch (node)
        {
          case IPowerSource s:
            _sources.Add(s);
            break;
          case IPowerStorage b:
            _storage.Add(b);
            break;
          case IPowerConsumer c:
            if (c.IsDemanding)
              _consumers.Add(c);
            break;
          case IPowerConduit conduit:
            _conduits.Add(conduit);
            break;
        }
      }

      var totalDemand = 0f;
      foreach (var c in _consumers)
        totalDemand += c.RequestedPower(deltaTime);

      // conduits can both request or discharge power
      foreach (var conduit in _conduits)
      {
        if (conduit.IsDemanding)
          totalDemand += conduit.RequestPower(deltaTime);
      }

      var networkIsDemanding = _consumers.Any(c => c.IsDemanding) || _storage.Any(s => s.CapacityRemaining > 0f);

      var fromStorage = 0f;
      var remainingDemand = totalDemand;

      foreach (var conduit in _conduits)
      {
        if (remainingDemand <= 0f) break;
        var supplied = conduit.SupplyPower(deltaTime);
        fromStorage += supplied;
        remainingDemand -= supplied;
      }

      foreach (var b in _storage)
      {
        if (remainingDemand <= 0f) break;
        var supplied = b.Discharge(remainingDemand);
        fromStorage += supplied;
        remainingDemand -= supplied;
      }

      var remaining = totalDemand - fromStorage;

      var fromSources = 0f;
      if (remaining > 0f)
      {
        foreach (var s in _sources)
        {
          fromSources += s.RequestAvailablePower(deltaTime, fromStorage, totalDemand, networkIsDemanding);
          if (fromStorage + fromSources >= totalDemand) break;
        }
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


      // lightning effects.

      if (lightningBurstCoroutine != null && !networkIsDemanding)
      {
        StopCoroutine(lightningBurstCoroutine);
        lightningBurstCoroutine = null;
      }

      if (networkIsDemanding)
      {
        if (lightningBurstCoroutine != null)
        {
          StopCoroutine(lightningBurstCoroutine);
          lightningBurstCoroutine = null;
        }
        lightningBurstCoroutine = StartCoroutine(ActivateLightningBursts());
      }
    }
  }
}