using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ValheimVehicles.SharedScripts;

public enum AnchorState
{
  Idle, // No action is taking place.

  Lowering, // The anchor is in the process of being dropped.
  Anchored, // The anchor is fully dropped, at the target position.

  Reeling, // The anchor is being reeled back in.
  Recovered // The anchor is fully reeled back in to its starting position.
}

public class AnchorMechanismController : ParentCollisionListener
{
  public const int baseTextSize = 4;
  public static bool HasAnchorTextHud = true;
  public static float HideAnchorTimer = 3f;
  public int anchorTextSize = baseTextSize;

  // This will make the character smaller allowing denser font
  public float anchorTextCharacterSize = 0.25f;
  // transforms

  // Point from which the rope is attached (usually the ship)
  public Transform
    rotationAnchorRopeAttachpoint;

  // Point where the rope is attached (top of the anchor)
  public Transform
    anchorRopeAttachmentPoint;

  public Transform anchorRopeAttachStartPoint;
  public Transform anchorReelTransform;
  public Transform anchorReelCogsTransform;

  public float anchorDropDistance = 10f; // Maximum depth the anchor can go
  public float reelSpeed = 5f; // Speed at which the anchor reels in
  public float reelCogAngleMult = 100f; // Speed at which the anchor reels in

  public Transform anchorTransform;

  public AnchorState
    currentState =
      AnchorState
        .Recovered; // Current state of the anchor (Idle, Dropping, Reeling)

  public LineRenderer ropeLine; // LineRenderer to visualize rope

  public Rigidbody prefabRigidbody;

  public bool CanUseHotkeys = true;

  public TextMeshPro anchorStateTextMesh;

  private readonly Color messageColor = new(249f, 224f, 0f, 255f);

  private Rigidbody anchorRb;
  private Vector3 anchorStartLocalPosition;
  private Transform anchorTextMeshProTransform;
  private float messageFadeValue = 255f;

  private float timePassedSinceStateUpdate;

  public static List<AnchorMechanismController> Instances = new();

  internal void OnEnable()
  {
    Instances.Add(this);
  }

  internal void OnDisable()
  {
    Instances.Remove(this);
  }

  /// <summary>
  ///   This rigidbody should not start awakened to prevent collision problems on
  ///   placement
  /// </summary>
  internal void Awake()
  {
    if (prefabRigidbody == null)
    {
      prefabRigidbody = transform.GetComponent<Rigidbody>();
      if (prefabRigidbody) prefabRigidbody.Sleep();
    }

    if (anchorTransform == null)
      anchorTransform = transform.Find("anchor");

    if (anchorRopeAttachmentPoint == null)
      anchorRopeAttachmentPoint = anchorTransform.Find("attachpoint_anchor");

    if (rotationAnchorRopeAttachpoint == null)
      rotationAnchorRopeAttachpoint =
        transform.Find("attachpoint_rotational");

    if (anchorRopeAttachStartPoint == null)
      anchorRopeAttachStartPoint = transform.Find("attachpoint_anchor_start");

    anchorTextMeshProTransform = transform.Find("hover_anchor_state_message");
    anchorStateTextMesh = anchorTextMeshProTransform.gameObject
      .AddComponent<TextMeshPro>();

    anchorStateTextMesh.color = messageColor;
    anchorStateTextMesh.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
    anchorStateTextMesh.alignment = TextAlignmentOptions.Center;
    anchorStateTextMesh.outlineWidth = 0.136f;
    anchorStateTextMesh.fontSize = 4;

    anchorTransform.Find("scalar/colliders").gameObject
      .AddComponent<ChildCollisionDetector>();
    anchorReelTransform = transform.Find("anchor_reel");
    anchorReelCogsTransform = transform.Find("anchor_reel/cogs");
    anchorRb = anchorTransform.GetComponent<Rigidbody>();
    anchorStartLocalPosition = anchorRb.transform.localPosition;
  }

  private void Start()
  {
    // Initialize LineRenderer for rope visualization
    if (ropeLine == null) ropeLine = GetComponent<LineRenderer>();
    UpdateRopeVisual();
    UpdateAnchorState(AnchorState.Recovered);
    // SetScaledTextSize();
  }

  private void Update()
  {
    if (CanUseHotkeys) HandleKeyInputs();
  }

  public virtual void FixedUpdate()
  {
    if (anchorRb == null) return;
    // Execute behavior based on the current state
    switch (currentState)
    {
      case AnchorState.Lowering:
        DropAnchor();
        break;
      case AnchorState.Reeling:
        ReelAnchor();
        break;
      case AnchorState.Anchored:
        break;
      case AnchorState.Recovered:
      case AnchorState.Idle:
        anchorRb.transform.localPosition = anchorStartLocalPosition;
        anchorRb.transform.localRotation = Quaternion.identity;
        break;
    }

    UpdateRopeVisual();
    UpdateText();
  }

  public void SetScaledTextSize()
  {
    anchorTextSize =
      Mathf.RoundToInt(
        Mathf.Clamp(DisplayScaledValues.GetScaledSize(), baseTextSize,
          baseTextSize * 4));

    anchorStateTextMesh.fontSize = anchorTextSize;
  }

  public override void OnChildCollisionEnter(Collision collision)
  {
    if (currentState == AnchorState.Lowering && collision.gameObject.layer ==
        LayerMask.NameToLayer("terrain"))
      UpdateAnchorState(AnchorState.Anchored);
  }

  /// <summary>
  ///   To be overridden with in-game translations
  /// </summary>
  /// <returns></returns>
  public virtual string GetCurrentStateText()
  {
    return currentState.ToString();
  }

  private void hideAnchorText()
  {
    if (anchorTextMeshProTransform.gameObject.activeInHierarchy)
      anchorTextMeshProTransform.gameObject.SetActive(false);
  }

  private void showAnchorText()
  {
    if (!anchorTextMeshProTransform.gameObject.activeSelf)
      anchorTextMeshProTransform.gameObject.SetActive(true);
  }

  public bool ShouldHideAfterLastStateUpdate()
  {
    if (Mathf.Approximately(HideAnchorTimer, 0f)) return false;
    return timePassedSinceStateUpdate > HideAnchorTimer;
  }

  private void UpdateText()
  {
    if (!HasAnchorTextHud || ShouldHideAfterLastStateUpdate())
    {
      hideAnchorText();
      return;
    }

    showAnchorText();

    if (HideAnchorTimer != 0f)
      timePassedSinceStateUpdate += Time.fixedDeltaTime;

    if (anchorTextMeshProTransform != null && Camera.main != null)
    {
      // Calculate the point at which the fade should start (last 25% of the timer)
      var fadeStartTime = HideAnchorTimer * 0.1f;

      // Only start fading when we're in the last 25% of the timer
      if (timePassedSinceStateUpdate > fadeStartTime)
      {
        // Calculate the normalized time for the fading effect (approaches 0 as time passes)
        var fadeProgress = Mathf.InverseLerp(fadeStartTime, HideAnchorTimer,
          timePassedSinceStateUpdate);

        // Use this progress value to lerp the alpha value
        messageFadeValue =
          Mathf.Lerp(1f, 0f,
            fadeProgress); // Fade from 1 to 0 over the last 25%

        // Apply the new alpha value to the color
        anchorStateTextMesh.color = new Color(messageColor.r, messageColor.g,
          messageColor.b, messageFadeValue);
      }
      else
      {
        anchorStateTextMesh.color = messageColor;
      }

      anchorTextMeshProTransform.LookAt(Camera.main.transform);
      anchorStateTextMesh.text = GetCurrentStateText();
      anchorTextMeshProTransform.rotation =
        Quaternion.LookRotation(anchorTextMeshProTransform.forward *
                                -1); // Flip to face correctly
      // anchorStateTextMesh.fontSize = anchorTextSize;
    }
  }

  public virtual void OnAnchorStateChange(AnchorState newState)
  {
  }

  internal void UpdateAnchorState(AnchorState newState)
  {
    // Do nothing if state is equivalent
    if (newState == currentState) return;
    timePassedSinceStateUpdate = 0f;

    currentState = newState;
    // Execute behavior based on the current state
    switch (currentState)
    {
      case AnchorState.Lowering:
      case AnchorState.Reeling:
      case AnchorState.Anchored:
      case AnchorState.Recovered:
      case AnchorState.Idle:
        anchorRb.isKinematic = true;
        break;
    }

    OnAnchorStateChange(currentState);
  }

  private void HandleKeyInputs()
  {
    if (anchorRb == null) return;
    if (Input.GetKeyDown(KeyCode.D)) // Toggle dropping with 'D' key
    {
      if (currentState != AnchorState.Lowering)
        StartDropping();
      else
        StopDropping();
    }

    if (Input.GetKeyDown(KeyCode.R)) // Toggle reeling with 'R' key
    {
      if (currentState != AnchorState.Reeling)
        StartReeling();
      else
        StopReeling();
    }
  }

  public void StartDropping()
  {
    if (anchorRb == null) return;
    if (currentState != AnchorState.Lowering)
      UpdateAnchorState(AnchorState.Lowering);
  }

  public void StopDropping()
  {
    if (anchorRb == null) return;
    if (currentState == AnchorState.Lowering)
      UpdateAnchorState(AnchorState.Anchored);
  }

  public void StartReeling()
  {
    if (anchorRb == null) return;
    if (currentState != AnchorState.Reeling)
      UpdateAnchorState(AnchorState.Reeling);
  }

  public void StopReeling()
  {
    if (anchorRb == null) return;

    if (currentState == AnchorState.Reeling)
      UpdateAnchorState(AnchorState.Recovered);
  }

  private void DropAnchor()
  {
    // Check if the anchor is still above the maximum drop distance
    if (anchorRopeAttachmentPoint.position.y >
        rotationAnchorRopeAttachpoint.position.y - anchorDropDistance)
    {
      var deltaReelSpeed = reelSpeed * 1.5f * Time.fixedDeltaTime;

      // Move the anchor downward
      anchorRb.MovePosition(anchorRb.position +
                            Vector3.down * deltaReelSpeed);

      // Rotate the cogs using quaternions
      var rotationStep =
        Quaternion.Euler(-reelCogAngleMult * deltaReelSpeed, 0, 0);
      anchorReelCogsTransform.localRotation *= rotationStep;
    }
    else
    {
      // Stop dropping when the anchor reaches the max drop distance
      StopDropping();
    }
  }


  private void ReelAnchor()
  {
    // Only reel if the anchor is below the attachment point (on the Y-axis)
    if (anchorRopeAttachmentPoint.position.y <
        anchorRopeAttachStartPoint.position.y)
    {
      var deltaReelSpeed = reelSpeed * Time.fixedDeltaTime;

      // Move the anchor upward
      anchorRb.MovePosition(anchorRb.position + Vector3.up * deltaReelSpeed);

      // Rotate the cogs using quaternions
      var rotationStep =
        Quaternion.Euler(-reelCogAngleMult * deltaReelSpeed, 0, 0);
      anchorReelCogsTransform.localRotation *= rotationStep;

      // Check if the anchor is close enough to the attachment point
      if (anchorRopeAttachmentPoint.position.y >
          anchorRopeAttachStartPoint.position.y) StopReeling();
    }
    else
    {
      // Stop reeling if the anchor has reached or passed the attachment point
      StopReeling();
    }
  }

  private void UpdateRopeVisual()
  {
    if (ropeLine == null) return;

    ropeLine.useWorldSpace = false; // Ensure the rope uses local space.
    ropeLine.startWidth = 0.2f;
    ropeLine.endWidth = 0.2f;

    // Get the transform of the LineRenderer and the relative positions of the points in local space.
    var ropelineTransform = ropeLine.transform;

    // Calculate the relative local positions from the RopeLine to the anchor points (ignore world space)
    var rotationAttachpoint =
      ropelineTransform.InverseTransformPoint(rotationAnchorRopeAttachpoint
        .position);
    var localStartPosition =
      ropelineTransform.InverseTransformPoint(anchorRopeAttachStartPoint
        .position);
    var localEndPosition =
      ropelineTransform.InverseTransformPoint(anchorRopeAttachmentPoint
        .position);

    // The length of the rope in world space (but it's easier to use the local difference here)
    var ropeLength = Vector3.Distance(localStartPosition, localEndPosition);

    // Number of segments based on rope length (adjust this divisor for finer or coarser rope segments)
    var numberOfPoints =
      Mathf.RoundToInt(ropeLength /
                       2f); // Adjust this divisor for finer or coarser rope segments

    // Ensure a minimum of 2 points (start and end) and a maximum of 40 points
    var lerpedPositionCount = Mathf.Clamp(numberOfPoints + 1, 3, 40);

    // List to store the positions of the rope segments in local space
    var positions = new List<Vector3>();

    // rotation attachpoint
    positions.Add(rotationAttachpoint);
    // Add the first point (local position of the start point)
    positions.Add(
      localStartPosition); // This will be the first point of the rope in local space

    // Loop to interpolate the rope segments in local space
    for (var i = 1; i < lerpedPositionCount; i++)
    {
      // Calculate interpolation factor (t) between 0 and 1
      var t = i / (float)(lerpedPositionCount - 1);

      // Lerp between startPosition (first point) and endPosition (last point) in local space
      var lerpedPosition =
        Vector3.Lerp(localStartPosition, localEndPosition, t);

      // Add this new point (as an offset from the LineRenderer in local space)
      positions.Add(lerpedPosition);
    }

    // Add the last point (local position of the end point)
    positions.Add(
      localEndPosition); // This will be the last point of the rope in local space

    ropeLine.positionCount = positions.Count;
    // Set the calculated positions to the LineRenderer in local space
    ropeLine.SetPositions(positions.ToArray());
  }

  /// <summary>
  ///   For validating anchor state is in safe range. Likely not needed in favor of
  ///   GetSafeAnchorState
  /// </summary>
  /// <param name="anchorState"></param>
  /// <returns></returns>
  public static bool IsAnchorStateValid(AnchorState anchorState)
  {
    switch (anchorState)
    {
      case AnchorState.Idle:
      case AnchorState.Lowering:
      case AnchorState.Anchored:
      case AnchorState.Reeling:
      case AnchorState.Recovered:
        return true;
      default:
        // Logger.LogError("Invalid anchor state. Enum out of range");
        return false;
    }
  }

  /// <summary>
  ///   Returns a safe range for anchor state
  /// </summary>
  /// <param name="anchorStateInt"></param>
  /// <returns></returns>
  public static AnchorState GetSafeAnchorState(int anchorStateInt)
  {
    switch (anchorStateInt)
    {
      case (int)AnchorState.Idle:
      case (int)AnchorState.Lowering:
      case (int)AnchorState.Anchored:
      case (int)AnchorState.Reeling:
      case (int)AnchorState.Recovered:
        return (AnchorState)anchorStateInt;
      default:
        // Logger.LogError("Invalid anchor state. Enum out of range");
        return AnchorState.Recovered;
    }
  }
}