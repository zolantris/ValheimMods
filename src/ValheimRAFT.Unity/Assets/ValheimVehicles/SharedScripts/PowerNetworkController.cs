// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.Helpers;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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

    [SerializeField] private int curvedLinePoints = 50;
    [SerializeField] private Material fallbackWireMaterial;
    protected readonly Dictionary<string, List<IPowerNode>> _networks = new();

    internal readonly float _updateInterval = 0.25f;
    internal float _nextUpdate;

    public Coroutine? _rebuildPylonNetworkRoutine;
    public static Material WireMaterial { get; set; }


    public override void Awake()
    {
#if UNITY_EDITOR
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

    protected void OnDestroy()
    {
      ClearAllSimulatedNetworkData();
      StopAllCoroutines();
    }

    protected virtual void FixedUpdate()
    {
      if (Time.time < _nextUpdate) return;
      _nextUpdate = Time.time + _updateInterval;

      foreach (var pair in _networks)
      {
        Host_SimulateNetwork(pair.Value, pair.Key);
      }
    }

    public static void RegisterPowerComponent<T>(T component)
    {
      switch (component)
      {
        case IPowerSource s:
          if (!Sources.Contains(s)) Sources.Add(s);
          break;
        case IPowerStorage b:
          if (!Storages.Contains(b)) Storages.Add(b);
          break;
        case IPowerConsumer c:
          if (!Consumers.Contains(c)) Consumers.Add(c);
          break;
        case PowerPylon p:
          if (!Pylons.Contains(p)) Pylons.Add(p);
          break;
        case IPowerConduit conduit:
          if (!Conduits.Contains(conduit)) Conduits.Add(conduit);
          break;
        default:
          LoggerProvider.LogWarning($"[Power] Unrecognized component type: {typeof(T).Name}");
          break;
      }
      RequestRebuildNetwork();
    }

    public static void UnregisterPowerComponent<T>(T component)
    {
      switch (component)
      {
        case IPowerSource s:
          Sources.FastRemove(s);
          break;
        case IPowerStorage b:
          Storages.FastRemove(b);
          break;
        case IPowerConsumer c:
          Consumers.FastRemove(c);
          break;
        case PowerPylon p:
          Pylons.FastRemove(p);
          break;
        case IPowerConduit conduit:
          Conduits.FastRemove(conduit);
          break;
        default:
          LoggerProvider.LogWarning($"[Power] Unrecognized component type: {typeof(T).Name}");
          break;
      }
      RequestRebuildNetwork();
    }

    /// <summary>
    /// This is super silly we have to call to force valheim server to render all the missing ZDOs.
    ///
    /// The other alternative is to always have a client owner. But this would mean all ZDOs would have to be claimed via that client. For anything in the network.
    /// </summary>
    ///
    /// - This does not add new instances.
    /// - This does not show any spawned instances (server spawn only).
    /// 
    public IEnumerator ForceSpawnOnServerIfItDoesNotExist(List<ZDO> zdos)
    {
      var instances = new List<GameObject>();

      foreach (var zdo in zdos)
      {
        ZNet.instance.SetReferencePosition(zdo.GetPosition());
        if (zdo.GetOwner() == 0 || zdo.GetOwner() != ZDOMan.GetSessionID())
        {
          zdo.SetOwner(ZDOMan.GetSessionID());
          LoggerProvider.LogDebug($"[ZDORebuild] Set ownership of ZDO {zdo.m_uid} to this server: {ZDOMan.GetSessionID()}");
        }

        var instance = ZNetScene.instance.FindInstance(zdo.m_uid);
        if (instance == null)
        {
          var prefabHash = zdo.GetPrefab();
          LoggerProvider.LogInfoDebounced($"Got prefabhash: {prefabHash}");
          var hasPrefab = ZNetScene.instance.m_namedPrefabs.ContainsKey(prefabHash);
          if (!hasPrefab)
          {
            LoggerProvider.LogError($"[ZDORebuild] ZNetScene is missing prefab hash {prefabHash}. Cannot safely CreateObject.");
          }
          else
          {
            var prefab = ZNetScene.instance.GetPrefab(prefabHash);
            if (prefab == null)
            {
              LoggerProvider.LogError($"[ZDORebuild] GetPrefab({prefabHash}) returned null.");
            }
            else
            {
              var go = ZNetScene.instance.CreateObject(zdo);
              instances.Add(ZNetScene.instance.FindInstance(zdo.m_uid));
              LoggerProvider.LogDebug($"[ZDORebuild] Successfully instantiated: {go?.name}");
            }
          }
        }
      }

      yield return null;

      LoggerProvider.LogInfoDebounced($"Instances without null references before fixed update {instances.Where(x => x && x != null).ToList().Count}");

      yield return new WaitForFixedUpdate();
      LoggerProvider.LogInfoDebounced($"Instances without null references after fixed update {instances.Where(x => x).ToList().Count}");

      instances.Clear();

      foreach (var zdo in zdos)
      {
        instances.Add(ZNetScene.instance.FindInstance(zdo.m_uid));
      }

      LoggerProvider.LogInfoDebounced($"Instances without null references {instances.Where(x => x).ToList().Count}");

      var successCount = 0;

      foreach (var ri in instances)
      {
        try
        {
          if (!ri || ri == null) continue;
          var powerSource = ri.GetComponent<IPowerSource>();
          var powerStorage = ri.GetComponent<IPowerStorage>();
          var powerConsumer = ri.GetComponent<IPowerConsumer>();
          var powerPylon = ri.GetComponent<PowerPylon>();
          var powerConduit = ri.GetComponent<IPowerConduit>();
          var powerConduitIntegration = ri.GetComponent<PowerConduitPlateDrainComponentIntegration>();

          var powerStorageIntegration = ri.GetComponent<PowerStorageComponentIntegration>();
          if (powerStorageIntegration)
          {
            LoggerProvider.LogDebug("Has powerStorageIntegration");
          }
          if (powerConduitIntegration)
          {
            LoggerProvider.LogDebug("Has powerConduitIntegration");
          }

          if (powerSource != null)
          {
            LoggerProvider.LogDebug("Has powerSource");
            RegisterPowerComponent(powerSource);
            successCount++;
          }
          if (powerStorage != null)
          {
            LoggerProvider.LogDebug("Has powerStorage");
            RegisterPowerComponent(powerStorage);
            successCount++;
          }
          if (powerConsumer != null)
          {
            LoggerProvider.LogDebug("Has powerConsumer");
            RegisterPowerComponent(powerConsumer);
            successCount++;
          }
          if (powerPylon != null)
          {
            LoggerProvider.LogDebug("Has powerPylon");
            RegisterPowerComponent(powerPylon);
            successCount++;
          }
          if (powerConduit != null)
          {
            LoggerProvider.LogDebug("Has powerConduit");
            RegisterPowerComponent(powerConduit);
            successCount++;
          }
        }
        catch (Exception e)
        {
          LoggerProvider.LogWarning($"Bad gameobject or something else.  {e}");
        }
      }

      LoggerProvider.LogInfoDebounced($"Finished ForceSpawnIfItDoesNotExist with successCount: {successCount} and instances.Count: {instances.Count}");

      try
      {

        if (instances.Count > 0)
        {
          var message = "";
          foreach (var go in instances)
          {
            if (go == null) continue;
            message += $"Spawned: {go?.name}";
          }
          LoggerProvider.LogInfoDebounced(message);
        }
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning($"Bad message about spawned objects {e}");
      }
      yield return null;

      LoggerProvider.LogDebug("Continuing on and manual regenerating networks");
      yield return RequestRebuildPowerNetworkCoroutine();

      LoggerProvider.LogDebug("Finished RebuildPowerNetworkCoroutine triggered by ForceSpawnIfItDoesNotExist");
      yield return null;

      LoggerProvider.LogInfo($"[ZDORebuild] Processed ZDO claim/instantiate for zdos: {zdos.Count} items. \nNew count of all elements Consumers: {Consumers.Count} Conduits: {Conduits.Count} Storages: {Storages.Count} Sources {Sources.Count}, Pylons count: {Pylons.Count}, network(s) {_networks.Count}");
    }

    public virtual void Server_HandleRebuildRequest(long sender, ZPackage pkg) {}


#if UNITY_EDITOR
    [InitializeOnLoadMethod]
private static void ClearPowerListsOnReload()
{
    Sources.Clear();
    Consumers.Clear();
    Storages.Clear();
}
#endif
    public static void RequestRebuildNetwork()
    {
      if (Instance == null || Instance._rebuildPylonNetworkRoutine != null) { return; }
      Instance._rebuildPylonNetworkRoutine = Instance.StartCoroutine(Instance.RequestRebuildPowerNetworkCoroutine());
    }
  }
}