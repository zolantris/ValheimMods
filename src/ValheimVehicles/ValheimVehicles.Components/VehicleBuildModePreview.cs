using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Patches;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Components;

public class VehicleBuildModePreview : MonoBehaviour
{
  public HoverFadeText m_hoverFadeText;

  public void Awake()
  {
    m_hoverFadeText = HoverFadeText.CreateHoverFadeText(transform);

    // don't fade.
    m_hoverFadeText.SetFadeTime(9999f);
  }

  public void FixedUpdate()
  {
    if (m_hoverFadeText == null) return;
    // only run for ghost mode.
    var lastHitPiece = PatchSharedData.PlayerLastRayPiece;
    if (lastHitPiece == null) return;
    var vehicle = lastHitPiece.GetComponentInParent<VehiclePiecesController>();
    if (vehicle == null) return;
    if (vehicle.Manager == null) return;

    // todo ensure this does not spam.

    m_hoverFadeText.SetText($"Current Mode: {vehicle.Manager.BuildMode.GetModeText()}\nNext Mode: {vehicle.Manager.BuildMode.GetNextMode().GetModeText()}");

    if (!m_hoverFadeText.isActiveAndEnabled || !m_hoverFadeText.IsVisible())
    {
      m_hoverFadeText.transform.position = transform.position;
      m_hoverFadeText.Show();
    }
  }
}