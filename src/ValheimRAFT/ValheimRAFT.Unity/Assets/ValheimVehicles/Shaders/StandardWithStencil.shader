Shader "Custom/StandardWithStencil"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMap ("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


        // Blending state
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [Enum(ZWrite)] _ZWrite ("ZWrite", Int) = 1
        [Enum(CullMode)] _CullMode ("Cull", Integer) = 2 
        _StencilMask("StencilMask", Range(0,255)) = 44
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
        }
        LOD 200

        ZWrite Off
        Cull Back
        ZTest LEqual
//        ZTest LEqual
//        Stencil
//        {
//            Ref 1
//            Comp Always
//            Pass Replace
//        }
//        Stencil
//            {
//                Ref 1
//                Comp NotEqual
//                Pass Keep
//            }
        
//        Pass
//        {
//            Stencil
//            {
//                Ref 44
//                Comp Equal
//                Pass Replace
//            }
//            ColorMask 0
//        }
        
        Pass
        {
            Stencil
            {
                Ref 44
                Comp Equal
                Pass Replace
            }
            ColorMask 0
        }
//        
//        Pass
//        {
//            Stencil
//            {
//                Ref 44
//                Comp NotEqual
//                Pass Replace
//            }
            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0

            sampler2D _MainTex;

            struct Input
            {
                float2 uv_MainTex;
            };

            half _Glossiness;
            half _Metallic;
            fixed4 _Color;

            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            // #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                // Albedo comes from a texture tinted by color
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a;
            }
            ENDCG
    }

    FallBack "Diffuse"
}