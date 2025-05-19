// PowerNetworkController.Visual
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public partial class PowerNetworkController
  {

    public static bool CanShowNetworkData = true;
    [SerializeField] private bool enableVisualWires = true;
    private readonly List<LineRenderer> _activeLines = new();

    public static string GetMechanismPowerConsumerStatus(bool hasPower)
    {
      var activationText = hasPower ? ModTranslations.PowerState_HasPower : ModTranslations.PowerState_NoPower;
      var activationColor = hasPower ? "yellow" : "red";
      return $"\n({ModTranslations.WithBoldText(activationText, activationColor)})";
    }

    public static string GetMechanismPowerSourceStatus(bool isActive)
    {
      var activationText = isActive ? ModTranslations.PowerState_HasPower : ModTranslations.PowerState_NoPower;
      var activationColor = isActive ? "yellow" : "red";
      return $"\n({ModTranslations.WithBoldText(activationText, activationColor)})";
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

    /// <summary>
    /// Meant for inline integration within Hoverable interfaces.
    /// </summary>
    public static string GetNetworkPowerStatusString(string networkId)
    {
      if (!CanShowNetworkData) return string.Empty;
      return TryNetworkPowerData(networkId, out var networkData) ? networkData.Cached_NetworkDataString : string.Empty;
    }

    private static string GenerateNetworkDataString(string networkId, PowerNetworkData data)
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