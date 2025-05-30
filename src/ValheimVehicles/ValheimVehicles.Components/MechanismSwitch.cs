using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.BepInExConfig;
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
  public List<SwivelComponent> nearbySwivelComponents = new();
  public MechanismSwitchConfigSync prefabConfigSync = new();
  public bool hasInitPrefabConfigSync = false;
  public MechanismSwitchCustomConfig Config => this.GetOrCache<MechanismSwitchConfigSync>(ref prefabConfigSync, ref hasInitPrefabConfigSync).Config;
  public static Vector3 detachOffset = new(0f, 0.5f, 0f);

  public SwivelComponent? TargetSwivel
  {
    get;
    set;
  }

  private int targetSwivelId;

  public int TargetSwivelId
  {
    get => targetSwivelId;
    set
    {
      targetSwivelId = value;
      if (value == 0)
      {
        TargetSwivel = null;
      }
      else
      {
        TargetSwivel = MechanismSwitchCustomConfig.ResolveSwivel(value);
      }
    }
  }

  public List<SwivelComponent> NearestSwivels
  {
    get => nearbySwivelComponents;
    set => nearbySwivelComponents = value;
  }

  public float lerpedHandDistance = 0f;

  public bool CanFireMechanismActionSideEffects = false;

  public MechanismAction SelectedAction
  {
    get => Config.SelectedAction;
    set => Config.SelectedAction = value;
  }

  public List<SwivelComponent> GetNearestSwivels()
  {
    SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out var nearbySwivels);
    NearestSwivels = nearbySwivels;
    return nearbySwivels;
  }

  public override void Awake()
  {
    base.Awake();
    m_nview = GetComponent<ZNetView>();
    prefabConfigSync = gameObject.AddComponent<MechanismSwitchConfigSync>();
  }

  public void Start()
  {
    if (!this.IsNetViewValid()) return;
    StartCoroutine(ScheduleIntendedAction());

    // Do not run any updaters until the awake method has called for a bit.
    Invoke(nameof(AllowSideEffects), 2f);
  }

  public void AllowSideEffects()
  {
    CanFireMechanismActionSideEffects = true;
  }

  private readonly Stopwatch _timer = new();

  public IEnumerator ScheduleIntendedAction()
  {
    _timer.Restart();
    while (_timer.ElapsedMilliseconds < 10000f && (!isActiveAndEnabled || !prefabConfigSync || !prefabConfigSync.HasInitLoaded || !this.IsNetViewValid()))
    {
      _timer.Reset();
      yield return new WaitForFixedUpdate();
    }

    if (_timer.ElapsedMilliseconds >= 10000f && (!isActiveAndEnabled || !prefabConfigSync || !prefabConfigSync.HasInitLoaded || !this.IsNetViewValid()))
    {
      _timer.Reset();
      yield break;
    }

    UpdateIntendedAction();

    // further delayed load in case we did not get the data
    yield return new WaitForSeconds(5f);

    if (!isActiveAndEnabled)
    {
      _timer.Reset();
      yield break;
    }

    UpdateIntendedAction();
    _timer.Reset();
  }

  /// <summary>
  /// Automatically finds the most likely action of this lever.
  /// </summary>
  public void UpdateIntendedAction()
  {
    prefabConfigSync.Load();
    // Already set and stored a SelectedAction and Swivel.
    if (targetSwivelId != 0 || SelectedAction is not MechanismAction.None)
    {
      if (SelectedAction is MechanismAction.SwivelActivateMode or MechanismAction.SwivelEditMode)
      {
        TargetSwivel = MechanismSwitchCustomConfig.ResolveSwivel(TargetSwivelId);
      }

      return;
    }

    // for None status / new prefab
    if (TargetSwivelId == 0 && SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out _, out var closestSwivel) && Vector3.Distance(transform.position, closestSwivel.transform.position) < 1f)
    {
      TargetSwivel = closestSwivel;
      SetMechanismAction(MechanismAction.SwivelActivateMode);
      SetMechanismSwivel(closestSwivel);
      return;
    }

    var pieceController = transform.GetComponentInParent<IPieceController>();
    if (pieceController != null)
    {
      if (pieceController.ComponentName == PrefabNames.SwivelPrefabName)
      {
        SetMechanismAction(MechanismAction.SwivelActivateMode);
        var swivelComponent = pieceController.transform.GetComponent<SwivelComponent>();
        if (TargetSwivelId == 0)
        {
          SetMechanismSwivel(swivelComponent);
        }
        return;
      }

      if (PrefabNames.IsVehicle(pieceController.ComponentName))
      {
        SetMechanismAction(MechanismAction.CommandsHud);
        return;
      }
    }

    SetMechanismAction(PowerSystemConfig.Mechanism_Switch_DefaultAction.Value);
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
    CanFireMechanismActionSideEffects = false;
    CancelInvoke();
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
    prefabConfigSync.Request_SetSelectedAction(action);
  }

  /// <summary>
  /// All syncing is delegated through config when calling Save.
  /// </summary>
  /// <param name="swivel"></param>
  public void SetMechanismSwivel(SwivelComponent swivel)
  {
    // must be set before apply from
    // TargetSwivel = swivel;
    // if (TargetSwivelId == 0)
    // {
    //   TargetSwivel = null;
    // }
    prefabConfigSync.Request_SetSwivelTargetId(swivel.SwivelPersistentId);
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

  public bool OnPressHandler(MechanismSwitch toggleSwitch, Humanoid humanoid)
  {
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
      {
        if (!TargetSwivel || TargetSwivel.swivelPowerConsumer && TargetSwivel.swivelPowerConsumer.IsPowerDenied)
        {
          return false;
        }
        TriggerSwivelAction();
      }
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    ToggleVisualActivationState();
    AddPlayerToPullSwitchAnimations(humanoid);


    return true;
  }

  public void TriggerSwivelPanel()
  {
    if (!TargetSwivel && !SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents, out var swivelComponent))
    {
      TargetSwivel = swivelComponent;
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
    if (!TargetSwivel)
    {
      if (TargetSwivelId == 0)
      {
        if (nearbySwivelComponents.Count > 0)
        {
          TargetSwivel = nearbySwivelComponents[0];
        }
        else if (SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents))
        {
          TargetSwivel = nearbySwivelComponents[0];
        }
      }
      else
      {
        TargetSwivel = MechanismSwitchCustomConfig.ResolveSwivel(TargetSwivelId);
        if (!TargetSwivel)
        {
          LoggerProvider.LogMessage($"Swivel with id {TargetSwivelId} not found. Resetting mechanism settings");
          prefabConfigSync.Request_ClearSwivelId();
        }
        return;
      }
    }


    if (TargetSwivel != null)
    {
      TargetSwivel.Request_NextMotionState();
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

  public bool OnAltPressHandler()
  {
    if (!MechanismSelectorPanelIntegration.Instance)
    {
      MechanismSelectorPanelIntegration.Init();
    }
    if (!MechanismSelectorPanelIntegration.Instance) return false;
    MechanismSelectorPanelIntegration.Instance.BindTo(this, true);

    return true;
  }

  public bool Interact(Humanoid character, bool hold, bool alt)
  {
    if (hold)
      return false;
    if (!alt)
    {
      return OnPressHandler(this, character);
    }
    else
    {
      return OnAltPressHandler();
    }
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
    if (prefabConfigSync && !prefabConfigSync.HasInitLoaded)
    {
      prefabConfigSync.Load();
    }

    var message = $"{ModTranslations.MechanismSwitch_CurrentActionString} {GetLocalizedActionText(SelectedAction)}\n{ModTranslations.MechanismSwitch_AltActionString}";

    if (TargetSwivel && TargetSwivel.swivelPowerConsumer)
    {
      var isPowerDenied = TargetSwivel.swivelPowerConsumer.IsPowerDenied;
      message += $"\n[{PowerNetworkController.GetMechanismRequiredPowerStatus(!isPowerDenied)}]";
    }

    if ((SelectedAction == MechanismAction.SwivelActivateMode || SelectedAction == MechanismAction.SwivelEditMode) && !TargetSwivel)
    {
      message += $"\n{ModTranslations.NoMechanismNearby}";
    }

    if (SelectedAction == MechanismAction.SwivelActivateMode && TargetSwivel && TargetSwivel.swivelPowerConsumer)
    {
      message += PowerNetworkController.GetNetworkPowerStatusString(TargetSwivel.swivelPowerConsumer.NetworkId);
    }
    return message;
  }
}