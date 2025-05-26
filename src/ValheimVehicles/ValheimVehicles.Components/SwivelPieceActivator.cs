using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
namespace ValheimVehicles.Components;

/// <summary>
/// Todo move this to the base activator with Type passthrough or write a interface for Init so these Flavors of Base can use that.
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public class SwivelPieceActivator : BasePieceActivatorComponent
{
  [SerializeField] private SwivelComponentBridge _host;
  public override IPieceActivatorHost Host => _host;

  protected override void TrySetPieceToParent(ZNetView netView)
  {
    // Classic vehicle-specific logic
    netView.transform.SetParent(_host.GetPieceContainer(), false);
  }

  protected override void AddPiece(ZNetView netView, bool isNewPiece = false)
  {
    _host.AddPiece(netView, isNewPiece);
  }

  public void Init(SwivelComponentBridge host)
  {
    _host = host;
  }

  public static void AddPendingSwivelPiece(int swivelId, ZNetView netView)
  {
    if (!m_pendingPieces.TryGetValue(swivelId, out var list))
    {
      list = new List<ZNetView>();
      m_pendingPieces[swivelId] = list;
    }

    list.Add(netView);
  }
}