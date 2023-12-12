// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.HideRaftConsoleCommand
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using Jotunn;
using Jotunn.Entities;
using UnityEngine;

namespace ValheimRAFT
{
  internal class HideRaftConsoleCommand : ConsoleCommand
  {
    public virtual string Name => "RaftHide";

    public virtual string Help => "Toggles the base raft mesh";

    public virtual void Run(string[] args)
    {
      if (args.Length < 1)
      {
        Logger.LogInfo((object) "Missing arguments, arguments required: true\\false");
      }
      else
      {
        bool result;
        if (!bool.TryParse(args[0], out result))
        {
          Logger.LogInfo((object) ("Invalid arguments, " + args[0]));
        }
        else
        {
          Player localPlayer = Player.m_localPlayer;
          if (!Object.op_Implicit((Object) localPlayer))
            return;
          Ship standingOnShip = ((Character) localPlayer).GetStandingOnShip();
          if (Object.op_Implicit((Object) standingOnShip) && HideRaftConsoleCommand.HideRaft(localPlayer, standingOnShip, result))
            return;
          RaycastHit raycastHit;
          if (!Physics.Raycast(((Component) GameCamera.instance).transform.position, ((Component) GameCamera.instance).transform.forward, ref raycastHit, 50f, LayerMask.GetMask(new string[1]
          {
            "piece"
          })))
            return;
          MoveableBaseRootComponent componentInParent = ((Component) ((RaycastHit) ref raycastHit).collider).GetComponentInParent<MoveableBaseRootComponent>();
          if (Object.op_Implicit((Object) componentInParent))
            HideRaftConsoleCommand.HideRaft(localPlayer, componentInParent.m_ship, result);
        }
      }
    }

    private static bool HideRaft(Player player, Ship ship, bool hide)
    {
      MoveableBaseShipComponent component = ((Component) ship).GetComponent<MoveableBaseShipComponent>();
      if (!Object.op_Implicit((Object) component))
        return false;
      component.SetVisual(hide);
      return true;
    }
  }
}
