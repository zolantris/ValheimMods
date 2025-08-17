using UnityEngine;
namespace Eldritch.Core
{

  public class ValheimZoneSystemStub
  {
    public static ValheimZoneSystemStub instance = new();

    public bool GetGroundHeight(Vector3 p, out float height)
    {
      height = -9999f;
      var hasHit = Physics.Raycast(p + Vector3.up * 500f, Vector3.down, out var hit, 5000f, LayerMask.GetMask("terrain"));
      if (!hasHit) return false;
      height = hit.point.y;
      return true;
    }
  }
}