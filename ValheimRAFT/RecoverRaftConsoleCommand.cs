using Jotunn.Entities;
using System.Collections.Generic;
using UnityEngine;
using ValheimRAFT.MoveableBaseRootComponent;

namespace ValheimRAFT
{
  internal class RecoverRaftConsoleCommand : ConsoleCommand
  {
    public override string Name => "RaftRecover";

    public override string Help => "Attempts to recover unattached rafts.";

    public override void Run(string[] args)
    {
      Collider[] colliderArray =
        Physics.OverlapSphere(((Component)GameCamera.instance).transform.position, 1000f);
      Dictionary<ZDOID, List<ZNetView>> dictionary = new Dictionary<ZDOID, List<ZNetView>>();
      ZLog.Log((object)string.Format("Searching {0}",
        (object)((Component)GameCamera.instance).transform.position));
      foreach (Component component1 in colliderArray)
      {
        ZNetView component2 = component1.GetComponent<ZNetView>();
        if (component2 != null && component2.m_zdo != null &&
            !component2.GetComponentInParent<Delegate>())
        {
          ZDOID zdoid = component2.m_zdo.GetZDOID(Server.MbParentHash);
          if ((zdoid != ZDOID.None))
          {
            if (!((Object)ZNetScene.instance.FindInstance(zdoid) != (Object)null))
            {
              List<ZNetView> znetViewList;
              if (!dictionary.TryGetValue(zdoid, out znetViewList))
              {
                znetViewList = new List<ZNetView>();
                dictionary.Add(zdoid, znetViewList);
              }

              znetViewList.Add(component2);
            }
          }
          else
            component2.m_zdo.GetVec3(Server.MbPositionHash, Vector3.zero);
        }
      }

      ZLog.Log($"Found {(object)dictionary.Count} potential ships to recover.");
      if (args.Length != 0 && args[0] == "confirm")
      {
        foreach (ZDOID key in dictionary.Keys)
        {
          List<ZNetView> znetViewList = dictionary[key];
          MoveableBaseShipComponent component = Object
            .Instantiate<GameObject>(ZNetScene.instance.GetPrefab("MBRaft"),
              ((Component)znetViewList[0]).transform.position,
              ((Component)znetViewList[0]).transform.rotation)
            .GetComponent<MoveableBaseShipComponent>();
          foreach (ZNetView netview in znetViewList)
          {
            ((Component)netview).transform.SetParent(((Component)component.m_baseRootDelegate)
              .transform);
            ((Component)netview).transform.localPosition =
              netview.m_zdo.GetVec3(Server.MbPositionHash, Vector3.zero);
            ((Component)netview).transform.localRotation =
              netview.m_zdo.GetQuaternion(Server.MbRotationHash,
                Quaternion.identity);
            component.m_baseRootDelegate.Instance.AddNewPiece(netview);
          }
        }
      }
      else if (dictionary.Count > 0)
        ZLog.Log((object)"Use \"RaftRecover confirm\" to complete the recover.");
    }
  }
}