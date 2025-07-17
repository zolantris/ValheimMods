using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.RPC;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;
using Random = UnityEngine.Random;

public class CannonHandHeldController : CannonController, Hoverable
{
  [Header("Aiming")]
  [SerializeField] public float minPitch = -70f; // Looking down
  [SerializeField] public float maxPitch = 70f; // Looking up
  [SerializeField] public float minYaw = -30f; // Yaw limit left
  [SerializeField] public float maxYaw = 30f; // Yaw limit right
  [SerializeField] public float defaultElevationDegrees = 5f;
  [SerializeField] public bool debugDraw = false;

  private float _lastPitch;
  private float _lastYaw;
  private bool _hasLastAngles;
  private Quaternion _lastRotation;

  public static float SquareMagnitudeThreshold = 1e-6f;
  public static bool ShouldUpdate = true;

  public AmmoController ammoController;

  protected internal override void Start()
  {
    m_nview = GetComponentInParent<ZNetView>();
    ammoController = GetComponent<AmmoController>();
    TryInitController();
    base.Start();
  }

  private bool TryInitController()
  {
    try
    {
      if (ammoController == null)
        ammoController = GetComponent<AmmoController>();
      return ammoController != null;
    }
    catch (Exception ex)
    {
      LoggerProvider.LogError($"[HandCannonCameraPitchAiming] Failed to init controller: \n{ex.Message}");
      return false;
    }
  }

  public Transform? playerParentTransform;
  public Player? playerParentComponent;

  public void UpdatePlayerParent()
  {
    if (playerParentTransform != null) return;
    playerParentComponent = transform.GetComponentInParent<Player>();
    if (playerParentComponent == null)
    {
      playerParentTransform = null;
      playerParentComponent = null;
      return;
    }
    playerParentTransform = playerParentComponent.transform;
  }

  protected internal override void FixedUpdate()
  {
    if (!ShouldUpdate) return;
    if (!TryInitController()) return;
    UpdatePlayerParent();

    var isLocalPlayer = IsHeldByLocalPlayer();
    if (playerParentTransform == null) return;
    var parent = cannonRotationalTransform.parent;
    if (!parent) return;

    Vector3 localLook;

    if (isLocalPlayer)
    {
      if (GameCamera.instance == null) return;
      var camera = GameCamera.instance.m_camera;
      if (camera == null) return;
      localLook = parent.InverseTransformDirection(camera.transform.forward);
    }
    else
    {
      localLook = parent.InverseTransformDirection(playerParentTransform.forward);
    }

    // Check for degenerate input
    if (localLook.sqrMagnitude < SquareMagnitudeThreshold) return;

    var yaw = Mathf.Atan2(localLook.x, localLook.z) * Mathf.Rad2Deg;
    yaw = Mathf.Clamp(yaw, minYaw, maxYaw);

    var pitch = -Mathf.Asin(Mathf.Clamp(localLook.y, -1f, 1f)) * Mathf.Rad2Deg;
    pitch -= defaultElevationDegrees;
    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

    // Don't update if unchanged (floating point safe check)
    if (_hasLastAngles &&
        Mathf.Approximately(pitch, _lastPitch) &&
        Mathf.Approximately(yaw, _lastYaw))
    {
      return;
    }

    // Only update if we're within the valid aiming range (already clamped above)
    // If you want to prevent updating at *exact* clamp edges, check:
    // if (pitch == minPitch || pitch == maxPitch || yaw == minYaw || yaw == maxYaw) return;
    // Or, only skip update if the *input* before clamping was outside the range.

    var rotation = Quaternion.Euler(pitch, yaw, 0f);
    var forward = rotation * Vector3.forward;
    if (forward.sqrMagnitude < 1e-5f)
    {
      rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
#if UNITY_EDITOR
            Debug.LogWarning("HandCannonCameraPitchAiming: Aiming rotation would result in zero forward, using fallback.");
#endif
    }

    // Only update transform if rotation is actually different, to avoid dirtying transform unnecessarily
    if (!_hasLastAngles || Quaternion.Angle(rotation, _lastRotation) > 0.01f)
    {
      cannonRotationalTransform.localRotation = rotation;
      _lastRotation = rotation;
      _lastPitch = pitch;
      _lastYaw = yaw;
      _hasLastAngles = true;
    }

    if (debugDraw)
    {
      RuntimeDebugLineDrawer.DrawLine(cannonShooterTransform.position,
        cannonShooterTransform.position + cannonShooterTransform.forward * 5f,
        Color.green, 0.1f);
    }

    base.FixedUpdate();
  }

  public void Request_FireHandHeld()
  {
    // prevent firing at 0 ammo.
    if (ammoController.GetAmmoAmountFromCannonballVariant(AmmoVariant) < 1) return;
    if (!m_nview)
    {
      m_nview = GetComponentInParent<ZNetView>();
    }
    if (!m_nview)
    {
      LoggerProvider.LogWarning("cannonController missing znetview!");
      return;
    }
    var data = CannonFireData.CreateCannonFireDataFromHandHeld(this);
    if (!data.HasValue)
    {
      LoggerProvider.LogWarning("cannonController missing cannonfiredata!");
      return;
    }
    var package = CannonFireData.WriteToPackage(data.Value);
    cannonRotationalTransform.localRotation = data.Value.cannonLocalRotation;
    FireHandHeldCannon_RPC.Send(ZNetView.Everybody, package);
  }

  public static RPCEntity FireHandHeldCannon_RPC = null!;

  public static void RegisterCannonControllerRPCs()
  {
    FireHandHeldCannon_RPC = RPCManager.RegisterRPC(nameof(RPC_FireHandHeldCannon), RPC_FireHandHeldCannon);
  }

  public static IEnumerator RPC_FireHandHeldCannon(long senderId, ZPackage package)
  {
    package.SetPos(0);
    var cannonFireData = CannonFireData.ReadFromPackage(package);
    var cannonControllerZDOID = cannonFireData.cannonControllerZDOID;

    var cannonHandHeldInstance = ZNetScene.instance.FindInstance(cannonControllerZDOID);
    if (!cannonHandHeldInstance)
    {
      LoggerProvider.LogWarning($"cannonHandheld {cannonControllerZDOID} not found. CannonController should exist otherwise we cannot instantiate cannonball without collision issues");
      yield break;
    }

    // can be a child for the handheld version.
    var cannonHandheld = cannonHandHeldInstance.GetComponentInChildren<CannonHandHeldController>();
    if (!cannonHandheld)
    {
      LoggerProvider.LogWarning($"cannonHandheld {cannonControllerZDOID} not found. CannonController should exist otherwise we cannot instantiate cannonball without collision issues");
      yield break;
    }

    cannonHandheld.FireHandHeldCannon(cannonFireData);
  }

  internal bool FireHandHeldCannon(CannonFireData data)
  {
    if (Fire(data, true))
    {
      if (data.canApplyDamage)
      {
        ammoController.OnAmmoChangedFromVariant(data.ammoVariant, data.allocatedAmmo);
      }
      return true;
    }
    return false;
  }

  private bool IsHeldByLocalPlayer()
  {
    if (Player.m_localPlayer == null) return false;
    if (playerParentComponent == null) return false;
    return Player.m_localPlayer == playerParentComponent;
  }

  public string GetHoverText()
  {
    return Localization.instance.Localize("$valheim_vehicles_cannon_handheld_item (H)");
  }
  public string GetHoverName()
  {
    return Localization.instance.Localize("$valheim_vehicles_cannon_handheld_item");
  }
}