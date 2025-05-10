// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class ElectricPylon : MonoBehaviour
  {
    public static Material LightningMaterial = null !;
    public Transform wireConnector;
    public LightningBolt lightningBolt;
    public Transform coilTop;
    public Transform coilBottom;
    public Transform lightningBoltParent;

    private void Awake()
    {

      lightningBoltParent = transform.Find("lightning_effects");
      coilTop = transform.Find("coil/start");
      coilBottom = transform.Find("coil/end");
      wireConnector = transform.Find("wire_connector");

      lightningBolt = lightningBoltParent.GetComponent<LightningBolt>();
      if (!lightningBolt)
      {
        SetupPylonLightningConfig();
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

    private void SetupPylonLightningConfig()
    {
      var lineRenderer = lightningBoltParent.GetComponent<LineRenderer>() ? lightningBoltParent.GetComponent<LineRenderer>() : lightningBoltParent.gameObject.AddComponent<LineRenderer>();
      lineRenderer.material = LightningMaterial;

      lightningBolt = lightningBoltParent.gameObject.AddComponent<LightningBolt>();
      lightningBolt.Duration = 0.4f;
      lightningBolt.ChaosFactor = 0.15f;
      lightningBolt.m_rows = 8;
      lightningBolt.Columns = 1;
      lightningBolt.AnimationMode = LightningBoltAnimationMode.PingPong;

      lineRenderer.startWidth = 0.02f;
      lineRenderer.endWidth = 0.01f;
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