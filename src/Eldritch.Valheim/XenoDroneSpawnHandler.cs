using System;
using UnityEngine;
using Eldritch.Core;
using ValheimVehicles.SharedScripts;
namespace Eldritch.Valheim;

public class XenoDroneSpawnHandler : MonoBehaviour
{
  public XenoDroneAI droneAi;
  public bool HasRandomizedPackId = false;
  public static bool shouldAssignPrimaryTargetToPlayer = true;
  private void Awake()
  {
    droneAi = GetComponent<XenoDroneAI>();
    UpdatePackId();
    InvokeRepeating(nameof(UpdateXeno), 0f, 5f);
  }

  public void UpdatePackId()
  {
    if (HasRandomizedPackId) return;
    if (!droneAi) return;
    var randomInt = UnityEngine.Random.Range(1, 5);

    // guard in case it doesn't work.
    droneAi.packId = Mathf.RoundToInt(randomInt);
    HasRandomizedPackId = true;
  }

  public void UpdatePlayerTarget()
  {
    if (!shouldAssignPrimaryTargetToPlayer) return;
    if (Player.m_localPlayer == null) return;
    droneAi.PrimaryTarget = Player.m_localPlayer.transform;
  }

  public void UpdateXeno()
  {
    if (!droneAi) return;
    UpdatePlayerTarget();
    UpdatePackId();
  }
}