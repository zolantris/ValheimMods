#region

  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Diagnostics.CodeAnalysis;
  using DynamicLocations.Constants;
  using DynamicLocations.Controllers;
  using Jotunn.Managers;
  using TMPro;
  using UnityEngine;
  using UnityEngine.UI;
  using ValheimVehicles.Components;
  using ValheimVehicles.BepInExConfig;
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

      if (commandsToggleButtonWindow) Destroy(commandsToggleButtonWindow);
      if (configToggleButtonWindow) Destroy(configToggleButtonWindow);
      if (commandsWindow) Destroy(commandsWindow);
      if (configWindow) Destroy(configWindow);
      if (GuiObj) Destroy(GuiObj);
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

      // also hide config panel. But do not show it.
      if (!hasCommandsWindowOpened)
      {
        HideOrShowVehicleConfigPanel(hasCommandsWindowOpened, true);
      }
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
        var vehicleManager = VehicleCommands.GetNearestVehicleManager();
        if (vehicleManager == null)
        {
          Instance.targetInstance = null;
          return;
        }
        Instance.targetInstance = vehicleManager;
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
      var panelStyles = new Unity2dViewStyles
      {
        anchorMin = anchorMin,
        anchorMax = anchorMax
      };

      var buttonStyles = new Unity2dViewStyles
      {
        anchorMin = new Vector2(0.5f, 0f),
        anchorMax = new Vector2(0.5f, 1f),
        position = Vector2.zero,
        height = buttonHeight,
        width = buttonWidth
      };

      var panel = PanelUtil.CreateDraggableHideShowPanel(ConfigPanelWindowName, panelStyles, buttonStyles, vehicleConfigHide, vehicleConfigShow, GuiConfig.VehicleCommandsPanelLocation, OnConfigCommandsPanelToggle);
      return panel;
    }

    private void OnConfigCommandsPanelToggle(Text buttonText)
    {
      var nextState = !configWindow.activeSelf;
      HideOrShowVehicleConfigPanel(nextState, false);
    }

    private void OnWindowCommandsPanelToggle(Text buttonText)
    {
      var nextState = !commandsWindow.activeSelf;
      buttonText.text = nextState ? vehicleCommandsHide : vehicleCommandsShow;
      HideOrShowCommandPanel(nextState, false);
    }

    private const string CommandsPanelWindowName = "ValheimVehicles_commandsWindow";
    private const string ConfigPanelWindowName = "ValheimVehicles_configWindow";

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
        anchorMin = new Vector2(0.5f, 0f),
        anchorMax = new Vector2(0.5f, 1f),
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

    public static GameObject? ConfigScrollView;

    public static Slider? LandVehicleTreadDistance_Slider;
    public static Slider? LandVehicleTreadScale_Slider;

    public static Slider? WaterFloatation_Slider;
    public static Toggle? WaterFloatation_Toggle;
    public static GameObject? WaterFloatationSliderRow;

    public static VehicleManager? CurrentSelectedVehicle;


    public static bool IsEditing = false;
    private static TextMeshProUGUI _saveStatus;
    private static Button _resetButton;

    public virtual void SetSavedState()
    {
      IsEditing = false;
      if (_saveStatus) _saveStatus.text = SwivelUIPanelStrings.Saved;
    }

    public static VehicleCustomConfig _tempVehicleConfig = new();

    public static void OnConfigSave(bool isReset)
    {
      if (!TryUpdateNearestVehicle(out var manager)) return;
      if (isReset)
      {
        _tempVehicleConfig = new VehicleCustomConfig();
      }
      manager.VehicleConfigSync.Config.ApplyFrom(_tempVehicleConfig);
      manager.VehicleConfigSync.Save(manager.m_nview.GetZDO(), [VehicleCustomConfig.Key_TreadDistance, VehicleCustomConfig.Key_CustomFloatationHeight]);

      if (manager.LandMovementController)
      {
        manager.UpdateLandMovementControllerProperties();
      }

      if (manager.PiecesController)
      {
        manager.PiecesController.ForceRebuildBounds();
      }
    }
    public static void UnsetSavedState()
    {
      IsEditing = true;
      if (_saveStatus) _saveStatus.text = SwivelUIPanelStrings.Save;
    }

    public static bool TryUpdateNearestVehicle([NotNullWhen(true)] out VehicleManager? manager)
    {
      if (!CurrentSelectedVehicle)
      {
        CurrentSelectedVehicle = VehicleCommands.GetNearestVehicleManager();
      }
      manager = CurrentSelectedVehicle;
      return CurrentSelectedVehicle != null;
    }

    private void CreateVehicleConfigShortcutPanel()
    {
      // We destroy every time so we can refresh this panel to be accurate.
      if (configToggleButtonWindow != null)
      {
        Destroy(configToggleButtonWindow);
      }
      if (configWindow)
      {
        Destroy(configWindow);
      }
      if (configToggleButtonWindow != null && configWindow != null) return;

      // syncs the config to our local copy so we never mutate the original.
      _tempVehicleConfig = new VehicleCustomConfig();
      if (TryUpdateNearestVehicle(out var manager))
      {
        manager.Config.ApplyTo(_tempVehicleConfig);
      }

      configToggleButtonWindow = CreateConfigTogglePanel();


      if (!configToggleButtonWindow) return;

      // var dynamicPanelHeight = VehicleGUIItems.configSections.Count * buttonHeight + VehicleGUIItems.configSections.Count * 5;
      var height = Mathf.Min(1000f, Screen.height * 0.8f);
      var width = Mathf.Min(700f, Screen.width * 0.8f);

      // insets the whole scrollpanel and center aligns it
      configWindow = GUIManager.Instance.CreateWoodpanel(
        configToggleButtonWindow.transform,
        new Vector2(0.5f, 0f),
        new Vector2(0.5f, 1f),
        new Vector2(0, 0f),
        width,
        height,
        true);
      configWindow.SetActive(hasConfigPanelOpened);

      var windowVerticalGroup = configWindow.AddComponent<VerticalLayoutGroup>();
      windowVerticalGroup.padding = new RectOffset(16, 16, 16, 16);
      windowVerticalGroup.childForceExpandHeight = false;
      windowVerticalGroup.childForceExpandWidth = true;
      windowVerticalGroup.childControlWidth = true;
      windowVerticalGroup.childControlWidth = true;

      var scrollWidth = 500f;
      ConfigScrollView = GUIManager.Instance.CreateScrollView(configWindow.transform, false, true, 20, 10f, GUIManager.Instance.ValheimToggleColorBlock, new Color(0, 0, 0, 1), scrollWidth, height);

      //allow togglepanel to let children expand
      var viewport = ConfigScrollView.transform.Find("Scroll View/Viewport/Content");
      var viewportVerticalLayout = viewport.GetComponent<VerticalLayoutGroup>();
      windowVerticalGroup.padding = new RectOffset(16, 16, 16, 16);
      viewportVerticalLayout.childForceExpandHeight = true;
      viewportVerticalLayout.childForceExpandWidth = true;
      viewportVerticalLayout.childControlWidth = true;
      viewportVerticalLayout.spacing = 16;

      // ensures the scrollview is able to work within a VerticalLayoutGroup.
      var scrollViewLayoutElement = ConfigScrollView.AddComponent<LayoutElement>();
      scrollViewLayoutElement.flexibleHeight = 600f;
      scrollViewLayoutElement.minHeight = 200f;
      scrollViewLayoutElement.minWidth = scrollWidth;

      var scrollViewVerticalLayout = ConfigScrollView.GetComponentInChildren<VerticalLayoutGroup>();

      if (!scrollViewVerticalLayout) return;

      // var startHeight = height / 2f - buttonHeight / 2;
      var MinTargetOffset = -5;
      var MaxTargetOffset = 20;

      var viewStyles = new SwivelUISharedStyles();
      var svParent = scrollViewVerticalLayout.transform;

      if (manager == null)
      {
        SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleCommand_Message_VehicleNotFound);
        return;
      }

      var sliderWidth = scrollWidth * 0.8f;

      if (!manager.IsLandVehicle)
      {
        // water vehicles

        // water floatation height
        SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleConfig_WaterVehicle_Section);
        SwivelUIHelpers.AddToggleRow(svParent, viewStyles, ModTranslations.VehicleConfig_CustomFloatationHeight, _tempVehicleConfig.HasCustomFloatationHeight, v =>
        {
          _tempVehicleConfig.HasCustomFloatationHeight = v;
          if (WaterFloatationSliderRow != null) WaterFloatationSliderRow.gameObject.SetActive(v);
          UnsetSavedState();
        }, out WaterFloatation_Toggle);

        WaterFloatationSliderRow = SwivelUIHelpers.AddSliderRow(svParent, viewStyles, ModTranslations.VehicleConfig_CustomFloatationHeight, -25f, 25f, manager.Config.CustomFloatationHeight, v =>
        {
          _tempVehicleConfig.HasCustomFloatationHeight = true;
          _tempVehicleConfig.CustomFloatationHeight = v;
          UnsetSavedState();
        }, out WaterFloatation_Slider, sliderWidth);
      }

      if (manager.IsLandVehicle)
      {
        // land vehicles
        SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleConfig_LandVehicle_Section);

        // distance
        SwivelUIHelpers.AddSliderRow(svParent, viewStyles, ModTranslations.VehicleConfig_TreadsDistance, MinTargetOffset, MaxTargetOffset, manager.Config.TreadDistance, v =>
        {
          _tempVehicleConfig.TreadDistance = v;
          UnsetSavedState();
        }, out LandVehicleTreadDistance_Slider, sliderWidth);
        // scale
        SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleConfig_TreadsScale);
        SwivelUIHelpers.AddSliderRow(svParent, viewStyles, ModTranslations.VehicleConfig_TreadsScale, MinTargetOffset, MaxTargetOffset, manager.Config.TreadScaleX, v =>
        {
          _tempVehicleConfig.TreadScaleX = v;
          UnsetSavedState();
        }, out LandVehicleTreadScale_Slider, sliderWidth);
      }

      // action buttons
      var actionButtonRow = SwivelUIHelpers.AddRowWithButton(configWindow.transform, viewStyles, null, SwivelUIPanelStrings.Save, 96f, 48f, out _saveStatus, () =>
      {
        OnConfigSave(false);
        SetSavedState();
      });

      var buttonGO = SwivelUIHelpers.AddButton(actionButtonRow.transform, viewStyles, ModTranslations.SharedKeys_Reset, 96f, 48f, out _resetButton, out _, () =>
      {
        _tempVehicleConfig = new VehicleCustomConfig();

        if (manager.IsLandVehicle && LandVehicleTreadDistance_Slider && LandVehicleTreadScale_Slider)
        {
          LandVehicleTreadDistance_Slider.SetValueWithoutNotify(_tempVehicleConfig.TreadDistance);
          LandVehicleTreadScale_Slider.SetValueWithoutNotify(_tempVehicleConfig.TreadScaleX);
        }
        WaterFloatation_Slider.SetValueWithoutNotify(_tempVehicleConfig.CustomFloatationHeight);
        WaterFloatation_Toggle.SetIsOnWithoutNotify(_tempVehicleConfig.HasCustomFloatationHeight);
        WaterFloatationSliderRow.gameObject.SetActive(_tempVehicleConfig.HasCustomFloatationHeight);
        UnsetSavedState();
      });
      buttonGO.transform.SetSiblingIndex(0);
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
        new Vector2(0.5f, 1f),
        new Vector2(0, 0f),
        panelWidth,
        dynamicPanelHeight,
        false);
      commandsWindow.SetActive(hasCommandsWindowOpened);

      var startHeight = 0f;
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
      var currentInstance = VehicleCommands.GetNearestVehicleManager();

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
      var currentShip = VehicleCommands.GetNearestVehicleManager();
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
      {
        var nearest = VehicleCommands.GetNearestVehicleManager();
        if (nearest != null)
        {
          nearest.PiecesController?.StartActivatePendingPieces();
        }
      }

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
        var currentVehicle = VehicleCommands.GetNearestVehicleManager();
        if (currentVehicle != null && currentVehicle.m_nview != null && ZNetScene.instance != null)
          ZNetScene.instance.Destroy(currentVehicle.m_nview
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
        var currentVehicle = VehicleCommands.GetNearestVehicleManager();
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