using UnityEngine;
namespace ValheimVehicles.Injections;

public class CustomTexture
{
  public Texture Texture { get; internal set; } = null!;


  public Texture Normal { get; internal set; } = null!;


  public int Index { get; internal set; } = 0;
}