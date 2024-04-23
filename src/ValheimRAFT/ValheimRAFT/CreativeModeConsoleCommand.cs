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
    var player = Player.m_localPlayer;
    if (!player)
    {
      return;
    }

    var ship = player.GetStandingOnShip();
    if (!(bool)ship) return;

    if (ToggleMode(player, ship) || !Physics.Raycast(
          GameCamera.instance.transform.position, GameCamera.instance.transform.forward,
          out var hitinfo, 50f, LayerMask.GetMask("piece"))) return;

    var vehicleShip = hitinfo.collider.GetComponentInParent<VehicleShip>();

    if (vehicleShip.Instance != null && (bool)vehicleShip)
    {
      var vehicleShipController = (WaterVehicleController)vehicleShip.Instance.Controller;
      ToggleMode(player, vehicleShipController);
      return;
    }

    var mbr =
      hitinfo.collider.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbr)
    {
      ToggleMode(player, mbr.m_ship);
    }
  }

  private static bool ToggleMode(Character player,
    WaterVehicleController waterVehicleController)
  {
    if (!(bool)waterVehicleController || !(bool)waterVehicleController.VehicleInstance)
    {
      return false;
    }

    var ship = waterVehicleController.VehicleInstance;

    ship.m_body.isKinematic = !ship.m_body.isKinematic;
    ship.m_zsyncTransform.m_isKinematicBody = ship.m_body.isKinematic;
    if (ship.m_body.isKinematic)
    {
      if (player.transform.parent == waterVehicleController.transform)
      {
        player.m_body.position = new Vector3(
          player.m_body.transform.position.x,
          player.m_body.transform.position.y +
          ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
          player.m_body.transform.position.z);
      }

      var directionRaftUpwards = new Vector3(ship.transform.position.x,
        ship.m_body.position.y + ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
        ship.transform.position.z);
      var rotationWithoutTilt = Quaternion.Euler(0f, ship.m_body.rotation.eulerAngles.y, 0f);
      waterVehicleController.isCreative = true;

      ship.m_body.position = directionRaftUpwards;
      waterVehicleController.transform.rotation = rotationWithoutTilt;
      ship.m_body.transform.rotation = rotationWithoutTilt;
    }
    else
    {
      waterVehicleController.isCreative = false;
    }


    return true;
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
        mb.m_rigidbody.transform.rotation =
          Quaternion.Euler(0f, mb.m_rigidbody.rotation.eulerAngles.y, 0f);
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