using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ValheimVehicles.Vehicles.Components;

public class WaterMaskDisplacement : MonoBehaviour
{
  public Material Material;

  private WaterVolume prevWaterVolume = new();

  public static readonly int WaterColorID = Shader.PropertyToID("_WaterColor");
  public static readonly int WaterLevelID = Shader.PropertyToID("_WaterLevel");

  public static readonly int MaskPositionID =
    Shader.PropertyToID("_MaskPosition");

  public static readonly int MaskSizeID = Shader.PropertyToID("_MaskSize");

  public static readonly int TransparencyID =
    Shader.PropertyToID("_Transparency");

  private static Color GreenishColor = new Color(0.1f, 0.5f, 0.5f, 0.3f);

  public Color waterColor = GreenishColor;

  private void Awake()
  {
    Material = GetComponent<MeshRenderer>().material;
  }

  private void Start()
  {
    Material.SetColor(WaterColorID, waterColor);
    Material.SetVector(MaskSizeID,
      new Vector4(4, 4, 4, 0)); // Set size to fit your cube dimensions
    Material.SetFloat(TransparencyID,
      0.7f); // Set transparency for water outside the cube
  }

  private void FixedUpdate()
  {
    UpdateMaterial();
  }

  private void UpdateMaterial()
  {
    var waterLevel =
      Floating.GetWaterLevel(transform.position, ref prevWaterVolume);

    Material.SetFloat(WaterLevelID,
      waterLevel);
    // Set this to your water level Y-coordinate
    Material.SetVector(MaskPositionID,
      new Vector4(transform.position.x, transform.position.y,
        transform.position.z, 0));
  }
}