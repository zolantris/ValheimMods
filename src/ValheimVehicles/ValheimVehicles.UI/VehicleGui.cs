#region

  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using DynamicLocations.Constants;
  using DynamicLocations.Controllers;
  using Jotunn.Managers;
  using TMPro;
  using UnityEngine;
  using UnityEngine.UI;
  using ValheimVehicles.Components;
  using ValheimVehicles.Config;
  using ValheimVehicles.ConsoleCommands;
  using ValheimVehicles.Constants;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.UI;
  using ValheimVehicles.Structs;
  using Logger = Jotunn.Logger;

#endregion

  namespace ValheimVehicles.UI;

  public class VehicleGui : SingletonBehaviour<VehicleGui>
  {
    private GUIStyle? myButtonStyle;
    private string ShipMovementOffsetText;
    private Vector3 _shipMovementOffset;
    public static TMP_Dropdown VehicleSelectDropdown;

    public static VehicleGui Gui;
    public static GameObject GuiObj;

    public static void AddRemoveVehicleGui()
    {
      if (Game.instance == null) return;
      if (GuiObj == null)
      {
        GuiObj = new GameObject("ValheimVehicles_VehicleGui")
        {
          transform = { parent = Game.instance.transform },
          layer = LayerHelpers.UILayer
        };
      }

      if (Gui == null)
      {
        Gui = GuiObj.GetComponent<VehicleGui>();
      }

      if (Gui == null)
      {
        Gui = GuiObj.AddComponent<VehicleGui>();
      }


      if (Instance != null)
      {
        Instance.InitPanel();
        SetCommandsPanelState(VehicleDebugConfig.VehicleDebugMenuEnabled.Value);
      }
    }

    private Vector3 GetShipMovementOffset()
    {
      var shipMovementVectors = ShipMovementOffsetText.Split(',');
      if (shipMovementVectors.Length != 3) return new Vector3(0, 0, 0);
      var x = float.Parse(shipMovementVectors[0]);
      var y = float.Parse(shipMovementVectors[1]);
      var z = float.Parse(shipMovementVectors[2]);
      return new Vector3(x, y, z);
    }


    public static string vehicleCommandsHide => $"{ModTranslations.GuiCommandsMenuTitle} ({ModTranslations.GuiHide})";
    public static string vehicleCommandsShow => $"{ModTranslations.GuiCommandsMenuTitle} ({ModTranslations.GuiShow})";

    // todo translate this
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

    // overlay buttons that control toggling the panel
    public static bool isCommandsToggleButtonVisible = false;
    public static bool isConfigPanelToggleButtonVisible = false;


    private GameObject configWindow;
    private GameObject commandsWindow;

    private GameObject commandsToggleButtonWindow;
    private GameObject configToggleButtonWindow;

    private List<GameObject> commandsPanelToggleObjects = [];
    // private List<GameObject> devCommandsPanelToggleObjects = [];
    private List<GameObject> configPanelToggleObjects = [];

    private bool hasInitialized = false;

    private const int panelHeight = 500;
    private const float buttonHeight = 60f;
    private const float buttonWidth = 350f;
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
      if (toggleWindow == null || panelWindow == null)
      {
        InitPanel();
      }

      if (toggleWindow != null && shouldDeactivateToggleButton)
      {
        toggleWindow.SetActive(isVisible);
      }

      if (panelWindow != null)
      {
        panelWindow.SetActive(isVisible);
        toggleObjects.ForEach((x) =>
        {
          if (x == null) return;
          x.SetActive(isVisible);
        });
      }
    }

    public static void HideOrShowCommandPanel(bool isVisible, bool canUpdateTogglePanel)
    {
      if (Instance == null)
      {
        AddRemoveVehicleGui();
        return;
      }
      Instance.HideOrShowPanel(isVisible, canUpdateTogglePanel, ref Instance.commandsToggleButtonWindow, ref Instance.commandsWindow, ref Instance.commandsPanelToggleObjects);
    }

    public static void HideOrShowVehicleConfigPanel(bool isVisible, bool canUpdateTogglePanel)
    {
      if (Instance == null)
      {
        AddRemoveVehicleGui();
        return;
      }
      Instance.HideOrShowPanel(isVisible, canUpdateTogglePanel, ref Instance.configToggleButtonWindow, ref Instance.configWindow, ref Instance.configPanelToggleObjects);
    }

    public static void SetCommandsPanelState(bool val)
    {
      hasCommandsWindowOpened = val;
      HideOrShowVehicleConfigPanel(val, false);
    }

    public static void ToggleCommandsPanelState(bool canUpdateTogglePanel)
    {
      hasCommandsWindowOpened = !hasCommandsWindowOpened;
      // this should not hide the actual commands panel button.
      HideOrShowCommandPanel(hasCommandsWindowOpened, canUpdateTogglePanel);
    }

    public void InitPanel()
    {
      if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
      {
        return;
      }

      CreateCommandsShortcutPanel();
      CreateVehicleConfigShortcutPanel();

      HideOrShowCommandPanel(hasCommandsWindowOpened, true);
      HideOrShowVehicleConfigPanel(hasConfigPanelOpened, true);

      hasInitialized = true;
    }

    public VehicleManager? targetInstance;

    private GenericInputAction _getCurrentVehicleGenericInputAction = new()
    {
      title = "Update current vehicle",
      OnButtonPress = () =>
      {
        if (Instance == null) return;
        var piecesController = VehicleDebugHelpers.GetVehiclePiecesController();
        if (piecesController == null)
        {
          Instance.targetInstance = null;
          return;
        }
        Instance.targetInstance = piecesController.Manager;
      }
    };


    public static void VehicleSelectOnDropdownChanged(int index)
    {
      var vehicles = VehicleStorageController.GetAllVehicles();

      // index 0 is a [None].
      if (index == 0)
      {
        VehicleStorageController.SelectedVehicle = "";
        return;
      }

      if (index > 0 && index <= vehicles.Count)
      {
        VehicleStorageController.SelectedVehicle = vehicles[index - 1].VehicleName;
        LoggerProvider.LogInfo($"Selected Vehicle: {VehicleStorageController.SelectedVehicle}");
      }
      else
      {
        LoggerProvider.LogWarning("No vehicles detected cannot select any vehicle.");
        VehicleStorageController.SelectedVehicle = "";
      }
    }

    public static GameObject AddDropdownWithAction(GenericInputAction genericInputAction, int index, float StartHeight, Transform parent)
    {
      var tmpDropdown = TMPDropdownFactory.CreateTMPDropDown(
        parent,
        new Vector2(0f, StartHeight - index * buttonHeight),
        new Vector2(buttonWidth, buttonHeight * 1.5f)
      );

      // it should never be null here.
      if (genericInputAction.OnDropdownChanged != null)
      {
        tmpDropdown.onValueChanged.AddListener(genericInputAction.OnDropdownChanged);
      }
      else
      {
        LoggerProvider.LogError("OnDropdownChanged not provided for a AddDropdownAction. This is an error with Valheim Vehicles. Please Report");
      }

      var refreshHandler = tmpDropdown.gameObject.AddComponent<DropdownRefreshOnHover>();
      if (genericInputAction.OnPointerEnterAction != null)
      {
        refreshHandler.OnPointerEnterAction = genericInputAction.OnPointerEnterAction;
      }

      if (genericInputAction.OnCreateDropdown != null)
      {
        genericInputAction.OnCreateDropdown(tmpDropdown);
      }

      return tmpDropdown.gameObject;
    }


    public GameObject AddInputWithAction(GenericInputAction genericInputAction, int index, float StartHeight, Transform windowTransform)
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


    public GameObject AddButtonWithAction(GenericInputAction genericInputAction, int index, float StartHeight, Transform windowTransform)
    {

      var buttonObj = GUIManager.Instance.CreateButton(
        genericInputAction.title,
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

      button.onClick.AddListener(() => genericInputAction.OnButtonPress());
      return buttonObj;
    }

    private GameObject CreateConfigTogglePanel()
    {
      var panel = DefaultControls.CreatePanel(
        GUIManager.Instance.ValheimControlResources
      );
      panel.name = "ValheimVehicles_configWindow";
      var dragWindowExtension = panel.AddComponent<DragWindowControllerExtension>();
      panel.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
      panel.GetComponent<Image>().pixelsPerUnitMultiplier = 1f;
      var panelTransform = (RectTransform)panel.transform;
      panelTransform.anchoredPosition = new Vector2(VehicleDebugConfig.VehicleConfigWindowPosX.Value, VehicleDebugConfig.VehicleConfigWindowPosY.Value);
      panelTransform.anchorMin = anchorMin;
      panelTransform.anchorMax = anchorMax;

      panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
      panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);

      dragWindowExtension.OnDragCalled += (rectTransform) =>
      {
        var anchoredPosition = rectTransform.anchoredPosition;
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
      var buttonText = buttonObject.GetComponentInChildren<Text>();

      // Add a listener to the button to close the panel again
      var button = buttonObject.GetComponent<Button>();
      button.onClick.AddListener(() =>
      {
        var nextState = !configWindow.activeSelf;
        buttonText.text = nextState ? vehicleConfigHide : vehicleConfigShow;
        HideOrShowVehicleConfigPanel(nextState, true);
      });

      panel.SetActive(hasConfigPanelOpened);

      return panel;
    }

    private void OnWindowCommandsPanelToggle(Text buttonText)
    {
      var nextState = !commandsWindow.activeSelf;
      buttonText.text = nextState ? vehicleCommandsHide : vehicleCommandsShow;
      HideOrShowCommandPanel(nextState, false);
    }

    private const string CommandsPanelWindowName = "ValheimVehicles_commandsWindow";

    /// <summary>
    /// Todo replace with PanelUtils.CreatePanel
    /// </summary>
    /// <returns></returns>
    private GameObject CreateCommandsTogglePanel()
    {
      var panelStyles = new Unity2dViewStyles
      {
        anchorMin = anchorMin,
        anchorMax = anchorMax
      };

      var buttonStyles = new Unity2dViewStyles
      {
        anchorMin = new Vector2(0.5f, 0.5f),
        anchorMax = new Vector2(0.5f, 0.5f),
        position = Vector2.zero,
        height = buttonHeight,
        width = buttonWidth
      };

      var panel = PanelUtil.CreateDraggableHideShowPanel(CommandsPanelWindowName, panelStyles, buttonStyles, vehicleCommandsHide, vehicleCommandsShow, GuiConfig.VehicleCommandsPanelLocation, OnWindowCommandsPanelToggle);

      // var panel = DefaultControls.CreatePanel(
      //   GUIManager.Instance.ValheimControlResources
      // );
      // panel.name = "ValheimVehicles_commandsWindow";
      // var dragWindowExtension = panel.AddComponent<DragWindowControllerExtension>();
      // panel.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
      // panel.GetComponent<Image>().pixelsPerUnitMultiplier = 1f;
      // var panelTransform = (RectTransform)panel.transform;
      // panelTransform.anchoredPosition = new Vector2(VehicleDebugConfig.CommandsWindowPosX.Value, VehicleDebugConfig.CommandsWindowPosY.Value);
      // panelTransform.anchorMin = anchorMin;
      // panelTransform.anchorMax = anchorMax;
      //
      // panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
      // panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);
      //
      // dragWindowExtension.OnDragCalled += (rectTransform) =>
      // {
      //   var anchoredPosition = rectTransform.anchoredPosition;
      //   VehicleDebugConfig.CommandsWindowPosX.Value = anchoredPosition.x;
      //   VehicleDebugConfig.CommandsWindowPosY.Value = anchoredPosition.y;
      // };
      // // Create the button object above the gui manager. So it can hide itself.
      // var buttonObject = GUIManager.Instance.CreateButton(
      //   vehicleCommandsHide,
      //   panel.transform,
      //   new Vector2(0.5f, 0.5f),
      //   new Vector2(0.5f, 0.5f),
      //   new Vector2(0, 0),
      //   buttonWidth,
      //   buttonHeight);
      // var buttonText = buttonObject.GetComponentInChildren<Text>();
      //
      // // Add a listener to the button to close the panel again
      // var button = buttonObject.GetComponent<Button>();
      // button.onClick.AddListener(() =>
      // {
      //   OnWindowCommandsPanelToggle(buttonText);
      // });
      //
      // panel.SetActive(hasCommandsWindowOpened);

      return panel;
    }

    private void CreateVehicleConfigShortcutPanel()
    {
      if (configToggleButtonWindow != null && configWindow != null) return;

      configToggleButtonWindow = CreateConfigTogglePanel();

      var dynamicPanelHeight = VehicleGUIItems.configSections.Count * buttonHeight + VehicleGUIItems.configSections.Count * 5;
      configWindow = GUIManager.Instance.CreateWoodpanel(
        configToggleButtonWindow.transform,
        new Vector2(0.5f, 0f),
        new Vector2(0.5f, 0f),
        new Vector2(0, -(dynamicPanelHeight / 2 + 15f)),
        500f,
        Math.Min(1000f, Screen.height * 0.8f),
        true);
      configWindow.SetActive(hasConfigPanelOpened);

      var startHeight = dynamicPanelHeight / 2f - buttonHeight / 2;
      for (var index = 0; index < VehicleGUIItems.configSections.Count; index++)
      {
        var inputAction = VehicleGUIItems.configSections[index];
        var obj = AddInputWithAction(inputAction, index, startHeight, configWindow.transform);
        configPanelToggleObjects.Add(obj);
      }
    }

    private static bool CanAddAdminCommand()
    {
      if (ZNet.instance == null) return false;
      if (ZNet.instance.LocalPlayerIsAdminOrHost() || VehicleDebugConfig.AllowDebugCommandsForNonAdmins.Value) return true;
      return false;
    }

    private void CreateCommandsShortcutPanel()
    {
      if (commandsToggleButtonWindow != null) return;

      commandsToggleButtonWindow = CreateCommandsTogglePanel();

      var dynamicPanelHeight = VehicleGUIItems.commandButtonActions.Count * buttonHeight + VehicleGUIItems.commandButtonActions.Count * 5;
      commandsWindow = GUIManager.Instance.CreateWoodpanel(
        commandsToggleButtonWindow.transform,
        new Vector2(0.5f, 0f),
        new Vector2(0.5f, 0f),
        new Vector2(0, -(dynamicPanelHeight / 2 + 15f)),
        panelWidth,
        dynamicPanelHeight,
        false);
      commandsWindow.SetActive(hasCommandsWindowOpened);

      var startHeight = dynamicPanelHeight / 2f - buttonHeight / 2;
      for (var index = 0; index < VehicleGUIItems.commandButtonActions.Count; index++)
      {
        var genericActionElement = VehicleGUIItems.commandButtonActions[index];
        GameObject obj;

        // prevent non-admins from seeing debug/hack commands.
        if (genericActionElement.IsAdminOnly && !CanAddAdminCommand()) continue;

        switch (genericActionElement.inputType)
        {
          case InputType.Dropdown:
            obj = AddDropdownWithAction(genericActionElement, index, startHeight, commandsWindow.transform);
            break;
          case InputType.Input:
            obj = AddInputWithAction(genericActionElement, index, startHeight, commandsWindow.transform);
            break;
          case InputType.Button:
            obj = AddButtonWithAction(genericActionElement, index, startHeight, commandsWindow.transform);
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
        if (obj != null)
        {
          commandsPanelToggleObjects.Add(obj);
        }
      }
    }

    public static void ToggleConvexHullDebugger()
    {
      Logger.LogMessage(
        "Toggling convex hull debugger on the ship. This will show/hide the current convex hulls.");
      var currentInstance = VehicleCommands.GetNearestVehicleShip(Player.m_localPlayer.transform.position);

      if (currentInstance == null || currentInstance.PiecesController == null) return;

      var convexHullComponent = currentInstance
        .PiecesController.convexHullComponent;

      convexHullComponent.PreviewMode =
        convexHullComponent.PreviewMode switch
        {
          ConvexHullAPI.PreviewModes.None => ConvexHullAPI.PreviewModes.Bubble,
          ConvexHullAPI.PreviewModes.Bubble => ConvexHullAPI.PreviewModes.Debug,
          _ => ConvexHullAPI.PreviewModes.Bubble
        };

      currentInstance.PiecesController.convexHullComponent
        .CreatePreviewConvexHullMeshes();
    }


    public static void ToggleColliderDebugger()
    {
      Logger.LogMessage(
        "Collider debugger called, \nblue = BlockingCollider for collisions and keeping boat on surface, \ngreen is float collider for pushing the boat upwards, typically it needs to be below or at same level as BlockingCollider to prevent issues, \nYellow is onboardtrigger for calculating if player is onboard");
      var currentShip = VehicleCommands.GetNearestVehicleShip(Player.m_localPlayer.transform.position);
      if (currentShip == null) return;
      currentShip.Instance.HasVehicleDebugger = !currentShip.Instance.HasVehicleDebugger;
      var currentInstance = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();
      if (currentInstance == null) return;
      currentInstance.StartRenderAllCollidersLoop();
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
        VehicleCommands.ToggleCreativeMode();

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
        if (currentShip != null && currentShip.m_nview != null && ZNetScene.instance != null)
          ZNetScene.instance.Destroy(currentShip.m_nview
            .gameObject);
      }

      if (GUILayout.Button("Set logoutpoint"))
      {
        var zdo = Player.m_localPlayer
          .GetComponentInParent<VehiclePiecesController>().m_nview
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
        var currentVehicle = VehicleDebugHelpers.GetVehiclePiecesController();
        if (currentVehicle != null)
        {
          var shipBody = currentVehicle?.MovementController?.m_body;
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