using Jotunn.Extensions;
using UnityEngine;
namespace ValheimVehicles.Helpers;

public static class TransformUtils
{
  public static GameObject GetOrFindObj(GameObject returnObj,
    GameObject searchObj,
    string objectName)
  {
    if ((bool)returnObj) return returnObj;

    var gameObjTransform = searchObj.transform.FindDeepChild(objectName);
    if (!gameObjTransform) return returnObj;

    returnObj = gameObjTransform.gameObject;
    return returnObj;
  }
}