Shader "Custom/CornerBasedBendingShader"
{
    Properties
    {
        _CubePosition ("Cube Position", Vector) = (0, 0, 0, 0)
        _CubeSize ("Cube Size", Vector) = (1, 1, 1, 1)
        _BendStrength ("Bend Strength", Range(0, 1)) = 0.5
        _MainTex ("Texture", 2D) = "white" {}
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
            sampler2D _MainTex;

            v2f vert (appdata_t v)
            {
                v2f o;

                // Calculate the position of the vertex
                float3 vertexPos = v.vertex.xyz;

                // Cube dimensions
                float halfWidth = _CubeSize.x * 0.5;
                float halfHeight = _CubeSize.y * 0.5;
                float halfDepth = _CubeSize.z * 0.5;

                // Calculate the cube corners
                float3 corners[8];
                corners[0] = _CubePosition + float3(-halfWidth, -halfHeight, -halfDepth);
                corners[1] = _CubePosition + float3(halfWidth, -halfHeight, -halfDepth);
                corners[2] = _CubePosition + float3(halfWidth, halfHeight, -halfDepth);
                corners[3] = _CubePosition + float3(-halfWidth, halfHeight, -halfDepth);
                corners[4] = _CubePosition + float3(-halfWidth, -halfHeight, halfDepth);
                corners[5] = _CubePosition + float3(halfWidth, -halfHeight, halfDepth);
                corners[6] = _CubePosition + float3(halfWidth, halfHeight, halfDepth);
                corners[7] = _CubePosition + float3(-halfWidth, halfHeight, halfDepth);

                // Initialize displacement vector
                float3 displacement = float3(0, 0, 0);
                float closestDistance = 0.0;

                // Check distances to the cube corners
                for (int i = 0; i < 8; i++)
                {
                    float distance = length(vertexPos - corners[i]);
                    if (distance < closestDistance || closestDistance == 0.0)
                    {
                        closestDistance = distance;
                        float bendAmount = (_BendStrength * (1.0 - (distance / (halfWidth * 2.0))));
                        displacement += bendAmount * normalize(vertexPos - corners[i]);
                    }
                }

                // Apply the displacement
                vertexPos += displacement;

                v.vertex = float4(vertexPos, 1.0);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv); // Sample the texture
                return col; // Return the sampled color
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
