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
  internal static Dictionary<int, List<ZNetView>> m_pendingPieces =
    new Dictionary<int, List<ZNetView>>();

  internal static Dictionary<int, List<ZDO>> m_allPieces = new Dictionary<int, List<ZDO>>();

  internal static Dictionary<int, List<ZDOID>>
    m_dynamicObjects = new Dictionary<int, List<ZDOID>>();

  internal MoveableBaseShipComponent m_moveableBaseShip;

  internal Rigidbody m_rigidbody;

  public ZNetView m_nview;

  internal Rigidbody m_syncRigidbody;

  internal Ship m_ship;

  internal List<ZNetView> m_pieces = new List<ZNetView>();

  internal List<MastComponent> m_mastPieces = new List<MastComponent>();

  internal List<RudderComponent> m_rudderPieces = new List<RudderComponent>();

  internal List<ZNetView> m_portals = new List<ZNetView>();

  internal List<RopeLadderComponent> m_ladders = new List<RopeLadderComponent>();

  internal List<BoardingRampComponent> m_boardingRamps = new List<BoardingRampComponent>();

  public Vector2i m_sector;

  internal Bounds m_bounds = default(Bounds);

  internal BoxCollider m_blockingcollider;

  internal BoxCollider m_floatcollider;

  internal BoxCollider m_onboardcollider;

  internal int m_id;

  public bool m_statsOverride = false;

  public static bool itemsRemovedDuringWait;

  public virtual BoxCollider GetFloatCollider()
  {
    return m_floatcollider;
  }

  public virtual List<RudderComponent> GetRudderPieces()
  {
    return m_rudderPieces;
  }

  public virtual bool GetStatsOverride()
  {
    return m_statsOverride;
  }

  public virtual void CleanUp()
  {
  }

  public virtual void InitializeShipComponent(MoveableBaseShipComponent moveableBaseShipComponent,
    ZNetView nView, Ship ship, Rigidbody rigidbody)
  {
  }

  public virtual void InitializeShipColliders(BoxCollider[] colliders)
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
    yield return null;
  }

  public abstract List<MastComponent> GetMastPieces();

  public abstract float GetColliderBottom();

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

  public virtual void ActivatePiece(ZNetView netview)
  {
  }

  public virtual void AddTemporaryPiece(Piece piece)
  {
  }

  public abstract void AddNewPiece(Piece piece);

  public abstract void AddNewPiece(ZNetView netview);

  public abstract void AddPiece(ZNetView netview);

  internal virtual void UpdatePieceCount()
  {
  }

  public abstract void EncapsulateBounds(ZNetView netview);

  internal abstract int GetPieceCount();
}