using System.Collections.Generic;
using UnityEngine;
namespace ValheimVehicles.Interfaces;

public interface IPieceActivatorHost
{
  int GetPersistentId();
  ZNetView? GetNetView();
  Transform GetPieceContainer();
}