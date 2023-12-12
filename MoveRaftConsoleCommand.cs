// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.MoveRaftConsoleCommand
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using Jotunn;
using Jotunn.Entities;
using System.Diagnostics;
using UnityEngine;
using ValheimRAFT.Util;

namespace ValheimRAFT
{
  internal class MoveRaftConsoleCommand : ConsoleCommand
  {
    public virtual string Name => "RaftOffset";

    public virtual string Help => "Offsets the raft by the given coordinates (X Y Z). Note: it's intended use is so you can slightly offset the pieces relative to the raft center. The actual center of the raft will NOT move.";

    public virtual void Run(string[] args)
    {
      if (args.Length < 3)
      {
        Logger.LogInfo((object) "Missing arguments, arguments required: X Y Z");
      }
      else
      {
        float result1;
        if (!float.TryParse(args[0], out result1))
        {
          Logger.LogInfo((object) ("Invalid argument X: " + args[0]));
        }
        else
        {
          float result2;
          if (!float.TryParse(args[1], out result2))
          {
            Logger.LogInfo((object) ("Invalid argument Y: " + args[1]));
          }
          else
          {
            float result3;
            if (!float.TryParse(args[2], out result3))
            {
              Logger.LogInfo((object) ("Invalid argument Z: " + args[2]));
            }
            else
            {
              Vector3 offset;
              // ISSUE: explicit constructor call
              ((Vector3) ref offset).\u002Ector(result1, result2, result3);
              Player localPlayer = Player.m_localPlayer;
              if (!Object.op_Implicit((Object) localPlayer))
                return;
              Ship standingOnShip = ((Character) localPlayer).GetStandingOnShip();
              if (Object.op_Implicit((Object) standingOnShip) && MoveRaftConsoleCommand.MoveRaft(localPlayer, standingOnShip, offset))
                return;
              RaycastHit raycastHit;
              if (!Physics.Raycast(((Component) GameCamera.instance).transform.position, ((Component) GameCamera.instance).transform.forward, ref raycastHit, 50f, LayerMask.GetMask(new string[1]
              {
                "piece"
              })))
                return;
              MoveableBaseRootComponent componentInParent = ((Component) ((RaycastHit) ref raycastHit).collider).GetComponentInParent<MoveableBaseRootComponent>();
              if (Object.op_Implicit((Object) componentInParent))
                MoveRaftConsoleCommand.MoveRaft(localPlayer, componentInParent.m_ship, offset);
            }
          }
        }
      }
    }

    private static bool MoveRaft(Player player, Ship ship, Vector3 offset)
    {
      MoveableBaseShipComponent component = ((Component) ship).GetComponent<MoveableBaseShipComponent>();
      if (!Object.op_Implicit((Object) component) || !Object.op_Implicit((Object) component.m_baseRoot))
        return false;
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      int persistantId = ZDOPersistantID.Instance.GetOrCreatePersistantID(component.m_baseRoot.m_nview.m_zdo);
      foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
      {
        if (zdo.GetInt(MoveableBaseRootComponent.MBParentIdHash, 0) == persistantId)
        {
          Vector3 vector3 = Vector3.op_Addition(zdo.GetVec3(MoveableBaseRootComponent.MBPositionHash, Vector3.zero), offset);
          zdo.Set(MoveableBaseRootComponent.MBPositionHash, vector3);
          zdo.SetPosition(((Component) ship).transform.position);
          ZNetView instance = ZNetScene.instance.FindInstance(zdo);
          if (Object.op_Implicit((Object) instance))
            ((Component) instance).transform.localPosition = vector3;
        }
      }
      Logger.LogInfo((object) string.Format("Completed MoveRaft in {0}ms", (object) stopwatch.ElapsedMilliseconds));
      return true;
    }
  }
}
