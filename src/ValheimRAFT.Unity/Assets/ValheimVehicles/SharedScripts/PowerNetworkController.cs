﻿// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;
using Debug = UnityEngine.Debug;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController : SingletonBehaviour<PowerNetworkController>
  {

    public static List<IPowerSource> Sources = new();
    public static List<IPowerStorage> Storages = new();
    public static List<IPowerConsumer> Consumers = new();
    public static List<PowerPylon> Pylons = new();
    public static List<IPowerConduit> Conduits = new();
    protected static readonly Dictionary<string, List<IPowerNode>> powerNodeNetworks = new();

    [SerializeField] private int curvedLinePoints = 50;
    [SerializeField] private Material fallbackWireMaterial;

    internal readonly float _updateInterval = 1f;
    internal float _nextUpdate;

    public Coroutine? _rebuildPylonNetworkRoutine;
    public static Material WireMaterial { get; set; }


    public override void Awake()
    {
#if UNITY_2022
      if (WireMaterial == null)
      {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader)
        {
          WireMaterial = new Material(shader)
          {
            color = Color.black
          };
          WireMaterial.EnableKeyword("_EMISSION");
          WireMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
          WireMaterial.SetColor("_EmissionColor", Color.black * 1.5f);
        }
        else
        {
          Debug.LogWarning("Default wire shader not found. WireMaterial will be pink.");
        }
      }
      if (WireMaterial == null && fallbackWireMaterial != null)
        WireMaterial = fallbackWireMaterial;
#endif
      base.Awake();
    }

    protected virtual void FixedUpdate()
    {
      if (Time.time < _nextUpdate) return;
      _nextUpdate = Time.time + _updateInterval;

      // foreach (var pair in powerNodeNetworks)
      // {
      //   
      // }
    }

    protected void OnDestroy()
    {
      StopAllCoroutines();
    }

    // public static void RegisterPowerComponent<T>(T component)
    // {
    //   switch (component)
    //   {
    //     case IPowerSource s:
    //       if (!Sources.Contains(s)) Sources.Add(s);
    //       break;
    //     case IPowerStorage b:
    //       if (!Storages.Contains(b)) Storages.Add(b);
    //       break;
    //     case IPowerConsumer c:
    //       if (!Consumers.Contains(c)) Consumers.Add(c);
    //       break;
    //     case PowerPylon p:
    //       if (!Pylons.Contains(p)) Pylons.Add(p);
    //       break;
    //     case IPowerConduit conduit:
    //       if (!Conduits.Contains(conduit)) Conduits.Add(conduit);
    //       break;
    //     default:
    //       LoggerProvider.LogWarning($"[Power] Unrecognized component type: {typeof(T).Name}");
    //       break;
    //   }
    //   RequestRebuildNetwork();
    // }

    // public static void UnregisterPowerComponent<T>(T component)
    // {
    //   switch (component)
    //   {
    //     case IPowerSource s:
    //       Sources.FastRemove(s);
    //       break;
    //     case IPowerStorage b:
    //       Storages.FastRemove(b);
    //       break;
    //     case IPowerConsumer c:
    //       Consumers.FastRemove(c);
    //       break;
    //     case PowerPylon p:
    //       Pylons.FastRemove(p);
    //       break;
    //     case IPowerConduit conduit:
    //       Conduits.FastRemove(conduit);
    //       break;
    //     default:
    //       LoggerProvider.LogWarning($"[Power] Unrecognized component type: {typeof(T).Name}");
    //       break;
    //   }
    //   RequestRebuildNetwork();
    //
    // }

// #if UNITY_2022
//     [InitializeOnLoadMethod]
// private static void ClearPowerListsOnReload()
// {
//     Sources.Clear();
//     Consumers.Clear();
//     Storages.Clear();
// }
// #endif
  }
}