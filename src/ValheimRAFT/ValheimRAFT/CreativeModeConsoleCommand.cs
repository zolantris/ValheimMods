using Jotunn.Entities;
using UnityEngine;

namespace ValheimRAFT;

internal class CreativeModeConsoleCommand : ConsoleCommand
{
  public override string Name => "RaftCreative";

  public override string Help => "Sets the current raft you are standing on into creative mode.";

  public override void Run(string[] args)
  {
    Player player = Player.m_localPlayer;
    if (!player)
    {
      return;
    }

    Ship ship = player.GetStandingOnShip();
    if ((!ship || !ToggleMode(player, ship)) && Physics.Raycast(
          GameCamera.instance.transform.position, GameCamera.instance.transform.forward,
          out var hitinfo, 50f, LayerMask.GetMask("piece")))
    {
      MoveableBaseRootComponent mbr =
        hitinfo.collider.GetComponentInParent<MoveableBaseRootComponent>();
      if ((bool)mbr)
      {
        ToggleMode(player, mbr.m_ship);
      }
    }
  }

  private static bool ToggleMode(Player player, Ship ship)
  {
    MoveableBaseShipComponent mb = ship.GetComponent<MoveableBaseShipComponent>();
    if ((bool)mb)
    {
      ZSyncTransform zsync = ship.GetComponent<ZSyncTransform>();
      mb.m_rigidbody.isKinematic = !mb.m_rigidbody.isKinematic;
      zsync.m_isKinematicBody = mb.m_rigidbody.isKinematic;
      if (mb.m_rigidbody.isKinematic)
      {
        if (player.transform.parent == mb.m_baseRoot.transform)
        {
          ((Character)player).m_body.position = new Vector3(
            ((Character)player).m_body.transform.position.x,
            ((Character)player).m_body.transform.position.y +
            ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
            ((Character)player).m_body.transform.position.z);
        }

        mb.m_rigidbody.position =
          new Vector3(mb.transform.position.x,
            mb.m_rigidbody.position.y + ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
            mb.transform.position.z);
        mb.m_rigidbody.rotation = Quaternion.Euler(mb.m_rigidbody.rotation.x, 0f,
          mb.m_rigidbody.rotation.z);
        mb.isCreative = true;
      }
      else
      {
        mb.isCreative = false;
      }


      return true;
    }

    mb.isCreative = false;
    return false;
  }
}