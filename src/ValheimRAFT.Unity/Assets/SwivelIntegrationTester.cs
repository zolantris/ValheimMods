#region

using UnityEngine;
using ValheimVehicles.SharedScripts.UI;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class SwivelIntegrationTester : MonoBehaviour
  {
    public SwivelComponent swivelComponent;
    // Start is called before the first frame update
    private void Start()
    {
      if (!swivelComponent) GetComponent<SwivelComponent>();
      // if (swivelComponent && SwivelUIPanelComponent.Instance)
      // {
      //   SwivelUIPanelComponent.Instance.BindTo(swivelComponent);
      // }
    }
  }
}