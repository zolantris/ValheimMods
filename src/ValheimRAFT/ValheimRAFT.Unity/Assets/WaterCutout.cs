using UnityEngine;

public class WaterCutout : MonoBehaviour
{
    public Material waterMaterial;
    public float waterLevel = 0.0f; // Adjust this to match your water plane's height

    void Start()
    {
        // Set the material for the cube
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = waterMaterial;
    }

    void Update()
    {
        // Check the position of the cube and adjust the material's properties as necessary
        Vector3 position = transform.position;

        // If above water, set shader transparency
        if (position.y > waterLevel)
        {
            waterMaterial.color = new Color(waterMaterial.color.r, waterMaterial.color.g, waterMaterial.color.b, 0); // Fully transparent
        }
        else
        {
            waterMaterial.color = new Color(waterMaterial.color.r, waterMaterial.color.g, waterMaterial.color.b, 1); // Fully opaque
        }
    }
}