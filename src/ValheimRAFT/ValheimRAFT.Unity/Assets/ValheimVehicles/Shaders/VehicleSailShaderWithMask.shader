Shader "Custom/StencilWithAllProperties"
{
    Properties
    {
        _Color ("Color", Color) = (1.000000,1.000000,1.000000,1.000000)
        _MainTex ("Albedo", 2D) = "white" { }
        _Cutoff ("Alpha Cutoff", Range(0.000000, 1.000000)) = 0.500000
        _Glossiness ("Smoothness", Range(0.000000, 1.000000)) = 0.500000
        _GlossMapScale ("Smoothness Scale", Range(0.000000, 1.000000)) = 1.000000
        [Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0.000000
        [Gamma] _Metallic ("Metallic", Range(0.000000, 1.000000)) = 0.000000
        _MetallicGlossMap ("Metallic", 2D) = "white" { }
        [ToggleOff] _SpecularHighlights ("Specular Highlights", Float) = 1.000000
        [ToggleOff] _GlossyReflections ("Glossy Reflections", Float) = 1.000000
        _BumpScale ("Scale", Float) = 1.000000
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" { }
        _Parallax ("Height Scale", Range(0.005000, 0.080000)) = 0.020000
        _ParallaxMap ("Height Map", 2D) = "black" { }
        _OcclusionStrength ("Strength", Range(0.000000, 1.000000)) = 1.000000
        _OcclusionMap ("Occlusion", 2D) = "white" { }
        _EmissionColor ("Color", Color) = (0.000000,0.000000,0.000000,1.000000)
        _EmissionMap ("Emission", 2D) = "white" { }
        _DetailMask ("Detail Mask", 2D) = "white" { }
        _DetailAlbedoMap ("Detail Albedo x2", 2D) = "grey" { }
        _DetailNormalMapScale ("Scale", Float) = 1.000000
        [Normal] _DetailNormalMap ("Normal Map", 2D) = "bump" { }
        [Enum(UV0, 0, UV1, 1)] _UVSec ("UV Set for secondary textures", Float) = 0.000000
        [HideInInspector] _Mode ("__mode", Float) = 0.000000
        [HideInInspector] _SrcBlend ("__src", Float) = 1.000000
        [HideInInspector] _DstBlend ("__dst", Float) = 0.000000
        [HideInInspector] _ZWrite ("__zw", Float) = 1.000000
        _StencilRef("Stencil Reference Value", Range(0,255)) = 1
        _StencilComp("Stencil Comparison", Float) = 4 // Only where stencil value equals _StencilRef
    }

//    SubShader
//    {
//        // Pass 1: Write to Stencil Buffer
//        Pass
//        {
//            Tags { "Queue"="Background" }
//
//            Stencil
//            {
//                Ref [_StencilRef]
//                Comp Always
//                Pass Replace
//            }
//
//            ColorMask 0      // Don't write color, only modify stencil
//            ZWrite On        // Enable depth writing
//            ZTest LEqual     // Standard depth test
//        }
//
//        // Pass 2: Custom shader logic, only if stencil test passes
//        Pass
//        {
//            Tags { "Queue"="Overlay" }
//
//            Stencil
//            {
//                Ref [_StencilRef]     // Reference stencil value
//                Comp Equal            // Only where stencil equals the reference value
//                Pass Keep
//            }
//
//            ColorMask 0      // Don't write color, only modify stencil
//        }
//    }
    SubShader
    {
        Stencil
        {
            Ref [_StencilRef]
            Comp Equal
            Pass Zero
        }
//        ZWrite On
//        ZClip Off
//        ZTest Always
                
        Pass
        {
            Stencil
            {
                Ref [_StencilComp]
                Comp NotEqual
                Pass Keep
            }
            ColorMask 0      // Don't write color, only modify stencil
            ZWrite On        // Enable depth writing
            ZTest LEqual     // Standard depth test
        }
        //        Pass
//        {
//            Stencil
//            {
//                Ref [_StencilComp]
//                Comp Greater
//                Fail DecrSat
//                Pass Keep
//            }
//            ZTest LEqual     // Standard depth test
////            SetTexture [_MainTex]{}
//        }
    }

    Fallback "Standard"
}
