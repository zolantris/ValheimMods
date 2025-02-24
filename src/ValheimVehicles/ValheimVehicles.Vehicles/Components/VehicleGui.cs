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

public class VehicleGui : SingletonBehaviour<VehicleGui>
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

  public const string vehicleCommandsHide = "Vehicle Commands (Hide)";
  public const string vehicleCommandsShow = "Vehicle Commands (Show)";

  public const string vehicleConfigHide = "Vehicle Config (Hide)";
  public const string vehicleConfigShow = "Vehicle Config (Show)";

  public static bool vehicleDebugPhysicsSync = true;

  private int buttonFontSize = 16;

  private int titleFontSize = 18;

  private GUIStyle buttonStyle;

  private GUIStyle labelStyle;

  // private GameObject devCommandsWindow;
  public static bool hasCommandsWindowOpened = false;
  public static bool hasConfigPanelOpened = false;


  private GameObject configWindow;
  private GameObject commandsWindow;

  private GameObject commandsToggleWindow;
  private GameObject configToggleWindow;

  private List<GameObject> commandsPanelToggleObjects = [];
  // private List<GameObject> devCommandsPanelToggleObjects = [];
  private List<GameObject> configPanelToggleObjects = [];
  
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
    buttonFontSize = VehicleDebugConfig.ButtonFontSize.Value;
    titleFontSize = VehicleDebugConfig.TitleFontSize.Value;
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
    // devCommandsPanelToggleObjects.Clear();
    commandsPanelToggleObjects.Clear();
    configPanelToggleObjects.Clear();
    if (commandsWindow != null) Destroy(commandsWindow);
    if (configWindow != null) Destroy(configWindow);
  }

  public bool lastPanelState = false;

  public static void ToggleConfigPanelState(bool shouldHideShowButton = false)
  {
    hasConfigPanelOpened = !hasConfigPanelOpened;
    HideOrShowVehicleConfigPanel(hasConfigPanelOpened, shouldHideShowButton);
  }

  public static void SetConfigPanelState(bool val)
  {
    hasConfigPanelOpened = val;
    HideOrShowVehicleConfigPanel(val, true);
  }

  public void HideOrShowPanel(bool isVisible, bool shouldDeactivateToggleButton, ref GameObject toggleWindow, ref GameObject panelWindow, ref List<GameObject> toggleObjects)
  {
    if (Instance == null) return;
    if (toggleWindow != null && shouldDeactivateToggleButton)
    {
      toggleWindow.SetActive(isVisible);
    }

    if (panelWindow != null)
    {
      panelWindow.SetActive(isVisible);
      toggleObjects.ForEach((x) =>
      {
        x.SetActive(isVisible);
      });
    }
  }

  public static void HideOrShowDebugCommandPanel(bool isVisible, bool shouldDeactivateToggleButton = false)
  {
    if (Instance == null) return;
    Instance.HideOrShowPanel(isVisible, shouldDeactivateToggleButton, ref Instance.commandsToggleWindow, ref Instance.commandsWindow, ref Instance.commandsPanelToggleObjects);
  }

  public static void HideOrShowVehicleConfigPanel(bool isVisible, bool shouldDeactivateToggleButton = false)
  {
    if (Instance == null) return;
    Instance.HideOrShowPanel(isVisible, shouldDeactivateToggleButton, ref Instance.configToggleWindow, ref Instance.configWindow, ref Instance.configPanelToggleObjects);
  }

  public static void SetCommandsPanelState(bool val)
  {
    hasCommandsWindowOpened = val;
    HideOrShowVehicleConfigPanel(val, true);
  }

  public static void ToggleCommandsPanelState()
  {
    hasCommandsWindowOpened = !hasCommandsWindowOpened;
    HideOrShowDebugCommandPanel(hasCommandsWindowOpened, true);
  }

  private void InitPanel()
  {
    if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
    {
      return;
    }

    CreateCommandsShortcutPanel();
    CreateVehicleConfigShortcutPanel();
    
    if (VehicleDebugConfig.VehicleDebugMenuEnabled.Value)
    {
      HideOrShowDebugCommandPanel(true);
    }
    else
    {
      ToggleCommandsPanelState();
    }

    HideOrShowVehicleConfigPanel(hasConfigPanelOpened);
    
    hasInitialized = true;
  }

  public struct ButtonAction
  {
    public string title;
    public Action action;
  }

  public struct InputAction
  {
    public string title;
    public string description;
    public Action saveAction;
    public Action resetAction;
  }

  public VehicleShip? targetInstance;

  private ButtonAction GetCurrentVehicleButtonAction = new()
  {
    title = "Update current vehicle",
    action = () =>
    {
      if (Instance == null) return;
      var piecesController = VehicleDebugHelpers.GetVehiclePiecesController();
      if (piecesController == null)
      {
        Instance.targetInstance = null;
        return;
      }
      Instance.targetInstance = piecesController.VehicleInstance?.Instance;
    }
  };

  private static List<InputAction> configSections =
  [
    new()
    {
      title = "Treads Max Width",
      description = "Set the max width of treads",
      saveAction = () =>
      {
      },
      resetAction = () => {}
    }
  ];


  private static List<ButtonAction> commandButtonActions =
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
    },
    new()
    {
      title = "Config",
      action = () =>
      {
        ToggleConfigPanelState(true);
      }
    }
  ];

  public GameObject AddInputWithAction(InputAction inputAction, int index, float StartHeight, Transform windowTransform)
  {

    var buttonObj = GUIManager.Instance.CreateInputField(
      windowTransform,
      new Vector2(0.5f, 0.5f),
      new Vector2(0.5f, 0.5f),
      new Vector2(0, StartHeight - index * buttonHeight),
      InputField.ContentType.IntegerNumber, "8"
    );
    buttonObj.SetActive(true);
    // Add a listener to the button to close the panel again
    var inputField = buttonObj.GetComponent<InputField>();
    // var text = buttonObj.GetComponent<Text>();?
    // if (inputField != null && inputField.placeholder)
    // {
    //   inputField.placeholder.textfontSize = buttonFontSize;
    // }
    inputField.onSubmit.AddListener((x) =>
    {
      var intString = float.TryParse(x, out var value);
      if (intString)
      {
        Logger.LogDebug($"Converted string to float {value}");
      }
      else
      {
        Logger.LogDebug($"Not a string {x}");
      }
    });

    // button.OnSubmit(() =>
    // {
    //   return inputAction.saveAction;
    // });
    return buttonObj;
  }


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

  private GameObject CreateConfigTogglePanel()
  {
    var panel = DefaultControls.CreatePanel(
      GUIManager.Instance.ValheimControlResources
    );
    panel.name = "ValheimVehicles_configWindow";
    var dragWindowExtension = panel.AddComponent<DragWindowCntrlExtension>();
    panel.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
    panel.GetComponent<Image>().pixelsPerUnitMultiplier = 1f;
    var panelTransform = (RectTransform)panel.transform;
    panelTransform.anchoredPosition = new Vector2(VehicleDebugConfig.VehicleConfigWindowPosX.Value, VehicleDebugConfig.VehicleConfigWindowPosY.Value);
    panelTransform.anchorMin = anchorMin;
    panelTransform.anchorMax = anchorMax;

    panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
    panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);

    dragWindowExtension.OnDragCalled += () =>
    {
      var anchoredPosition = panelTransform.anchoredPosition;
      VehicleDebugConfig.VehicleConfigWindowPosX.Value = anchoredPosition.x;
      VehicleDebugConfig.VehicleConfigWindowPosY.Value = anchoredPosition.y;
    };

    // Create the button object above the gui manager. So it can hide itself.
    var buttonObject = GUIManager.Instance.CreateButton(
      vehicleConfigHide,
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
      var nextState = !configWindow.activeSelf;
      buttonText.text = nextState ? vehicleConfigHide : vehicleConfigShow;
      HideOrShowVehicleConfigPanel(nextState);
    });

    return panel;
  }

  private GameObject CreateCommandsTogglePanel()
  {
    var panel = DefaultControls.CreatePanel(
      GUIManager.Instance.ValheimControlResources
    );
    panel.name = "ValheimVehicles_commandsWindow";
    var dragWindowExtension = panel.AddComponent<DragWindowCntrlExtension>();
    panel.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
    panel.GetComponent<Image>().pixelsPerUnitMultiplier = 1f;
    var panelTransform = (RectTransform)panel.transform;
    panelTransform.anchoredPosition = new Vector2(VehicleDebugConfig.CommandsWindowPosX.Value, VehicleDebugConfig.CommandsWindowPosY.Value);
    panelTransform.anchorMin = anchorMin;
    panelTransform.anchorMax = anchorMax;

    panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
    panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);

    dragWindowExtension.OnDragCalled += () =>
    {
      var anchoredPosition = panelTransform.anchoredPosition;
      VehicleDebugConfig.CommandsWindowPosX.Value = anchoredPosition.x;
      VehicleDebugConfig.CommandsWindowPosY.Value = anchoredPosition.y;
    };
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
      var nextState = !commandsWindow.activeSelf;
      buttonText.text = nextState ? vehicleCommandsHide : vehicleCommandsShow;
      HideOrShowDebugCommandPanel(nextState);
    });

    return panel;
  }

  private void CreateVehicleConfigShortcutPanel()
  {
    if (configToggleWindow != null && configWindow != null) return;

    configToggleWindow = CreateConfigTogglePanel();

    var dynamicPanelHeight = configSections.Count * buttonHeight + configSections.Count * 5;
    configWindow = GUIManager.Instance.CreateWoodpanel(
      configToggleWindow.transform,
      new Vector2(0.5f, 0f),
      new Vector2(0.5f, 0f),
      new Vector2(0, -(dynamicPanelHeight / 2 + 15f)),
      500f,
      Math.Min(1000f, Screen.height * 0.8f),
      true);
    configWindow.SetActive(false);

    var startHeight = dynamicPanelHeight / 2f - buttonHeight / 2;
    for (var index = 0; index < configSections.Count; index++)
    {
      var inputAction = configSections[index];
      var obj = AddInputWithAction(inputAction, index, startHeight, configWindow.transform);
      configPanelToggleObjects.Add(obj);
    }
  }

  private void CreateCommandsShortcutPanel()
  {
    if (commandsToggleWindow != null) return;

    commandsToggleWindow = CreateCommandsTogglePanel();

    var dynamicPanelHeight = commandButtonActions.Count * buttonHeight + commandButtonActions.Count * 5;
    commandsWindow = GUIManager.Instance.CreateWoodpanel(
      commandsToggleWindow.transform,
      new Vector2(0.5f, 0f),
      new Vector2(0.5f, 0f),
      new Vector2(0, -(dynamicPanelHeight / 2 + 15f)),
      panelWidth,
      dynamicPanelHeight,
      false);
    commandsWindow.SetActive(false);

    var startHeight = dynamicPanelHeight / 2f - buttonHeight / 2;
    for (var index = 0; index < commandButtonActions.Count; index++)
    {
      var buttonAction = commandButtonActions[index];
      var obj = AddButtonWithAction(buttonAction, index, startHeight, commandsWindow.transform);
      commandsPanelToggleObjects.Add(obj);
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
}