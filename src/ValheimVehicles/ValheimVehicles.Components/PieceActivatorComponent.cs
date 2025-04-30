using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Enums;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Structs;

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

  protected bool CanActivatePendingPieces => _pendingPiecesCoroutine == null && _isInitComplete && Host.GetPersistentId() != 0;

  private Coroutine? _initPersistentIdCoroutine;
  private bool _isInitComplete = false;

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

    if (CanActivatePendingPieces)
    {
      StartActivatePendingPieces();
    }
  }


  public void StartActivatePendingPieces()
  {
    if (!CanActivatePendingPieces) return;

    var id = Host.GetPersistentId();
    if (!m_pendingPieces.TryGetValue(id, out var pending) || pending.Count == 0) return;

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
      yield break;
    }

    var currentPieces = m_pendingPieces.TryGetValue(persistentId, out var list) ? list : null;

    if (currentPieces == null || currentPieces.Count == 0)
    {
      _pendingPiecesState = PendingPieceStateEnum.Complete;
      _pendingPiecesCoroutine = null;
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
        yield break;
      }

      _pendingPiecesDirty = false;

      foreach (var piece in currentPieces.ToList())
      {
        TrySetPieceToParent(piece);
        FinalizeTransform(piece);
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
  }

  protected void FinalizeTransform(ZNetView netView)
  {
    if (netView == null || netView.m_zdo == null) return;

    var t = netView.transform;
    t.localPosition = netView.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, t.localPosition);
    t.localRotation = Quaternion.Euler(netView.m_zdo.GetVec3(VehicleZdoVars.MBRotationVecHash, t.localRotation.eulerAngles));

    if (netView.TryGetComponent<WearNTear>(out var wnt))
      wnt.enabled = true;
  }

  protected abstract void TrySetPieceToParent(ZNetView netView);
}