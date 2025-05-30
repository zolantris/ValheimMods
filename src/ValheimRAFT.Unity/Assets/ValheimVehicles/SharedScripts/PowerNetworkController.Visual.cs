// PowerNetworkController.Visual
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController
  {

    public static bool CanShowNetworkData = true;
    public static Dictionary<string, PowerSystemSimulator.PowerSystemDisplayData> PowerNetworkDataInstances = new();
    [SerializeField] private bool enableVisualWires = true;
    private readonly List<LineRenderer> _activeLines = new();

    public void Client_SimulateNetworkPowerAnimations()
    {

      // foreach (var powerNetworkDataInstance in PowerNetworkDataInstances)
      // {
      //   if (powerNetworkDataInstance.Value.NetworkConsumerPowerStatus == "Normal")
      //   {
      //     // run lightning burst for this network.
      //   }
      // }
      // 7. Lightning burst effect (visual only)
      // if (lightningBurstCoroutine != null && !isDemanding)
      // {
      //   StopCoroutine(lightningBurstCoroutine);
      //   lightningBurstCoroutine = null;
      // }

      // if (isDemanding)
      // {
      // if (lightningBurstCoroutine != null)
      // {
      //   StopCoroutine(lightningBurstCoroutine);
      //   lightningBurstCoroutine = null;
      // }
      // if (lightningBurstCoroutine != null) return;
      // lightningBurstCoroutine = StartCoroutine(ActivateLightningBursts());
      // }
    }

    public static string GetDrainMechanismActivationStatus(bool isActive, bool hasPlayersWithEitr)
    {
      var text = "";
      if (!isActive)
      {
        text = ModTranslations.PowerState_Inactive;
      }
      else if (isActive && !hasPlayersWithEitr)
      {
        text = ModTranslations.PowerState_Inactive_NoEitrOnPlayers;
      }
      else
      {
        text = ModTranslations.PowerState_Active;
      }

      return ModTranslations.WithBoldText(text, isActive && hasPlayersWithEitr ? "yellow" : "red");
    }

    /// <summary>
    /// For showing activator power items. Either active or inactive status.
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public static string GetMechanismActivationStatus(bool isActive)
    {
      var activationText = isActive ? ModTranslations.PowerState_Active : ModTranslations.PowerState_Inactive;
      var activationColor = isActive ? "yellow" : "red";
      return ModTranslations.WithBoldText(activationText, activationColor);
    }

    /// <summary>
    /// For showing "power" diction. Usually relates to devices that require power.
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public static string GetMechanismRequiredPowerStatus(bool isActive)
    {
      // infinity text. aka "\u221E"
      if (!SwivelComponent.IsPoweredSwivel) return ModTranslations.WithBoldText("∞", "yellow");
      var activationText = isActive ? ModTranslations.PowerState_HasPower : ModTranslations.PowerState_NoPower;
      var activationColor = isActive ? "yellow" : "red";
      return ModTranslations.WithBoldText(activationText, activationColor);
    }


    /// <summary>
    /// Consumers must be not demanding or demanding and active. To be healthy/powered.
    /// </summary>
    /// <returns></returns>
    public static string GetNetworkHealthStatus(List<IPowerConsumer> consumers)
    {
      if (consumers.Count == 0) return ModTranslations.Power_NetworkInfo_NetworkFullPower;

      var poweredOrInactiveConsumers = 0;
      var inactiveDemandingConsumers = 0;

      foreach (var powerConsumer in consumers)
      {
        if (!powerConsumer.IsActive && powerConsumer.IsDemanding)
        {
          poweredOrInactiveConsumers++;
        }
        else
        {
          inactiveDemandingConsumers++;
        }
      }

      if (inactiveDemandingConsumers == 0) return ModTranslations.Power_NetworkInfo_NetworkFullPower;
      if (inactiveDemandingConsumers > 0 && poweredOrInactiveConsumers > 0) return ModTranslations.Power_NetworkInfo_NetworkPartialPower;

      return ModTranslations.Power_NetworkInfo_NetworkLowPower;
    }

    public static bool TryNetworkPowerData(string networkId, [NotNullWhen(true)] out PowerSystemSimulator.PowerSystemDisplayData? _data)
    {
      if (networkId == null)
      {
        LoggerProvider.LogInfoDebounced("NetworkId is null");
        _data = null;
        return false;
      }

      if (PowerNetworkDataInstances.TryGetValue(networkId, out _data))
      {
        return true;
      }

      _data = null;
      return false;
    }

    public static void UpdateNetworkPowerData(string networkId, PowerSystemSimulator.PowerSystemDisplayData data)
    {
      PowerNetworkDataInstances[networkId] = data;
    }
    /// <summary>
    /// Meant for inline integration within Hoverable interfaces.
    /// </summary>
    public static string GetNetworkPowerStatusString(string networkId)
    {
      if (!CanShowNetworkData) return string.Empty;
      return TryNetworkPowerData(networkId, out var networkData) ? networkData.Cached_NetworkDataString : string.Empty;
    }

    public static string GenerateNetworkDataString(string networkId, PowerSystemSimulator.PowerSystemDisplayData data)
    {
      var baseString = "";
      baseString += "\n";
      baseString += $"\n{ModTranslations.WithBoldText(ModTranslations.Power_NetworkInfo_NetworkData)}";
      baseString += $"\n{ModTranslations.WithBoldText(ModTranslations.Power_NetworkInfo_NetworkStatus, "yellow")}: {data.NetworkConsumerPowerStatus}";
      baseString += $"\n{ModTranslations.WithBoldText(ModTranslations.Power_NetworkInfo_NetworkId, "yellow")}: {networkId}";
      baseString += $"\n{ModTranslations.WithBoldText(ModTranslations.Power_NetworkInfo_NetworkPower, "yellow")}: {data.NetworkPowerSupply}/{data.NetworkPowerCapacity}";
      baseString += $"\n{ModTranslations.WithBoldText(ModTranslations.Power_NetworkInfo_NetworkFuel, "yellow")}: {data.NetworkFuelSupply}/{data.NetworkFuelCapacity}";
      baseString += $"\n{ModTranslations.WithBoldText(ModTranslations.Power_NetworkInfo_NetworkDemand, "yellow")}: {data.NetworkPowerDemand}";

      return baseString;
    }

    private static string GenerateNetworkDataString(string? networkId)
    {
      if (!CanShowNetworkData || networkId == null || !TryNetworkPowerData(networkId, out var networkData)) return string.Empty;
      return GenerateNetworkDataString(networkId, networkData);
    }

    protected void ClearVisualWires()
    {
      if (!enableVisualWires) return;
      foreach (var line in _activeLines)
      {
        if (line != null)
        {
          Destroy(line.gameObject);
        }
      }
      _activeLines.Clear();
    }

    protected void AddChainLine(Vector3[] points)
    {
      if (!enableVisualWires) return;
      var obj = new GameObject("PylonVisualLine");
      obj.transform.SetParent(transform, false);
      var line = obj.AddComponent<LineRenderer>();

      line.material = WireMaterial;
      line.widthMultiplier = 0.02f;
      line.positionCount = points.Length;
      line.SetPositions(points);

      _activeLines.Add(line);
    }
  }
}