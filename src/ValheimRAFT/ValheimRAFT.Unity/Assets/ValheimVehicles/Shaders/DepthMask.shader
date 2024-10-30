Shader "Custom/DepthMask"
{
    SubShader
    {
        Tags { "Queue"="Overlay" }
        ZClip Off
        Cull Off
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
