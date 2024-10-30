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
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent-2"
        }
        LOD 100
        Cull Off
        ZClip Off
        Blend SrcColor OneMinusSrcAlpha

        Stencil
        {
            Ref 2 // Stencil reference value to compare against
            Comp Always // Write to the stencil buffer
            Pass Keep // Replace stencil value
        }

        Pass
        {
            Stencil
            {
                Ref 1 // Match the stencil reference value in SelectiveMask
                Comp Equal  // Render where stencil value is NOT equal to Ref
                Pass IncrSat // Keep the existing stencil value
                Fail DecrSat
                ZFail DecrSat
            }
            ZTest Always
//            ZTest GEqual
//            ZClip On
//            ZWrite On
//            ZTest GEqual

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
                if (i.worldY > _MaxHeight)
                {
                    return fixed4(0, 0, 0, 0); // Fully transparent
                }

                return fixed4(_Color.rgb, _Color.a); // Return the color with alpha
            }
            ENDCG
        }


       Pass
        {
            Stencil
            {
                Ref 1 // Match the stencil reference value in SelectiveMask
                Comp GEqual  // Render where stencil value is NOT equal to Ref
                Pass Keep // Keep the existing stencil value
            }
            ZTest Always

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
            
            struct Input
            {
                float2 uv_MainTex;
            };

            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            // #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)

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
                if (i.worldY > _MaxHeight)
                {
                    return fixed4(0, 0, 0, 0); // Fully transparent
                }

                fixed4 c = tex2D (_MainTex, i.uv) * _Color;
                return c * _Color.a;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
