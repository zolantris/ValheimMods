// PowerNetworkController.Lightning
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController
  {
    [SerializeField] private bool enableLightning = true;
    [SerializeField] private float lightningCycleTime = 10f;
    [SerializeField] private float lightningDuration = 3f;

    private float _lightningTimer;
    private bool _lightningActive;
    private Coroutine? lightningBurstCoroutine;

    protected virtual void Update()
    {
      if (!enableLightning || _lightningActive) return;

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

      foreach (var pylon in Pylons)
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
  }
}