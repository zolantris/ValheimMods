using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Utis;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace Components;

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

  private void OnGUI()
  {
    myButtonStyle ??= new GUIStyle(GUI.skin.button)
    {
      fontSize = 50
    };

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

      currentInstance.StartRenderAllCollidersLoop();
    }

    if (GUILayout.Button("raftcreative"))
    {
      CreativeModeConsoleCommand.RunCreativeModeCommand("raftcreative");
    }

    if (GUILayout.Button("activatePendingPieces"))
    {
      VehicleDebugHelpers.GetVehicleController()?.ActivatePendingPiecesCoroutine();
    }

    if (GUILayout.Button("rebuild bounds"))
    {
      VehicleDebugHelpers.GetVehicleController()?.RebuildBounds();
    }

    if (GUILayout.Button("Flip Ship"))
    {
      VehicleDebugHelpers.GetOnboardVehicleDebugHelper()?.FlipShip();
    }

    GUILayout.EndArea();
  }
}