using System.Collections.Generic;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

internal class RecoverRaftConsoleCommand : ConsoleCommand
{
  public override string Name => "RaftRecover";

  public override string Help => "Attempts to recover unattached rafts.";

  public override void Run(string[] args)
  {
    Collider[] colliders = Physics.OverlapSphere(GameCamera.instance.transform.position, 1000f);
    Dictionary<ZDOID, List<ZNetView>> unattached = new Dictionary<ZDOID, List<ZNetView>>();
    Logger.LogDebug($"Searching {GameCamera.instance.transform.position}");
    Collider[] array = colliders;
    foreach (Collider collider in array)
    {
      ZNetView netview = collider.GetComponent<ZNetView>();
      if (!(netview != null) || netview.m_zdo == null ||
          (bool)netview.GetComponentInParent<MoveableBaseRootComponent>())
      {
        continue;
      }

      ZDOID zdoid2 = netview.m_zdo.GetZDOID(MoveableBaseRootComponent.MBParentHash);
      if (zdoid2 != ZDOID.None)
      {
        GameObject parentInstance = ZNetScene.instance.FindInstance(zdoid2);
        if (!(parentInstance != null))
        {
          if (!unattached.TryGetValue(zdoid2, out var list2))
          {
            list2 = new List<ZNetView>();
            unattached.Add(zdoid2, list2);
          }

          list2.Add(netview);
        }
      }
      else
      {
        Vector3 partOffset =
          netview.m_zdo.GetVec3(MoveableBaseRootComponent.MBPositionHash, Vector3.zero);
      }
    }

    Logger.LogDebug($"Found {unattached.Count} potential ships to recover.");
    if (args.Length != 0 && args[0] == "confirm")
    {
      foreach (ZDOID zdoid in unattached.Keys)
      {
        List<ZNetView> list = unattached[zdoid];
        GameObject shipPrefab = ZNetScene.instance.GetPrefab("MBRaft");
        GameObject ship = Object.Instantiate(shipPrefab, list[0].transform.position,
          list[0].transform.rotation);
        MoveableBaseShipComponent mbship = ship.GetComponent<MoveableBaseShipComponent>();
        foreach (ZNetView piece in list)
        {
          piece.transform.SetParent(mbship.m_baseRoot.transform);
          piece.transform.localPosition =
            piece.m_zdo.GetVec3(MoveableBaseRootComponent.MBPositionHash, Vector3.zero);
          piece.transform.localRotation =
            piece.m_zdo.GetQuaternion(MoveableBaseRootComponent.MBRotationHash, Quaternion.identity);
          mbship.m_baseRoot.AddNewPiece(piece);
        }
      }

      return;
    }

    if (unattached.Count > 0)
    {
      Logger.LogDebug("Use \"RaftRecover confirm\" to complete the recover.");
    }
  }
}