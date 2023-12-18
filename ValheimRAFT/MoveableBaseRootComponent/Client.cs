// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.MoveableBaseRootComponent

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;
using Main = ValheimRAFT.Main;

namespace ValheimRAFT.MoveableBaseRootComponent;

public sealed class Client : MoveBaseRoot
{
  public void Awake()
  {
  }


  public override List<MastComponent> GetMastPieces()
  {
    throw new System.NotImplementedException();
  }

  public override float GetColliderBottom()
  {
    return 0f;
  }

  public void CleanUp()
  {
  }

  public void Sync()
  {
  }

  public void FixedUpdate()
  {
  }

  public void LateUpdate()
  {
  }

  public void UpdateAllPieces()
  {
  }

  public IEnumerator UpdatePieceSectors()
  {
    yield return true;
  }

  public static void AddInactivePiece(int id, ZNetView netview)
  {
  }

  public void RemovePiece(ZNetView netview)
  {
  }

  private void UpdateStats()
  {
  }

  public void DestroyPiece(WearNTear wnt)
  {
  }

  public void ActivatePendingPiecesCoroutine()
  {
  }

  public IEnumerator ActivatePendingPieces()
  {
    yield return null;
  }

  public static void AddDynamicParent(ZNetView source, GameObject target)
  {
  }

  public static void AddDynamicParent(ZNetView source, GameObject target, Vector3 offset)
  {
  }

  public static void InitZDO(ZDO zdo)
  {
  }

  public static void RemoveZDO(ZDO zdo)
  {
  }

  /**
   * returning empty zdo
   */
  private static int GetParentID(ZDO zdo)
  {
    return 0;
  }

  public static void InitPiece(ZNetView netview)
  {
  }

  public void ActivatePiece(ZNetView netview)
  {
  }

  public void AddTemporaryPiece(Piece piece)
  {
  }

  public override void AddNewPiece(Piece piece)
  {
  }

  public override void AddNewPiece(ZNetView netview)
  {
  }

  public override void AddPiece(ZNetView netview)
  {
  }

  /**
   * this is not used directly
   */
  // private void UpdatePieceCount()
  // {
  // }
  public override void EncapsulateBounds(ZNetView netview)
  {
  }

  internal override int GetPieceCount()
  {
    ZLog.LogError("error with client call for GetPieceCount");
    return 0;
  }
}