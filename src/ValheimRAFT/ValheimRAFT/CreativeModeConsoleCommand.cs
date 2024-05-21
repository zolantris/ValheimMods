using Jotunn.Entities;
using UnityEngine;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class CreativeModeConsoleCommand : ConsoleCommand
{
  public override string Name => "RaftCreative";

  public override string Help => "Sets the current raft you are standing on into creative mode.";

  public override void Run(string[] args)
  {
    RunCreativeModeCommand(Name);
  }

  public static void RunCreativeModeCommand(string comandName)
  {
    var player = Player.m_localPlayer;
    if (!player)
    {
      Logger.LogWarning($"Player does not exist, this command {comandName} cannot be run");
      return;
    }

    if (!Physics.Raycast(
          GameCamera.instance.transform.position, GameCamera.instance.transform.forward,
          out var hitinfo, 50f,
          LayerMask.GetMask("piece") + LayerMask.GetMask("CustomVehicleLayer")))
    {
      Logger.LogWarning(
        $"boat not detected within 50f, get nearer to the boat and look directly at the boat");
      return;
    }

    var baseVehicleController = hitinfo.collider.GetComponentInParent<BaseVehicleController>();

    if ((bool)baseVehicleController?.VehicleInstance?.Instance)
    {
      var vehicleShipController = baseVehicleController?.VehicleInstance?.Instance;
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
    VehicleShip ship)
  {
    if (!(bool)ship)
    {
      return false;
    }

    var toggledKinematicValue = !ship.m_body.isKinematic;
    ship.m_body.isKinematic = toggledKinematicValue;
    ship.m_zsyncTransform.m_isKinematicBody = toggledKinematicValue;

    if (toggledKinematicValue)
    {
      if (player.transform.parent == ship.VehicleController.Instance.transform)
      {
        player.m_body.position = new Vector3(
          player.m_body.transform.position.x,
          player.m_body.transform.position.y + 2f +
          ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
          player.m_body.transform.position.z);

        // prevents player from being launched into the sky if the ship hits them when it is moved upwards
        player.m_body.isKinematic = true;
      }

      var directionRaftUpwards = new Vector3(ship.transform.position.x,
        ship.m_body.position.y + ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
        ship.transform.position.z);
      var rotationWithoutTilt = Quaternion.Euler(0f, ship.m_body.rotation.eulerAngles.y, 0f);
      ship.SetCreativeMode(true);

      ship.m_body.position = directionRaftUpwards;
      ship.m_body.transform.rotation = rotationWithoutTilt;
      ship.Instance.transform.rotation = rotationWithoutTilt;
      ship.VehicleController.Instance.transform.rotation = rotationWithoutTilt;
      ship.transform.rotation = rotationWithoutTilt;

      // set player back to being controllable
      player.m_body.isKinematic = false;
    }
    else
    {
      ship.SetCreativeMode(false);
    }


    return true;
  }

  private static bool ToggleMode(Player player, Ship ship)
  {
    var mb = ship.GetComponent<MoveableBaseShipComponent>();
    if ((bool)mb)
    {
      var zsync = ship.GetComponent<ZSyncTransform>();
      mb.m_rigidbody.isKinematic = !mb.m_rigidbody.isKinematic;
      zsync.m_isKinematicBody = mb.m_rigidbody.isKinematic;
      if (mb.m_rigidbody.isKinematic)
      {
        if (player.transform.parent == mb.m_baseRoot.transform)
        {
          player.m_body.position = new Vector3(
            player.m_body.transform.position.x,
            player.m_body.transform.position.y +
            ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
            player.m_body.transform.position.z);
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