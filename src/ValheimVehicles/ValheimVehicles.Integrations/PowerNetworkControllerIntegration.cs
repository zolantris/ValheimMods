// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations;

public partial class PowerNetworkControllerIntegration : PowerNetworkController
{
  private readonly List<string> _networksToRemove = new();

  public override void Awake()
  {
    LoggerProvider.LogDebug("Called Awake with debug");
    LoggerProvider.LogMessage("Called Awake with Message");
    base.Awake();
    LoggerProvider.LogMessage("Called post awake with message");
    StartCoroutine(DelayedRegister());
  }

  public IEnumerator DelayedRegister()
  {
    while (ZNet.instance == null || ZRoutedRpc.instance == null)
    {
      yield return null;
    }

    ZDOClaimUtility.RegisterClaimZdoRpc();
    PowerSystemRPC.Register();
    StartDirtyNetworkCoroutine();
  }

  // Do nothing for fixed update. Hosts can run for it. But a host/client could freeze and not run this causing massive desyncs for non-hosts. 
  protected override void FixedUpdate()
  {
    SimulateOnClientAndServer();
  }

  protected override void Update()
  {
    base.Update();
  }

  public static void SimulateAllNetworks(List<IPowerNode> allNodes)
  {
    var groupedByNetwork = new Dictionary<string, List<IPowerNode>>();
    foreach (var node in allNodes)
    {
      if (!ZNetScene.instance || node == null) continue;

      var netView = node.gameObject.GetComponent<ZNetView>();
      if (!netView || !netView.IsValid()) continue;

      var zdo = netView.GetZDO();
      if (zdo == null) continue;

      var networkId = zdo.GetString(VehicleZdoVars.Power_NetworkId);
      if (string.IsNullOrEmpty(networkId)) continue;

      if (!groupedByNetwork.TryGetValue(networkId, out var list))
      {
        list = new List<IPowerNode>();
        groupedByNetwork[networkId] = list;
      }

      list.Add(node);
    }

    // do not do client sync on dedicated.
    if (!ZNet.instance || ZNet.instance.IsDedicated())
    {
      return;
    }

    Client_SyncActiveInstances();
    foreach (var kvp in groupedByNetwork)
    {
      var networkId = kvp.Key;
      Client_SyncNetworkStats(networkId);
    }
  }

  private static readonly HashSet<string> _dirtyNetworkIds = new();
  private float _lastSimulateTime;

  private Coroutine _simulateCoroutine;

  private const float IdleDelay = 1f;
  private const float MaxDelay = 3f;

  public void StartDirtyNetworkCoroutine()
  {
    if (_simulateCoroutine != null) return;
    _simulateCoroutine = StartCoroutine(SimulateDirtyNetworksCoroutine());
  }

  public void StopDirtyNetworkCoroutine()
  {
    if (_simulateCoroutine != null)
    {
      StopCoroutine(_simulateCoroutine);
      _simulateCoroutine = null;
    }
  }

  public static void MarkNetworkDirty(string networkId)
  {
    _dirtyNetworkIds.Add(networkId);
  }

  private IEnumerator SimulateDirtyNetworksCoroutine()
  {
    while (isActiveAndEnabled)
    {
      yield return new WaitForSeconds(0.5f);

      if (_dirtyNetworkIds.Count == 0) continue;

      var now = Time.time;
      var canRun = now - _lastSimulateTime >= IdleDelay;

      if (canRun || now - _lastSimulateTime >= MaxDelay)
      {
        foreach (var networkId in _dirtyNetworkIds.ToList())
        {
          if (!PowerZDONetworkManager.Networks.TryGetValue(networkId, out var zdos)) continue;

          // reloads only the zdos that have updated.
          foreach (var zdo in zdos)
          {
            if (!PowerZDONetworkManager.TryGetActiveComponentUpdater(zdo.m_uid, out var updater)) continue;
            updater.Load();
          }
        }

        _dirtyNetworkIds.Clear();
        _lastSimulateTime = now;
      }
    }
  }

  public override IEnumerator RequestRebuildPowerNetworkCoroutine()
  {
    PowerZDONetworkManager.RebuildClusters();
    // Skip this as it does too much now. Only need to reference our ZDOS then match them to powernodes
    // yield return base.RequestRebuildPowerNetworkCoroutine();

    // Updates the power nodes with the latest simulation. This is for all rendered nodes and will only be accurate on clients.
    UpdateAllPowerNodes();

    yield return null;
    // for creating a network of IPowerNodes using our new logic
    SimulateAllNetworks(AllPowerNodes);

    yield return null;
  }

  private static List<IPowerNode> GetNodesFromZDOs(List<ZDO> zdos)
  {
    var nodes = new List<IPowerNode>();
    foreach (var zdo in zdos)
    {
      if (ZNetScene.instance == null) continue;

      var go = ZNetScene.instance.FindInstance(zdo.m_uid);
      if (go == null) continue;

      var components = go.GetComponents<MonoBehaviour>();
      foreach (var comp in components)
      {
        if (comp is IPowerNode node)
        {
          nodes.Add(node);
          break;
        }
      }
    }
    return nodes;
  }

  public float GetTotalConsumerDemand(List<(PowerConsumerData consumer, ZDO zdo)> powerConsumers, float deltaTime)
  {
    var totalDemand = 0f;
    foreach (var (consumer, _) in powerConsumers)
      totalDemand += consumer.RequestedPower(deltaTime);

    return totalDemand;
  }

  public static List<PowerStorageData> storagesToUpdate = new();
  public static List<PowerSourceData> sourcesToUpdate = new();

  public override void Host_SimulateNetwork(string networkId)
  {
    if (!PowerZDONetworkManager.TryBuildPowerNetworkSimData(networkId, out var simData))
    {
      LoggerProvider.LogError($"Failed to build sim data for network {networkId}");
      return;
    }

    storagesToUpdate.Clear();
    sourcesToUpdate.Clear();

    var deltaTime = Time.fixedDeltaTime;
    var changedZDOs = new HashSet<ZDOID>();

    var totalConsumerDemand = GetTotalConsumerDemand(simData.Consumers, deltaTime);

    var totalDemand = totalConsumerDemand;

    // Step 1: Total demand
    foreach (var (storage, _) in simData.Storages)
    {
      totalDemand += Mathf.Max(0f, storage.MaxCapacity - storage.StoredEnergy);
    }

    var conduitDemand = 0f;
    foreach (var (conduit, _) in simData.Conduits)
    {
      conduit.ResolvePlayersFromIds();
      conduitDemand += PowerConduitData.GetAverageEitr(conduit.Players);
    }

    totalDemand += conduitDemand;
    var isDemanding = totalDemand > 0f;

    // Step 2: Peek storage discharge
    var suppliedFromStorage = 0f;
    var storageDischargeList = new List<(PowerStorageData storage, float amount)>();

    var remainingDemand = totalDemand;

    foreach (var (storage, _) in simData.Storages)
    {
      if (remainingDemand <= 0f) break;

      var peek = storage.PeekDischarge(remainingDemand);
      if (peek > 0f)
      {
        storageDischargeList.Add((storage, peek));
        suppliedFromStorage += peek;
        remainingDemand -= peek;
      }
    }

    // Step 3: Offer from sources
    var offeredFromSources = 0f;
    var sourceOfferMap = new Dictionary<PowerSourceData, float>();

    foreach (var (source, _) in simData.Sources)
    {
      if (remainingDemand <= 0f && !NeedsCharging(simData.Storages)) break;

      var clamped = source.GetMaxPotentialOutput(deltaTime);

      if (clamped > 0f)
      {
        sourceOfferMap[source] = clamped;
        offeredFromSources += clamped;
        remainingDemand -= clamped;
      }
    }

    var totalAvailable = offeredFromSources + suppliedFromStorage;

    // Step 4: Apply to conduits (players needing Eitr)
    foreach (var (conduit, zdo) in simData.Conduits)
    {
      if (totalAvailable <= 0f || conduit.Players.Count == 0) continue;

      var perPlayer = totalAvailable / conduit.Players.Count;
      var used = 0f;

      foreach (var player in conduit.Players)
      {
        if (player.m_eitr < player.m_maxEitr - 0.1f)
        {
          PowerConduitPlateComponentIntegration.Request_AddEitr(player, perPlayer);
          used += perPlayer;
        }
      }

      totalAvailable -= used;
      changedZDOs.Add(zdo.m_uid);
    }

    // Discharge (before feeding back into storage)
    var toDischarge = usedFromStorage;
    foreach (var (storage, amount) in storageDischargeList)
    {
      if (toDischarge <= 0f) break;

      storage.CommitDischarge();
      storagesToUpdate.Add(storage);
      toDischarge -= amount;
    }

    // Step 5: Feed true surplus into storage
    foreach (var (storage, zdo) in simData.Storages)
    {
      if (totalAvailable <= 0f) break;

      var previousStoredEnergy = storage.StoredEnergy;

      var remainingCap = Mathf.Max(0f, storage.MaxCapacity - storage.StoredEnergy);
      var accepted = Mathf.Min(remainingCap, totalAvailable);

      storage.StoredEnergy += accepted;
      totalAvailable -= accepted;

      if (!Mathf.Approximately(previousStoredEnergy, storage.StoredEnergy))
      {
        storagesToUpdate.Add(storage);
      }
    }

    // Step 6: Final fuel/discharge commit
    var totalUsed = offeredFromSources + suppliedFromStorage - totalAvailable;
    var usedFromStorage = Mathf.Clamp(totalUsed - offeredFromSources, 0f, suppliedFromStorage);
    var usedFromSources = Mathf.Clamp(totalUsed - usedFromStorage, 0f, offeredFromSources);

    // Fuel burn
    var toBurn = usedFromSources;
    foreach (var kvp in sourceOfferMap)
    {
      var sourceData = kvp.Key;
      var amountToBurn = kvp.Value;

      var burntEnergy = sourceData.ProducePower(amountToBurn);
      toBurn -= sourceData.EstimateFuelCost(burntEnergy);

      if (burntEnergy > 0f && sourceData.zdo != null)
      {
        sourceData.CommitEnergyUsed(burntEnergy);
        sourcesToUpdate.Add(sourceData);
      }

      if (toBurn <= 0f) break;
    }

    // Step 7: Save mutated data only.
    foreach (var source in sourcesToUpdate)
    {
      if (source == null || source.zdo == null) continue;
      source.zdo.Set(VehicleZdoVars.Power_StoredFuel, source.Fuel);
      changedZDOs.Add(source.zdo.m_uid);
    }

    foreach (var powerStorageData in storagesToUpdate)
    {
      if (powerStorageData == null || powerStorageData.zdo == null) continue;
      powerStorageData.zdo.Set(VehicleZdoVars.Power_StoredEnergy, powerStorageData.StoredEnergy);
      changedZDOs.Add(powerStorageData.zdo.m_uid);
    }

    // Step 8: Fire one RPC
    if (changedZDOs.Count > 0)
    {
      var pos = GetNetworkFallbackPosition(simData);
      PowerSystemRPC.SendPowerZDOsChangedToNearbyPlayers(networkId, changedZDOs.ToList(), simData);
    }
  }

  private static bool NeedsCharging(List<(PowerStorageData storage, ZDO zdo)> storages)
  {
    foreach (var (storage, _) in storages)
    {
      if (storage.StoredEnergy < storage.MaxCapacity)
        return true;
    }
    return false;
  }

  private static Vector3 GetNetworkFallbackPosition(PowerNetworkSimData simData)
  {
    if (simData.Conduits.Count > 0)
      return simData.Conduits[0].Item2.GetPosition();
    if (simData.Storages.Count > 0)
      return simData.Storages[0].Item2.GetPosition();
    if (simData.Sources.Count > 0)
      return simData.Sources[0].Item2.GetPosition();
    return Vector3.zero;
  }

  /// <summary>
  /// For nesting in a loop so O(n) can be reached.
  /// </summary>
  /// <param name="consumer"></param>
  /// <param name="status"></param>
  /// <param name="poweredOrInactiveConsumers"></param>
  /// <param name="inactiveDemandingConsumers"></param>
  public static void GetNetworkHealthStatusEnumeration(PowerConsumerData? consumer, ref string status, ref int poweredOrInactiveConsumers, ref int inactiveDemandingConsumers)
  {
    if (consumer == null && poweredOrInactiveConsumers == 0 && inactiveDemandingConsumers == 0)
    {
      status = ModTranslations.Power_NetworkInfo_NetworkFullPower;
      return;
    }

    if (!consumer.IsActive && consumer.IsDemanding)
    {
      poweredOrInactiveConsumers++;
    }
    else
    {
      inactiveDemandingConsumers++;
    }

    if (inactiveDemandingConsumers == 0)
    {
      status = ModTranslations.Power_NetworkInfo_NetworkFullPower;
      return;
    }

    if (inactiveDemandingConsumers > 0 && poweredOrInactiveConsumers > 0)
    {
      status = ModTranslations.Power_NetworkInfo_NetworkPartialPower;
      return;
    }

    status = ModTranslations.Power_NetworkInfo_NetworkLowPower;
  }

  public static void Client_SyncNetworkStats(string networkId)
  {
    if (!PowerZDONetworkManager.TryBuildPowerNetworkSimData(networkId, out var simData))
    {
      LoggerProvider.LogError($"Failed to build sim data for network {networkId}");
      return;
    }

    var deltaTime = Time.fixedDeltaTime;

    var totalDemand = 0f;
    var totalCapacity = 0f;

    // pure power units
    var totalConduitDemand = 0f;

    // pure power units
    var totalConduitSupply = 0f;

    var totalSupply = 0f;
    var totalFuel = 0f;
    var totalFuelCapacity = 0f;

    var chargeConduits = new List<PowerConduitData>();
    var drainConduits = new List<PowerConduitData>();


    var poweredOrInactiveConsumers = 0;
    var inactiveDemandingConsumers = 0;
    var NetworkConsumerPowerStatus = "";

    // Default Needed if there are no consumers.
    GetNetworkHealthStatusEnumeration(null, ref NetworkConsumerPowerStatus, ref poweredOrInactiveConsumers, ref inactiveDemandingConsumers);


    foreach (var (data, zdo) in simData.Conduits)
    {
      if (data.Mode == PowerConduitMode.Drain)
      {
        drainConduits.Add(data);
      }
      if (data.Mode == PowerConduitMode.Charge)
      {
        chargeConduits.Add(data);
      }
    }

    foreach (var (data, _) in simData.Consumers)
    {
      totalDemand += data.RequestedPower(deltaTime);
      GetNetworkHealthStatusEnumeration(data, ref NetworkConsumerPowerStatus, ref poweredOrInactiveConsumers, ref inactiveDemandingConsumers);
    }

    foreach (var (data, _) in simData.Storages)
    {
      totalSupply += data.StoredEnergy;
      totalCapacity += data.MaxCapacity;
    }

    foreach (var (data, _) in simData.Sources)
    {
      totalFuel += data.Fuel;
      totalFuelCapacity += data.MaxFuel;
    }

    var newData = new PowerNetworkData
    {
      NetworkConsumerPowerStatus = NetworkConsumerPowerStatus,
      NetworkPowerSupply = MathUtils.RoundToHundredth(totalSupply),
      NetworkPowerCapacity = MathUtils.RoundToHundredth(totalCapacity),
      NetworkPowerDemand = MathUtils.RoundToHundredth(totalDemand),
      NetworkFuelSupply = MathUtils.RoundToHundredth(totalFuel),
      NetworkFuelCapacity = MathUtils.RoundToHundredth(totalFuelCapacity)
    };
    newData.Cached_NetworkDataString = GenerateNetworkDataString(networkId, newData);
    UpdateNetworkPowerData(networkId, newData);
  }

  public static void Client_SyncActiveInstances()
  {

    foreach (var powerStorageComponentIntegration in PowerStorageComponentIntegration.Instances)
    {
      powerStorageComponentIntegration.Data.Load();
    }

    foreach (var powerStorageComponentIntegration in PowerConduitPlateComponentIntegration.Instances)
    {
      powerStorageComponentIntegration.Data.Load();
    }
    foreach (var powerStorageComponentIntegration in PowerSourceComponentIntegration.Instances)
    {
      powerStorageComponentIntegration.Data.Load();
    }
    foreach (var powerStorageComponentIntegration in PowerConsumerComponentIntegration.Instances)
    {
      powerStorageComponentIntegration.Data.Load();
    }
  }

  public void SimulateOnClientAndServer()
  {
    if (!isActiveAndEnabled || !ZNet.instance || !ZoneSystem.instance) return;
    if (Time.time < _nextUpdate) return;
    _nextUpdate = Time.time + _updateInterval;

    LoggerProvider.LogInfoDebounced($"_networks, {powerNodeNetworks.Count}, Consumers, {Consumers.Count}, Conduits, {Conduits.Count}, Storages, {Storages.Count}, Sources, {Sources.Count}");

    foreach (var pair in PowerZDONetworkManager.Networks)
    {
      var nodes = pair.Value;
      var networkId = pair.Key;

      LoggerProvider.LogInfoDebounced($"Pair Key: {networkId}, nodes: {nodes.Count}");

      if (nodes.Count == 0)
      {
        _networksToRemove.Add(networkId);
        continue;
      }

      nodes.RemoveAll(n => n == null);

      if (nodes.Count == 0)
      {
        _networksToRemove.Add(networkId);
        continue;
      }

      var isLoadedInZone = nodes.Any((x) => ZoneSystem.instance.IsZoneLoaded(x.GetPosition()));
      if (!isLoadedInZone)
        continue;

      if (ZNet.instance.IsServer())
      {
        Host_SimulateNetwork(networkId);
      }

      // only dedicated server.
      if (!ZNet.instance.IsDedicated())
      {
        Client_SyncActiveInstances();
        Client_SyncNetworkStats(networkId);
      }
    }

    foreach (var key in _networksToRemove)
    {
      powerNodeNetworks.Remove(key);
    }

    _networksToRemove.Clear();
  }
}