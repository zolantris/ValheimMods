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
      MovableBaseRootComponent mbr =
        hitinfo.collider.GetComponentInParent<MovableBaseRootComponent>();
      if ((bool)mbr)
      {
        ToggleMode(player, mbr.m_ship);
      }
    }
  }

  private static bool ToggleMode(Player player, Ship ship)
  {
    MovableBaseShipComponent mb = ship.GetComponent<MovableBaseShipComponent>();
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
            ((Character)player).m_body.transform.position.y + 34.5f - mb.m_rigidbody.position.y,
            ((Character)player).m_body.transform.position.z);
        }

        mb.m_rigidbody.position =
          new Vector3(mb.transform.position.x, 35f, mb.transform.position.z);
        mb.m_rigidbody.rotation = Quaternion.Euler(0f,
          Mathf.Floor(mb.m_rigidbody.rotation.eulerAngles.y / 22.5f) * 22.5f, 0f);
      }

      return true;
    }

    return false;
  }
}