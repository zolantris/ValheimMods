using Jotunn;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ValheimRAFT
{
  internal class HideRaftConsoleCommand : ConsoleCommand
  {
    public override string Name => "RaftHide";

    public override string Help => "Toggles the base raft mesh";

    public override void Run(string[] args)
    {
      if (args.Length < 1)
      {
        Logger.LogInfo((object)"Missing arguments, arguments required: true\\false");
      }
      else
      {
        bool result;
        if (!bool.TryParse(args[0], out result))
        {
          Logger.LogInfo((object)("Invalid arguments, " + args[0]));
        }
        else
        {
          Player localPlayer = Player.m_localPlayer;
          if (!localPlayer)
            return;
          Ship standingOnShip = ((Character)localPlayer).GetStandingOnShip();
          if (standingOnShip &&
              HideRaftConsoleCommand.HideRaft(localPlayer, standingOnShip, result))
            return;

          RaycastHit raycastHit = new RaycastHit();
          if (!Physics.Raycast(((Component)GameCamera.instance).transform.position,
                ((Component)GameCamera.instance).transform.forward, out raycastHit, 50f,
                LayerMask.GetMask(new string[1]
                {
                  "piece"
                })))
            return;
          MoveableBaseRootComponent.Server componentInParent =
            raycastHit.collider.GetComponentInParent<MoveableBaseRootComponent.Server>();
          if (componentInParent)
            HideRaftConsoleCommand.HideRaft(localPlayer, componentInParent.m_ship, result);
        }
      }
    }

    private static bool HideRaft(Player player, Ship ship, bool hide)
    {
      MoveableBaseShipComponent component =
        ((Component)ship).GetComponent<MoveableBaseShipComponent>();
      if (!component)
        return false;
      component.SetVisual(hide);
      return true;
    }
  }
}