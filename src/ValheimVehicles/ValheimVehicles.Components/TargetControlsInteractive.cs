using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
namespace ValheimVehicles.ValheimVehicles.Components;

public class TargetControlsInteractive : MonoBehaviour, Hoverable, Interactable,
  IDoodadController
{
  public TargetController targetController;
  public ZNetView m_nview;
  public bool ShowTutorial = true;

  public const string Key_ShowTutorial = "TargetControlsInteractive_ShowTutorial";
  private Dictionary<Player, CannonFiringHotkeys> playerHotkeyControllers = new();
  public static CannonFiringHotkeys? cannonFiringHotkeys = null;
  public HoverFadeText m_hoverText;
  public Transform hoverPoint;
  public Transform attachpoint;

  public void Awake()
  {
    hoverPoint = transform.Find("hover_text_point");
    attachpoint = transform.Find("attachpoint");
    m_hoverText = HoverFadeText.CreateHoverFadeText();
    m_hoverText.canUpdate = false;
  }

  public void Start()
  {
    this.WaitForZNetView((nv) =>
    {
      m_nview = nv;
      targetController = GetComponent<TargetController>();
      ShowTutorial = m_nview.GetZDO().GetBool("TargetControlsInteractive_ShowTutorial", ShowTutorial);
    });
  }

  public string GetHoverText()
  {
    var hoverText = Localization.instance.Localize("$valheim_vehicles_cannon_control_center");

    hoverText += $"\n{ModTranslations.SharedKeys_InteractPrimary}";

    if (ShowTutorial)
    {
      hoverText += $"\n{ModTranslations.Vehicle_Cannon_Controls_Tutorial}";
    }
    var tutorialToggle = ShowTutorial ? ModTranslations.GuiHide : ModTranslations.GuiShow;
    hoverText += $"\n{ModTranslations.SharedKeys_InteractAlt} to {ModTranslations.WithBoldText(tutorialToggle, "yellow")}";
    return hoverText;
  }

  public string GetHoverName()
  {
    return "";
  }

  public void AddOrRemoveHotKeyControllers(Player player, bool shouldRemove)
  {
    if (Player.m_localPlayer != player) return;
    if (shouldRemove || !targetController)
    {
      if (!targetController && m_nview != null)
      {
        LoggerProvider.LogWarning($"TargetController is null for {name}. Removing player from controls but netview is valid.");
      }
      if (playerHotkeyControllers.TryGetValue(player, out var hotkeyController))
      {
        hotkeyController.OnCannonGroupChange -= UpdateTextFromCannonDirectionGroup;
        Destroy(hotkeyController);
        playerHotkeyControllers.Remove(player);
      }
      return;
    }

    // may need to guard this more if multiple events can be subscribed.
    if (!shouldRemove && !cannonFiringHotkeys || !playerHotkeyControllers.TryGetValue(player, out _) || targetController != null)
    {
      cannonFiringHotkeys = player.gameObject.GetOrAddComponent<CannonFiringHotkeys>();
      cannonFiringHotkeys.SetTargetController(targetController);
      cannonFiringHotkeys.OnCannonGroupChange += UpdateTextFromCannonDirectionGroup;
      playerHotkeyControllers[player] = cannonFiringHotkeys;
      targetController.OnDetectionModeChange();
    }
  }

  public void ToggleTutorial()
  {
    var val = !ShowTutorial;
    if (m_nview != null && m_nview.IsValid())
    {
      m_nview.GetZDO().Set(Key_ShowTutorial, val);
    }
    ShowTutorial = val;
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
    AddOrRemoveHotKeyControllers(player, shouldRemovePreviousDoodad);

    player.AttachStart(attachpoint, null, true, false, false, "Standing Torch Idle right", Vector3.zero);

    player.m_doodadController = shouldRemovePreviousDoodad ? null : this;
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    LoggerProvider.LogDev($"UseItem called to {name}");
    return false;
  }

  public void OnUseStop(Player player)
  {
    LoggerProvider.LogDev($"OnUseStop called to {name}");
  }

  public void UpdateTextFromCannonDirectionGroup(CannonDirectionGroup cannonGroup)
  {
    if (m_hoverText == null) return;
    var text = cannonGroup switch
    {
      CannonDirectionGroup.Forward => ModTranslations.CannonGroup_Forward,
      CannonDirectionGroup.Right => ModTranslations.CannonGroup_Right,
      CannonDirectionGroup.Left => ModTranslations.CannonGroup_Left,
      CannonDirectionGroup.Back => ModTranslations.CannonGroup_Backward,
      _ => throw new ArgumentOutOfRangeException()
    };
    if (m_hoverText.currentText == text) return;
    m_hoverText.currentText = text;
    m_hoverText.Show();
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  {
    if (cannonFiringHotkeys == null) return;
    if (moveDir == Vector3.up)
    {
      cannonFiringHotkeys.AdjustManualGroupTilt(cannonFiringHotkeys.lastManualGroup, -cannonFiringHotkeys.tiltStep * Time.deltaTime);
    }
    else if (moveDir == Vector3.down)
    {
      cannonFiringHotkeys.AdjustManualGroupTilt(cannonFiringHotkeys.lastManualGroup, +cannonFiringHotkeys.tiltStep * Time.deltaTime);
    }
    else if (moveDir == Vector3.left)
    {
      cannonFiringHotkeys.SetGroup(cannonFiringHotkeys.GetNextGroup(-1));
      UpdateTextFromCannonDirectionGroup(cannonFiringHotkeys.lastManualGroup);
    }
    else if (moveDir == Vector3.right)
    {
      cannonFiringHotkeys.SetGroup(cannonFiringHotkeys.GetNextGroup(1));
      UpdateTextFromCannonDirectionGroup(cannonFiringHotkeys.lastManualGroup);
    }
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