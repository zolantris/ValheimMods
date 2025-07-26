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

  public override CannonballVariant AmmoVariant
  {
    get => PersistentConfig.AmmoVariant;
    set => OnAmmoVariantUpdate(value);
  }

  public override void OnAmmoVariantUpdate(CannonballVariant variant)
  {
    base.OnAmmoVariantUpdate(variant);

    PersistentConfig.AmmoVariant = variant;
    if (this.IsNetViewValid(out var nv))
    {
      prefabConfigSync.Save([CannonPersistentConfig.Key_AmmoType]);
    }
  }

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
    prefabConfigSync.Load([]);
    ammoController = GetComponent<AmmoController>();
  }

  protected internal override sealed void OnEnable()
  {
    base.OnEnable();
  }

  protected internal override sealed void OnDisable()
  {
    base.OnDisable();
  }

  // todo Add a TryGetFuel similar to PowerHoverComponent.

  public string CannonballNameFromType()
  {
    return AmmoVariant == CannonballVariant.Explosive ? ModTranslations.VehicleCannon_CannonBallItemExplosive : ModTranslations.VehicleCannon_CannonBallItemSolid;
  }

  public void ToggleAmmoVariant()
  {
    var nextVariant = AmmoVariant == CannonballVariant.Explosive ? CannonballVariant.Solid : CannonballVariant.Explosive;
    AmmoVariant = nextVariant;
  }


  public string GetHoverText()
  {
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
      return false;
    }

    if (alt)
    {
      ToggleAmmoVariant();
      return true;
    }

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