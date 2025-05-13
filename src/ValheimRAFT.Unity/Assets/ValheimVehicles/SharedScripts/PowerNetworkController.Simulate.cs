// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController
  {
    public void SimulateNetwork(List<IPowerNode> nodes)
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
  }
}