#region

  using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Enums;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;

#endregion

namespace ValheimVehicles.Controllers;

public class VehicleConfigSyncComponent : MonoBehaviour, IVehicleSharedProperties
{
  private VehicleBaseController _vehicle;
  private ZNetView? m_nview => _vehicle.NetView;
  private BoxCollider? FloatCollider => _vehicle.FloatCollider;
  private bool _hasRegister;
  private RetryGuard _rpcRegisterRetry;
  private VehicleFloatationMode _cachedFloatationMode = VehicleFloatationMode.Average;
  private float _cachedFloatationHeight = 0;

  public VehicleCustomZdoConfig CustomZdoConfig = new();

  public void Awake()
  {
    _rpcRegisterRetry = new RetryGuard(this);
    _vehicle = GetComponent<VehicleBaseController>();
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
    if (!IsValid(out var netView))
    {
      _rpcRegisterRetry.Retry(RegisterRPCListeners, 1);
      return;
    }
    if (_hasRegister) return;
    // ship piece bounds syncing
    netView.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);

    // all config sync
    netView.Register<ZPackage>(nameof(RPC_SetVehicleConfig), RPC_SetVehicleConfig);
    netView.Register(nameof(RPC_SyncVehicleConfig), RPC_SyncVehicleConfig);

    VehicleCustomZdoConfig.LoadVehicleConfig(netView.GetZDO());

    _hasRegister = true;
  }
  
  private void UnregisterRPCListeners()
  {
    if (!IsValid(out var netView))
    {
      _hasRegister = false;
      return;
    }

    if (!_hasRegister) return;

    // ship piece bounds syncing
    netView.Unregister(nameof(RPC_SyncBounds));

    // all configs
    netView.Unregister(nameof(RPC_SetVehicleConfig));
    netView.Unregister(nameof(RPC_SyncVehicleConfig));

    _hasRegister = false;
  }

  public void SendVehicleConfig()
  {
    if (!IsValid(out var netView)) return;
    var package = new ZPackage();
    CustomZdoConfig.Serialize(package);
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
    var localVehicleConfig = VehicleCustomZdoConfig.Deserialize(package);
    LoggerProvider.LogDebug($"Received vehicleConfig: {CustomZdoConfig}");
    CustomZdoConfig = localVehicleConfig;

    if (!netView.HasOwner() || netView.IsOwner())
    {
      VehicleCustomZdoConfig.SaveVehicleConfig(netView.GetZDO(), CustomZdoConfig);
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
    CustomZdoConfig = VehicleCustomZdoConfig.LoadVehicleConfig(netView.GetZDO());
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
  
  public VehicleFloatationMode GetWaterFloatationHeightMode()
  {
    if (CustomZdoConfig.HasCustomFloatationHeight) return VehicleFloatationMode.Custom;
    return PhysicsConfig.HullFloatationColliderLocation.Value;
  }

  public void SendSyncFloatationMode(bool isCustom, float relativeHeight)
  {
    // do nothing for land-vehicles.
    if (_vehicle.IsLandVehicle) return;
    
    // set config immediately, but it might not be updated if the ranges are out of bounds
    CustomZdoConfig.HasCustomFloatationHeight = isCustom;
    CustomZdoConfig.CustomFloatationHeight = relativeHeight;

    SendVehicleConfig();
  }

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

#region IVehicleSharedProperties

  public VehiclePiecesController? PiecesController
  {
    get;
    set;
  }
  public VehicleMovementController? MovementController
  {
    get;
    set;
  }
  public VehicleConfigSyncComponent? VehicleConfigSync
  {
    get;
    set;
  }
  public VehicleOnboardController? OnboardController
  {
    get;
    set;
  }
  public VehicleWheelController? WheelController
  {
    get;
    set;
  }
  public VehicleBaseController? BaseController
  {
    get;
    set;
  }
  
  public ZNetView? NetView
  {
    get;
    set;
  }

#endregion
  
}