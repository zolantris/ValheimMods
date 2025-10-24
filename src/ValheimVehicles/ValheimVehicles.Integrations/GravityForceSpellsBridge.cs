using UnityEngine;
using ValheimVehicles.SharedScripts.Magic;
namespace ValheimVehicles.Integrations;

public class GravityForceSpellsBridge : GravityForceSpells
{
  protected override Camera GetGameCamera()
  {
    if (GameCamera.m_instance == null) return null;
    return GameCamera.instance.m_camera;
  }
}