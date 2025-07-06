using System;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
namespace ValheimVehicles.Integrations;

public class CannonControllerBridge : CannonController, Hoverable, Interactable, INetView
{
  public CannonConfigSync prefabConfigSync = null!;
  private bool _hasInitPrefabSync = false;
  public CannonConfig Config => this.GetOrCache(ref prefabConfigSync, ref _hasInitPrefabSync).Config;

  protected internal override sealed void Awake()
  {
    base.Awake();

    if (!prefabConfigSync)
    {
      this.GetOrCache(ref prefabConfigSync, ref _hasInitPrefabSync);
    }

    m_nview = GetComponent<ZNetView>();
  }

  protected internal override sealed void OnEnable()
  {
    OnAmmoChanged += OnAmmoUpdate;
  }

  protected internal override sealed void OnDisable()
  {
    OnAmmoChanged -= OnAmmoUpdate;
  }

  public void OnAmmoUpdate(int val)
  {
    if (!this.IsNetViewValid(out var nv)) return;
    if (!nv.HasOwner())
    {
      nv.ClaimOwnership();
    }

    // do not do anything for non-owner. Otherwise this could loop.
    if (!nv.IsOwner()) return;

    AmmoCount = val;

    // commit update and sync.
    prefabConfigSync.Request_CommitConfigChange(Config);
    prefabConfigSync.Load();
  }

  // todo Add a TryGetFuel similar to PowerHoverComponent.

  public string CannonballNameFromType()
  {
    return AmmoType == Cannonball.CannonballType.Explosive ? ModTranslations.VehicleCannon_CannonBallExplosive : ModTranslations.VehicleCannon_CannonBallSolid;
  }

  public string GetHoverText()
  {
    var s = $"{ModTranslations.PowerSource_Interact_AddOne} / {ModTranslations.SharedKeys_Hold} {ModTranslations.SharedKeys_AddMany}";
    s += $"\n{ModTranslations.SharedKeys_InteractAltAndPlace} {ModTranslations.VehicleCannon_SwapCannonBallType}";
    s += $"\n{ModTranslations.VehicleCannon_AmmoText}: {CannonballNameFromType()}\n";
    return s;
  }

  public string GetHoverName()
  {
    return "HOVER NAME";
  }
  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (hold)
    {
      AmmoCount += 10;
      return false;
    }

    if (alt)
    {
      AmmoType = AmmoType == Cannonball.CannonballType.Explosive ? Cannonball.CannonballType.Solid : Cannonball.CannonballType.Explosive;
      return true;
    }

    AmmoCount += 1;

    return true;
  }
  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }
  public ZNetView? m_nview
  {
    get;
    set;
  }
}