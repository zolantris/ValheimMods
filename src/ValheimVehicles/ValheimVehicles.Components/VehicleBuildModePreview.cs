using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Patches;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Components;

public class VehicleBuildModePreview : MonoBehaviour
{
  public HoverFadeText? m_hoverFadeText;
  public static Vector3 transformOffset = Vector3.up * 1f;

  public void Awake()
  {
    m_hoverFadeText = HoverFadeText.CreateHoverFadeText(transform);

    // object spawns inside a Vector3.one * 0.1f cube so this is rescaled.
    var hoverFadeTextTransform = m_hoverFadeText.transform;
    hoverFadeTextTransform.SetGlobalScale(Vector3.one);

    // don't fade.
    m_hoverFadeText.SetFadeTime(9999f);
  }

  public void FixedUpdate()
  {
    if (m_hoverFadeText == null) return;
    var lastHitPiece = PatchSharedData.PlayerLastRayPiece;
    if (lastHitPiece == null) return;
    var vehicle = lastHitPiece.GetComponentInParent<VehiclePiecesController>();
    if (vehicle == null) return;
    if (vehicle.Manager == null) return;

    var text = $"Current Mode: {vehicle.Manager.BuildMode.GetModeText()}\nNext Mode: {vehicle.Manager.BuildMode.GetNextMode().GetModeText()}";

    // must run on the frame otherwise it does not update position properly.
    m_hoverFadeText.transform.position = transform.position + transformOffset;

    m_hoverFadeText.SetText(text);

    if (!m_hoverFadeText.isActiveAndEnabled || !m_hoverFadeText.IsVisible())
    {
      m_hoverFadeText.transform.position = transform.position;
      m_hoverFadeText.Show();
    }
  }
}