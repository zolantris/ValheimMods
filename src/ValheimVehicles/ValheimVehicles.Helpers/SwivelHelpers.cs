using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Integrations;
namespace ValheimVehicles.Helpers;

public static class SwivelHelpers
{
  public static RaycastHit[] m_raycasthits = new RaycastHit[20];

  public static bool FindNearestSwivel(Transform transform, [NotNullWhen(true)] out SwivelComponentIntegration swivelComponentIntegration)
  {
    swivelComponentIntegration = transform.GetComponentInParent<SwivelComponentIntegration>();
    if (swivelComponentIntegration)
    {
      return true;
    }

    var num = Physics.SphereCastNonAlloc(transform.position, 5f, Vector3.up, m_raycasthits, 100f, LayerMask.GetMask("piece"));
    for (var i = 0; i < num; i++)
    {
      var raycastHit = m_raycasthits[i];
      swivelComponentIntegration = raycastHit.collider.GetComponentInParent<SwivelComponentIntegration>();
      if (swivelComponentIntegration)
      {
        return true;
      }
    }
    return false;
  }
}