using System.Collections.Generic;
using UnityEngine;
namespace ValheimVehicles.Interfaces;

public interface IPieceActivatorHost
{
  Transform transform { get; }
  public int GetPersistentId();
  public ZNetView? GetNetView();
  public Transform GetPieceContainer();
}