Shader "Custom/BendingShader"
{
    Properties
    {
        _CubePosition ("Cube Position", Vector) = (0, 0, 0, 0)
        _CubeSize ("Cube Size", Vector) = (1, 1, 1, 1)
        _BendStrength ("Bend Strength", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float3 _CubePosition;
            float3 _CubeSize;
            float _BendStrength;

            v2f vert (appdata_t v)
            {
                v2f o;

                // Calculate the position of the vertex
                float3 vertexPos = v.vertex.xyz;

                // Check if the vertex is within the bounds of the cube
                if (vertexPos.x >= _CubePosition.x - _CubeSize.x * 0.5 &&
                    vertexPos.x <= _CubePosition.x + _CubeSize.x * 0.5 &&
                    vertexPos.y >= _CubePosition.y - _CubeSize.y * 0.5 &&
                    vertexPos.y <= _CubePosition.y + _CubeSize.y * 0.5 &&
                    vertexPos.z >= _CubePosition.z - _CubeSize.z * 0.5 &&
                    vertexPos.z <= _CubePosition.z + _CubeSize.z * 0.5)
                {
                    // Bend the vertex away from the cube
                    vertexPos.y += _BendStrength * (_CubeSize.y * 0.5 - (vertexPos.y - _CubePosition.y));
                }

                v.vertex = float4(vertexPos, 1.0);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0, 0.5, 1, 1); // Replace with your desired water color
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}