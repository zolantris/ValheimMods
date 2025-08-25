#region

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using TMPro;
  using UnityEngine;
  using UnityEngine.TextCore;
  using Zolantris.Shared;
#if VALHEIM
  using ValheimVehicles.Prefabs;
#endif
  using static System.String;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts
  {

    public class HoverFadeText : MonoBehaviour
    {
      public static bool hasHudEnabled = true;
      public static float hideTextTimer = 3f;
      private static readonly Color messageColor = new(249f, 224f, 0f, 255f);
      public float timePassedSinceStateUpdate;
      public string currentText = "";
      public bool canUpdate = true;
      private float messageFadeValue = 255f;
      private CoroutineHandle UpdateTextCoroutine = null!;

      private static TMP_FontAsset s_DefaultArialSdf;
      public TextMeshPro textMeshPro = null!;
      [SerializeField] public TMP_FontAsset fontAsset = null!;


      public void Awake()
      {
        UpdateTextCoroutine ??= new CoroutineHandle(this);
        textMeshPro = gameObject.AddComponent<TextMeshPro>();
        gameObject.layer = LayerMask.NameToLayer("UI");

        if (fontAsset == null)
        {
          EnsureDefaultTmpFont(textMeshPro);
        }

        // Match Valheim-ish yellow, using byte channels (crisper than floats at 0–255)
        textMeshPro.color = new Color32(249, 224, 0, 255);

        var features = textMeshPro.fontFeatures ?? new List<OTL_FeatureTag>();
        if (!features.Contains(OTL_FeatureTag.kern))
        {
          features.Add(OTL_FeatureTag.kern); // kerning
        }
        if (!features.Contains(OTL_FeatureTag.liga))
        {
          features.Add(OTL_FeatureTag.liga); // common ligatures
        }
        textMeshPro.fontFeatures = features; // assign back

        // World-space text hygiene
        textMeshPro.alignment = TextAlignmentOptions.Center;

        textMeshPro.extraPadding = true; // adds atlas padding so outlines/dilates don’t clip

        textMeshPro.overflowMode = TextOverflowModes.Overflow; // prevents cutoff
        textMeshPro.color = messageColor;
        textMeshPro.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.outlineWidth = 0.136f;
        textMeshPro.fontSize = 4;
      }

      public void OnEnable()
      {
        UpdateTextCoroutine ??= new CoroutineHandle(this);
      }

      public static HoverFadeText CreateHoverFadeText(Transform? parent = null)
      {
        var hoverFadeGameObject = new GameObject("HoverFadeText")
        {
          transform = { parent = parent, localPosition = Vector3.zero, localRotation = Quaternion.identity, localScale = Vector3.one }
        };
        var hoverFadeText = hoverFadeGameObject.AddComponent<HoverFadeText>();
        return hoverFadeText;
      }

      private static void EnsureDefaultTmpFont(TextMeshPro label)
      {
        if (!label) return;
        if (label.font) return; // already assigned elsewhere

        // Try project-wide TMP default first
        var font = TMP_Settings.defaultFontAsset;

        // If none, create once from built-in Arial (Valheim ships this)
        if (!font)
        {
          if (!s_DefaultArialSdf)
          {
            var arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (arial)
            {
              s_DefaultArialSdf = TMP_FontAsset.CreateFontAsset(arial);
              // Make sure material is usable
              if (s_DefaultArialSdf && s_DefaultArialSdf.material)
                s_DefaultArialSdf.material.enableInstancing = true;
            }
          }
          font = s_DefaultArialSdf;
        }

        if (font)
        {
          label.font = font;
          if (font.material) label.fontMaterial = font.material;
        }
        else
        {
          Debug.LogWarning("[HoverFadeText] Could not resolve a TMP_FontAsset. Text may not render.");
        }
      }

      public bool ShouldHideAfterLastStateUpdate()
      {
        if (Mathf.Approximately(hideTextTimer, 0f)) return false;
        return timePassedSinceStateUpdate >= hideTextTimer;
      }

      /// <summary>
      /// Hide will not reset the timer as Hide is called in the FixedUpdate method.
      /// </summary>
      public void Hide()
      {
        if (gameObject.activeSelf)
        {
          gameObject.SetActive(false);
        }

        UpdateTextCoroutine.Stop();
      }

      /// <summary>
      /// Show resets HoverTimer as well when the gameobject was previously hidden.
      /// </summary>
      public void Show()
      {
        ResetHoverTimer();
        gameObject.SetActive(true);
        UpdateTextCoroutine.Start(UpdateText());
      }

      public void ResetHoverTimer()
      {
        timePassedSinceStateUpdate = 0f;
      }

      public IEnumerator UpdateText()
      {
        while (isActiveAndEnabled)
        {
          if (!canUpdate) yield return new WaitForFixedUpdate();
          if (!Camera.main) yield return new WaitForFixedUpdate();

          if (!hasHudEnabled || currentText == Empty || ShouldHideAfterLastStateUpdate() || textMeshPro == null)
          {
            Hide();
            yield break;
          }

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

          if (Camera.main)
          {
            transform.LookAt(Camera.main.transform);
          }

          textMeshPro.text = currentText;
          transform.rotation =
            QuaternionExtensions.LookRotationSafe(transform.forward *
                                                  -1); // Flip to face correctly
          // anchorStateTextMesh.fontSize = anchorTextSize;
          yield return new WaitForFixedUpdate();
        }
      }
    }
  }