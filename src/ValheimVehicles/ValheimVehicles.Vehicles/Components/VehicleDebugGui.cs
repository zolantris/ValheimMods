using UnityEngine;
using ValheimVehicles.Utis;
using ValheimVehicles.Vehicles;

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

    // if (GUILayout.Button("Render All BoxColliders"))
    // {
    //   var currentInstance = VehicleDebugHelpers.GetVehicleController();
    //   if (currentInstance != null)
    //     VehicleDebugHelpers.GetOnboardVehicleDebugHelper()
    //       ?.RenderAllVehicleBoxColliders(currentInstance);
    // }
    //

    ShipMovementOffsetText = GUILayout.TextField(ShipMovementOffsetText);
    if (GUILayout.Button("MoveShip"))
    {
      VehicleDebugHelpers.GetOnboardVehicleDebugHelper()?.MoveShip(GetShipMovementOffset());
    }

    if (GUILayout.Button("Flip Ship"))
    {
      VehicleDebugHelpers.GetOnboardVehicleDebugHelper()?.FlipShip();
    }

    GUILayout.EndArea();
  }
}