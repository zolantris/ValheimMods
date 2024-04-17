using UnityEngine;
using ValheimVehicles.Utis;
using ValheimVehicles.Vehicles;

namespace Components;

public class VehicleDebugGui : SingletonBehaviour<VehicleDebugGui>
{
  GUIStyle? myButtonStyle;

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

    // if (GUILayout.Button("stop collider debugger"))
    // {
    //   var currentInstance = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();
    //
    //   if (!currentInstance)
    //   {
    //     return;
    //   }
    //
    //   currentInstance.Stop();
    // }

    if (GUILayout.Button("Flip Ship"))
    {
      VehicleDebugHelpers.GetOnboardVehicleDebugHelper()?.FlipShip();
    }

    GUILayout.EndArea();
  }
}