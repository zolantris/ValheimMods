// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class ElectricityPylon : MonoBehaviour
  {
    public Transform wireConnector;
    public PylonLightningBolt lightningBolt;
    public Transform coilTop;
    public Transform coilBottom;
    public Transform lightningBoltParent;
    public Material lightningMaterial;

    private void Awake()
    {

      lightningBoltParent = transform.Find("lightning_effects");
      coilTop = transform.Find("coil/start");
      coilBottom = transform.Find("coil/end");
      wireConnector = transform.Find("wire_connector");
      
      lightningBolt = lightningBoltParent.GetComponent<PylonLightningBolt>();
      if (!lightningBolt)
      {
        lightningBolt = lightningBoltParent.gameObject.AddComponent<PylonLightningBolt>();
      }
      
      var randomSpeed = lightningBolt.Duration * Random.Range(0.9f, 1.1f);
      lightningBolt.Duration = randomSpeed;
      
      ElectricityPylonRegistry.Add(this);
    }

    private void OnEnable()
    {
      if (lightningBolt)
      {
        lightningBolt.StartObject = coilTop.gameObject;
        lightningBolt.EndObject = coilBottom.gameObject;
      }
    }

    private void OnDestroy()
    {
      if (!Application.isPlaying) return;
      ElectricityPylonRegistry.Remove(this);
    }

    public void UpdateCoilPosition(GameObject start, GameObject end)
    {
      if (lightningBolt)
      {
        lightningBolt.StartObject = start;
        lightningBolt.EndObject = end;
      }
    }
  }
}