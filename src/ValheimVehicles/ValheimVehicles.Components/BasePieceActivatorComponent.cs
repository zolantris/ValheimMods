#region

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using UnityEngine;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Integrations;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Shared.Constants;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.Structs;
  using ZdoWatcher;
  using Zolantris.Shared;
  using static ValheimVehicles.Prefabs.Registry.RamPrefabRegistry;

#endregion

  namespace ValheimVehicles.Components;

  public abstract class BasePieceActivatorComponent : MonoBehaviour
  {
    protected CoroutineHandle _pendingPiecesCoroutine;
    public PendingPieceStateEnum pieceState;
    protected bool _pendingPiecesDirty;
    protected List<ZNetView> _newPendingPiecesQueue = new();
    protected Stopwatch PendingPiecesTimer = new();


    /// <summary>
    /// Temp pieces within a moving object. These could move outside a vehicle/swivel.
    /// </summary>
    public static Dictionary<int, List<ActivationPieceData>> m_pendingTempPieces = new();

    public abstract IPieceActivatorHost Host { get; }

    public static readonly Dictionary<int, List<ZNetView>> m_pendingPieces = new();

    public bool CanActivatePendingPieces => _isInitComplete && Host != null && Host.GetPersistentId() != 0;

    public bool IsPendingPieceActivationRunning => _pendingPiecesCoroutine.IsRunning;

    private CoroutineHandle _initPersistentIdCoroutine;
    private bool _isInitComplete = false;
    public Action<PendingPieceStateEnum> OnActivationComplete = (val) =>
    {
      LoggerProvider.LogWarning("No OnActivationComplete assigned");
    };
    public Action OnInitComplete = () =>
    {
      LoggerProvider.LogWarning("No OnInitComplete assigned");
    };

    public bool IsInitialActivationComplete => _isInitComplete;

    protected abstract void TrySetPieceToParent(ZNetView netView);
    protected abstract void AddPiece(ZNetView netView, bool isNewPiece = false);

    public void StartInitPersistentId()
    {
      _initPersistentIdCoroutine.Start(InitPersistentIdRoutine());
    }

    protected IEnumerator InitPersistentIdRoutine()
    {
      while (Host == null || Host.GetNetView() == null || Host.GetPersistentId() == 0)
      {
        yield return null;
      }
      _isInitComplete = true;

      OnInitComplete.Invoke();
    }

    public void OnEnable()
    {
      _pendingPiecesCoroutine ??= new CoroutineHandle(this);
      _initPersistentIdCoroutine ??= new CoroutineHandle(this);
    }

    public void OnDisable()
    {
      _pendingPiecesCoroutine?.Stop();
      _initPersistentIdCoroutine?.Stop();
    }


    public void StartActivatePendingPieces()
    {
      if (!CanActivatePendingPieces || !isActiveAndEnabled || ZNet.instance == null || ZNetScene.instance == null) return;

      var id = Host.GetPersistentId();

      if (!m_pendingPieces.TryGetValue(id, out var pending) || pending.Count == 0)
      {
        OnActivationComplete?.Invoke(pieceState);
        return;
      }

      if (!_pendingPiecesCoroutine.IsRunning)
      {
        _pendingPiecesCoroutine?.Start(ActivatePendingPiecesCoroutine());
      }
    }

    public IEnumerator ActivatePendingPiecesCoroutine()
    {
      pieceState = PendingPieceStateEnum.Running;
      PendingPiecesTimer.Restart();

      var persistentId = Host.GetPersistentId();
      if (persistentId == 0)
      {
        OnActivationComplete?.Invoke(pieceState);
        yield break;
      }

      var currentPieces = m_pendingPieces.TryGetValue(persistentId, out var list) ? list : null;

      if (currentPieces == null || currentPieces.Count == 0)
      {
        pieceState = PendingPieceStateEnum.Complete;
        OnActivationComplete?.Invoke(pieceState);
        yield break;
      }

      do
      {
        if (ZNetScene.instance != null && ZNetScene.instance.InLoadingScreen())
          yield return new WaitForFixedUpdate();

        if (Host.GetNetView() == null)
        {
          pieceState = PendingPieceStateEnum.ForceReset;
          OnActivationComplete?.Invoke(pieceState);
          yield break;
        }

        _pendingPiecesDirty = false;

        foreach (var piece in currentPieces.ToList())
        {
          ActivatePiece(piece);
        }

        currentPieces.Clear();

        if (_newPendingPiecesQueue.Count > 0)
        {
          currentPieces.AddRange(_newPendingPiecesQueue);
          _newPendingPiecesQueue.Clear();
          _pendingPiecesDirty = true;
        }

      } while (_pendingPiecesDirty);

      pieceState = PendingPieceStateEnum.Complete;
      OnActivationComplete?.Invoke(pieceState);
    }

    protected void FinalizeTransform(ZNetView netView)
    {
      if (netView == null || netView.m_zdo == null) return;

      var t = netView.transform;
      t.localPosition = netView.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
      t.localRotation = Quaternion.Euler(netView.m_zdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero));

      if (netView.TryGetComponent<WearNTear>(out var wnt))
        wnt.enabled = true;
    }
    public void ActivatePiece(ZNetView netView)
    {
      TrySetPieceToParent(netView);
      FinalizeTransform(netView);
      AddPiece(netView);
    }


    public static int GetSwivelParentId(ZDO zdo)
    {
      var id = zdo.GetInt(VehicleZdoVars.SwivelParentId);
      return id;
    }

    public static void AddPendingPiece(int parentId, ZNetView netView, bool skipActivation = false, bool isVehicle = false, bool isSwivel = false)
    {
      if (!ValheimExtensions.IsCurrentGameHealthy()) return;
      if (netView == null || netView.GetZDO() == null) return;
      if (!m_pendingPieces.TryGetValue(parentId, out var list) || list.Count == 0)
      {
        list = [netView];

        // must set the list.
        m_pendingPieces[parentId] = list;
        return;
      }

      if (!list.Contains(netView))
      {
        list.Add(netView);
      }

      if (!skipActivation && isVehicle)
      {
        if (VehiclePiecesController.ActiveInstances.TryGetValue(parentId, out var vehicle))
        {
          vehicle.StartActivatePendingPieces();
        }
      }

      if (!skipActivation && isSwivel)
      {
        if (SwivelComponentBridge.ActiveInstances.TryGetValue(parentId, out var swivel))
        {
          swivel.StartActivatePendingSwivelPieces();
        }
      }
    }

    private static bool TryInitSwivelParentPiece(ZNetView netView, ZDO zdo)
    {
      var id = GetSwivelParentId(zdo);
      if (id == 0) return false;

      var parentObj = ZdoWatchController.Instance.GetGameObject(id);

      var swivelPieceActivator = parentObj == null ? null : parentObj.GetComponent<SwivelPieceActivator>();
      if (swivelPieceActivator != null)
      {
        swivelPieceActivator.ActivatePiece(netView);
        return true;
      }

      // If the ZDO object is not loaded add it to a Pending Piece.
      AddPendingPiece(id, netView, false, false, true);
      return false;
    }

    private static bool TryInitVehicleParentPiece(ZNetView netView, ZDO zdo)
    {
      var id = VehiclePiecesController.GetParentID(zdo);
      if (id == 0) return false;

      var parentObj = ZdoWatchController.Instance.GetGameObject(id);

      var vehicleBaseController = parentObj == null ? null : parentObj.GetComponent<VehicleManager>();
      if (vehicleBaseController != null && vehicleBaseController.PiecesController != null)
      {
        vehicleBaseController.PiecesController.ActivatePiece(netView);
        return true;
      }

      VehiclePiecesController.AddInactivePiece(id, netView, null);

      return false;
    }

    public static bool IsExcludedPrefab(GameObject netView)
    {
      if (PrefabNames.IsVehicle(netView.name) ||
          netView.name.StartsWith(PrefabNames.VehiclePiecesContainer))
        return true;

      return false;
    }

    public static void InitPiece(ZNetView netView)
    {
      if (!ValheimExtensions.IsCurrentGameHealthy())
      {
        return;
      }

      if (netView == null) return;
      if (VehiclePiecesController.TryInitTempPiece(netView)) return;

      var isPiecesOrWaterVehicle = IsExcludedPrefab(netView.gameObject);

      if (isPiecesOrWaterVehicle) return;

      var rb = netView.GetComponentInChildren<Rigidbody>();
      if ((bool)rb && !rb.isKinematic && !IsRam(netView.name)) return;

      var zdo = netView.GetZDO();
      if (zdo == null) return;

      if (TryInitSwivelParentPiece(netView, zdo)) return;
      if (TryInitVehicleParentPiece(netView, zdo)) return;

      // todo other logic;
    }
  }