using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Constants;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Structs;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Components;

public class MechanismSwitch : AnimatedLeverMechanism, IAnimatorHandler, Interactable, Hoverable
{
  /// <summary>
  /// Actions for VehicleCommmands
  /// </summary>
  /// todo
  /// Add actions for Non-vehicle commands to open a panel or just add a panel toggle as another command. Also retain the last position of the command.
  public enum ToggleSwitchAction
  {
    CommandsHud,
    CreativeMode,
    ColliderEditMode
  }

  public ToggleSwitchAction mToggleSwitchType = ToggleSwitchAction.CommandsHud;
  private ZNetView netView;

  // todo might be better to just run OnAnimatorIK in the fixed update loop.
  public static Dictionary<Humanoid, IAnimatorHandler> m_animatedHumanoids = new();
  private List<Humanoid> m_localAnimatedHumanoids = new();
  private SmoothToggleLerp _handDistanceLerp = new();

  public override void Awake()
  {
    base.Awake();
    netView = GetComponent<ZNetView>();
  }

  public void Start()
  {
    SyncSwitchMode();
  }

  public void OnEnable()
  {
    OnToggleCompleted += OnAnimationsComplete;
    netView.Register(nameof(RPC_UpdateSwitch), RPC_UpdateSwitch);
  }

  public void OnDisable()
  {
    netView.Unregister(nameof(RPC_UpdateSwitch));
  }

  public void RPC_UpdateSwitch(long sender)
  {
    SyncSwitchMode();
  }

  public override void FixedUpdate()
  {
    base.FixedUpdate();

    if (m_localAnimatedHumanoids.Count == 0) return;

    for (var index = 0; index < m_localAnimatedHumanoids.Count; index++)
    {
      var humanoid = m_localAnimatedHumanoids[index];
      if (humanoid == null)
      {
        m_localAnimatedHumanoids.FastRemoveAt(ref index);
        continue;
      }
      UpdateIK(humanoid.m_animator);
    }

    _handDistanceLerp.Update(IsToggleInProgress, Time.fixedDeltaTime);
    lerpedHandDistance = _handDistanceLerp.Value;
  }

  /// <summary>
  /// This must be run by the client that needs to update the switch
  /// </summary>
  public void UpdateSwitch()
  {
    netView.m_zdo.Set(VehicleZdoVars.ToggleSwitchAction, mToggleSwitchType.ToString());
    // todo may want to just send the string to other clients.
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_UpdateSwitch));
  }

  public ToggleSwitchAction GetActivationActionFromString(string activationActionString)
  {
    if (!Enum.TryParse<ToggleSwitchAction>(activationActionString, out var result))
    {
      result = ToggleSwitchAction.CommandsHud;
    }

    return result;
  }

  public void SyncSwitchMode()
  {
    if (!netView || netView.GetZDO() == null || !isActiveAndEnabled) return;
    var activationActionString = netView.GetZDO().GetString(VehicleZdoVars.ToggleSwitchAction, nameof(ToggleSwitchAction.CreativeMode));
    mToggleSwitchType = GetActivationActionFromString(activationActionString);
  }

  private void HandleToggleCreativeMode()
  {
    VehicleCommands.ToggleCreativeMode();
  }

  private void HandleToggleCommandsHud()
  {
    VehicleCommands.ToggleVehicleCommandsHud();
  }

  public void OnAnimationsComplete(bool _)
  {
    RemoveAllAnimatorsFromPullSwitchAnimations();
  }

  public void RemoveAllAnimatorsFromPullSwitchAnimations()
  {
    foreach (var humanoid in m_localAnimatedHumanoids.ToList())
    {
      if (humanoid == null) continue;
      if (m_animatedHumanoids.TryGetValue(humanoid, out _))
      {
        m_animatedHumanoids.Remove(humanoid);
      }
      humanoid.AttachStop();
    }

    m_animatedHumanoids.Clear();
  }

  public void AddPlayerToPullSwitchAnimations(Humanoid humanoid)
  {
    // humanoid.AttachStart(attachPoint, null, true, false, false, "Standing Torch Idle right", detachOffset);
    if (!m_animatedHumanoids.TryGetValue(humanoid, out _))
    {
      m_animatedHumanoids.Add(humanoid, this);
    }

    if (!m_localAnimatedHumanoids.Contains(humanoid))
    {
      m_localAnimatedHumanoids.Add(humanoid);
    }
  }

  public static Vector3 detachOffset = new(0f, 0.5f, 0f);

  public void OnPressHandler(MechanismSwitch toggleSwitch, Humanoid humanoid)
  {
    ToggleActivationState();
    AddPlayerToPullSwitchAnimations(humanoid);

    switch (mToggleSwitchType)
    {
      case ToggleSwitchAction.CommandsHud:
        HandleToggleCommandsHud();
        break;
      case ToggleSwitchAction.CreativeMode:
        HandleToggleCreativeMode();
        break;
      case ToggleSwitchAction.ColliderEditMode:
        VehicleCommands.ToggleColliderEditMode();
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public float lerpedHandDistance = 0f;

  /// <summary>
  /// TODO might need to make this more optimized. The IK animations are basic to test things.
  /// </summary>
  /// <param name="animator"></param>
  public void UpdateIK(Animator animator)
  {
    if (!IsToggleInProgress && Mathf.Approximately(lerpedHandDistance, 0f))
    {
      RemoveAllAnimatorsFromPullSwitchAnimations();
      animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
      return;
    }

    if (!IsToggleInProgress)
    {

      animator.SetIKPositionWeight(AvatarIKGoal.RightHand, lerpedHandDistance);
    }
    else
    {
      animator.SetIKPosition(AvatarIKGoal.RightHand, attachPoint.position);
      animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
    }
  }

  /// <summary>
  /// This must always loop from first to last -> first.
  /// </summary>
  /// <returns></returns>
  public ToggleSwitchAction GetNextAction()
  {
    return mToggleSwitchType switch
    {
      ToggleSwitchAction.CommandsHud => ToggleSwitchAction.CreativeMode,
      ToggleSwitchAction.CreativeMode => ToggleSwitchAction.ColliderEditMode,
      ToggleSwitchAction.ColliderEditMode => ToggleSwitchAction.CommandsHud,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  public void SwapHandlerToNextAction()
  {
    mToggleSwitchType = GetNextAction();
  }

  public void OnAltPressHandler()
  {
    SwapHandlerToNextAction();
    UpdateSwitch();
  }

  public bool Interact(Humanoid character, bool hold, bool alt)
  {
    if (hold)
      return false;
    if (!alt)
    {
      OnPressHandler(this, character);
    }
    else
    {
      OnAltPressHandler();
    }
    return true;
  }

  public string GetLocalizedActionText(ToggleSwitchAction action)
  {
    return action switch
    {
      ToggleSwitchAction.CommandsHud => ModTranslations.ToggleSwitch_CommandsHudText,
      ToggleSwitchAction.CreativeMode => ModTranslations.CreativeMode,
      ToggleSwitchAction.ColliderEditMode => ModTranslations.ToggleSwitch_MaskColliderEditMode,
      _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }


  public string GetHoverName()
  {
    return ModTranslations.ToggleSwitch_SwitchName;
  }

  public string GetHoverText()
  {
    return $"{ModTranslations.ToggleSwitch_CurrentActionString} {GetLocalizedActionText(mToggleSwitchType)}\n{ModTranslations.ToggleSwitch_NextActionString} {GetLocalizedActionText(GetNextAction())}";
  }
}