using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimRAFT;

public class RecoverRaftConsoleCommand : ConsoleCommand
{
  private static class RecoverRaftCommands
  {
    public const string Preview = "preview";
    public const string Confirm = "confirm";
  }

  private const string CommandName = "raftrecover";

  public override string Name => CommandName;

  public override List<string> CommandOptionList() =>
  [
    RecoverRaftCommands.Preview,
    RecoverRaftCommands.Confirm,
  ];

  public override string Help =>
    "Attempts to recover unattached rafts." +
    "\nMust provide a confirm command to recover the rafts." +
    "default radius is 1000 (units) but the user can supply a number such as 50 units if they want to only recover their raft within a smaller area";

  public override void Run(string[] args)
  {
    var radius = 1000f;
    foreach (var arg in args)
    {
      try
      {
        var parsedFloat = float.Parse(arg);
        radius = parsedFloat;
        break;
      }
      catch (Exception e)
      {
        // ignored
      }
    }

    var unattachedVehicleNetViews = GetUnAttachedNetViews(CommandName, radius);

    Logger.LogDebug($"Found {unattachedVehicleNetViews.Count} potential ships to recover.");

    if (args.Contains(RecoverRaftCommands.Confirm))
    {
      RecoverShip(unattachedVehicleNetViews);
      return;
    }

    if (unattachedVehicleNetViews.Count > 0)
    {
      Logger.LogDebug("Use \"RaftRecover confirm\" to complete the recover.");
    }
  }

  public static void RecoverRaftWithoutDryRun(string commandName = CommandName,
    float radius = 1000f)
  {
    var unattachedVehicleNetViews = GetUnAttachedNetViews(commandName, radius);
    RecoverShip(unattachedVehicleNetViews);
  }

  private static Dictionary<int, List<ZNetView>> GetUnAttachedNetViews(string commandName,
    float radius)
  {
    var colliders = Physics.OverlapSphere(GameCamera.instance.transform.position, radius);
    var unattached = new Dictionary<ZDOID, List<ZNetView>>();
    var unattachedRaftIds = new Dictionary<int, List<ZNetView>>();

    Logger.LogInfo($"{commandName}: Searching {GameCamera.instance.transform.position}");

    var colliderRoots = new List<Transform>();

    foreach (var collider in colliders)
    {
      if (colliderRoots.Contains(collider.transform.root))
      {
        Logger.LogDebug("Skipping collider root that already exists");
        continue;
      }

      colliderRoots.Add(collider.transform.root);

      var nv = collider.GetComponent<ZNetView>();
      if (!nv)
      {
        nv = collider.GetComponentInParent<ZNetView>();
      }

      if (!nv)
      {
        var rootNv = collider.transform.root.gameObject.GetComponent<ZNetView>();
        var rootChildrenWithNv = collider.transform.root.GetComponentInChildren<ZNetView>();

        if (rootNv)
        {
          nv = rootNv;
        }
      }

      if (nv == null || nv.m_zdo == null) continue;

      var withinMBRoot = (bool)nv.GetComponentInParent<MoveableBaseRootComponent>();
      var withinVehicleRoot = (bool)nv.GetComponentInParent<VehiclePiecesController>();
      if (withinMBRoot || withinVehicleRoot)
      {
        continue;
      }

      var zdoid2 = nv.m_zdo.GetZDOID(MoveableBaseRootComponent.MBParentHash);
      var mbRaftZdo = nv.m_zdo.GetInt(MoveableBaseRootComponent.MBParentIdHash);


      var parentInstance = ZNetScene.instance.FindInstance(zdoid2);
      if (parentInstance != null) continue;


      if (mbRaftZdo != 0)
      {
        Logger.LogDebug($"MbRaftZDO found in {nv.name}");
        if (!unattachedRaftIds.TryGetValue(mbRaftZdo, out var netViewList))
        {
          netViewList = [];
          unattachedRaftIds.Add(mbRaftZdo, netViewList);
        }

        netViewList.Add(nv);
        continue;
      }

      if (zdoid2 != ZDOID.None)
      {
        if (!unattached.TryGetValue(zdoid2, out var list2))
        {
          list2 = new List<ZNetView>();
          unattached.Add(zdoid2, list2);
        }

        list2.Add(nv);
      }
      else
      {
        Vector3 partOffset =
          nv.m_zdo.GetVec3(MoveableBaseRootComponent.MBPositionHash, Vector3.zero);
      }
    }

    return unattachedRaftIds;
  }

  private static void RecoverShip(Dictionary<int, List<ZNetView>> unattachedVehicleNetViews)
  {
    foreach (var id in unattachedVehicleNetViews.Keys)
    {
      var list = unattachedVehicleNetViews[id];
      var vehicleShip = VehicleShip.InitWithoutStarterPiece(list[0].transform);
      foreach (var piece in list)
      {
        piece.transform.SetParent(vehicleShip.VehiclePiecesController.Instance.transform);
        piece.transform.localPosition =
          piece.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        piece.transform.localRotation =
          Quaternion.Euler(piece.m_zdo.GetVec3(VehicleZdoVars.MBRotationVecHash,
            Vector3.zero));
        vehicleShip.VehiclePiecesController.Instance.AddNewPiece(piece);
      }

      Logger.LogInfo($"Completed, RecoverShip for {id}, recovering {list.Count} pieces");
    }
  }
}