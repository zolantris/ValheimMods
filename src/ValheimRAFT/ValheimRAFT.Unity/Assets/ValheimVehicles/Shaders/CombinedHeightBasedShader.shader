Shader "Custom/CombinedHeightBasedShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 0)
        _MaxHeight ("Max Height", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
//        ColorMask 0
//        ZWrite On
//        ZTest GEqual
//        Stencil
//        {
//            Ref 2 // Match the stencil reference value in SelectiveMask
//            Comp Always // Render where stencil value is NOT equal to Ref
//            Pass Keep // Keep the existing stencil value
//        }
        
//        ZWrite On
//        ZTest LEqual
//        Cull Off

        Pass
        {
            Stencil
            {
                Ref 1 // Match the stencil reference value in SelectiveMask
                Comp Equal  // Render where stencil value is NOT equal to Ref
                Pass Replace // Keep the existing stencil value
            }
            ZTest GEqual
            ZClip On
            ZWrite On
            ZTest GEqual

            Blend SrcColor OneMinusSrcAlpha
//            Blend DstAlpha OneMinusDstAlpha
//            ZWrite Off // No depth writing
            Cull Off // Render both sides

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _MaxHeight;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float worldY : TEXCOORD1; 
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.worldY = worldPosition.y;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Check if the world Y position is above the maximum height
                // if (i.worldY > _MaxHeight)
                // {
                //     return fixed4(0, 0, 0, 0); // Fully transparent
                // }

                return fixed4(_Color.rgb, _Color.a); // Return the color with alpha
            }
            ENDCG
        }

//        Pass
//        {
//            Stencil
//            {
//                Ref 9833 // Match the stencil reference value in SelectiveMask
//                Comp LEqual // Render where stencil value is NOT equal to Ref
//                Pass Keep // Keep the existing stencil value
////                ZFail Replace
////                WriteMask 1
//            }
//            ZWrite On
//            ZTest Always
////            ZTest GEqual
//
////            Blend SrcColor OneMinusSrcAlpha
//            Blend DstColor DstAlpha
////            Blend DstAlpha OneMinusDstAlpha
////            ZWrite Off // No depth writing
//            Cull Off // Render both sides
//
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #include "UnityCG.cginc"
//
//            sampler2D _MainTex;
//            fixed4 _Color;
//            float _MaxHeight;
//
//            struct appdata_t
//            {
//                float4 vertex : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            struct v2f
//            {
//                float2 uv : TEXCOORD0;
//                float4 vertex : SV_POSITION;
//                float worldY : TEXCOORD1; 
//            };
//
//            v2f vert(appdata_t v)
//            {
//                v2f o;
//                o.vertex = UnityObjectToClipPos(v.vertex);
//                o.uv = v.uv;
//
//                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
//                o.worldY = worldPosition.y;
//
//                return o;
//            }
//
//            fixed4 frag(v2f i) : SV_Target
//            {
//                // Check if the world Y position is above the maximum height
//                // if (i.worldY > _MaxHeight)
//                // {
//                //     return fixed4(0, 0, 0, 0); // Fully transparent
//                // }
//
//                return fixed4(_Color.rgb, _Color.a); // Return the color with alpha
//            }
//            ENDCG
//        }
    }

    Fallback "Diffuse"
}
