// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.CreativeModeConsoleCommand
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using Jotunn.Entities;
using UnityEngine;

namespace ValheimRAFT
{
  internal class CreativeModeConsoleCommand : ConsoleCommand
  {
    public virtual string Name => "RaftCreative";

    public virtual string Help => "Sets the current raft you are standing on into creative mode.";

    public virtual void Run(string[] args)
    {
      Player localPlayer = Player.m_localPlayer;
      if (!Object.op_Implicit((Object) localPlayer))
        return;
      Ship standingOnShip = ((Character) localPlayer).GetStandingOnShip();
      if (Object.op_Implicit((Object) standingOnShip) && CreativeModeConsoleCommand.ToggleMode(localPlayer, standingOnShip))
        return;
      RaycastHit raycastHit;
      if (!Physics.Raycast(((Component) GameCamera.instance).transform.position, ((Component) GameCamera.instance).transform.forward, ref raycastHit, 50f, LayerMask.GetMask(new string[1]
      {
        "piece"
      })))
        return;
      MoveableBaseRootComponent componentInParent = ((Component) ((RaycastHit) ref raycastHit).collider).GetComponentInParent<MoveableBaseRootComponent>();
      if (Object.op_Implicit((Object) componentInParent))
        CreativeModeConsoleCommand.ToggleMode(localPlayer, componentInParent.m_ship);
    }

    private static bool ToggleMode(Player player, Ship ship)
    {
      MoveableBaseShipComponent component1 = ((Component) ship).GetComponent<MoveableBaseShipComponent>();
      if (!Object.op_Implicit((Object) component1))
        return false;
      ZSyncTransform component2 = ((Component) ship).GetComponent<ZSyncTransform>();
      component1.m_rigidbody.isKinematic = !component1.m_rigidbody.isKinematic;
      component2.m_isKinematicBody = component1.m_rigidbody.isKinematic;
      if (component1.m_rigidbody.isKinematic)
      {
        if (Object.op_Equality((Object) ((Component) player).transform.parent, (Object) ((Component) component1.m_baseRoot).transform))
          ((Character) player).m_body.position = new Vector3(((Component) ((Character) player).m_body).transform.position.x, ((Component) ((Character) player).m_body).transform.position.y + 34.5f - component1.m_rigidbody.position.y, ((Component) ((Character) player).m_body).transform.position.z);
        component1.m_rigidbody.position = new Vector3(((Component) component1).transform.position.x, 35f, ((Component) component1).transform.position.z);
        Rigidbody rigidbody = component1.m_rigidbody;
        Quaternion rotation = component1.m_rigidbody.rotation;
        Quaternion quaternion = Quaternion.Euler(0.0f, (float) ((double) Mathf.Floor(((Quaternion) ref rotation).eulerAngles.y / 22.5f) * 22.5), 0.0f);
        rigidbody.rotation = quaternion;
      }
      return true;
    }
  }
}
