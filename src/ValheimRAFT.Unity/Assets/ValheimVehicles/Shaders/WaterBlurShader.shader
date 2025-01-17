Shader "Custom/WaterDistortionBlur"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" { }
        _GrabTexture ("Grabbed Scene", 2D) = "white" { }
        _TintColor ("Tint Color", Color) = (0, 0, 1, 0.5)
        _BlurAmount ("Blur Amount", Range(0, 10)) = 3
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.2
        _DistortionSpeed ("Distortion Speed", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        // Step 1: Grab the scene behind the object into a texture (outside of any Pass)
        GrabPass
        {
            "_GrabTexture"
        }

        Pass
        {
            Tags
            {
                "LightMode"="Always"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Properties
            sampler2D _MainTex;
            sampler2D _GrabTexture; // The grabbed background texture
            float4 _TintColor;
            float _BlurAmount;
            float _DistortionStrength;
            float _DistortionSpeed;

            // Texture Sampler Offset
            float2 _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Main Fragment Shader
            float4 frag(v2f i) : SV_Target
            {
                // Apply distortion to simulate refraction (using UV offsets)
                float2 distortion = sin(i.uv * 10.0 + _Time * _DistortionSpeed)
                    * _DistortionStrength;
                float2 distortedUV = i.uv + distortion;

                // Sample the background texture (from GrabPass)
                float4 background = tex2D(_GrabTexture, distortedUV);

                // Apply blur by sampling surrounding pixels
                float2 offset = _BlurAmount * _MainTex_TexelSize;
                float4 blurColor = background * 0.36;
                blurColor += tex2D(_GrabTexture,
                    distortedUV + float2(offset.x, 0)) * 0.18;
                blurColor += tex2D(_GrabTexture,
                    distortedUV - float2(offset.x, 0)) * 0.18;
                blurColor += tex2D(_GrabTexture,
  distortedUV + float2(0, offset.y)) * 0.18;
                blurColor += tex2D(_GrabTexture,
   distortedUV - float2(0, offset.y)) * 0.18;

                // Apply tint color
                blurColor *= _TintColor;

                return blurColor;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}