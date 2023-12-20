using Jotunn;
using Jotunn.Entities;
using UnityEngine;

namespace ValheimRAFT;

internal class HideRaftConsoleCommand : ConsoleCommand
{
	public override string Name => "RaftHide";

	public override string Help => "Toggles the base raft mesh";

	public override void Run(string[] args)
	{
		if (args.Length < 1)
		{
			Jotunn.Logger.LogInfo("Missing arguments, arguments required: true\\false");
			return;
		}
		if (!bool.TryParse(args[0], out var hide))
		{
			Jotunn.Logger.LogInfo("Invalid arguments, " + args[0]);
			return;
		}
		Player player = Player.m_localPlayer;
		if (!player)
		{
			return;
		}
		Ship ship = player.GetStandingOnShip();
		if ((!ship || !HideRaft(player, ship, hide)) && Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitinfo, 50f, LayerMask.GetMask("piece")))
		{
			MoveableBaseRootComponent mbr = hitinfo.collider.GetComponentInParent<MoveableBaseRootComponent>();
			if ((bool)mbr)
			{
				HideRaft(player, mbr.m_ship, hide);
			}
		}
	}

	private static bool HideRaft(Player player, Ship ship, bool hide)
	{
		MoveableBaseShipComponent mb = ship.GetComponent<MoveableBaseShipComponent>();
		if ((bool)mb)
		{
			mb.SetVisual(hide);
			return true;
		}
		return false;
	}
}
