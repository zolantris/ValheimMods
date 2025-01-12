Shader "Custom/HeightBasedShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 0)
        _MaxHeight ("Max Height", Float) = 30.0
    }

    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

       Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "SharedHeightShader.cginc"
            ENDCG
        }
    }

    Fallback "Diffuse"
}
