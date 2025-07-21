// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Patches;
using Random = UnityEngine.Random;

namespace ValheimVehicles.SharedScripts
{
  public class CannonFiringHotkeys : MonoBehaviour
  {
    public TargetController targetController;
    public bool inputActive = false;

    public KeyCode kbForward = KeyCode.Alpha1;
    public KeyCode kbLeft = KeyCode.Alpha2;
    public KeyCode kbRight = KeyCode.Alpha3;
    public KeyCode kbBack = KeyCode.Alpha4;
    public KeyCode shiftKey = KeyCode.LeftShift;
    public KeyCode shiftKeyAlt = KeyCode.RightShift;
    public string gamepadShift = "JoyLTrigger";

    public Action<CannonDirectionGroup>? OnCannonGroupChange;

    private readonly string dpadUp = "JoyDPadUp";
    private readonly string dpadLeft = "JoyDPadLeft";
    private readonly string dpadRight = "JoyDPadRight";
    private readonly string dpadDown = "JoyDPadDown";
    private readonly string rightStickY = "JoyRightStickVertical"; // Confirm correct ZInput axis for your setup

    public float minPitch = -35f;
    public float maxPitch = 35f;

    public float tiltStep = 2f; // for scroll and DPad
    public float tiltSpeed = 30f; // deg/sec for stick
    public float holdToAdjustThreshold = 0.5f; // seconds

    public CannonDirectionGroup lastManualGroup = CannonDirectionGroup.Forward;

    // Track per-group tilt
    private readonly Dictionary<CannonDirectionGroup, float> manualGroupTilt = new()
    {
      { CannonDirectionGroup.Forward, 15f },
      { CannonDirectionGroup.Left, 15f },
      { CannonDirectionGroup.Right, 15f },
      { CannonDirectionGroup.Back, 15f }
    };

    // Track key/button hold start time
    private readonly Dictionary<CannonDirectionGroup, float> groupHoldStartTime = new()
    {
      { CannonDirectionGroup.Forward, -1f },
      { CannonDirectionGroup.Left, -1f },
      { CannonDirectionGroup.Right, -1f },
      { CannonDirectionGroup.Back, -1f }
    };

    public void SetTargetController(TargetController controller)
    {
      targetController = controller;
      inputActive = controller != null;
    }

    public void SetInputActive(bool active)
    {
      inputActive = active;
    }

    public void OnDestroy()
    {
      ZInput_Patches.ShouldBlockInputForAlpha1234Keys = false;
    }

    private void Update()
    {
      if (!inputActive || targetController == null) return;

      if (ZInput.IsGamepadActive())
        HandleGamepadInput();
      else
        HandleKeyboardInput();
    }

  #region Keyboard

    private void HandleKeyboardInput()
    {
      ZInput_Patches.ShouldBlockInputForAlpha1234Keys = false;
      // Only allow input if Shift held
      if (!(ZInput.GetKey(shiftKey) || ZInput.GetKey(shiftKeyAlt)))
      {
        return;
      }
      // Process per-group for Alpha1-4 keys
      ProcessKeyGroup(kbForward, CannonDirectionGroup.Forward);
      ProcessKeyGroup(kbLeft, CannonDirectionGroup.Left);
      ProcessKeyGroup(kbRight, CannonDirectionGroup.Right);
      ProcessKeyGroup(kbBack, CannonDirectionGroup.Back);

      // Tilt with scroll (for last active group)
      var scroll = Input.mouseScrollDelta.y;
      if (Mathf.Abs(scroll) > 0.01f)
      {
        var tiltDelta = scroll > 0f ? +tiltStep : -tiltStep;
        AdjustManualGroupTilt(lastManualGroup, tiltDelta);
      }

      // blocks if we are holding that key down.
      ZInput_Patches.ShouldBlockInputForAlpha1234Keys = true;
    }

    private void ProcessKeyGroup(KeyCode key, CannonDirectionGroup group)
    {
      // Down: start hold timer, set as last active group
      if (ZInput.GetKeyDown(key))
      {
        groupHoldStartTime[group] = Time.unscaledTime;
        SetGroup(group);
      }

      // Held: allow tilt
      if (ZInput.GetKey(key))
      {
        // Shift+Scroll already handled in HandleKeyboardInput
        // Could support "Q/E" for up/down if you wish
      }

      // Up: check if short tap, fire if under threshold
      if (ZInput.GetKeyUp(key))
      {
        var held = Time.unscaledTime - groupHoldStartTime[group];
        if (held <= holdToAdjustThreshold)
          FireManualGroup(group);
        groupHoldStartTime[group] = -1f;
      }
    }

  #endregion

  #region Gamepad

    private void HandleGamepadInput()
    {
      ZInput_Patches.ShouldBlockInputForAlpha1234Keys = false;
      if (!ZInput.GetButton(gamepadShift))
      {
        return;
      }

      // DPad: per-group input with hold/tap logic
      ProcessButtonGroup(dpadUp, CannonDirectionGroup.Forward);
      ProcessButtonGroup(dpadLeft, CannonDirectionGroup.Left);
      ProcessButtonGroup(dpadRight, CannonDirectionGroup.Right);
      ProcessButtonGroup(dpadDown, CannonDirectionGroup.Back);

      // Tilt with right stick (while LT held, applies to last active group)
      var stick = ZInput.GetJoyRightStickY();
      if (Mathf.Abs(stick) > 0.2f)
      {
        var tiltDelta = stick * tiltSpeed * Time.deltaTime;
        AdjustManualGroupTilt(lastManualGroup, tiltDelta);
      }

      // Also allow DPad Up/Down to adjust tilt (hold for adjust, tap for fire)
      if (ZInput.GetButton(dpadUp) && !ZInput.GetButtonDown(dpadUp))
      {
        AdjustManualGroupTilt(lastManualGroup, +tiltStep * Time.deltaTime);
      }
      if (ZInput.GetButton(dpadDown) && !ZInput.GetButtonDown(dpadDown))
      {
        AdjustManualGroupTilt(lastManualGroup, -tiltStep * Time.deltaTime);
      }
    }

    private void ProcessButtonGroup(string button, CannonDirectionGroup group)
    {
      // Down: start hold timer, set as last active group
      if (ZInput.GetButtonDown(button))
      {
        groupHoldStartTime[group] = Time.unscaledTime;
        lastManualGroup = group;
      }

      // Held: (optionally handled above for tilt)
      // if (ZInput.GetButton(button)) { ... }

      // Up: check if short tap, fire if under threshold
      if (ZInput.GetButtonUp(button))
      {
        var held = Time.unscaledTime - groupHoldStartTime[group];
        if (held <= holdToAdjustThreshold)
          FireManualGroup(group);
        groupHoldStartTime[group] = -1f;
      }
    }

  #endregion

    // Tilt helper
    public void AdjustManualGroupTilt(CannonDirectionGroup group, float tiltDelta)
    {
      if (targetController == null) return;
      manualGroupTilt[group] = Mathf.Clamp(
        manualGroupTilt[group] + tiltDelta,
        minPitch, maxPitch
      );
      targetController.SetManualGroupTilt(group, manualGroupTilt[group]);
      targetController.ScheduleGroupCannonSync(group);
    }

    public CannonDirectionGroup GetNextGroup(int dir)
    {
      return dir switch
      {
        -1 => lastManualGroup switch
        {
          CannonDirectionGroup.Forward => CannonDirectionGroup.Right,
          CannonDirectionGroup.Back => CannonDirectionGroup.Forward,
          CannonDirectionGroup.Left => CannonDirectionGroup.Back,
          CannonDirectionGroup.Right => CannonDirectionGroup.Left,
          _ => throw new ArgumentOutOfRangeException()
        },
        1 => lastManualGroup switch
        {
          CannonDirectionGroup.Forward => CannonDirectionGroup.Back,
          CannonDirectionGroup.Back => CannonDirectionGroup.Left,
          CannonDirectionGroup.Left => CannonDirectionGroup.Right,
          CannonDirectionGroup.Right => CannonDirectionGroup.Forward,
          _ => throw new ArgumentOutOfRangeException()
        },
        _ => throw new Exception("Invalid dir. Must be -1 or 1")
      };
    }

    public void SetGroup(CannonDirectionGroup group)
    {
      if (lastManualGroup == group) return;
      lastManualGroup = group;
      OnCannonGroupChange?.Invoke(group);
    }

    public void FireManualGroup(CannonDirectionGroup group)
    {
      if (!targetController)
      {
        LoggerProvider.LogWarning("No target controller but somehow tried to fire a cannon. This should not be possible as TargetingController initializes CannonFiringHotkeys ");
        return;
      }
      targetController.Request_ManualFireCannonGroup(group);
    }
  }
}