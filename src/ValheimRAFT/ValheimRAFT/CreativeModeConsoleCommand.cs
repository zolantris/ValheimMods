using Jotunn.Entities;
using UnityEngine;
using ValheimVehicles.Vehicles;

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

    var ship = player.GetStandingOnShip();
    if ((!ship || !ToggleMode(player, ship)) && Physics.Raycast(
          GameCamera.instance.transform.position, GameCamera.instance.transform.forward,
          out var hitinfo, 50f, LayerMask.GetMask("piece")))
    {
      BaseVehicle mbr =
        hitinfo.collider.GetComponentInParent<BaseVehicle>();
      if ((bool)mbr)
      {
        ToggleMode(player, mbr.m_ship);
      }
    }
  }

  private static bool ToggleModeInternal(WaterVehicle waterVehicle, Player player)
  {
    if ((bool)waterVehicle)
    {
      ZSyncTransform zsync = waterVehicle.GetComponent<ZSyncTransform>();
      waterVehicle.m_rigidbody.isKinematic = !waterVehicle.m_rigidbody.isKinematic;
      zsync.m_isKinematicBody = waterVehicle.m_rigidbody.isKinematic;
      if (waterVehicle.m_rigidbody.isKinematic)
      {
        if (player.transform.parent == waterVehicle.baseVehicle.transform)
        {
          ((Character)player).m_body.position = new Vector3(
            ((Character)player).m_body.transform.position.x,
            ((Character)player).m_body.transform.position.y +
            ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
            ((Character)player).m_body.transform.position.z);
        }

        waterVehicle.m_rigidbody.position =
          new Vector3(waterVehicle.transform.position.x,
            waterVehicle.m_rigidbody.position.y +
            ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
            waterVehicle.transform.position.z);
        waterVehicle.m_rigidbody.transform.rotation =
          Quaternion.Euler(0f, waterVehicle.m_rigidbody.rotation.eulerAngles.y, 0f);
        waterVehicle.isCreative = true;
      }
      else
      {
        waterVehicle.isCreative = false;
      }


      return true;
    }

    waterVehicle.isCreative = false;
    return false;
  }

  private static bool ToggleMode(Player player, Ship ship)
  {
    MoveableBaseShipComponent baseShipComponent = ship.GetComponent<MoveableBaseShipComponent>();
    return ToggleModeInternal(baseShipComponent, player);
  }

  private static bool ToggleMode(Player player, ValheimShip ship)
  {
    MoveableBaseShipComponent baseShipComponent = ship.GetComponent<MoveableBaseShipComponent>();
    return ToggleModeInternal(baseShipComponent, player);
  }
}