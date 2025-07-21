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
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Patches;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.Structs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
using ValheimVehicles.SharedScripts.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.UI;
using ZdoWatcher;
using Random = UnityEngine.Random;

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
    while (_timer.ElapsedMilliseconds < 10000f && (isActiveAndEnabled || !prefabConfigSync || !prefabConfigSync.HasInitLoaded || !this.IsNetViewValid()))
    {
      _timer.Restart();
      yield return new WaitForFixedUpdate();
    }

    if (_timer.ElapsedMilliseconds >= 10000f || !isActiveAndEnabled || !prefabConfigSync || !prefabConfigSync.HasInitLoaded || !this.IsNetViewValid())
    {
      _timer.Reset();
      yield break;
    }

    yield return UpdateIntendedAction();

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
  public IEnumerator UpdateIntendedAction()
  {
    prefabConfigSync.Load();
    // Already set and stored a SelectedAction and Swivel.
    if (targetSwivelId != 0 || SelectedAction is not MechanismAction.None)
    {
      if (SelectedAction is MechanismAction.SwivelActivateMode or MechanismAction.SwivelEditMode)
      {
        var shouldResolve = false;
        yield return ZdoWatchController.Instance.GetZdoFromServerAsync(targetSwivelId, (asyncZdo) =>
        {
          shouldResolve = asyncZdo != null;
          if (!shouldResolve)
          {
            LoggerProvider.LogDebug("Swivel with id {targetSwivelId} not found. Resetting mechanism settings");
            prefabConfigSync.Request_ClearSwivelId();
          }
          else
          {
            TargetSwivel = MechanismSwitchCustomConfig.ResolveSwivel(TargetSwivelId);
          }
        });
      }

      yield break;
    }

    // for None status / new prefab
    if (TargetSwivelId == 0 && SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out _, out var closestSwivel) && Vector3.Distance(transform.position, closestSwivel.transform.position) < 1f)
    {
      TargetSwivel = closestSwivel;
      SetMechanismAction(MechanismAction.SwivelActivateMode);
      SetMechanismSwivel(closestSwivel);
      yield break;
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
        yield break;
      }

      if (PrefabNames.IsVehicle(pieceController.ComponentName))
      {
        SetMechanismAction(MechanismAction.CommandsHud);
        yield break;
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

  public bool OnHoldActionHandler()
  {
    if (TargetSwivel == null) return false;

    if (SelectedAction == MechanismAction.SwivelActivateMode)
    {
      TriggerSwivelPanel();
      return true;
    }

    return false;
  }

  public bool OnPressHandler(Humanoid humanoid)
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
      case MechanismAction.VehicleDock:
        TriggerDockSequence();
        break;
      case MechanismAction.SwivelActivateMode:
      {
        TriggerSwivelAction();
        break;
      }
      case MechanismAction.FireCannonGroup:
      {
        FireCannonGroup();
        break;
      }
      default:
        throw new ArgumentOutOfRangeException();
    }

    ToggleVisualActivationState();
    AddPlayerToPullSwitchAnimations(humanoid);

    return true;
  }

  public void FireCannonGroup()
  {
    var targetController = GetComponentInParent<TargetController>();
    if (!targetController)
    {
      Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No target controller. This must be used on a vehicle for now.");
      return;
    }
    targetController.Request_FireAllManualCannonGroups([CannonDirectionGroup.Forward, CannonDirectionGroup.Back, CannonDirectionGroup.Left, CannonDirectionGroup.Right]);
  }

  public bool TriggerDockSequence()
  {
    var piecesController = transform.GetComponentInParent<VehiclePiecesController>();
    if (!piecesController)
    {
      Player.m_localPlayer.Message(MessageHud.MessageType.Center, ModTranslations.DockingMessages_NotAttachedToVehicle);
      return false;
    }

    var manager = piecesController.Manager;

    if (!manager || manager.MovementController == null || manager.PiecesController == null) return false;

    var parentId = manager.m_nview.GetZDO().GetInt(VehicleZdoVars.MBParentId, 0);

    if (manager.VehicleParent != null || parentId != 0 || manager.ForceDocked)
    {
      StartCoroutine(IgnoreCollisionWhileUndocking(manager, manager.VehicleParent));
      VehicleManager.RemoveVehicleParent(manager);
      Player.m_localPlayer.Message(MessageHud.MessageType.Center, ModTranslations.DockingMessages_Undocked);
    }
    else
    {
      var vehiclePosition = manager.MovementController.m_body.worldCenterOfMass;
      if (manager.PiecesController.m_dockAnchor != null)
      {
        vehiclePosition = manager.PiecesController.m_dockAnchor.transform.position;
      }

      var maxCastDistance = PrefabConfig.VehicleDockVerticalHeight.Value;
      VehicleManager? closestVehicleManager = null;

      // square cast upwards. Do this first as it's better to match upwards first
      if (closestVehicleManager == null && manager.OnboardCollider != null)
      {
        var bounds = manager.OnboardCollider.bounds;
        var startPoint = manager.OnboardCollider.transform.position + new Vector3(0, bounds.extents.y, 0);
        closestVehicleManager = VehicleCommands.GetNearestVehicleManagerInBox(startPoint, maxCastDistance, bounds, manager);
      }

      if (closestVehicleManager == null)
      {
        closestVehicleManager = VehicleCommands.GetNearestVehicleManagerInSphere(vehiclePosition, PrefabConfig.VehicleDockSphericalRadius.Value, manager);
      }

      if (closestVehicleManager == null)
      {
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, ModTranslations.DockingMessages_NoVehicleToDockFound);
        return false;
      }
      VehicleManager.AddVehicleParent(closestVehicleManager, manager, true);
      Player.m_localPlayer.Message(MessageHud.MessageType.Center, ModTranslations.DockingMessages_Docked);
    }
    return true;
  }

  public IEnumerator IgnoreCollisionWhileUndocking(VehicleManager childManager, VehicleManager? parentManager)
  {
    if (parentManager == null) yield break;
    var childVehicleColliders = childManager.PiecesController.allVehicleColliders;
    var parentVehicleColliders = parentManager.PiecesController.allVehicleColliders;

    foreach (var collider in childVehicleColliders)
    foreach (var pieceData in parentManager.PiecesController.m_prefabPieceDataItems)
    foreach (var pieceCollider in pieceData.Value.AllColliders)
    {

      if (collider == null || pieceCollider == null) continue;
      Physics.IgnoreCollision(collider, pieceCollider, true);
    }

    foreach (var collider in childVehicleColliders)
    foreach (var parentVehicleCollider in parentVehicleColliders)
    {
      if (collider == null && parentVehicleCollider == null) continue;
      Physics.IgnoreCollision(collider, parentVehicleCollider, true);
    }


    yield return new WaitForSeconds(5f);

    foreach (var collider in childVehicleColliders)
    foreach (var pieceData in parentManager.PiecesController.m_prefabPieceDataItems)
    foreach (var pieceCollider in pieceData.Value.AllColliders)
    {

      if (collider == null || pieceCollider == null) continue;
      Physics.IgnoreCollision(collider, pieceCollider, false);
    }

    foreach (var collider in childVehicleColliders)
    foreach (var parentVehicleCollider in parentVehicleColliders)
    {
      if (collider == null && parentVehicleCollider == null) continue;
      Physics.IgnoreCollision(collider, parentVehicleCollider, false);
    }
  }

  public bool TriggerSwivelPanel()
  {
    if (!TargetSwivel)
    {
      if (!SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out nearbySwivelComponents, out var swivelComponent))
      {
        TargetSwivel = null;
        TargetSwivelId = 0;
        return false;
      }

      TargetSwivel = swivelComponent;
      TargetSwivelId = swivelComponent.SwivelPersistentId;
    }

    if (!SwivelUIPanelComponentIntegration.Instance)
    {
      SwivelUIPanelComponentIntegration.Init();
    }

    if (SwivelUIPanelComponentIntegration.Instance && TargetSwivel)
    {
      SwivelUIPanelComponentIntegration.Instance.BindTo(TargetSwivel, true);
      return true;
    }

    LoggerProvider.LogError("SwivelUIPanelComponentIntegration failed to initialize.");
    return false;
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
          // LoggerProvider.LogMessage($"Swivel with id {TargetSwivelId} not found. Resetting mechanism settings");
          // prefabConfigSync.Request_ClearSwivelId();
          return;
        }
      }
    }


    if (TargetSwivel != null)
    {
      if (!PowerSystemConfig.Swivels_DoNotRequirePower.Value && TargetSwivel.swivelPowerConsumer.IsPowerDenied)
      {
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Not enough power to activate this swivel.");
        return;
      }

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

  private bool hasHoldDelay = false;
  private Stopwatch holdTimer = new();

  public bool Interact(Humanoid character, bool hold, bool alt)
  {
    if (holdTimer.ElapsedMilliseconds > 1000f)
    {
      CancelInvoke(nameof(OnHoldActionHandler));
      holdTimer.Reset();
    }

    if (hold && !alt)
    {
      if (SelectedAction == MechanismAction.FireCannonGroup)
      {
        FireCannonGroup();
        return true;
      }
      return false;
    }

    if (SelectedAction == MechanismAction.SwivelActivateMode && hold && alt)
    {
      if (holdTimer.IsRunning) return false;
      Invoke(nameof(OnHoldActionHandler), 1f);
      holdTimer.Restart();
      return false;
    }

    if (!alt)
    {
      return OnPressHandler(character);
    }

    return OnAltPressHandler();
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
      MechanismAction.VehicleDock => ModTranslations.MechanismMode_VehicleDock,
      MechanismAction.FireCannonGroup => ModTranslations.MechanismMode_FireCannonGroup,
      // MechanismAction.VehicleConfig => ModTranslations.MechanismMode_VehicleConfig,
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

    if (SelectedAction == MechanismAction.SwivelActivateMode)
    {
      message += $"\n{ModTranslations.MechanismSwitch_AltHoldActionString}";
    }

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