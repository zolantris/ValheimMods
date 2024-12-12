Shader "Custom/StandardWithStencil"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Base Texture", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Glossiness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        // Pass 1: Write to the stencil buffer
        Pass
        {
            Tags
            {
                "LightMode"="Always"
            }

            Stencil
            {
                Ref 1 // Reference value to compare
                Comp Equal // Compare stencil buffer value with reference value
                Pass Replace // Replace stencil buffer value with reference value if comparison passes
                Fail Keep // Keep the stencil buffer value if comparison fails
                ZFail Keep // Keep the stencil buffer value if depth test fails
            }

            // Write to the stencil buffer (no lighting calculation here)
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Define the appdata structure explicitly
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            // Define the v2f structure for passing data to the fragment shader
            struct v2f
            {
                float4 pos : POSITION;
                float3 normal : TEXCOORD0;
                float4 color : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            // Vertex Shader
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(mul((float3x3)unity_WorldToObject,
                                         v.normal));
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            // Fragment Shader (Write to the stencil buffer)
            half4 frag(v2f i) : SV_Target
            {
                return half4(1, 1, 1, 1); // Write to the stencil buffer
            }
            ENDCG
        }

        // Pass 2: Standard shader pass with stencil test applied
        Pass
        {
            Tags
            {
                "LightMode"="ForwardBase"
            }

            Stencil
            {
                Ref 1 // Reference value to compare
                Comp Equal // Compare stencil buffer value with reference value
                Pass Keep // Keep the stencil buffer value if comparison passes
                Fail Keep // Do nothing if the comparison fails
                ZFail Keep // Do nothing if the Z-buffer fails
            }

            // Use standard lighting calculations with custom properties
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            // Properties
            uniform sampler2D _MainTex;
            uniform sampler2D _BumpMap;
            uniform float _Glossiness;
            uniform float _Metallic;
            uniform float4 _Color;

            // Vertex Shader
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float3 normal : TEXCOORD0;
                float4 color : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(mul((float3x3)unity_WorldToObject,
                                         v.normal));
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            // Fragment Shader
            half4 frag(v2f i) : SV_Target
            {
                // Sample textures
                half4 col = tex2D(_MainTex, i.uv) * _Color;
                half3 normal = tex2D(_BumpMap, i.uv).rgb;

                // Lighting calculations (simplified PBR)
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                half3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;

                // Diffuse lighting
                half3 diffuse = max(0, dot(normal, lightDir));

                // Specular lighting (simplified)
                half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.pos.xyz);
                half3 halfDir = normalize(lightDir + viewDir);
                half specular = pow(max(0, dot(normal, halfDir)),
                    1.0 / (_Glossiness + 1.0));

                // Combine diffuse and specular contributions
                half3 finalColor = ambient + diffuse * (1 - _Metallic) +
                    specular * _Metallic;

                return half4(finalColor, col.a);
            }
            ENDCG
        }
    }

    Fallback "Standard"
}