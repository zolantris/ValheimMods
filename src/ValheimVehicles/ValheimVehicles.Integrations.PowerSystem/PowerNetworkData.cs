using System;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
namespace ValheimVehicles.Integrations.PowerSystem;

/// <summary>
/// Generic power network data used for sharing single source of truth for lookups.
/// All dictionary values should lead to this for matching ZDO to data or inverse.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class PowerNetworkData : IEquatable<PowerNetworkData>
{
  public readonly ZDO Zdo;
  public readonly ZDOID Zdoid;
  public readonly PowerSystemComputeData Data;

  public PowerNetworkData(ZDO zdo, PowerSystemComputeData data)
  {
    Zdo = zdo ?? throw new ArgumentNullException(nameof(zdo));
    Data = data ?? throw new ArgumentNullException(nameof(data));
    Zdoid = zdo.m_uid;
  }

  public bool Equals(PowerNetworkData other)
  {
    return other != null && Zdoid == other.Zdoid;
  }

  public override bool Equals(object obj)
  {
    return obj is PowerNetworkData other && Equals(other);
  }

  public override int GetHashCode()
  {
    return Zdoid.GetHashCode();
  }
}