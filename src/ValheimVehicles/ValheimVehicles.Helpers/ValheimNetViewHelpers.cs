using System;
namespace ValheimVehicles.Helpers;

public class ValheimNetViewHelpers
{
  public static void RunIfValidOwnerOrServer(ZNetView? nview, Action callback)
  {
    if (nview == null || !nview.IsValid()) return;

    //nview.IsOwner() || 
    if (ZNet.instance.IsServer())
    {
      if (!nview.IsOwner())
        nview.ClaimOwnership();

      callback();
    }
  }

  public static void UpdateZDOFloat(ZNetView? nview, string key, float value)
  {
    if (nview?.IsValid() == true && (nview.IsOwner() || ZNet.instance.IsServer()))
      nview.GetZDO()?.Set(key, value);
  }

  public static float LoadZDOFloat(ZNetView? nview, string key, float fallback = 0f)
  {
    return nview?.IsValid() == true
      ? nview.GetZDO()?.GetFloat(key, fallback) ?? fallback
      : fallback;
  }
}