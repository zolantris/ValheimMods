// Test water mask
Shader "Custom/WaterMask"
{
    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "RenderQueue"="Transparent-1"
        }
        ZTest LEqual // renders only at layer and lower
        ColorMask 0 // no colors to render
        Cull Off // Cull back faces for inside-out effect

        Stencil
        {
            Ref 1 // Set stencil reference value
            Comp Always // Always write to stencil buffer
            Pass Replace // Replace the stencil buffer value with Ref
        }

        // do nothing
        Pass {}
    }
    Fallback "Diffuse"
}
