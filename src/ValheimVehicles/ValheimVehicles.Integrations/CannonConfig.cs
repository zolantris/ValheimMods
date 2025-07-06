// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.Storage.Serialization;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public partial class CannonConfig : ISerializableConfig<CannonConfig, ICannonConfig>, ICannonConfig
  {
    public const string Key_AmmoType = "cannon_ammo_type";
    public const string Key_AmmoCount = "cannon_ammo_count";

    public int GetStableHashCode()
    {
      unchecked
      {
        var hash = 17;
        hash = hash * 31 + AmmoCount.GetHashCode();
        hash = hash * 31 + (int)AmmoType;
        return hash;
      }
    }

    public void Serialize(ZPackage pkg)
    {
      throw new System.NotImplementedException();
    }
    public CannonConfig Deserialize(ZPackage pkg)
    {
      pkg.SetPos(0); // Always reset read pointer otherwise we start at end and fail.

      return new CannonConfig
      {
        AmmoCount = pkg.ReadInt(),
        AmmoType = (Cannonball.CannonballType)pkg.ReadInt()
      };
    }

    public void LoadByKey(ZDO zdo, ICannonConfig config, string key)
    {
      switch (key)
      {
        case Key_AmmoCount:
          config.AmmoCount = zdo.GetInt(Key_AmmoCount, config.AmmoCount);
          break;
        case Key_AmmoType:
          config.AmmoType = (Cannonball.CannonballType)zdo.GetInt(Key_AmmoType, (int)config.AmmoType);
          break;
        default:
          LoggerProvider.LogDebug($"CannonConfig: Unknown key: {key}");
          break;
      }
    }
    public void Save(ZDO zdo, CannonConfig config, string[]? filterKeys)
    {
      if (filterKeys == null || filterKeys.Length == 0)
      {
        SaveAll(zdo, config);
        return;
      }

      foreach (var filterKey in filterKeys)
      {
        SaveByKey(zdo, config, filterKey);
      }
    }

    public void SaveAll(ZDO zdo, CannonConfig config)
    {
      zdo.SetDelta(Key_AmmoType, (int)config.AmmoType);
      zdo.SetDelta(Key_AmmoCount, config.AmmoCount);
    }

    public void SaveByKey(ZDO zdo, CannonConfig config, string key)
    {
      switch (key)
      {
        case Key_AmmoType:
          zdo.Set(Key_AmmoType, (int)config.AmmoType);
          break;
        case Key_AmmoCount:
          zdo.Set(Key_AmmoCount, config.AmmoCount);
          break;
        default:
          LoggerProvider.LogDebug($"SwivelConfig: Unknown key: {key}");
          break;
      }
    }

    public CannonConfig LoadAll(ZDO zdo, ICannonConfig configFromComponent)
    {
      var newConfig = new CannonConfig
      {
        AmmoCount = zdo.GetInt(Key_AmmoCount, configFromComponent.AmmoCount),
        AmmoType = (Cannonball.CannonballType)zdo.GetInt(Key_AmmoType, (int)configFromComponent.AmmoType)
      };
      return newConfig;
    }

    public void ApplyTo(ICannonConfig component)
    {
      component.AmmoCount = AmmoCount;
      component.AmmoType = AmmoType;
    }
    public void ApplyFrom(ICannonConfig component)
    {
      AmmoCount = component.AmmoCount;
      AmmoType = component.AmmoType;
    }

    public CannonConfig Load(ZDO zdo, ICannonConfig configFromComponent, string[]? filterKeys = null)
    {
      if (filterKeys == null || filterKeys.Length == 0)
      {
        return LoadAll(zdo, configFromComponent);
      }
      return LoadByKeys(zdo, configFromComponent, filterKeys);
    }

    public CannonConfig LoadByKeys(ZDO zdo, ICannonConfig configFromComponent, string[] filterKeys)
    {
      var config = new CannonConfig();
      config.ApplyFrom(configFromComponent);

      foreach (var key in filterKeys)
      {
        LoadByKey(zdo, config, key);
      }

      return config;
    }

    public int AmmoCount
    {
      get;
      set;
    }
    public Cannonball.CannonballType AmmoType
    {
      get;
      set;
    }
  }
}