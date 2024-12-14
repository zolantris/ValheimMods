Shader "Custom/WallWithHole"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _StencilRef ("Stencil Reference Value", Range(0,255)) = 1  // New property for stencil reference
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // Pass 1: Write to Stencil Buffer
        Pass
        {
            Name "STENCIL_WRITE"
            Tags { "LightMode"="Always" }

            Stencil
            {
                Ref 44       // Use the unique stencil reference value for each wall
                Comp Always             // Always pass the stencil test
                Pass Replace            // Replace the stencil value with the reference value
            }

            ColorMask 0               // Do not write color (only stencil buffer is modified)
            ZWrite On                 // Enable depth writing
            ZTest LEqual              // Standard depth test
        }

        // Pass 2: Render the wall normally (without the cut-out region)
        Pass
        {
            Name "STENCIL_RENDER"
            Tags { "LightMode"="ForwardBase" }

            Stencil
            {
                Ref 44       // Reference value to compare with stencil buffer
                Comp NotEqual            // Only render where stencil value is NOT equal to the reference value (1)
                Pass Keep               // Keep the current stencil value
            }
        }
        
                    // Standard shader logic to render the wall
            CGPROGRAM
            #pragma surface surf Standard alpha
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;

            struct Input
            {
                float2 uv_MainTex : TEXCOORD0;
            };

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
                o.Alpha = tex2D(_MainTex, IN.uv_MainTex).a;
            }
            ENDCG
    }

    Fallback "Standard"
}