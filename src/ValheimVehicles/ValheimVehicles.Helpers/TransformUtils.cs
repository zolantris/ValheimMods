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

  public static void SetGlobalScale(this Transform transform, Vector3 targetGlobalScale)
  {
    if (transform.parent == null)
    {
      // If there is no parent, local scale IS global scale
      transform.localScale = targetGlobalScale;
    }
    else
    {
      // Divide the target scale by the parent's global (lossy) scale
      transform.localScale = new Vector3(
        targetGlobalScale.x / transform.parent.lossyScale.x,
        targetGlobalScale.y / transform.parent.lossyScale.y,
        targetGlobalScale.z / transform.parent.lossyScale.z
      );
    }
  }
}