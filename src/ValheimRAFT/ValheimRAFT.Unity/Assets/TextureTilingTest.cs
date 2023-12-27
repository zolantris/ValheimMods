using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureTilingTest : MonoBehaviour
{
    public float textureScale = 4f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var uvoffset = transform.parent ? new Vector2(-transform.localPosition.x, -transform.localPosition.z) : new Vector2(-transform.position.x, -transform.position.z);
        uvoffset /= textureScale;

        var uvrotation = transform.parent ? -transform.localEulerAngles.y : -transform.eulerAngles.y;
        uvrotation /= 360f;

        var uvscale = new Vector2(transform.localScale.x, transform.localScale.z);
        uvscale /= textureScale;

        var renderers = GetComponentsInChildren<MeshRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].material;
            mat.SetTextureOffset("_MainTex", uvoffset);
            mat.SetTextureScale("_MainTex", uvscale);
            mat.SetTextureOffset("_MainNormal", uvoffset);
            mat.SetTextureScale("_MainNormal", uvscale);
            mat.SetFloat("_MainRotation", uvrotation);
        }
    }
}
