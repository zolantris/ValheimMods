Shader "Custom/DoubleSidedTransparentStencilMask"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.5)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        LOD 200

        Pass
        {
            Stencil
            {
                Ref 1 // Match the stencil reference value in SelectiveMask
                Comp NotEqual // Render where stencil value is NOT equal to Ref
                Pass Replace // Keep the existing stencil value
                //                Fail IncrSat
                //                ZFail IncrSat
            }

            Name "Forward"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off // Render both front and back faces
            ZWrite On // Do not write to the depth buffer
            Lighting Off // Disable lighting to avoid lighting differences
            ZClip Off
            ZTest LEqual


            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc" // For Unity's built-in functions

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; // Add normal for lighting calculations
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                // Pass the normal to the fragment shader
            };

            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                // Use mul to transform the vertex position from object space to clip space
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

                // For double-sided rendering, reverse the normal on back faces
                o.normal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                // Transform normal to world space
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Use the normal to determine the final color (no lighting here, simple color output)
                return _Color; // Output the specified color with transparency
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}