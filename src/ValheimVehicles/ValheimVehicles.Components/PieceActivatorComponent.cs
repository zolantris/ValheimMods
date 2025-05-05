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
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.Structs;
  using ZdoWatcher;
  using static ValheimVehicles.Prefabs.Registry.RamPrefabs;

#endregion

  namespace ValheimVehicles.Components;

  public abstract class BasePieceActivatorComponent : MonoBehaviour
  {
    protected Coroutine? _pendingPiecesCoroutine;
    protected PendingPieceStateEnum _pendingPiecesState;
    protected bool _pendingPiecesDirty;
    protected List<ZNetView> _newPendingPiecesQueue = new();
    protected Stopwatch PendingPiecesTimer = new();

    public abstract IPieceActivatorHost Host { get; }

    public static readonly Dictionary<int, List<ZNetView>> m_pendingPieces = new();

    protected bool CanActivatePendingPieces => _pendingPiecesCoroutine == null && _isInitComplete && Host != null && Host.GetPersistentId() != 0;

    private Coroutine? _initPersistentIdCoroutine;
    private bool _isInitComplete = false;
    public Action OnActivationComplete = () =>
    {
      LoggerProvider.LogWarning("No OnActivationComplete assigned");
    };
    public Action OnInitComplete = () =>
    {
      LoggerProvider.LogWarning("No OnInitComplete assigned");
    };

    protected abstract void TrySetPieceToParent(ZNetView netView);
    protected abstract void AddPiece(ZNetView netView, bool isNewPiece = false);

    public void StartInitPersistentId()
    {
      if (_initPersistentIdCoroutine != null) return;
      _initPersistentIdCoroutine = StartCoroutine(InitPersistentIdRoutine());
    }

    protected IEnumerator InitPersistentIdRoutine()
    {
      while (Host == null || Host.GetNetView() == null || Host.GetPersistentId() == 0)
      {
        yield return null;
      }
      _initPersistentIdCoroutine = null;
      _isInitComplete = true;

      OnInitComplete.Invoke();
    }

    public void OnDisable()
    {
      StopAllCoroutines();
      _pendingPiecesCoroutine = null;
    }


    public void StartActivatePendingPieces()
    {
      if (!CanActivatePendingPieces || !isActiveAndEnabled || ZNet.instance == null || ZNetScene.instance == null) return;

      var id = Host.GetPersistentId();
      if (!m_pendingPieces.TryGetValue(id, out var pending) || pending.Count == 0)
      {
        OnActivationComplete.Invoke();
        return;
      }

      if (_pendingPiecesCoroutine == null)
        _pendingPiecesCoroutine = StartCoroutine(ActivatePendingPiecesCoroutine());
    }

    public IEnumerator ActivatePendingPiecesCoroutine()
    {
      _pendingPiecesState = PendingPieceStateEnum.Running;
      PendingPiecesTimer.Restart();

      var persistentId = Host.GetPersistentId();
      if (persistentId == 0)
      {
        _pendingPiecesCoroutine = null;
        OnActivationComplete.Invoke();
        yield break;
      }

      var currentPieces = m_pendingPieces.TryGetValue(persistentId, out var list) ? list : null;

      if (currentPieces == null || currentPieces.Count == 0)
      {
        _pendingPiecesState = PendingPieceStateEnum.Complete;
        _pendingPiecesCoroutine = null;
        OnActivationComplete.Invoke();
        yield break;
      }

      do
      {
        if (ZNetScene.instance != null && ZNetScene.instance.InLoadingScreen())
          yield return new WaitForFixedUpdate();

        if (Host.GetNetView() == null)
        {
          _pendingPiecesState = PendingPieceStateEnum.ForceReset;
          _pendingPiecesCoroutine = null;
          OnActivationComplete.Invoke();
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

      _pendingPiecesState = PendingPieceStateEnum.Complete;
      _pendingPiecesCoroutine = null;
      OnActivationComplete?.Invoke();
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

    public static void AddPendingPiece(int swivelParentId, ZNetView netView)
    {
      if (!ValheimValidation.IsCurrentGameHealthy()) return;
      if (netView == null || netView.GetZDO() == null) return;
      if (!m_pendingPieces.TryGetValue(swivelParentId, out var list) || list.Count == 0)
      {
        list = [netView];

        // must set the list.
        m_pendingPieces[swivelParentId] = list;
        return;
      }

      if (!list.Contains(netView))
      {
        list.Add(netView);
      }

      if (SwivelComponentIntegration.ActiveInstances.TryGetValue(swivelParentId, out var swivel))
      {
        swivel.StartActivatePendingSwivelPieces();
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
      AddPendingPiece(id, netView);
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
      if (!ValheimValidation.IsCurrentGameHealthy())
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