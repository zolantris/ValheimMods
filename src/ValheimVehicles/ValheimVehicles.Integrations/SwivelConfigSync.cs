using ValheimVehicles.Components;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.UI;
namespace ValheimVehicles.Integrations;

/// <summary>
/// This gets around the non-generic usage of MonoBehavior. We have to directly make this class extension and then are able to AddComponent etc.
/// </summary>
public class SwivelConfigSync : PrefabConfigSync<SwivelCustomConfig, ISwivelConfig>
{


  public override void OnLoad()
  {
    if (controller == null || SwivelUIPanelComponentIntegration.Instance == null) return;

    var swivel = (SwivelComponent)controller;

    // Only update if currently bound to this swivel
    var panel = SwivelUIPanelComponent.Instance as SwivelUIPanelComponentIntegration;
    if (panel != null && panel.CurrentSwivel == swivel)
    {
      panel.SyncUIFromPartialConfig(Config);
    }

    swivel.mode = Config.Mode;
    swivel.InterpolationSpeed = Config.InterpolationSpeed;
    swivel.hingeAxes = Config.HingeAxes;
    swivel.currentMotionState = Config.MotionState;
  }
}