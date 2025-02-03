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
  private GameObject modderCommandToggleWindow;
  private List<GameObject> modderCommandsPanelToggleObjects = [];
  private List<GameObject> devCommandsPanelToggleObjects = [];
  private bool hasInitialized = false;

  private const int panelHeight = 500;
  private const float buttonHeight = 60f;
  private const float buttonWidth = 150f;
  private static float panelWidth = buttonWidth * 1.1f;

  private static readonly Vector2 anchorMin = new(0f, 0.5f);
  private static readonly Vector2 anchorMax = new(0.5f, 0.5f);
  private static readonly Vector2 panelPosition = Vector2.zero;
  private static readonly Vector3 buttonHeightVector3 = Vector3.up * buttonHeight;
  private static readonly Vector3 panelHeightVector3 = Vector3.up * panelHeight / 2f;

  private void Start()
  {
    windowRect.x = VehicleDebugConfig.windowPosX.Value;
    windowRect.y = VehicleDebugConfig.windowPosY.Value;
    buttonFontSize = VehicleDebugConfig.buttonFontSize.Value;
    titleFontSize = VehicleDebugConfig.TitleFontSize.Value;
    windowRect.width = VehicleDebugConfig.windowWidth.Value;
    windowRect.height = VehicleDebugConfig.windowHeight.Value;
    hasInitialized = false;

    GUIManager.OnCustomGUIAvailable += InitPanel;
  }

  private void OnEnable()
  {
    hasInitialized = false;
    InitPanel();
  }

  private void OnDisable()
  {
    devCommandsPanelToggleObjects.Clear();
    modderCommandsPanelToggleObjects.Clear();
    if (devCommandsWindow != null) Destroy(devCommandsWindow);
    if (modderCommandsWindow != null) Destroy(modderCommandsWindow);
  }

  public bool lastPanelState = false;

  public static void HideOrShowPanel(bool isVisible)
  {
    if (Instance == null) return;
    if (Instance.devCommandsWindow)
    {
      Instance.devCommandsWindow.SetActive(isVisible);
      Instance.devCommandsPanelToggleObjects.ForEach((x) =>
      {
        x.SetActive(isVisible);
      });
    }

    if (Instance.modderCommandsWindow)
    {
      Instance.modderCommandsWindow.SetActive(isVisible);
    }
  }

  private void TogglePanelState()
  {
    var state = !hasInitialized || !modderCommandsWindow.activeSelf;
    HideOrShowPanel(state);
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

    if (VehicleDebugConfig.VehicleDebugMenuEnabled.Value)
    {
      HideOrShowPanel(true);
    }
    else
    {
      TogglePanelState();
    }
    hasInitialized = true;
  }

  public struct ButtonAction
  {
    public string title;
    public Action action;
  }


  private static List<ButtonAction> buttonActions =
  [
    new()
    {
      title = "Hull debugger",
      action = () =>
      {
        ToggleConvexHullDebugger();
      }
    },
    new()
    {
      title = "Physics Debugger",
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


  public GameObject AddButtonWithAction(ButtonAction buttonAction, int index, float StartHeight, Transform windowTransform)
  {

    var buttonObj = GUIManager.Instance.CreateButton(
      buttonAction.title,
      windowTransform,
      new Vector2(0.5f, 0.5f),
      new Vector2(0.5f, 0.5f),
      new Vector2(0, StartHeight - index * buttonHeight),
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
    return buttonObj;
  }

  private GameObject CreateModderCommandsTogglePanel()
  {
    var panel = DefaultControls.CreatePanel(
      GUIManager.Instance.ValheimControlResources
    );
    panel.name = "ValheimVehicles_modderCommandsWindow_commands";
    panel.AddComponent<DragWindowCntrlExtension>();
    panel.transform.SetParent(GUIManager.CustomGUIBack.transform, false);
    panel.GetComponent<Image>().pixelsPerUnitMultiplier = 1f;
    var panelTransform = (RectTransform)panel.transform;
    panelTransform.anchoredPosition = panelPosition;
    panelTransform.anchorMin = anchorMin;
    panelTransform.anchorMax = anchorMax;

    panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
    panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);

    // Create the text object
    // GUIManager.Instance.CreateText(
    //   "Vehicle Shortcuts",
    //   panel.transform,
    //   new Vector2(0.5f, 0.5f),
    //   new Vector2(0.5f, 0.5f),
    //   new Vector2(0f, 0f),
    //   GUIManager.Instance.AveriaSerifBold,
    //   titleFontSize,
    //   GUIManager.Instance.ValheimOrange,
    //   true,
    //   Color.black,
    //   200f,
    //   buttonHeight,
    //   true);

    const string vehicleCommandsHide = "VehicleCommands (Hide)";
    const string vehicleCommandsShow = "VehicleCommands (Show)";
    // Create the button object above the gui manager. So it can hide itself.
    var buttonObject = GUIManager.Instance.CreateButton(
      vehicleCommandsHide,
      panel.transform,
      new Vector2(0.5f, 0.5f),
      new Vector2(0.5f, 0.5f),
      new Vector2(0, 0),
      buttonWidth,
      buttonHeight);
    buttonObject.SetActive(true);
    var buttonText = buttonObject.GetComponentInChildren<Text>();

    // Add a listener to the button to close the panel again
    var button = buttonObject.GetComponent<Button>();
    button.onClick.AddListener(() =>
    {
      var nextState = !modderCommandsWindow.activeSelf;
      buttonText.text = nextState ? vehicleCommandsHide : vehicleCommandsShow;
      HideOrShowPanel(nextState);
    });

    return panel;
  }


  private void CreateShortcutPanel()
  {
    if (modderCommandsWindow != null) return;

    modderCommandToggleWindow = CreateModderCommandsTogglePanel();

    var panelDrag = modderCommandToggleWindow.GetComponent<DragWindowCntrlExtension>();
    var dynamicPanelHeight = buttonActions.Count * buttonHeight + buttonActions.Count * 5;
    modderCommandsWindow = GUIManager.Instance.CreateWoodpanel(
      modderCommandToggleWindow.transform,
      new Vector2(0.5f, 0f),
      new Vector2(0.5f, 0f),
      new Vector2(0, -(dynamicPanelHeight / 2 + 15f)),
      panelWidth,
      dynamicPanelHeight,
      false);
    modderCommandsWindow.SetActive(false);
    var modderCommandsPanelOffset = -(Vector3.up * (dynamicPanelHeight / 2f));

    // panelDrag.OnDragCalled = () =>
    // {
    //   modderCommandsWindow.transform.position = modderCommandToggleWindow.transform.position + modderCommandsPanelOffset;
    // };

    // modderCommandsWindow.transform.position = modderCommandToggleWindow.transform.position + modderCommandsPanelOffset;

    var startHeight = dynamicPanelHeight / 2f - buttonHeight / 2;
    for (var index = 0; index < buttonActions.Count; index++)
    {
      var buttonAction = buttonActions[index];
      var obj = AddButtonWithAction(buttonAction, index, startHeight, modderCommandsWindow.transform);
      modderCommandsPanelToggleObjects.Add(obj);
    }
  }

  private static void ToggleConvexHullDebugger()
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


  private static void ToggleColliderDebugger()
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