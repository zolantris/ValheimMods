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

public abstract class MoveBaseRoot : MonoBehaviour
{
  public virtual void CleanUp()
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

  public virtual IEnumerator UpdatePieceSectors()
  {
  }

  internal float GetColliderBottom()
  {
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

  private static int GetParentID(ZDO zdo)
  {
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

  public void AddNewPiece(Piece piece)
  {
  }

  public void AddNewPiece(ZNetView netview)
  {
  }

  public void AddPiece(ZNetView netview)
  {
  }

  private void UpdatePieceCount()
  {
  }

  public void EncapsulateBounds(ZNetView netview)
  {
  }

  internal int GetPieceCount()
  {
  }
}