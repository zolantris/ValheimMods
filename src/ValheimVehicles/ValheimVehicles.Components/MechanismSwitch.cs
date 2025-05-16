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

public class MechanismSwitch : AnimatedLeverMechanism, IAnimatorHandler, Interactable, IHoverableObj, IMechanismActionSetter, INetView
{
  // todo might be better to just run OnAnimatorIK in the fixed update loop.
  private List<Humanoid> m_localAnimatedHumanoids = new();
  private SmoothToggleLerp _handDistanceLerp = new();
  public static bool m_forceRunAnimateOnFixedUpdate = false;
  private MechanismAction _selectedMechanismAction = MechanismAction.CommandsHud;
  private SafeRPCHandler? _safeRPCHandler;

  private int m_targetSwivelId = 0;
  private SwivelComponent? m_targetSwivel;
  public List<SwivelComponent> nearbySwivelComponents = new();

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

  public MechanismAction SelectedAction
  {
    get => _selectedMechanismAction;
    set => _selectedMechanismAction = value;
  }

  public override void Awake()
  {
    base.Awake();
    m_nview = GetComponent<ZNetView>();
    _selectedMechanismAction = PrefabConfig.Mechanism_Switch_DefaultAction.Value;
  }

  public void Start()
  {
    if (!this.IsNetViewValid()) return;
    SyncMechanismAction();
    StartCoroutine(ScheduleIntendedAction());
  }


  public IEnumerator ScheduleIntendedAction()
  {
    if (!isActiveAndEnabled) yield break;
    yield return new WaitForFixedUpdate();
    if (!isActiveAndEnabled || !this.IsNetViewValid(out var netView))
    {
      yield break;
    }
    SyncMechanismSwivelTargetId();
    UpdateIntendedAction();
  }

  /// <summary>
  /// Automatically finds the most likely action of this lever.
  /// </summary>
  public void UpdateIntendedAction()
  {
    if (m_targetSwivelId == 0)
    {
      SyncMechanismSwivelTargetId();
    }

    if (m_targetSwivelId == 0 && !m_targetSwivel && SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents, out m_targetSwivel))
    {
      SetMechanismAction(MechanismAction.SwivelActivateMode);
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
  }

  public void OnEnable()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    OnToggleCompleted += OnAnimationsComplete;

    _safeRPCHandler = new SafeRPCHandler(netView);
    _safeRPCHandler.Register(nameof(RPC_SyncMechanismAction), RPC_SyncMechanismAction);
    _safeRPCHandler.Register<string>(nameof(RPC_SetMechanismAction), RPC_SetMechanismAction);
    _safeRPCHandler.Register<int>(nameof(RPC_SetMechanismSwivelTargetId), RPC_SetMechanismSwivelTargetId);
    _safeRPCHandler.Register<int>(nameof(RPC_SyncMechanismSwivelTargetId), RPC_SyncMechanismSwivelTargetId);

    StartCoroutine(ScheduleIntendedAction());
  }

  public void OnDisable()
  {
    _safeRPCHandler?.UnregisterAll();
    StopAllCoroutines();
  }

  public void RPC_SetMechanismSwivelTargetId(long sender, int swivelZdoId)
  {
    SaveMechanismSwivelTargetId(swivelZdoId);
  }

  /// <summary>
  /// To be run only by an owner.
  /// </summary>
  /// <param name="swivelZdoId"></param>
  public void SaveMechanismSwivelTargetId(int swivelZdoId)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (netView.IsOwner())
    {
      netView.GetZDO().Set(VehicleZdoVars.Mechanism_Swivel_TargetId, swivelZdoId);
      _safeRPCHandler?.InvokeRPC(nameof(RPC_SyncMechanismSwivelTargetId), swivelZdoId);
    }
  }

  public void UpdateOrRPCMechanismSwivelTargetId(SwivelComponent swivelComponent)
  {
    if (!swivelComponent) return;
    SetMechanismSwivel(swivelComponent);
    if (!this.IsNetViewValid(out var netView)) return;
    var swivelZNetview = swivelComponent.GetComponent<ZNetView>();
    if (!swivelZNetview || swivelZNetview.GetZDO() == null) return;
    var zdoId = ZdoWatchController.Instance.GetOrCreatePersistentID(swivelZNetview.GetZDO());
    if (!swivelZNetview.IsOwner())
    {
      // should only sync to owner. Then owner emits a sync call.
      _safeRPCHandler?.InvokeRPC(swivelZNetview.GetZDO().GetOwner(), nameof(RPC_SetMechanismSwivelTargetId), zdoId);
    }
    else
    {
      SaveMechanismSwivelTargetId(zdoId);
    }
  }

  public void Load()
  {
    SyncMechanismSwivelTargetId();
  }

  public void SyncMechanismSwivelTargetId()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    m_targetSwivelId = netView.GetZDO().GetInt(VehicleZdoVars.Mechanism_Swivel_TargetId, 0);

    if (m_targetSwivelId == 0)
    {
      TargetSwivel = null;
    }

    // Get the component from the ZdoWatcherController match of the persistent swivelId.
    var targetSwivelNetview = ZdoWatchController.Instance.GetInstance(m_targetSwivelId);
    if (targetSwivelNetview != null)
    {
      m_targetSwivel = targetSwivelNetview.GetComponent<SwivelComponent>();
    }
  }

  public void RPC_SyncMechanismSwivelTargetId(long sender, int targetSwivelId)
  {
    SyncMechanismSwivelTargetId();

    if (m_targetSwivelId != targetSwivelId)
    {
      LoggerProvider.LogError("Sync somehow desynced. This is a problem with ZDO sync commands...Report this error.");
    }
  }

  public void RPC_SetMechanismAction(long sender, string actionString)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (netView.IsOwner())
    {
      SelectedAction = GetActivationActionFromString(actionString);
      SetMechanismAction(actionString);
    }
  }


  public void RPC_SyncMechanismAction(long sender)
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
        if (!SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents))
        {
          Player.m_localPlayer.Message(MessageHud.MessageType.Center, ModTranslations.NoMechanismNearby);
          return;
        }
        break;
      }
      case MechanismAction.SwivelActivateMode:
        if (!SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents))
        {
          Player.m_localPlayer.Message(MessageHud.MessageType.Center, ModTranslations.NoMechanismNearby);
          return;
        }
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(action), action, null);
    }
    SyncMechanismSwivelTargetId();
    UpdateOrRPCMechanismAction();
  }
  public void SetMechanismSwivel(SwivelComponent swivel)
  {
    TargetSwivel = swivel;
  }

  /// <summary>
  /// To be called in Host via RPC or directly and will force a sync on all clients
  /// </summary>
  /// <param name="switchAction"></param>
  public void SetMechanismAction(string switchAction)
  {
    if (!this.IsNetViewValid(out var netView) || !netView.IsOwner()) return;
    netView.m_zdo.Set(VehicleZdoVars.ToggleSwitchAction, switchAction);
    _safeRPCHandler?.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SyncMechanismAction));
  }

  /// <summary>
  /// This must be run by the client that needs to update the switch
  /// </summary>
  public void UpdateOrRPCMechanismAction()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (netView.IsOwner())
    {
      SetMechanismAction(SelectedAction.ToString());
    }
    else
    {
      _safeRPCHandler?.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SetMechanismAction), SelectedAction.ToString());
    }
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
    if (!isActiveAndEnabled || !this.IsNetViewValid(out var netView)) return;
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
    ToggleVisualActivationState();
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
      SyncMechanismSwivelTargetId();
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
      m_targetSwivel.RequestNextMotionState();
    }
    else
    {
      LoggerProvider.LogError("No swivel detected but the user is toggling a swivel action.");
    }
  }

  public void Save(SwivelComponent swivelComponent)
  {
    UpdateOrRPCMechanismSwivelTargetId(swivelComponent);
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
    var message = $"{ModTranslations.ToggleSwitch_CurrentActionString} {GetLocalizedActionText(SelectedAction)}\n{ModTranslations.ToggleSwitch_NextActionString}";
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
  public ZNetView? m_nview
  {
    get;
    set;
  }
}