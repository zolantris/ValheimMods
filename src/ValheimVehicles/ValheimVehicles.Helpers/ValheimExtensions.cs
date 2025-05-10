using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Interfaces;
namespace ValheimVehicles.Helpers;

public static class ValheimExtensions
{
  // non-extension method
  public static bool IsNetViewValid(ZNetView? netView, [NotNullWhen(true)] out ZNetView? validNetView)
  {
    validNetView = null;
    if (netView == null || netView.GetZDO() == null || !netView.IsValid()) return false;
    validNetView = netView;
    return true;
  }

  public static bool TryGetHoverableParent(GameObject currentGo, [NotNullWhen(true)] out GameObject? go)
  {
    go = null;
    var hoverableRoot = currentGo.GetComponentInParent<IHoverableObj>();
    if (hoverableRoot != null)
    {
      go = hoverableRoot.gameObject;
      return true;
    }
    return false;
  }

  /// <summary>
  /// INetView extensions.
  /// </summary>
  /// <param name="instance"></param>
  /// <param name="validNetView"></param>
  /// <returns></returns>
  public static bool IsNetViewValid(this INetView instance, [NotNullWhen(true)] out ZNetView? validNetView)
  {
    validNetView = null;
    if (instance.m_nview == null || instance.m_nview.GetZDO() == null || !instance.m_nview.IsValid()) return false;
    validNetView = instance.m_nview;
    return true;
  }

  public static bool IsNetViewValid(this INetView instance)
  {
    if (instance.m_nview != null && instance.m_nview.GetZDO() != null && instance.m_nview.IsValid())
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


  /// <summary>
  /// Similar to 
  /// </summary>
  /// <param name="nv"></param>
  /// <returns></returns>
  public static bool IsNetViewValid(this ZNetView nv)
  {
    if (nv == null || nv.GetZDO() == null || !nv.IsValid()) return false;
    return true;
  }
}