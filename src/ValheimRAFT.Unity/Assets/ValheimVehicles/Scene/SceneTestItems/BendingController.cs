using UnityEngine;

namespace ValheimVehicles.Scene
{
    public class BendingController : MonoBehaviour
    {
        public Material bendingMaterial;
        public GameObject cube;

        void Update()
        {
            if (bendingMaterial != null && cube != null)
            {
                // Get the cube's position and size
                Vector3 cubePosition = cube.transform.position;
                Vector3 cubeSize = cube.transform.localScale;

                // Update the shader properties
                bendingMaterial.SetVector("_CubePosition", cubePosition);
                bendingMaterial.SetVector("_CubeSize", cubeSize);
            }
        }
    }
}