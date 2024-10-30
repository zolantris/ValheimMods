Shader "Custom/DepthMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 0)
        _MaxHeight ("Max Height", Float) = 1.0
    }
    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "RenderQueue"="Transparent-1"
        }
                Blend SrcColor OneMinusSrcAlpha
        LOD 100
        ZTest LEqual // renders only at layer and lower
        ZClip Off // no clip when camera intersects
        ColorMask 0  // no colors to render
        Cull Off // Cull back faces for inside-out effect
        Stencil
        {
            Ref 1 // Set stencil reference value
            Comp Always // Always write to stencil buffer
            Pass Replace // Replace the stencil buffer value with Ref
        }

        // do nothing
        Pass
        {
            Stencil
            {
                Ref 2
                Comp Equal
                Pass Keep
            }
            ZTest Never
        }
//        Pass
//        {
//        }
    }
//    Fallback "Diffuse"
}
