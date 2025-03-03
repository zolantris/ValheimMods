using System.Diagnostics;
using Jotunn;
using Jotunn.Entities;
using UnityEngine;
using ValheimRAFT.Util;
using ZdoWatcher;

namespace ValheimRAFT;

internal class MoveRaftConsoleCommand : ConsoleCommand
{
  public override string Name => "RaftOffset";

  public override string Help =>
    "Offsets the raft by the given coordinates (X Y Z). Note: it's intended use is so you can slightly offset the pieces relative to the raft center. The actual center of the raft will NOT move.";

  public override void Run(string[] args)
  {
    if (args.Length < 3)
    {
      Jotunn.Logger.LogInfo("Missing arguments, arguments required: X Y Z");
      return;
    }

    if (!float.TryParse(args[0], out var x))
    {
      Jotunn.Logger.LogInfo("Invalid argument X: " + args[0]);
      return;
    }

    if (!float.TryParse(args[1], out var y))
    {
      Jotunn.Logger.LogInfo("Invalid argument Y: " + args[1]);
      return;
    }

    if (!float.TryParse(args[2], out var z))
    {
      Jotunn.Logger.LogInfo("Invalid argument Z: " + args[2]);
      return;
    }

    var offset = new Vector3(x, y, z);
    var player = Player.m_localPlayer;
    if (!player) return;

    var ship = player.GetStandingOnShip();
    if ((!ship || !MoveRaft(player, ship, offset)) && Physics.Raycast(
          GameCamera.instance.transform.position,
          GameCamera.instance.transform.forward,
          out var hitinfo, 50f, LayerMask.GetMask("piece")))
    {
      var mbr =
        hitinfo.collider.GetComponentInParent<MoveableBaseRootComponent>();
      if ((bool)mbr) MoveRaft(player, mbr.m_ship, offset);
    }
  }

  public static bool MoveRaft(Player player, Ship ship, Vector3 offset)
  {
    var mb = ship.GetComponent<MoveableBaseShipComponent>();

    if ((bool)mb && (bool)mb.m_baseRoot)
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();
      var id =
        ZdoWatchController.Instance.GetOrCreatePersistentID(mb.m_baseRoot
          .m_nview.m_zdo);
      foreach (var zdo in ZDOMan.instance.m_objectsByID.Values)
      {
        var zdoid = zdo.GetInt(MoveableBaseRootComponent.MBParentIdHash);
        if (zdoid == id)
        {
          var pos = zdo.GetVec3(MoveableBaseRootComponent.MBPositionHash,
            Vector3.zero);
          var newpos = pos + offset;
          zdo.Set(MoveableBaseRootComponent.MBPositionHash, newpos);
          zdo.SetPosition(ship.transform.position);
          var obj = ZNetScene.instance.FindInstance(zdo);
          if ((bool)obj) obj.transform.localPosition = newpos;
        }
      }

      Jotunn.Logger.LogInfo(
        $"Completed MoveRaft in {stopWatch.ElapsedMilliseconds}ms");
      return true;
    }

    return false;
  }
}