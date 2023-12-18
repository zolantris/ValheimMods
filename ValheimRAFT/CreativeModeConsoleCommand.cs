using Jotunn.Entities;
using UnityEngine;

namespace ValheimRAFT
{
  internal class CreativeModeConsoleCommand : ConsoleCommand
  {
    public override string Name => "RaftCreative";

    public override string Help => "Sets the current raft you are standing on into creative mode.";

    public override void Run(string[] args)
    {
      Player localPlayer = Player.m_localPlayer;
      if (!localPlayer)
        return;
      Ship standingOnShip = ((Character)localPlayer).GetStandingOnShip();
      if (standingOnShip &&
          CreativeModeConsoleCommand.ToggleMode(localPlayer, standingOnShip))
        return;

      RaycastHit raycastHit;
      if (!Physics.Raycast(((Component)GameCamera.instance).transform.position,
            ((Component)GameCamera.instance).transform.forward, out raycastHit, 50f,
            LayerMask.GetMask(new string[1]
            {
              "piece"
            })))
        return;
      MoveableBaseRootComponent componentInParent =
        raycastHit.collider.GetComponentInParent<MoveableBaseRootComponent>();
      if (componentInParent)
        CreativeModeConsoleCommand.ToggleMode(localPlayer, componentInParent.m_ship);
    }

    private static bool ToggleMode(Player player, Ship ship)
    {
      MoveableBaseShipComponent component1 =
        ((Component)ship).GetComponent<MoveableBaseShipComponent>();
      if (!component1)
        return false;
      ZSyncTransform component2 = ((Component)ship).GetComponent<ZSyncTransform>();
      component1.m_rigidbody.isKinematic = !component1.m_rigidbody.isKinematic;
      component2.m_isKinematicBody = component1.m_rigidbody.isKinematic;
      if (component1.m_rigidbody.isKinematic)
      {
        if (player.transform.parent == component1.m_baseRoot.transform)
          ((Character)player).m_body.position = new Vector3(
            ((Component)((Character)player).m_body).transform.position.x,
            ((Component)((Character)player).m_body).transform.position.y + 34.5f -
            component1.m_rigidbody.position.y,
            ((Component)((Character)player).m_body).transform.position.z);
        component1.m_rigidbody.position = new Vector3(((Component)component1).transform.position.x,
          35f, ((Component)component1).transform.position.z);
        Rigidbody rigidbody = component1.m_rigidbody;
        Quaternion rotation = component1.m_rigidbody.rotation;
        Quaternion quaternion = Quaternion.Euler(0.0f,
          (float)((double)Mathf.Floor(rotation.eulerAngles.y / 22.5f) * 22.5),
          0.0f);
        rigidbody.rotation = quaternion;
      }

      return true;
    }
  }
}