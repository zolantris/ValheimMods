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
        Tags
        {
            "RenderType"="Transparent"
            "LightMode"="Always"
            "Queue"="Overlay" // Changed queue to Overlay to render after opaque geometry
        }
        LOD 100
        Cull Off
        ZWrite Off
        Fog
        {
            Mode Off
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "SharedHeightShader.cginc"
            ENDCG
        }
    }

    Fallback "Unlit/Transparent"
}