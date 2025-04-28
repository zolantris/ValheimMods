using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Enums;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Controllers;

public class VehicleConfigSyncComponent : MonoBehaviour
{
  private VehicleShip _vehicle;
  private ZNetView? m_nview => _vehicle.NetView;
  private VehiclePiecesController? PiecesController => _vehicle.PiecesController;
  private VehicleOnboardController? OnboardController => _vehicle.OnboardController;
  private BoxCollider? FloatCollider => _vehicle.FloatCollider;
  private bool _hasRegister;
  private RetryGuard _rpcRegisterRetry;
  private VehicleFloatationMode _cachedFloatationMode = VehicleFloatationMode.Average;
  private float _cachedFloatationHeight = 0;

  public VehicleConfig config = new();

  public void Awake()
  {
    _rpcRegisterRetry = new RetryGuard(this);
    _vehicle = GetComponent<VehicleShip>();
  }

  private void OnEnable()
  {
    RegisterRPCListeners();
  }

  private void OnDisable()
  {
    UnregisterRPCListeners();
  }

  private void RegisterRPCListeners()
  {
    // retry guards. Sometimes things are not available on Awake().
    if (m_nview == null)
    {
      _rpcRegisterRetry.Retry(RegisterRPCListeners, 1);
      return;
    }
    if (_hasRegister) return;
    // ship piece bounds syncing
    m_nview.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);

    // all config sync
    m_nview.Register<ZPackage>(nameof(RPC_SetVehicleConfig), RPC_SetVehicleConfig);
    m_nview.Register(nameof(RPC_SyncVehicleConfig), RPC_SyncVehicleConfig);

    // piece float mode syncing
    // m_nview.Register<bool>(nameof(RPC_SyncWaterFloatationMode), RPC_SyncWaterFloatationMode);

    // ship water floatation syncing
    // m_nview.Register(nameof(RPC_SyncWaterFloatationHeight), RPC_SyncWaterFloatationHeight);

    _hasRegister = true;
  }
  
  private void UnregisterRPCListeners()
  {
    if (!_hasRegister || m_nview == null)
    {
      _hasRegister = false;
      return;
    }

    // ship piece bounds syncing
    m_nview.Unregister(nameof(RPC_SyncBounds));

    // all configs
    m_nview.Unregister(nameof(RPC_SetVehicleConfig));
    m_nview.Unregister(nameof(RPC_SyncVehicleConfig));

    // piece float mode syncing
    // m_nview.Unregister(nameof(RPC_SyncWaterFloatationMode));

    // ship water floatation height
    // m_nview.Unregister(nameof(RPC_SyncWaterFloatationHeight));

    _hasRegister = false;
  }

  public void SendVehicleConfig()
  {
    if (!IsValid(out var netView)) return;
    var package = new ZPackage();
    config.Serialize(package);
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SetVehicleConfig), package);
  }

  /// <summary>
  /// The main function to sync config. Only applies for the NetView owner.
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="package"></param>
  public void RPC_SetVehicleConfig(long sender, ZPackage package)
  {
    if (!IsValid(out var netView)) return;
    var localVehicleConfig = VehicleConfig.Deserialize(package);
    LoggerProvider.LogDebug($"Received vehicleConfig: {config}");
    config = localVehicleConfig;

    if (!netView.HasOwner() || netView.IsOwner())
    {
      VehicleConfig.SaveVehicleConfig(netView.GetZDO(), config);
    }

    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SyncVehicleConfig), package);
  }

  public void RPC_SyncVehicleConfig(long sender)
  {
    SyncVehicleConfig();
  }

  public void SyncVehicleConfig()
  {
    if (!IsValid(out var netView)) return;
    config = VehicleConfig.LoadVehicleConfig(netView.GetZDO());
  }

  public bool IsValid()
  {
    if (!isActiveAndEnabled || m_nview == null || m_nview.GetZDO() == null) return false;
    return true;
  }

  /// <summary>
  /// Guards on ZNetView and ZDO
  /// </summary>
  /// <param name="netView"></param>
  /// <returns></returns>
  public bool IsValid([NotNullWhen(true)] out ZNetView netView)
  {
    netView = m_nview;
    if (IsValid()) return false;
    return true;
  }

  // public void SetWaterFloatHeight(float waterFloatHeight)
  // {
  //   if (!IsValid(out var netView)) return;
  //   netView.GetZDO().Set(VehicleZdoVars.VehicleFloatationHeight, waterFloatHeight);
  //   netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SyncWaterFloatationHeight));
  // }
  //
  public VehicleFloatationMode GetWaterFloatationHeightMode()
  {
    if (config.HasCustomFloatationHeight) return VehicleFloatationMode.Custom;
    return PhysicsConfig.HullFloatationColliderLocation.Value;
  }

  // {
  // if (!IsValid()) return PhysicsConfig.HullFloatationColliderLocation.Value;
  // var vehicleFloatationCustomModeEnabled = m_nview!.GetZDO().GetBool(VehicleZdoVars.VehicleFloatationCustomModeEnabled, false);
  // return vehicleFloatationCustomModeEnabled ? VehicleFloatationMode.Custom : PhysicsConfig.HullFloatationColliderLocation.Value;
  // }

  public void SendSyncFloatationMode(bool isCustom, float relativeHeight)
  {
    // set config immediately, but it might not be updated if the ranges are out of bounds
    config.HasCustomFloatationHeight = isCustom;
    config.CustomFloatationHeight = relativeHeight;

    var shouldUpdate = config.HasCustomFloatationHeight != isCustom || !Mathf.Approximately(config.CustomFloatationHeight, relativeHeight);
    if (!shouldUpdate)
    {
      return;
    }

    SendVehicleConfig();
  }
// floatation Sync

  // public void SendSyncFloatationMode(bool isCustom, float relativeHeight)
  // {
  //   if (!IsValid(out var netView)) return;
  //   netView.GetZDO().Set(VehicleZdoVars.VehicleFloatationCustomModeEnabled, isCustom);
  //   netView.InvokeRPC(nameof(RPC_SyncWaterFloatationMode), isCustom);
  //
  //   if (!isCustom)
  //   {
  //     return;
  //   }
  //
  //   // we want to set the new height if enabling the custom height
  //   SetWaterFloatHeight(relativeHeight);
  // }

  // public void RPC_SyncWaterFloatationHeight(long sender)
  // {
  //   if (!PiecesController) return;
  //   SyncWaterFloatationHeight();
  // }

  // public void RPC_SyncWaterFloatationMode(long sender, bool isCustom)
  // {
  //   if (!IsValid()) return;
  //   if (!PiecesController) return;
  //   SyncWaterFloatationMode();
  //   SyncWaterFloatationHeight();
  // }

  // public void SyncWaterFloatationHeight()
  // {
  //   if (!PiecesController || FloatCollider == null) return;
  //  
  //   var center = FloatCollider.center;
  //   _cachedFloatationHeight = GetWaterFloatationHeight(center.y);
  //
  //   center.y = _cachedFloatationHeight;
  //   FloatCollider.center = center;
  // }

  // public void SyncWaterFloatationMode()
  // {
  //   _cachedFloatationMode = GetWaterFloatationHeightMode();
  // }

  // public float GetWaterFloatationHeight(float fallbackValue = 0)
  // {
  //   if (!IsValid(out var netView)) return fallbackValue;
  //   var floatationHeight = netView.GetZDO().GetFloat(VehicleZdoVars.VehicleFloatationHeight, fallbackValue);
  //   return floatationHeight;
  // }

  // bounds sync

  /// <summary>
  /// Todo integrate this and confirm it works. This will help avoid any one player from updating too quickly.
  ///
  /// Also should prevent desyncs if we can synchronize it.
  /// </summary>
  public void SendSyncBounds()
  {
    if (!IsValid(out var netView) || OnboardController == null) return;

    OnboardController.m_localPlayers.ForEach(x =>
    {
      netView.InvokeRPC(x.GetPlayerID(), nameof(RPC_SyncBounds));
    });
  }

  /// <summary>
  ///   Forces a resync of bounds for all players on the ship, this may need to be
  ///   only from host but then would require syncing all collider data that updates
  ///   in the OnBoundsUpdate
  /// </summary>
  public void RPC_SyncBounds(long sender)
  {
    if (!PiecesController) return;
    SyncVehicleBounds();
  }

  public void SyncVehicleBounds()
  {
    if (PiecesController == null) return;
    PiecesController.RequestBoundsRebuild();
  }
}