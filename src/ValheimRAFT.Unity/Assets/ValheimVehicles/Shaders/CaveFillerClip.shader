Shader "Custom/CaveFillerClip"
{
    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100
        ZClip On
        Cull Off
        ZTest Less
        ZWrite On
        Lighting Off
        
        ColorMask 0
        Pass {}
    }
    Fallback "Diffuse"
}
