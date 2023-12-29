using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Reflection;
using System.Security.Cryptography;
using Jotunn.Entities;
using UnityEngine;
using UnityEngine.U2D;

namespace ValheimVehicles.VehicleUtils;

public class PrefabRegistry : MonoBehaviour
{
  public void GeneratePrefab(string name, string description)
  {
  }

  public void prefabListener()
  {
  }

  public static void CreateCustomFloor()
  {
    var prefabManager = PrefabManager.Instance;
    var pieceManger = PieceManager.Instance;
    var floor = ZNetScene.instance.GetPrefab("wood_floor");
    var largeWoodFloor = prefabManager.CreateClonedPrefab("VVehiclesWoodFloor4x4", floor);

    // var m_assetBundle =
    //   AssetUtils.LoadAssetBundleFromResources("valheimraft", Assembly.GetExecutingAssembly());
    // var sprites = m_assetBundle.LoadAsset<SpriteAtlas>("Assets/icons.spriteatlas");

    // In theory this should be 4x larger
    largeWoodFloor.transform.localScale = new Vector3(4, 4, 4);


    Logger.LogDebug("Registered CustomFloor");
    // prefabManager.AddPrefab(largeWoodFloor);
    pieceManger.AddPiece(new CustomPiece(largeWoodFloor, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "This is a larger wood slab",
      // Icon = sprites.GetSprite("raftmast"),
      Category = "Crafting",
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 40,
          Item = "Wood",
          Recover = true
        }
      }
    }));

    // var floor = ZNetScene.instance.GetPrefab("wood_floor");
    // for (var x = -1f; x < 1.01f; x += 2f)
    // for (var z = -2f; z < 2.01f; z += 2f)
    // {
    //   var pt = transform.TransformPoint(new Vector3(x, 0.6f, z));
    //   var obj = Instantiate(floor, pt, transform.rotation);
    //   var baseObject = new GameObject();
    //   baseObject.AddComponent(obj);
    //   largeWoodFloor.GetComponents<GameObject>(obj);
    //   // var floorItem = largeWoodFloor.GetComponent<Piece>(floor.gameObject);
    //   // var netview = obj.GetComponent<ZNetView>();
    //   // m_baseRoot.AddNewPiece(netview);
    // }

    // largeWoodFloor.name = "$vvehicle_floor_2x3";
  }

  public static void CreateCustomPrefabs()
  {
    CreateCustomFloor();
  }
}