using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
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
  /// For running on server or owner. This works much better for dedicated servers.
  /// </summary>
  public static void RunIfOwnerOrServerOrNoOwner(this INetView instance, Action<ZNetView> action)
  {
    if (!instance.IsNetViewValid(out var netView)) return;
    if (netView.IsOwner() || ZNet.instance.IsServer() || !netView.HasOwner())
    {
      if (!netView.IsOwner())
        netView.ClaimOwnership();

      action.Invoke(netView);
    }
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

  public static Coroutine WaitForZNetView(this MonoBehaviour instance, Action action)
  {
    return instance.StartCoroutine(WaitForZNetViewCoroutine(instance, _ => action()));
  }

  public static Coroutine WaitForZNetView(this MonoBehaviour instance, Action<ZNetView> action)
  {
    return instance.StartCoroutine(WaitForZNetViewCoroutine(instance, action));
  }

  private static IEnumerator WaitForZNetViewCoroutine(MonoBehaviour instance, Action<ZNetView> action, float timeout = 10f)
  {
    var startTime = Time.realtimeSinceStartup;
    var netView = instance.GetComponent<ZNetView>();

    while (instance && instance.isActiveAndEnabled && (!netView || !netView.IsValid()))
    {
      if (Time.realtimeSinceStartup - startTime > timeout)
      {
        LoggerProvider.LogInfoDebounced(
          $"znet_timeout_{instance.GetType().Name}",
          $"[ZNetViewUtil] ZNetView not valid on {instance.GetType().Name} after {timeout}s at {instance.transform.position}"
        );
        yield break;
      }

      yield return null;
      netView = instance.GetComponent<ZNetView>();
    }

    if (!instance)
    {
      LoggerProvider.LogInfoDebounced("Bailed due to instance becoming invalid.");
      yield break;
    }
    LoggerProvider.LogInfoDebounced(
      $"znet_register_{instance.GetType().Name}",
      $"ZNetView ready, registering {instance.GetType().Name} on '{instance.name}' at {instance.transform.position}"
    );

    action.Invoke(netView);
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