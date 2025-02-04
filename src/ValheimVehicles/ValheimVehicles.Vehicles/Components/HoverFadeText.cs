using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace ValheimVehicles.Vehicles;

public class HoverFadeText : MonoBehaviour
{
  public static bool hasHudEnabled = true;
  public static float hideTextTimer = 3f;
  // public static List<HoverFadeText> Instances = [];
  public TextMeshPro textMeshPro;
  public float timePassedSinceStateUpdate;
  private float messageFadeValue = 255f;
  private static readonly Color messageColor = new(249f, 224f, 0f, 255f);
  public string currentText;

  public bool ShouldHideAfterLastStateUpdate()
  {
    if (Mathf.Approximately(hideTextTimer, 0f)) return false;
    return timePassedSinceStateUpdate > hideTextTimer;
  }

  private void hideText()
  {
    if (gameObject.activeSelf) gameObject.SetActive(false);
  }

  private void showText()
  {
    if (!gameObject.activeSelf) gameObject.SetActive(true);
  }

  public void ResetHoverTimer()
  {
    timePassedSinceStateUpdate = 0f;
  }

  /// <summary>
  /// Method meant to be called by integrated component
  /// </summary>
  public void UpdateText()
  {
    if (Camera.main == null) return;
    if (!hasHudEnabled || ShouldHideAfterLastStateUpdate())
    {
      hideText();
      return;
    }

    showText();

    if (hideTextTimer != 0f)
      timePassedSinceStateUpdate += Time.fixedDeltaTime;

    // Calculate the point at which the fade should start (last 25% of the timer)
    var fadeStartTime = hideTextTimer * 0.1f;

    // Only start fading when we're in the last 25% of the timer
    if (timePassedSinceStateUpdate > fadeStartTime)
    {
      // Calculate the normalized time for the fading effect (approaches 0 as time passes)
      var fadeProgress = Mathf.InverseLerp(fadeStartTime, hideTextTimer,
        timePassedSinceStateUpdate);

      // Use this progress value to lerp the alpha value
      messageFadeValue =
        Mathf.Lerp(1f, 0f,
          fadeProgress); // Fade from 1 to 0 over the last 25%

      // Apply the new alpha value to the color
      textMeshPro.color = new Color(messageColor.r, messageColor.g,
        messageColor.b, messageFadeValue);
    }
    else
    {
      textMeshPro.color = messageColor;
    }

    transform.LookAt(Camera.main.transform);
    textMeshPro.text = currentText;
    transform.rotation =
      Quaternion.LookRotation(transform.forward *
                              -1); // Flip to face correctly
    // anchorStateTextMesh.fontSize = anchorTextSize;

  }
}