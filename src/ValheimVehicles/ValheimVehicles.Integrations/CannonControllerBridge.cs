using System;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
namespace ValheimVehicles.Integrations;

public class CannonControllerBridge : CannonController, Hoverable, Interactable, INetView
{
  public CannonConfigSync prefabConfigSync = null!;
  private bool _hasInitPrefabSync = false;
  public CannonPersistentConfig PersistentConfig => this.GetOrCache(ref prefabConfigSync, ref _hasInitPrefabSync).Config;

  public AmmoController? ammoController;

  protected internal override sealed void Awake()
  {
#if VALHEIM
    CannonballSolidPrefab = CannonPrefabs.CannonballSolidProjectile;
    CannonballExplosivePrefab = CannonPrefabs.CannonballExplosiveProjectile;
#endif

    base.Awake();

    m_nview = GetComponent<ZNetView>();
    if (!m_nview)
    {
      m_nview = GetComponentInParent<ZNetView>();
    }

    if (!prefabConfigSync)
    {
      this.GetOrCache(ref prefabConfigSync, ref _hasInitPrefabSync);
    }
  }

  protected internal override void Start()
  {
    base.Start();
    ammoController = GetComponent<AmmoController>();
  }

  public void SetAmmoType()
  {

  }

  protected internal override sealed void OnEnable()
  {
    base.OnEnable();
    OnAmmoChanged += OnAmmoUpdate;
  }

  protected internal override sealed void OnDisable()
  {
    base.OnDisable();
    OnAmmoChanged -= OnAmmoUpdate;
  }

  public void OnAmmoUpdate(int val)
  {
    // if (!this.IsNetViewValid(out var nv)) return;
    // if (!nv.HasOwner())
    // {
    //   nv.ClaimOwnership();
    // }
    //
    // // do not do anything for non-owner. Otherwise this could loop.
    // if (!nv.IsOwner()) return;
    //
    // AmmoCount = val;
    //
    // // commit update and sync.
    // prefabConfigSync.Request_CommitConfigChange(PersistentConfig);
    // prefabConfigSync.Load();
  }

  // todo Add a TryGetFuel similar to PowerHoverComponent.

  public string CannonballNameFromType()
  {
    return AmmoVariant == CannonballVariant.Explosive ? ModTranslations.VehicleCannon_CannonBallExplosive : ModTranslations.VehicleCannon_CannonBallSolid;
  }

  public string GetHoverText()
  {
    // var s = $"{ModTranslations.PowerSource_Interact_AddOne} / {ModTranslations.SharedKeys_Hold} {ModTranslations.SharedKeys_AddMany}";
    var s = "";
    s += $"\n{ModTranslations.SharedKeys_InteractAlt} {ModTranslations.VehicleCannon_SwapCannonBallType}";

    s += $"\n{ModTranslations.VehicleCannon_AmmoText}: {CannonballNameFromType()}";

    if (ammoController != null)
    {
      s += $"({ammoController.GetAmmoAmountFromCannonballVariant(AmmoVariant)})";
    }

    if (!hasNearbyPowderBarrel)
    {
      s += $"\n{ModTranslations.VehicleCannon_CannonMissingNearbyPowderBarrel}";
    }
    return s;
  }

  public string GetHoverName()
  {
    return "CannonController Hover (Should not be visible)";
  }
  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (hold)
    {
      // AmmoCount += 10;
      return false;
    }

    if (alt)
    {
      AmmoVariant = AmmoVariant == CannonballVariant.Explosive ? CannonballVariant.Solid : CannonballVariant.Explosive;
      return true;
    }

    // AmmoCount += 1;

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