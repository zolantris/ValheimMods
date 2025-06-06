// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerPylon : MonoBehaviour, IPowerNode
  {
    public static Material LightningMaterial = null!;
    public Transform wireConnector;
    public LightningBolt lightningBolt;
    public Transform coilTop;
    public Transform coilBottom;
    public Transform lightningBoltParent;

    [SerializeField] private float maxConnectionDistance = 50f;

    public float MaxConnectionDistance => maxConnectionDistance;

    protected virtual void Awake()
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

      lightningBolt.Duration *= Random.Range(0.9f, 1.1f);
    }

    protected virtual void Start()
    {
#if UNITY_2022
      // todo make a simplified unity method for registering and testing these consumers with our PowerManager.
      //
      // if (canSelfRegisterToNetwork)
      // {
      //   PowerNetworkController.RegisterPowerComponent(this); // or RegisterNode(this)
      // }
#endif
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
#if UNITY_2022
      // if (!Application.isPlaying) return;
      // if (canSelfRegisterToNetwork)
      // {
      //   PowerNetworkController.UnregisterPowerComponent(this);
      // }
#endif
    }

    public string NetworkId
    {
      get;
      private set;
    } = string.Empty;

    public Vector3 Position => transform.position;
    public bool IsActive => true;

    public Vector3 ConnectorPoint => wireConnector.position;

    public void SetNetworkId(string id)
    {
      NetworkId = id;
    }

    private void SetupPylonLightningConfig()
    {
      var lineRenderer = lightningBoltParent.GetComponent<LineRenderer>() ?? lightningBoltParent.gameObject.AddComponent<LineRenderer>();
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