#region

using UnityEngine;
using ValheimVehicles.SharedScripts.UI;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class SwivelIntegrationTester : MonoBehaviour
  {
    public SwivelComponent swivelComponent;

    [SerializeField] public Transform windDirection;
    // Start is called before the first frame update
    private void Start()
    {
      if (!swivelComponent) GetComponent<SwivelComponent>();
      if (swivelComponent && SwivelUIPanelComponent.Instance)
      {
        SwivelUIPanelComponent.Instance.BindTo(swivelComponent);
      }
    }

    public void FixedUpdate()
    {
      if (windDirection)
      {
        // SwivelComponent. = windDirection.forward;
      }
    }
  }
}