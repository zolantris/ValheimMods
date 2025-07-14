using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Helpers;

public static class ValheimExtensions
{
  /// <summary>
  /// For debugging. These values/stats are not accurate if connecting TO a server. Meaning if you are not a server you will not know if you are connecting to a dedicated server without additional RPC values (I think). Jotunn api for checking does the same thing and seems inaccurate...with latest valheim reported values.
  /// </summary>
  [Conditional("DEBUG")]
  public static void LogValheimServerStats()
  {
    if (!ZNet.instance || !ZNetScene.instance) return;
    var znet = ZNet.instance;

    LoggerProvider.LogDebugDebounced($"IsDedicated: {znet.IsDedicated()}\n IsServer: {znet.IsServer()}, IsClient: {!znet.IsServer() && !znet.IsDedicated()}, IsLocalServer: {!znet.IsServer() && !znet.IsDedicated()}");
  }

  public static bool GetZDO(this GameObject go, [NotNullWhen(true)] out ZDO? zdo)
  {
    zdo = null;
    if (!go) return false;
    var nv = go.GetComponent<ZNetView>();
    if (!nv || !nv.IsValid()) return false;
    zdo = nv.GetZDO();
    return zdo != null;
  }

  public static void SetDelta<T>(this ZDO zdo, string key, T currentValue)
  {
    var type = typeof(T);
    if (type == typeof(float))
    {
      SetDeltaFloat(zdo, key, (float)(object)currentValue);
    }
    else
    {
      LoggerProvider.LogError($"TrySetZDOOnChange generic only supports float for now. Got: {type}");
    }
  }

  public static void SetDelta(this ZDO zdo, string key, string currentValue)
  {
    SetDeltaString(zdo, key, currentValue);
  }

  public static void SetDelta(this ZDO zdo, string key, float currentValue)
  {
    SetDeltaFloat(zdo, key, currentValue);
  }

  public static void SetDelta(this ZDO zdo, string key, int currentValue)
  {
    SetDeltaInt(zdo, key, currentValue);
  }

  public static void SetDelta(this ZDO zdo, string key, bool currentValue)
  {
    SetDeltaBool(zdo, key, currentValue);
  }


  public static void SetDeltaBool(this ZDO zdo, string key, bool currentValue)
  {
    var storedValue = zdo.GetBool(key);
    if (currentValue != storedValue)
    {
      zdo.Set(key, currentValue);
    }
  }

  public static void SetDeltaString(this ZDO zdo, string key, string currentValue)
  {
    var storedValue = zdo.GetString(key);
    if (storedValue != currentValue)
    {
      zdo.Set(key, currentValue);
    }
  }

  public static void SetDeltaInt(this ZDO zdo, string key, int currentValue)
  {
    var storedValue = zdo.GetInt(key);
    if (storedValue != currentValue)
    {
      zdo.Set(key, currentValue);
    }
  }

  public static void SetDeltaFloat(this ZDO zdo, string key, float currentValue)
  {
    var storedValue = zdo.GetFloat(key);
    if (!Mathf.Approximately(storedValue, currentValue))
    {
      zdo.Set(key, currentValue);
    }
  }

  // non-extension method
  public static bool Internal_IsNetViewValid(ZNetView? netView, [NotNullWhen(true)] out ZNetView? validNetView)
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
  public static void RunIfServerOrSinglePlayer(this INetView instance, Action<ZNetView> action)
  {
    if (!ZNet.instance || !ZNet.instance.IsServer() && !ZNet.IsSinglePlayer)
    {
      return;
    }
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

  public static Coroutine? WaitForZNetView(this MonoBehaviour instance, Action action)
  {
    var nvController = instance as INetView;

    // early bail.
    if (nvController != null && nvController.m_nview != null && nvController.m_nview.IsValid())
    {
      action();
      return null;
    }

    return instance.StartCoroutine(WaitForZNetViewCoroutine(instance, _ => action()));
  }

  public static Coroutine? WaitForZNetView(this MonoBehaviour instance, Action<ZNetView> action, float timeout = 10f, bool shouldLookAtParent = false)
  {
    var nvController = instance as INetView;

    // early bail.
    if (nvController != null && nvController.m_nview != null && nvController.m_nview.IsValid())
    {
      action(nvController.m_nview);
      return null;
    }
    return instance.StartCoroutine(WaitForZNetViewCoroutine(instance, action, timeout, shouldLookAtParent));
  }

  public static IEnumerator WaitForZNetViewCoroutine(this MonoBehaviour instance, Action<ZNetView> action, float timeout = 10f, bool shouldLookAtParent = false)
  {
    ZNetView GetNetView()
    {
      return shouldLookAtParent ? instance.GetComponentInParent<ZNetView>() : instance.GetComponent<ZNetView>();
    }

    var netView = GetNetView();


    var timer = Stopwatch.StartNew();

    while (timer.ElapsedMilliseconds < timeout && instance && instance.isActiveAndEnabled && (netView == null || !netView.IsValid()))
    {
      yield return null;
      if (!instance) yield break;
      if (!netView)
      {
        netView = GetNetView();
      }
      if (netView == null || !netView.IsValid()) continue;
    }

    if (timer.ElapsedMilliseconds >= timeout)
    {
#if DEBUG
      LoggerProvider.LogInfoDebounced(
        $"znet_timeout_{instance.GetType().Name}",
        $"[ZNetViewUtil] ZNetView not valid on {instance.GetType().Name} after {timeout}s at {instance.transform.position}"
      );
#endif
      yield break;
    }

    if (!instance)
    {
#if DEBUG
      LoggerProvider.LogInfoDebounced("Bailed due to instance becoming invalid.");
#endif
      yield break;
    }

    if (!Internal_IsNetViewValid(netView, out netView))
    {
      yield break;
    }

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

  public static bool TryClaimOwnership(this ZDO zdo)
  {
    if (zdo == null) return false;
    if (!zdo.IsOwner())
    {
      zdo.SetOwner(ZDOMan.GetSessionID());
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