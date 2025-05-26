// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{

  public interface IPowerSystemEntityData
  {
    float ConnectionRange { get; set; }
    string NetworkId { get; set; }
    int PrefabHash { get; set; }

    // actions
    Action? OnLoad { get; set; }
    Action OnSave { get; set; }
    Action? OnActive { get; set; }

    public void MarkDirty(string zdoKey);
    public void ClearDirty();

    // internal methods that call actions for ZDOs
    public void Load();
    public void Save();

    // state setters for visuals
    public bool IsActive { get; }
  }
}