Shader "Custom/Exp1WaterMaskShader"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (0, 0, 1, 1) // Water color
        _CubePosition ("Cube Position", Vector) = (0, 0, 0, 0) // Position of the cube
        _CubeSize ("Cube Size", Vector) = (1, 1, 1, 1) // Size of the cube
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
//        Cull Front // Culls the front faces

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
            };

            fixed4 _WaterColor;
            float3 _CubePosition;
            float3 _CubeSize;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Get the position of the vertex
                float3 worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;

                // Check if the vertex is inside the cube
                if (worldPos.x >= _CubePosition.x - _CubeSize.x * 0.5 &&
                    worldPos.x <= _CubePosition.x + _CubeSize.x * 0.5 &&
                    worldPos.y >= _CubePosition.y - _CubeSize.y * 0.5 &&
                    worldPos.y <= _CubePosition.y + _CubeSize.y * 0.5 &&
                    worldPos.z >= _CubePosition.z - _CubeSize.z * 0.5 &&
                    worldPos.z <= _CubePosition.z + _CubeSize.z * 0.5)
                {
                    return fixed4(0, 0, 0, 0); // Fully transparent if inside the cube
                }

                return _WaterColor; // Return water color if outside
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
