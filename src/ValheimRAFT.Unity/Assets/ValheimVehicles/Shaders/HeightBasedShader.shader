Shader "Custom/WorldHeightBasedShader"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MaxHeight ("Max Height", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        LOD 100
        Cull Off

        Blend SrcAlpha OneMinusSrcAlpha // Enable transparency

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float worldY : TEXCOORD1; // World Y position
            };

            fixed4 _Color;
            float _MaxHeight;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Convert to world position
                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.worldY = worldPosition.y; // Use world Y position

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Check if the world Y position is above the maximum height
                if (i.worldY > _MaxHeight)
                {
                    return fixed4(0, 0, 0, 0); // Return fully transparent color
                }
                return _Color; // Render the color normally
            }
            ENDCG
        }
    }
}
