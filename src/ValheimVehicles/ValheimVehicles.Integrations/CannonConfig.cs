// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.Storage.Serialization;
using Zolantris.Shared;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public partial class CannonPersistentConfig : ISerializableConfig<CannonPersistentConfig, ICannonPersistentConfig>, ICannonPersistentConfig
  {
    public const string Key_AmmoVariant = "cannon_ammo_variant";
    public const string Key_CannonFiringMode = "cannon_firing_mode";

    public int GetStableHashCode()
    {
      unchecked
      {
        var hash = 17;
        hash = hash * 31 + CannonFiringMode.GetHashCode();
        hash = hash * 31 + AmmoVariant.GetHashCode();
        return hash;
      }
    }

    public void Serialize(ZPackage pkg)
    {
      if (pkg == null)
      {
        LoggerProvider.LogError("No package provided");
        return;
      }
      pkg.Write((int)CannonFiringMode);
      pkg.Write((int)AmmoVariant);

      pkg.SetPos(0);
    }

    public CannonPersistentConfig Deserialize(ZPackage pkg)
    {
      pkg.SetPos(0); // Always reset read pointer otherwise we start at end and fail.

      return new CannonPersistentConfig
      {
        CannonFiringMode = (CannonFiringMode)pkg.ReadInt(),
        AmmoVariant = (CannonballVariant)pkg.ReadInt()
      };
    }

    public void LoadByKey(ZDO zdo, ICannonPersistentConfig persistentConfig, string key)
    {
      switch (key)
      {
        case Key_AmmoVariant:
          persistentConfig.AmmoVariant = (CannonballVariant)zdo.GetInt(Key_AmmoVariant, (int)AmmoVariant);
          break;
        case Key_CannonFiringMode:
          persistentConfig.CannonFiringMode = (CannonFiringMode)zdo.GetInt(Key_CannonFiringMode, (int)CannonFiringMode);
          break;
        default:
          LoggerProvider.LogDebug($"CannonConfig: Unknown key: {key}");
          break;
      }
    }

    public void Save(ZDO zdo, CannonPersistentConfig persistentConfig, string[]? filterKeys)
    {
      if (filterKeys == null || filterKeys.Length == 0)
      {
        SaveAll(zdo, persistentConfig);
        return;
      }

      foreach (var filterKey in filterKeys)
      {
        SaveByKey(zdo, persistentConfig, filterKey);
      }
    }

    public void SaveAll(ZDO zdo, CannonPersistentConfig persistentConfig)
    {
      zdo.SetDelta(Key_AmmoVariant, (int)persistentConfig.AmmoVariant);
      zdo.SetDelta(Key_CannonFiringMode, (int)persistentConfig.CannonFiringMode);
    }

    public void SaveByKey(ZDO zdo, CannonPersistentConfig persistentConfig, string key)
    {
      switch (key)
      {
        case Key_AmmoVariant:
          zdo.Set(Key_AmmoVariant, (int)persistentConfig.AmmoVariant);
          break;
        case Key_CannonFiringMode:
          zdo.Set(Key_CannonFiringMode, (int)persistentConfig.CannonFiringMode);
          break;
        default:
          LoggerProvider.LogDebug($"SwivelConfig: Unknown key: {key}");
          break;
      }
    }

    public CannonPersistentConfig LoadAll(ZDO zdo, ICannonPersistentConfig persistentConfigFromComponent)
    {
      var newConfig = new CannonPersistentConfig
      {
        CannonFiringMode = (CannonFiringMode)zdo.GetInt(Key_CannonFiringMode, (int)persistentConfigFromComponent.CannonFiringMode),
        AmmoVariant = (CannonballVariant)zdo.GetInt(Key_AmmoVariant, (int)persistentConfigFromComponent.AmmoVariant)
      };
      return newConfig;
    }

    public void ApplyTo(ICannonPersistentConfig component)
    {
      component.CannonFiringMode = CannonFiringMode;
      component.AmmoVariant = AmmoVariant;
    }
    public void ApplyFrom(ICannonPersistentConfig component)
    {
      CannonFiringMode = component.CannonFiringMode;
      AmmoVariant = component.AmmoVariant;
    }

    public CannonPersistentConfig Load(ZDO zdo, ICannonPersistentConfig persistentConfigFromComponent, string[]? filterKeys = null)
    {
      if (filterKeys == null || filterKeys.Length == 0)
      {
        return LoadAll(zdo, persistentConfigFromComponent);
      }
      return LoadByKeys(zdo, persistentConfigFromComponent, filterKeys);
    }

    public CannonPersistentConfig LoadByKeys(ZDO zdo, ICannonPersistentConfig persistentConfigFromComponent, string[] filterKeys)
    {
      var config = new CannonPersistentConfig();
      config.ApplyFrom(persistentConfigFromComponent);

      foreach (var key in filterKeys)
      {
        LoadByKey(zdo, config, key);
      }

      return config;
    }

    public CannonballVariant AmmoVariant
    {
      get;
      set;
    }
    public CannonFiringMode CannonFiringMode
    {
      get;
      set;
    }
  }
}