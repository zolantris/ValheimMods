// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class ElectricitySparkManager : MonoBehaviour
  {
    [Header("Timing Settings")]
    public float lightningCycleTime = 10f;
    public float lightningDuration = 3f;
    private bool _lightningActive;

    private float _lightningTimer;

    private void Update()
    {
      if (_lightningActive) return;

      _lightningTimer += Time.deltaTime;
      if (_lightningTimer >= lightningCycleTime)
      {
        _lightningTimer = 0f;
        StartCoroutine(ActivateLightningBursts());
      }
    }

    private IEnumerator ActivateLightningBursts()
    {
      _lightningActive = true;

      foreach (var pylon in PowerPylonRegistry.All)
      {
        if (pylon == null || pylon.lightningBolt == null || pylon.coilTop == null) continue;

        var target = GetClosestPylonWire(pylon);
        if (target != null)
        {
          pylon.UpdateCoilPosition(pylon.coilTop.gameObject, target.gameObject);
        }
      }

      yield return new WaitForSeconds(lightningDuration);

      foreach (var pylon in PowerPylonRegistry.All)
      {
        if (pylon == null || pylon.lightningBolt == null) continue;
        pylon.UpdateCoilPosition(pylon.coilTop.gameObject, pylon.coilBottom.gameObject);
      }

      _lightningActive = false;
    }

    private Transform? GetClosestPylonWire(PowerPylon origin)
    {
      Transform? closest = null;
      var closestDist = float.MaxValue;

      foreach (var other in PowerPylonRegistry.All)
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