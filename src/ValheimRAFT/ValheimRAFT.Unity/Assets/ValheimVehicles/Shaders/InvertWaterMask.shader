Shader "Custom/InvertWaterMask"
{
    Properties
    {
        _MaskPosition ("Mask Position", Vector) = (0,0,0,0)
        _MaskSize ("Mask Size", Vector) = (1,1,1,0)
        _WaterColor ("Water Color", Color) = (0,0.5,0.7,1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _MaskPosition;
            float4 _MaskSize;
            float4 _WaterColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Calculate bounds
                float3 minBounds = _MaskPosition.xyz - _MaskSize.xyz * 0.5;
                float3 maxBounds = _MaskPosition.xyz + _MaskSize.xyz * 0.5;

                // Check if pixel is outside mask bounds
                if (i.worldPos.x < minBounds.x || i.worldPos.x > maxBounds.x ||
                    i.worldPos.y < minBounds.y || i.worldPos.y > maxBounds.y ||
                    i.worldPos.z < minBounds.z || i.worldPos.z > maxBounds.z)
                {
                    // Render water color outside the mask area
                    return _WaterColor;
                }
                else
                {
                    // Transparent return color if inside the mask area
                    return float4(0, 0, 0, 0); // RGBA transparent
                }
            }
            ENDCG
        }
    }
}
