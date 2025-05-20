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
    private const string RPC_RequestRebuildName = "ValheimVehicles_RequestRebuildNetwork";
    private static bool _rebuildRegistered;
    public void RegisterRebuildRpc()
    {
      if (_rebuildRegistered || ZRoutedRpc.instance == null)
        return;

      if (ZNet.instance.IsServer())
      {
        ZRoutedRpc.instance.Register<ZPackage>(RPC_RequestRebuildName, Server_HandleRebuildRequest);
        LoggerProvider.LogDebug($"[ZDORebuild] Registered rebuild handler on server.");
      }
      else
      {
        ZRoutedRpc.instance.Register<ZPackage>(RPC_RequestRebuildName, (_, __) => {});
        LoggerProvider.LogDebug($"[ZDORebuild] Registered rebuild stub on client.");
      }

      _rebuildRegistered = true;
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
      yield return RebuildPowerNetworkCoroutine();

      LoggerProvider.LogDebug("Finished RebuildPowerNetworkCoroutine triggered by ForceSpawnIfItDoesNotExist");
      yield return null;

      LoggerProvider.LogInfo($"[ZDORebuild] Processed ZDO claim/instantiate for zdos: {zdos.Count} items. \nNew count of all elements Consumers: {Consumers.Count} Conduits: {Conduits.Count} Storages: {Storages.Count} Sources {Sources.Count}, Pylons count: {Pylons.Count}, network(s) {_networks.Count}");
    }

    private void Server_HandleRebuildRequest(long sender, ZPackage pkg)
    {
      if (!ZNet.instance.IsServer())
        return;

      pkg.SetPos(0);

      var count = pkg.ReadInt();
      var ids = new List<ZDOID>(count);

      for (var i = 0; i < count; i++)
      {
        try
        {

          ids.Add(pkg.ReadZDOID());
        }
        catch (Exception e)
        {
          LoggerProvider.LogError($"Error while reading zdoid {e}");
        }
      }

      try
      {
        LoggerProvider.LogInfo($"[ZDORebuild] Received rebuild request with {ids.Count} ZDOIDs");
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"{e}");
      }

      // var registerInstances = new List<GameObject>();
      List<ZDO> zdos = new();
      foreach (var id in ids)
      {
        var zdo = ZDOMan.instance.GetZDO(id);
        if (zdo == null || !zdo.Persistent)
        {
          ZLog.LogWarning($"[ZDORebuild] Skipping invalid ZDO {id}");
          continue;
        }

        if (zdo.GetOwner() != ZDOMan.GetSessionID())
        {
          LoggerProvider.LogDebug($"[ZDORebuild] was not owner calling setowner");
          zdo.SetOwner(ZDOMan.GetSessionID());
        }

        zdos.Add(zdo);
      }
      StartCoroutine(ForceSpawnOnServerIfItDoesNotExist(zdos));
    }

    // private static void Server_HandleRebuildRequest(long sender, ZPackage pkg)
    // {
    //   if (!ZNet.instance.IsServer())
    //     return;
    //
    //   pkg.SetPos(0);
    //
    //   var count = pkg.ReadInt();
    //   var ids = new List<ZDOID>(count);
    //
    //   for (var i = 0; i < count; i++)
    //   {
    //     try
    //     {
    //
    //       ids.Add(pkg.ReadZDOID());
    //     }
    //     catch (Exception e)
    //     {
    //       LoggerProvider.LogError($"Error while reading zdoid {e}");
    //     }
    //   }
    //
    //   try
    //   {
    //     LoggerProvider.LogInfo($"[ZDORebuild] Received rebuild request with {ids.Count} ZDOIDs");
    //   }
    //   catch (Exception e)
    //   {
    //     LoggerProvider.LogError($"{e}");
    //   }
    //
    //   var registerInstances = new List<GameObject>();
    //
    //   foreach (var id in ids)
    //   {
    //     var zdo = ZDOMan.instance.GetZDO(id);
    //     if (zdo == null || !zdo.Persistent)
    //     {
    //       ZLog.LogWarning($"[ZDORebuild] Skipping invalid ZDO {id}");
    //       continue;
    //     }
    //
    //     if (zdo.GetOwner() != ZDOMan.GetSessionID())
    //     {
    //       LoggerProvider.LogDebug($"[ZDORebuild] was not owner calling setowner");
    //       zdo.SetOwner(ZDOMan.GetSessionID());
    //     }
    //
    //     var instance = ZNetScene.instance.FindInstance(id);
    //
    //     if (instance == null)
    //     {
    //       LoggerProvider.LogDebug($"[ZDORebuild] found nothing so will instantiate from ZDO id:<{id}>");
    //
    //       var zdoPrefab = zdo.GetPrefab();
    //       LoggerProvider.LogDebug($"[ZDORebuild] Got zdoPrefab {zdoPrefab}");
    //       var prefab = ZNetScene.instance.GetPrefab(zdoPrefab);
    //       if (prefab == null)
    //       {
    //         LoggerProvider.LogDebug($"[ZDORebuild] found a null prefab for zdo.GetPrefab()");
    //       }
    //       if (prefab != null)
    //       {
    //         Instantiate(prefab, zdo.GetPosition(), Quaternion.identity);
    //         LoggerProvider.LogDebug($"[ZDORebuild] Instantiated {prefab.name} from ZDO {id}");
    //       }
    //     }
    //
    //     if (instance != null)
    //     {
    //       registerInstances.Add(instance);
    //     }
    //   }
    //
    //   var successCount = 0;
    //
    //   foreach (var ri in registerInstances)
    //   {
    //     var powerSource = ri.GetComponent<IPowerSource>();
    //     var powerStorage = ri.GetComponent<IPowerStorage>();
    //     var powerConsumer = ri.GetComponent<IPowerConsumer>();
    //     var powerPylon = ri.GetComponent<PowerPylon>();
    //     var powerConduit = ri.GetComponent<IPowerConduit>();
    //
    //     if (powerSource != null)
    //     {
    //       LoggerProvider.LogDebug("Has powerSource");
    //       RegisterPowerComponent(powerSource);
    //       successCount++;
    //     }
    //     if (powerStorage != null)
    //     {
    //       LoggerProvider.LogDebug("Has powerStorage");
    //       RegisterPowerComponent(powerStorage);
    //       successCount++;
    //     }
    //     if (powerConsumer != null)
    //     {
    //       LoggerProvider.LogDebug("Has powerConsumer");
    //       RegisterPowerComponent(powerConsumer);
    //       successCount++;
    //     }
    //     if (powerPylon != null)
    //     {
    //       LoggerProvider.LogDebug("Has powerPylon");
    //       RegisterPowerComponent(powerPylon);
    //       successCount++;
    //     }
    //     if (powerConduit != null)
    //     {
    //       LoggerProvider.LogDebug("Has powerConduit");
    //       RegisterPowerComponent(powerConduit);
    //       successCount++;
    //     }
    //   }
    //
    //   RequestRebuildNetwork();
    //
    //   LoggerProvider.LogInfo($"[ZDORebuild] Processed ZDO claim/instantiate for {ids.Count} items. And {successCount} components were detected and fired RegisterPowerComponents. \nNew count of all elements Consumers: {Consumers.Count} Conduits: {Conduits.Count} Storages: {Storages.Count} Sources {Sources.Count}");
    //   LoggerProvider.LogInfo($"[ZDORebuild] Got New registerInstances <{registerInstances.Count}>");
    // }

    public static void RequestRebuildNetworkWithZDOs(List<ZDOID> zdos)
    {
      var pkg = new ZPackage();
      pkg.Write(zdos.Count);
      foreach (var zdo in zdos)
      {
        pkg.Write(zdo); // ZDOID has a Write(ZPackage) overload
      }

      ZRoutedRpc.instance.InvokeRoutedRPC(
        ZRoutedRpc.instance.GetServerPeerID(),
        RPC_RequestRebuildName,
        pkg
      );
    }

    // public void RequestRebuildNetworkFromClient(IEnumerable<ZDO> zdos)
    // {
    //   if (!ZNet.instance || ZNet.instance.IsServer()) return;
    //
    //   var ids = zdos
    //     .Where(zdo => zdo != null && zdo.IsValid())
    //     .Select(zdo => zdo.m_uid)
    //     .ToList();
    //
    //   LoggerProvider.LogInfo($"[ZDORebuild] Sending rebuild request with {ids.Count} ZDOs");
    //   ZRoutedRpc.instance.InvokeRoutedRPC(
    //     ZRoutedRpc.instance.GetServerPeerID(),
    //     RPC_RequestRebuild,
    //     ids
    //   );
    // }

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
      LoggerProvider.LogInfoDebounced($"Called RequestRebuildNetwork instanceIsNotNull: <{Instance != null}>");
      if (Instance == null) return;
      LoggerProvider.LogInfoDebounced($"Called RequestRebuildNetwork Instance._rebuildPylonNetworkRoutine: <{Instance._rebuildPylonNetworkRoutine != null}>");
      if (Instance._rebuildPylonNetworkRoutine != null) { return; }
      LoggerProvider.LogInfoDebounced("Called RequestRebuildNetwork Made it past instance check.");
      Instance._rebuildPylonNetworkRoutine = Instance.StartCoroutine(Instance.RebuildPowerNetworkCoroutine());
    }
  }
}