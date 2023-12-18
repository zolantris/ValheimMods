using Jotunn;
using Jotunn.Entities;
using System.Diagnostics;
using UnityEngine;
using ValheimRAFT.MoveableBaseRootComponent;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;

namespace ValheimRAFT
{
  internal class MoveRaftConsoleCommand : ConsoleCommand
  {
    public override string Name => "RaftOffset";

    public override string Help =>
      "Offsets the raft by the given coordinates (X Y Z). Note: it's intended use is so you can slightly offset the pieces relative to the raft center. The actual center of the raft will NOT move.";

    public override void Run(string[] args)
    {
      if (args.Length < 3)
      {
        Logger.LogInfo((object)"Missing arguments, arguments required: X Y Z");
      }
      else
      {
        float result1;
        if (!float.TryParse(args[0], out result1))
        {
          Logger.LogInfo((object)("Invalid argument X: " + args[0]));
        }
        else
        {
          float result2;
          if (!float.TryParse(args[1], out result2))
          {
            Logger.LogInfo((object)("Invalid argument Y: " + args[1]));
          }
          else
          {
            float result3;
            if (!float.TryParse(args[2], out result3))
            {
              Logger.LogInfo((object)("Invalid argument Z: " + args[2]));
            }
            else
            {
              Vector3 offset = new Vector3(result1, result2, result3);
              Player localPlayer = Player.m_localPlayer;
              if (!localPlayer)
                return;
              Ship standingOnShip = ((Character)localPlayer).GetStandingOnShip();
              if (standingOnShip &&
                  MoveRaftConsoleCommand.MoveRaft(localPlayer, standingOnShip, offset))
                return;
              if (!Physics.Raycast(((Component)GameCamera.instance).transform.position,
                    ((Component)GameCamera.instance).transform.forward, out var raycastHit, 50f,
                    LayerMask.GetMask(new string[1]
                    {
                      "piece"
                    })))
                return;

              /*
               * @todo This must be fired from the Server even if on the client
               *
               * disabled due to this taking a bit of time to refactor will come back to this.
               */
              // MoveableBaseRootComponent.Delegate.GetComponentInParent() = raycastHit
              //   .collider.GetComponentInParent<MoveableBaseRootComponent>();
              // if (componentInParent)
              //   MoveRaftConsoleCommand.MoveRaft(localPlayer, componentInParent.m_ship, offset);
            }
          }
        }
      }
    }

    private static bool MoveRaft(Player player, Ship ship, Vector3 offset)
    {
      if (!ZNet.instance.IsServer())
      {
        ZLog.Log("client cannot move ship, only server");
        return false;
      }

      MoveableBaseShipComponent component =
        ((Component)ship).GetComponent<MoveableBaseShipComponent>();
      if (!component ||
          !component.m_baseRootDelegate)
        return false;
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      int persistantId =
        ZDOPersistantID.Instance.GetOrCreatePersistantID(((Server)component.m_baseRootDelegate
          .Instance).m_nview.m_zdo);
      foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
      {
        if (zdo.GetInt(Server.MbParentIdHash, 0) == persistantId)
        {
          Vector3 vector3 =
            (zdo.GetVec3(Server.MbPositionHash, Vector3.zero) +
             offset);
          zdo.Set(Server.MbPositionHash, vector3);
          zdo.SetPosition(((Component)ship).transform.position);
          ZNetView instance = ZNetScene.instance.FindInstance(zdo);
          if (instance)
            ((Component)instance).transform.localPosition = vector3;
        }
      }

      Logger.LogInfo((object)string.Format("Completed MoveRaft in {0}ms",
        (object)stopwatch.ElapsedMilliseconds));
      return true;
    }
  }
}