using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Constants;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Patches;
using ValheimVehicles.Structs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Interfaces;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.UI;

namespace ValheimVehicles.Components;

public class MechanismSwitch : AnimatedLeverMechanism, IAnimatorHandler, Interactable, IHoverableObj, IMechanismActionSetter
{
  private ZNetView netView;

  // todo might be better to just run OnAnimatorIK in the fixed update loop.
  private List<Humanoid> m_localAnimatedHumanoids = new();
  private SmoothToggleLerp _handDistanceLerp = new();
  public static bool m_forceRunAnimateOnFixedUpdate = false;
  private MechanismAction _selectedMechanismAction = MechanismAction.CommandsHud;

  public MechanismAction SelectedAction
  {
    get => _selectedMechanismAction;
    set => _selectedMechanismAction = value;
  }

  public override void Awake()
  {
    base.Awake();
    netView = GetComponent<ZNetView>();
  }

  public void Start()
  {
    SyncMechanismAction();
  }

  public void OnEnable()
  {
    OnToggleCompleted += OnAnimationsComplete;
    netView.Register(nameof(RPC_UpdateMechanismAction), RPC_UpdateMechanismAction);
  }

  public void OnDisable()
  {
    netView.Unregister(nameof(RPC_UpdateMechanismAction));
  }

  public void RPC_UpdateMechanismAction(long sender)
  {
    SyncMechanismAction();
  }

  public override void FixedUpdate()
  {
    base.FixedUpdate();

    if (m_localAnimatedHumanoids.Count == 0) return;

    if (m_forceRunAnimateOnFixedUpdate)
    {
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
    }

    _handDistanceLerp.Update(IsToggleInProgress, Time.fixedDeltaTime);
    lerpedHandDistance = _handDistanceLerp.Value;
  }

  public void SetMechanismAction(MechanismAction action)
  {
    SelectedAction = action;
    switch (action)
    {

      case MechanismAction.CommandsHud:
        break;
      case MechanismAction.CreativeMode:
        break;
      case MechanismAction.ColliderEditMode:
        break;
      case MechanismAction.SwivelEditMode:
      {
        if (!FindNearestSwivel(out _))
        {
          SelectedAction = MechanismAction.CommandsHud;
          // todo localize.
          Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No swivel nearby");
          return;
        }
        break;
      }
      case MechanismAction.SwivelActivateMode:
        if (!FindNearestSwivel(out _))
        {
          SelectedAction = MechanismAction.CommandsHud;
          // todo localize.
          Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No swivel nearby");
          return;
        }
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(action), action, null);
    }
    UpdateSwitch();
  }
  /// <summary>
  /// This must be run by the client that needs to update the switch
  /// </summary>
  public void UpdateSwitch()
  {
    netView.m_zdo.Set(VehicleZdoVars.ToggleSwitchAction, SelectedAction.ToString());
    // todo may want to just send the string to other clients.
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_UpdateMechanismAction));
  }

  public MechanismAction GetActivationActionFromString(string activationActionString)
  {
    if (!Enum.TryParse<MechanismAction>(activationActionString, out var result))
    {
      result = MechanismAction.CommandsHud;
    }

    return result;
  }

  public void SyncMechanismAction()
  {
    if (!netView || netView.GetZDO() == null || !isActiveAndEnabled) return;
    var activationActionString = netView.GetZDO().GetString(VehicleZdoVars.ToggleSwitchAction, nameof(MechanismAction.CreativeMode));
    SelectedAction = GetActivationActionFromString(activationActionString);
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
      if (humanoid == null || humanoid.m_animator == null) continue;
      if (CharacterAnimEvent_Patch.m_animatedHumanoids.TryGetValue(humanoid.m_animator, out _))
      {
        CharacterAnimEvent_Patch.m_animatedHumanoids.Remove(humanoid.m_animator);
      }
    }

    m_localAnimatedHumanoids.Clear();
  }

  public void AddPlayerToPullSwitchAnimations(Humanoid humanoid)
  {
    if (humanoid == null || humanoid.m_animator == null) return;
    if (!CharacterAnimEvent_Patch.m_animatedHumanoids.TryGetValue(humanoid.m_animator, out _))
    {
      CharacterAnimEvent_Patch.m_animatedHumanoids.Add(humanoid.m_animator, this);
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

    switch (SelectedAction)
    {
      case MechanismAction.CommandsHud:
        HandleToggleCommandsHud();
        break;
      case MechanismAction.CreativeMode:
        HandleToggleCreativeMode();
        break;
      case MechanismAction.ColliderEditMode:
        VehicleCommands.ToggleColliderEditMode();
        break;
      case MechanismAction.SwivelEditMode:
        TriggerSwivelPanel();
        break;
      case MechanismAction.SwivelActivateMode:
        TriggerSwivelAction();
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  private SwivelComponentIntegration m_nearestSwivel;


  public void TriggerSwivelPanel()
  {
    if (!m_nearestSwivel)
    {
      FindNearestSwivel(out m_nearestSwivel);
      return;
    }
    if (!SwivelUIPanelComponentIntegration.Instance)
    {
      SwivelUIPanelComponentIntegration.Init();
    }
    SwivelUIPanelComponentIntegration.Instance.BindTo(m_nearestSwivel, true);
  }

  public void TriggerSwivelAction()
  {
    m_nearestSwivel.RequestNextMotionState();
    return;
  }

  public RaycastHit[] m_raycasthits = new RaycastHit[20];

  public bool FindNearestSwivel(out SwivelComponentIntegration swivelComponentIntegration)
  {
    swivelComponentIntegration = GetComponentInParent<SwivelComponentIntegration>();
    if (swivelComponentIntegration)
    {
      m_nearestSwivel = swivelComponentIntegration;
      return true;
    }

    var num = Physics.SphereCastNonAlloc(transform.position, 0.1f, Vector3.up, m_raycasthits, 100f, LayerHelpers.PieceLayer);
    for (var i = 0; i < num; i++)
    {
      var raycastHit = m_raycasthits[i];
      swivelComponentIntegration = raycastHit.collider.GetComponentInParent<SwivelComponentIntegration>();
      if (swivelComponentIntegration)
      {
        return true;
      }
    }
    return false;
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
      animator.SetIKPositionWeight(AvatarIKGoal.RightHand, lerpedHandDistance);
    }
  }


  public void OnAltPressHandler()
  {
    if (!MechanismSelectorPanelIntegration.Instance)
    {
      MechanismSelectorPanelIntegration.Init();
      if (!MechanismSelectorPanelIntegration.Instance) return;
    }
    MechanismSelectorPanelIntegration.Instance.BindTo(this, true);
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

  public string GetLocalizedActionText(MechanismAction action)
  {
    return action switch
    {
      MechanismAction.CommandsHud => ModTranslations.ToggleSwitch_CommandsHudText,
      MechanismAction.CreativeMode => ModTranslations.CreativeMode,
      MechanismAction.ColliderEditMode => ModTranslations.ToggleSwitch_MaskColliderEditMode,
      MechanismAction.SwivelEditMode => ModTranslations.Swivel_Edit,
      MechanismAction.SwivelActivateMode => ModTranslations.Swivel_Name,
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
    return $"{ModTranslations.ToggleSwitch_CurrentActionString} {GetLocalizedActionText(SelectedAction)}\n{ModTranslations.ToggleSwitch_NextActionString}";
  }
}