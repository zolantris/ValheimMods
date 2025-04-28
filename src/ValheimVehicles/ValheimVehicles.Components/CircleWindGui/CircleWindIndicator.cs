using UnityEngine;

namespace ValheimVehicles.Components;

public class CircleWindIndicator : MonoBehaviour
{
  public static GameObject OrangeCircle;
  public static GameObject BlackClipCircle;
  private CircleLine orangeCircleLine;
  private CircleLine blackClipCircleLine;

  private void Awake()
  {
    // for unity debugging
    var children = GetComponentsInChildren<CircleLine>();

    if (children == null)
    {
      return;
    }

    foreach (var child in children)
    {
      Destroy(child.gameObject);
    }

    OrangeCircle = new GameObject("OrangeCircle")
    {
      layer = LayerMask.NameToLayer("UI"),
      transform = { parent = transform }
    };
    OrangeCircle.SetActive(false);
    orangeCircleLine = OrangeCircle.AddComponent<CircleLine>();
    orangeCircleLine.MaterialColor = CircleWindColors.ValheimWindOrange;

    BlackClipCircle = new GameObject("BlackClipCircle")
    {
      layer = LayerMask.NameToLayer("UI"),
      transform = { parent = transform, localPosition = new Vector3(0, 0, -1f) }
    };

    BlackClipCircle.SetActive(false);
    blackClipCircleLine = BlackClipCircle.AddComponent<CircleLine>();
    blackClipCircleLine.MaterialColor = CircleWindColors.ValheimWindGray;
    blackClipCircleLine.arc = 45;
    blackClipCircleLine.segments = 18;

    OrangeCircle.SetActive(true);
    BlackClipCircle.SetActive(true);
  }

  public void Cleanup()
  {
    if (OrangeCircle)
    {
      Destroy(BlackClipCircle.gameObject);
    }

    if (OrangeCircle)
    {
      Destroy(OrangeCircle.gameObject);
    }
  }

  public void OnDestroy()
  {
    Cleanup();
  }
}