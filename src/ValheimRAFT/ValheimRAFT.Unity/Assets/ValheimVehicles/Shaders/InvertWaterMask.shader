Shader "Custom/InvertWaterMask"
{
    Properties
    {
        _WaterLevel ("Water Level", Float) = 0.0
        _MaskPosition ("Mask Position", Vector) = (0,0,0,0)
        _MaskSize ("Mask Size", Vector) = (1,1,1,0)
        _WaterColor ("Water Color", Color) = (0,0.5,0.7,1)
        _Transparency ("Transparency Outside Mask", Range(0,1)) = 0.7
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off // Double-sided rendering
        ZWrite On // Enable depth writing to avoid flickering
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
            float _Transparency;
            float _WaterLevel;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Calculate mask bounds
                float3 minBounds = _MaskPosition.xyz - _MaskSize.xyz * 0.5;
                float3 maxBounds = _MaskPosition.xyz + _MaskSize.xyz * 0.5;

                // Inside the mask volume, make fully transparent
                if (i.worldPos.x > minBounds.x && i.worldPos.x < maxBounds.x &&
                    i.worldPos.y > minBounds.y && i.worldPos.y < maxBounds.y &&
                    i.worldPos.z > minBounds.z && i.worldPos.z < maxBounds.z)
                {
                    return float4(0, 0, 0, 0); // Fully transparent inside
                }
                
                // Render water color only on submerged walls
                if (i.worldPos.y <= _WaterLevel)
                {
                    return float4(_WaterColor.rgb, _Transparency); // Water color below water level
                }
                
                // Fully transparent above the water level
                return float4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
