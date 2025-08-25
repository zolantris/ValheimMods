using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
using Zolantris.Shared;
namespace ValheimVehicles.ValheimVehicles.Components;

public class TargetControlsInteractive : MonoBehaviour, Hoverable, Interactable,
  IDoodadController
{
  public TargetController targetController;
  public ZNetView m_nview;
  public bool hasTutorial = true;

  public const string Key_ShowTutorial = "TargetControlsInteractive_ShowTutorial";
  public HoverFadeText m_hoverFadeText;
  public Transform hoverTextPoint;
  public Transform attachpoint;
  private bool _hasSubscribed = false;

  public void Awake()
  {
    hoverTextPoint = transform.Find("hover_text_point");
    attachpoint = transform.Find("attachpoint");
    m_hoverFadeText = HoverFadeText.CreateHoverFadeText();
    m_hoverFadeText.transform.position = hoverTextPoint.position;
    m_hoverFadeText.transform.SetParent(hoverTextPoint);
    m_hoverFadeText.canUpdate = false;
  }

  public void Start()
  {
    this.WaitForZNetView((nv) =>
    {
      m_nview = nv;
      targetController = GetComponentInParent<TargetController>();
      targetController.OnCannonGroupChange += (val) => UpdateTextFromCannonDirectionGroup(m_hoverFadeText, val, targetController.LastGroupSize);
      _hasSubscribed = true;
      hasTutorial = GetCanShowTutorial(nv);
    });
  }

  public void OnEnable()
  {
    if (targetController != null)
    {
      _hasSubscribed = true;
      targetController.OnCannonGroupChange += (val) => UpdateTextFromCannonDirectionGroup(m_hoverFadeText, val, targetController.LastGroupSize);
    }
  }

  public void OnDisable()
  {
    if (targetController)
    {
      _hasSubscribed = false;
      targetController.OnCannonGroupChange -= (val) => UpdateTextFromCannonDirectionGroup(m_hoverFadeText, val, targetController.LastGroupSize);
    }
  }

  public static bool GetCanShowTutorial(ZNetView nv)
  {
    if (nv == null) return true;
    var zdo = nv.GetZDO();
    if (zdo == null) return true;
    return zdo.GetBool(Key_ShowTutorial, true);
  }

  public static string GetCannonControlsText(bool hasTutorial)
  {
    if (Localization.instance == null) return "";
    var hoverText = Localization.instance.Localize("$valheim_vehicles_cannon_control_center");
    hoverText += $"\n{ModTranslations.SharedKeys_InteractPrimary}";
    if (hasTutorial)
    {
      hoverText += $"\n{ModTranslations.Vehicle_Cannon_Controls_Tutorial}";
    }
    var tutorialToggle = hasTutorial ? ModTranslations.GuiHide : ModTranslations.GuiShow;
    hoverText += $"\n{ModTranslations.SharedKeys_InteractAlt} to {ModTranslations.WithBoldText(tutorialToggle, "yellow")}";
    return hoverText;
  }

  public string GetHoverText()
  {
    return GetCannonControlsText(hasTutorial);
  }

  public string GetHoverName()
  {
    return "";
  }

  public void AddOrRemoveHotKeyControllers(Player player, bool shouldRemove)
  {
    // if (Player.m_localPlayer != player) return;
    // if (shouldRemove || !targetController)
    // {
    //   if (!targetController && m_nview != null)
    //   {
    //     LoggerProvider.LogWarning($"TargetController is null for {name}. Removing player from controls but netview is valid.");
    //   }
    //   if (playerHotkeyControllers.TryGetValue(player, out var hotkeyController))
    //   {
    //     hotkeyController.OnCannonGroupChange -= UpdateTextFromCannonDirectionGroup;
    //     Destroy(hotkeyController);
    //     playerHotkeyControllers.Remove(player);
    //   }
    //   return;
    // }
    //
    // // may need to guard this more if multiple events can be subscribed.
    // if (!shouldRemove && !cannonFiringHotkeys || !playerHotkeyControllers.TryGetValue(player, out _) || targetController != null)
    // {
    //   cannonFiringHotkeys = player.gameObject.GetOrAddComponent<CannonFiringHotkeys>();
    //   cannonFiringHotkeys.SetTargetController(targetController);
    //
    //   // we do not need an input listener for this. (leverage the direction movement of the player for doodadcontroller). 
    //   cannonFiringHotkeys.SetInputActive(false);
    //
    //   cannonFiringHotkeys.OnCannonGroupChange += UpdateTextFromCannonDirectionGroup;
    //   playerHotkeyControllers[player] = cannonFiringHotkeys;
    //   targetController.OnDetectionModeChange();
    // }
  }

  public void ToggleTutorial()
  {
    var val = !hasTutorial;
    if (m_nview != null && m_nview.IsValid())
    {
      m_nview.GetZDO().Set(Key_ShowTutorial, val);
    }
    hasTutorial = val;
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (alt)
    {
      ToggleTutorial();
      return false;
    }

    var isAttached = user.IsAttached();
    if (hold || isAttached)
    {
      user.AttachStop();
      return false;
    }

    var player = user.GetComponent<Player>();
    if (!player) return false;
    var shouldRemovePreviousDoodad = player.m_doodadController != null;

    targetController.OnDetectionModeChange();

    const string animation = "Standing Torch Idle right";
    player.AttachStart(attachpoint, null, true, false, false, animation, Vector3.zero);

    player.m_doodadController = shouldRemovePreviousDoodad ? null : this;
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    if (ModEnvironment.IsRelease) return false;
    LoggerProvider.LogDev($"UseItem called to {name}");
    return false;
  }

  public void OnUseStop(Player player)
  {
    if (ModEnvironment.IsRelease) return;
    LoggerProvider.LogDev($"OnUseStop called to {name}");
  }

  public static void UpdateTextFromCannonDirectionGroup(HoverFadeText hoverFadeText, CannonDirectionGroup cannonGroup, int groupSize)
  {
    if (hoverFadeText == null) return;
    var text = cannonGroup switch
    {
      CannonDirectionGroup.Forward => ModTranslations.CannonGroup_Forward,
      CannonDirectionGroup.Right => ModTranslations.CannonGroup_Right,
      CannonDirectionGroup.Left => ModTranslations.CannonGroup_Left,
      CannonDirectionGroup.Back => ModTranslations.CannonGroup_Backward,
      _ => throw new ArgumentOutOfRangeException()
    };
    if (hoverFadeText.currentText == text) return;
    hoverFadeText.currentText = $"{text} ({groupSize})";

    // must manually reset as show does not reset without coroutine expiring.
    hoverFadeText.ResetHoverTimer();
    hoverFadeText.Show();
  }

  /// <summary>
  /// A more valheim friendly way to combine hotkeys without interfering with other menu controls.
  /// </summary>
  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  {
    if (targetController == null) return;
    targetController.HandleManualCannonControls(moveDir, lookDir, run, autoRun, block);
  }

  public Component GetControlledComponent()
  {
    return this;
  }

  public Vector3 GetPosition()
  {
    return transform.position;
  }

  public bool IsValid()
  {
    return m_nview && m_nview.IsValid();
  }
}