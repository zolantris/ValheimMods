using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterMaskController : MonoBehaviour
{
    public Material waterMaterial;
    public GameObject waterCube;

    void Update()
    {
        if (waterMaterial != null && waterCube != null)
        {
            // Get the cube's position and size
            Vector3 cubePosition = waterCube.transform.position;
            Vector3 cubeSize = waterCube.transform.localScale;

            // Update the shader properties
            waterMaterial.SetVector("_CubePosition", cubePosition);
            waterMaterial.SetVector("_CubeSize", cubeSize);
        }
    }
}
