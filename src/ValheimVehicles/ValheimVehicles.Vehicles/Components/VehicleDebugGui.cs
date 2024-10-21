using DynamicLocations;
using DynamicLocations.Controllers;
using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.Patches;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Enums;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Components;

public class VehicleDebugGui : SingletonBehaviour<VehicleDebugGui>
{
  GUIStyle? myButtonStyle;
  private string ShipMovementOffsetText;
  private Vector3 _shipMovementOffset;

  private Vector3 GetShipMovementOffset()
  {
    var shipMovementVectors = ShipMovementOffsetText.Split(',');
    if (shipMovementVectors.Length != 3) return new Vector3(0, 0, 0);
    var x = float.Parse(shipMovementVectors[0]);
    var y = float.Parse(shipMovementVectors[1]);
    var z = float.Parse(shipMovementVectors[2]);
    return new Vector3(x, y, z);
  }

  public static bool vehicleDebugPhysicsSync = true;

  private void OnGUI()
  {
    myButtonStyle ??= new GUIStyle(GUI.skin.button)
    {
      fontSize = 50
    };
#if DEBUG
    GUILayout.BeginArea(new Rect(250, 10, 200, 200), myButtonStyle);
    if (GUILayout.Button(
          $"ActivatePendingPieces {VehiclePiecesController.DEBUGAllowActivatePendingPieces}"))
    {
      VehiclePiecesController.DEBUGAllowActivatePendingPieces =
        !VehiclePiecesController.DEBUGAllowActivatePendingPieces;
      if (VehiclePiecesController.DEBUGAllowActivatePendingPieces)
      {
        foreach (var vehiclePiecesController in VehiclePiecesController
                   .ActiveInstances.Values)
        {
          vehiclePiecesController.ActivatePendingPiecesCoroutine();
        }
      }
    }

    // if (GUILayout.Button(
    //       $"SyncPiecesPhysics {vehicleDebugPhysicsSync}"))
    // {
    //   vehicleDebugPhysicsSync = !vehicleDebugPhysicsSync;
    //   VehicleMovementController.SetPhysicsSyncTarget(
    //     vehicleDebugPhysicsSync);
    // }
    //
    // if (GUILayout.Button(
    //       $"Toggle Sync Physics"))
    // {
    //   foreach (var vehiclePiecesController in VehiclePiecesController
    //              .ActiveInstances)
    //   {
    //     vehiclePiecesController.Value?.SetVehiclePhysicsType(VehiclePhysicsMode
    //       .ForceSyncedRigidbody);
    //   }
    // }

    if (GUILayout.Button("Delete ShipZDO"))
    {
      var currentShip = VehicleDebugHelpers.GetVehiclePiecesController();
      if (currentShip != null)
      {
        ZNetScene.instance.Destroy(currentShip?.VehicleInstance?.NetView?
          .gameObject);
      }
    }

    if (GUILayout.Button("Set logoutpoint"))
    {
      PlayerSpawnController.Instance?.SyncLogoutPoint();
    }

    if (GUILayout.Button("Move to current spawn"))
    {
      PlayerSpawnController.Instance?.DEBUG_MoveToLogoutPoint();
    }

    if (GUILayout.Button("Move to current logout"))
    {
      PlayerSpawnController.Instance?.MovePlayerToLogoutPoint();
    }

    if (GUILayout.Button("DebugFind PlayerSpawnController"))
    {
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
      foreach (var obj in allObjects)
      {
        if (obj.name.Contains($"{PrefabNames.PlayerSpawnControllerObj}(Clone)"))
        {
          Logger.LogDebug("found playerSpawn controller");
        }
      }
    }

    GUILayout.EndArea();
#endif

    GUILayout.BeginArea(new Rect(500, 10, 200, 200), myButtonStyle);
    if (GUILayout.Button("collider debugger"))
    {
      Logger.LogMessage(
        "Collider debugger called, \nblue = BlockingCollider for collisions and keeping boat on surface, \ngreen is float collider for pushing the boat upwards, typically it needs to be below or at same level as BlockingCollider to prevent issues, \nYellow is onboardtrigger for calculating if player is onboard");
      var currentInstance = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();

      if (!currentInstance)
      {
        currentInstance = VehicleDebugHelpers.GetOnboardMBRaftDebugHelper();
      }

      if (!currentInstance)
      {
        return;
      }

      currentInstance?.StartRenderAllCollidersLoop();
    }

    if (GUILayout.Button("raftcreative"))
    {
      CreativeModeConsoleCommand.RunCreativeModeCommand("raftcreative");
    }

    if (GUILayout.Button("activatePendingPieces"))
    {
      VehicleDebugHelpers.GetVehiclePiecesController()
        ?.ActivatePendingPiecesCoroutine();
    }

    if (GUILayout.Button("Zero Ship RotationXZ"))
    {
      VehicleDebugHelpers.GetOnboardVehicleDebugHelper()?.FlipShip();
    }

    if (GUILayout.Button("Toggle Ocean Sway"))
    {
      VehicleCommands.VehicleToggleOceanSway();
    }

    GUILayout.EndArea();
  }
}