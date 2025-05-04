using System.Diagnostics.CodeAnalysis;
using ValheimVehicles.Interfaces;
namespace ValheimVehicles.Helpers;

public static class ValheimClassValidation
{
  /// <summary>
  /// INetView extensions.
  /// </summary>
  /// <param name="instance"></param>
  /// <param name="validNetView"></param>
  /// <returns></returns>
  public static bool IsNetViewValid(this INetView instance, [NotNullWhen(true)] out ZNetView? validNetView)
  {
    validNetView = instance.m_nview;
    if (validNetView != null && validNetView.m_zdo != null && validNetView.IsValid())
    {
      return true;
    }
    return false;
  }

  public static bool IsCurrentGameHealthy()
  {
    if (ZNet.instance == null || ZNetScene.instance == null)
    {
      return false;
    }
    return true;
  }
}