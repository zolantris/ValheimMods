using System;
using System.Collections.Generic;
using System.Diagnostics;
using DynamicLocations;
using DynamicLocations.Constants;
using DynamicLocations.Controllers;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ValheimRAFT;
using ValheimRAFT.Patches;
using ValheimVehicles.Config;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Enums;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Components;

public class VehicleDebugGui : SingletonBehaviour<VehicleDebugGui>
{
  private GUIStyle? myButtonStyle;
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

  private Rect windowRect;

  private Rect errorWinRect;

  private int buttonFontSize = 16;

  private int titleFontSize = 18;

  private GUIStyle buttonStyle;

  private GUIStyle labelStyle;

  private GameObject devCommandsWindow;
  private GameObject modderCommandsWindow;
  private bool hasInitialized = false;

  private void Start()
  {
    windowRect.x = VehicleDebugConfig.windowPosX.Value;
    windowRect.y = VehicleDebugConfig.windowPosY.Value;
    buttonFontSize = VehicleDebugConfig.buttonFontSize.Value;
    titleFontSize = VehicleDebugConfig.TitleFontSize.Value;
    windowRect.width = VehicleDebugConfig.windowWidth.Value;
    windowRect.height = VehicleDebugConfig.windowHeight.Value;
    InitPanel();
  }

  private void TogglePanelState()
  {
    var state = !hasInitialized || !modderCommandsWindow.activeSelf;

    if (devCommandsWindow)
    {
      devCommandsWindow.SetActive(state);
    }

    if (modderCommandsWindow)
    {
      modderCommandsWindow.SetActive(state);
    }

    // Toggle input for the player and camera while displaying the GUI
    // GUIManager.BlockInput(state);
  }

  private void InitPanel()
  {
    if (GUIManager.Instance == null)
    {
      Logger.LogError("GUIManager instance is null");
      return;
    }

    if (!GUIManager.CustomGUIFront)
    {
      Logger.LogError("GUIManager CustomGUI is null");
      return;
    }

    CreateShortcutPanel();
    TogglePanelState();
    hasInitialized = true;
  }

  public struct ButtonAction
  {
    public string title;
    public Action action;
  }

  public static float buttonHeight = 60f;
  public static float buttonWidth = 150f;
  public void AddButtonWithAction(ButtonAction buttonAction, int index, float StartHeight, Transform windowTransform)
  {

    var buttonObj = GUIManager.Instance.CreateButton(
      buttonAction.title,
      windowTransform,
      new Vector2(0.5f, 0.5f),
      new Vector2(0.5f, 0.5f),
      new Vector2(-20, StartHeight - index * buttonHeight),
      buttonWidth, buttonHeight
    );
    buttonObj.SetActive(true);
    // Add a listener to the button to close the panel again
    var button = buttonObj.GetComponent<Button>();
    var text = buttonObj.GetComponent<Text>();
    if (text != null)
    {
      text.fontSize = buttonFontSize;
    }

    button.onClick.AddListener(() => buttonAction.action());
  }

  private void CreateShortcutPanel()
  {
    if (modderCommandsWindow != null) return;

    modderCommandsWindow = GUIManager.Instance.CreateWoodpanel(
      GUIManager.CustomGUIFront.transform,
      new Vector2(1f, 0.5f),
      new Vector2(0.5f, 0.5f),
      new Vector2(500, Screen.height - 510),
      200,
      500,
      true);
    modderCommandsWindow.SetActive(false);
    modderCommandsWindow.name = "modderCommandsWindow";

    modderCommandsWindow.gameObject.AddComponent<DragWindowCntrl>();

    // Create the text object
    GUIManager.Instance.CreateText(
      "Vehicle Shortcuts",
      modderCommandsWindow.transform,
      new Vector2(0.5f, 1f),
      new Vector2(0.5f, 1f),
      new Vector2(0f, -20f),
      GUIManager.Instance.AveriaSerifBold,
      titleFontSize,
      GUIManager.Instance.ValheimOrange,
      true,
      Color.black,
      200f,
      40f,
      true);

    // Create the button object
    var buttonObject = GUIManager.Instance.CreateButton(
      "Hide",
      modderCommandsWindow.transform,
      new Vector2(1f, 1f),
      new Vector2(1f, 1f),
      new Vector2(60, 0),
      60f,
      60f);
    buttonObject.SetActive(true);

    // Add a listener to the button to close the panel again
    var button = buttonObject.GetComponent<Button>();
    button.onClick.AddListener(InitPanel);

    List<ButtonAction> buttonActions =
    [
      new()
      {
        title = "ConvexHull debugger",
        action = () =>
        {
          ToggleConvexHullDebugger();
        }
      },
      new()
      {
        title = "ConvexHull debugger",
        action = () =>
        {
          ToggleColliderDebugger();
        }
      },
      new()
      {
        title = "Raft Creative",
        action = () => CreativeModeConsoleCommand.RunCreativeModeCommand("raftcreative")
      },
      new()
      {
        title = "Zero Ship Rotation X/Z",
        action = () =>
        {
          var onboardHelpers = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();
          if (onboardHelpers != null) onboardHelpers.FlipShip();
        }
      },
      new()
      {
        title = "Toggle Ocean Sway",
        action = VehicleCommands.VehicleToggleOceanSway
      }
    ];

    var startHeight = 100f;
    for (var index = 0; index < buttonActions.Count; index++)
    {
      var buttonAction = buttonActions[index];
      AddButtonWithAction(buttonAction, index, startHeight, modderCommandsWindow.transform);
    }

    modderCommandsWindow.SetActive(true);
  }

  private void ToggleConvexHullDebugger()
  {
    Logger.LogMessage(
      "Toggling convex hull debugger on the ship. This will show/hide the current convex hulls.");
    var currentInstance = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();

    if (!currentInstance)
      currentInstance = VehicleDebugHelpers.GetOnboardMBRaftDebugHelper();

    if (!currentInstance) return;

    var convexHullComponent = currentInstance.VehicleShipInstance
      .PiecesController.convexHullComponent;
    // Just a 3 mode loop
    convexHullComponent.PreviewMode =
      convexHullComponent.PreviewMode switch
      {
        ConvexHullAPI.PreviewModes.None => ConvexHullAPI.PreviewModes.Bubble,
        ConvexHullAPI.PreviewModes.Bubble => ConvexHullAPI.PreviewModes.Debug,
        _ => ConvexHullAPI.PreviewModes.Bubble
      };

    currentInstance.VehicleShipInstance.PiecesController.convexHullComponent
      .CreatePreviewConvexHullMeshes();
  }


  private void ToggleColliderDebugger()
  {
    Logger.LogMessage(
      "Collider debugger called, \nblue = BlockingCollider for collisions and keeping boat on surface, \ngreen is float collider for pushing the boat upwards, typically it needs to be below or at same level as BlockingCollider to prevent issues, \nYellow is onboardtrigger for calculating if player is onboard");
    var currentInstance = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();

    if (!currentInstance)
      currentInstance = VehicleDebugHelpers.GetOnboardMBRaftDebugHelper();

    if (!currentInstance) return;

    currentInstance?.StartRenderAllCollidersLoop();
  }

  /// <summary>
  /// For Developers, Modders, and normal users that need to frequently use ValheimRAFT commands.
  /// </summary>
  private void DrawVehicleDebugCommandsMenu()
  {
    GUILayout.BeginArea(new Rect(500, Screen.height - 510, 200, 500),
      myButtonStyle);

    if (GUILayout.Button("ConvexHull debugger"))
    {
      ToggleConvexHullDebugger();
    }

    if (GUILayout.Button("collider debugger"))
    {
      ToggleColliderDebugger();
    }

    if (GUILayout.Button("raftcreative"))
      CreativeModeConsoleCommand.RunCreativeModeCommand("raftcreative");

    if (GUILayout.Button("activatePendingPieces"))
      VehicleDebugHelpers.GetVehiclePiecesController()
        ?.StartActivatePendingPieces();

    if (GUILayout.Button("Zero Ship RotationXZ"))
      VehicleDebugHelpers.GetOnboardVehicleDebugHelper()?.FlipShip();

    if (GUILayout.Button("Toggle Ocean Sway"))
      VehicleCommands.VehicleToggleOceanSway();

    GUILayout.EndArea();
  }

  /// <summary>
  /// Meant for developers. Should never be enabled for players. These commands can be very destructive to the entire game world.
  /// </summary>
  [Conditional("DEBUG")]
  private void DrawDeveloperDebugCommandsWindow()
  {
#if DEBUG
    GUILayout.BeginArea(new Rect(250, Screen.height - 510, 200, 200),
      myButtonStyle);
    if (GUILayout.Button(
          $"ActivatePendingPieces {VehiclePiecesController.DEBUGAllowActivatePendingPieces}"))
    {
      VehiclePiecesController.DEBUGAllowActivatePendingPieces =
        !VehiclePiecesController.DEBUGAllowActivatePendingPieces;
      if (VehiclePiecesController.DEBUGAllowActivatePendingPieces)
        foreach (var vehiclePiecesController in VehiclePiecesController
                   .ActiveInstances.Values)
          vehiclePiecesController.StartActivatePendingPieces();
    }

    if (GUILayout.Button("Delete ShipZDO"))
    {
      var currentShip = VehicleDebugHelpers.GetVehiclePiecesController();
      if (currentShip != null)
        ZNetScene.instance.Destroy(currentShip?.VehicleInstance?.NetView?
          .gameObject);
    }

    if (GUILayout.Button("Set logoutpoint"))
    {
      var zdo = Player.m_localPlayer
        .GetComponentInParent<VehiclePiecesController>()?.VehicleInstance
        ?.NetView?
        .GetZDO();
      if (zdo != null) PlayerSpawnController.Instance?.SyncLogoutPoint(zdo);
    }

    if (GUILayout.Button("Move to current spawn"))
      PlayerSpawnController.Instance?.DEBUG_MoveTo(LocationVariation
        .Spawn);

    if (GUILayout.Button("Move to current logout"))
      PlayerSpawnController.Instance?.DEBUG_MoveTo(LocationVariation
        .Logout);

    if (GUILayout.Button("DebugFind PlayerSpawnController"))
    {
      var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
      foreach (var obj in allObjects)
        if (obj.name.Contains($"{PrefabNames.PlayerSpawnControllerObj}(Clone)"))
          Logger.LogDebug("found playerSpawn controller");
    }

    if (GUILayout.Button("Force Move Vehicle"))
    {
      var currentShip = VehicleDebugHelpers.GetVehiclePiecesController();
      if (currentShip != null)
      {
        var shipBody = currentShip.VehicleInstance?.Instance?.MovementController?.m_body;
        if (shipBody == null) return;
        shipBody.MovePosition(shipBody.position + Vector3.forward);
      }
    }

    if (GUILayout.Button("Delete All Vehicles"))
    {
      var allObjects = Resources.FindObjectsOfTypeAll<ZNetView>();
      foreach (var obj in allObjects)
        if (obj.name.Contains($"{PrefabNames.WaterVehicleShip}(Clone)") || obj.name.Contains($"{PrefabNames.LandVehicle}(Clone)"))
        {
          Logger.LogInfo($"Destroying {obj.name}");
          ZNetScene.instance.Destroy(obj.gameObject);
        }
    }

    GUILayout.EndArea();
#endif
  }

  private void OnGUI()
  {
    myButtonStyle ??= new GUIStyle(GUI.skin.button)
    {
      fontSize = 50
    };

    // DrawDeveloperDebugCommandsWindow();
    // DrawVehicleDebugCommandsMenu();
  }
}