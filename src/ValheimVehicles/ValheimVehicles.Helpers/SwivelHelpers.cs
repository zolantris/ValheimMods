using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Helpers;

public static class SwivelHelpers
{
  public static RaycastHit[] m_raycasthits = new RaycastHit[20];

  /// <summary>
  /// This is much more performant than a raycast as we only look for Instances registered.
  /// </summary>
  public static bool FindAllSwivelsWithinRange(Vector3 currentPosition, out List<SwivelComponent> swivelComponents)
  {
    swivelComponents = SwivelComponent.Instances
      .Where(x => x && x.swivelPowerConsumer)
      .Select(x => new { Component = x, Distance = Vector3.Distance(currentPosition, x.transform.position) })
      .Where(x => x.Distance <= 50f)
      .OrderBy(x => x.Distance)
      .Select(x => x.Component)
      .ToList();
    return swivelComponents.Count > 0;
  }

  public static bool FindAllSwivelsWithinRange(Vector3 currentPosition, out List<SwivelComponent> swivelComponents, [NotNullWhen(true)] out SwivelComponent? closestSwivelComponent)
  {
    closestSwivelComponent = null;
    swivelComponents = SwivelComponent.Instances
      .Where(x => x && x.swivelPowerConsumer)
      .Select(x => new { Component = x, Distance = Vector3.Distance(currentPosition, x.transform.position) })
      .Where(x => x.Distance <= 50f)
      .OrderBy(x => x.Distance)
      .Select(x => x.Component)
      .ToList();

    if (swivelComponents.Count > 0)
    {
      closestSwivelComponent = swivelComponents[0];
    }
    return swivelComponents.Count > 0;
  }

  // public static bool FindNearestSwivel(Transform transform, [NotNullWhen(true)] out SwivelComponent swivelComponentIntegration)
  // {
  //   swivelComponentIntegration = transform.GetComponentInParent<SwivelComponent>();
  //   if (swivelComponentIntegration)
  //   {
  //     return true;
  //   }
  //
  //   var num = Physics.SphereCastNonAlloc(transform.position, 5f, Vector3.up, m_raycasthits, 100f, LayerMask.GetMask("piece"));
  //   for (var i = 0; i < num; i++)
  //   {
  //     var raycastHit = m_raycasthits[i];
  //     swivelComponentIntegration = raycastHit.collider.GetComponentInParent<SwivelComponent>();
  //     if (swivelComponentIntegration)
  //     {
  //       return true;
  //     }
  //   }
  //   return false;
  // }
}