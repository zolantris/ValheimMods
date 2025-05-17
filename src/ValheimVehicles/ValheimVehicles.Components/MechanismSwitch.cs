using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Config;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Patches;
using ValheimVehicles.Structs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
using ValheimVehicles.SharedScripts.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.UI;
using ZdoWatcher;

namespace ValheimVehicles.Components;

public class MechanismSwitch : AnimatedLeverMechanism, IAnimatorHandler, Interactable, IHoverableObj, IMechanismActionSetter, INetView, IPrefabConfig<MechanismSwitchCustomConfig>
{
  // todo might be better to just run OnAnimatorIK in the fixed update loop.
  private List<Humanoid> m_localAnimatedHumanoids = new();
  private SmoothToggleLerp _handDistanceLerp = new();
  public static bool m_forceRunAnimateOnFixedUpdate = false;
  private MechanismAction _selectedMechanismAction = MechanismAction.CommandsHud;
  private SafeRPCHandler? _safeRPCHandler;
  public ZNetView? m_nview
  {
    get;
    set;
  }
  private int m_targetSwivelId = 0;
  private SwivelComponent? m_targetSwivel;
  public List<SwivelComponent> nearbySwivelComponents = new();
  public MechanismSwitchConfigSync prefabConfigSync;
  public MechanismSwitchCustomConfig Config => prefabConfigSync.Config;
  public IMechanismActionSetter mechanismAction;
  public static Vector3 detachOffset = new(0f, 0.5f, 0f);

  public SwivelComponent? TargetSwivel
  {
    get => m_targetSwivel;
    set => m_targetSwivel = value;
  }

  public List<SwivelComponent> NearestSwivels
  {
    get => nearbySwivelComponents;
    set => nearbySwivelComponents = value;
  }

  public float lerpedHandDistance = 0f;

  /// <summary>
  /// todo might make this only a getter.
  /// </summary>
  public MechanismAction SelectedAction
  {
    get => prefabConfigSync.Config.SelectedAction;
    set => prefabConfigSync.Config.SelectedAction = value;
  }

  public override void Awake()
  {
    base.Awake();
    mechanismAction = this;
    m_nview = GetComponent<ZNetView>();
    prefabConfigSync = gameObject.AddComponent<MechanismSwitchConfigSync>();
  }

  public void Start()
  {
    if (!this.IsNetViewValid()) return;
    StartCoroutine(ScheduleIntendedAction());
  }


  public IEnumerator ScheduleIntendedAction()
  {
    if (!isActiveAndEnabled) yield break;
    yield return new WaitForFixedUpdate();
    if (!isActiveAndEnabled || !prefabConfigSync || !prefabConfigSync.HasInitLoaded || !this.IsNetViewValid())
    {
      yield break;
    }
    UpdateIntendedAction();
  }

  // Get the component from the ZdoWatcherController match of the persistent swivelId.
  public bool TryFindTargetSwivelComponent()
  {
    if (m_targetSwivelId == 0) return false;
    var targetSwivelNetview = ZdoWatchController.Instance.GetInstance(m_targetSwivelId);
    if (targetSwivelNetview == null) return false;

    m_targetSwivel = targetSwivelNetview.GetComponent<SwivelComponent>();
    return true;
  }

  /// <summary>
  /// Automatically finds the most likely action of this lever.
  /// </summary>
  public void UpdateIntendedAction()
  {
    if (m_targetSwivelId == 0)
    {
      prefabConfigSync.Load();
    }

    // Already set and stored a SelectedAction
    if (SelectedAction is not MechanismAction.None)
    {
      if (SelectedAction is MechanismAction.SwivelActivateMode or MechanismAction.SwivelEditMode)
      {
        TryFindTargetSwivelComponent();
        return;
      }

      return;
    }

    // for None status / new prefab
    if (m_targetSwivelId == 0 && SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out _, out var closestSwivel) && Vector3.Distance(transform.position, closestSwivel.transform.position) < 1f)
    {
      m_targetSwivel = closestSwivel;
      SetMechanismSwivel(closestSwivel);
      return;
    }

    var pieceController = transform.GetComponentInParent<IPieceController>();
    if (pieceController != null)
    {
      if (pieceController.ComponentName == PrefabNames.SwivelPrefabName)
      {
        SetMechanismAction(MechanismAction.SwivelActivateMode);
        return;
      }

      if (PrefabNames.IsVehicle(pieceController.ComponentName))
      {
        SetMechanismAction(MechanismAction.CommandsHud);
      }
    }

    SetMechanismAction(PrefabConfig.Mechanism_Switch_DefaultAction.Value);
  }

  public void OnEnable()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    OnToggleCompleted += OnAnimationsComplete;
    StartCoroutine(ScheduleIntendedAction());
  }

  public void OnDisable()
  {
    _safeRPCHandler?.UnregisterAll();
    StopAllCoroutines();
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
    // bail on recursive setting of same action.
    if (action.Equals(SelectedAction)) return;

    SelectedAction = action;
    OnMechanismActionUpdate(action);
    prefabConfigSync.Save();
  }

  public void OnMechanismActionUpdate(MechanismAction action)
  {
    switch (action)
    {
      case MechanismAction.None:
      case MechanismAction.CommandsHud:
      case MechanismAction.CreativeMode:
      case MechanismAction.ColliderEditMode:
        m_targetSwivel = null;
        m_targetSwivelId = 0;
        break;
      case MechanismAction.SwivelEditMode:
      case MechanismAction.SwivelActivateMode:
      {
        // should be fired whenever we update this state.
        if (!SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents))
        {
          Player.m_localPlayer.Message(MessageHud.MessageType.Center, ModTranslations.NoMechanismNearby);
          return;
        }
      }
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(action), action, null);
    }

    if (MechanismSelectorPanelIntegration.Instance != null && MechanismSelectorPanelIntegration.Instance.mechanismAction == mechanismAction)
    {
      MechanismSelectorPanelIntegration.Instance.SelectedAction = action;
      MechanismSelectorPanelIntegration.Instance.SelectedSwivel = m_targetSwivel;
    }
  }

  /// <summary>
  /// All syncing is delegated through config when calling Save.
  /// </summary>
  /// <param name="swivel"></param>
  public void SetMechanismSwivel(SwivelComponent swivel)
  {
    prefabConfigSync.Request_SetSwivelTargetId(swivel);
  }

  // public MechanismAction GetActivationActionFromString(string activationActionString)
  // {
  //   if (!Enum.TryParse<MechanismAction>(activationActionString, out var result))
  //   {
  //     result = MechanismAction.CommandsHud;
  //   }
  //
  //   return result;
  // }

  // public void SyncMechanismAction()
  // {
  //   if (!isActiveAndEnabled || !this.IsNetViewValid(out var netView)) return;
  //   var activationActionString = netView.GetZDO().GetString(VehicleZdoVars.ToggleSwitchAction, nameof(MechanismAction.CreativeMode));
  //   SelectedAction = GetActivationActionFromString(activationActionString);
  // }

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

  public void OnPressHandler(MechanismSwitch toggleSwitch, Humanoid humanoid)
  {
    ToggleVisualActivationState();
    AddPlayerToPullSwitchAnimations(humanoid);

    switch (SelectedAction)
    {
      // do nothing for this
      case MechanismAction.None:
        break;
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

  public void TriggerSwivelPanel()
  {
    if (!TargetSwivel && !SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents, out m_targetSwivel))
    {
      return;
    }
    if (!SwivelUIPanelComponentIntegration.Instance)
    {
      SwivelUIPanelComponentIntegration.Init();
    }
    if (SwivelUIPanelComponentIntegration.Instance && TargetSwivel)
    {
      SwivelUIPanelComponentIntegration.Instance.BindTo(TargetSwivel, true);
    }
    else
    {
      LoggerProvider.LogError("SwivelUIPanelComponentIntegration failed to initialize.");
    }
  }

  public void TriggerSwivelAction()
  {
    if (!m_targetSwivel)
    {
      if (m_targetSwivelId == 0)
      {
        if (nearbySwivelComponents.Count > 0)
        {
          m_targetSwivel = nearbySwivelComponents[0];
        }
        else if (SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents))
        {
          m_targetSwivel = nearbySwivelComponents[0];
        }
      }
    }

    if (m_targetSwivel != null)
    {
      m_targetSwivel.Request_NextMotionState();
    }
    else
    {
      LoggerProvider.LogError("No swivel detected but the user is toggling a swivel action.");
    }
  }


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
      MechanismAction.CommandsHud => ModTranslations.MechanismSwitch_CommandsHudText,
      MechanismAction.CreativeMode => ModTranslations.CreativeMode,
      MechanismAction.ColliderEditMode => ModTranslations.MechanismSwitch_MaskColliderEditMode,
      MechanismAction.SwivelEditMode => ModTranslations.MechanismMode_Swivel_Edit,
      MechanismAction.SwivelActivateMode => ModTranslations.Swivel_Name,
      MechanismAction.None => ModTranslations.MechanismMode_None,
      _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }


  public string GetHoverName()
  {
    return ModTranslations.MechanismSwitch_SwitchName;
  }

  public string GetHoverText()
  {
    var message = $"{ModTranslations.MechanismSwitch_CurrentActionString} {GetLocalizedActionText(SelectedAction)}\n{ModTranslations.MechanismSwitch_AltActionString}";
    if ((SelectedAction == MechanismAction.SwivelActivateMode || SelectedAction == MechanismAction.SwivelEditMode) && !m_targetSwivel)
    {
      message += $"\n{ModTranslations.NoMechanismNearby}";
    }
    if (SelectedAction == MechanismAction.SwivelActivateMode && m_targetSwivel && m_targetSwivel.swivelPowerConsumer)
    {
      message += PowerNetworkController.GetNetworkPowerStatusString(m_targetSwivel.swivelPowerConsumer.NetworkId);
    }
    return message;
  }
}