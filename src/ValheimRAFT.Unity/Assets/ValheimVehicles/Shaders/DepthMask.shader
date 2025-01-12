Shader "Custom/DepthMask"
{
    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100
        ZClip Off
        Cull Off
        ZTest LEqual
        ZWrite On
        Lighting Off
        
        ColorMask 0
        Stencil
        {
            Ref 2 // Stencil reference value to compare against
            Comp Always // Write to the stencil buffer
            Pass Replace // Replace stencil value
         }
        // do nothing
        Pass {}
    }
    Fallback "Diffuse"
}
