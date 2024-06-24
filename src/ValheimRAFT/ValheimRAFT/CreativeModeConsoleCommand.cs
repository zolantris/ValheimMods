using System.Collections;
using Jotunn.Entities;
using UnityEngine;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
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

  /// <summary>
  /// Prevents player from being launched into the sky.
  /// This can happen when the ship hits them when it is moved upwards.
  /// </summary>
  /// <param name="character"></param>
  /// <returns></returns>
  private static IEnumerator SetPlayerBackOnBoat(Character character)
  {
    yield return new WaitForSeconds(0.5f);
    yield return new WaitForFixedUpdate();
    character.m_body.velocity = Vector3.zero;
    character.m_body.angularVelocity = Vector3.zero;
    character.m_body.isKinematic = false;
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

    var vehiclePiecesController = hitinfo.collider.GetComponentInParent<VehiclePiecesController>();

    if ((bool)vehiclePiecesController?.VehicleInstance?.Instance)
    {
      var vehicleShipController = vehiclePiecesController?.VehicleInstance?.Instance;
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

  /// <summary>
  /// This moves the ship in the air for ship creation
  /// </summary>
  /// todo move all rigidbodies near or on the ship into a non-collision state and then move them back to the ship with the offset. This way this prevents all rigidbodies from being smashed into air
  /// <param name="character"></param>
  /// <param name="ship"></param>
  /// <returns></returns>
  private static bool ToggleMode(Character character,
    VehicleShip ship)
  {
    if (!(bool)ship || ship.MovementController == null)
    {
      return false;
    }

    var toggledKinematicValue = !ship.MovementController.m_body.isKinematic;
    ship.MovementController.m_body.isKinematic = toggledKinematicValue;
    ship.MovementController.zsyncTransform.m_isKinematicBody = toggledKinematicValue;

    if (toggledKinematicValue)
    {
      var shipYPosition =
        ship.MovementController.m_body.position.y +
        ValheimRaftPlugin.Instance.RaftCreativeHeight.Value;
      var playerInBoat =
        character.transform.parent == ship.VehiclePiecesController.Instance.transform;
      if (playerInBoat)
      {
        character.m_body.isKinematic = true;
        character.m_body.position = new Vector3(
          character.m_body.transform.position.x,
          character.m_body.transform.position.y + 0.2f +
          ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
          character.m_body.transform.position.z);
      }

      var directionRaftUpwards = new Vector3(ship.transform.position.x,
        shipYPosition,
        ship.transform.position.z);
      ship.MovementController.m_body.position = directionRaftUpwards;
      ship.SetCreativeMode(true);
      character.StartCoroutine(SetPlayerBackOnBoat(character));
    }
    else
    {
      ship.SetCreativeMode(false);
    }


    return true;
  }

  private static bool ToggleMode(Player character, Ship ship)
  {
    var mb = ship.GetComponent<MoveableBaseShipComponent>();
    if ((bool)mb)
    {
      var zsync = ship.GetComponent<ZSyncTransform>();
      mb.m_rigidbody.isKinematic = !mb.m_rigidbody.isKinematic;
      zsync.m_isKinematicBody = mb.m_rigidbody.isKinematic;
      if (mb.m_rigidbody.isKinematic)
      {
        if (character.transform.parent == mb.m_baseRoot.transform)
        {
          character.m_body.position = new Vector3(
            character.m_body.transform.position.x,
            character.m_body.transform.position.y +
            ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
            character.m_body.transform.position.z);
        }

        mb.m_rigidbody.position =
          new Vector3(mb.transform.position.x,
            mb.m_rigidbody.position.y + ValheimRaftPlugin.Instance.RaftCreativeHeight.Value,
            mb.transform.position.z);
        mb.m_rigidbody.transform.rotation =
          Quaternion.Euler(0, mb.m_rigidbody.rotation.eulerAngles.y, 0);
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