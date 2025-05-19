namespace ValheimVehicles.Interfaces;

/// <summary>
/// These actions can be called and are delegated to either client or host syncs
/// </summary>
public interface IPrefabConfigActions
{
  internal void Load(bool forceUpdate = false);
}