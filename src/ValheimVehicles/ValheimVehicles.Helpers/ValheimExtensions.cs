using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using Logger = HarmonyLib.Tools.Logger;
namespace ValheimVehicles.Helpers;

public static class ValheimExtensions
{

  public static void TrySetZDOOnChange<T>(this ZDO zdo, string key, T currentValue)
  {
    if (typeof(T) == typeof(float))
    {
      var storedValue = zdo.GetFloat(key);
      var currentValueAsFloat = (float)((object)currentValue ?? storedValue);
      if (!Mathf.Approximately(storedValue, currentValueAsFloat))
      {
        zdo.Set(key, currentValueAsFloat);
      }
    }
  }

  public static void TrySetZDOBoolOnChange(this ZDO zdo, string key, bool currentValue)
  {
    var storedValue = zdo.GetBool(key);
    if (currentValue != storedValue)
    {
      zdo.Set(key, currentValue);
    }
  }

  public static void TrySetZDOStringOnChange(this ZDO zdo, string key, string currentValue)
  {
    var storedValue = zdo.GetString(key);
    if (storedValue != currentValue)
    {
      zdo.Set(key, currentValue);
    }
  }

  public static void TrySetZDOIntOnChange(this ZDO zdo, string key, int currentValue)
  {
    var storedValue = zdo.GetInt(key);
    if (storedValue != currentValue)
    {
      zdo.Set(key, currentValue);
    }
  }

  public static void TrySetZDOFloatOnChange(this ZDO zdo, string key, float currentValue)
  {
    var storedValue = zdo.GetFloat(key);
    if (!Mathf.Approximately(storedValue, currentValue))
    {
      zdo.Set(key, currentValue);
    }
  }

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
  public static void RunIfServer(this INetView instance, Action<ZNetView> action)
  {
    if (!ZNet.instance || !ZNet.instance.IsServer()) LoggerProvider.LogWarning("Not running action as we are not a server");
    if (!instance.IsNetViewValid(out var netView)) return;
    if (!netView.IsOwner())
      netView.ClaimOwnership();
    action.Invoke(netView);
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

  public static IEnumerator WaitForZNetViewCoroutine(MonoBehaviour instance, Action<ZNetView> action, float timeout = 10f)
  {
    var startTime = Time.realtimeSinceStartup;
    var netView = instance.GetComponent<ZNetView>();

    while (instance && instance.isActiveAndEnabled && (!netView || !netView.IsValid()))
    {
      if (!instance) yield break;
      if (Time.realtimeSinceStartup - startTime > timeout)
      {
#if DEBUG
        LoggerProvider.LogInfoDebounced(
          $"znet_timeout_{instance.GetType().Name}",
          $"[ZNetViewUtil] ZNetView not valid on {instance.GetType().Name} after {timeout}s at {instance.transform.position}"
        );
#endif
        yield break;
      }

      yield return null;
      netView = instance.GetComponent<ZNetView>();
    }

    if (!instance)
    {
#if DEBUG
      LoggerProvider.LogInfoDebounced("Bailed due to instance becoming invalid.");
#endif
      yield break;
    }

#if DEBUG
    LoggerProvider.LogInfoDebounced(
      $"znet_register_{instance.GetType().Name}",
      $"ZNetView ready, registering {instance.GetType().Name} on '{instance.name}' at {instance.transform.position}"
    );
#endif

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