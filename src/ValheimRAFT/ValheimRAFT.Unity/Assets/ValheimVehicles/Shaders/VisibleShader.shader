Shader "Custom/CombinedHeightBasedShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        _SrcBlend ("Source Blend", Float) = 1 // One
        _DstBlend ("Destination Blend", Float) = 10 // One Minus Src Alpha
        _ZWrite ("Z Write", Float) = 0 // Off
        _BlurAmount ("Blur Amount", Range(0, 10)) = 0 // Blur strength
        _MaxHeight ("Max Height", Float) = 1.0 // Height threshold for visibility
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }

        Cull [_Cull]

        Pass
        {
            Stencil
            {
                Ref 9833
                Comp NotEqual
                Pass Keep
            }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _BlurAmount;
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
                fixed4 color : COLOR;
                float worldY : TEXCOORD1; // World Y position
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Convert to world position
                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.worldY = worldPosition.y; // Store world Y position

                o.color = _Color; // Pass color to fragment
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Check if the world Y position is above the maximum height
                if (i.worldY > _MaxHeight)
                {
                    // Return fully transparent
                    return fixed4(0, 0, 0, 0); // Transparent
                }

                // Calculate the main texture color
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Calculate blur using a simple 3x3 kernel
                float2 offset = _BlurAmount / 100.0; // Scale blur effect
                fixed4 blurColor = col;

                // Sample surrounding pixels for blur
                blurColor += tex2D(_MainTex, i.uv + float2(-offset.x, 0)) * 0.111; // Left
                blurColor += tex2D(_MainTex, i.uv + float2(offset.x, 0)) * 0.111; // Right
                blurColor += tex2D(_MainTex, i.uv + float2(0, -offset.y)) * 0.111; // Down
                blurColor += tex2D(_MainTex, i.uv + float2(0, offset.y)) * 0.111; // Up

                // Normalize the color
                blurColor = blurColor * (1.0 / 1.444);

                // Return the final color
                return blurColor;
            }
            ENDCG
        }
    }

    // Fallback to a built-in shader if this fails
    Fallback "Diffuse"
}
